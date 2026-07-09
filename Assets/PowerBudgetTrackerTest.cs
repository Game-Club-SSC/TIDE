using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PowerBudgetTrackerTest : MonoBehaviour
{
    [ContextMenu("Run Power Budget Tracker Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Power Budget Tracker Tests ===");

        TestSingletonCreation();
        TestSingletonDuplicateGuard();
        TestSingletonClearsOnDestroy();
        TestDontDestroyOnLoad();
        TestSeedBudgetsInitializesAllIslands();
        TestSeedBudgetsNoRedundantBranch();
        TestTryConsumeBudgetSucceedsWithinBudget();
        TestTryConsumeBudgetRejectsInsufficient();
        TestTryConsumeBudgetRejectsNullOrEmptyIsland();
        TestTryConsumeBudgetRejectsZeroOrNegativeCost();
        TestTryConsumeBudgetSeedsUnknownIsland();
        TestTryConsumeBudgetEventFires();
        TestSetBudgetClampsNegative();
        TestGetRemainingBudgetReturnsZeroForUnknown();
        TestRefundBudgetAddsAmount();
        TestResetBudgetRestoresDefault();
        TestResetAllBudgetsResetsAll();
        TestSnapshotReturnsCurrentState();

        Debug.Log("=== All Power Budget Tracker Tests Passed ===");
    }

    private PowerBudgetTracker CreateIsolatedTracker()
    {
        if (PowerBudgetTracker.Instance != null)
        {
            DestroyImmediate(PowerBudgetTracker.Instance.gameObject);
        }

        GameObject go = new GameObject("TestPowerBudgetTracker");
        PowerBudgetTracker tracker = go.AddComponent<PowerBudgetTracker>();
        Assert.AreSame(tracker, PowerBudgetTracker.Instance,
            "Tracker singleton should reference the isolated test instance.");
        return tracker;
    }

    private void TestSingletonCreation()
    {
        Debug.Log("Testing PowerBudgetTracker singleton creation...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            Assert.IsNotNull(PowerBudgetTracker.Instance, "Instance should be set.");
            Assert.AreSame(tracker, PowerBudgetTracker.Instance, "Instance should match.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Singleton creation test passed");
    }

    private void TestSingletonDuplicateGuard()
    {
        Debug.Log("Testing PowerBudgetTracker duplicate guard...");

        PowerBudgetTracker first = CreateIsolatedTracker();
        GameObject firstGo = first.gameObject;

        try
        {
            GameObject secondGo = new GameObject("TestPowerBudgetTracker_Duplicate");
            secondGo.AddComponent<PowerBudgetTracker>();

            Assert.IsTrue(secondGo == null, "Duplicate instance should be destroyed.");
            Assert.AreSame(first, PowerBudgetTracker.Instance, "Original should remain.");
        }
        finally
        {
            if (firstGo != null) DestroyImmediate(firstGo);
        }

        Debug.Log("✓ Duplicate guard test passed");
    }

    private void TestSingletonClearsOnDestroy()
    {
        Debug.Log("Testing PowerBudgetTracker clears Instance on destroy...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        DestroyImmediate(tracker.gameObject);

        Assert.IsNull(PowerBudgetTracker.Instance, "Instance should be null after destroy.");

        Debug.Log("✓ Singleton clear on destroy test passed");
    }

    private void TestDontDestroyOnLoad()
    {
        Debug.Log("Testing PowerBudgetTracker uses DontDestroyOnLoad...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "PowerBudgetTracker.cs"));

        Assert.IsTrue(sourceCode.Contains("DontDestroyOnLoad"),
            "PowerBudgetTracker should use DontDestroyOnLoad.");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            Assert.IsTrue(go.scene.IsValid(), "GameObject should be valid.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ DontDestroyOnLoad test passed");
    }

    private void TestSeedBudgetsInitializesAllIslands()
    {
        Debug.Log("Testing SeedBudgets initializes budgets for all progression islands...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;
            for (int i = 0; i < order.Count; i++)
            {
                float budget = tracker.GetRemainingBudget(order[i]);
                Assert.AreEqual(tracker.DefaultBudgetPerIsland, budget, 0.001f,
                    $"Island '{order[i]}' should have default budget of {tracker.DefaultBudgetPerIsland}.");
            }
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ SeedBudgets initializes all islands test passed");
    }

    private void TestSeedBudgetsNoRedundantBranch()
    {
        Debug.Log("Testing SeedBudgets has no redundant branch...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "PowerBudgetTracker.cs"));

        string seedBudgetsBody = ExtractMethodBody(sourceCode, "SeedBudgets");
        Assert.IsNotNull(seedBudgetsBody, "SeedBudgets method body should be extractable.");

        Assert.IsFalse(seedBudgetsBody.Contains("ContainsKey"),
            "SeedBudgets should not have a redundant ContainsKey guard.");

        Debug.Log("✓ SeedBudgets redundant branch test passed");
    }

    private string ExtractMethodBody(string source, string methodName)
    {
        int methodStart = source.IndexOf($"private void {methodName}()");
        if (methodStart < 0)
        {
            methodStart = source.IndexOf($"private void {methodName}(");
        }
        if (methodStart < 0) return null;

        int braceStart = source.IndexOf('{', methodStart);
        if (braceStart < 0) return null;

        int depth = 0;
        for (int i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            if (source[i] == '}') depth--;
            if (depth == 0) return source.Substring(braceStart, i - braceStart + 1);
        }

        return null;
    }

    private void TestTryConsumeBudgetSucceedsWithinBudget()
    {
        Debug.Log("Testing TryConsumeBudget succeeds when cost is within budget...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_test", 5f);

            bool result = tracker.TryConsumeBudget("island_test", 3f);

            Assert.IsTrue(result, "TryConsumeBudget should succeed when cost <= remaining budget.");
            Assert.AreEqual(2f, tracker.GetRemainingBudget("island_test"), 0.001f,
                "Remaining budget should be 2 after consuming 3 from 5.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget within budget test passed");
    }

    private void TestTryConsumeBudgetRejectsInsufficient()
    {
        Debug.Log("Testing TryConsumeBudget rejects when cost exceeds budget...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_test", 2f);

            bool result = tracker.TryConsumeBudget("island_test", 5f);

            Assert.IsFalse(result, "TryConsumeBudget should reject when cost > remaining budget.");
            Assert.AreEqual(2f, tracker.GetRemainingBudget("island_test"), 0.001f,
                "Budget should remain unchanged after rejected consumption.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget insufficient test passed");
    }

    private void TestTryConsumeBudgetRejectsNullOrEmptyIsland()
    {
        Debug.Log("Testing TryConsumeBudget rejects null or empty islandId...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            Assert.IsFalse(tracker.TryConsumeBudget(null, 1f),
                "TryConsumeBudget should reject null islandId.");
            Assert.IsFalse(tracker.TryConsumeBudget("", 1f),
                "TryConsumeBudget should reject empty islandId.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget null/empty island test passed");
    }

    private void TestTryConsumeBudgetRejectsZeroOrNegativeCost()
    {
        Debug.Log("Testing TryConsumeBudget rejects zero or negative cost...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            Assert.IsFalse(tracker.TryConsumeBudget("island_test", 0f),
                "TryConsumeBudget should reject zero cost.");
            Assert.IsFalse(tracker.TryConsumeBudget("island_test", -1f),
                "TryConsumeBudget should reject negative cost.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget zero/negative cost test passed");
    }

    private void TestTryConsumeBudgetSeedsUnknownIsland()
    {
        Debug.Log("Testing TryConsumeBudget seeds unknown island with default budget...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            float unknownBudget = tracker.GetRemainingBudget("island_unknown");
            Assert.AreEqual(0f, unknownBudget, 0.001f, "Unknown island should return 0 before consumption.");

            bool result = tracker.TryConsumeBudget("island_unknown", 1f);

            Assert.IsTrue(result, "TryConsumeBudget should succeed for unknown island (auto-seeded).");
            Assert.AreEqual(tracker.DefaultBudgetPerIsland - 1f, tracker.GetRemainingBudget("island_unknown"), 0.001f,
                "Unknown island should be seeded with default budget then reduced by cost.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget seeds unknown island test passed");
    }

    private void TestTryConsumeBudgetEventFires()
    {
        Debug.Log("Testing TryConsumeBudget fires OnBudgetChanged event...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            string eventIslandId = null;
            float eventNewRemaining = -1f;
            float eventDelta = 0f;

            tracker.OnBudgetChanged += (islandId, newRemaining, delta) =>
            {
                eventIslandId = islandId;
                eventNewRemaining = newRemaining;
                eventDelta = delta;
            };

            tracker.SetBudget("island_event", 5f);
            tracker.TryConsumeBudget("island_event", 2f);

            Assert.AreEqual("island_event", eventIslandId, "Event should pass the correct island ID.");
            Assert.AreEqual(3f, eventNewRemaining, 0.001f, "Event should pass the correct remaining budget.");
            Assert.AreEqual(-2f, eventDelta, 0.001f, "Event should pass a negative delta for consumption.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ TryConsumeBudget event test passed");
    }

    private void TestSetBudgetClampsNegative()
    {
        Debug.Log("Testing SetBudget clamps negative values to 0...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_neg", -5f);

            Assert.AreEqual(0f, tracker.GetRemainingBudget("island_neg"), 0.001f,
                "SetBudget should clamp negative values to 0.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ SetBudget clamps negative test passed");
    }

    private void TestGetRemainingBudgetReturnsZeroForUnknown()
    {
        Debug.Log("Testing GetRemainingBudget returns 0 for unknown island...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            float budget = tracker.GetRemainingBudget("island_nonexistent");
            Assert.AreEqual(0f, budget, 0.001f, "Unknown island should return 0.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ GetRemainingBudget unknown island test passed");
    }

    private void TestRefundBudgetAddsAmount()
    {
        Debug.Log("Testing RefundBudget adds amount to current budget...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_refund", 3f);
            tracker.TryConsumeBudget("island_refund", 2f);

            Assert.AreEqual(1f, tracker.GetRemainingBudget("island_refund"), 0.001f,
                "Should have 1 remaining after consuming 2 from 3.");

            tracker.RefundBudget("island_refund", 1.5f);

            Assert.AreEqual(2.5f, tracker.GetRemainingBudget("island_refund"), 0.001f,
                "Should have 2.5 after refunding 1.5.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ RefundBudget test passed");
    }

    private void TestResetBudgetRestoresDefault()
    {
        Debug.Log("Testing ResetBudget restores default budget...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_reset", 10f);
            tracker.TryConsumeBudget("island_reset", 8f);

            tracker.ResetBudget("island_reset");

            Assert.AreEqual(tracker.DefaultBudgetPerIsland, tracker.GetRemainingBudget("island_reset"), 0.001f,
                "ResetBudget should restore to default budget.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ResetBudget test passed");
    }

    private void TestResetAllBudgetsResetsAll()
    {
        Debug.Log("Testing ResetAllBudgets resets all islands...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;

            for (int i = 0; i < order.Count; i++)
            {
                tracker.TryConsumeBudget(order[i], 1f);
            }

            tracker.ResetAllBudgets();

            for (int i = 0; i < order.Count; i++)
            {
                Assert.AreEqual(tracker.DefaultBudgetPerIsland, tracker.GetRemainingBudget(order[i]), 0.001f,
                    $"Island '{order[i]}' should be reset to default budget.");
            }
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ResetAllBudgets test passed");
    }

    private void TestSnapshotReturnsCurrentState()
    {
        Debug.Log("Testing Snapshot returns current budget state...");

        PowerBudgetTracker tracker = CreateIsolatedTracker();
        GameObject go = tracker.gameObject;

        try
        {
            tracker.SetBudget("island_snap", 7f);

            IReadOnlyDictionary<string, float> snapshot = tracker.Snapshot();

            Assert.IsNotNull(snapshot, "Snapshot should not be null.");
            Assert.IsTrue(snapshot.ContainsKey("island_snap"), "Snapshot should contain the set island.");
            Assert.AreEqual(7f, snapshot["island_snap"], 0.001f, "Snapshot should reflect current budget.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Snapshot test passed");
    }
}
