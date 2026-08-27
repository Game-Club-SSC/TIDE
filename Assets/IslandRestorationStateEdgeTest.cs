using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandRestorationStateEdgeTest : MonoBehaviour
{
    [ContextMenu("Run All Island Restoration State Edge Tests")]
    public void RunAllTests()
    {
        TestContributionCappingCombatAndPuzzleIndividually();
        TestEpsilonToleranceOnIsIslandRestored();
        TestInvalidContributionsAreIgnored();
        TestMalformedSnapshotIsSanitizedAndMigrated();
        Debug.Log("=== All Island Restoration State Edge Tests Passed ===");
    }

    [ContextMenu("Test Contribution Capping")]
    public void TestContributionCappingCombatAndPuzzleIndividually()
    {
        Debug.Log("[IslandRestorationStateEdgeTest] Testing contribution capping...");
        IslandRestorationState state = new IslandRestorationState("island_test");
        try
        {
            state.RecordCompletion("combat_1", EncounterType.Combat, 0.6f);
            Assert.AreEqual(0.5f, state.CombatContribution, 0.001f,
                "Ordinary combat contribution should be capped at the GDD maximum of 50%.");
            Assert.AreEqual(0.5f, state.TotalContribution, 0.001f,
                "Ordinary combat alone should restore at most 50% of an island.");

            state.RecordCompletion("combat_2", EncounterType.Combat, 0.6f);
            Assert.AreEqual(0.5f, state.CombatContribution, 0.001f,
                "Additional ordinary combat should not exceed the 50% cap.");
            Assert.AreEqual(0.5f, state.TotalContribution, 0.001f,
                "Additional ordinary combat should not complete an island without Tide restoration.");

            state.RecordCompletion("boss_1", EncounterType.Combat, 0.25f, true);
            Assert.AreEqual(0.25f, state.BossContribution, 0.001f,
                "Boss restoration should be tracked separately from ordinary combat.");
            Assert.AreEqual(0.75f, state.TotalContribution, 0.001f,
                "Boss restoration should not be discarded by the ordinary-combat cap.");

            state.Reset();

            state.RecordCompletion("puzzle_1", EncounterType.Puzzle, 0.7f);
            Assert.AreEqual(0.7f, state.PuzzleContribution, 0.001f, "Puzzle contribution should be 0.7.");

            state.RecordCompletion("puzzle_2", EncounterType.Puzzle, 0.7f);
            Assert.AreEqual(1.0f, state.PuzzleContribution, 0.001f, "Puzzle contribution should be capped at 1.0.");
            Assert.IsTrue(state.TotalContribution <= 1.0f, "Total contribution should be capped at 1.0.");

            Debug.Log("[IslandRestorationStateEdgeTest] TestContributionCappingCombatAndPuzzleIndividually passed.");
        }
        finally
        {
            state.Reset();
        }
    }

    [ContextMenu("Test Epsilon Tolerance On IsIslandRestored")]
    public void TestEpsilonToleranceOnIsIslandRestored()
    {
        Debug.Log("[IslandRestorationStateEdgeTest] Testing epsilon tolerance on IsIslandRestored...");
        IslandRestorationState state = new IslandRestorationState("island_epsilon");
        try
        {
            state.RecordCompletion("combat_1", EncounterType.Combat, 0.5f);
            state.RecordCompletion("puzzle_1", EncounterType.Puzzle, 0.499f);
            Assert.AreEqual(0.999f, state.TotalContribution, 0.001f, "Total should be 0.999.");
            Assert.IsTrue(state.IsIslandRestored, "0.999 should count as restored (>= 0.999f threshold).");

            state.Reset();
            state.RecordCompletion("combat_c", EncounterType.Combat, 0.5f);
            state.RecordCompletion("puzzle_c", EncounterType.Puzzle, 0.498f);
            Assert.AreEqual(0.998f, state.TotalContribution, 0.001f, "Total should be 0.998.");
            Assert.IsFalse(state.IsIslandRestored, "0.998 should NOT count as restored (< 0.999f threshold).");

            state.Reset();
            state.RecordCompletion("combat_a", EncounterType.Combat, 0.5f);
            state.RecordCompletion("puzzle_a", EncounterType.Puzzle, 0.5f);
            Assert.AreEqual(1.0f, state.TotalContribution, 0.001f, "Total should be 1.0.");
            Assert.IsTrue(state.IsIslandRestored, "1.0 should count as restored.");

            state.Reset();
            state.RecordCompletion("combat_b", EncounterType.Combat, 0.6f);
            state.RecordCompletion("puzzle_b", EncounterType.Puzzle, 0.5f);
            Assert.IsTrue(state.IsIslandRestored, "Capped combat plus puzzle restoration should count as restored.");

            Debug.Log("[IslandRestorationStateEdgeTest] TestEpsilonToleranceOnIsIslandRestored passed.");
        }
        finally
        {
            state.Reset();
        }
    }

    [ContextMenu("Test Invalid Contributions")]
    public void TestInvalidContributionsAreIgnored()
    {
        IslandRestorationState state = new IslandRestorationState("island_invalid");

        state.RecordCompletion("negative", EncounterType.Combat, -0.25f);
        state.RecordCompletion("nan", EncounterType.Puzzle, float.NaN);
        state.RecordCompletion("infinity", EncounterType.Combat, float.PositiveInfinity, true);

        Assert.AreEqual(0f, state.TotalContribution, 0.001f,
            "Invalid contributions should not alter restoration.");
        Assert.AreEqual(0, state.CompletedEncounterIds.Count,
            "Invalid contributions should not consume encounter completion ids.");
    }

    [ContextMenu("Test Malformed Snapshot Sanitization")]
    public void TestMalformedSnapshotIsSanitizedAndMigrated()
    {
        IslandRestorationState state = new IslandRestorationState("island_snapshot");
        IslandRestorationStateSnapshot snapshot = new IslandRestorationStateSnapshot
        {
            islandId = "island_snapshot",
            combatContribution = 0.8f,
            puzzleContribution = float.PositiveInfinity,
            bossContribution = 0.1f,
            combatEncountersCompleted = -3,
            puzzleEncountersCompleted = -2
        };

        state.ApplySnapshot(snapshot);

        Assert.AreEqual(0.5f, state.CombatContribution, 0.001f,
            "Loaded ordinary combat should be clamped to the 50% GDD cap.");
        Assert.AreEqual(0.4f, state.BossContribution, 0.001f,
            "Legacy over-cap combat should migrate into the boss bucket without losing progress.");
        Assert.AreEqual(0f, state.PuzzleContribution, 0.001f,
            "Non-finite puzzle contribution should be discarded.");
        Assert.AreEqual(0.9f, state.TotalContribution, 0.001f,
            "Snapshot totals should be recomputed from sanitized category values.");
        Assert.AreEqual(0, state.CombatEncountersCompleted,
            "Negative encounter counts should clamp to zero.");
        Assert.AreEqual(0, state.PuzzleEncountersCompleted,
            "Negative encounter counts should clamp to zero.");
    }
}
