using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class StoryProgressionTest : MonoBehaviour
{
    private static readonly FieldInfo EnablePersistentSaveDataField =
        typeof(GameStateManager).GetField("enablePersistentSaveData", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo TrackBossVictoryThresholdProgressMethod =
        typeof(GameStateManager).GetMethod("TrackBossVictoryThresholdProgress", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo ResolveFateFinaleAfterCombatMethod =
        typeof(GameStateManager).GetMethod("ResolveFateFinaleAfterCombat", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo NotifyBossDefeatAttemptMethod =
        typeof(GameStateManager).GetMethod("NotifyBossDefeatAttempt", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo CaptureStoryProgressionSaveDataMethod =
        typeof(GameStateManager).GetMethod("CaptureStoryProgressionSaveData", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo ApplyStoryProgressionSaveDataMethod =
        typeof(GameStateManager).GetMethod("ApplyStoryProgressionSaveData", BindingFlags.Instance | BindingFlags.NonPublic);

    [ContextMenu("Run Story Progression Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Story Progression Tests ===");

        TestStoryActProgressesWithIslandTravel();
        TestFinalBossDefeatThresholdTriggersBadEnding();
        TestConfiguredFinalBossThresholdIsRespected();
        TestMinimumRestorationRuleModesAreSwitchable();
        TestStoryRuleModePersistsThroughSaveDataRoundTrip();
        TestMissingStorySaveDataResetsRuleModeToDefault();
        TestProceededRuleRequiresForwardIslandTravel();
        TestEndingEvaluatorGoodEndingPath();
        TestGoodEndingPathTriggersOnFinalBossDefeat();
        TestBadEndingPathTriggersOnFinalBossDefeat();

        Debug.Log("=== All Story Progression Tests Passed ===");
    }

    private void TestStoryActProgressesWithIslandTravel()
    {
        Debug.Log("Testing story act progression from island travel...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_StoryAct");

            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();
            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_desire");
            manager.RefreshStoryProgressionForDebug();

            Assert.AreEqual(GameStateManager.StoryAct.ActII, manager.CurrentStoryAct,
                "Traveling into the middle islands should advance story state to Act II.");

            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_envy");
            manager.RefreshStoryProgressionForDebug();

            Assert.AreEqual(GameStateManager.StoryAct.ActIII, manager.CurrentStoryAct,
                "Traveling to the final island should advance story state to Act III.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Story act progression test passed");
    }

    private void TestFinalBossDefeatThresholdTriggersBadEnding()
    {
        Debug.Log("Testing repeated final boss defeats trigger bad ending...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_FinalBossLosses");
            string finalIslandId = IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1];

            for (int i = 0; i < BossEncounterGate.DefaultDefeatsForBadEnding; i++)
            {
                manager.RecordFinalBossDefeatAttempt(finalIslandId);
            }

            Assert.AreEqual(BossEncounterGate.DefaultDefeatsForBadEnding, manager.GetFinalBossDefeatCount(finalIslandId),
                "Final boss defeat count should accumulate in game state.");
            Assert.AreEqual(GameStateManager.EndingBranch.Bad, manager.ResolvedEndingBranch,
                "Crossing the final boss defeat threshold should lock in the bad ending.");
            Assert.IsTrue(manager.IsEndingTriggered,
                "Bad ending should be marked as triggered after repeated final boss defeats.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Final boss defeat threshold test passed");
    }

    private void TestConfiguredFinalBossThresholdIsRespected()
    {
        Debug.Log("Testing configured final boss defeat threshold is respected...");

        GameObject gateObject = null;

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_CustomThreshold");
            string finalIslandId = IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1];

            gateObject = new GameObject("TestFinalBossGate_CustomThreshold");
            BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();
            SetPrivateField(gate, "treatAsFinalBoss", true);
            SetPrivateField(gate, "islandId", finalIslandId);
            SetPrivateField(gate, "defeatsForBadEnding", 2);

            manager.SetBossDefeatTrackingContext(finalIslandId, true);
            NotifyBossDefeatAttempt(manager);
            Assert.AreEqual(GameStateManager.EndingBranch.None, manager.ResolvedEndingBranch,
                "Custom final boss threshold should not trigger after the first defeat.");

            NotifyBossDefeatAttempt(manager);
            Assert.AreEqual(2, manager.GetFinalBossDefeatCount(finalIslandId),
                "Configured threshold path should record both final boss defeats.");
            Assert.AreEqual(GameStateManager.EndingBranch.Bad, manager.ResolvedEndingBranch,
                "Configured final boss threshold should drive bad-ending resolution.");
        }
        finally
        {
            if (gateObject != null)
            {
                DestroyImmediate(gateObject);
            }

            CleanupEnvironment();
        }

        Debug.Log("  Configured final boss threshold test passed");
    }

    private void TestMinimumRestorationRuleModesAreSwitchable()
    {
        Debug.Log("Testing switchable minimum-restoration ending rule modes...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_RuleModes");
            manager.ResetStoryProgressionForDebug();

            IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
            for (int i = 0; i < progressionOrder.Count; i++)
            {
                TrackBossVictoryAtThreshold(manager, progressionOrder[i], IslandRestorationTracker.DefaultBossUnlockThresholdPercent);
            }

            manager.SetMinimumRestorationBadEndingRuleModeForDebug(GameStateManager.MinimumRestorationBadEndingRuleMode.OptionalContentOnly);
            ResolveEndingAfterFinalBossVictory(manager);
            Assert.AreEqual(GameStateManager.EndingBranch.Good, manager.ResolvedEndingBranch,
                "The default optional-content rule should not auto-fail the current slice content.");

            manager.ResetStoryProgressionForDebug();
            for (int i = 0; i < progressionOrder.Count; i++)
            {
                TrackBossVictoryAtThreshold(manager, progressionOrder[i], IslandRestorationTracker.DefaultBossUnlockThresholdPercent);
            }

            manager.SetMinimumRestorationBadEndingRuleModeForDebug(GameStateManager.MinimumRestorationBadEndingRuleMode.BossDefeatedAtThreshold);
            ResolveEndingAfterFinalBossVictory(manager);
            Assert.AreEqual(GameStateManager.EndingBranch.Bad, manager.ResolvedEndingBranch,
                "Strict boss-threshold mode should treat threshold-only clears as the bad ending path.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Switchable minimum-restoration rule test passed");
    }

    private void TestStoryRuleModePersistsThroughSaveDataRoundTrip()
    {
        Debug.Log("Testing story rule mode save-data round-trip...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_RuleModePersist");
            manager.SetMinimumRestorationBadEndingRuleModeForDebug(GameStateManager.MinimumRestorationBadEndingRuleMode.ProceededAtThreshold);

            object saveData = CaptureStoryProgressionSaveData(manager);
            manager.ResetStoryProgressionForDebug();
            ApplyStoryProgressionSaveData(manager, saveData);

            Assert.AreEqual(GameStateManager.MinimumRestorationBadEndingRuleMode.ProceededAtThreshold,
                manager.MinimumRestorationBadEndingRule,
                "Story rule mode should survive the story save-data round-trip.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Story rule mode round-trip test passed");
    }

    private void TestMissingStorySaveDataResetsRuleModeToDefault()
    {
        Debug.Log("Testing missing story save data resets rule mode...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_MissingStoryData");
            manager.SetMinimumRestorationBadEndingRuleModeForDebug(GameStateManager.MinimumRestorationBadEndingRuleMode.ProceededAtThreshold);

            ApplyStoryProgressionSaveData(manager, null);

            Assert.AreEqual(GameStateManager.MinimumRestorationBadEndingRuleMode.OptionalContentOnly,
                manager.MinimumRestorationBadEndingRule,
                "Applying missing story save data should restore the default rule mode.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Missing story save data reset test passed");
    }

    private void TestEndingEvaluatorGoodEndingPath()
    {
        Debug.Log("Testing EndingEvaluator reports GoodEnding on the good path...");

        EndingEvaluator evaluator = null;
        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EndingEvalGood");
            if (EndingEvaluator.Instance != null)
            {
                DestroyImmediate(EndingEvaluator.Instance.gameObject);
            }

            GameObject evaluatorObject = new GameObject("TestEndingEvaluator_Story");
            evaluator = evaluatorObject.AddComponent<EndingEvaluator>();
            evaluator.ConfigureForCurrentSlice(true, EndingEvaluator.DefaultMinimumRestorationThresholdRatio);

            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();
            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug(
                IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1]);

            IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
            for (int i = 0; i < progressionOrder.Count; i++)
            {
                TrackBossVictoryAtThreshold(manager, progressionOrder[i], IslandRestorationTracker.DefaultBossUnlockThresholdPercent);
            }

            EndingEvaluatorSnapshot snapshot = evaluator.BuildSnapshot(manager);
            Assert.IsFalse(snapshot.hasFinalBossDefeats,
                "Good path should not record any final boss defeats.");
            Assert.GreaterOrEqual(snapshot.minimumRestorationClearedRatio, snapshot.minimumRestorationThresholdRatio,
                "Tracking boss victories at threshold for every island should clear the minimum restoration rule.");

            EndingOutcome outcome = EndingEvaluator.EvaluateOutcomeFromSnapshot(snapshot);
            Assert.AreEqual(EndingOutcome.GoodEnding, outcome,
                "Good-ending path with all islands threshold-cleared should evaluate to GoodEnding.");
        }
        finally
        {
            if (evaluator != null)
            {
                DestroyImmediate(evaluator.gameObject);
            }

            CleanupEnvironment();
        }

        Debug.Log("  EndingEvaluator good-ending path test passed");
    }

    private void TestGoodEndingPathTriggersOnFinalBossDefeat()
    {
        Debug.Log("Testing OnFinalBossDefeated fires when good-ending path resolves...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_GoodEndingOnFinalBoss");
            manager.ForceEndingBranchForDebug(GameStateManager.EndingBranch.Good);
            manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActIII);

            manager.OnFinalBossDefeated();

            AudioManager audioManager = AudioManager.Instance;
            Assert.IsNotNull(audioManager, "AudioManager should be bootstrapped for the ending BGM hook.");
        }
        finally
        {
            if (AudioManager.Instance != null)
            {
                DestroyImmediate(AudioManager.Instance.gameObject);
            }

            CleanupEnvironment();
        }

        Debug.Log("  Good-ending OnFinalBossDefeated test passed");
    }

    private void TestBadEndingPathTriggersOnFinalBossDefeat()
    {
        Debug.Log("Testing OnFinalBossDefeated fires when bad-ending path resolves...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_BadEndingOnFinalBoss");
            manager.ForceEndingBranchForDebug(GameStateManager.EndingBranch.Bad);

            manager.OnFinalBossDefeated();

            Assert.IsTrue(manager.IsEndingTriggered,
                "Bad-ending path should leave the ending flagged as triggered.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Bad-ending OnFinalBossDefeated test passed");
    }

    private void TestProceededRuleRequiresForwardIslandTravel()
    {
        Debug.Log("Testing proceeded rule requires forward travel...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_ProceedRule");
            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();

            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_desire");
            TrackBossVictoryAtThreshold(manager, "island_desire", IslandRestorationTracker.DefaultBossUnlockThresholdPercent);

            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_anger");

            CollectionAssert.DoesNotContain(manager.GetThresholdOnlyProceedIslandIds(), "island_desire",
                "Backtracking should not count as proceeding for the threshold-only rule.");
        }
        finally
        {
            CleanupEnvironment();
        }

        Debug.Log("  Proceeded rule directionality test passed");
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        CleanupEnvironment();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        DisablePersistentSave(manager);

        Assert.AreSame(manager, GameStateManager.Instance,
            "GameStateManager singleton should reference the isolated story test manager instance.");
        Assert.IsNotNull(IslandProgressionManager.Instance,
            "GameStateManager should bootstrap an IslandProgressionManager for story tests.");
        return manager;
    }

    private static void DisablePersistentSave(GameStateManager manager)
    {
        Assert.IsNotNull(EnablePersistentSaveDataField,
            "GameStateManager persistent save field was not found. Update the story test helper.");
        EnablePersistentSaveDataField.SetValue(manager, false);
    }

    private static void TrackBossVictoryAtThreshold(GameStateManager manager, string islandId, float restorationPercent)
    {
        Assert.IsNotNull(TrackBossVictoryThresholdProgressMethod,
            "TrackBossVictoryThresholdProgress helper was not found on GameStateManager.");
        TrackBossVictoryThresholdProgressMethod.Invoke(manager, new object[] { islandId, restorationPercent });
    }

    private static void ResolveEndingAfterFinalBossVictory(GameStateManager manager)
    {
        Assert.IsNotNull(ResolveFateFinaleAfterCombatMethod,
            "ResolveFateFinaleAfterCombat helper was not found on GameStateManager.");
        ResolveFateFinaleAfterCombatMethod.Invoke(manager, new object[] { true });
    }

    private static void NotifyBossDefeatAttempt(GameStateManager manager)
    {
        Assert.IsNotNull(NotifyBossDefeatAttemptMethod,
            "NotifyBossDefeatAttempt helper was not found on GameStateManager.");
        NotifyBossDefeatAttemptMethod.Invoke(manager, null);
    }

    private static object CaptureStoryProgressionSaveData(GameStateManager manager)
    {
        Assert.IsNotNull(CaptureStoryProgressionSaveDataMethod,
            "CaptureStoryProgressionSaveData helper was not found on GameStateManager.");
        return CaptureStoryProgressionSaveDataMethod.Invoke(manager, null);
    }

    private static void ApplyStoryProgressionSaveData(GameStateManager manager, object saveData)
    {
        Assert.IsNotNull(ApplyStoryProgressionSaveDataMethod,
            "ApplyStoryProgressionSaveData helper was not found on GameStateManager.");
        ApplyStoryProgressionSaveDataMethod.Invoke(manager, new[] { saveData });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void CleanupEnvironment()
    {
        DestroyImmediateIfPresent(GameStateManager.Instance);
        DestroyImmediateIfPresent(IslandRestorationTracker.Instance);
        DestroyImmediateIfPresent(IslandProgressionManager.Instance);
        DestroyImmediateIfPresent(HeroProgressionManager.Instance);
        DestroyImmediateIfPresent(DevCheatService.Instance);

        DevModeController[] devModeControllers = FindObjectsByType<DevModeController>(FindObjectsSortMode.None);
        for (int i = 0; i < devModeControllers.Length; i++)
        {
            if (devModeControllers[i] != null)
            {
                DestroyImmediate(devModeControllers[i].gameObject);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DevMenuUI[] devMenus = FindObjectsByType<DevMenuUI>(FindObjectsSortMode.None);
        for (int i = 0; i < devMenus.Length; i++)
        {
            if (devMenus[i] != null)
            {
                DestroyImmediate(devMenus[i].gameObject);
            }
        }
#endif

        BossEncounterGate[] bossGates = FindObjectsByType<BossEncounterGate>(FindObjectsSortMode.None);
        for (int i = 0; i < bossGates.Length; i++)
        {
            if (bossGates[i] != null)
            {
                DestroyImmediate(bossGates[i].gameObject);
            }
        }
    }

    private static void DestroyImmediateIfPresent(MonoBehaviour behaviour)
    {
        if (behaviour != null)
        {
            DestroyImmediate(behaviour.gameObject);
        }
    }
}
