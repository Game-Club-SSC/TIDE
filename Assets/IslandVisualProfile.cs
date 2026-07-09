using System;
using UnityEngine;

// ---------------------------------------------------------------------------
// Tile color mapping — maps a tide value (1-10) to a per-island color.
// ---------------------------------------------------------------------------
[Serializable]
public class TideColorEntry
{
    [Range(1, 10)]
    public int tideValue;
    public Color color;
}

// ---------------------------------------------------------------------------
// Corruption gradient — defines how ground color shifts with restoration %.
// 0 %  = excess evil  (dark / corrupted)
// 50 % = balanced (neutral)
// 100 % = excess good  (bright / purified)
// ---------------------------------------------------------------------------
[Serializable]
public class CorruptionGradient
{
    [Tooltip("Color at 0% restoration (excess evil).")]
    public Color evilColor = Color.black;

    [Tooltip("Color at 50% restoration (balanced).")]
    public Color neutralColor = Color.gray;

    [Tooltip("Color at 100% restoration (excess good).")]
    public Color goodColor = Color.white;

    public Color Evaluate(float restorationPercent)
    {
        float t = Mathf.Clamp01(restorationPercent);
        if (t < 0.5f)
        {
            return Color.Lerp(evilColor, neutralColor, t * 2f);
        }
        return Color.Lerp(neutralColor, goodColor, (t - 0.5f) * 2f);
    }
}

// ---------------------------------------------------------------------------
// IslandVisualProfile — per-island visual configuration for environment art.
// ---------------------------------------------------------------------------
[CreateAssetMenu(fileName = "IslandVisualProfile", menuName = "TIDE/Island Visual Profile")]
public class IslandVisualProfile : ScriptableObject
{
    public string islandId = "island_lust";

    [Header("Ground")]
    [Tooltip("Gradient applied to ground tiles based on restoration percentage.")]
    public CorruptionGradient groundGradient;

    [Tooltip("Base ground tint overlaid on top of the gradient.")]
    public Color groundTint = Color.white;

    [Header("Walls")]
    public Color wallColor = new Color(0.35f, 0.35f, 0.35f);
    public Color wallHighlightColor = new Color(0.55f, 0.55f, 0.55f);

    [Header("Water")]
    public Color waterShallowColor = new Color(0.3f, 0.6f, 0.85f);
    public Color waterDeepColor = new Color(0.1f, 0.2f, 0.45f);

    [Header("Corruption Overlay")]
    [Tooltip("Color applied on top of tiles when the island is highly corrupted.")]
    public Color corruptionOverlayColor = new Color(0.4f, 0f, 0.6f);
    [Range(0f, 1f)]
    public float corruptionOverlayMaxOpacity = 0.45f;

    [Header("Tile Tide Colors")]
    [Tooltip("Per-tide-value color overrides. If empty, TideTile defaults are used.")]
    public TideColorEntry[] tideColors = Array.Empty<TideColorEntry>();

    [Header("Textures")]
    public Texture2D groundTexture;
    public Texture2D wallTexture;
    public Texture2D waterTexture;

    /// <summary>
    /// Returns the color for a given tide value, or null if no override is defined.
    /// </summary>
    public Color? GetTileColorForTide(int tideValue)
    {
        if (tideColors == null) return null;
        for (int i = 0; i < tideColors.Length; i++)
        {
            if (tideColors[i].tideValue == tideValue)
            {
                return tideColors[i].color;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the corruption overlay color at the given opacity.
    /// </summary>
    public Color GetCorruptionOverlay(float restorationPercent)
    {
        float corruptionAmount = 1f - Mathf.Clamp01(restorationPercent);
        float alpha = corruptionAmount * corruptionOverlayMaxOpacity;
        return new Color(corruptionOverlayColor.r, corruptionOverlayColor.g, corruptionOverlayColor.b, alpha);
    }

    // -----------------------------------------------------------------------
    // Built-in defaults — mirrors the seven-vices palette from
    // IslandEnemyVisualProfile.GetDefault but for environment art.
    // -----------------------------------------------------------------------
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(islandId)
            && groundGradient != null;
    }

    public static IslandVisualProfile GetDefault(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        IslandVisualProfile profile = CreateInstance<IslandVisualProfile>();
        profile.islandId = islandId;

        switch (islandId)
        {
            case "island_lust":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.18f, 0.02f, 0.06f),
                    neutralColor = new Color(0.72f, 0.30f, 0.35f),
                    goodColor = new Color(0.95f, 0.70f, 0.65f)
                };
                profile.groundTint = new Color(0.9f, 0.75f, 0.75f);
                profile.wallColor = new Color(0.55f, 0.15f, 0.20f);
                profile.wallHighlightColor = new Color(0.85f, 0.40f, 0.45f);
                profile.waterShallowColor = new Color(0.80f, 0.35f, 0.45f);
                profile.waterDeepColor = new Color(0.45f, 0.05f, 0.18f);
                profile.corruptionOverlayColor = new Color(0.80f, 0.10f, 0.35f);
                profile.corruptionOverlayMaxOpacity = 0.40f;
                profile.tideColors = BuildTideColors(
                    new Color(0.55f, 0.15f, 0.25f), new Color(0.75f, 0.45f, 0.50f),
                    new Color(0.85f, 0.25f, 0.35f), new Color(0.95f, 0.60f, 0.50f),
                    new Color(0.80f, 0.55f, 0.60f));
                break;

            case "island_greed":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.10f, 0.12f, 0.05f),
                    neutralColor = new Color(0.42f, 0.48f, 0.22f),
                    goodColor = new Color(0.70f, 0.75f, 0.45f)
                };
                profile.groundTint = new Color(0.75f, 0.80f, 0.55f);
                profile.wallColor = new Color(0.35f, 0.30f, 0.15f);
                profile.wallHighlightColor = new Color(0.65f, 0.60f, 0.35f);
                profile.waterShallowColor = new Color(0.50f, 0.55f, 0.30f);
                profile.waterDeepColor = new Color(0.20f, 0.22f, 0.10f);
                profile.corruptionOverlayColor = new Color(0.35f, 0.50f, 0.15f);
                profile.corruptionOverlayMaxOpacity = 0.35f;
                profile.tideColors = BuildTideColors(
                    new Color(0.30f, 0.28f, 0.15f), new Color(0.50f, 0.52f, 0.28f),
                    new Color(0.42f, 0.48f, 0.22f), new Color(0.65f, 0.70f, 0.40f),
                    new Color(0.55f, 0.60f, 0.35f));
                break;

            case "island_greed":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.15f, 0.10f, 0.05f),
                    neutralColor = new Color(0.62f, 0.50f, 0.20f),
                    goodColor = new Color(0.95f, 0.88f, 0.55f)
                };
                profile.groundTint = new Color(0.90f, 0.82f, 0.50f);
                profile.wallColor = new Color(0.50f, 0.40f, 0.15f);
                profile.wallHighlightColor = new Color(0.80f, 0.70f, 0.35f);
                profile.waterShallowColor = new Color(0.60f, 0.55f, 0.30f);
                profile.waterDeepColor = new Color(0.25f, 0.18f, 0.08f);
                profile.corruptionOverlayColor = new Color(0.60f, 0.45f, 0.10f);
                profile.corruptionOverlayMaxOpacity = 0.35f;
                profile.tideColors = BuildTideColors(
                    new Color(0.45f, 0.35f, 0.12f), new Color(0.65f, 0.55f, 0.25f),
                    new Color(0.78f, 0.62f, 0.18f), new Color(0.90f, 0.85f, 0.50f),
                    new Color(0.80f, 0.70f, 0.35f));
                break;

            case "island_desire":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.08f, 0.05f, 0.12f),
                    neutralColor = new Color(0.45f, 0.35f, 0.52f),
                    goodColor = new Color(0.78f, 0.70f, 0.82f)
                };
                profile.groundTint = new Color(0.70f, 0.65f, 0.78f);
                profile.wallColor = new Color(0.35f, 0.25f, 0.40f);
                profile.wallHighlightColor = new Color(0.60f, 0.52f, 0.65f);
                profile.waterShallowColor = new Color(0.50f, 0.42f, 0.55f);
                profile.waterDeepColor = new Color(0.18f, 0.12f, 0.22f);
                profile.corruptionOverlayColor = new Color(0.30f, 0.22f, 0.40f);
                profile.corruptionOverlayMaxOpacity = 0.40f;
                profile.tideColors = BuildTideColors(
                    new Color(0.25f, 0.18f, 0.32f), new Color(0.40f, 0.35f, 0.48f),
                    new Color(0.45f, 0.35f, 0.52f), new Color(0.65f, 0.58f, 0.72f),
                    new Color(0.55f, 0.48f, 0.62f));
                break;

            case "island_anger":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.20f, 0.02f, 0.02f),
                    neutralColor = new Color(0.80f, 0.25f, 0.10f),
                    goodColor = new Color(1.0f, 0.75f, 0.45f)
                };
                profile.groundTint = new Color(0.95f, 0.60f, 0.40f);
                profile.wallColor = new Color(0.60f, 0.15f, 0.08f);
                profile.wallHighlightColor = new Color(0.90f, 0.40f, 0.20f);
                profile.waterShallowColor = new Color(0.85f, 0.35f, 0.15f);
                profile.waterDeepColor = new Color(0.40f, 0.05f, 0.05f);
                profile.corruptionOverlayColor = new Color(0.85f, 0.15f, 0.05f);
                profile.corruptionOverlayMaxOpacity = 0.50f;
                profile.tideColors = BuildTideColors(
                    new Color(0.50f, 0.10f, 0.08f), new Color(0.70f, 0.20f, 0.12f),
                    new Color(0.90f, 0.25f, 0.10f), new Color(1.0f, 0.55f, 0.25f),
                    new Color(0.95f, 0.40f, 0.18f));
                break;

            case "island_envy":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.05f, 0.12f, 0.04f),
                    neutralColor = new Color(0.30f, 0.62f, 0.28f),
                    goodColor = new Color(0.65f, 0.90f, 0.60f)
                };
                profile.groundTint = new Color(0.60f, 0.85f, 0.55f);
                profile.wallColor = new Color(0.20f, 0.42f, 0.18f);
                profile.wallHighlightColor = new Color(0.45f, 0.70f, 0.42f);
                profile.waterShallowColor = new Color(0.35f, 0.65f, 0.32f);
                profile.waterDeepColor = new Color(0.10f, 0.25f, 0.08f);
                profile.corruptionOverlayColor = new Color(0.20f, 0.45f, 0.18f);
                profile.corruptionOverlayMaxOpacity = 0.40f;
                profile.tideColors = BuildTideColors(
                    new Color(0.15f, 0.35f, 0.12f), new Color(0.28f, 0.52f, 0.25f),
                    new Color(0.30f, 0.62f, 0.28f), new Color(0.55f, 0.82f, 0.50f),
                    new Color(0.42f, 0.72f, 0.38f));
                break;

            case "island_ego":
                profile.groundGradient = new CorruptionGradient
                {
                    evilColor = new Color(0.15f, 0.12f, 0.05f),
                    neutralColor = new Color(0.75f, 0.70f, 0.50f),
                    goodColor = new Color(1.0f, 0.98f, 0.90f)
                };
                profile.groundTint = new Color(0.95f, 0.92f, 0.82f);
                profile.wallColor = new Color(0.70f, 0.60f, 0.30f);
                profile.wallHighlightColor = new Color(0.92f, 0.85f, 0.55f);
                profile.waterShallowColor = new Color(0.75f, 0.72f, 0.55f);
                profile.waterDeepColor = new Color(0.35f, 0.30f, 0.15f);
                profile.corruptionOverlayColor = new Color(0.70f, 0.60f, 0.10f);
                profile.corruptionOverlayMaxOpacity = 0.30f;
                profile.tideColors = BuildTideColors(
                    new Color(0.45f, 0.40f, 0.18f), new Color(0.65f, 0.60f, 0.35f),
                    new Color(0.80f, 0.75f, 0.45f), new Color(0.95f, 0.92f, 0.72f),
                    new Color(0.88f, 0.82f, 0.58f));
                break;

            default:
                Debug.LogWarning($"[IslandVisualProfile] Unknown island id '{islandId}'. Returning null.");
                DestroyImmediate(profile);
                return null;
        }

        return profile;
    }

    private static TideColorEntry[] BuildTideColors(
        Color low1, Color low2, Color mid, Color high1, Color high2)
    {
        return new[]
        {
            new TideColorEntry { tideValue = 1, color = low1 },
            new TideColorEntry { tideValue = 2, color = low1 },
            new TideColorEntry { tideValue = 3, color = low2 },
            new TideColorEntry { tideValue = 4, color = low2 },
            new TideColorEntry { tideValue = 5, color = mid },
            new TideColorEntry { tideValue = 6, color = mid },
            new TideColorEntry { tideValue = 7, color = high1 },
            new TideColorEntry { tideValue = 8, color = high1 },
            new TideColorEntry { tideValue = 9, color = high2 },
            new TideColorEntry { tideValue = 10, color = high2 }
        };
    }
}

// ---------------------------------------------------------------------------
// GroundVisualizer — applies island-specific ground colors based on
// restoration percentage and corruption gradients.
// ---------------------------------------------------------------------------
[RequireComponent(typeof(Renderer))]
public class GroundVisualizer : MonoBehaviour
{
    [Tooltip("Island ID to track. Leave empty to use the active progression island.")]
    [SerializeField] private string islandId;

    [Tooltip("Optional visual profile. If null, one is auto-resolved from the island id.")]
    [SerializeField] private IslandVisualProfile overrideProfile;

    [Tooltip("Optional reference to the ground renderer. Falls back to this GameObject's Renderer.")]
    [SerializeField] private Renderer groundRenderer;

    private IslandRestorationTracker tracker;
    private IslandVisualProfile activeProfile;
    private Material cachedMaterial;

    private void OnEnable()
    {
        if (groundRenderer == null)
        {
            groundRenderer = GetComponent<Renderer>();
        }

        if (groundRenderer != null)
        {
            cachedMaterial = groundRenderer.material;
        }

        ResolveProfile();
        TryFindTracker();
        SubscribeToGameStateManager();
        RefreshColor();
    }

    private void OnDisable()
    {
        UnsubscribeFromTracker();
        UnsubscribeFromGameStateManager();
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

    // -- Profile resolution -------------------------------------------------

    private void ResolveProfile()
    {
        if (overrideProfile != null)
        {
            activeProfile = overrideProfile;
            return;
        }

        string resolvedIsland = ResolveIslandId();
        activeProfile = IslandVisualProfile.GetDefault(resolvedIsland);
    }

    // -- Tracker hooks ------------------------------------------------------

    private void TryFindTracker()
    {
        if (tracker != null) return;

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

    // -- Event handlers -----------------------------------------------------

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        if (changedIslandId == ResolveIslandId())
        {
            RefreshColor();
        }
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        if (restoredIslandId == ResolveIslandId())
        {
            RefreshColor();
        }
    }

    private void HandlePuzzleCompleted()
    {
        RefreshColor();
    }

    // -- Color application --------------------------------------------------

    private void RefreshColor()
    {
        if (cachedMaterial == null || activeProfile == null) return;

        float percent = 0f;
        if (tracker != null)
        {
            percent = tracker.GetRestorationPercent(ResolveIslandId()) / 100f;
        }

        Color groundColor = activeProfile.groundGradient.Evaluate(percent);
        groundColor = Color.Lerp(groundColor, activeProfile.groundTint, 0.25f);

        // Apply corruption overlay — stronger when restoration is low.
        Color overlay = activeProfile.GetCorruptionOverlay(percent);
        groundColor = Color.Lerp(groundColor, overlay, overlay.a);

        cachedMaterial.color = groundColor;
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>
    /// Force-refresh the visual profile (e.g. after switching islands).
    /// </summary>
    public void SetIslandId(string newIslandId)
    {
        islandId = newIslandId;
        ResolveProfile();
        RefreshColor();
    }

    /// <summary>
    /// Set an explicit visual profile at runtime.
    /// </summary>
    public void SetProfile(IslandVisualProfile profile)
    {
        overrideProfile = profile;
        activeProfile = profile;
        RefreshColor();
    }

    /// <summary>
    /// Returns the color the tile system should use for the given tide value
    /// based on the current visual profile.
    /// </summary>
    public Color GetTileColorForTide(int tideValue)
    {
        if (activeProfile == null) return Color.white;

        Color? mapped = activeProfile.GetTileColorForTide(tideValue);
        return mapped ?? Color.white;
    }

    // -- Helpers ------------------------------------------------------------

    private string ResolveIslandId()
    {
        if (!string.IsNullOrEmpty(islandId))
        {
            return IslandThemeRegistry.ResolveIslandId(islandId);
        }
        return IslandThemeRegistry.GetActiveIslandId();
    }
}
