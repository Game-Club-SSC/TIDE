using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandRestorationTracker : MonoBehaviour
{
    public const float DefaultBossUnlockThresholdPercent = 75f;

    [Serializable]
    public sealed class TrackerSnapshot
    {
        public List<IslandRestorationStateSnapshot> islands = new List<IslandRestorationStateSnapshot>();
    }

    public static IslandRestorationTracker Instance { get; private set; }

    public event Action<string, float> OnRestorationChanged;
    public event Action<string> OnIslandRestored;

    private readonly Dictionary<string, IslandRestorationState> islandStates =
        new Dictionary<string, IslandRestorationState>();

    private const string DefaultIslandId = "island_lust";
    private const string LegacyEncounterId = "__legacy_complete_encounter__";

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            DestroyDuplicateComponent();
            return;
        }

        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        ClearSingletonInstance();
    }

    private void OnApplicationQuit()
    {
        ClearSingletonInstance();
    }

    private void ClearSingletonInstance()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void DestroyDuplicateComponent()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
            return;
        }

        DestroyImmediate(this);
    }

    public void CompleteEncounter(float contribution)
    {
        RecordEncounterCompletion(DefaultIslandId, LegacyEncounterId, EncounterType.Combat, contribution);
    }

    public bool RecordEncounterCompletion(string islandId, string encounterId, EncounterType type, float value)
    {
        islandId = ResolveIslandId(islandId);

        if (string.IsNullOrEmpty(islandId))
        {
            Debug.LogWarning("[IslandRestorationTracker] Cannot resolve island id. Skipping completion.");
            return false;
        }

        if (string.IsNullOrEmpty(encounterId))
        {
            Debug.LogWarning($"[IslandRestorationTracker] Encounter id is required for island '{islandId}'. Skipping completion.");
            return false;
        }

        if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
        {
            return false;
        }

        IslandRestorationState state = GetOrCreateState(islandId);

        if (state.HasCompleted(encounterId))
        {
            Debug.LogWarning($"[IslandRestorationTracker] Encounter '{encounterId}' already completed on island '{islandId}'. Skipping.");
            return false;
        }

        float previous = state.TotalContribution;
        bool isBossEncounter = IsConfiguredBossEncounter(islandId, encounterId);
        state.RecordCompletion(encounterId, type, value, isBossEncounter);

        Debug.Log(
            $"[IslandRestorationTracker] {(isBossEncounter ? "Boss" : type.ToString())} encounter '{encounterId}' complete on '{islandId}'. " +
            $"Restoration: {state.RestorationPercent:F1}% " +
            $"(Combat: {state.CombatContribution * 100:F0}%, Puzzle: {state.PuzzleContribution * 100:F0}%, " +
            $"Boss: {state.BossContribution * 100:F0}%)");

        if (!suppressEvents)
        {
            OnRestorationChanged?.Invoke(islandId, state.RestorationPercent);

            if (state.IsIslandRestored && previous < 1f)
            {
                Debug.Log($"[IslandRestorationTracker] Island '{islandId}' fully restored!");
                OnIslandRestored?.Invoke(islandId);
            }
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }

        return true;
    }

    public void ResetTracker()
    {
        ResetIsland(DefaultIslandId);
    }

    public void ResetIsland(string islandId)
    {
        islandId = ResolveIslandId(islandId);

        if (string.IsNullOrEmpty(islandId))
        {
            Debug.LogWarning("[IslandRestorationTracker] Cannot resolve island id. Skipping reset.");
            return;
        }

        IslandRestorationState state = GetOrCreateState(islandId);
        state.Reset();

        if (!suppressEvents)
        {
            OnRestorationChanged?.Invoke(islandId, 0f);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }
    }

    public float GetRestorationPercent(string islandId)
    {
        islandId = ResolveIslandId(islandId);

        if (string.IsNullOrEmpty(islandId))
        {
            return 0f;
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return 0f;
        }

        return state.RestorationPercent;
    }

    public IslandRestorationState GetRestorationState(string islandId)
    {
        islandId = ResolveIslandId(islandId);

        // Return an empty state (matches previous behavior for non-existent islands)
        // rather than null, so 6 internal callers don't need null-guard changes.
        if (string.IsNullOrEmpty(islandId))
        {
            return new IslandRestorationState(string.Empty);
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return new IslandRestorationState(islandId);
        }

        return state;
    }

    public bool IsRestorationAtOrAbove(string islandId, float thresholdPercent)
    {
        return GetRestorationPercent(islandId) >= thresholdPercent;
    }

    public bool IsIslandRestored(string islandId)
    {
        islandId = ResolveIslandId(islandId);

        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return false;
        }

        return state.IsIslandRestored;
    }

    public bool HasClearedEncounter(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        foreach (IslandRestorationState state in islandStates.Values)
        {
            if (state.HasCompleted(encounterId))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasClearedEncounter(string islandId, string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        islandId = ResolveIslandId(islandId);

        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return false;
        }

        return state.HasCompleted(encounterId);
    }

    public TrackerSnapshot CaptureSnapshot()
    {
        TrackerSnapshot snapshot = new TrackerSnapshot();
        foreach (KeyValuePair<string, IslandRestorationState> pair in islandStates)
        {
            if (pair.Value != null)
            {
                snapshot.islands.Add(pair.Value.CaptureSnapshot());
            }
        }

        return snapshot;
    }

    private bool suppressEvents;

    public void SetSuppressEvents(bool suppress)
    {
        suppressEvents = suppress;
    }

    public void ApplySnapshot(TrackerSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        islandStates.Clear();

        if (snapshot.islands != null)
        {
            for (int i = 0; i < snapshot.islands.Count; i++)
            {
                IslandRestorationStateSnapshot stateSnapshot = snapshot.islands[i];
                if (stateSnapshot == null)
                {
                    continue;
                }

                string scopedIslandId = ResolveIslandId(stateSnapshot.islandId);
                if (string.IsNullOrEmpty(scopedIslandId))
                {
                    Debug.LogWarning($"[IslandRestorationTracker] Skipping snapshot island with unresolved id '{stateSnapshot.islandId}'.");
                    continue;
                }

                stateSnapshot = MigrateConfiguredBossContribution(scopedIslandId, stateSnapshot);
                IslandRestorationState state = new IslandRestorationState(scopedIslandId);
                state.ApplySnapshot(stateSnapshot);
                islandStates[scopedIslandId] = state;
            }
        }

        // Emit refresh events so that gates, HUD, and restoration visuals
        // pick up the restored state. Preserve the caller's suppression flag.
        bool previousSuppression = suppressEvents;
        suppressEvents = false;
        foreach (KeyValuePair<string, IslandRestorationState> pair in islandStates)
        {
            OnRestorationChanged?.Invoke(pair.Key, pair.Value.RestorationPercent);
            if (pair.Value.IsIslandRestored)
            {
                OnIslandRestored?.Invoke(pair.Key);
            }
        }
        suppressEvents = previousSuppression;
    }

    private IslandRestorationState GetOrCreateState(string islandId)
    {
        islandId = ResolveIslandId(islandId);

        // All callers early-guard on null islandId, so this state is unreachable.
        // Return empty state defensively rather than null to keep call sites simple.
        if (string.IsNullOrEmpty(islandId))
        {
            return new IslandRestorationState(string.Empty);
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            state = new IslandRestorationState(islandId);
            islandStates[islandId] = state;
        }

        return state;
    }

    private static string ResolveIslandId(string islandId)
    {
        return IslandThemeRegistry.ResolveIslandId(islandId);
    }

    private static bool IsConfiguredBossEncounter(string islandId, string encounterId)
    {
        if (string.IsNullOrEmpty(islandId) || string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
        if (config == null || config.encounters == null)
        {
            return false;
        }

        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter != null
                && encounter.isBossEncounter
                && string.Equals(encounter.encounterId, encounterId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IslandRestorationStateSnapshot MigrateConfiguredBossContribution(
        string islandId,
        IslandRestorationStateSnapshot snapshot)
    {
        if (snapshot == null
            || SanitizeContribution(snapshot.bossContribution) > 0f
            || snapshot.completedEncounterIds == null
            || snapshot.completedEncounterIds.Count == 0)
        {
            return snapshot;
        }

        IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
        if (config == null || config.encounters == null)
        {
            return snapshot;
        }

        HashSet<string> completedEncounterIds = new HashSet<string>(
            snapshot.completedEncounterIds,
            StringComparer.Ordinal);
        float configuredCombatContribution = 0f;
        float configuredBossContribution = 0f;

        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter == null
                || encounter.type != EncounterType.Combat
                || string.IsNullOrEmpty(encounter.encounterId)
                || !completedEncounterIds.Contains(encounter.encounterId))
            {
                continue;
            }

            float configuredValue = SanitizeContribution(encounter.restorationValue);
            if (encounter.isBossEncounter)
            {
                configuredBossContribution += configuredValue;
            }
            else
            {
                configuredCombatContribution += configuredValue;
            }
        }

        configuredBossContribution = Mathf.Clamp01(configuredBossContribution);
        if (configuredBossContribution <= 0f)
        {
            return snapshot;
        }

        float legacyCombatContribution = SanitizeContribution(snapshot.combatContribution);
        float configuredCombinedContribution = configuredCombatContribution + configuredBossContribution;
        float unconfiguredCombatContribution = Mathf.Max(
            0f,
            legacyCombatContribution - configuredCombinedContribution);

        IslandRestorationStateSnapshot migratedSnapshot = new IslandRestorationStateSnapshot
        {
            islandId = islandId,
            combatContribution = Mathf.Min(0.5f, configuredCombatContribution + unconfiguredCombatContribution),
            puzzleContribution = snapshot.puzzleContribution,
            bossContribution = configuredBossContribution,
            totalContribution = snapshot.totalContribution,
            combatEncountersCompleted = snapshot.combatEncountersCompleted,
            puzzleEncountersCompleted = snapshot.puzzleEncountersCompleted,
            completedEncounterIds = new List<string>(snapshot.completedEncounterIds)
        };

        Debug.Log(
            $"[IslandRestorationTracker] Migrated legacy boss restoration progress for '{islandId}' " +
            $"(Combat: {migratedSnapshot.combatContribution * 100f:F0}%, " +
            $"Boss: {migratedSnapshot.bossContribution * 100f:F0}%).");

        return migratedSnapshot;
    }

    private static float SanitizeContribution(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? 0f
            : Mathf.Max(0f, value);
    }

    public void ResetAllIslandsForDebug()
    {
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            ResetIsland(progressionOrder[i]);
        }
    }

    public void SetIslandRestorationPercentForDebug(string islandId, float percent)
    {
        string scopedIslandId = ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            Debug.LogWarning("[IslandRestorationTracker] Cannot resolve island id for debug set.");
            return;
        }
        IslandRestorationState state = GetOrCreateState(scopedIslandId);
        state.Reset();

        float clampedContribution = float.IsNaN(percent) || float.IsInfinity(percent)
            ? 0f
            : Mathf.Clamp01(percent / 100f);
        if (clampedContribution > 0f)
        {
            IslandRestorationStateSnapshot snapshot = state.CaptureSnapshot();
            snapshot.combatContribution = Mathf.Min(0.5f, clampedContribution);
            snapshot.puzzleContribution = Mathf.Max(0f, clampedContribution - snapshot.combatContribution);
            snapshot.bossContribution = 0f;
            snapshot.totalContribution = clampedContribution;
            snapshot.combatEncountersCompleted = snapshot.combatContribution > 0f ? 1 : 0;
            snapshot.puzzleEncountersCompleted = snapshot.puzzleContribution > 0f ? 1 : 0;
            snapshot.completedEncounterIds.Clear();
            if (clampedContribution >= 1f)
            {
                snapshot.completedEncounterIds.Add($"__debug_full_restore__::{scopedIslandId}");
            }

            state.ApplySnapshot(snapshot);
        }

        if (!suppressEvents)
        {
            OnRestorationChanged?.Invoke(scopedIslandId, state.RestorationPercent);
            if (state.IsIslandRestored)
            {
                OnIslandRestored?.Invoke(scopedIslandId);
            }
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }
    }
}
