using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

internal static class Program
{
    private static int checks;

    private static int Main()
    {
        try
        {
            TestTokensAndAuthorization();
            TestPairingAndRateLimits();
            TestOriginPolicy();
            TestBodyLimitsAndDeadline();
            TestSessionGenerationRaces();
            Console.WriteLine($"Phone security harness passed {checks} checks.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    private static void TestTokensAndAuthorization()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        string token = PhoneControllerAuthService.GenerateSessionToken();
        Check(token.Length == 64, "Session tokens must contain 256 bits encoded as hex.");
        Check(PhoneControllerAuthService.ValidateToken(token), "A fresh session token must validate.");
        Check(PhoneWebController.IsAuthorizationValid("Bearer " + token),
            "A valid bearer token must authorize.");
        Check(!PhoneWebController.IsAuthorizationValid(null),
            "A missing bearer token must not authorize.");
        Check(!PhoneWebController.IsAuthorizationValid("Bearer wrong-token"),
            "An unknown bearer token must not authorize.");

        string persistedToken = PhoneControllerAuthService.GenerateToken();
        Check(PhoneControllerAuthService.ValidateToken(persistedToken),
            "A persisted token must validate in the current session.");
        Check(PhoneControllerAuthService.RegisterToken(persistedToken, TimeSpan.FromHours(2)),
            "Refreshing a known token must succeed.");
        Check(PhoneControllerAuthService.GetActiveTokenCount() == 2,
            "Refreshing must not duplicate a token.");

        string qrResponse = PhoneWebController.BuildQrDataResponse("http://127.0.0.1:8080");
        Check(!qrResponse.Contains("code", StringComparison.OrdinalIgnoreCase),
            "QR data must not disclose a pairing code.");
        string page = PhoneWebPageBuilder.BuildHtmlPage();
        Check(page.Contains("data.token", StringComparison.Ordinal) &&
            page.Contains("Authorization", StringComparison.Ordinal),
            "The controller page must retain and send its bearer token.");
    }

    private static void TestPairingAndRateLimits()
    {
        DateTime now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        PhonePairingGuard guard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        string firstCode = guard.GetCurrentCode(now);
        Check(firstCode.Length == 6 && long.TryParse(firstCode, out _),
            "Pairing codes must contain six digits.");
        string wrongCode = DifferentCode(firstCode);
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Check(guard.TryPair("192.0.2.10", wrongCode, now.AddSeconds(attempt)) ==
                PhonePairingResult.InvalidCode,
                "Early wrong guesses must fail without a global lockout.");
        }
        Check(guard.TryPair("192.0.2.10", wrongCode, now.AddSeconds(4)) ==
            PhonePairingResult.RateLimited,
            "The fifth wrong guess from one peer must be rate limited.");
        string afterAbuse = guard.GetCurrentCode(now.AddSeconds(4));
        Check(afterAbuse != firstCode, "Abuse must rotate the pairing code.");

        DateTime afterWindow = now.AddMinutes(2);
        string validCode = guard.GetCurrentCode(afterWindow);
        Check(guard.TryPair("192.0.2.11", validCode, afterWindow) == PhonePairingResult.Success,
            "A peer must pair with the current code after the attempt window closes.");
        Check(guard.GetCurrentCode(afterWindow) != validCode,
            "A successful pairing must rotate the used code.");

        PhonePairingGuard expiringGuard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        string expiredCode = expiringGuard.GetCurrentCode(now);
        Check(expiringGuard.TryPair("192.0.2.12", expiredCode, now.AddMinutes(6)) ==
            PhonePairingResult.ExpiredCode,
            "An expired code must not pair.");

        PhonePairingGuard globalGuard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 4, now);
        string globalWrongCode = DifferentCode(globalGuard.GetCurrentCode(now));
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Check(globalGuard.TryPair("192.0.2." + (20 + attempt), globalWrongCode, now) ==
                PhonePairingResult.InvalidCode,
                "Distributed guesses below the global cap must fail normally.");
        }
        Check(globalGuard.TryPair("192.0.2.23", globalWrongCode, now) ==
            PhonePairingResult.RateLimited,
            "The global cap must stop guesses spread across peers.");
    }

    private static void TestOriginPolicy()
    {
        Uri requestUrl = new Uri("http://192.0.2.10:8080/api/pair");
        Check(PhoneWebController.IsOriginAllowed(null, requestUrl),
            "Native clients without an Origin header must remain allowed.");
        Check(PhoneWebController.IsOriginAllowed("http://192.0.2.10:8080", requestUrl),
            "The page served by this server must remain allowed.");
        Check(!PhoneWebController.IsOriginAllowed("https://evil.example", requestUrl),
            "An unrelated origin must be denied.");
        Check(!PhoneWebController.IsOriginAllowed("http://192.0.2.10:8081", requestUrl),
            "A different port must be denied.");
        Check(!PhoneWebController.IsOriginAllowed("http://192.0.2.10:8080/path", requestUrl),
            "An origin value with a path must be denied.");
    }

    private static void TestBodyLimitsAndDeadline()
    {
        byte[] exactBody = Encoding.UTF8.GetBytes("1234");
        using (MemoryStream stream = new MemoryStream(exactBody))
        {
            Check(PhoneWebController.ReadLimitedBody(
                stream, Encoding.UTF8, exactBody.Length, 4, 1000) == "1234",
                "A body at the byte cap must be accepted.");
        }

        Expect<RequestBodyTooLargeException>(() =>
        {
            using MemoryStream stream = new MemoryStream(exactBody);
            PhoneWebController.ReadLimitedBody(stream, Encoding.UTF8, 5, 4, 1000);
        }, "A declared body over the cap must be rejected.");

        Expect<RequestBodyTooLargeException>(() =>
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("12345"));
            PhoneWebController.ReadLimitedBody(stream, Encoding.UTF8, -1, 4, 1000);
        }, "A streamed body over the cap must be rejected.");

        SlowReadStream slowStream = new SlowReadStream(Encoding.UTF8.GetBytes("1"), 50);
        try
        {
            Expect<TimeoutException>(() =>
                PhoneWebController.ReadLimitedBody(slowStream, Encoding.UTF8, 1, 4, 5),
                "A slow request body must hit the read deadline.");
        }
        finally
        {
            Thread.Sleep(60);
            slowStream.Dispose();
        }
    }

    private static void TestSessionGenerationRaces()
    {
        TestPairVersusRegenerate();
        TestCommandBodyReadVersusRegenerate();
        TestQueuedCommandVersusStop();
        TestNormalConcurrentCommands();
    }

    private static void TestPairVersusRegenerate()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        PhoneServerSessionGate<int> gate = new PhoneServerSessionGate<int>(8);
        DateTime now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        PhonePairingGuard guard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        Check(gate.TryCapture(
            () => guard,
            value => value != null,
            out PhoneSessionAttempt<PhonePairingGuard> attempt),
            "A running session must allow a pair attempt to start.");

        using Barrier paired = new Barrier(2);
        using Barrier resume = new Barrier(2);
        Exception workerError = null;
        bool completed = false;
        string issuedToken = null;
        PhonePairingResult pairingResult = PhonePairingResult.InvalidCode;
        Thread worker = new Thread(() =>
        {
            try
            {
                pairingResult = guard.TryPair(
                    "192.0.2.10", guard.GetCurrentCode(now), now);
                ReachBarrier(paired);
                ReachBarrier(resume);
                completed = gate.TryComplete(
                    attempt,
                    value => ReferenceEquals(value, guard),
                    PhoneControllerAuthService.GenerateSessionToken,
                    out issuedToken);
            }
            catch (Exception error)
            {
                workerError = error;
            }
        });
        worker.Start();

        ReachBarrier(paired);
        gate.AdvanceKeepingRunState(() =>
        {
            PhoneControllerAuthService.RevokeAllTokens();
            guard.Rotate(now.AddSeconds(1));
        });
        ReachBarrier(resume);

        Check(worker.Join(2000), "Pair race worker did not finish.");
        Check(workerError == null, workerError?.ToString());
        Check(pairingResult == PhonePairingResult.Success,
            "The pair must succeed before regeneration reaches its commit point.");
        Check(!completed, "A pre-regeneration pair must not mint a token.");
        Check(issuedToken == null, "A stale pair must return no token.");
        Check(PhoneControllerAuthService.GetActiveTokenCount() == 0,
            "Regeneration must leave no token from a stale pair.");
    }

    private static void TestCommandBodyReadVersusRegenerate()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        PhoneServerSessionGate<int> gate = new PhoneServerSessionGate<int>(8);
        string token = null;
        gate.Advance(true, () =>
        {
            PhoneControllerAuthService.RevokeAllTokens();
            token = PhoneControllerAuthService.GenerateSessionToken();
        });
        Check(gate.TryCaptureAuthorized(() => true, out PhoneSessionStamp queuedStamp),
            "A current command must capture the running session.");
        Check(gate.TryEnqueue(queuedStamp, 99) == PhoneCommandEnqueueResult.Enqueued,
            "A current command must enter the queue before regeneration.");
        long generationBeforeRegenerate = gate.CurrentGeneration;

        using Barrier authorized = new Barrier(2);
        using Barrier bodyRead = new Barrier(2);
        Exception workerError = null;
        bool captured = false;
        PhoneCommandEnqueueResult enqueueResult = PhoneCommandEnqueueResult.Enqueued;
        Thread worker = new Thread(() =>
        {
            try
            {
                captured = gate.TryCaptureAuthorized(
                    () => PhoneControllerAuthService.ValidateToken(token),
                    out PhoneSessionStamp stamp);
                ReachBarrier(authorized);
                ReachBarrier(bodyRead);
                enqueueResult = gate.TryEnqueue(stamp, 1);
            }
            catch (Exception error)
            {
                workerError = error;
            }
        });
        worker.Start();

        ReachBarrier(authorized);
        gate.AdvanceKeepingRunState(PhoneControllerAuthService.RevokeAllTokens);
        Check(gate.CurrentGeneration > generationBeforeRegenerate,
            "Regeneration must advance the session generation.");
        Check(gate.PendingCount == 0,
            "Regeneration must clear commands from the prior session.");
        ReachBarrier(bodyRead);

        Check(worker.Join(2000), "Command race worker did not finish.");
        Check(workerError == null, workerError?.ToString());
        Check(captured, "The command must authorize before its body read.");
        Check(enqueueResult == PhoneCommandEnqueueResult.StaleSession,
            "A command body read across regeneration must not enqueue.");
        Check(gate.PendingCount == 0, "A stale command must leave the queue empty.");
    }

    private static void TestQueuedCommandVersusStop()
    {
        PhoneServerSessionGate<int> gate = new PhoneServerSessionGate<int>(8);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        Check(gate.TryCaptureAuthorized(() => true, out PhoneSessionStamp stamp),
            "A normal command must capture the running session.");
        Check(gate.TryEnqueue(stamp, 7) == PhoneCommandEnqueueResult.Enqueued,
            "A normal command must enter the queue.");
        Check(gate.TryEnqueue(stamp, 8) == PhoneCommandEnqueueResult.Enqueued,
            "A second normal command must enter the queue.");
        Queue<PhoneQueuedCommand<int>> batch = gate.TakeBatch(1);
        PhoneQueuedCommand<int> queued = batch.Dequeue();
        Check(gate.PendingCount == 1,
            "One queued command must remain for the stop cleanup check.");
        long generationBeforeStop = gate.CurrentGeneration;

        using Barrier dequeued = new Barrier(2);
        using Barrier stopped = new Barrier(2);
        Exception workerError = null;
        bool dispatched = false;
        int dispatchCount = 0;
        Thread worker = new Thread(() =>
        {
            try
            {
                ReachBarrier(dequeued);
                ReachBarrier(stopped);
                dispatched = gate.TryDispatch(queued, _ => dispatchCount++);
            }
            catch (Exception error)
            {
                workerError = error;
            }
        });
        worker.Start();

        ReachBarrier(dequeued);
        gate.Advance(false, PhoneControllerAuthService.RevokeAllTokens);
        Check(gate.CurrentGeneration > generationBeforeStop,
            "Stop must advance the session generation.");
        Check(gate.PendingCount == 0,
            "Stop must clear commands still held by the prior session.");
        ReachBarrier(stopped);

        Check(worker.Join(2000), "Dispatch race worker did not finish.");
        Check(workerError == null, workerError?.ToString());
        Check(!dispatched, "A dequeued command must become stale after stop.");
        Check(dispatchCount == 0, "A stale dequeued command must not reach the game.");
    }

    private static void TestNormalConcurrentCommands()
    {
        const int CommandCount = 24;
        PhoneServerSessionGate<int> gate = new PhoneServerSessionGate<int>(64);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        using Barrier start = new Barrier(CommandCount + 1);
        Thread[] threads = new Thread[CommandCount];
        Exception[] errors = new Exception[CommandCount];
        PhoneCommandEnqueueResult[] results = new PhoneCommandEnqueueResult[CommandCount];

        for (int i = 0; i < CommandCount; i++)
        {
            int command = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    ReachBarrier(start);
                    if (!gate.TryCaptureAuthorized(() => true, out PhoneSessionStamp stamp))
                    {
                        throw new InvalidOperationException("Normal command was not authorized.");
                    }
                    results[command] = gate.TryEnqueue(stamp, command);
                }
                catch (Exception error)
                {
                    errors[command] = error;
                }
            });
            threads[i].Start();
        }

        ReachBarrier(start);
        for (int i = 0; i < CommandCount; i++)
        {
            Check(threads[i].Join(2000), "Normal command worker did not finish.");
            Check(errors[i] == null, errors[i]?.ToString());
            Check(results[i] == PhoneCommandEnqueueResult.Enqueued,
                "A normal concurrent command must enqueue.");
        }

        Queue<PhoneQueuedCommand<int>> batch = gate.TakeBatch(CommandCount);
        HashSet<int> dispatched = new HashSet<int>();
        while (batch.Count > 0)
        {
            Check(gate.TryDispatch(batch.Dequeue(), value => dispatched.Add(value)),
                "A current queued command must dispatch.");
        }
        Check(dispatched.Count == CommandCount,
            "Every normal concurrent command must dispatch once.");
        Check(gate.PendingCount == 0, "Normal dispatch must drain the queue.");
    }

    private static void ReachBarrier(Barrier barrier)
    {
        if (!barrier.SignalAndWait(2000))
        {
            throw new TimeoutException("Concurrent security test barrier timed out.");
        }
    }

    private static string DifferentCode(string code)
    {
        return code == "999999" ? "000000" : "999999";
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Expect<TException>(Action action, string message)
        where TException : Exception
    {
        checks++;
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private sealed class SlowReadStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly int delayMs;

        internal SlowReadStream(byte[] bytes, int delayMs)
        {
            inner = new MemoryStream(bytes);
            this.delayMs = delayMs;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(delayMs);
            return inner.Read(buffer, offset, count);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
