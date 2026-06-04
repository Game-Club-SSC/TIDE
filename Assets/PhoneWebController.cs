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

    [Header("Pairing")]
    [SerializeField] private int pairingCodeLength = 6;

    private HttpListener httpListener;
    private Thread listenerThread;
    private bool isRunning;
    private string pairingCode;
    private string serverUrl;
    private string cachedHtmlPage;

    // Thread-safe command queue
    private readonly Queue<PhoneInputCommand> pendingCommands = new Queue<PhoneInputCommand>();
    private readonly object commandLock = new object();

    // Game state snapshot (updated from main thread, read from HTTP thread)
    private string cachedGameStateJson = "{}";
    private readonly object stateLock = new object();

    public static PhoneWebController Instance { get; private set; }
    public bool IsRunning => isRunning;
    public string ServerUrl => serverUrl;
    public string PairingCode => pairingCode;
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
        StopServer();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // Process pending commands on the main Unity thread
        ProcessPendingCommands();
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
            httpListener = new HttpListener();

            // Try to listen on all interfaces first
            string prefix = $"http://*:{port}/";
            httpListener.Prefixes.Add(prefix);

            try
            {
                httpListener.Start();
            }
            catch (HttpListenerException)
            {
                // Wildcard binding failed (needs admin / URL reservation).
                // Fall back to localhost + specific IP.
                httpListener.Prefixes.Clear();
                httpListener.Prefixes.Add($"http://localhost:{port}/");
                string localIP = GetLocalIPAddress();
                if (localIP != "127.0.0.1")
                {
                    httpListener.Prefixes.Add($"http://{localIP}:{port}/");
                }
                httpListener.Start();
                // Rebuild URL to match what we're actually listening on
                serverUrl = $"http://{localIP}:{port}";
                Log("Note: Wildcard binding failed. Using specific IP. Run as admin for network access.");
            }

            isRunning = true;
            GeneratePairingCode();
            BuildServerUrl();

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
            Log($"Pairing Code: {pairingCode}");
            Log("NOTE: If connection fails, you may need to run this command as Administrator:");
            Log($"  netsh http add urlacl url=http://*:{port}/ user=Everyone");
        }
        catch (Exception ex)
        {
            Log($"Failed to start server: {ex.Message}");
            isRunning = false;
        }
    }

    public void StopServer()
    {
        isRunning = false;

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
        GeneratePairingCode();
        Log($"New pairing code: {pairingCode}");
    }

    private void GeneratePairingCode()
    {
        // Generate a random numeric code
        System.Random rng = new System.Random();
        StringBuilder sb = new StringBuilder(pairingCodeLength);
        for (int i = 0; i < pairingCodeLength; i++)
        {
            sb.Append(rng.Next(0, 10));
        }
        pairingCode = sb.ToString();
    }

    private void BuildServerUrl()
    {
        try
        {
            // Get the local IP address
            string localIP = GetLocalIPAddress();
            serverUrl = $"http://{localIP}:{port}";
        }
        catch (Exception)
        {
            serverUrl = $"http://localhost:{port}";
        }
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

                IAsyncResult result = httpListener.BeginGetContext(OnContext, null);
                result.AsyncWaitHandle.WaitOne(500);
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
                    Log($"Listener error: {ex.Message}");
                }
                break;
            }
        }
    }

    private void OnContext(IAsyncResult result)
    {
        if (!isRunning || httpListener == null)
        {
            return;
        }

        HttpListenerContext context;
        try
        {
            context = httpListener.EndGetContext(result);
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            HandleRequest(context);
        }
        catch (Exception ex)
        {
            Log($"Request handling error: {ex.Message}");
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
    }

    private void HandleRequest(HttpListenerContext context)
    {
        string path = context.Request.Url.AbsolutePath.ToLowerInvariant();
        string method = context.Request.HttpMethod.ToUpperInvariant();

        // CORS headers for all responses
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

        if (method == "OPTIONS")
        {
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

        string body = ReadRequestBody(context);
        // Simple JSON parsing for {"code":"123456"}
        string submittedCode = ExtractJsonString(body, "code");

        if (string.IsNullOrEmpty(submittedCode))
        {
            context.Response.StatusCode = 400;
            WriteJsonResponse(context, "{\"error\":\"missing code\"}");
            return;
        }

        bool success = submittedCode == pairingCode;
        string response = $"{{\"success\":{success.ToString().ToLowerInvariant()},\"message\":\"{(success ? "Paired successfully!" : "Invalid code")}\"}}";

        if (success)
        {
            // Notify the input bridge that we're paired
            PhoneInputBridge bridge = PhoneInputBridge.Instance;
            if (bridge != null)
            {
                // This is called from the HTTP thread, but SetPaired just sets a bool
                // which is safe to do from any thread
                bridge.SetPaired(true);
            }
            Log("Phone paired successfully!");
        }

        context.Response.StatusCode = success ? 200 : 401;
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

        string body = ReadRequestBody(context);
        PhoneInputCommand command = PhoneInputCommand.FromJson(body);

        if (command == null)
        {
            context.Response.StatusCode = 400;
            WriteJsonResponse(context, "{\"error\":\"invalid command\"}");
            return;
        }

        lock (commandLock)
        {
            pendingCommands.Enqueue(command);
        }

        WriteJsonResponse(context, "{\"ok\":true}");
    }

    private void HandleStateRequest(HttpListenerContext context)
    {
        string stateJson;
        lock (stateLock)
        {
            stateJson = cachedGameStateJson;
        }
        WriteJsonResponse(context, stateJson);
    }

    private void HandleQrDataRequest(HttpListenerContext context)
    {
        // Return the server URL as JSON for QR code generation
        string response = $"{{\"url\":\"{serverUrl}\",\"code\":\"{pairingCode}\"}}";
        WriteJsonResponse(context, response);
    }

    private void ProcessPendingCommands()
    {
        Queue<PhoneInputCommand> commandsToProcess;

        lock (commandLock)
        {
            if (pendingCommands.Count == 0)
            {
                return;
            }
            commandsToProcess = new Queue<PhoneInputCommand>(pendingCommands);
            pendingCommands.Clear();
        }

        while (commandsToProcess.Count > 0)
        {
            PhoneInputCommand command = commandsToProcess.Dequeue();
            try
            {
                OnCommandReceived?.Invoke(command);
            }
            catch (Exception ex)
            {
                Log($"Command processing error: {ex.Message}");
            }
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

    private static string ReadRequestBody(HttpListenerContext context)
    {
        using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            return reader.ReadToEnd();
        }
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
