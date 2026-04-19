using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class DevCheatService : MonoBehaviour
{
    public static DevCheatService Instance { get; private set; }

    public bool GodModeInvincible { get; set; }
    public bool GodModeOneHitKill { get; set; }
    public bool GodModeInfiniteResources { get; set; }
    public bool ShowDebugOverlay { get; set; } = true;
    public bool SuppressStoryProgressionSideEffects { get; private set; }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyContinuousCheats()
    {
        BattleManager battle = FindFirstObjectByType<BattleManager>();
        if (battle == null)
        {
            return;
        }

        if (GodModeInvincible)
        {
            IReadOnlyList<CombatUnit> allies = battle.AllyUnits;
            for (int i = 0; i < allies.Count; i++)
            {
                CombatUnit ally = allies[i];
                if (ally == null)
                {
                    continue;
                }

                ally.HP = ally.MaxHP;
                ally.MP = ally.MaxMP;
            }
        }

        if (GodModeOneHitKill)
        {
            IReadOnlyList<CombatUnit> enemies = battle.EnemyUnits;
            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnit enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.Defense = 0;
                enemy.HP = Mathf.Min(enemy.HP, 1);
            }
        }

        if (GodModeInfiniteResources)
        {
            IReadOnlyList<CombatUnit> allies = battle.AllyUnits;
            for (int i = 0; i < allies.Count; i++)
            {
                CombatUnit ally = allies[i];
                if (ally == null)
                {
                    continue;
                }

                ally.MP = ally.MaxMP;
            }

            battle.Momentum.ShiftTowardPlayer(5f);
        }
    }

    public void FullResetAllState()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.ClearPersistentWorldStateForDebug(true);
            gsm.ResetRuntimeWorldStateForDebug();
        }

        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.ResetAllIslandsForDebug();
        }

        IslandProgressionManager progression = IslandProgressionManager.Instance;
        if (progression != null)
        {
            progression.ResetProgressionForDebug();
        }

        HeroProgressionManager heroProgression = HeroProgressionManager.Instance;
        if (heroProgression != null)
        {
            heroProgression.ResetProgressionForDebug();
        }

        if (gsm != null)
        {
            gsm.HandleIslandTravelFlowReset();
            gsm.SaveWorldState();
        }
    }

    public void ResetEncounterAndFightState()
    {
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.ResetAllIslandsForDebug();
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.ClearPersistentWorldStateForDebug(true);
            gsm.ResetRuntimeWorldStateForDebug();
            gsm.SaveWorldState();
        }
    }

    public void CycleMinimumRestorationBadEndingRuleMode()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        int nextValue = ((int)gsm.MinimumRestorationBadEndingRule + 1)
            % System.Enum.GetValues(typeof(GameStateManager.MinimumRestorationBadEndingRuleMode)).Length;
        gsm.SetMinimumRestorationBadEndingRuleModeForDebug((GameStateManager.MinimumRestorationBadEndingRuleMode)nextValue);
    }

    public void ForceStoryAct(GameStateManager.StoryAct act)
    {
        GameStateManager.Instance?.ForceStoryActForDebug(act);
    }

    public void ForceEndingBranch(GameStateManager.EndingBranch branch)
    {
        GameStateManager.Instance?.ForceEndingBranchForDebug(branch);
    }

    public void ResetStoryProgression()
    {
        GameStateManager.Instance?.ResetStoryProgressionForDebug();
    }

    public void RecordFinalBossDefeatAttempt()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        if (!TryGetFinalIslandId(out string finalIslandId))
        {
            Debug.LogWarning("[DevCheatService] Could not record final boss defeat attempt because progression order is empty.");
            return;
        }

        gsm.RecordFinalBossDefeatAttemptAndQueueEvent(finalIslandId, gsm.GetConfiguredFinalBossDefeatThreshold(finalIslandId));
    }

    public void ResetFinalBossDefeatAttempts()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        if (!TryGetFinalIslandId(out string finalIslandId))
        {
            Debug.LogWarning("[DevCheatService] Could not reset final boss defeat attempts because progression order is empty.");
            return;
        }

        gsm.SetFinalBossDefeatCount(finalIslandId, 0);
        gsm.ForceEndingBranchForDebug(GameStateManager.EndingBranch.None);
    }

    public void UnlockAllIslands()
    {
        IslandProgressionManager progression = IslandProgressionManager.Instance;
        if (progression == null)
        {
            return;
        }

        progression.UnlockAllIslandsForDebug();
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }
    }

    public void SetActiveIsland(string islandId)
    {
        IslandProgressionManager progression = IslandProgressionManager.Instance;
        if (progression == null)
        {
            return;
        }

        SuppressStoryProgressionSideEffects = true;
        bool changedIsland = false;
        try
        {
            changedIsland = progression.ForceSetActiveIslandForDebug(islandId);
        }
        finally
        {
            SuppressStoryProgressionSideEffects = false;
        }

        if (!changedIsland)
        {
            return;
        }

        TeleportToActiveIslandSpawn();
    }

    public void TeleportToActiveIslandSpawn()
    {
        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return;
        }

        string activeIslandId = IslandThemeRegistry.GetActiveIslandId();
        Vector3 destination = player.transform.position;
        if (IslandProgressionManager.Instance != null
            && IslandProgressionManager.Instance.TryGetIslandReturnPosition(activeIslandId, out Vector3 cached))
        {
            destination = cached;
        }
        else
        {
            IslandBoatInteractable boat = FindFirstObjectByType<IslandBoatInteractable>();
            if (boat != null && boat.TryGetSpawnPositionForIsland(activeIslandId, out Vector3 boatSpawn))
            {
                destination = boatSpawn;
            }
        }

        player.transform.position = destination;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void MaxEverything()
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        PartyManager party = PartyManager.Instance;
        if (progression != null && party != null)
        {
            HeroData[] active = party.GetActiveParty();
            HeroData[] reserve = party.GetReserveParty();
            MaxHeroes(active, progression);
            MaxHeroes(reserve, progression);
            progression.SetCurrency(999999);
            progression.GrantCosmeticXp(999999);
        }

        UnlockAllIslands();
        IReadOnlyList<string> islandIds = IslandThemeRegistry.ProgressionOrder;
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            for (int i = 0; i < islandIds.Count; i++)
            {
                tracker.SetIslandRestorationPercentForDebug(islandIds[i], 100f);
            }
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }
    }

    public void SetActiveIslandRestoration(float percent)
    {
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker == null)
        {
            return;
        }

        tracker.SetIslandRestorationPercentForDebug(IslandThemeRegistry.GetActiveIslandId(), percent);
    }

    public string BuildDebugSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            builder.AppendLine($"GameState: {gsm.currentState}");
            builder.AppendLine($"Transitioning: {gsm.IsTransitioning}");
            builder.AppendLine($"StoryAct: {gsm.CurrentStoryAct} (Highest: {gsm.HighestStoryActReached})");
            builder.AppendLine($"EndingBranch: {gsm.ResolvedEndingBranch} Triggered:{gsm.IsEndingTriggered}");
            builder.AppendLine($"75RuleMode: {gsm.MinimumRestorationBadEndingRule}");
        }

        string activeIsland = IslandThemeRegistry.GetActiveIslandId();
        builder.AppendLine($"ActiveIsland: {activeIsland}");

        if (IslandProgressionManager.Instance != null)
        {
            string[] unlocked = IslandProgressionManager.Instance.GetUnlockedIslandIds();
            builder.AppendLine($"UnlockedIslands: {string.Join(", ", unlocked)}");
        }

        if (gsm != null)
        {
            if (TryGetFinalIslandId(out string finalIslandId))
            {
                builder.AppendLine($"FinalBossDefeats[{finalIslandId}]: {gsm.GetFinalBossDefeatCount(finalIslandId)}");
            }
            else
            {
                builder.AppendLine("FinalBossDefeats: unavailable (progression order empty)");
            }

            builder.AppendLine($"ThresholdBossWins: {string.Join(", ", gsm.GetThresholdOnlyBossVictoryIslandIds())}");
            builder.AppendLine($"ThresholdProceeds: {string.Join(", ", gsm.GetThresholdOnlyProceedIslandIds())}");
        }

        if (IslandRestorationTracker.Instance != null)
        {
            IReadOnlyList<string> ids = IslandThemeRegistry.ProgressionOrder;
            for (int i = 0; i < ids.Count; i++)
            {
                string islandId = ids[i];
                float pct = IslandRestorationTracker.Instance.GetRestorationPercent(islandId);
                builder.AppendLine($"{islandId}: {pct:F1}%");
            }
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            builder.AppendLine($"PlayerPos: {player.transform.position}");
        }

        IslandFlowController flow = FindFirstObjectByType<IslandFlowController>();
        if (flow != null)
        {
            builder.AppendLine($"FlowActive: {flow.IsActive}");
            builder.AppendLine($"FlowEncounterIndex: {flow.CurrentEncounterIndex}");
        }

        BattleManager battle = FindFirstObjectByType<BattleManager>();
        if (battle != null)
        {
            builder.AppendLine($"BattlePhase: {battle.CurrentPhase}");
            builder.AppendLine($"Cheats Invincible:{GodModeInvincible} OneHit:{GodModeOneHitKill} Infinite:{GodModeInfiniteResources}");
        }

        return builder.ToString();
    }

    private static void MaxHeroes(HeroData[] heroes, HeroProgressionManager progression)
    {
        if (heroes == null || progression == null)
        {
            return;
        }

        for (int i = 0; i < heroes.Length; i++)
        {
            HeroData hero = heroes[i];
            if (hero == null)
            {
                continue;
            }

            progression.MaxOutHeroForDebug(hero.heroId);
            progression.MaxOutGearForDebug(hero.heroId);
        }
    }

    private static bool TryGetFinalIslandId(out string finalIslandId)
    {
        finalIslandId = string.Empty;
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        if (progressionOrder == null || progressionOrder.Count == 0)
        {
            return false;
        }

        finalIslandId = IslandThemeRegistry.ResolveIslandId(progressionOrder[progressionOrder.Count - 1]);
        return !string.IsNullOrEmpty(finalIslandId);
    }
}
