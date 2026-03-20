using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class RestorationTrackerTest : MonoBehaviour
{
    [ContextMenu("Run Restoration Tracker Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Restoration Tracker Tests ===");

        TestCombatContribution();
        TestPuzzleContribution();
        TestDuplicateEncounterPrevented();
        TestDuplicatePuzzleEncounterPrevented();
        TestLegacyCompleteEncounterCannotStack();
        TestTypedBuckets();
        TestRestorationPercentCalculation();
        TestThresholdQuery();
        TestFullRestorationEvent();
        TestResetIsland();
        TestMultiIslandIsolation();
        TestIslandRestorationStateDirect();

        Debug.Log("=== All Restoration Tracker Tests Passed ===");
    }

    private static IslandRestorationTracker CreateIsolatedTracker(string trackerName)
    {
        if (IslandRestorationTracker.Instance != null)
        {
            DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        GameObject trackerObject = new GameObject(trackerName);
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference the isolated test tracker instance.");
        return tracker;
    }

    private void TestCombatContribution()
    {
        Debug.Log("Testing combat contribution...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Combat");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);

        float percent = tracker.GetRestorationPercent("island_test");
        Assert.AreEqual(20f, percent, 0.01f, "Combat completion should add 20% restoration.");

        IslandRestorationState state = tracker.GetRestorationState("island_test");
        Assert.AreEqual(0.2f, state.CombatContribution, 0.001f, "Combat contribution should be 0.2.");
        Assert.AreEqual(1, state.CombatEncountersCompleted, "Should have 1 combat encounter completed.");
        Assert.AreEqual(0, state.PuzzleEncountersCompleted, "Should have 0 puzzle encounters completed.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Combat contribution test passed");
    }

    private void TestPuzzleContribution()
    {
        Debug.Log("Testing puzzle contribution...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Puzzle");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);

        float percent = tracker.GetRestorationPercent("island_test");
        Assert.AreEqual(30f, percent, 0.01f, "Puzzle completion should add 30% restoration.");

        IslandRestorationState state = tracker.GetRestorationState("island_test");
        Assert.AreEqual(0.3f, state.PuzzleContribution, 0.001f, "Puzzle contribution should be 0.3.");
        Assert.AreEqual(0, state.CombatEncountersCompleted, "Should have 0 combat encounters completed.");
        Assert.AreEqual(1, state.PuzzleEncountersCompleted, "Should have 1 puzzle encounter completed.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Puzzle contribution test passed");
    }

    private void TestDuplicateEncounterPrevented()
    {
        Debug.Log("Testing duplicate encounter prevention...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Dupe");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);
        tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);

        float percent = tracker.GetRestorationPercent("island_test");
        Assert.AreEqual(20f, percent, 0.01f, "Duplicate encounter should not add extra restoration.");

        IslandRestorationState state = tracker.GetRestorationState("island_test");
        Assert.AreEqual(1, state.CombatEncountersCompleted, "Should still have exactly 1 combat encounter completed.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Duplicate encounter prevention test passed");
    }

    private void TestDuplicatePuzzleEncounterPrevented()
    {
        Debug.Log("Testing duplicate puzzle encounter prevention...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_PuzzleDupe");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);
        tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);

        float percent = tracker.GetRestorationPercent("island_test");
        Assert.AreEqual(30f, percent, 0.01f, "Duplicate puzzle encounter should not add extra restoration.");

        IslandRestorationState state = tracker.GetRestorationState("island_test");
        Assert.AreEqual(1, state.PuzzleEncountersCompleted, "Should still have exactly 1 puzzle encounter completed.");

        DestroyImmediate(trackerObject);
        Debug.Log("笨・Duplicate puzzle encounter prevention test passed");
    }

    private void TestLegacyCompleteEncounterCannotStack()
    {
        Debug.Log("Testing legacy CompleteEncounter duplicate protection...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Legacy");
        GameObject trackerObject = tracker.gameObject;

        tracker.CompleteEncounter(0.2f);
        tracker.CompleteEncounter(0.4f);

        float percent = tracker.GetRestorationPercent("default");
        Assert.AreEqual(20f, percent, 0.01f, "Legacy CompleteEncounter should only count once without a real encounter id.");

        IslandRestorationState state = tracker.GetRestorationState("default");
        Assert.AreEqual(1, state.CombatEncountersCompleted, "Legacy CompleteEncounter should not stack duplicate combat completions.");
        Assert.AreEqual(1, state.CompletedEncounterIds.Count, "Legacy path should register a stable encounter id for duplicate protection.");

        DestroyImmediate(trackerObject);
        Debug.Log("笨・Legacy CompleteEncounter duplicate protection test passed");
    }

    private void TestTypedBuckets()
    {
        Debug.Log("Testing typed buckets (combat + puzzle)...");
        // Simulates Island 1: Combat A (20%) + Puzzle A (30%) + Combat B (20%) + Puzzle B (30%) = 100%

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Buckets");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_1", "combat_a", EncounterType.Combat, 0.2f);
        tracker.RecordEncounterCompletion("island_1", "puzzle_a", EncounterType.Puzzle, 0.3f);
        tracker.RecordEncounterCompletion("island_1", "combat_b", EncounterType.Combat, 0.2f);
        tracker.RecordEncounterCompletion("island_1", "puzzle_b", EncounterType.Puzzle, 0.3f);

        IslandRestorationState state = tracker.GetRestorationState("island_1");
        Assert.AreEqual(0.4f, state.CombatContribution, 0.001f, "Combat bucket should be 0.4 (20% + 20%).");
        Assert.AreEqual(0.6f, state.PuzzleContribution, 0.001f, "Puzzle bucket should be 0.6 (30% + 30%).");
        Assert.AreEqual(100f, state.RestorationPercent, 0.01f, "Total should be 100%.");
        Assert.IsTrue(state.IsIslandRestored, "Island should be fully restored.");
        Assert.AreEqual(2, state.CombatEncountersCompleted, "Should have 2 combat encounters completed.");
        Assert.AreEqual(2, state.PuzzleEncountersCompleted, "Should have 2 puzzle encounters completed.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Typed buckets test passed");
    }

    private void TestRestorationPercentCalculation()
    {
        Debug.Log("Testing restoration percent calculation...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Percent");
        GameObject trackerObject = tracker.gameObject;

        // Empty island should be 0%
        Assert.AreEqual(0f, tracker.GetRestorationPercent("empty_island"), "Empty island should be 0%.");

        tracker.RecordEncounterCompletion("island_pct", "c1", EncounterType.Combat, 0.25f);
        Assert.AreEqual(25f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be 25%.");

        tracker.RecordEncounterCompletion("island_pct", "p1", EncounterType.Puzzle, 0.50f);
        Assert.AreEqual(75f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be 75%.");

        // Adding more should clamp to 100%
        tracker.RecordEncounterCompletion("island_pct", "c2", EncounterType.Combat, 0.50f);
        Assert.AreEqual(100f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be clamped to 100%.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Restoration percent calculation test passed");
    }

    private void TestThresholdQuery()
    {
        Debug.Log("Testing threshold query...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Threshold");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_thresh", "c1", EncounterType.Combat, 0.4f);

        Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 30f), "40% should be >= 30% threshold.");
        Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 40f), "40% should be >= 40% threshold.");
        Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 50f), "40% should NOT be >= 50% threshold.");
        Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 80f), "40% should NOT be >= 80% threshold.");

        tracker.RecordEncounterCompletion("island_thresh", "p1", EncounterType.Puzzle, 0.4f);
        Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 80f), "80% should be >= 80% threshold after puzzle.");
        Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 81f), "80% should NOT be >= 81% threshold.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Threshold query test passed");
    }

    private void TestFullRestorationEvent()
    {
        Debug.Log("Testing full restoration event...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Event");
        GameObject trackerObject = tracker.gameObject;

        string restoredIsland = null;
        List<string> changedIslands = new List<string>();
        List<float> changedProgress = new List<float>();

        tracker.OnIslandRestored += (id) => { restoredIsland = id; };
        tracker.OnRestorationChanged += (id, progress) => { changedIslands.Add(id); changedProgress.Add(progress); };

        tracker.RecordEncounterCompletion("island_evt", "c1", EncounterType.Combat, 0.5f);
        Assert.IsNull(restoredIsland, "Should not fire OnIslandRestored at 50%.");
        Assert.AreEqual(1, changedIslands.Count, "Should fire OnRestorationChanged once.");

        tracker.RecordEncounterCompletion("island_evt", "p1", EncounterType.Puzzle, 0.5f);
        Assert.AreEqual("island_evt", restoredIsland, "Should fire OnIslandRestored with correct islandId at 100%.");
        Assert.AreEqual(2, changedIslands.Count, "Should fire OnRestorationChanged twice total.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Full restoration event test passed");
    }

    private void TestResetIsland()
    {
        Debug.Log("Testing reset island...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Reset");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_reset", "c1", EncounterType.Combat, 0.5f);
        Assert.AreEqual(50f, tracker.GetRestorationPercent("island_reset"), 0.01f, "Should be 50% before reset.");

        tracker.ResetIsland("island_reset");
        Assert.AreEqual(0f, tracker.GetRestorationPercent("island_reset"), 0.01f, "Should be 0% after reset.");
        Assert.IsFalse(tracker.IsIslandRestored("island_reset"), "Should not be restored after reset.");

        IslandRestorationState state = tracker.GetRestorationState("island_reset");
        Assert.AreEqual(0, state.CombatEncountersCompleted, "Should have 0 encounters after reset.");
        Assert.AreEqual(0, state.PuzzleEncountersCompleted, "Should have 0 encounters after reset.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Reset island test passed");
    }

    private void TestMultiIslandIsolation()
    {
        Debug.Log("Testing multi-island isolation...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Multi");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_a", "c1", EncounterType.Combat, 0.5f);
        tracker.RecordEncounterCompletion("island_b", "c1", EncounterType.Combat, 0.3f);

        Assert.AreEqual(50f, tracker.GetRestorationPercent("island_a"), 0.01f, "Island A should be 50%.");
        Assert.AreEqual(30f, tracker.GetRestorationPercent("island_b"), 0.01f, "Island B should be 30%.");

        tracker.ResetIsland("island_a");
        Assert.AreEqual(0f, tracker.GetRestorationPercent("island_a"), 0.01f, "Island A should be 0% after reset.");
        Assert.AreEqual(30f, tracker.GetRestorationPercent("island_b"), 0.01f, "Island B should still be 30%.");

        DestroyImmediate(trackerObject);
        Debug.Log("✓ Multi-island isolation test passed");
    }

    private void TestIslandRestorationStateDirect()
    {
        Debug.Log("Testing IslandRestorationState direct usage...");

        IslandRestorationState state = new IslandRestorationState("test_island");
        Assert.AreEqual("test_island", state.IslandId);
        Assert.AreEqual(0f, state.TotalContribution);
        Assert.IsFalse(state.IsIslandRestored);
        Assert.IsFalse(state.HasCompleted("anything"));

        state.RecordCompletion("c1", EncounterType.Combat, 0.2f);
        Assert.IsTrue(state.HasCompleted("c1"));
        Assert.IsFalse(state.HasCompleted("c2"));
        Assert.AreEqual(0.2f, state.CombatContribution, 0.001f);
        Assert.AreEqual(1, state.CombatEncountersCompleted);

        state.RecordCompletion("c1", EncounterType.Combat, 0.2f);
        Assert.AreEqual(1, state.CombatEncountersCompleted, "Duplicate should not increment count.");
        Assert.AreEqual(0.2f, state.CombatContribution, 0.001f, "Duplicate should not add contribution.");

        state.RecordCompletion("p1", EncounterType.Puzzle, 0.3f);
        Assert.AreEqual(0.5f, state.TotalContribution, 0.001f);
        Assert.AreEqual(20f + 30f, state.RestorationPercent, 0.01f);

        state.Reset();
        Assert.AreEqual(0f, state.TotalContribution);
        Assert.AreEqual(0, state.CombatEncountersCompleted);
        Assert.AreEqual(0, state.PuzzleEncountersCompleted);
        Assert.IsFalse(state.HasCompleted("c1"));

        Debug.Log("✓ IslandRestorationState direct test passed");
    }
}
