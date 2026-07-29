using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExplorationMapUI : MonoBehaviour
{
    private const string CanvasName = "ExplorationMapCanvas";

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private KeyCode loreLogKey = KeyCode.L;

    [Header("Map Visibility")]
    [SerializeField] private bool showMiniMapByDefault = true;

    [Header("Tracking")]
    [SerializeField] private string islandId = "";

    [Header("Mini Map Layout")]
    [SerializeField] private Vector2 miniMapSize = new Vector2(300f, 200f);
    [SerializeField] private Vector2 miniMapOffset = new Vector2(-24f, 24f);

    [Header("World Bounds")]
    [SerializeField] private string worldBoundsObjectName = "Ground";
    [SerializeField] private bool useFallbackBoundsWhenMissing = true;
    [SerializeField] private Vector2 fallbackWorldCenter = new Vector2(12.5f, 0f);
    [SerializeField] private Vector2 fallbackWorldSize = new Vector2(200f, 200f);
    
    // Cached reference to avoid repeated GameObject.Find calls
    private GameObject cachedBoundsObject;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.07f, 0.1f, 0.14f, 0.82f);
    [SerializeField] private Color mapColor = new Color(0.16f, 0.22f, 0.28f, 0.96f);
    [SerializeField] private Color miniMapBorderColor = new Color(0.23f, 0.34f, 0.45f, 0.95f);
    [SerializeField] private Color playerMarkerColor = new Color(0.2f, 0.9f, 0.85f, 1f);
    [SerializeField] private Color puzzleMarkerColor = new Color(1f, 0.6f, 0.25f, 1f);
    [SerializeField] private Color combatMarkerColor = new Color(0.95f, 0.3f, 0.25f, 1f);
    [SerializeField] private Color enemyMarkerColor = new Color(0.9f, 0.2f, 0.45f, 1f);

    [Header("Mini Map Frame")]
    [SerializeField] [Range(0f, 24f)] private float miniMapBorderPadding = 8f;

    [Header("Markers")]
    [SerializeField] private float playerMarkerSize = 14f;
    [SerializeField] private float pointMarkerSize = 9f;
    [SerializeField] private float markerRefreshInterval = 1f;
    [SerializeField] private bool showEnemyMarkersInMiniMap = true;
    [SerializeField] [Range(-180f, 180f)] private float playerHeadingOffsetDegrees = 45f;

    private sealed class MapMarker
    {
        public Transform target;
        public RectTransform rect;
    }

    private readonly List<MapMarker> mapMarkers = new List<MapMarker>();

    private bool isMapOpen;
    private bool isInExplorationState = true;
    private float markerRefreshTimer;

    private IsometricPlayer player;
    private Canvas mapCanvas;
    private RectTransform panelRoot;
    private Image panelImage;
    private RectTransform mapRect;
    private RectTransform miniMapBorderRect;
    private Image miniMapBorderImage;
    private RectTransform markerRoot;
    private Mask mapMask;
    private Sprite miniMapMaskSprite;
    private Text titleLabel;
    private Text hintLabel;
    private Text restorationLabel;
    private Image playerMarker;
    private Vector2 worldCenter;
    private Vector2 worldSize;
    private void Awake()
    {
        isMapOpen = showMiniMapByDefault;
        isInExplorationState = CanDisplayMap();
        ResolvePlayer();
        ResolveWorldBounds();
        EnsureCanvas();
        RebuildMapMarkers();
        ApplyLayout();
        RefreshRestorationLabel();
    }

    private void Update()
    {
        bool shouldDisplay = CanDisplayMap();
        if (shouldDisplay != isInExplorationState)
        {
            SetExplorationVisibility(shouldDisplay);
        }

        if (!shouldDisplay)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMapVisibility();
        }

        // Keep lore access map-scoped so this hotkey does not unexpectedly trigger while the map is hidden.
        if (isMapOpen && Input.GetKeyDown(loreLogKey))
        {
            OpenAncientTextLog();
        }

        ResolvePlayer();
        UpdatePlayerMarkerPosition();
        UpdateMarkerPositions();
        RefreshRestorationLabel();

        markerRefreshTimer -= Time.unscaledDeltaTime;
        if (markerRefreshTimer <= 0f)
        {
            markerRefreshTimer = Mathf.Max(0.25f, markerRefreshInterval);
            ResolveWorldBounds();
            RebuildMapMarkers();
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        // Try to get from this GameObject first
        player = GetComponent<IsometricPlayer>();
        if (player != null)
        {
            return;
        }

        // Fallback to scene-wide search (expensive, but cached after first success)
        player = FindFirstObjectByType<IsometricPlayer>();
    }

    private bool CanDisplayMap()
    {
        GameStateManager manager = GameStateManager.Instance;
        if (manager == null)
        {
            return SceneManager.GetActiveScene().name == GameStateManager.MainSceneName;
        }

        return manager.currentState == GameStateManager.GameState.Exploration
            && !manager.IsTransitioning;
    }

    private void SetExplorationVisibility(bool visible)
    {
        isInExplorationState = visible;

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(visible && isMapOpen);
        }
    }

    private void ToggleMapVisibility()
    {
        isMapOpen = !isMapOpen;
        ApplyLayout();
    }

    public void ToggleMapVisibilityPublic()
    {
        ToggleMapVisibility();
    }

    private void ResolveWorldBounds()
    {
        if (useFallbackBoundsWhenMissing)
        {
            worldCenter = fallbackWorldCenter;
            worldSize = new Vector2(Mathf.Max(1f, fallbackWorldSize.x), Mathf.Max(1f, fallbackWorldSize.y));
        }

        if (cachedBoundsObject == null && !string.IsNullOrEmpty(worldBoundsObjectName))
        {
            cachedBoundsObject = GameObject.Find(worldBoundsObjectName);
        }

        GameObject boundsObject = cachedBoundsObject;
        if (boundsObject == null)
        {
            return;
        }

        Renderer worldRenderer = boundsObject.GetComponent<Renderer>();
        if (worldRenderer == null)
        {
            return;
        }

        Bounds bounds = worldRenderer.bounds;
        worldCenter = new Vector2(bounds.center.x, bounds.center.z);
        worldSize = new Vector2(Mathf.Max(1f, bounds.size.x), Mathf.Max(1f, bounds.size.z));
    }

    private void EnsureCanvas()
    {
        if (mapCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName);
        canvasObject.transform.SetParent(transform, false);

        mapCanvas = canvasObject.AddComponent<Canvas>();
        mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mapCanvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateMapPanel(canvasObject.transform);
    }

    private void CreateMapPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("MapPanel", typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);

        panelRoot = panelObject.GetComponent<RectTransform>();

        panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        miniMapMaskSprite = CreateCircleMaskSprite(128);

        GameObject miniMapBorderObject = new GameObject("MiniMapBorder", typeof(RectTransform));
        miniMapBorderObject.transform.SetParent(panelRoot, false);
        miniMapBorderRect = miniMapBorderObject.GetComponent<RectTransform>();

        miniMapBorderImage = miniMapBorderObject.AddComponent<Image>();
        miniMapBorderImage.sprite = miniMapMaskSprite;
        miniMapBorderImage.preserveAspect = true;
        miniMapBorderImage.color = miniMapBorderColor;
        miniMapBorderImage.raycastTarget = false;

        titleLabel = CreateLabel(panelRoot, "MINIMAP", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.86f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(14f, 0f);
        titleRect.offsetMax = new Vector2(-14f, 0f);

        GameObject mapObject = new GameObject("MapView", typeof(RectTransform));
        mapObject.transform.SetParent(panelRoot, false);

        mapRect = mapObject.GetComponent<RectTransform>();

        Image mapImage = mapObject.AddComponent<Image>();
        mapImage.color = mapColor;
        mapImage.raycastTarget = false;

        mapMask = mapObject.AddComponent<Mask>();
        mapMask.showMaskGraphic = true;

        markerRoot = new GameObject("Markers", typeof(RectTransform)).GetComponent<RectTransform>();
        markerRoot.transform.SetParent(mapRect, false);
        markerRoot.anchorMin = Vector2.zero;
        markerRoot.anchorMax = Vector2.one;
        markerRoot.offsetMin = Vector2.zero;
        markerRoot.offsetMax = Vector2.zero;

        playerMarker = CreateMarker(markerRoot, "PlayerMarker", playerMarkerColor, playerMarkerSize);

        restorationLabel = CreateLabel(panelRoot, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft);
        RectTransform restorationRect = restorationLabel.rectTransform;
        restorationRect.anchorMin = new Vector2(0f, 0f);
        restorationRect.anchorMax = new Vector2(1f, 0.16f);
        restorationRect.offsetMin = new Vector2(14f, 0f);
        restorationRect.offsetMax = new Vector2(-14f, 0f);

        hintLabel = CreateLabel(panelRoot, string.Empty, 12, FontStyle.Italic, TextAnchor.MiddleRight);
        RectTransform hintRect = hintLabel.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0.16f);
        hintRect.offsetMin = new Vector2(14f, 0f);
        hintRect.offsetMax = new Vector2(-14f, 0f);
        hintLabel.color = new Color(0.75f, 0.82f, 0.9f, 1f);
    }

    private static Text CreateLabel(RectTransform parent, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private static Image CreateMarker(RectTransform parent, string name, Color color, float size)
    {
        GameObject markerObject = new GameObject(name, typeof(RectTransform));
        markerObject.transform.SetParent(parent, false);

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(size, size);

        Image marker = markerObject.AddComponent<Image>();
        marker.color = color;
        marker.raycastTarget = false;
        return marker;
    }

    private static Sprite CreateCircleMaskSprite(int size)
    {
        int clampedSize = Mathf.Clamp(size, 32, 512);
        Texture2D texture = new Texture2D(clampedSize, clampedSize, TextureFormat.ARGB32, false);
        texture.name = "MiniMapCircleMask";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[clampedSize * clampedSize];
        float center = (clampedSize - 1) * 0.5f;
        float radius = center;
        float radiusSquared = radius * radius;

        for (int y = 0; y < clampedSize; y++)
        {
            float deltaY = y - center;
            for (int x = 0; x < clampedSize; x++)
            {
                float deltaX = x - center;
                bool inside = deltaX * deltaX + deltaY * deltaY <= radiusSquared;
                byte alpha = inside ? (byte)255 : (byte)0;
                pixels[y * clampedSize + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, clampedSize, clampedSize),
            new Vector2(0.5f, 0.5f),
            clampedSize);
    }

    private void ApplyMiniMapShape()
    {
        if (mapRect == null)
        {
            return;
        }

        Image mapImage = mapRect.GetComponent<Image>();
        if (mapImage == null)
        {
            return;
        }

        mapImage.sprite = miniMapMaskSprite;
        mapImage.preserveAspect = true;

        if (mapMask != null)
        {
            mapMask.enabled = true;
        }
    }

    private void ApplyLayout()
    {
        if (panelRoot == null || mapRect == null)
        {
            return;
        }

        if (panelImage != null)
        {
            panelImage.sprite = null;
            panelImage.preserveAspect = false;
            panelImage.color = panelColor;
        }

        panelRoot.anchorMin = new Vector2(1f, 0f);
        panelRoot.anchorMax = new Vector2(1f, 0f);
        panelRoot.pivot = new Vector2(1f, 0f);
        panelRoot.sizeDelta = miniMapSize;
        panelRoot.anchoredPosition = miniMapOffset;

        float miniMapDiameter = Mathf.Min(panelRoot.sizeDelta.x * 0.8f, panelRoot.sizeDelta.y * 0.64f);

        if (miniMapBorderRect != null)
        {
            float borderDiameter = miniMapDiameter + Mathf.Max(0f, miniMapBorderPadding) * 2f;
            miniMapBorderRect.anchorMin = new Vector2(0.5f, 0.52f);
            miniMapBorderRect.anchorMax = new Vector2(0.5f, 0.52f);
            miniMapBorderRect.pivot = new Vector2(0.5f, 0.5f);
            miniMapBorderRect.sizeDelta = new Vector2(borderDiameter, borderDiameter);
            miniMapBorderRect.anchoredPosition = new Vector2(0f, 2f);
            miniMapBorderRect.gameObject.SetActive(true);
        }

        if (miniMapBorderImage != null)
        {
            miniMapBorderImage.color = miniMapBorderColor;
        }

        mapRect.anchorMin = new Vector2(0.5f, 0.52f);
        mapRect.anchorMax = new Vector2(0.5f, 0.52f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(miniMapDiameter, miniMapDiameter);
        mapRect.anchoredPosition = new Vector2(0f, 2f);

        if (titleLabel != null)
        {
            titleLabel.text = "MINIMAP";
        }

        if (hintLabel != null)
        {
            hintLabel.text = $"[{toggleKey}] {(isMapOpen ? "Hide" : "Show")}  [{loreLogKey}] Lore";
        }

        ApplyMiniMapShape();

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(isMapOpen && isInExplorationState);
        }

        RefreshRestorationLabel();
        UpdatePlayerMarkerPosition();
        UpdateMarkerPositions();
    }

    private void RefreshRestorationLabel()
    {
        if (restorationLabel == null)
        {
            return;
        }

        string targetIslandId = ResolveTrackedIslandId();

        if (IslandRestorationTracker.Instance == null)
        {
            restorationLabel.text = "Restoration: --";
            return;
        }

        IslandRestorationState state = IslandRestorationTracker.Instance.GetRestorationState(targetIslandId);
        if (state == null)
        {
            restorationLabel.text = $"Restoration: {GetIslandDisplayName(targetIslandId)} 0.0%";
            return;
        }
        restorationLabel.text = $"Restoration: {GetIslandDisplayName(targetIslandId)} {state.RestorationPercent:F1}%";
    }

    private void RebuildMapMarkers()
    {
        for (int i = 0; i < mapMarkers.Count; i++)
        {
            if (mapMarkers[i].rect != null)
            {
                Destroy(mapMarkers[i].rect.gameObject);
            }
        }

        mapMarkers.Clear();

        if (markerRoot == null)
        {
            return;
        }

        PuzzleBoxInteractable[] puzzles = FindObjectsByType<PuzzleBoxInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < puzzles.Length; i++)
        {
            TryCreateMapMarker(puzzles[i] != null ? puzzles[i].transform : null, puzzleMarkerColor);
        }

        CombatBoxInteractable[] combatBoxes = FindObjectsByType<CombatBoxInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < combatBoxes.Length; i++)
        {
            TryCreateMapMarker(combatBoxes[i] != null ? combatBoxes[i].transform : null, combatMarkerColor);
        }

        if (showEnemyMarkersInMiniMap)
        {
            EnemyTrigger[] enemyTriggers = FindObjectsByType<EnemyTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < enemyTriggers.Length; i++)
            {
                TryCreateMapMarker(enemyTriggers[i] != null ? enemyTriggers[i].transform : null, enemyMarkerColor);
            }

            OverworldEnemy[] roamingEnemies = FindObjectsByType<OverworldEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < roamingEnemies.Length; i++)
            {
                TryCreateMapMarker(roamingEnemies[i] != null ? roamingEnemies[i].transform : null, enemyMarkerColor);
            }
        }

        UpdateMarkerPositions();
    }

    private void TryCreateMapMarker(Transform target, Color color)
    {
        if (target == null || markerRoot == null)
        {
            return;
        }

        Image marker = CreateMarker(markerRoot, "MapMarker", color, pointMarkerSize);
        RectTransform markerRect = marker.rectTransform;
        markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        mapMarkers.Add(new MapMarker
        {
            target = target,
            rect = markerRect
        });
    }

    private void UpdatePlayerMarkerPosition()
    {
        if (playerMarker == null)
        {
            return;
        }

        if (player == null)
        {
            playerMarker.gameObject.SetActive(false);
            return;
        }

        playerMarker.gameObject.SetActive(true);
        playerMarker.rectTransform.anchoredPosition = WorldToMapPosition(player.transform.position);
        playerMarker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.y + playerHeadingOffsetDegrees);
    }

    private void UpdateMarkerPositions()
    {
        for (int i = mapMarkers.Count - 1; i >= 0; i--)
        {
            MapMarker marker = mapMarkers[i];
            if (marker == null || marker.rect == null)
            {
                mapMarkers.RemoveAt(i);
                continue;
            }

            if (marker.target == null)
            {
                Destroy(marker.rect.gameObject);
                mapMarkers.RemoveAt(i);
                continue;
            }

            bool shouldShow = marker.target.gameObject.activeInHierarchy;
            marker.rect.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            marker.rect.anchoredPosition = WorldToMapPosition(marker.target.position);
        }
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        if (mapRect == null)
        {
            return Vector2.zero;
        }

        Rect rect = mapRect.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return Vector2.zero;
        }

        Vector2 visibleCenter = worldCenter;
        Vector2 visibleSize = worldSize;

        float minX = visibleCenter.x - visibleSize.x * 0.5f;
        float maxX = visibleCenter.x + visibleSize.x * 0.5f;
        float minZ = visibleCenter.y - visibleSize.y * 0.5f;
        float maxZ = visibleCenter.y + visibleSize.y * 0.5f;

        float normalizedX = Mathf.Clamp01(Mathf.InverseLerp(minX, maxX, worldPosition.x));
        float normalizedY = Mathf.Clamp01(Mathf.InverseLerp(minZ, maxZ, worldPosition.z));

        float x = Mathf.Lerp(-rect.width * 0.5f, rect.width * 0.5f, normalizedX);
        float y = Mathf.Lerp(-rect.height * 0.5f, rect.height * 0.5f, normalizedY);
        return new Vector2(x, y);
    }

    private void OnDestroy()
    {
        if (miniMapMaskSprite != null)
        {
            Texture2D maskTexture = miniMapMaskSprite.texture;
            Destroy(miniMapMaskSprite);
            miniMapMaskSprite = null;

            if (maskTexture != null)
            {
                Destroy(maskTexture);
            }
        }

        if (mapCanvas != null)
        {
            Destroy(mapCanvas.gameObject);
            mapCanvas = null;
        }

        mapMarkers.Clear();
    }

    private void OpenAncientTextLog()
    {
        AncientTextLogUI logUi = FindFirstObjectByType<AncientTextLogUI>();
        if (logUi == null)
        {
            GameObject logObject = new GameObject("AncientTextLogUI");
            logUi = logObject.AddComponent<AncientTextLogUI>();
        }

        if (logUi != null)
        {
            logUi.ShowDiscoveredLog();
        }
    }

    private string ResolveTrackedIslandId()
    {
        if (!string.IsNullOrEmpty(islandId))
        {
            return IslandThemeRegistry.ResolveIslandId(islandId);
        }

        return IslandThemeRegistry.GetActiveIslandId();
    }

    private static string GetIslandDisplayName(string resolvedIslandId)
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(resolvedIslandId);
        if (config != null && !string.IsNullOrEmpty(config.viceName))
        {
            return config.viceName;
        }

        if (string.IsNullOrEmpty(resolvedIslandId))
        {
            return "Island";
        }

        const string islandPrefix = "island_";
        string trimmedId = resolvedIslandId.StartsWith(islandPrefix, StringComparison.Ordinal)
            ? resolvedIslandId.Substring(islandPrefix.Length)
            : resolvedIslandId;

        string[] words = trimmedId.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "Island";
        }

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            words[i] = word.Length == 1
                ? char.ToUpperInvariant(word[0]).ToString()
                : char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
        }

        return string.Join(" ", words);
    }
}
