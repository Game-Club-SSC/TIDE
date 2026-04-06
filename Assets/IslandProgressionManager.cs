using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandProgressionManager : MonoBehaviour
{
    [Serializable]
    public sealed class IslandReturnPositionSnapshot
    {
        public string islandId;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class ProgressionSnapshot
    {
        public string activeIslandId;
        public List<string> unlockedIslandIds = new List<string>();
        public List<IslandReturnPositionSnapshot> returnPositions = new List<IslandReturnPositionSnapshot>();
    }

    public static IslandProgressionManager Instance { get; private set; }

    [Header("Progression")]
    [SerializeField] private string activeIslandId = IslandThemeRegistry.DefaultIslandId;
    [SerializeField] private bool autoAdvanceOnIslandRestored = true;

    public event Action<string> OnActiveIslandChanged;
    public event Action<string> OnIslandUnlocked;

    public string ActiveIslandId => activeIslandId;

    private readonly HashSet<string> unlockedIslandIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector3> islandReturnPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);

    private IslandRestorationTracker tracker;
    private bool isApplyingSnapshot;
    private bool hasInitializedDefaults;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeIslandId = ResolveKnownOrDefault(activeIslandId);
        if (!hasInitializedDefaults)
        {
            InitializeSafeDefaults();
            hasInitializedDefaults = true;
        }
        else
        {
            EnsureStateIntegrity();
        }

        IslandThemeRegistry.SetActiveIslandId(activeIslandId);
        TryBindTracker();
        ReconcileStateFromRestoration();
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryBindTracker();
            if (tracker != null)
            {
                ReconcileStateFromRestoration();
            }
        }
    }

    private void OnDisable()
    {
        UnbindTracker();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetActiveIsland(string islandId)
    {
        if (!TryResolveKnownIslandId(islandId, out string resolved))
        {
            Debug.LogWarning($"[IslandProgressionManager] Cannot set active island to unknown id '{islandId}'.");
            return;
        }

        if (!unlockedIslandIds.Contains(resolved))
        {
            Debug.LogWarning($"[IslandProgressionManager] Cannot set active island to locked id '{resolved}'.");
            return;
        }

        SetActiveIslandInternal(resolved, true);
    }

    public bool TrySetActiveIslandForTravel(string islandId)
    {
        if (!TryResolveKnownIslandId(islandId, out string resolved))
        {
            return false;
        }

        if (!unlockedIslandIds.Contains(resolved))
        {
            return false;
        }

        SetActiveIslandInternal(resolved, true);
        return true;
    }

    public IslandConfig GetActiveIslandConfig()
    {
        return IslandThemeRegistry.GetConfig(ActiveIslandId);
    }

    public string GetNextIslandId()
    {
        return IslandThemeRegistry.GetNextIslandId(ActiveIslandId);
    }

    public bool IsIslandUnlocked(string islandId)
    {
        if (!TryResolveKnownIslandId(islandId, out string resolved))
        {
            return false;
        }

        return unlockedIslandIds.Contains(resolved);
    }

    public bool CanTravelToIsland(string islandId)
    {
        return IsIslandUnlocked(islandId);
    }

    public bool TryUnlockIsland(string islandId)
    {
        if (!TryResolveKnownIslandId(islandId, out string resolved))
        {
            return false;
        }

        return UnlockIslandInternal(resolved, true);
    }

    public string[] GetUnlockedIslandIds()
    {
        EnsureStateIntegrity();

        List<string> ordered = new List<string>();
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (unlockedIslandIds.Contains(islandId))
            {
                ordered.Add(islandId);
            }
        }

        foreach (string islandId in unlockedIslandIds)
        {
            if (!ordered.Contains(islandId))
            {
                ordered.Add(islandId);
            }
        }

        return ordered.ToArray();
    }

    public void RecordIslandReturnPosition(string islandId, Vector3 worldPosition)
    {
        string resolved = ResolveKnownOrDefault(islandId);
        if (string.IsNullOrEmpty(resolved) || !IsFiniteVector(worldPosition))
        {
            return;
        }

        islandReturnPositions[resolved] = worldPosition;
    }

    public bool TryGetIslandReturnPosition(string islandId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        string resolved = ResolveKnownOrDefault(islandId);
        if (string.IsNullOrEmpty(resolved))
        {
            return false;
        }

        if (!islandReturnPositions.TryGetValue(resolved, out worldPosition))
        {
            return false;
        }

        if (!IsFiniteVector(worldPosition))
        {
            islandReturnPositions.Remove(resolved);
            worldPosition = Vector3.zero;
            return false;
        }

        return true;
    }

    public ProgressionSnapshot CaptureSnapshot()
    {
        EnsureStateIntegrity();

        ProgressionSnapshot snapshot = new ProgressionSnapshot
        {
            activeIslandId = activeIslandId
        };

        string[] unlocked = GetUnlockedIslandIds();
        for (int i = 0; i < unlocked.Length; i++)
        {
            snapshot.unlockedIslandIds.Add(unlocked[i]);
        }

        foreach (KeyValuePair<string, Vector3> pair in islandReturnPositions)
        {
            if (!IsFiniteVector(pair.Value))
            {
                continue;
            }

            snapshot.returnPositions.Add(new IslandReturnPositionSnapshot
            {
                islandId = pair.Key,
                x = pair.Value.x,
                y = pair.Value.y,
                z = pair.Value.z
            });
        }

        return snapshot;
    }

    public void ApplySnapshot(ProgressionSnapshot snapshot)
    {
        isApplyingSnapshot = true;

        try
        {
            unlockedIslandIds.Clear();
            islandReturnPositions.Clear();

            string firstIsland = GetFirstAvailableIslandId();
            if (!string.IsNullOrEmpty(firstIsland))
            {
                UnlockIslandInternal(firstIsland, false);
            }

            if (snapshot != null)
            {
                if (snapshot.unlockedIslandIds != null)
                {
                    for (int i = 0; i < snapshot.unlockedIslandIds.Count; i++)
                    {
                        string candidate = snapshot.unlockedIslandIds[i];
                        if (TryResolveKnownIslandId(candidate, out string resolvedUnlock))
                        {
                            UnlockIslandInternal(resolvedUnlock, false);
                        }
                    }
                }

                if (snapshot.returnPositions != null)
                {
                    for (int i = 0; i < snapshot.returnPositions.Count; i++)
                    {
                        IslandReturnPositionSnapshot entry = snapshot.returnPositions[i];
                        if (entry == null || !TryResolveKnownIslandId(entry.islandId, out string resolvedIslandId))
                        {
                            continue;
                        }

                        Vector3 position = new Vector3(entry.x, entry.y, entry.z);
                        if (IsFiniteVector(position))
                        {
                            islandReturnPositions[resolvedIslandId] = position;
                        }
                    }
                }

                activeIslandId = ResolveKnownOrDefault(snapshot.activeIslandId);
            }
            else
            {
                activeIslandId = ResolveKnownOrDefault(activeIslandId);
            }

            ReconcileStateFromRestorationInternal();
            EnsureStateIntegrity();

            IslandThemeRegistry.SetActiveIslandId(activeIslandId);
            OnActiveIslandChanged?.Invoke(activeIslandId);
        }
        finally
        {
            isApplyingSnapshot = false;
        }
    }

    public void ReconcileStateFromRestoration()
    {
        ReconcileStateFromRestorationInternal();
        EnsureStateIntegrity();
    }

    private void InitializeSafeDefaults()
    {
        unlockedIslandIds.Clear();
        islandReturnPositions.Clear();

        string firstIsland = GetFirstAvailableIslandId();
        if (!string.IsNullOrEmpty(firstIsland))
        {
            UnlockIslandInternal(firstIsland, false);
        }

        activeIslandId = ResolveKnownOrDefault(activeIslandId);
        UnlockIslandInternal(activeIslandId, false);
        EnsureStateIntegrity();
    }

    private void EnsureStateIntegrity()
    {
        string firstIsland = GetFirstAvailableIslandId();

        List<string> invalidUnlocks = null;
        foreach (string islandId in unlockedIslandIds)
        {
            if (IslandThemeRegistry.IsKnownIslandId(islandId))
            {
                continue;
            }

            if (invalidUnlocks == null)
            {
                invalidUnlocks = new List<string>();
            }

            invalidUnlocks.Add(islandId);
        }

        if (invalidUnlocks != null)
        {
            for (int i = 0; i < invalidUnlocks.Count; i++)
            {
                unlockedIslandIds.Remove(invalidUnlocks[i]);
            }
        }

        if (!string.IsNullOrEmpty(firstIsland))
        {
            unlockedIslandIds.Add(firstIsland);
        }

        activeIslandId = ResolveKnownOrDefault(activeIslandId);
        if (!string.IsNullOrEmpty(activeIslandId))
        {
            unlockedIslandIds.Add(activeIslandId);
        }
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        string scopedRestored = ResolveKnownOrDefault(restoredIslandId);
        if (string.IsNullOrEmpty(scopedRestored))
        {
            return;
        }

        UnlockIslandInternal(scopedRestored, !isApplyingSnapshot);

        string nextIsland = IslandThemeRegistry.GetNextIslandId(scopedRestored);
        if (!string.IsNullOrEmpty(nextIsland) && !string.Equals(nextIsland, scopedRestored, StringComparison.Ordinal))
        {
            UnlockIslandInternal(nextIsland, !isApplyingSnapshot);
        }

        if (!autoAdvanceOnIslandRestored || isApplyingSnapshot)
        {
            return;
        }

        if (!string.Equals(scopedRestored, ActiveIslandId, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrEmpty(nextIsland)
            && !string.Equals(nextIsland, scopedRestored, StringComparison.Ordinal)
            && unlockedIslandIds.Contains(nextIsland))
        {
            SetActiveIslandInternal(nextIsland, true);
        }
    }

    private void ReconcileStateFromRestorationInternal()
    {
        if (tracker == null)
        {
            return;
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (!IslandThemeRegistry.IsKnownIslandId(islandId) || !tracker.IsIslandRestored(islandId))
            {
                continue;
            }

            UnlockIslandInternal(islandId, !isApplyingSnapshot);

            string nextIsland = IslandThemeRegistry.GetNextIslandId(islandId);
            if (!string.IsNullOrEmpty(nextIsland) && !string.Equals(nextIsland, islandId, StringComparison.Ordinal))
            {
                UnlockIslandInternal(nextIsland, !isApplyingSnapshot);
            }
        }
    }

    private bool UnlockIslandInternal(string resolvedIslandId, bool invokeEvents)
    {
        if (string.IsNullOrEmpty(resolvedIslandId) || !unlockedIslandIds.Add(resolvedIslandId))
        {
            return false;
        }

        Debug.Log($"[IslandProgressionManager] Unlocked island '{resolvedIslandId}'.");
        if (invokeEvents)
        {
            OnIslandUnlocked?.Invoke(resolvedIslandId);
        }

        return true;
    }

    private void SetActiveIslandInternal(string resolvedIslandId, bool invokeEvents)
    {
        if (string.IsNullOrEmpty(resolvedIslandId)
            || string.Equals(activeIslandId, resolvedIslandId, StringComparison.Ordinal))
        {
            return;
        }

        activeIslandId = resolvedIslandId;
        IslandThemeRegistry.SetActiveIslandId(activeIslandId);
        Debug.Log($"[IslandProgressionManager] Active island set to '{activeIslandId}'.");
        if (invokeEvents)
        {
            OnActiveIslandChanged?.Invoke(activeIslandId);
        }
    }

    private void TryBindTracker()
    {
        if (tracker != null)
        {
            return;
        }

        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnIslandRestored += HandleIslandRestored;
        }
    }

    private void UnbindTracker()
    {
        if (tracker != null)
        {
            tracker.OnIslandRestored -= HandleIslandRestored;
            tracker = null;
        }
    }

    private static bool TryResolveKnownIslandId(string islandId, out string resolvedIslandId)
    {
        resolvedIslandId = string.Empty;
        if (string.IsNullOrEmpty(islandId) || !IslandThemeRegistry.IsKnownIslandId(islandId))
        {
            return false;
        }

        resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        return !string.IsNullOrEmpty(resolvedIslandId);
    }

    private static string ResolveKnownOrDefault(string islandId)
    {
        if (!string.IsNullOrEmpty(islandId) && IslandThemeRegistry.IsKnownIslandId(islandId))
        {
            return IslandThemeRegistry.ResolveIslandId(islandId);
        }

        return GetFirstAvailableIslandId();
    }

    private static string GetFirstAvailableIslandId()
    {
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string candidate = progressionOrder[i];
            if (IslandThemeRegistry.IsKnownIslandId(candidate))
            {
                return candidate;
            }
        }

        return IslandThemeRegistry.ResolveIslandId(IslandThemeRegistry.DefaultIslandId);
    }

    private static bool IsFiniteVector(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
