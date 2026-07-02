using System.Collections;
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

    [Header("Transition Settings")]
    [Tooltip("Duration of smooth color transition when restoration changes (seconds).")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Tooltip("Island ID to track. Leave empty to track active progression island.")]
    [SerializeField] private string islandId = "island_lust";

    private IslandRestorationTracker tracker;
    private Coroutine activeTransitionCoroutine;
    private Color currentDisplayedColor;

    private void OnEnable()
    {
        TryFindTracker();
        SubscribeToGameStateManager();
        if (groundRenderer == null)
        {
            FindGroundRenderer();
        }

        if (groundRenderer != null)
        {
            currentDisplayedColor = groundRenderer.material.color;
        }

        RefreshColor();
    }

    private void OnDisable()
    {
        UnsubscribeFromTracker();
        UnsubscribeFromGameStateManager();

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }
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

        Color targetColor = CalculateTargetColor();
        StartColorTransition(targetColor);
    }

    private Color CalculateTargetColor()
    {
        float percent = 0f;
        Color localMinColor = minColor;
        Color localMaxColor = maxColor;

        if (tracker != null)
        {
            string targetIsland = ResolveIslandIdForDisplay();
            percent = tracker.GetRestorationPercent(targetIsland) / 100f;

            IslandConfig islandConfig = IslandThemeRegistry.GetConfig(targetIsland);
            if (islandConfig != null)
            {
                localMinColor = Color.Lerp(Color.black, islandConfig.viceSecondaryColor, 0.58f);
                localMaxColor = Color.Lerp(Color.white, islandConfig.vicePrimaryColor, 0.2f);
            }
        }

        return Color.Lerp(localMinColor, localMaxColor, percent);
    }

    private void StartColorTransition(Color targetColor)
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(TransitionToColor(targetColor));
    }

    private IEnumerator TransitionToColor(Color targetColor)
    {
        if (groundRenderer == null)
        {
            yield break;
        }

        Color startColor = currentDisplayedColor;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            currentDisplayedColor = Color.Lerp(startColor, targetColor, smoothedT);
            groundRenderer.material.color = currentDisplayedColor;
            yield return null;
        }

        currentDisplayedColor = targetColor;
        groundRenderer.material.color = targetColor;
        Debug.Log($"[GroundRestorationVisualizer] Ground color updated to {targetColor} (restoration {GetRestorationPercent():P0})");
        activeTransitionCoroutine = null;
    }

    private float GetRestorationPercent()
    {
        if (tracker == null)
        {
            return 0f;
        }

        string targetIsland = ResolveIslandIdForDisplay();
        return tracker.GetRestorationPercent(targetIsland) / 100f;
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryFindTracker();
            if (tracker != null)
            {
                RefreshColor();
            }
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
