using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandRestorationTracker : MonoBehaviour
{
    public static IslandRestorationTracker Instance { get; private set; }

    public event Action<string, float> OnRestorationChanged;
    public event Action<string> OnIslandRestored;

    private readonly Dictionary<string, IslandRestorationState> islandStates =
        new Dictionary<string, IslandRestorationState>();

    private const string DefaultIslandId = "default";
    private const string LegacyEncounterId = "__legacy_complete_encounter__";

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

    private void OnApplicationQuit()
    {
        Instance = null;
    }

    public void CompleteEncounter(float contribution)
    {
        RecordEncounterCompletion(DefaultIslandId, LegacyEncounterId, EncounterType.Combat, contribution);
    }

    public void RecordEncounterCompletion(string islandId, string encounterId, EncounterType type, float value)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
        }

        if (string.IsNullOrEmpty(encounterId))
        {
            Debug.LogWarning($"[IslandRestorationTracker] Encounter id is required for island '{islandId}'. Skipping completion.");
            return;
        }

        if (value <= 0f)
        {
            return;
        }

        IslandRestorationState state = GetOrCreateState(islandId);

        if (state.HasCompleted(encounterId))
        {
            Debug.LogWarning($"[IslandRestorationTracker] Encounter '{encounterId}' already completed on island '{islandId}'. Skipping.");
            return;
        }

        float previous = state.TotalContribution;
        state.RecordCompletion(encounterId, type, value);

        Debug.Log(
            $"[IslandRestorationTracker] {type} encounter '{encounterId}' complete on '{islandId}'. " +
            $"Restoration: {state.RestorationPercent:F1}% " +
            $"(Combat: {state.CombatContribution * 100:F0}%, Puzzle: {state.PuzzleContribution * 100:F0}%)");

        OnRestorationChanged?.Invoke(islandId, state.TotalContribution);

        if (state.IsIslandRestored && previous < 1f)
        {
            Debug.Log($"[IslandRestorationTracker] Island '{islandId}' fully restored!");
            OnIslandRestored?.Invoke(islandId);
        }
    }

    public void ResetTracker()
    {
        ResetIsland(DefaultIslandId);
    }

    public void ResetIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
        }

        IslandRestorationState state = GetOrCreateState(islandId);
        state.Reset();
        OnRestorationChanged?.Invoke(islandId, 0f);
    }

    public float GetRestorationPercent(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return 0f;
        }

        return state.RestorationPercent;
    }

    public IslandRestorationState GetRestorationState(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
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
        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
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

        if (string.IsNullOrEmpty(islandId))
        {
            islandId = DefaultIslandId;
        }

        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            return false;
        }

        return state.HasCompleted(encounterId);
    }

    private IslandRestorationState GetOrCreateState(string islandId)
    {
        if (!islandStates.TryGetValue(islandId, out IslandRestorationState state))
        {
            state = new IslandRestorationState(islandId);
            islandStates[islandId] = state;
        }

        return state;
    }
}
