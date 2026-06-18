using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EndingEvaluatorSnapshot
{
    public bool hasFinalBossDefeats;
    public int finalBossDefeats;
    public int finalBossDefeatThreshold;
    public string finalBossDefeatedIslandId;
    public bool isMinimumRestorationRuleEnabled;
    public float minimumRestorationClearedRatio;
    public float minimumRestorationThresholdRatio;
    public List<string> islandsClearedAtThreshold = new List<string>();
    public List<string> islandsProceededAtThreshold = new List<string>();
    public bool onlyRequiresFinalIsland;
    public bool requiresOptionalPreBossContent;
}

public enum EndingOutcome
{
    Unresolved,
    GoodEnding,
    BadEnding
}

[DisallowMultipleComponent]
public class EndingEvaluator : MonoBehaviour
{
    public const float DefaultMinimumRestorationThresholdRatio = 0.75f;

    [Header("Minimum Restoration Rule")]
    [SerializeField] private bool enableMinimumRestorationRule = true;
    [SerializeField, Range(0f, 1f)] private float minimumRestorationThresholdRatio = DefaultMinimumRestorationThresholdRatio;
    [SerializeField] private bool requireFinalIslandForBadEnding = false;
    [SerializeField] private bool requireOptionalPreBossContent = true;

    public static EndingEvaluator Instance { get; private set; }

    public bool EnableMinimumRestorationRule => enableMinimumRestorationRule;
    public float MinimumRestorationThresholdRatio => Mathf.Clamp01(minimumRestorationThresholdRatio);

    public event Action<EndingOutcome, EndingEvaluatorSnapshot> OnEndingEvaluated;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public EndingOutcome EvaluateOutcome(GameStateManager gameState)
    {
        EndingEvaluatorSnapshot snapshot = BuildSnapshot(gameState);
        EndingOutcome outcome = EvaluateOutcomeFromSnapshot(snapshot);
        OnEndingEvaluated?.Invoke(outcome, snapshot);
        return outcome;
    }

    public static EndingOutcome EvaluateOutcomeFromSnapshot(EndingEvaluatorSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return EndingOutcome.Unresolved;
        }

        if (snapshot.hasFinalBossDefeats
            && snapshot.finalBossDefeatThreshold > 0
            && snapshot.finalBossDefeats >= snapshot.finalBossDefeatThreshold)
        {
            return EndingOutcome.BadEnding;
        }

        if (snapshot.isMinimumRestorationRuleEnabled
            && snapshot.minimumRestorationClearedRatio + Mathf.Epsilon < snapshot.minimumRestorationThresholdRatio)
        {
            return EndingOutcome.BadEnding;
        }

        return EndingOutcome.GoodEnding;
    }

    public EndingEvaluatorSnapshot BuildSnapshot(GameStateManager gameState)
    {
        EndingEvaluatorSnapshot snapshot = new EndingEvaluatorSnapshot();
        snapshot.minimumRestorationThresholdRatio = enableMinimumRestorationRule
            ? Mathf.Clamp01(minimumRestorationThresholdRatio)
            : 0f;
        snapshot.isMinimumRestorationRuleEnabled = enableMinimumRestorationRule;
        snapshot.onlyRequiresFinalIsland = requireFinalIslandForBadEnding;
        snapshot.requiresOptionalPreBossContent = requireOptionalPreBossContent;

        if (gameState == null)
        {
            return snapshot;
        }

        snapshot.finalBossDefeats = gameState.GetFinalBossDefeatCount(GetFinalIslandIdOrEmpty(gameState));
        snapshot.finalBossDefeatThreshold = Mathf.Max(1, gameState.GetConfiguredFinalBossDefeatThreshold());
        snapshot.hasFinalBossDefeats = snapshot.finalBossDefeats > 0;

        GameStateManager.MinimumRestorationBadEndingRuleMode ruleMode = gameState.MinimumRestorationBadEndingRule;
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        HashSet<string> thresholdOnlyBossVictoryIslandIds = ToIdHash(gameState.GetThresholdOnlyBossVictoryIslandIds());
        HashSet<string> thresholdOnlyProceedIslandIds = ToIdHash(gameState.GetThresholdOnlyProceedIslandIds());

        int requiredIslandCount = 0;
        int satisfiedIslandCount = 0;
        float thresholdClearedRatio = 0f;

        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            bool isFinalIsland = i == progressionOrder.Count - 1;
            if (requireFinalIslandForBadEnding && !isFinalIsland)
            {
                continue;
            }

            if (requireOptionalPreBossContent && !HasOptionalPreBossRestorationAvailable(islandId))
            {
                continue;
            }

            requiredIslandCount++;
            if (IsIslandThresholdCleared(ruleMode, islandId, thresholdOnlyBossVictoryIslandIds, thresholdOnlyProceedIslandIds))
            {
                satisfiedIslandCount++;
            }

            float ratioForIsland = gameState.GetIslandRestorationPercent(islandId) / 100f;
            if (ratioForIsland >= snapshot.minimumRestorationThresholdRatio)
            {
                thresholdClearedRatio += 1f;
            }
        }

        if (requiredIslandCount > 0)
        {
            snapshot.minimumRestorationClearedRatio = Mathf.Clamp01(satisfiedIslandCount / (float)requiredIslandCount);
        }

        snapshot.islandsClearedAtThreshold.AddRange(thresholdOnlyBossVictoryIslandIds);
        snapshot.islandsProceededAtThreshold.AddRange(thresholdOnlyProceedIslandIds);
        snapshot.finalBossDefeatedIslandId = GetFinalIslandIdOrEmpty(gameState);
        return snapshot;
    }

    private static bool IsIslandThresholdCleared(
        GameStateManager.MinimumRestorationBadEndingRuleMode ruleMode,
        string islandId,
        HashSet<string> thresholdOnlyBossVictoryIslandIds,
        HashSet<string> thresholdOnlyProceedIslandIds)
    {
        switch (ruleMode)
        {
            case GameStateManager.MinimumRestorationBadEndingRuleMode.BossDefeatedAtThreshold:
                return thresholdOnlyBossVictoryIslandIds.Contains(islandId);
            case GameStateManager.MinimumRestorationBadEndingRuleMode.ProceededAtThreshold:
                return thresholdOnlyProceedIslandIds.Contains(islandId);
            default:
                return thresholdOnlyBossVictoryIslandIds.Contains(islandId);
        }
    }

    private static HashSet<string> ToIdHash(IEnumerable<string> source)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (source == null)
        {
            return result;
        }

        foreach (string id in source)
        {
            string resolved = IslandThemeRegistry.ResolveIslandId(id);
            if (!string.IsNullOrEmpty(resolved))
            {
                result.Add(resolved);
            }
        }

        return result;
    }

    private static bool HasOptionalPreBossRestorationAvailable(string islandId)
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
        if (config == null || config.encounters == null)
        {
            return false;
        }

        float nonBossContribution = 0f;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter == null || encounter.isBossEncounter || IsBossEncounterId(encounter.encounterId))
            {
                continue;
            }

            nonBossContribution += Mathf.Max(0f, encounter.restorationValue);
        }

        return nonBossContribution > 0.01f;
    }

    private static bool IsBossEncounterId(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId)
            && encounterId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetFinalIslandIdOrEmpty(GameStateManager gameState)
    {
        if (gameState == null)
        {
            return string.Empty;
        }

        return gameState.GetFinalProgressionIslandIdForDebug();
    }

    public void ConfigureForCurrentSlice(bool enableMinimumRestorationRule, float minimumRestorationThresholdRatio)
    {
        this.enableMinimumRestorationRule = enableMinimumRestorationRule;
        this.minimumRestorationThresholdRatio = Mathf.Clamp01(minimumRestorationThresholdRatio);
    }

    public void SetRequireFinalIslandForBadEnding(bool requireFinalIsland)
    {
        requireFinalIslandForBadEnding = requireFinalIsland;
    }
}
