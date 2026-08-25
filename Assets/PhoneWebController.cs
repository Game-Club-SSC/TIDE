using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Embedded HTTP server that serves a mobile-friendly web controller page
/// and handles API requests from phones on the local network.
/// </summary>
[DisallowMultipleComponent]
public class PhoneWebController : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private int port = 8080;
    [SerializeField] private bool startOnAwake = false;
    [SerializeField] private bool showDebugLog = true;

    [Header("Network Safety")]
    [Tooltip("Off by default because this server uses plain HTTP. Turn on only for a trusted LAN after accepting that other devices on that LAN may read controller traffic.")]
    [SerializeField] private bool allowInsecureLanAccess = false;

    [Header("Pairing")]
    [SerializeField] private int pairingCodeLength = 6;

    private const int MaxConcurrentRequests = 4;
    private const int MaxPendingCommands = 64;
    private const int MaxCommandsPerFrame = 32;
    private const int MaxPendingLogs = 64;
    private const int PairRequestBodyLimit = 256;
    private const int CommandRequestBodyLimit = 4096;
    private const int RequestBodyReadTimeoutMs = 3000;
    private static readonly TimeSpan PairingCodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PairingAttemptWindow = TimeSpan.FromMinutes(1);

    private HttpListener httpListener;
    private Thread listenerThread;
    private volatile bool isRunning;
    private string serverUrl;
    private string cachedHtmlPage;
    private long pairingSucceededGeneration = -1;
    private PhonePairingGuard pairingGuard;
    private readonly SemaphoreSlim requestSlots =
        new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);

    // Authorization, revocation, queueing, and dispatch share one generation gate.
    private readonly PhoneServerSessionGate<PhoneInputCommand> sessionGate =
        new PhoneServerSessionGate<PhoneInputCommand>(MaxPendingCommands);
    private readonly Queue<string> pendingLogs = new Queue<string>();
    private readonly object logLock = new object();

    // Game state snapshot (updated from main thread, read from HTTP thread)
    private string cachedGameStateJson = "{}";
    private readonly object stateLock = new object();

    public static PhoneWebController Instance { get; private set; }
    public bool IsRunning => isRunning;
    public string ServerUrl => serverUrl;
    public string PairingCode => pairingGuard?.GetCurrentCode(DateTime.UtcNow) ?? string.Empty;
    public event Action<PhoneInputCommand> OnCommandReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartServer();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            StopServer();
            Instance = null;
        }
    }

    private void Update()
    {
        long pairedGeneration = Interlocked.Exchange(ref pairingSucceededGeneration, -1);
        if (pairedGeneration >= 0)
        {
            sessionGate.TryRunGeneration(pairedGeneration, () =>
            {
                PhoneInputBridge.Instance?.SetPaired(true);
                Log("Phone paired successfully!");
            });
        }

        // Process pending commands on the main Unity thread
        ProcessPendingCommands();
        ProcessPendingLogs();
        // Update game state snapshot for API responses
        UpdateGameStateSnapshot();
    }

    public void StartServer()
    {
        if (isRunning)
        {
            Log("Server is already running.");
            return;
        }

        try
        {
            sessionGate.Advance(false, () =>
            {
                isRunning = false;
                Interlocked.Exchange(ref pairingSucceededGeneration, -1);
                pairingGuard = null;
                PhoneControllerAuthService.RevokeAllTokens();
            });
            httpListener = new HttpListener();

            string localIP = GetLocalIPAddress();
            httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
            if (allowInsecureLanAccess && localIP != "127.0.0.1")
            {
                httpListener.Prefixes.Add($"http://{localIP}:{port}/");
            }
            httpListener.Start();

            sessionGate.Advance(true, () =>
            {
                isRunning = true;
                GeneratePairingCode();
            });
            BuildServerUrl(localIP);

            // Pre-build the HTML page
            cachedHtmlPage = PhoneWebPageBuilder.BuildHtmlPage();

            // Start listener thread
            listenerThread = new Thread(ListenerLoop)
            {
                IsBackground = true,
                Name = "PhoneWebListener"
            };
            listenerThread.Start();

            Log($"Phone Web Controller server started on port {port}");
            Log($"Server URL: {serverUrl}");
            Log($"Pairing Code: {PairingCode}");
            if (allowInsecureLanAccess)
            {
                Log("Warning: trusted-LAN access is on and controller traffic is not encrypted.");
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to start server: {ex.Message}");
            sessionGate.Advance(false, () =>
            {
                isRunning = false;
                Interlocked.Exchange(ref pairingSucceededGeneration, -1);
                pairingGuard = null;
                PhoneControllerAuthService.RevokeAllTokens();
            });
            if (httpListener != null)
            {
                try
                {
                    httpListener.Close();
                }
                catch (Exception)
                {
                    // Keep the original start error.
                }
                httpListener = null;
            }
        }
    }

    public void StopServer()
    {
        sessionGate.Advance(false, () =>
        {
            isRunning = false;
            Interlocked.Exchange(ref pairingSucceededGeneration, -1);
            PhoneControllerAuthService.RevokeAllTokens();
            pairingGuard = null;
        });
        PhoneInputBridge.Instance?.SetPaired(false);

        if (httpListener != null)
        {
            try
            {
                httpListener.Stop();
                httpListener.Close();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
            httpListener = null;
        }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            try
            {
                listenerThread.Join(1000);
            }
            catch (Exception)
            {
                // Ignore thread join errors
            }
            listenerThread = null;
        }

        Log("Phone Web Controller server stopped.");
    }

    public void RegeneratePairingCode()
    {
        sessionGate.AdvanceKeepingRunState(() =>
        {
            PhoneControllerAuthService.RevokeAllTokens();
            Interlocked.Exchange(ref pairingSucceededGeneration, -1);
            GeneratePairingCode();
        });
        PhoneInputBridge.Instance?.SetPaired(false);
        Log($"New pairing code: {PairingCode}");
    }

    private void GeneratePairingCode()
    {
        // The bundled controller page has six input boxes.
        pairingCodeLength = 6;
        if (pairingGuard == null)
        {
            pairingGuard = new PhonePairingGuard(
                pairingCodeLength,
                PairingCodeLifetime,
                PairingAttemptWindow,
                maxAttemptsPerPeer: 5,
                maxAttemptsGlobal: 20,
                nowUtc: DateTime.UtcNow);
        }
        else
        {
            pairingGuard.Rotate(DateTime.UtcNow);
        }
    }

    private void BuildServerUrl(string localIP)
    {
        string host = allowInsecureLanAccess ? localIP : "127.0.0.1";
        serverUrl = $"http://{host}:{port}";
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(address))
                {
                    return address.ToString();
                }
            }
        }
        catch (Exception)
        {
            // Fall through
        }
        return "127.0.0.1";
    }

    private void ListenerLoop()
    {
        while (isRunning)
        {
            try
            {
                if (httpListener == null || !httpListener.IsListening)
                {
                    break;
                }

                HttpListenerContext context = httpListener.GetContext();
                if (!requestSlots.Wait(0))
                {
                    context.Response.StatusCode = 429;
                    context.Response.AddHeader("Retry-After", "1");
                    WriteJsonResponse(context, "{\"error\":\"server busy\"}");
                    continue;
                }

                ThreadPool.QueueUserWorkItem(_ => HandleContext(context));
            }
            catch (HttpListenerException)
            {
                // Listener was stopped
                break;
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    QueueLog($"Listener error: {ex.Message}");
                }
                break;
            }
        }
    }

    private void HandleContext(HttpListenerContext context)
    {
        try
        {
            HandleRequest(context);
        }
        catch (Exception ex)
        {
            QueueLog($"Request handling error: {ex.Message}");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch (Exception)
            {
                // Ignore
            }
        }
        finally
        {
            requestSlots.Release();
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        string path = context.Request.Url.AbsolutePath.ToLowerInvariant();
        string method = context.Request.HttpMethod.ToUpperInvariant();
        string origin = context.Request.Headers["Origin"];

        if (!IsOriginAllowed(origin, context.Request.Url))
        {
            context.Response.StatusCode = 403;
            WriteJsonResponse(context, "{\"error\":\"cross-origin access denied\"}");
            return;
        }

        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", origin);
            context.Response.AddHeader("Vary", "Origin");
        }

        if (method == "OPTIONS")
        {
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
            context.Response.AddHeader("Access-Control-Max-Age", "300");
            context.Response.StatusCode = 204;
            context.Response.Close();
            return;
        }

        switch (path)
        {
            case "/":
            case "/index.html":
                ServeHtmlPage(context);
                break;

            case "/api/pair":
                HandlePairRequest(context);
                break;

            case "/api/command":
                HandleCommandRequest(context);
                break;

            case "/api/state":
                HandleStateRequest(context);
                break;

            case "/api/qr":
                HandleQrDataRequest(context);
                break;

            default:
                context.Response.StatusCode = 404;
                WriteJsonResponse(context, "{\"error\":\"not found\"}");
                break;
        }
    }

    private void ServeHtmlPage(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            context.Response.StatusCode = 405;
            WriteJsonResponse(context, "{\"error\":\"method not allowed\"}");
            return;
        }

        string html = cachedHtmlPage ?? PhoneWebPageBuilder.BuildHtmlPage();
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }

    private void HandlePairRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "POST")
        {
            context.Response.StatusCode = 405;
            WriteJsonResponse(context, "{\"error\":\"method not allowed\"}");
            return;
        }

        if (!TryReadRequestBody(context, PairRequestBodyLimit, out string body))
        {
            return;
        }
        // Simple JSON parsing for {"code":"123456"}
        string submittedCode = ExtractJsonString(body, "code");

        if (string.IsNullOrEmpty(submittedCode))
        {
            context.Response.StatusCode = 400;
            WriteJsonResponse(context, "{\"error\":\"missing code\"}");
            return;
        }

        if (!sessionGate.TryCapture(
                () => pairingGuard,
                guard => guard != null,
                out PhoneSessionAttempt<PhonePairingGuard> attempt))
        {
            context.Response.StatusCode = 503;
            WriteJsonResponse(context, "{\"error\":\"server stopping\"}");
            return;
        }

        PhonePairingGuard guard = attempt.State;
        string peer = context.Request.RemoteEndPoint?.Address.ToString();
        PhonePairingResult pairingResult = guard.TryPair(peer, submittedCode, DateTime.UtcNow);
        bool success = pairingResult == PhonePairingResult.Success;
        string token = null;
        if (success)
        {
            bool sessionCurrent = sessionGate.TryComplete(
                attempt,
                capturedGuard => ReferenceEquals(pairingGuard, capturedGuard),
                () =>
                {
                    string issuedToken = PhoneControllerAuthService.GenerateSessionToken();
                    // Update the Unity object from Update, not from the HTTP worker.
                    Interlocked.Exchange(
                        ref pairingSucceededGeneration,
                        attempt.Stamp.Generation);
                    return issuedToken;
                },
                out token);
            if (!sessionCurrent)
            {
                context.Response.StatusCode = 503;
                WriteJsonResponse(context, "{\"error\":\"server session changed\"}");
                return;
            }
        }
        string tokenJson = success ? $",\"token\":\"{token}\"" : string.Empty;
        string message = success ? "Paired successfully!" :
            pairingResult == PhonePairingResult.RateLimited ? "Too many pairing attempts" :
            pairingResult == PhonePairingResult.ExpiredCode ? "Pairing code expired" :
            "Invalid code";
        string response = $"{{\"success\":{success.ToString().ToLowerInvariant()},\"message\":\"{message}\"{tokenJson}}}";

        context.Response.StatusCode = success ? 200 :
            pairingResult == PhonePairingResult.RateLimited ? 429 : 401;
        if (pairingResult == PhonePairingResult.RateLimited)
        {
            context.Response.AddHeader("Retry-After", "60");
        }
        WriteJsonResponse(context, response);
    }

    private void HandleCommandRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "POST")
        {
            context.Response.StatusCode = 405;
            WriteJsonResponse(context, "{\"error\":\"method not allowed\"}");
            return;
        }

        if (!sessionGate.TryCaptureAuthorized(
                () => IsAuthorizationValid(context.Request.Headers["Authorization"]),
                out PhoneSessionStamp authorizationStamp))
        {
            context.Response.StatusCode = 401;
            WriteJsonResponse(context, "{\"error\":\"not authorized\"}");
            return;
        }

        if (!TryReadRequestBody(context, CommandRequestBodyLimit, out string body))
        {
            return;
        }
        PhoneInputCommand command = PhoneInputCommand.FromJson(body);

        if (command == null)
        {
            context.Response.StatusCode = 400;
            WriteJsonResponse(context, "{\"error\":\"invalid command\"}");
            return;
        }

        PhoneCommandEnqueueResult enqueueResult =
            sessionGate.TryEnqueue(authorizationStamp, command);
        if (enqueueResult == PhoneCommandEnqueueResult.StaleSession)
        {
            context.Response.StatusCode = 401;
            WriteJsonResponse(context, "{\"error\":\"server session changed\"}");
            return;
        }
        if (enqueueResult == PhoneCommandEnqueueResult.QueueFull)
        {
            context.Response.StatusCode = 429;
            context.Response.AddHeader("Retry-After", "1");
            WriteJsonResponse(context, "{\"error\":\"command queue full\"}");
            return;
        }

        WriteJsonResponse(context, "{\"ok\":true}");
    }

    private void HandleStateRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            context.Response.StatusCode = 405;
            WriteJsonResponse(context, "{\"error\":\"method not allowed\"}");
            return;
        }

        if (!sessionGate.TryCaptureAuthorized(
                () => IsAuthorizationValid(context.Request.Headers["Authorization"]),
                out PhoneSessionStamp authorizationStamp))
        {
            context.Response.StatusCode = 401;
            WriteJsonResponse(context, "{\"error\":\"not authorized\"}");
            return;
        }

        string stateJson;
        lock (stateLock)
        {
            stateJson = cachedGameStateJson;
        }
        if (!sessionGate.TryRunGeneration(
                authorizationStamp.Generation,
                () => WriteJsonResponse(context, stateJson)))
        {
            context.Response.StatusCode = 401;
            WriteJsonResponse(context, "{\"error\":\"server session changed\"}");
        }
    }

    private void HandleQrDataRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            context.Response.StatusCode = 405;
            WriteJsonResponse(context, "{\"error\":\"method not allowed\"}");
            return;
        }

        // The code is shown by the game UI. Do not disclose it to unpaired clients.
        string response = BuildQrDataResponse(serverUrl);
        WriteJsonResponse(context, response);
    }

    internal static bool IsAuthorizationValid(string authorization)
    {
        const string bearerPrefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorization)
            || !authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = authorization.Substring(bearerPrefix.Length).Trim();
        return PhoneControllerAuthService.ValidateToken(token);
    }

    internal static string BuildQrDataResponse(string url)
    {
        return $"{{\"url\":\"{url}\"}}";
    }

    internal static bool IsOriginAllowed(string origin, Uri requestUrl)
    {
        return PhoneWebRequestPolicy.IsOriginAllowed(origin, requestUrl);
    }

    private void ProcessPendingCommands()
    {
        Queue<PhoneQueuedCommand<PhoneInputCommand>> commandsToProcess =
            sessionGate.TakeBatch(MaxCommandsPerFrame);

        while (commandsToProcess.Count > 0)
        {
            PhoneQueuedCommand<PhoneInputCommand> queuedCommand = commandsToProcess.Dequeue();
            try
            {
                sessionGate.TryDispatch(
                    queuedCommand,
                    command => OnCommandReceived?.Invoke(command));
            }
            catch (Exception ex)
            {
                Log($"Command processing error: {ex.Message}");
            }
        }
    }

    private void QueueLog(string message)
    {
        lock (logLock)
        {
            if (pendingLogs.Count >= MaxPendingLogs)
            {
                pendingLogs.Dequeue();
            }
            pendingLogs.Enqueue(message);
        }
    }

    private void ProcessPendingLogs()
    {
        if (!showDebugLog)
        {
            lock (logLock)
            {
                pendingLogs.Clear();
            }
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            string message;
            lock (logLock)
            {
                if (pendingLogs.Count == 0)
                {
                    break;
                }
                message = pendingLogs.Dequeue();
            }
            Debug.Log($"[PhoneWebController] {message}");
        }
    }

    private void UpdateGameStateSnapshot()
    {
        try
        {
            string json = GameStateSerializer.BuildFullStateJson();
            lock (stateLock)
            {
                cachedGameStateJson = json;
            }
        }
        catch (Exception)
        {
            // Don't crash the game if state serialization fails
        }
    }

    private static bool TryReadRequestBody(HttpListenerContext context, int maxBytes, out string body)
    {
        body = null;
        try
        {
            body = ReadLimitedBody(
                context.Request.InputStream,
                context.Request.ContentEncoding,
                context.Request.ContentLength64,
                maxBytes,
                RequestBodyReadTimeoutMs);
            return true;
        }
        catch (RequestBodyTooLargeException)
        {
            context.Response.StatusCode = 413;
            WriteJsonResponse(context, "{\"error\":\"request body too large\"}");
            return false;
        }
        catch (TimeoutException)
        {
            context.Response.StatusCode = 408;
            WriteJsonResponse(context, "{\"error\":\"request body timed out\"}");
            return false;
        }
        catch (IOException)
        {
            context.Response.StatusCode = 400;
            WriteJsonResponse(context, "{\"error\":\"could not read request body\"}");
            return false;
        }
    }

    internal static string ReadLimitedBody(
        Stream input,
        Encoding encoding,
        long declaredLength,
        int maxBytes,
        int timeoutMs)
    {
        return PhoneWebRequestPolicy.ReadLimitedBody(
            input, encoding, declaredLength, maxBytes, timeoutMs);
    }

    private static void WriteJsonResponse(HttpListenerContext context, string json)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }

    private static string ExtractJsonString(string json, string key)
    {
        // Simple JSON string extraction without external dependencies
        string search = $"\"{key}\"";
        int keyIndex = json.IndexOf(search, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return null;
        }

        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0)
        {
            return null;
        }

        int quoteStart = json.IndexOf('"', colonIndex + 1);
        if (quoteStart < 0)
        {
            return null;
        }

        int quoteEnd = json.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0)
        {
            return null;
        }

        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log($"[PhoneWebController] {message}");
        }
    }

}

/// <summary>
/// Represents a command sent from the phone controller.
/// </summary>
public class PhoneInputCommand
{
    public string type; // "joystick", "button", "action"
    public string action; // For buttons: "interact", "dash", "hop", "sprint", etc.
    public float x; // Joystick X (-1 to 1)
    public float y; // Joystick Y (-1 to 1)
    public bool pressed; // Button pressed/released

    public static PhoneInputCommand FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            PhoneInputCommand command = new PhoneInputCommand();
            command.type = ExtractString(json, "type");
            command.action = ExtractString(json, "action");
            command.x = ExtractFloat(json, "x");
            command.y = ExtractFloat(json, "y");
            command.pressed = ExtractBool(json, "pressed");
            return command;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ExtractString(string json, string key)
    {
        string search = $"\"{key}\"";
        int keyIndex = json.IndexOf(search, StringComparison.Ordinal);
        if (keyIndex < 0) return null;
        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return null;
        int quoteStart = json.IndexOf('"', colonIndex + 1);
        if (quoteStart < 0) return null;
        int quoteEnd = json.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0) return null;
        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }

    private static float ExtractFloat(string json, string key)
    {
        string search = $"\"{key}\"";
        int keyIndex = json.IndexOf(search, StringComparison.Ordinal);
        if (keyIndex < 0) return 0f;
        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return 0f;

        // Find the end of the number
        int start = colonIndex + 1;
        while (start < json.Length && (char.IsWhiteSpace(json[start]) || json[start] == ','))
        {
            start++;
        }

        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-' || json[end] == 'e' || json[end] == 'E' || json[end] == '+'))
        {
            end++;
        }

        if (float.TryParse(json.Substring(start, end - start), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0f;
    }

    private static bool ExtractBool(string json, string key)
    {
        string search = $"\"{key}\"";
        int keyIndex = json.IndexOf(search, StringComparison.Ordinal);
        if (keyIndex < 0) return false;
        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return false;

        int start = colonIndex + 1;
        while (start < json.Length && char.IsWhiteSpace(json[start]))
        {
            start++;
        }

        return json.Substring(start).StartsWith("true", StringComparison.OrdinalIgnoreCase);
    }
}
