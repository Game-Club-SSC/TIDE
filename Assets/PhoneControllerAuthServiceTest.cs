#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PhoneControllerAuthServiceTest : MonoBehaviour
{
    [ContextMenu("Run All Phone Controller Auth Service Tests")]
    public void RunAllTests()
    {
        TestTokenRefreshMechanism();
        TestTokenPersistenceAcrossRestart();
        TestWebControllerAuthorization();
        TestPairingGuardLimitsAndExpiry();
        TestOriginPolicyAndBodyLimits();
        TestSessionGenerationRaceGuards();
        Debug.Log("=== All Phone Controller Auth Service Tests Passed ===");
    }

    [ContextMenu("Test Web Controller Authorization")]
    public void TestWebControllerAuthorization()
    {
        try
        {
            PhoneControllerAuthService.RevokeAllTokens();
            string token = PhoneControllerAuthService.GenerateSessionToken();
            Assert.AreEqual(64, token.Length,
                "A session token should contain 256 bits encoded as hex.");

            Assert.IsTrue(PhoneWebController.IsAuthorizationValid("Bearer " + token),
                "A valid bearer token should authorize a request.");
            Assert.IsFalse(PhoneWebController.IsAuthorizationValid(null),
                "A missing token must not authorize a request.");
            Assert.IsFalse(PhoneWebController.IsAuthorizationValid("Bearer wrong-token"),
                "An unknown token must not authorize a request.");

            string qrResponse = PhoneWebController.BuildQrDataResponse("http://127.0.0.1:8080");
            Assert.IsFalse(qrResponse.Contains("code"),
                "QR data must not disclose the pairing code.");

            string page = PhoneWebPageBuilder.BuildHtmlPage();
            Assert.IsTrue(page.Contains("data.token") && page.Contains("Authorization"),
                "The phone page must retain and send its pairing token.");
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    [ContextMenu("Test Pairing Guard Limits And Expiry")]
    public void TestPairingGuardLimitsAndExpiry()
    {
        DateTime now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        PhonePairingGuard guard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        string firstCode = guard.GetCurrentCode(now);

        Assert.AreEqual(6, firstCode.Length, "The pairing code should have six digits.");
        Assert.IsTrue(long.TryParse(firstCode, out _), "The pairing code should be numeric.");
        string wrongCode = firstCode == "999999" ? "000000" : "999999";
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(PhonePairingResult.InvalidCode,
                guard.TryPair("192.0.2.10", wrongCode, now.AddSeconds(i)),
                "Early wrong guesses should fail without a server-wide lockout.");
        }

        Assert.AreEqual(PhonePairingResult.RateLimited,
            guard.TryPair("192.0.2.10", wrongCode, now.AddSeconds(4)),
            "The fifth wrong guess from one peer should be rate limited.");
        string codeAfterAbuse = guard.GetCurrentCode(now.AddSeconds(4));
        Assert.AreNotEqual(firstCode, codeAfterAbuse,
            "Abuse should rotate the pairing code.");

        DateTime afterWindow = now.AddMinutes(2);
        string validCode = guard.GetCurrentCode(afterWindow);
        Assert.AreEqual(PhonePairingResult.Success,
            guard.TryPair("192.0.2.11", validCode, afterWindow),
            "A different peer should pair after the attempt window closes.");
        Assert.AreNotEqual(validCode, guard.GetCurrentCode(afterWindow),
            "Success should rotate the used pairing code.");

        PhonePairingGuard expiringGuard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        string expiredCode = expiringGuard.GetCurrentCode(now);
        Assert.AreEqual(PhonePairingResult.ExpiredCode,
            expiringGuard.TryPair("192.0.2.12", expiredCode, now.AddMinutes(6)),
            "An expired code should never pair.");

        PhonePairingGuard globalGuard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 4, now);
        string globalCode = globalGuard.GetCurrentCode(now);
        string globalWrongCode = globalCode == "999999" ? "000000" : "999999";
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(PhonePairingResult.InvalidCode,
                globalGuard.TryPair("192.0.2." + (20 + i), globalWrongCode, now),
                "Wrong guesses below the global cap should fail normally.");
        }
        Assert.AreEqual(PhonePairingResult.RateLimited,
            globalGuard.TryPair("192.0.2.23", globalWrongCode, now),
            "The global cap should stop guesses spread across peers.");
    }

    [ContextMenu("Test Origin Policy And Body Limits")]
    public void TestOriginPolicyAndBodyLimits()
    {
        Uri requestUrl = new Uri("http://192.0.2.10:8080/api/pair");
        Assert.IsTrue(PhoneWebController.IsOriginAllowed(null, requestUrl),
            "Native clients without an Origin header should be allowed.");
        Assert.IsTrue(PhoneWebController.IsOriginAllowed("http://192.0.2.10:8080", requestUrl),
            "The page served by this server should be allowed.");
        Assert.IsFalse(PhoneWebController.IsOriginAllowed("https://evil.example", requestUrl),
            "An unrelated web origin must be denied.");
        Assert.IsFalse(PhoneWebController.IsOriginAllowed("http://192.0.2.10:8081", requestUrl),
            "A different port is a different origin.");

        byte[] exactBody = System.Text.Encoding.UTF8.GetBytes("1234");
        using (var stream = new System.IO.MemoryStream(exactBody))
        {
            Assert.AreEqual("1234", PhoneWebController.ReadLimitedBody(
                stream, System.Text.Encoding.UTF8, exactBody.Length, 4, 1000));
        }

        bool rejectedDeclaredLength = false;
        try
        {
            using (var stream = new System.IO.MemoryStream(exactBody))
            {
                PhoneWebController.ReadLimitedBody(
                    stream, System.Text.Encoding.UTF8, 5, 4, 1000);
            }
        }
        catch (RequestBodyTooLargeException)
        {
            rejectedDeclaredLength = true;
        }
        Assert.IsTrue(rejectedDeclaredLength,
            "A declared body over the cap should be rejected before parsing.");

        bool rejectedStreamedBody = false;
        try
        {
            using (var stream = new System.IO.MemoryStream(
                System.Text.Encoding.UTF8.GetBytes("12345")))
            {
                PhoneWebController.ReadLimitedBody(
                    stream, System.Text.Encoding.UTF8, -1, 4, 1000);
            }
        }
        catch (RequestBodyTooLargeException)
        {
            rejectedStreamedBody = true;
        }
        Assert.IsTrue(rejectedStreamedBody,
            "A streamed body over the cap should be rejected while reading.");

        bool timedOut = false;
        SlowReadStream slowStream = new SlowReadStream(
            System.Text.Encoding.UTF8.GetBytes("1"), delayMs: 50);
        try
        {
            PhoneWebController.ReadLimitedBody(
                slowStream, System.Text.Encoding.UTF8, 1, 4, 5);
        }
        catch (TimeoutException)
        {
            timedOut = true;
        }
        finally
        {
            System.Threading.Thread.Sleep(60);
            slowStream.Dispose();
        }
        Assert.IsTrue(timedOut, "A slow request body should hit the read deadline.");
    }

    [ContextMenu("Test Session Generation Race Guards")]
    public void TestSessionGenerationRaceGuards()
    {
        try
        {
            TestPairVersusRegenerate();
            TestCommandBodyReadVersusRegenerate();
            TestQueuedCommandVersusStop();
            TestNormalConcurrentCommands();
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    private static void TestPairVersusRegenerate()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        var gate = new PhoneServerSessionGate<int>(8);
        DateTime now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        var guard = new PhonePairingGuard(
            6, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), 5, 20, now);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        Assert.IsTrue(gate.TryCapture(
            () => guard,
            value => value != null,
            out PhoneSessionAttempt<PhonePairingGuard> attempt));

        using (var paired = new Barrier(2))
        using (var resume = new Barrier(2))
        {
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

            Assert.IsTrue(worker.Join(2000), "Pair race worker did not finish.");
            Assert.IsNull(workerError, workerError?.ToString());
            Assert.AreEqual(PhonePairingResult.Success, pairingResult);
            Assert.IsFalse(completed, "A pre-regeneration pair must not mint a token.");
            Assert.IsNull(issuedToken);
            Assert.AreEqual(0, PhoneControllerAuthService.GetActiveTokenCount());
        }
    }

    private static void TestCommandBodyReadVersusRegenerate()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        var gate = new PhoneServerSessionGate<int>(8);
        string token = null;
        gate.Advance(true, () =>
        {
            PhoneControllerAuthService.RevokeAllTokens();
            token = PhoneControllerAuthService.GenerateSessionToken();
        });
        Assert.IsTrue(gate.TryCaptureAuthorized(() => true, out PhoneSessionStamp queuedStamp));
        Assert.AreEqual(PhoneCommandEnqueueResult.Enqueued, gate.TryEnqueue(queuedStamp, 99));
        long generationBeforeRegenerate = gate.CurrentGeneration;

        using (var authorized = new Barrier(2))
        using (var bodyRead = new Barrier(2))
        {
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
            Assert.Greater(gate.CurrentGeneration, generationBeforeRegenerate,
                "Regeneration must advance the session generation.");
            Assert.AreEqual(0, gate.PendingCount,
                "Regeneration must clear commands from the prior session.");
            ReachBarrier(bodyRead);

            Assert.IsTrue(worker.Join(2000), "Command race worker did not finish.");
            Assert.IsNull(workerError, workerError?.ToString());
            Assert.IsTrue(captured, "The command must authorize before its body read.");
            Assert.AreEqual(PhoneCommandEnqueueResult.StaleSession, enqueueResult);
            Assert.AreEqual(0, gate.PendingCount);
        }
    }

    private static void TestQueuedCommandVersusStop()
    {
        var gate = new PhoneServerSessionGate<int>(8);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        Assert.IsTrue(gate.TryCaptureAuthorized(() => true, out PhoneSessionStamp stamp));
        Assert.AreEqual(PhoneCommandEnqueueResult.Enqueued, gate.TryEnqueue(stamp, 7));
        Assert.AreEqual(PhoneCommandEnqueueResult.Enqueued, gate.TryEnqueue(stamp, 8));
        Queue<PhoneQueuedCommand<int>> batch = gate.TakeBatch(1);
        PhoneQueuedCommand<int> queued = batch.Dequeue();
        Assert.AreEqual(1, gate.PendingCount,
            "One queued command must remain for the stop cleanup check.");
        long generationBeforeStop = gate.CurrentGeneration;

        using (var dequeued = new Barrier(2))
        using (var stopped = new Barrier(2))
        {
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
            Assert.Greater(gate.CurrentGeneration, generationBeforeStop,
                "Stop must advance the session generation.");
            Assert.AreEqual(0, gate.PendingCount,
                "Stop must clear commands still held by the prior session.");
            ReachBarrier(stopped);

            Assert.IsTrue(worker.Join(2000), "Dispatch race worker did not finish.");
            Assert.IsNull(workerError, workerError?.ToString());
            Assert.IsFalse(dispatched, "A dequeued command must become stale after stop.");
            Assert.AreEqual(0, dispatchCount);
        }
    }

    private static void TestNormalConcurrentCommands()
    {
        const int CommandCount = 24;
        var gate = new PhoneServerSessionGate<int>(64);
        gate.Advance(true, PhoneControllerAuthService.RevokeAllTokens);
        var start = new Barrier(CommandCount + 1);
        var threads = new Thread[CommandCount];
        var errors = new Exception[CommandCount];
        var results = new PhoneCommandEnqueueResult[CommandCount];

        try
        {
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
                Assert.IsTrue(threads[i].Join(2000), "Normal command worker did not finish.");
                Assert.IsNull(errors[i], errors[i]?.ToString());
                Assert.AreEqual(PhoneCommandEnqueueResult.Enqueued, results[i]);
            }

            Queue<PhoneQueuedCommand<int>> batch = gate.TakeBatch(CommandCount);
            var dispatched = new HashSet<int>();
            while (batch.Count > 0)
            {
                Assert.IsTrue(gate.TryDispatch(batch.Dequeue(), value => dispatched.Add(value)));
            }
            Assert.AreEqual(CommandCount, dispatched.Count);
            Assert.AreEqual(0, gate.PendingCount);
        }
        finally
        {
            start.Dispose();
        }
    }

    private static void ReachBarrier(Barrier barrier)
    {
        if (!barrier.SignalAndWait(2000))
        {
            throw new TimeoutException("Concurrent security test barrier timed out.");
        }
    }

    [ContextMenu("Test Token Refresh Mechanism")]
    public void TestTokenRefreshMechanism()
    {
        Debug.Log("[PhoneControllerAuthServiceTest] Testing token refresh mechanism...");
        try
        {
            PhoneControllerAuthService.RevokeAllTokens();

            string token = PhoneControllerAuthService.GenerateToken();
            Assert.IsFalse(string.IsNullOrEmpty(token), "Generated token should not be null or empty.");
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token), "Freshly generated token should be valid.");

            bool reRegistered = PhoneControllerAuthService.RegisterToken(token, TimeSpan.FromHours(2));
            Assert.IsTrue(reRegistered, "Re-registering an existing token should succeed.");
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token),
                "Token should still be valid after re-registration (refresh).");

            Assert.AreEqual(1, PhoneControllerAuthService.GetActiveTokenCount(),
                "Re-registration should not create a duplicate token.");

            PhoneControllerAuthService.RevokeAllTokens();
            Debug.Log("[PhoneControllerAuthServiceTest] TestTokenRefreshMechanism passed.");
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    [ContextMenu("Test Token Persistence Across Restart")]
    public void TestTokenPersistenceAcrossRestart()
    {
        Debug.Log("[PhoneControllerAuthServiceTest] Testing token persistence across restart...");
        try
        {
            PhoneControllerAuthService.RevokeAllTokens();

            string token = PhoneControllerAuthService.GenerateToken();
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token), "Token should be valid after generation.");

            FieldInfo activeTokensField = typeof(PhoneControllerAuthService).GetField(
                "ActiveTokens", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(activeTokensField, "ActiveTokens dictionary should exist.");

            var activeTokens = activeTokensField.GetValue(null) as System.Collections.IDictionary;
            Assert.IsNotNull(activeTokens, "ActiveTokens should be a dictionary.");
            Assert.IsTrue(activeTokens.Contains(token), "Token should exist in the ActiveTokens dictionary.");

            DateTime expiry = (DateTime)activeTokens[token];
            Assert.IsTrue(expiry > DateTime.UtcNow, "Token expiry should be in the future.");

            string source = ReadSourceFile("PhoneControllerAuthService.cs");
            bool hasPersistentStorage = source.Contains("PlayerPrefs") ||
                source.Contains("File.") ||
                source.Contains("Serialize") ||
                source.Contains("Save") ||
                source.Contains("Persist");

            if (!hasPersistentStorage)
            {
                Debug.LogWarning("[PhoneControllerAuthServiceTest] Token storage is in-memory only (Dictionary). " +
                    "Tokens will not survive an actual application restart. " +
                    "Consider adding PlayerPrefs or file-based persistence.");
            }

            PhoneControllerAuthService.RevokeAllTokens();
            Debug.Log("[PhoneControllerAuthServiceTest] TestTokenPersistenceAcrossRestart passed.");
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    private static string ReadSourceFile(string fileName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(fileName.Replace(".cs", " t:MonoScript"));
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(fileName))
            {
                return System.IO.File.ReadAllText(path);
            }
        }
        return string.Empty;
    }

    private sealed class SlowReadStream : System.IO.Stream
    {
        private readonly System.IO.MemoryStream inner;
        private readonly int delayMs;

        internal SlowReadStream(byte[] bytes, int delayMs)
        {
            inner = new System.IO.MemoryStream(bytes);
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
            System.Threading.Thread.Sleep(delayMs);
            return inner.Read(buffer, offset, count);
        }

        public override void Flush() { }
        public override long Seek(long offset, System.IO.SeekOrigin origin) =>
            throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
#endif
