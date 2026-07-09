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
            Assert.AreEqual(0.6f, state.CombatContribution, 0.001f, "Combat contribution should be 0.6.");
            Assert.AreEqual(0.6f, state.TotalContribution, 0.001f, "Total should be 0.6.");

            state.RecordCompletion("combat_2", EncounterType.Combat, 0.6f);
            Assert.AreEqual(1.0f, state.CombatContribution, 0.001f, "Combat contribution should be capped at 1.0.");
            Assert.IsTrue(state.TotalContribution <= 1.0f, "Total contribution should be capped at 1.0.");

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
            Assert.IsTrue(state.IsIslandRestored, "Total > 1.0 should count as restored.");

            Debug.Log("[IslandRestorationStateEdgeTest] TestEpsilonToleranceOnIsIslandRestored passed.");
        }
        finally
        {
            state.Reset();
        }
    }
}
