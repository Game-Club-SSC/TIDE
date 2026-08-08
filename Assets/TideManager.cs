using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TideManager : MonoBehaviour
{
    private const string ViceIslandPrefix = "island_";

    [Header("Interaction")]
    public LayerMask tileLayer = ~0;
    [SerializeField] private int maxCarrySteps = 2;

    [Header("Board Generation")]
    [SerializeField] private Vector3 boardCenter = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float tileSpacing = 2.25f;
    [SerializeField] private Vector3 tileScale = new Vector3(1.9f, 0.35f, 1.9f);

    [Header("Board Presentation")]
    [SerializeField] private bool renderBoardAsUi = true;
    [SerializeField] private Vector2 uiBoardPanelSize = new Vector2(720f, 760f);
    [SerializeField] private Color uiPanelColor = new Color(0.09f, 0.12f, 0.18f, 0.94f);
    [SerializeField] private Color uiGridBackgroundColor = new Color(0.14f, 0.17f, 0.24f, 0.92f);
    [SerializeField] private Color uiTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
    [SerializeField] private Color uiTargetColor = new Color(0.58f, 0.85f, 0.66f, 1f);
    [SerializeField] private Color uiHighColor = new Color(0.96f, 0.84f, 0.58f, 1f);
    [SerializeField] private Color uiLowColor = new Color(0.41f, 0.6f, 0.8f, 1f);
    [SerializeField] private Color uiSealedColor = new Color(0.24f, 0.24f, 0.28f, 1f);
    [SerializeField] private Color uiSelectedColor = new Color(0.96f, 0.9f, 0.42f, 1f);
    [SerializeField] private Color uiReachableColor = new Color(0.64f, 0.9f, 0.78f, 1f);
    [SerializeField] private Color uiUnavailableColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    [SerializeField] private float uiSolvedDismissDuration = 0.38f;

    [Header("Completion Flow")]
    [SerializeField] private float completionReturnDelay = 1f;

    [Header("Instability Decay")]
    [SerializeField] private int instabilityThreshold = 3;

    [Header("Greed Consumption")]
    [SerializeField] private bool enableConsumption;
    [SerializeField] private int consumptionAmount = 1;

    [Header("Sealed Tile Combat")]
    [SerializeField] private string fallbackSealedTileEncounterId = "encounter_imp_trio";
    [SerializeField] private float sealedTileCombatRestorationValue = 0.001f;

    private TideTile[,] activeTiles = new TideTile[3, 3];
    private int gridRows = 3;
    private int gridCols = 3;
    private int[,] puzzleValues =
    {
        { 9, 1, 10 },
        { 7, 5, 2 },
        { 5, 3, 3 }
    };
    private int[,] initialPuzzleValues;
    private bool[,] sealedTiles =
    {
        { false, false, false },
        { false, true, false },
        { false, false, false }
    };
    private Vector2Int sealedPosition = new Vector2Int(1, 1);
    private Vector2Int lockedPosition = new Vector2Int(-1, -1);
    private string lockedEncounterId = "";
    private string lockedEncounterIslandId = "";
    private string puzzleIslandId = "";
    private PuzzleWinCondition winCondition = new PuzzleWinCondition();

    private Transform runtimeBoardRoot;
    private readonly List<GameObject> sealedTileEnemyMarkers = new List<GameObject>();
    private Canvas boardCanvas;
    private RectTransform boardPanel;
    private RectTransform boardGridRoot;
    private CanvasGroup boardCanvasGroup;
    private Text boardHeaderLabel;
    private UiTileView[,] uiTileViews = new UiTileView[3, 3];
    private TideTile hoveredTile;
    private TideTile carryingSource;
    private int carriedAmount;
    private bool puzzleSolved;
    private bool isStartingSealedTileCombat;
    private bool isClosingBoardUi;
    private bool overlayMode;
    private bool overlayClosing;
    private string overlayPuzzleBoxId = string.Empty;
    private string overlayIslandId = string.Empty;
    private string overlayEncounterId = string.Empty;
    private float overlayRestorationValue = 0.2f;
    private PuzzleData overlayPuzzleData;
    private int[,] overlayLegacyLayout;
    private Vector2Int overlayLegacySealed = new Vector2Int(-1, -1);
    private int[,] overlayRuntimeLayout;

    public event Action OverlayExitRequested;
    public event Action OverlayPuzzleSolved;

    public int CarriedAmount => carriedAmount;
    public bool IsCarrying => carriedAmount > 0;

    public event Action OnCarriedAmountChanged;
    public event Action OnPuzzleReset;

    public bool IsPuzzleSolved => puzzleSolved;
    public bool IsOverlayMode => overlayMode;
    public string OverlayPuzzleBoxId => overlayPuzzleBoxId;
    public bool UsesUiBoard => renderBoardAsUi;

    private sealed class UiTileView
    {
        public int Row;
        public int Col;
        public Button Button;
        public Image Background;
        public Text Label;
    }

    public void ConfigureOverlaySession(
        PuzzleData data,
        int[,] legacyLayout,
        Vector2Int legacySealed,
        int[,] runtimeLayout,
        string puzzleBoxId,
        string islandId,
        string encounterId,
        float restorationValue,
        Vector3 worldBoardCenter)
    {
        // Validate array dimensions if both are provided
        if (legacyLayout != null && runtimeLayout != null
            && (legacyLayout.GetLength(0) != runtimeLayout.GetLength(0)
                || legacyLayout.GetLength(1) != runtimeLayout.GetLength(1)))
        {
            Debug.LogError("[TideManager] ConfigureOverlaySession: legacyLayout and runtimeLayout dimensions mismatch.");
            return;
        }

        overlayMode = true;
        overlayClosing = false;
        overlayPuzzleData = data;
        overlayLegacyLayout = CloneGrid(legacyLayout);
        overlayLegacySealed = legacySealed;
        overlayRuntimeLayout = CloneGrid(runtimeLayout);
        overlayPuzzleBoxId = puzzleBoxId;
        overlayIslandId = islandId;
        overlayEncounterId = encounterId;
        overlayRestorationValue = restorationValue;
        boardCenter = worldBoardCenter;
        puzzleIslandId = islandId;
    }

    public int[,] CaptureCurrentGrid()
    {
        int[,] grid = new int[gridRows, gridCols];
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                grid[row, col] = tile != null ? tile.CurrentTideValue : puzzleValues[row, col];
            }
        }

        return grid;
    }

    public void RequestOverlayClose()
    {
        if (!overlayMode || overlayClosing)
        {
            return;
        }

        overlayClosing = true;

        if (carryingSource != null)
        {
            carryingSource.ApplyPlace(carriedAmount);
            ApplyInstabilityDecay();
            EvaluatePuzzleCompletion();
            carryingSource = null;
            carriedAmount = 0;
            OnCarriedAmountChanged?.Invoke();
        }

        OverlayExitRequested?.Invoke();
    }

    public void InitializePuzzle(int[,] layout, Vector2Int sealedTile)
    {
        if (layout == null || layout.GetLength(0) < 1 || layout.GetLength(1) < 1)
        {
            Debug.LogWarning("[TideManager] Invalid layout provided. Using default.");
            return;
        }

        gridRows = layout.GetLength(0);
        gridCols = layout.GetLength(1);
        activeTiles = new TideTile[gridRows, gridCols];
        uiTileViews = new UiTileView[gridRows, gridCols];
        puzzleValues = new int[gridRows, gridCols];
        sealedTiles = new bool[gridRows, gridCols];
        sealedPosition = new Vector2Int(-1, -1);
        lockedPosition = new Vector2Int(-1, -1);
        lockedEncounterId = string.Empty;
        lockedEncounterIslandId = puzzleIslandId;
        winCondition = new PuzzleWinCondition();
        enableConsumption = false;
        consumptionAmount = 1;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                puzzleValues[row, col] = layout[row, col];
            }
        }

        bool validSealedTile = sealedTile.x >= 0 && sealedTile.y >= 0
            && sealedTile.x < gridCols && sealedTile.y < gridRows;

        if (validSealedTile)
        {
            lockedPosition = sealedTile;
            lockedEncounterId = BuildLegacyLockedEncounterId(sealedTile);

            if (!IsLockedEncounterCleared())
            {
                TrySetSealedTile(sealedTile, true);
            }
        }
    }

    public void InitializePuzzle(PuzzleData data)
    {
        if (data == null || !data.IsValid())
        {
            Debug.LogWarning("[TideManager] Invalid PuzzleData provided. Keeping current puzzle.");
            return;
        }

        Vector2Int dimensions = data.GetResolvedGridDimensions();
        if (dimensions.x <= 0 || dimensions.y <= 0)
        {
            Debug.LogWarning("[TideManager] PuzzleData has invalid dimensions. Keeping current puzzle.");
            return;
        }

        gridRows = dimensions.y;
        gridCols = dimensions.x;
        activeTiles = new TideTile[gridRows, gridCols];
        uiTileViews = new UiTileView[gridRows, gridCols];
        puzzleValues = data.GetGrid();
        sealedPosition = data.sealedPosition;
        winCondition = data.winCondition ?? new PuzzleWinCondition();
        instabilityThreshold = data.instabilityThreshold;
        enableConsumption = data.enableConsumption;
        consumptionAmount = Mathf.Max(1, data.consumptionAmount);
        lockedPosition = data.lockedPosition;
        lockedEncounterId = data.lockedTileEncounterId;
        lockedEncounterIslandId = data.lockedTileIslandId;

        if (string.IsNullOrEmpty(puzzleIslandId))
        {
            puzzleIslandId = lockedEncounterIslandId;
        }

        sealedTiles = data.GetSealedMap();
        if (data.HasLockedTile)
        {
            bool lockedCleared = IsLockedEncounterCleared();

            if (!lockedCleared
                && data.lockedPosition.x >= 0 && data.lockedPosition.x < gridCols
                && data.lockedPosition.y >= 0 && data.lockedPosition.y < gridRows)
            {
                sealedTiles[data.lockedPosition.y, data.lockedPosition.x] = true;
            }
        }
    }

    private bool IsLockedEncounterCleared()
    {
        if (IslandRestorationTracker.Instance == null || string.IsNullOrEmpty(lockedEncounterId))
        {
            return false;
        }

        string islandScope = GetPuzzleIslandIdForLookup();
        return !string.IsNullOrEmpty(islandScope)
            && IslandRestorationTracker.Instance.HasClearedEncounter(islandScope, lockedEncounterId);
    }

    private string GetPuzzleIslandIdForLookup()
    {
        if (!string.IsNullOrEmpty(lockedEncounterIslandId))
        {
            return IslandThemeRegistry.ResolveIslandId(lockedEncounterIslandId);
        }

        if (!string.IsNullOrEmpty(puzzleIslandId))
        {
            return IslandThemeRegistry.ResolveIslandId(puzzleIslandId);
        }

        return IslandThemeRegistry.GetActiveIslandId();
    }

    private void Start()
    {
        if (overlayMode)
        {
            ApplyIslandVisualTheme(overlayIslandId);
            InitializeOverlaySession();
            return;
        }

        if (SceneManager.GetActiveScene().name != GameStateManager.PuzzleSceneName)
        {
            enabled = false;
            return;
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.PendingPuzzleData != null)
        {
            puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            ApplyIslandVisualTheme(puzzleIslandId);
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleData);
        }
        else if (GameStateManager.Instance != null && GameStateManager.Instance.PendingPuzzleLayout != null)
        {
            puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            ApplyIslandVisualTheme(puzzleIslandId);
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleLayout, GameStateManager.Instance.PendingPuzzleSealedTile);
        }
        else
        {
            if (GameStateManager.Instance != null)
            {
                puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            }

            ApplyIslandVisualTheme(puzzleIslandId);
            InitializePuzzle(puzzleValues, sealedPosition);
        }

        GenerateBoard();
        StoreInitialValues();
        UpdateTileVisuals();
        isStartingSealedTileCombat = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterPuzzle();
        }
    }

    private void ApplyIslandVisualTheme(string islandId)
    {
        if (string.IsNullOrEmpty(islandId) || !islandId.StartsWith(ViceIslandPrefix, StringComparison.Ordinal))
        {
            return;
        }

        IslandConfig[] configs = Resources.LoadAll<IslandConfig>("Islands");
        if (configs == null || configs.Length == 0)
        {
            return;
        }

        IslandConfig config = null;
        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig candidate = configs[i];
            if (candidate != null && string.Equals(candidate.islandId, islandId, StringComparison.Ordinal))
            {
                config = candidate;
                break;
            }
        }

        if (config == null)
        {
            return;
        }

        uiPanelColor = Color.Lerp(new Color(0.06f, 0.08f, 0.12f, 0.94f), config.viceSecondaryColor, 0.38f);
        uiGridBackgroundColor = Color.Lerp(new Color(0.13f, 0.16f, 0.23f, 0.92f), config.viceSecondaryColor, 0.22f);
        uiTargetColor = Color.Lerp(new Color(0.58f, 0.85f, 0.66f, 1f), config.vicePrimaryColor, 0.28f);
        uiSelectedColor = Color.Lerp(new Color(0.96f, 0.9f, 0.42f, 1f), config.vicePrimaryColor, 0.42f);
        uiReachableColor = Color.Lerp(new Color(0.64f, 0.9f, 0.78f, 1f), config.vicePrimaryColor, 0.2f);
        uiHighColor = Color.Lerp(new Color(0.96f, 0.84f, 0.58f, 1f), config.vicePrimaryColor, 0.25f);

        Debug.Log($"[TideManager] Applied vice puzzle palette for '{config.viceName}' ({islandId}).");
    }

    private void InitializeOverlaySession()
    {
        puzzleSolved = false;
        carriedAmount = 0;
        carryingSource = null;

        if (overlayPuzzleData != null)
        {
            InitializePuzzle(overlayPuzzleData);
        }
        else if (overlayLegacyLayout != null)
        {
            InitializePuzzle(overlayLegacyLayout, overlayLegacySealed);
        }
        else
        {
            InitializePuzzle(puzzleValues, sealedPosition);
        }

        if (overlayRuntimeLayout != null)
        {
            ApplyRuntimeLayout(overlayRuntimeLayout);
        }

        GenerateBoard();
        StoreInitialValues();
        UpdateTileVisuals();
        isStartingSealedTileCombat = false;
    }

    private void OnDestroy()
    {
        TeardownUiBoard();
    }

    private void Update()
    {
        if (isClosingBoardUi)
        {
            return;
        }

        if (overlayMode && Input.GetKeyDown(KeyCode.Escape))
        {
            RequestOverlayClose();
            return;
        }

        hoveredTile = renderBoardAsUi ? null : GetHoveredTile();

        if (puzzleSolved)
        {
            UpdateTileVisuals();
            return;
        }

        if (!renderBoardAsUi)
        {
            if (carriedAmount > 0)
            {
                HandleDestinationSelection();
            }
            else
            {
                HandleSourceSelection();
            }
        }

        UpdateTileVisuals();
    }

    private void GenerateBoard()
    {
        TeardownUiBoard();

        if (renderBoardAsUi)
        {
            GenerateBoardUi();
            return;
        }

        for (int i = 0; i < sealedTileEnemyMarkers.Count; i++)
        {
            if (sealedTileEnemyMarkers[i] != null)
            {
                Destroy(sealedTileEnemyMarkers[i]);
            }
        }
        sealedTileEnemyMarkers.Clear();

        if (runtimeBoardRoot != null)
        {
            Destroy(runtimeBoardRoot.gameObject);
        }

        runtimeBoardRoot = new GameObject("RuntimeTideBoard").transform;
        runtimeBoardRoot.SetParent(transform, false);

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"TideTile_{row}_{col}";
                tileObject.transform.SetParent(runtimeBoardRoot, false);
                tileObject.transform.position = GetWorldPosition(row, col);
                tileObject.transform.localScale = tileScale;
                tileObject.layer = gameObject.layer;

                TideTile tile = tileObject.AddComponent<TideTile>();
                tile.Configure(new Vector2Int(col, row), puzzleValues[row, col], sealedTiles[row, col]);
                activeTiles[row, col] = tile;

                if (tile.IsSealed)
                {
                    CreateSealedTileEnemyMarker(tileObject.transform);
                }
            }
        }
    }

    private void GenerateBoardUi()
    {
        GenerateHiddenLogicBoard();
        EnsureUiBoardCanvas();

        if (boardGridRoot != null)
        {
            for (int i = boardGridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(boardGridRoot.GetChild(i).gameObject);
            }
        }

        for (int i = 0; i < sealedTileEnemyMarkers.Count; i++)
        {
            if (sealedTileEnemyMarkers[i] != null)
            {
                Destroy(sealedTileEnemyMarkers[i]);
            }
        }
        sealedTileEnemyMarkers.Clear();

        if (runtimeBoardRoot != null)
        {
            // Keep hidden logic board root active in UI mode.
        }

        if (boardGridRoot == null)
        {
            return;
        }

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                GameObject tileObject = new GameObject(
                    $"UiTideTile_{row}_{col}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                tileObject.transform.SetParent(boardGridRoot, false);

                RectTransform tileRect = tileObject.GetComponent<RectTransform>();
                tileRect.anchorMin = new Vector2(col / (float)gridCols, 1f - (row + 1) / (float)gridRows);
                tileRect.anchorMax = new Vector2((col + 1) / (float)gridCols, 1f - row / (float)gridRows);
                tileRect.offsetMin = new Vector2(8f, 8f);
                tileRect.offsetMax = new Vector2(-8f, -8f);

                Image tileImage = tileObject.GetComponent<Image>();
                tileImage.color = uiGridBackgroundColor;

                Button button = tileObject.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.onClick.RemoveAllListeners();

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.96f);
                colors.selectedColor = colors.highlightedColor;
                colors.fadeDuration = 0.05f;
                button.colors = colors;

                GameObject labelObject = new GameObject("ValueLabel", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(tileObject.transform, false);

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                Text valueLabel = labelObject.GetComponent<Text>();
                valueLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                valueLabel.alignment = TextAnchor.MiddleCenter;
                valueLabel.fontSize = 46;
                valueLabel.fontStyle = FontStyle.Bold;
                valueLabel.color = uiTextColor;
                valueLabel.raycastTarget = false;

                UiTileView view = new UiTileView
                {
                    Row = row,
                    Col = col,
                    Button = button,
                    Background = tileImage,
                    Label = valueLabel
                };
                uiTileViews[row, col] = view;

                int capturedRow = row;
                int capturedCol = col;
                button.onClick.AddListener(() => OnUiTileClicked(capturedRow, capturedCol));
            }
        }

        UpdateUiHeader();
    }

    private void GenerateHiddenLogicBoard()
    {
        if (runtimeBoardRoot != null)
        {
            Destroy(runtimeBoardRoot.gameObject);
        }

        runtimeBoardRoot = new GameObject("RuntimeTideBoard").transform;
        runtimeBoardRoot.SetParent(transform, false);

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"TideTile_{row}_{col}";
                tileObject.transform.SetParent(runtimeBoardRoot, false);
                tileObject.transform.position = GetWorldPosition(row, col);
                tileObject.transform.localScale = tileScale;
                tileObject.layer = gameObject.layer;

                TideTile tile = tileObject.AddComponent<TideTile>();
                tile.Configure(new Vector2Int(col, row), puzzleValues[row, col], sealedTiles[row, col]);
                activeTiles[row, col] = tile;

                Collider tileCollider = tileObject.GetComponent<Collider>();
                if (tileCollider != null)
                {
                    tileCollider.enabled = false;
                }

                Renderer tileRenderer = tileObject.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.enabled = false;
                }

                Transform labelTransform = tileObject.transform.Find("ValueLabel");
                if (labelTransform != null)
                {
                    labelTransform.gameObject.SetActive(false);
                }
            }
        }
    }

    private void CreateSealedTileEnemyMarker(Transform tileTransform)
    {
        if (renderBoardAsUi)
        {
            return;
        }

        if (tileTransform == null)
        {
            return;
        }

        TideTile markerTile = tileTransform.GetComponent<TideTile>();
        if (markerTile != null)
        {
            for (int i = 0; i < sealedTileEnemyMarkers.Count; i++)
            {
                GameObject existingMarker = sealedTileEnemyMarkers[i];
                if (existingMarker == null)
                {
                    continue;
                }

                TideTile existingTile = existingMarker.GetComponentInParent<TideTile>();
                if (existingTile != null && existingTile.GridPosition == markerTile.GridPosition)
                {
                    return;
                }
            }
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "SealedTileEnemyMarker";
        marker.transform.SetParent(tileTransform, false);
        marker.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        marker.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.material.color = new Color(0.89f, 0.22f, 0.18f);
        }

        sealedTileEnemyMarkers.Add(marker);
    }

    private void EnsureUiBoardCanvas()
    {
        EnsureEventSystemExists();

        if (boardCanvas != null && boardPanel != null && boardGridRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("TidePuzzleBoardCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        boardCanvas = canvasObject.AddComponent<Canvas>();
        boardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        boardCanvas.sortingOrder = 720;

        boardCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        boardCanvasGroup.alpha = 1f;
        boardCanvasGroup.blocksRaycasts = true;
        boardCanvasGroup.interactable = true;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("BoardPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);

        boardPanel = panelObject.GetComponent<RectTransform>();
        boardPanel.anchorMin = new Vector2(0.5f, 0.5f);
        boardPanel.anchorMax = new Vector2(0.5f, 0.5f);
        boardPanel.pivot = new Vector2(0.5f, 0.5f);
        boardPanel.sizeDelta = uiBoardPanelSize;
        boardPanel.anchoredPosition = new Vector2(0f, -24f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = uiPanelColor;

        boardHeaderLabel = CreateBoardHeaderLabel(panelObject.transform);

        GameObject gridObject = new GameObject("BoardGrid", typeof(RectTransform), typeof(Image));
        gridObject.transform.SetParent(panelObject.transform, false);

        boardGridRoot = gridObject.GetComponent<RectTransform>();
        boardGridRoot.anchorMin = new Vector2(0f, 0f);
        boardGridRoot.anchorMax = new Vector2(1f, 1f);
        boardGridRoot.offsetMin = new Vector2(40f, 48f);
        boardGridRoot.offsetMax = new Vector2(-40f, -150f);

        Image gridImage = gridObject.GetComponent<Image>();
        gridImage.color = uiGridBackgroundColor;
    }

    private static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void TeardownUiBoard()
    {
        isClosingBoardUi = false;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                uiTileViews[row, col] = null;
            }
        }

        if (boardCanvas != null)
        {
            Destroy(boardCanvas.gameObject);
        }

        boardCanvas = null;
        boardPanel = null;
        boardGridRoot = null;
        boardCanvasGroup = null;
        boardHeaderLabel = null;
    }

    private Text CreateBoardHeaderLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("BoardHeader", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(26f, -126f);
        labelRect.offsetMax = new Vector2(-26f, -18f);

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.color = uiTextColor;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = "TIDE STABILIZATION";
        return label;
    }

    private void StoreInitialValues()
    {
        initialPuzzleValues = new int[gridRows, gridCols];
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                initialPuzzleValues[row, col] = puzzleValues[row, col];
            }
        }
    }

    public void ResetPuzzle()
    {
        if (puzzleSolved || initialPuzzleValues == null)
        {
            return;
        }

        if (carryingSource != null)
        {
            carryingSource.ApplyPlace(carriedAmount);
            ApplyInstabilityDecay();
            EvaluatePuzzleCompletion();
        }

        carryingSource = null;
        carriedAmount = 0;
        OnCarriedAmountChanged?.Invoke();

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null || tile.IsSealed)
                {
                    continue;
                }

                tile.currentTideValue = initialPuzzleValues[row, col];
                tile.RefreshVisuals();
            }
        }

        UpdateUiHeader();
        OnPuzzleReset?.Invoke();
    }

    private Vector3 GetWorldPosition(int row, int col)
    {
        float xOffset = (col - (gridCols - 1) * 0.5f) * tileSpacing;
        float zOffset = ((gridRows - 1) * 0.5f - row) * tileSpacing;
        return boardCenter + new Vector3(xOffset, 0f, zOffset);
    }

    private TideTile GetHoveredTile()
    {
        if (Camera.main == null)
        {
            return null;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayer))
        {
            return hit.collider.GetComponent<TideTile>();
        }

        return null;
    }

    private void HandleSourceSelection()
    {
        if (hoveredTile == null)
        {
            return;
        }

        if (!renderBoardAsUi && !Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (hoveredTile.IsSealed)
        {
            if (overlayMode)
            {
                hoveredTile.FlashInvalid();
                return;
            }

            TryTriggerSealedTileEncounter(hoveredTile);
            return;
        }

        int takeAmount = hoveredTile.GetMaxTake();
        if (takeAmount <= 0)
        {
            if (!hoveredTile.IsSealed)
            {
                hoveredTile.FlashInvalid();
            }
            return;
        }

        carryingSource = hoveredTile;
        carriedAmount = takeAmount;
        carryingSource.ApplyTake(carriedAmount);
        OnCarriedAmountChanged?.Invoke();

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleTileTake();
        }
    }

    private void TryTriggerSealedTileEncounter(TideTile sealedTile)
    {
        if (sealedTile == null || isStartingSealedTileCombat)
        {
            return;
        }

        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanEnterCombatFromPuzzle())
        {
            return;
        }

        Vector2Int tilePosition = sealedTile.GridPosition;
        string completionEncounterId = GetSealedTileEncounterId(tilePosition);
        string islandScope = GetPuzzleIslandIdForLookup();
        islandScope = string.IsNullOrEmpty(islandScope)
            ? IslandThemeRegistry.GetActiveIslandId()
            : IslandThemeRegistry.ResolveIslandId(islandScope);

        if (IslandRestorationTracker.Instance != null
            && !string.IsNullOrEmpty(completionEncounterId)
            && IslandRestorationTracker.Instance.HasClearedEncounter(islandScope, completionEncounterId))
        {
            sealedTile.FlashInvalid();
            return;
        }

        EncounterConfig encounterConfig = LoadEncounterConfigById(completionEncounterId);
        if (encounterConfig == null && !string.IsNullOrEmpty(fallbackSealedTileEncounterId))
        {
            encounterConfig = LoadEncounterConfigById(fallbackSealedTileEncounterId);
        }

        if (encounterConfig == null)
        {
            Debug.LogWarning($"[TideManager] No encounter config found for sealed tile '{completionEncounterId}'.");
            sealedTile.FlashInvalid();
            return;
        }

        if (string.IsNullOrEmpty(completionEncounterId))
        {
            completionEncounterId = $"sealed_{tilePosition.x}_{tilePosition.y}_guard";
        }

        GameStateManager.Instance.PendingEnemyComposition = EnemyComposition.FromEncounterConfig(encounterConfig);
        isStartingSealedTileCombat = true;
        Debug.Log($"[TideManager] Sealed tile combat started at {tilePosition} using encounter '{encounterConfig.encounterId}'.");
        GameStateManager.Instance.EnterCombatSceneFromPuzzle(islandScope, completionEncounterId, sealedTileCombatRestorationValue);
    }

    private void OnUiTileClicked(int row, int col)
    {
        if (!renderBoardAsUi || row < 0 || row >= gridRows || col < 0 || col >= gridCols)
        {
            return;
        }

        TideTile clickedTile = activeTiles[row, col];
        if (clickedTile == null)
        {
            return;
        }

        hoveredTile = clickedTile;
        if (boardGridRoot != null)
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }

        if (puzzleSolved)
        {
            UpdateTileVisuals();
            return;
        }

        if (carriedAmount > 0)
        {
            HandleDestinationSelection();
        }
        else
        {
            HandleSourceSelection();
        }

        UpdateTileVisuals();
    }

    private string GetSealedTileEncounterId(Vector2Int tilePosition)
    {
        if (tilePosition == lockedPosition && !string.IsNullOrEmpty(lockedEncounterId))
        {
            return lockedEncounterId;
        }

        return $"sealed_{tilePosition.x}_{tilePosition.y}_guard";
    }

    private string BuildLegacyLockedEncounterId(Vector2Int tilePosition)
    {
        if (GameStateManager.Instance != null && !string.IsNullOrEmpty(GameStateManager.Instance.PendingPuzzleEncounterId))
        {
            return $"{GameStateManager.Instance.PendingPuzzleEncounterId}_sealed_{tilePosition.x}_{tilePosition.y}_guard";
        }

        return $"sealed_{tilePosition.x}_{tilePosition.y}_guard";
    }

    private static EncounterConfig LoadEncounterConfigById(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return null;
        }

        EncounterConfig encounterConfig = Resources.Load<EncounterConfig>($"Encounters/{encounterId}");
        if (encounterConfig != null)
        {
            return encounterConfig;
        }

        EncounterConfig[] encounterConfigs = Resources.LoadAll<EncounterConfig>("Encounters");
        for (int i = 0; i < encounterConfigs.Length; i++)
        {
            EncounterConfig candidate = encounterConfigs[i];
            if (candidate != null && string.Equals(candidate.encounterId, encounterId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private void HandleDestinationSelection()
    {
        if (hoveredTile == null)
        {
            return;
        }

        if (!renderBoardAsUi && !Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (hoveredTile == carryingSource)
        {
            return;
        }

        if (!hoveredTile.CanReceive(carriedAmount))
        {
            hoveredTile.FlashInvalid();
            return;
        }

        if (!CanReachWithinCarrySteps(carryingSource, hoveredTile))
        {
            hoveredTile.FlashInvalid();
            return;
        }

        hoveredTile.ApplyPlace(carriedAmount);
        ApplyConsumption(hoveredTile);
        ApplyGreedCoinYield(hoveredTile);

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleTilePlace();
        }
        carriedAmount = 0;
        carryingSource = null;
        OnCarriedAmountChanged?.Invoke();
        ApplyInstabilityDecay();
        EvaluatePuzzleCompletion();
    }

    private void ApplyConsumption(TideTile destinationTile)
    {
        if (!enableConsumption || destinationTile == null || destinationTile.IsSealed)
        {
            return;
        }

        destinationTile.ApplyTake(consumptionAmount);
    }

    private void ApplyGreedCoinYield(TideTile destinationTile)
    {
        if (destinationTile == null)
        {
            return;
        }

        PuzzleData puzzleData = overlayPuzzleData;
        if (puzzleData == null && GameStateManager.Instance != null)
        {
            puzzleData = GameStateManager.Instance.PendingPuzzleData;
        }

        if (puzzleData == null || !puzzleData.enableGreedEconomy)
        {
            return;
        }

        Vector2Int pos = destinationTile.GridPosition;
        int[] dRow = { -1, 1, 0, 0 };
        int[] dCol = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nRow = pos.y + dRow[i];
            int nCol = pos.x + dCol[i];

            if (nRow < 0 || nRow >= gridRows || nCol < 0 || nCol >= gridCols)
            {
                continue;
            }

            if (sealedTiles[nRow, nCol])
            {
                continue;
            }

            TideTile neighbor = activeTiles[nRow, nCol];
            if (neighbor == null)
            {
                continue;
            }

            int newValue = Mathf.Min(10, neighbor.CurrentTideValue + puzzleData.coinTileYield);
            int delta = newValue - neighbor.CurrentTideValue;
            if (delta > 0)
            {
                neighbor.ApplyPlace(delta);
                Debug.Log($"[TideManager] Greed coin yield: added {delta} to tile ({nCol},{nRow}).");
                return;
            }
        }
    }

    private void ApplyInstabilityDecay()
    {
        int countAbove5 = 0;
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null)
                {
                    continue;
                }

                if (!tile.IsSealed && tile.CurrentTideValue > 5)
                {
                    countAbove5++;
                }
            }
        }

        if (countAbove5 <= instabilityThreshold)
        {
            return;
        }

        int decay = countAbove5 - instabilityThreshold;
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null)
                {
                    continue;
                }

                tile.ApplyDecay(decay);
            }
        }
    }

    private void EvaluatePuzzleCompletion()
    {
        int[,] grid = new int[gridRows, gridCols];
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null)
                {
                    grid[row, col] = puzzleValues[row, col];
                    continue;
                }

                grid[row, col] = tile.CurrentTideValue;
            }
        }

        if (!winCondition.IsMet(grid, sealedTiles))
        {
            return;
        }

        puzzleSolved = true;
        if (GameStateManager.Instance != null)
        {
            if (overlayMode)
            {
                GameStateManager.Instance.SavePuzzleRuntimeState(overlayPuzzleBoxId, grid, true);
                GameStateManager.Instance.CompletePuzzleInExploration(
                    overlayPuzzleBoxId,
                    overlayIslandId,
                    overlayEncounterId,
                    overlayRestorationValue);
                StartCoroutine(FinishOverlayPuzzleRoutine());
                return;
            }

            GameStateManager.Instance.MarkPuzzleSolved();
            StartCoroutine(FlashAllTilesComplete());
            StartCoroutine(ReturnToMainSceneAfterDelay());
        }
    }

    private IEnumerator FinishOverlayPuzzleRoutine()
    {
        yield return StartCoroutine(FlashAllTilesComplete());
        OverlayPuzzleSolved?.Invoke();
    }

    private IEnumerator FlashAllTilesComplete()
    {
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null)
                {
                    continue;
                }

                if (!tile.IsSealed)
                {
                    tile.FlashComplete();
                }
            }
        }

        yield return null;

        if (renderBoardAsUi)
        {
            yield return StartCoroutine(AnimateSolvedBoardDismiss());
        }
    }

    private IEnumerator AnimateSolvedBoardDismiss()
    {
        if (!renderBoardAsUi || boardCanvasGroup == null)
        {
            yield break;
        }

        isClosingBoardUi = true;
        boardCanvasGroup.blocksRaycasts = false;
        boardCanvasGroup.interactable = false;

        float duration = Mathf.Max(0.08f, uiSolvedDismissDuration);
        float elapsed = 0f;

        Vector2 startSize = boardPanel != null ? boardPanel.sizeDelta : uiBoardPanelSize;
        Vector2 endSize = startSize * 0.82f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            boardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);

            if (boardPanel != null)
            {
                boardPanel.sizeDelta = Vector2.Lerp(startSize, endSize, eased);
                boardPanel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-24f, -6f, eased));
            }

            yield return null;
        }

        boardCanvasGroup.alpha = 0f;
    }

    private IEnumerator ReturnToMainSceneAfterDelay()
    {
        yield return new WaitForSeconds(completionReturnDelay);

        if (GameStateManager.Instance == null)
        {
            yield break;
        }

        if (GameStateManager.Instance.HasActiveFlowController)
        {
            GameStateManager.Instance.PendingPuzzleLayout = null;
            GameStateManager.Instance.PendingPuzzleData = null;
        }

        GameStateManager.Instance.ReturnToMainScene();
    }

    private bool CanReachWithinCarrySteps(TideTile startTile, TideTile endTile)
    {
        if (startTile == null || endTile == null)
        {
            return false;
        }

        if (startTile == endTile)
        {
            return false;
        }

        if (startTile.IsSealed || endTile.IsSealed)
        {
            return false;
        }

        Queue<PathNode> frontier = new Queue<PathNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Vector2Int start = startTile.GridPosition;
        Vector2Int goal = endTile.GridPosition;

        frontier.Enqueue(new PathNode(start, 0));
        visited.Add(start);

        while (frontier.Count > 0)
        {
            PathNode current = frontier.Dequeue();
            if (current.Position == goal && current.StepsTaken > 0)
            {
                return true;
            }

            if (current.StepsTaken >= maxCarrySteps)
            {
                continue;
            }

            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    if (rowOffset == 0 && colOffset == 0)
                    {
                        continue;
                    }

                    int nextRow = current.Position.y + rowOffset;
                    int nextCol = current.Position.x + colOffset;
                    if (nextRow < 0 || nextRow >= gridRows || nextCol < 0 || nextCol >= gridCols)
                    {
                        continue;
                    }

                    if (sealedTiles[nextRow, nextCol])
                    {
                        continue;
                    }

                    if (rowOffset != 0 && colOffset != 0)
                    {
                        if (sealedTiles[current.Position.y, nextCol] || sealedTiles[nextRow, current.Position.x])
                        {
                            continue;
                        }
                    }

                    Vector2Int nextPosition = new Vector2Int(nextCol, nextRow);
                    if (visited.Contains(nextPosition))
                    {
                        continue;
                    }

                    visited.Add(nextPosition);
                    frontier.Enqueue(new PathNode(nextPosition, current.StepsTaken + 1));
                }
            }
        }

        return false;
    }

    private void UpdateTileVisuals()
    {
        bool carrying = carriedAmount > 0;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile == null)
                {
                    continue;
                }

                bool isSelected = tile == carryingSource;
                bool isHovered = tile == hoveredTile;
                bool isReachable = false;
                bool isUnavailable = false;

                if (carrying && tile != carryingSource)
                {
                    if (tile.CanReceive(carriedAmount))
                    {
                        isReachable = CanReachWithinCarrySteps(carryingSource, tile);
                    }
                    else
                    {
                        isUnavailable = !tile.IsSealed;
                    }
                }

                tile.SetVisualState(isSelected, isReachable, isHovered, isUnavailable);
                SyncUiTileVisual(row, col, tile, isSelected, isReachable, isHovered, isUnavailable);
            }
        }

        UpdateUiHeader();
    }

    private void TrySetSealedTile(Vector2Int position, bool isSealed)
    {
        if (position.x < 0 || position.x >= gridCols || position.y < 0 || position.y >= gridRows)
        {
            return;
        }

        bool wasSealed = sealedTiles[position.y, position.x];

        sealedTiles[position.y, position.x] = isSealed;

        if (activeTiles[position.y, position.x] != null)
        {
            activeTiles[position.y, position.x].Configure(position, activeTiles[position.y, position.x].CurrentTideValue, isSealed);
        }

        if (isSealed == wasSealed)
        {
            return;
        }

        if (isSealed)
        {
            if (runtimeBoardRoot != null)
            {
                Transform tileTransform = runtimeBoardRoot.Find($"TideTile_{position.y}_{position.x}");
                if (tileTransform != null)
                {
                    CreateSealedTileEnemyMarker(tileTransform);
                }
            }
            return;
        }

        for (int i = sealedTileEnemyMarkers.Count - 1; i >= 0; i--)
        {
            GameObject marker = sealedTileEnemyMarkers[i];
            if (marker == null)
            {
                sealedTileEnemyMarkers.RemoveAt(i);
                continue;
            }

            if (marker.transform.parent == null)
            {
                Destroy(marker);
                sealedTileEnemyMarkers.RemoveAt(i);
                continue;
            }

            TideTile markerTile = marker.GetComponentInParent<TideTile>();
            if (markerTile != null && markerTile.GridPosition == position)
            {
                Destroy(marker);
                sealedTileEnemyMarkers.RemoveAt(i);
            }
        }
    }

    private readonly struct PathNode
    {
        public PathNode(Vector2Int position, int stepsTaken)
        {
            Position = position;
            StepsTaken = stepsTaken;
        }

        public Vector2Int Position { get; }
        public int StepsTaken { get; }
    }

    private static int[,] CloneGrid(int[,] source)
    {
        if (source == null || source.GetLength(0) < 1 || source.GetLength(1) < 1)
        {
            return null;
        }

        int rows = source.GetLength(0);
        int cols = source.GetLength(1);
        int[,] clone = new int[rows, cols];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                clone[row, col] = source[row, col];
            }
        }

        return clone;
    }

    private void ApplyRuntimeLayout(int[,] runtimeGrid)
    {
        if (runtimeGrid == null || runtimeGrid.GetLength(0) != gridRows || runtimeGrid.GetLength(1) != gridCols)
        {
            return;
        }

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                puzzleValues[row, col] = Mathf.Clamp(runtimeGrid[row, col], 1, 10);
            }
        }
    }

    private void SyncUiTileVisual(
        int row,
        int col,
        TideTile tile,
        bool isSelected,
        bool isReachable,
        bool isHovered,
        bool isUnavailable)
    {
        if (!renderBoardAsUi)
        {
            return;
        }

        UiTileView view = uiTileViews[row, col];
        if (view == null || view.Background == null || view.Label == null)
        {
            return;
        }

        int value = tile.CurrentTideValue;
        bool sealedTile = tile.IsSealed;
        Color baseColor = GetUiBaseColorForTile(value, sealedTile);
        Color displayColor = baseColor;

        if (isUnavailable)
        {
            displayColor = Color.Lerp(displayColor, uiUnavailableColor, 0.45f);
        }

        if (isReachable)
        {
            displayColor = Color.Lerp(displayColor, uiReachableColor, 0.5f);
        }

        if (isHovered)
        {
            displayColor = Color.Lerp(displayColor, Color.white, 0.28f);
        }

        if (isSelected)
        {
            displayColor = Color.Lerp(displayColor, uiSelectedColor, 0.72f);
        }

        view.Background.color = displayColor;
        view.Label.text = sealedTile ? "X" : value.ToString();
        view.Label.color = sealedTile ? new Color(0.95f, 0.95f, 0.98f, 1f) : new Color(0.08f, 0.1f, 0.14f, 1f);

        if (view.Button != null)
        {
            view.Button.interactable = !puzzleSolved;
        }
    }

    private Color GetUiBaseColorForTile(int value, bool sealedTile)
    {
        if (sealedTile)
        {
            return uiSealedColor;
        }

        if (value == 5)
        {
            return uiTargetColor;
        }

        if (value > 5)
        {
            float t = Mathf.InverseLerp(6f, 10f, value);
            return Color.Lerp(uiHighColor * 0.88f, uiHighColor, t);
        }

        float d = Mathf.InverseLerp(4f, 1f, value);
        return Color.Lerp(uiLowColor * 0.92f, uiLowColor * 0.74f, d);
    }

    private void UpdateUiHeader()
    {
        if (!renderBoardAsUi || boardHeaderLabel == null)
        {
            return;
        }

        string carryText = carriedAmount > 0 ? $"Carry {carriedAmount}" : "Carry -";
        string targetText = BuildGoalHeaderText();
        string modeText = overlayMode ? "Esc: Exit Overlay" : "Reset available";
        boardHeaderLabel.text = $"TIDE STABILIZATION\n{targetText}   |   {carryText}   |   {modeText}";
    }

    private string BuildGoalHeaderText()
    {
        int targetValue = winCondition != null ? winCondition.targetValue : 5;
        if (winCondition == null || winCondition.type == WinConditionType.AllEqualToTarget)
        {
            return $"Goal: stabilize all open tiles to {targetValue}";
        }

        int requiredPercent = Mathf.RoundToInt(Mathf.Clamp01(winCondition.requiredPercent) * 100f);
        return $"Goal: stabilize {requiredPercent}% of open tiles to {targetValue}";
    }
}
