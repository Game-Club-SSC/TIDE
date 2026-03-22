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

    [Header("Tracking")]
    [SerializeField] private string islandId = "island_1";

    [Header("Mini Map Layout")]
    [SerializeField] private Vector2 miniMapSize = new Vector2(300f, 200f);
    [SerializeField] private Vector2 miniMapOffset = new Vector2(-24f, 24f);

    [Header("Expanded Layout")]
    [Range(0.5f, 1f)]
    [SerializeField] private float expandedWidthPercent = 0.9f;
    [Range(0.5f, 1f)]
    [SerializeField] private float expandedHeightPercent = 0.9f;

    [Header("World Bounds")]
    [SerializeField] private string worldBoundsObjectName = "Ground";
    [SerializeField] private Vector2 fallbackWorldCenter = new Vector2(12.5f, 0f);
    [SerializeField] private Vector2 fallbackWorldSize = new Vector2(200f, 200f);

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.07f, 0.1f, 0.14f, 0.93f);
    [SerializeField] private Color mapColor = new Color(0.16f, 0.22f, 0.28f, 0.96f);
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.58f);
    [SerializeField] private Color playerMarkerColor = new Color(0.2f, 0.9f, 0.85f, 1f);
    [SerializeField] private Color puzzleMarkerColor = new Color(1f, 0.6f, 0.25f, 1f);
    [SerializeField] private Color combatMarkerColor = new Color(0.95f, 0.3f, 0.25f, 1f);
    [SerializeField] private Color enemyMarkerColor = new Color(0.9f, 0.2f, 0.45f, 1f);

    [Header("Markers")]
    [SerializeField] private float playerMarkerSize = 14f;
    [SerializeField] private float pointMarkerSize = 9f;
    [SerializeField] private float markerRefreshInterval = 1f;

    private sealed class MapMarker
    {
        public Transform target;
        public RectTransform rect;
    }

    private readonly List<MapMarker> mapMarkers = new List<MapMarker>();

    private bool isExpanded;
    private bool isVisible = true;
    private float markerRefreshTimer;

    private IsometricPlayer player;
    private Canvas mapCanvas;
    private GameObject dimBackground;
    private RectTransform panelRoot;
    private RectTransform mapRect;
    private RectTransform markerRoot;
    private Text titleLabel;
    private Text hintLabel;
    private Text restorationLabel;
    private Image playerMarker;
    private Vector2 worldCenter;
    private Vector2 worldSize;

    private void Awake()
    {
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
        if (shouldDisplay != isVisible)
        {
            SetMapVisible(shouldDisplay);
        }

        if (!shouldDisplay)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMapSize();
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

        player = GetComponent<IsometricPlayer>();
        if (player != null)
        {
            return;
        }

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

    private void SetMapVisible(bool visible)
    {
        bool shouldCollapseToMini = !visible && isExpanded;
        if (shouldCollapseToMini)
        {
            isExpanded = false;
        }

        isVisible = visible;

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(visible);
        }

        if (dimBackground != null)
        {
            dimBackground.SetActive(visible && isExpanded);
        }

        if (shouldCollapseToMini)
        {
            ApplyLayout();
        }
    }

    private void ToggleMapSize()
    {
        isExpanded = !isExpanded;
        ApplyLayout();
    }

    private void ResolveWorldBounds()
    {
        worldCenter = fallbackWorldCenter;
        worldSize = new Vector2(Mathf.Max(1f, fallbackWorldSize.x), Mathf.Max(1f, fallbackWorldSize.y));

        GameObject boundsObject = null;
        if (!string.IsNullOrEmpty(worldBoundsObjectName))
        {
            boundsObject = GameObject.Find(worldBoundsObjectName);
        }

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

        CreateDimBackground(canvasObject.transform);
        CreateMapPanel(canvasObject.transform);
    }

    private void CreateDimBackground(Transform parent)
    {
        GameObject dimObject = new GameObject("MapDimBackground", typeof(RectTransform));
        dimObject.transform.SetParent(parent, false);

        RectTransform dimRect = dimObject.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        Image dimImage = dimObject.AddComponent<Image>();
        dimImage.color = dimColor;
        dimImage.raycastTarget = false;

        dimObject.SetActive(false);
        dimBackground = dimObject;
    }

    private void CreateMapPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("MapPanel", typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);

        panelRoot = panelObject.GetComponent<RectTransform>();

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

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

    private void ApplyLayout()
    {
        if (panelRoot == null || mapRect == null)
        {
            return;
        }

        if (isExpanded)
        {
            float halfWidth = Mathf.Clamp(expandedWidthPercent, 0.5f, 1f) * 0.5f;
            float halfHeight = Mathf.Clamp(expandedHeightPercent, 0.5f, 1f) * 0.5f;

            panelRoot.anchorMin = new Vector2(0.5f - halfWidth, 0.5f - halfHeight);
            panelRoot.anchorMax = new Vector2(0.5f + halfWidth, 0.5f + halfHeight);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = Vector2.zero;
            panelRoot.sizeDelta = Vector2.zero;

            mapRect.anchorMin = new Vector2(0.03f, 0.1f);
            mapRect.anchorMax = new Vector2(0.97f, 0.86f);
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = Vector2.zero;

            if (titleLabel != null)
            {
                titleLabel.text = "WORLD MAP";
            }

            if (hintLabel != null)
            {
                hintLabel.text = $"[{toggleKey}] Minimize";
            }
        }
        else
        {
            panelRoot.anchorMin = new Vector2(1f, 0f);
            panelRoot.anchorMax = new Vector2(1f, 0f);
            panelRoot.pivot = new Vector2(1f, 0f);
            panelRoot.sizeDelta = miniMapSize;
            panelRoot.anchoredPosition = miniMapOffset;

            mapRect.anchorMin = new Vector2(0.05f, 0.2f);
            mapRect.anchorMax = new Vector2(0.95f, 0.84f);
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = Vector2.zero;

            if (titleLabel != null)
            {
                titleLabel.text = "MINIMAP";
            }

            if (hintLabel != null)
            {
                hintLabel.text = $"[{toggleKey}] Expand";
            }
        }

        if (dimBackground != null)
        {
            dimBackground.SetActive(isExpanded && isVisible);
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

        string targetIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;

        if (IslandRestorationTracker.Instance == null)
        {
            restorationLabel.text = "Restoration: --";
            return;
        }

        IslandRestorationState state = IslandRestorationTracker.Instance.GetRestorationState(targetIslandId);
        if (isExpanded)
        {
            restorationLabel.text =
                $"{targetIslandId}  {state.RestorationPercent:F1}%  " +
                $"Combat {state.CombatEncountersCompleted}  " +
                $"Puzzle {state.PuzzleEncountersCompleted}";
        }
        else
        {
            restorationLabel.text = $"Restoration {state.RestorationPercent:F1}%";
        }
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

        float minX = worldCenter.x - worldSize.x * 0.5f;
        float maxX = worldCenter.x + worldSize.x * 0.5f;
        float minZ = worldCenter.y - worldSize.y * 0.5f;
        float maxZ = worldCenter.y + worldSize.y * 0.5f;

        float normalizedX = Mathf.InverseLerp(minX, maxX, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(minZ, maxZ, worldPosition.z);

        float x = Mathf.Lerp(-rect.width * 0.5f, rect.width * 0.5f, normalizedX);
        float y = Mathf.Lerp(-rect.height * 0.5f, rect.height * 0.5f, normalizedY);
        return new Vector2(x, y);
    }

    private void OnDestroy()
    {
        if (mapCanvas != null)
        {
            Destroy(mapCanvas.gameObject);
            mapCanvas = null;
        }

        mapMarkers.Clear();
    }
}
