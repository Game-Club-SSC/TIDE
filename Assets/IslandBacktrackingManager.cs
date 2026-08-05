using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages the ability to return to previous islands for additional
/// exploration, ancient text collection, and NPC conversations. The v2 GDD Chapter 5
/// describes traveling back to see if there's more ancient text, asking tribe leaders
/// about the text, and eventually resorting to the enchanted moura for information.
/// </summary>
[DisallowMultipleComponent]
public class IslandBacktrackingManager : MonoBehaviour
{
    public static IslandBacktrackingManager Instance { get; private set; }

    [Serializable]
    public class BacktrackingUnlock
    {
        [Tooltip("Which island, when completed, triggers this unlock")]
        public string unlockIslandId;
        [Tooltip("Which previous islands become accessible for backtracking")]
        public string[] unlockedIslands;
        [Tooltip("Narrative reason the party can return")]
        [TextArea(2, 4)]
        public string narrativeReason;
    }

    [Header("Backtracking Rules")]
    [SerializeField] private BacktrackingUnlock[] backtrackingUnlocks = GetDefaultUnlocks();

    private readonly HashSet<string> backtrackingAccessibleIslands = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> processedUnlockIslands = new HashSet<string>(StringComparer.Ordinal);
    private IslandRestorationTracker tracker;

    public event Action<string> OnBacktrackingIslandAccessible;
    public event Action<string> OnBacktrackingNarrativeAvailable;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        OnBacktrackingNarrativeAvailable += HandleNarrativeAvailable;
        TryBindTracker();
        ReconcileBacktrackingState();
    }

    private void OnDisable()
    {
        OnBacktrackingNarrativeAvailable -= HandleNarrativeAvailable;
        UnbindTracker();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryBindTracker();
        }
    }

    /// <summary>
    /// Returns true if the given island is accessible for backtracking travel.
    /// An island is accessible if it has been previously completed (restored)
    /// AND the backtracking unlock for it has been triggered.
    /// </summary>
    public bool CanVisitIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        string resolvedId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(resolvedId))
        {
            return false;
        }

        // The central hub is always accessible for return travel; it is not part
        // of the corruption progression or backtracking rule set.
        if (IslandThemeRegistry.IsHubIslandId(resolvedId))
        {
            return true;
        }

        // Always allow visiting the current active island
        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager != null
            && string.Equals(progressionManager.ActiveIslandId, resolvedId, StringComparison.Ordinal))
        {
            return true;
        }

        // Allow visiting the next island in progression (forward travel)
        if (progressionManager != null)
        {
            string nextIsland = IslandThemeRegistry.GetNextIslandId(progressionManager.ActiveIslandId);
            if (string.Equals(nextIsland, resolvedId, StringComparison.Ordinal)
                && progressionManager.IsIslandUnlocked(resolvedId))
            {
                return true;
            }
        }

        // For backtracking, check both that it was restored AND the unlock has triggered
        if (!backtrackingAccessibleIslands.Contains(resolvedId))
        {
            return false;
        }

        // Also check that the island is still unlocked in progression
        if (progressionManager != null && !progressionManager.IsIslandUnlocked(resolvedId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns all islands the player can currently visit (both forward progression and backtracking).
    /// </summary>
    public IReadOnlyCollection<string> GetAccessibleIslands()
    {
        HashSet<string> accessible = new HashSet<string>(StringComparer.Ordinal);

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager != null)
        {
            string[] unlocked = progressionManager.GetUnlockedIslandIds();
            for (int i = 0; i < unlocked.Length; i++)
            {
                if (CanVisitIsland(unlocked[i]))
                {
                    accessible.Add(unlocked[i]);
                }
            }
        }

        // Include all backtracking-accessible islands
        foreach (string islandId in backtrackingAccessibleIslands)
        {
            if (CanVisitIsland(islandId))
            {
                accessible.Add(islandId);
            }
        }

        return accessible;
    }

    /// <summary>
    /// Manually triggers backtracking unlocks for a completed island.
    /// Usually called automatically when an island is restored.
    /// </summary>
    public void UnlockBacktracking(string completedIslandId)
    {
        if (string.IsNullOrEmpty(completedIslandId))
        {
            return;
        }

        string resolvedId = IslandThemeRegistry.ResolveIslandId(completedIslandId);
        if (string.IsNullOrEmpty(resolvedId))
        {
            return;
        }

        if (processedUnlockIslands.Contains(resolvedId))
        {
            return;
        }

        processedUnlockIslands.Add(resolvedId);

        for (int i = 0; i < backtrackingUnlocks.Length; i++)
        {
            BacktrackingUnlock unlock = backtrackingUnlocks[i];
            if (unlock == null || string.IsNullOrEmpty(unlock.unlockIslandId))
            {
                continue;
            }

            if (!string.Equals(unlock.unlockIslandId, resolvedId, StringComparison.Ordinal))
            {
                continue;
            }

            if (unlock.unlockedIslands == null)
            {
                continue;
            }

            for (int j = 0; j < unlock.unlockedIslands.Length; j++)
            {
                string islandToUnlock = unlock.unlockedIslands[j];
                if (string.IsNullOrEmpty(islandToUnlock))
                {
                    continue;
                }

                string resolved = IslandThemeRegistry.ResolveIslandId(islandToUnlock);
                if (!string.IsNullOrEmpty(resolved) && backtrackingAccessibleIslands.Add(resolved))
                {
                    Debug.Log($"[IslandBacktrackingManager] Backtracking unlocked for '{resolved}' via completion of '{resolvedId}'.");
                    OnBacktrackingIslandAccessible?.Invoke(resolved);
                }
            }

            if (!string.IsNullOrEmpty(unlock.narrativeReason))
            {
                OnBacktrackingNarrativeAvailable?.Invoke(unlock.narrativeReason);
            }
        }
    }

    /// <summary>
    /// Returns the most recent narrative reason for backtracking from the given island.
    /// Returns null if no backtracking reason is available.
    /// </summary>
    public string GetBacktrackingNarrative(string currentIslandId)
    {
        if (string.IsNullOrEmpty(currentIslandId))
        {
            return null;
        }

        string resolvedId = IslandThemeRegistry.ResolveIslandId(currentIslandId);
        if (string.IsNullOrEmpty(resolvedId))
        {
            return null;
        }

        // Find the most relevant narrative reason based on the most recently completed island
        string bestNarrative = null;
        int highestIndex = -1;

        for (int i = 0; i < backtrackingUnlocks.Length; i++)
        {
            BacktrackingUnlock unlock = backtrackingUnlocks[i];
            if (unlock == null || string.IsNullOrEmpty(unlock.unlockIslandId))
            {
                continue;
            }

            if (!processedUnlockIslands.Contains(unlock.unlockIslandId))
            {
                continue;
            }

            if (unlock.unlockedIslands == null)
            {
                continue;
            }

            bool appliesToThisIsland = false;
            for (int j = 0; j < unlock.unlockedIslands.Length; j++)
            {
                if (string.Equals(unlock.unlockedIslands[j], resolvedId, System.StringComparison.Ordinal))
                {
                    appliesToThisIsland = true;
                    break;
                }
            }

            if (!appliesToThisIsland)
            {
                continue;
            }

            int unlockIndex = GetProgressionIndex(unlock.unlockIslandId);
            if (unlockIndex > highestIndex)
            {
                highestIndex = unlockIndex;
                bestNarrative = unlock.narrativeReason;
            }
        }

        return bestNarrative;
    }

    /// <summary>
    /// Returns true if the given island has been made accessible via backtracking
    /// (not through forward progression).
    /// </summary>
    public bool IsBacktrackingUnlocked(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        string resolvedId = IslandThemeRegistry.ResolveIslandId(islandId);
        return !string.IsNullOrEmpty(resolvedId) && backtrackingAccessibleIslands.Contains(resolvedId);
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

    private void HandleIslandRestored(string restoredIslandId)
    {
        UnlockBacktracking(restoredIslandId);
    }

    private void ReconcileBacktrackingState()
    {
        IslandRestorationTracker restorationTracker = tracker != null ? tracker : IslandRestorationTracker.Instance;
        if (restorationTracker == null)
        {
            return;
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (restorationTracker.IsIslandRestored(islandId))
            {
                UnlockBacktracking(islandId);
            }
        }
    }

    private int GetProgressionIndex(string islandId)
    {
        string resolvedId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(resolvedId))
        {
            return -1;
        }

        IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < order.Count; i++)
        {
            if (string.Equals(order[i], resolvedId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleNarrativeAvailable(string narrativeReason)
    {
        Debug.Log($"[IslandBacktrackingManager] Narrative available: {narrativeReason}");
    }

    private static BacktrackingUnlock[] GetDefaultUnlocks()
    {
        // GDD V2 progression order: lust(0), greed(1), desire(2), anger(3), envy(4), ego(5)
        //
        // After completing desire (index 2): unlock greed(1), lust(0)
        //   "The texts mention something we missed on the earlier islands..."
        // After completing envy (index 4): unlock anger(3), desire(2), greed(1), lust(0)
        //   "The ancient texts grow clearer. We must return and speak with the tribe leaders again."

        return new[]
        {
            new BacktrackingUnlock
            {
                unlockIslandId = "island_desire",
                unlockedIslands = new[] { "island_greed", "island_lust" },
                narrativeReason = "The texts mention something we missed on the earlier islands. The tribe leaders might know more than they let on."
            },
            new BacktrackingUnlock
            {
                unlockIslandId = "island_anger",
                unlockedIslands = new[] { "island_desire", "island_greed", "island_lust" },
                narrativeReason = "Anger's fury has subsided. The path back to the earlier islands is clear once more."
            },
            new BacktrackingUnlock
            {
                unlockIslandId = "island_envy",
                unlockedIslands = new[] { "island_anger", "island_desire", "island_greed", "island_lust" },
                narrativeReason = "The ancient texts grow clearer with each island restored. We must return and speak with the tribe leaders again — they have been withholding the truth."
            },
            new BacktrackingUnlock
            {
                unlockIslandId = "island_ego",
                unlockedIslands = new[] { "island_envy", "island_anger", "island_desire", "island_greed", "island_lust" },
                narrativeReason = "With ego overcome, every island is within reach. The final truths await where we first began."
            }
        };
    }
}
