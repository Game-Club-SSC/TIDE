using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

public static class PhoneControllerAuthService
{
    private static readonly Dictionary<string, DateTime> ActiveTokens = new Dictionary<string, DateTime>();
    private static readonly object TokenLock = new object();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(10);
    private const string PersistedTokensKey = "TIDE_PHONE_CONTROLLER_TOKENS";

    public static string GenerateToken()
    {
        lock (TokenLock)
        {
            string token = RegisterNewToken();
            PersistTokens();
            return token;
        }
    }

    /// <summary>
    /// Generates a token for the current server session without calling PlayerPrefs.
    /// The phone server invokes this from its listener thread.
    /// </summary>
    public static string GenerateSessionToken()
    {
        lock (TokenLock)
        {
            return RegisterNewToken();
        }
    }

    public static bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        lock (TokenLock)
        {
            if (!ActiveTokens.TryGetValue(token, out DateTime expiresAt))
            {
                return false;
            }

            if (DateTime.UtcNow > expiresAt)
            {
                // Persisted loading already ignores expired entries. Do not call
                // PlayerPrefs here because validation may run on the HTTP thread.
                ActiveTokens.Remove(token);
                return false;
            }

            return true;
        }
    }

    public static bool RefreshToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        lock (TokenLock)
        {
            if (!ActiveTokens.TryGetValue(token, out DateTime expiresAt))
            {
                return false;
            }

            if (DateTime.UtcNow > expiresAt)
            {
                ActiveTokens.Remove(token);
                PersistTokens();
                return false;
            }

            TimeSpan remaining = expiresAt - DateTime.UtcNow;
            if (remaining > RefreshWindow)
            {
                return true;
            }

            ActiveTokens[token] = DateTime.UtcNow + TokenLifetime;
            PersistTokens();
            return true;
        }
    }

    public static bool RegisterToken(string token, TimeSpan lifetime)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        lock (TokenLock)
        {
            ActiveTokens[token] = DateTime.UtcNow + lifetime;
            PersistTokens();
            return true;
        }
    }

    public static int GetActiveTokenCount()
    {
        lock (TokenLock)
        {
            int count = 0;
            DateTime now = DateTime.UtcNow;
            List<string> expired = new List<string>();
            foreach (KeyValuePair<string, DateTime> kvp in ActiveTokens)
            {
                if (now > kvp.Value) expired.Add(kvp.Key);
                else count++;
            }
            for (int i = 0; i < expired.Count; i++)
            {
                ActiveTokens.Remove(expired[i]);
            }
            if (expired.Count > 0)
            {
                PersistTokens();
            }
            return count;
        }
    }

    public static void RevokeAllTokens()
    {
        lock (TokenLock)
        {
            ActiveTokens.Clear();
            PersistTokens();
        }
    }

    public static void LoadPersistedTokens()
    {
        lock (TokenLock)
        {
            string raw = PlayerPrefs.GetString(PersistedTokensKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] entries = raw.Split(';');
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.IsNullOrEmpty(entries[i]))
                {
                    continue;
                }

                string[] parts = entries[i].Split(',');
                if (parts.Length != 2)
                {
                    continue;
                }

                string token = parts[0];
                if (!long.TryParse(parts[1], out long ticks))
                {
                    continue;
                }

                DateTime expiry = new DateTime(ticks, DateTimeKind.Utc);
                if (expiry > now)
                {
                    ActiveTokens[token] = expiry;
                }
            }
        }
    }

    private static string RegisterNewToken()
    {
        byte[] bytes = new byte[32];
        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(bytes);
        }

        StringBuilder tokenBuilder = new StringBuilder(bytes.Length * 2);
        for (int i = 0; i < bytes.Length; i++)
        {
            tokenBuilder.Append(bytes[i].ToString("x2"));
        }

        string token = tokenBuilder.ToString();
        ActiveTokens[token] = DateTime.UtcNow + TokenLifetime;
        return token;
    }

    private static void PersistTokens()
    {
        DateTime now = DateTime.UtcNow;
        List<string> entries = new List<string>();
        foreach (KeyValuePair<string, DateTime> kvp in ActiveTokens)
        {
            if (kvp.Value > now)
            {
                entries.Add(kvp.Key + "," + kvp.Value.Ticks);
            }
        }

        PlayerPrefs.SetString(PersistedTokensKey, string.Join(";", entries));
        PlayerPrefs.Save();
    }

    public static bool LogicOk()
    {
        RevokeAllTokens();
        string token = GenerateToken();
        bool ok = ValidateToken(token) && GetActiveTokenCount() == 1;
        RevokeAllTokens();
        return ok;
    }
}

internal readonly struct PhoneSessionStamp
{
    internal PhoneSessionStamp(long generation)
    {
        Generation = generation;
    }

    internal long Generation { get; }
}

internal readonly struct PhoneSessionAttempt<TState>
{
    internal PhoneSessionAttempt(PhoneSessionStamp stamp, TState state)
    {
        Stamp = stamp;
        State = state;
    }

    internal PhoneSessionStamp Stamp { get; }
    internal TState State { get; }
}

internal readonly struct PhoneQueuedCommand<TCommand>
{
    internal PhoneQueuedCommand(PhoneSessionStamp stamp, TCommand command)
    {
        Stamp = stamp;
        Command = command;
    }

    internal PhoneSessionStamp Stamp { get; }
    internal TCommand Command { get; }
}

internal enum PhoneCommandEnqueueResult
{
    Enqueued,
    QueueFull,
    StaleSession
}

/// <summary>
/// Gives each server session a monotonic generation and keeps authorization,
/// queueing, dispatch, and revocation on one lock. A request may do slow work
/// outside the lock, but it cannot commit work after its generation changes.
/// </summary>
internal sealed class PhoneServerSessionGate<TCommand>
{
    private readonly object sync = new object();
    private readonly Queue<PhoneQueuedCommand<TCommand>> pendingCommands =
        new Queue<PhoneQueuedCommand<TCommand>>();
    private readonly int maxPendingCommands;

    private long generation;
    private bool isRunning;

    internal PhoneServerSessionGate(int maxPendingCommands)
    {
        if (maxPendingCommands <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPendingCommands));
        }
        this.maxPendingCommands = maxPendingCommands;
    }

    internal long CurrentGeneration
    {
        get
        {
            lock (sync)
            {
                return generation;
            }
        }
    }

    internal int PendingCount
    {
        get
        {
            lock (sync)
            {
                return pendingCommands.Count;
            }
        }
    }

    internal PhoneSessionStamp Advance(bool running, Action invalidate)
    {
        return AdvanceCore(running, replaceRunningState: true, invalidate);
    }

    internal PhoneSessionStamp AdvanceKeepingRunState(Action invalidate)
    {
        return AdvanceCore(false, replaceRunningState: false, invalidate);
    }

    private PhoneSessionStamp AdvanceCore(
        bool running,
        bool replaceRunningState,
        Action invalidate)
    {
        lock (sync)
        {
            generation = checked(generation + 1);
            if (replaceRunningState)
            {
                isRunning = running;
            }
            pendingCommands.Clear();
            invalidate?.Invoke();
            return new PhoneSessionStamp(generation);
        }
    }

    internal bool TryCapture<TState>(
        Func<TState> captureState,
        Predicate<TState> isValid,
        out PhoneSessionAttempt<TState> attempt)
    {
        if (captureState == null) throw new ArgumentNullException(nameof(captureState));
        if (isValid == null) throw new ArgumentNullException(nameof(isValid));

        lock (sync)
        {
            if (!isRunning)
            {
                attempt = default;
                return false;
            }

            TState state = captureState();
            if (!isValid(state))
            {
                attempt = default;
                return false;
            }

            attempt = new PhoneSessionAttempt<TState>(
                new PhoneSessionStamp(generation), state);
            return true;
        }
    }

    internal bool TryCaptureAuthorized(
        Func<bool> authorize,
        out PhoneSessionStamp stamp)
    {
        if (authorize == null) throw new ArgumentNullException(nameof(authorize));

        lock (sync)
        {
            if (!isRunning || !authorize())
            {
                stamp = default;
                return false;
            }

            stamp = new PhoneSessionStamp(generation);
            return true;
        }
    }

    internal bool TryComplete<TState, TResult>(
        PhoneSessionAttempt<TState> attempt,
        Predicate<TState> stateIsCurrent,
        Func<TResult> commit,
        out TResult result)
    {
        if (stateIsCurrent == null) throw new ArgumentNullException(nameof(stateIsCurrent));
        if (commit == null) throw new ArgumentNullException(nameof(commit));

        lock (sync)
        {
            if (!IsCurrent(attempt.Stamp) || !stateIsCurrent(attempt.State))
            {
                result = default;
                return false;
            }

            result = commit();
            return true;
        }
    }

    internal PhoneCommandEnqueueResult TryEnqueue(
        PhoneSessionStamp stamp,
        TCommand command)
    {
        lock (sync)
        {
            if (!IsCurrent(stamp))
            {
                return PhoneCommandEnqueueResult.StaleSession;
            }
            if (pendingCommands.Count >= maxPendingCommands)
            {
                return PhoneCommandEnqueueResult.QueueFull;
            }

            pendingCommands.Enqueue(new PhoneQueuedCommand<TCommand>(stamp, command));
            return PhoneCommandEnqueueResult.Enqueued;
        }
    }

    internal Queue<PhoneQueuedCommand<TCommand>> TakeBatch(int maximum)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));

        lock (sync)
        {
            int count = Math.Min(maximum, pendingCommands.Count);
            Queue<PhoneQueuedCommand<TCommand>> batch =
                new Queue<PhoneQueuedCommand<TCommand>>(count);
            for (int i = 0; i < count; i++)
            {
                batch.Enqueue(pendingCommands.Dequeue());
            }
            return batch;
        }
    }

    internal bool TryDispatch(
        PhoneQueuedCommand<TCommand> queuedCommand,
        Action<TCommand> dispatch)
    {
        if (dispatch == null) throw new ArgumentNullException(nameof(dispatch));

        lock (sync)
        {
            if (!IsCurrent(queuedCommand.Stamp))
            {
                return false;
            }

            dispatch(queuedCommand.Command);
            return true;
        }
    }

    internal bool TryRunGeneration(long expectedGeneration, Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        lock (sync)
        {
            if (!isRunning || generation != expectedGeneration)
            {
                return false;
            }

            action();
            return true;
        }
    }

    private bool IsCurrent(PhoneSessionStamp stamp)
    {
        return isRunning && stamp.Generation == generation;
    }
}

internal enum PhonePairingResult
{
    Success,
    InvalidCode,
    ExpiredCode,
    RateLimited
}

/// <summary>
/// Owns the short-lived pairing code and failed-attempt limits. This class has
/// no Unity API calls, so the HTTP workers can use it safely.
/// </summary>
internal sealed class PhonePairingGuard
{
    private const int MaxTrackedPeers = 128;
    private const string OverflowPeer = "__other_peers__";

    private readonly object sync = new object();
    private readonly int codeLength;
    private readonly TimeSpan codeLifetime;
    private readonly TimeSpan attemptWindow;
    private readonly int maxAttemptsPerPeer;
    private readonly int maxAttemptsGlobal;
    private readonly Queue<DateTime> globalFailures = new Queue<DateTime>();
    private readonly Dictionary<string, Queue<DateTime>> peerFailures =
        new Dictionary<string, Queue<DateTime>>(StringComparer.Ordinal);

    private string code;
    private DateTime expiresAtUtc;

    internal PhonePairingGuard(
        int codeLength,
        TimeSpan codeLifetime,
        TimeSpan attemptWindow,
        int maxAttemptsPerPeer,
        int maxAttemptsGlobal,
        DateTime nowUtc)
    {
        this.codeLength = Math.Max(6, Math.Min(8, codeLength));
        this.codeLifetime = codeLifetime;
        this.attemptWindow = attemptWindow;
        this.maxAttemptsPerPeer = maxAttemptsPerPeer;
        this.maxAttemptsGlobal = maxAttemptsGlobal;
        RotateCore(nowUtc);
    }

    internal string GetCurrentCode(DateTime nowUtc)
    {
        lock (sync)
        {
            if (nowUtc >= expiresAtUtc)
            {
                RotateCore(nowUtc);
            }
            return code;
        }
    }

    internal void Rotate(DateTime nowUtc)
    {
        lock (sync)
        {
            RotateCore(nowUtc);
        }
    }

    internal PhonePairingResult TryPair(string peer, string submittedCode, DateTime nowUtc)
    {
        lock (sync)
        {
            PurgeFailures(nowUtc);

            if (nowUtc >= expiresAtUtc)
            {
                RotateCore(nowUtc);
                return PhonePairingResult.ExpiredCode;
            }

            string peerKey = NormalizePeer(peer);
            Queue<DateTime> failures = GetPeerFailures(peerKey);
            if (globalFailures.Count >= maxAttemptsGlobal || failures.Count >= maxAttemptsPerPeer)
            {
                return PhonePairingResult.RateLimited;
            }

            if (CodesMatch(code, submittedCode))
            {
                RotateCore(nowUtc);
                return PhonePairingResult.Success;
            }

            globalFailures.Enqueue(nowUtc);
            failures.Enqueue(nowUtc);
            if (globalFailures.Count >= maxAttemptsGlobal || failures.Count >= maxAttemptsPerPeer)
            {
                RotateCore(nowUtc);
                return PhonePairingResult.RateLimited;
            }

            return PhonePairingResult.InvalidCode;
        }
    }

    private Queue<DateTime> GetPeerFailures(string peer)
    {
        if (!peerFailures.TryGetValue(peer, out Queue<DateTime> failures))
        {
            if (peerFailures.Count >= MaxTrackedPeers)
            {
                peer = OverflowPeer;
            }
            if (!peerFailures.TryGetValue(peer, out failures))
            {
                failures = new Queue<DateTime>();
                peerFailures[peer] = failures;
            }
        }
        return failures;
    }

    private void PurgeFailures(DateTime nowUtc)
    {
        DateTime cutoff = nowUtc - attemptWindow;
        while (globalFailures.Count > 0 && globalFailures.Peek() <= cutoff)
        {
            globalFailures.Dequeue();
        }

        List<string> emptyPeers = null;
        foreach (KeyValuePair<string, Queue<DateTime>> entry in peerFailures)
        {
            while (entry.Value.Count > 0 && entry.Value.Peek() <= cutoff)
            {
                entry.Value.Dequeue();
            }
            if (entry.Value.Count == 0)
            {
                if (emptyPeers == null) emptyPeers = new List<string>();
                emptyPeers.Add(entry.Key);
            }
        }

        if (emptyPeers != null)
        {
            for (int i = 0; i < emptyPeers.Count; i++)
            {
                peerFailures.Remove(emptyPeers[i]);
            }
        }
    }

    private void RotateCore(DateTime nowUtc)
    {
        string previousCode = code;
        do
        {
            code = GenerateNumericCode(codeLength);
        }
        while (code == previousCode);
        expiresAtUtc = nowUtc + codeLifetime;
    }

    private static string GenerateNumericCode(int length)
    {
        StringBuilder builder = new StringBuilder(length);
        byte[] randomByte = new byte[1];
        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            while (builder.Length < length)
            {
                generator.GetBytes(randomByte);
                // 250 is the largest multiple of ten below 256. Rejecting the
                // remaining values prevents biased digits.
                if (randomByte[0] < 250)
                {
                    builder.Append((char)('0' + (randomByte[0] % 10)));
                }
            }
        }
        return builder.ToString();
    }

    private static bool CodesMatch(string expected, string submitted)
    {
        if (expected == null || submitted == null || expected.Length != submitted.Length)
        {
            return false;
        }

        int difference = 0;
        for (int i = 0; i < expected.Length; i++)
        {
            difference |= expected[i] ^ submitted[i];
        }
        return difference == 0;
    }

    private static string NormalizePeer(string peer)
    {
        return string.IsNullOrWhiteSpace(peer) ? "unknown" : peer;
    }
}

internal static class PhoneWebRequestPolicy
{
    internal static bool IsOriginAllowed(string origin, Uri requestUrl)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        if (requestUrl == null ||
            !Uri.TryCreate(origin, UriKind.Absolute, out Uri originUri) ||
            originUri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(originUri.Query) ||
            !string.IsNullOrEmpty(originUri.Fragment))
        {
            return false;
        }

        return string.Equals(originUri.Scheme, requestUrl.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(originUri.Host, requestUrl.Host, StringComparison.OrdinalIgnoreCase) &&
            originUri.Port == requestUrl.Port;
    }

    internal static string ReadLimitedBody(
        Stream input,
        Encoding encoding,
        long declaredLength,
        int maxBytes,
        int timeoutMs)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (declaredLength > maxBytes) throw new RequestBodyTooLargeException();

        if (input.CanTimeout)
        {
            input.ReadTimeout = timeoutMs;
        }

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        byte[] chunk = new byte[Math.Min(512, maxBytes + 1)];
        using (MemoryStream buffer = new MemoryStream())
        {
            while (true)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException();
                }

                int remainingMs = (int)Math.Max(1, (deadline - DateTime.UtcNow).TotalMilliseconds);
                IAsyncResult pendingRead = input.BeginRead(chunk, 0, chunk.Length, null, null);
                WaitHandle waitHandle = pendingRead.AsyncWaitHandle;
                int bytesRead;
                try
                {
                    if (!waitHandle.WaitOne(remainingMs))
                    {
                        throw new TimeoutException();
                    }
                    bytesRead = input.EndRead(pendingRead);
                    if (DateTime.UtcNow >= deadline)
                    {
                        throw new TimeoutException();
                    }
                }
                finally
                {
                    waitHandle.Close();
                }
                if (bytesRead == 0)
                {
                    break;
                }
                if (buffer.Length + bytesRead > maxBytes)
                {
                    throw new RequestBodyTooLargeException();
                }
                buffer.Write(chunk, 0, bytesRead);
            }

            return (encoding ?? Encoding.UTF8).GetString(buffer.ToArray());
        }
    }
}

internal sealed class RequestBodyTooLargeException : IOException
{
}
