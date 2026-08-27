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
        TestSingletonDuplicateGuardPreservesOriginalTracker();
        TestSingletonClearsInstanceOnDestroy();
        TestLegacyCompleteEncounterCannotStack();
        TestRecordEncounterCompletionReturnValue();
        TestTypedBuckets();
        TestRestorationPercentCalculation();
        TestThresholdQuery();
        TestConfiguredBossContributionCompletesIslandPastCombatCap();
        TestLegacyBossSnapshotMigration();
        TestFullRestorationEvent();
        TestResetIsland();
        TestMultiIslandIsolation();
        TestSnapshotRoundTripPreservesCompletedEncounterIds();
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
        trackerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference the isolated test tracker instance.");
        return tracker;
    }

    private void TestCombatContribution()
    {
        Debug.Log("Testing combat contribution...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Combat");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);

            float percent = tracker.GetRestorationPercent("island_test");
            Assert.AreEqual(20f, percent, 0.01f, "Combat completion should add 20% restoration.");

            IslandRestorationState state = tracker.GetRestorationState("island_test");
            Assert.AreEqual(0.2f, state.CombatContribution, 0.001f, "Combat contribution should be 0.2.");
            Assert.AreEqual(1, state.CombatEncountersCompleted, "Should have 1 combat encounter completed.");
            Assert.AreEqual(0, state.PuzzleEncountersCompleted, "Should have 0 puzzle encounters completed.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Combat contribution test passed");
    }

    private void TestPuzzleContribution()
    {
        Debug.Log("Testing puzzle contribution...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Puzzle");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);

            float percent = tracker.GetRestorationPercent("island_test");
            Assert.AreEqual(30f, percent, 0.01f, "Puzzle completion should add 30% restoration.");

            IslandRestorationState state = tracker.GetRestorationState("island_test");
            Assert.AreEqual(0.3f, state.PuzzleContribution, 0.001f, "Puzzle contribution should be 0.3.");
            Assert.AreEqual(0, state.CombatEncountersCompleted, "Should have 0 combat encounters completed.");
            Assert.AreEqual(1, state.PuzzleEncountersCompleted, "Should have 1 puzzle encounter completed.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Puzzle contribution test passed");
    }

    private void TestDuplicateEncounterPrevented()
    {
        Debug.Log("Testing duplicate encounter prevention...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Dupe");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);
            tracker.RecordEncounterCompletion("island_test", "combat_1", EncounterType.Combat, 0.2f);

            float percent = tracker.GetRestorationPercent("island_test");
            Assert.AreEqual(20f, percent, 0.01f, "Duplicate encounter should not add extra restoration.");

            IslandRestorationState state = tracker.GetRestorationState("island_test");
            Assert.AreEqual(1, state.CombatEncountersCompleted, "Should still have exactly 1 combat encounter completed.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Duplicate encounter prevention test passed");
    }

    private void TestDuplicatePuzzleEncounterPrevented()
    {
        Debug.Log("Testing duplicate puzzle encounter prevention...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_PuzzleDupe");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);
            tracker.RecordEncounterCompletion("island_test", "puzzle_1", EncounterType.Puzzle, 0.3f);

            float percent = tracker.GetRestorationPercent("island_test");
            Assert.AreEqual(30f, percent, 0.01f, "Duplicate puzzle encounter should not add extra restoration.");

            IslandRestorationState state = tracker.GetRestorationState("island_test");
            Assert.AreEqual(1, state.PuzzleEncountersCompleted, "Should still have exactly 1 puzzle encounter completed.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Duplicate puzzle encounter prevention test passed");
    }

    private void TestSingletonDuplicateGuardPreservesOriginalTracker()
    {
        Debug.Log("Testing singleton duplicate guard preserves original tracker...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_SingletonPrimary");
        GameObject trackerObject = tracker.gameObject;
        GameObject duplicateObject = new GameObject("TestTracker_SingletonDuplicate");

        try
        {
            IslandRestorationTracker duplicate = duplicateObject.AddComponent<IslandRestorationTracker>();
            duplicateObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            Assert.AreSame(tracker, IslandRestorationTracker.Instance,
                "Duplicate tracker should not replace the active singleton.");
            Assert.IsTrue(trackerObject != null,
                "Original tracker owner GameObject should remain after duplicate creation.");
            Assert.IsTrue(duplicateObject != null,
                "Duplicate tracker owner GameObject should remain after component-only cleanup.");

            if (Application.isPlaying)
            {
                Assert.AreNotSame(duplicate, IslandRestorationTracker.Instance,
                    "Duplicate tracker should never become the active singleton.");
            }
            else
            {
                Assert.IsNull(duplicateObject.GetComponent<IslandRestorationTracker>(),
                    "Duplicate tracker component should be removed without destroying its GameObject.");
                Assert.IsTrue(duplicate == null,
                    "Duplicate tracker component should be destroyed immediately during verification runs.");
            }
        }
        finally
        {
            if (duplicateObject != null)
            {
                DestroyImmediate(duplicateObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }

        Debug.Log("Tracker singleton duplicate guard test passed");
    }

    private void TestSingletonClearsInstanceOnDestroy()
    {
        Debug.Log("Testing tracker singleton clears Instance on destroy...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_SingletonDestroy");
        GameObject trackerObject = tracker.gameObject;

        trackerObject.SendMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
        DestroyImmediate(trackerObject);

        Assert.IsNull(IslandRestorationTracker.Instance,
            "Destroying the active tracker should clear the singleton instance.");
        Debug.Log("Tracker singleton destroy cleanup test passed");
    }

    private void TestLegacyCompleteEncounterCannotStack()
    {
        Debug.Log("Testing legacy CompleteEncounter duplicate protection...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Legacy");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.CompleteEncounter(0.2f);
            tracker.CompleteEncounter(0.4f);

            float percent = tracker.GetRestorationPercent("island_lust");
            Assert.AreEqual(20f, percent, 0.01f, "Legacy CompleteEncounter should only count once without a real encounter id.");

            IslandRestorationState state = tracker.GetRestorationState("island_lust");
            Assert.AreEqual(1, state.CombatEncountersCompleted, "Legacy CompleteEncounter should not stack duplicate combat completions.");
            Assert.AreEqual(1, state.CompletedEncounterIds.Count, "Legacy path should register a stable encounter id for duplicate protection.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Legacy CompleteEncounter duplicate protection test passed");
    }

    private void TestTypedBuckets()
    {
        Debug.Log("Testing typed buckets (combat + puzzle)...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Buckets");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_lust", "combat_a", EncounterType.Combat, 0.2f);
            tracker.RecordEncounterCompletion("island_lust", "puzzle_a", EncounterType.Puzzle, 0.3f);
            tracker.RecordEncounterCompletion("island_lust", "combat_b", EncounterType.Combat, 0.2f);
            tracker.RecordEncounterCompletion("island_lust", "puzzle_b", EncounterType.Puzzle, 0.3f);

            IslandRestorationState state = tracker.GetRestorationState("island_lust");
            Assert.AreEqual(0.4f, state.CombatContribution, 0.001f, "Combat bucket should be 0.4 (20% + 20%).");
            Assert.AreEqual(0.6f, state.PuzzleContribution, 0.001f, "Puzzle bucket should be 0.6 (30% + 30%).");
            Assert.AreEqual(100f, state.RestorationPercent, 0.01f, "Total should be 100%.");
            Assert.IsTrue(state.IsIslandRestored, "Island should be fully restored.");
            Assert.AreEqual(2, state.CombatEncountersCompleted, "Should have 2 combat encounters completed.");
            Assert.AreEqual(2, state.PuzzleEncountersCompleted, "Should have 2 puzzle encounters completed.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Typed buckets test passed");
    }

    private void TestRecordEncounterCompletionReturnValue()
    {
        Debug.Log("Testing encounter completion return value...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_ReturnValue");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            bool recordedFirst = tracker.RecordEncounterCompletion("island_return", "combat_1", EncounterType.Combat, 0.125f);
            bool recordedDuplicate = tracker.RecordEncounterCompletion("island_return", "combat_1", EncounterType.Combat, 0.125f);
            bool recordedInvalid = tracker.RecordEncounterCompletion("island_return", string.Empty, EncounterType.Combat, 0.125f);

            Assert.IsTrue(recordedFirst, "First encounter completion should report success.");
            Assert.IsFalse(recordedDuplicate, "Duplicate encounter completion should report failure.");
            Assert.IsFalse(recordedInvalid, "Invalid encounter completion should report failure.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Encounter completion return value test passed");
    }

    private void TestRestorationPercentCalculation()
    {
        Debug.Log("Testing restoration percent calculation...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Percent");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            Assert.AreEqual(0f, tracker.GetRestorationPercent("empty_island"), "Empty island should be 0%.");

            tracker.RecordEncounterCompletion("island_pct", "c1", EncounterType.Combat, 0.25f);
            Assert.AreEqual(25f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be 25%.");

            tracker.RecordEncounterCompletion("island_pct", "p1", EncounterType.Puzzle, 0.50f);
            Assert.AreEqual(75f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be 75%.");

            tracker.RecordEncounterCompletion("island_pct", "c2", EncounterType.Combat, 0.50f);
            Assert.AreEqual(100f, tracker.GetRestorationPercent("island_pct"), 0.01f, "Should be clamped to 100%.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Restoration percent calculation test passed");
    }

    private void TestThresholdQuery()
    {
        Debug.Log("Testing threshold query...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Threshold");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_thresh", "c1", EncounterType.Combat, 0.4f);

            Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 30f), "40% should be >= 30% threshold.");
            Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 40f), "40% should be >= 40% threshold.");
            Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 50f), "40% should NOT be >= 50% threshold.");
            Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 80f), "40% should NOT be >= 80% threshold.");

            tracker.RecordEncounterCompletion("island_thresh", "p1", EncounterType.Puzzle, 0.4f);
            Assert.IsTrue(tracker.IsRestorationAtOrAbove("island_thresh", 80f), "80% should be >= 80% threshold after puzzle.");
            Assert.IsFalse(tracker.IsRestorationAtOrAbove("island_thresh", 81f), "80% should NOT be >= 81% threshold.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

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

        try
        {
            tracker.RecordEncounterCompletion("island_envy", "c1", EncounterType.Combat, 0.5f);
            Assert.IsNull(restoredIsland, "Should not fire OnIslandRestored at 50%.");
            Assert.AreEqual(1, changedIslands.Count, "Should fire OnRestorationChanged once.");
            Assert.AreEqual("island_envy", changedIslands[0], "Event should report the correct islandId.");
            Assert.AreEqual(50f, changedProgress[0], 0.01f, "Event should report 50% progress.");

            tracker.RecordEncounterCompletion("island_envy", "p1", EncounterType.Puzzle, 0.5f);
            Assert.AreEqual("island_envy", restoredIsland, "Should fire OnIslandRestored with correct islandId at 100%.");
            Assert.AreEqual(2, changedIslands.Count, "Should fire OnRestorationChanged twice total.");
            Assert.AreEqual("island_envy", changedIslands[1], "Second event should report the correct islandId.");
            Assert.AreEqual(100f, changedProgress[1], 0.01f, "Second event should report 100% progress.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Full restoration event test passed");
    }

    private void TestConfiguredBossContributionCompletesIslandPastCombatCap()
    {
        Debug.Log("Testing configured boss contribution bypasses ordinary-combat cap...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossContribution");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            const string islandId = "island_lust";
            IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
            Assert.IsNotNull(config, "Lust island config should load for boss restoration verification.");
            Assert.IsNotNull(config.encounters, "Lust island should define encounters.");

            EncounterDefinition boss = null;
            for (int i = 0; i < config.encounters.Length; i++)
            {
                if (config.encounters[i] != null && config.encounters[i].isBossEncounter)
                {
                    boss = config.encounters[i];
                    break;
                }
            }

            Assert.IsNotNull(boss, "Lust island should define a boss encounter.");
            Assert.Greater(boss.restorationValue, 0f,
                "Configured boss should contribute restoration after the 75% gate.");

            tracker.RecordEncounterCompletion(islandId, "preboss_combat", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion(islandId, "preboss_puzzle", EncounterType.Puzzle, 0.25f);
            Assert.AreEqual(75f, tracker.GetRestorationPercent(islandId), 0.01f,
                "Setup should match the GDD boss-unlock threshold.");

            bool recorded = tracker.RecordEncounterCompletion(
                islandId,
                boss.encounterId,
                boss.type,
                boss.restorationValue);

            IslandRestorationState state = tracker.GetRestorationState(islandId);
            Assert.IsTrue(recorded, "Configured boss victory should be recorded.");
            Assert.AreEqual(0.5f, state.CombatContribution, 0.001f,
                "Ordinary combat should remain capped at 50% after the boss.");
            Assert.AreEqual(boss.restorationValue, state.BossContribution, 0.001f,
                "Boss value should be tracked outside the ordinary-combat bucket.");
            Assert.AreEqual(100f, state.RestorationPercent, 0.01f,
                "Configured boss victory should complete the island from 75%.");
            Assert.IsTrue(state.IsIslandRestored,
                "Configured boss victory should fire the full-restoration progression boundary.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Configured boss contribution test passed");
    }

    private void TestLegacyBossSnapshotMigration()
    {
        Debug.Log("Testing legacy capped boss snapshot migration...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossSnapshotMigration");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            const string islandId = "island_lust";
            IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
            Assert.IsNotNull(config, "Lust island config should load for save migration verification.");
            Assert.IsNotNull(config.encounters, "Lust island should define encounters for save migration.");

            IslandRestorationStateSnapshot legacyState = new IslandRestorationStateSnapshot
            {
                islandId = islandId,
                combatContribution = 0.5f,
                puzzleContribution = 0.25f,
                bossContribution = 0f,
                totalContribution = 0.75f,
                completedEncounterIds = new List<string>()
            };

            float configuredCombatContribution = 0f;
            float configuredBossContribution = 0f;
            for (int i = 0; i < config.encounters.Length; i++)
            {
                EncounterDefinition encounter = config.encounters[i];
                if (encounter == null || string.IsNullOrEmpty(encounter.encounterId))
                {
                    continue;
                }

                legacyState.completedEncounterIds.Add(encounter.encounterId);
                if (encounter.type == EncounterType.Combat)
                {
                    legacyState.combatEncountersCompleted++;
                    if (encounter.isBossEncounter)
                    {
                        configuredBossContribution += encounter.restorationValue;
                    }
                    else
                    {
                        configuredCombatContribution += encounter.restorationValue;
                    }
                }
                else
                {
                    legacyState.puzzleEncountersCompleted++;
                }
            }

            Assert.AreEqual(0.5f, configuredCombatContribution, 0.001f,
                "Lust ordinary combat data should match the GDD 50% cap.");
            Assert.AreEqual(0.25f, configuredBossContribution, 0.001f,
                "Lust boss data should supply the final 25% after the 75% gate.");

            IslandRestorationTracker.TrackerSnapshot legacySnapshot =
                new IslandRestorationTracker.TrackerSnapshot();
            legacySnapshot.islands.Add(legacyState);
            tracker.ApplySnapshot(legacySnapshot);

            IslandRestorationState migratedState = tracker.GetRestorationState(islandId);
            Assert.AreEqual(configuredCombatContribution, migratedState.CombatContribution, 0.001f,
                "Migration should retain the configured ordinary-combat contribution.");
            Assert.AreEqual(configuredBossContribution, migratedState.BossContribution, 0.001f,
                "Migration should recover the completed configured boss contribution.");
            Assert.AreEqual(100f, migratedState.RestorationPercent, 0.01f,
                "A cap-era boss-complete save should migrate from 75% to full restoration.");
            Assert.IsTrue(migratedState.IsIslandRestored,
                "Migrated boss-complete save should satisfy the island-restored boundary.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Legacy capped boss snapshot migration test passed");
    }

    private void TestResetIsland()
    {
        Debug.Log("Testing reset island...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Reset");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_reset", "c1", EncounterType.Combat, 0.5f);
            Assert.AreEqual(50f, tracker.GetRestorationPercent("island_reset"), 0.01f, "Should be 50% before reset.");

            tracker.ResetIsland("island_reset");
            Assert.AreEqual(0f, tracker.GetRestorationPercent("island_reset"), 0.01f, "Should be 0% after reset.");
            Assert.IsFalse(tracker.IsIslandRestored("island_reset"), "Should not be restored after reset.");

            IslandRestorationState state = tracker.GetRestorationState("island_reset");
            Assert.AreEqual(0, state.CombatEncountersCompleted, "Should have 0 encounters after reset.");
            Assert.AreEqual(0, state.PuzzleEncountersCompleted, "Should have 0 encounters after reset.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Reset island test passed");
    }

    private void TestMultiIslandIsolation()
    {
        Debug.Log("Testing multi-island isolation...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Multi");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_lust", "c1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion("island_greed", "c1", EncounterType.Combat, 0.3f);

            Assert.AreEqual(50f, tracker.GetRestorationPercent("island_lust"), 0.01f, "Lust should be 50%.");
            Assert.AreEqual(30f, tracker.GetRestorationPercent("island_greed"), 0.01f, "Greed should be 30%.");

            tracker.ResetIsland("island_lust");
            Assert.AreEqual(0f, tracker.GetRestorationPercent("island_lust"), 0.01f, "Lust should be 0% after reset.");
            Assert.AreEqual(30f, tracker.GetRestorationPercent("island_greed"), 0.01f, "Greed should still be 30%.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Multi-island isolation test passed");
    }

    private void TestSnapshotRoundTripPreservesCompletedEncounterIds()
    {
        Debug.Log("Testing tracker snapshot round-trip...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_Snapshot");
        GameObject trackerObject = tracker.gameObject;

        try
        {
            tracker.RecordEncounterCompletion("island_snapshot", "combat_1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion("island_snapshot", "puzzle_1", EncounterType.Puzzle, 0.25f);

            IslandRestorationTracker.TrackerSnapshot snapshot = tracker.CaptureSnapshot();
            tracker.ResetIsland("island_snapshot");
            tracker.ApplySnapshot(snapshot);

            Assert.IsTrue(tracker.HasClearedEncounter("island_snapshot", "combat_1"),
                "Completed combat encounter should survive snapshot round-trip.");
            Assert.IsTrue(tracker.HasClearedEncounter("island_snapshot", "puzzle_1"),
                "Completed puzzle encounter should survive snapshot round-trip.");
            Assert.AreEqual(75f, tracker.GetRestorationPercent("island_snapshot"), 0.01f,
                "Restoration percent should survive snapshot round-trip.");

            bool duplicateAfterLoad = tracker.RecordEncounterCompletion("island_snapshot", "combat_1", EncounterType.Combat, 0.5f);
            Assert.IsFalse(duplicateAfterLoad, "Reloaded encounter ids should still prevent duplicate completion.");
        }
        finally
        {
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }

        Debug.Log("✓ Tracker snapshot round-trip test passed");
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
