using System;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandProgressionManager : MonoBehaviour
{
    public static IslandProgressionManager Instance { get; private set; }

    [Header("Progression")]
    [SerializeField] private string activeIslandId = "island_lust";
    [SerializeField] private bool autoAdvanceOnIslandRestored = true;

    public event Action<string> OnActiveIslandChanged;

    public string ActiveIslandId => IslandThemeRegistry.ResolveIslandId(activeIslandId);

    private IslandRestorationTracker tracker;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        activeIslandId = IslandThemeRegistry.ResolveIslandId(activeIslandId);
        IslandThemeRegistry.SetActiveIslandId(activeIslandId);

        TryBindTracker();
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryBindTracker();
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
        string resolved = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.Equals(activeIslandId, resolved, StringComparison.Ordinal))
        {
            return;
        }

        activeIslandId = resolved;
        IslandThemeRegistry.SetActiveIslandId(activeIslandId);
        Debug.Log($"[IslandProgressionManager] Active island set to '{activeIslandId}'.");
        OnActiveIslandChanged?.Invoke(activeIslandId);
    }

    public IslandConfig GetActiveIslandConfig()
    {
        return IslandThemeRegistry.GetConfig(ActiveIslandId);
    }

    public string GetNextIslandId()
    {
        return IslandThemeRegistry.GetNextIslandId(ActiveIslandId);
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        if (!autoAdvanceOnIslandRestored)
        {
            return;
        }

        string scopedRestored = IslandThemeRegistry.ResolveIslandId(restoredIslandId);
        if (!string.Equals(scopedRestored, ActiveIslandId, StringComparison.Ordinal))
        {
            return;
        }

        string nextIsland = IslandThemeRegistry.GetNextIslandId(scopedRestored);
        if (!string.Equals(nextIsland, scopedRestored, StringComparison.Ordinal))
        {
            SetActiveIsland(nextIsland);
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
}
