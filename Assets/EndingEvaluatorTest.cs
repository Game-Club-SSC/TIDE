using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class EndingEvaluatorTest : MonoBehaviour
{
    [ContextMenu("Run Ending Evaluator Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Ending Evaluator Tests ===");

        TestEvaluatorReturnsBadEndingWhenLossesHitThreshold();
        TestEvaluatorReturnsBadEndingWhenMinimumRestorationRuleFails();
        TestEvaluatorReturnsGoodEndingWhenAllRulesSatisfied();
        TestEvaluatorHandlesNullGameStateGracefully();
        TestEvaluatorDoesNotFlagBadEndingBelowThreshold();

        Debug.Log("=== All Ending Evaluator Tests Passed ===");
    }

    private void TestEvaluatorReturnsBadEndingWhenLossesHitThreshold()
    {
        Debug.Log("Testing final boss defeat threshold triggers bad ending via EndingEvaluator...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EvaluatorLosses");
            EndingEvaluator evaluator = CreateIsolatedEvaluator(manager);

            string finalIslandId = IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1];
            for (int i = 0; i < BossEncounterGate.DefaultDefeatsForBadEnding; i++)
            {
                manager.RecordFinalBossDefeatAttempt(finalIslandId);
            }

            EndingOutcome outcome = evaluator.EvaluateOutcome(manager);
            Assert.AreEqual(EndingOutcome.BadEnding, outcome,
                "Evaluator should return BadEnding when final boss defeat threshold is met.");

            EndingEvaluatorSnapshot snapshot = evaluator.BuildSnapshot(manager);
            Assert.IsTrue(snapshot.hasFinalBossDefeats,
                "Snapshot should record hasFinalBossDefeats=true once defeats accumulate.");
            Assert.AreEqual(BossEncounterGate.DefaultDefeatsForBadEnding, snapshot.finalBossDefeats,
                "Snapshot should record accumulated defeat count.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Loss-threshold bad ending test passed");
    }

    private void TestEvaluatorReturnsBadEndingWhenMinimumRestorationRuleFails()
    {
        Debug.Log("Testing minimum restoration rule failure returns bad ending...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EvaluatorMinRestoration");
            EndingEvaluator evaluator = CreateIsolatedEvaluator(manager);

            EndingEvaluatorSnapshot snapshot = new EndingEvaluatorSnapshot
            {
                hasFinalBossDefeats = false,
                finalBossDefeats = 0,
                finalBossDefeatThreshold = 4,
                isMinimumRestorationRuleEnabled = true,
                minimumRestorationClearedRatio = 0.2f,
                minimumRestorationThresholdRatio = 0.75f
            };

            EndingOutcome outcome = EndingEvaluator.EvaluateOutcomeFromSnapshot(snapshot);
            Assert.AreEqual(EndingOutcome.BadEnding, outcome,
                "Evaluator should return BadEnding when cleared ratio is below the minimum threshold.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Minimum-restoration rule test passed");
    }

    private void TestEvaluatorReturnsGoodEndingWhenAllRulesSatisfied()
    {
        Debug.Log("Testing all rules satisfied returns good ending...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EvaluatorGood");
            EndingEvaluator evaluator = CreateIsolatedEvaluator(manager);

            EndingEvaluatorSnapshot snapshot = new EndingEvaluatorSnapshot
            {
                hasFinalBossDefeats = false,
                finalBossDefeats = 0,
                finalBossDefeatThreshold = 4,
                isMinimumRestorationRuleEnabled = true,
                minimumRestorationClearedRatio = 1f,
                minimumRestorationThresholdRatio = 0.75f
            };

            EndingOutcome outcome = EndingEvaluator.EvaluateOutcomeFromSnapshot(snapshot);
            Assert.AreEqual(EndingOutcome.GoodEnding, outcome,
                "Evaluator should return GoodEnding when no loss rule and minimum restoration rule passes.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Good-ending all-rules-satisfied test passed");
    }

    private void TestEvaluatorHandlesNullGameStateGracefully()
    {
        Debug.Log("Testing evaluator handles null GameStateManager gracefully...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EvaluatorNullSafe");
            EndingEvaluator evaluator = CreateIsolatedEvaluator(manager);

            EndingEvaluatorSnapshot snapshot = evaluator.BuildSnapshot(null);
            Assert.IsNotNull(snapshot, "Snapshot should be returned even when GameStateManager is null.");
            Assert.AreEqual(0, snapshot.finalBossDefeats, "Null manager should produce empty snapshot.");

            EndingOutcome outcome = evaluator.EvaluateOutcome(null);
            Assert.AreEqual(EndingOutcome.GoodEnding, outcome,
                "Null manager with no recorded defeats should default to GoodEnding.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Null-safe evaluator test passed");
    }

    private void TestEvaluatorDoesNotFlagBadEndingBelowThreshold()
    {
        Debug.Log("Testing partial defeats below threshold keep GoodEnding path...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EvaluatorPartialDefeats");
            EndingEvaluator evaluator = CreateIsolatedEvaluator(manager);

            string finalIslandId = IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1];
            manager.RecordFinalBossDefeatAttempt(finalIslandId);

            EndingEvaluatorSnapshot snapshot = evaluator.BuildSnapshot(manager);
            Assert.AreEqual(1, snapshot.finalBossDefeats, "Single defeat should be reflected in snapshot.");

            EndingOutcome outcome = EndingEvaluator.EvaluateOutcomeFromSnapshot(snapshot);
            Assert.AreEqual(EndingOutcome.GoodEnding, outcome,
                "Below-threshold defeat count should not trip the BadEnding path.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Below-threshold partial defeats test passed");
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        CleanupEnvironment();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        SetPersistentSaveDisabled(manager);

        return manager;
    }

    private static EndingEvaluator CreateIsolatedEvaluator(GameStateManager manager)
    {
        EndingEvaluator evaluator = EndingEvaluator.Instance;
        if (evaluator != null)
        {
            DestroyImmediate(evaluator.gameObject);
            evaluator = null;
        }

        GameObject evaluatorObject = new GameObject("TestEndingEvaluator");
        evaluator = evaluatorObject.AddComponent<EndingEvaluator>();
        evaluator.ConfigureForCurrentSlice(true, EndingEvaluator.DefaultMinimumRestorationThresholdRatio);
        return evaluator;
    }

    private static void SetPersistentSaveDisabled(GameStateManager manager)
    {
        FieldInfo field = typeof(GameStateManager).GetField(
            "enablePersistentSaveData",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(manager, false);
        }
    }

    private static void CleanupEnvironment()
    {
        if (EndingEvaluator.Instance != null)
        {
            DestroyImmediate(EndingEvaluator.Instance.gameObject);
        }

        if (GameStateManager.Instance != null)
        {
            DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        if (IslandProgressionManager.Instance != null)
        {
            DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (DevCheatService.Instance != null)
        {
            DestroyImmediate(DevCheatService.Instance.gameObject);
        }
    }
}
