using UnityEngine;

public class GroundRestorationVisualizer : MonoBehaviour
{
    [Tooltip("Optional reference to the ground renderer. If null, will try to find by name 'Ground'.")]
    [SerializeField] private Renderer groundRenderer;

    [Header("Color Gradient")]
    [Tooltip("Color when restoration is 0% (Excess Evil - Black corruption).")]
    [SerializeField] private Color minColor = Color.black;
    [Tooltip("Color when restoration is 100% (Excess Good - White distortion).")]
    [SerializeField] private Color maxColor = Color.white;

    [Tooltip("Island ID to track. Leave empty to track active progression island.")]
    [SerializeField] private string islandId = "island_lust";

    private IslandRestorationTracker tracker;

    private void OnEnable()
    {
        TryFindTracker();
        SubscribeToGameStateManager();
        if (groundRenderer == null)
        {
            FindGroundRenderer();
        }
        RefreshColor();
    }

    private void OnDisable()
    {
        UnsubscribeFromTracker();
        UnsubscribeFromGameStateManager();
    }

    private void SubscribeToGameStateManager()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompleted;
            GameStateManager.Instance.OnPuzzleCompleted += HandlePuzzleCompleted;
        }
    }

    private void UnsubscribeFromGameStateManager()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompleted;
        }
    }

    private void HandlePuzzleCompleted()
    {
        if (groundRenderer == null)
        {
            return;
        }

        RefreshColor();
    }

    private void TryFindTracker()
    {
        if (tracker != null)
        {
            return;
        }

        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnRestorationChanged += HandleRestorationChanged;
            tracker.OnIslandRestored += HandleIslandRestored;
        }
    }

    private void UnsubscribeFromTracker()
    {
        if (tracker != null)
        {
            tracker.OnRestorationChanged -= HandleRestorationChanged;
            tracker.OnIslandRestored -= HandleIslandRestored;
        }

        tracker = null;
    }

    private void FindGroundRenderer()
    {
        GameObject groundObject = GameObject.Find("Ground");
        if (groundObject != null)
        {
            groundRenderer = groundObject.GetComponent<Renderer>();
        }

        if (groundRenderer == null)
        {
            Debug.LogWarning($"[GroundRestorationVisualizer] Could not find Ground renderer.", this);
        }
    }

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        if (changedIslandId != ResolveIslandIdForDisplay())
        {
            return;
        }

        RefreshColor();
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        if (restoredIslandId != ResolveIslandIdForDisplay())
        {
            return;
        }

        RefreshColor();
    }

    private void RefreshColor()
    {
        if (groundRenderer == null)
        {
            return;
        }

        float percent = 0f;
        if (tracker != null)
        {
            string targetIsland = ResolveIslandIdForDisplay();
            percent = tracker.GetRestorationPercent(targetIsland) / 100f;

            IslandConfig islandConfig = IslandThemeRegistry.GetConfig(targetIsland);
            if (islandConfig != null)
            {
                minColor = Color.Lerp(Color.black, islandConfig.viceSecondaryColor, 0.58f);
                maxColor = Color.Lerp(Color.white, islandConfig.vicePrimaryColor, 0.2f);
            }
        }

        Color targetColor = Color.Lerp(minColor, maxColor, percent);
        groundRenderer.material.color = targetColor;
        Debug.Log($"[GroundRestorationVisualizer] Ground color updated to {targetColor} (restoration {percent:P0})");
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryFindTracker();
        }
    }

    private string ResolveIslandIdForDisplay()
    {
        if (!string.IsNullOrEmpty(islandId))
        {
            return IslandThemeRegistry.ResolveIslandId(islandId);
        }

        return IslandThemeRegistry.GetActiveIslandId();
    }
}
