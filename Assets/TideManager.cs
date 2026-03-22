using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TideManager : MonoBehaviour
{
    [Header("Interaction")]
    public LayerMask tileLayer = ~0;
    [SerializeField] private int maxCarrySteps = 2;

    [Header("Board Generation")]
    [SerializeField] private Vector3 boardCenter = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float tileSpacing = 2.25f;
    [SerializeField] private Vector3 tileScale = new Vector3(1.9f, 0.35f, 1.9f);

    [Header("Completion Flow")]
    [SerializeField] private float completionReturnDelay = 1f;

    [Header("Instability Decay")]
    [SerializeField] private int instabilityThreshold = 3;

    [Header("Sealed Tile Combat")]
    [SerializeField] private string fallbackSealedTileEncounterId = "encounter_imp_trio";
    [SerializeField] private float sealedTileCombatRestorationValue = 0.001f;

    private readonly TideTile[,] activeTiles = new TideTile[3, 3];
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
    private TideTile hoveredTile;
    private TideTile carryingSource;
    private int carriedAmount;
    private bool puzzleSolved;
    private bool isStartingSealedTileCombat;

    public int CarriedAmount => carriedAmount;
    public bool IsCarrying => carriedAmount > 0;

    public event Action OnCarriedAmountChanged;
    public event Action OnPuzzleReset;

    public void InitializePuzzle(int[,] layout, Vector2Int sealedTile)
    {
        if (layout == null || layout.GetLength(0) != 3 || layout.GetLength(1) != 3)
        {
            Debug.LogWarning("[TideManager] Invalid layout provided. Using default.");
            return;
        }

        puzzleValues = new int[3, 3];
        sealedTiles = new bool[3, 3];
        sealedPosition = new Vector2Int(-1, -1);
        lockedPosition = new Vector2Int(-1, -1);
        lockedEncounterId = string.Empty;
        lockedEncounterIslandId = puzzleIslandId;
        winCondition = new PuzzleWinCondition();

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                puzzleValues[row, col] = layout[row, col];
            }
        }

        if (sealedTile.x < 0 || sealedTile.y < 0 || sealedTile.x >= 3 || sealedTile.y >= 3)
        {
            return;
        }

        lockedPosition = sealedTile;
        lockedEncounterId = BuildLegacyLockedEncounterId(sealedTile);

        if (!IsLockedEncounterCleared())
        {
            TrySetSealedTile(sealedTile, true);
        }
    }

    public void InitializePuzzle(PuzzleData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[TideManager] Null PuzzleData provided. Using default.");
            return;
        }

        puzzleValues = data.GetGrid();
        sealedPosition = data.sealedPosition;
        winCondition = data.winCondition ?? new PuzzleWinCondition();
        instabilityThreshold = data.instabilityThreshold;
        lockedPosition = data.lockedPosition;
        lockedEncounterId = data.lockedTileEncounterId;
        lockedEncounterIslandId = data.lockedTileIslandId;

        if (string.IsNullOrEmpty(puzzleIslandId))
        {
            puzzleIslandId = lockedEncounterIslandId;
        }

        sealedTiles = new bool[3, 3];
        if (data.HasSealedTile)
        {
            TrySetSealedTile(data.sealedPosition, true);
        }

        if (data.HasLockedTile)
        {
            bool lockedCleared = IsLockedEncounterCleared();

            if (!lockedCleared)
            {
                TrySetSealedTile(data.lockedPosition, true);
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
        if (!string.IsNullOrEmpty(islandScope))
        {
            return IslandRestorationTracker.Instance.HasClearedEncounter(islandScope, lockedEncounterId);
        }

        return IslandRestorationTracker.Instance.HasClearedEncounter(lockedEncounterId);
    }

    private string GetPuzzleIslandIdForLookup()
    {
        if (!string.IsNullOrEmpty(lockedEncounterIslandId))
        {
            return lockedEncounterIslandId;
        }

        if (!string.IsNullOrEmpty(puzzleIslandId))
        {
            return puzzleIslandId;
        }

        return string.Empty;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != GameStateManager.PuzzleSceneName)
        {
            enabled = false;
            return;
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.PendingPuzzleData != null)
        {
            puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleData);
        }
        else if (GameStateManager.Instance != null && GameStateManager.Instance.PendingPuzzleLayout != null)
        {
            puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleLayout, GameStateManager.Instance.PendingPuzzleSealedTile);
        }
        else
        {
            if (GameStateManager.Instance != null)
            {
                puzzleIslandId = GameStateManager.Instance.PendingPuzzleIslandId;
            }

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

    private void Update()
    {
        hoveredTile = GetHoveredTile();

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

    private void GenerateBoard()
    {
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

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
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

    private void CreateSealedTileEnemyMarker(Transform tileTransform)
    {
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

    private void StoreInitialValues()
    {
        initialPuzzleValues = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
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
        }

        carryingSource = null;
        carriedAmount = 0;
        OnCarriedAmountChanged?.Invoke();

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (!activeTiles[row, col].IsSealed)
                {
                    activeTiles[row, col].currentTideValue = initialPuzzleValues[row, col];
                    activeTiles[row, col].RefreshVisuals();
                }
            }
        }

        OnPuzzleReset?.Invoke();
    }

    private Vector3 GetWorldPosition(int row, int col)
    {
        float xOffset = (col - 1) * tileSpacing;
        float zOffset = (1 - row) * tileSpacing;
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
        if (!Input.GetMouseButtonDown(0) || hoveredTile == null)
        {
            return;
        }

        if (hoveredTile.IsSealed)
        {
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
        if (string.IsNullOrEmpty(islandScope))
        {
            islandScope = "default";
        }

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
            if (encounterConfig != null)
            {
                Debug.LogWarning($"[TideManager] Missing sealed tile encounter '{completionEncounterId}'. Falling back to '{fallbackSealedTileEncounterId}'.");
            }
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
        if (!Input.GetMouseButtonDown(0) || hoveredTile == null)
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
        carriedAmount = 0;
        carryingSource = null;
        OnCarriedAmountChanged?.Invoke();
        ApplyInstabilityDecay();
        EvaluatePuzzleCompletion();
    }

    private void ApplyInstabilityDecay()
    {
        int countAbove5 = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                TideTile tile = activeTiles[row, col];
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
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                activeTiles[row, col].ApplyDecay(decay);
            }
        }
    }

    private void EvaluatePuzzleCompletion()
    {
        int[,] grid = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = activeTiles[row, col].CurrentTideValue;
            }
        }

        if (!winCondition.IsMet(grid, sealedTiles))
        {
            return;
        }

        puzzleSolved = true;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkPuzzleSolved();
            StartCoroutine(FlashAllTilesComplete());
            StartCoroutine(ReturnToMainSceneAfterDelay());
        }
    }

    private IEnumerator FlashAllTilesComplete()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (!activeTiles[row, col].IsSealed)
                {
                    activeTiles[row, col].FlashComplete();
                }
            }
        }

        yield return null;
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
                    if (nextRow < 0 || nextRow >= 3 || nextCol < 0 || nextCol >= 3)
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

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                TideTile tile = activeTiles[row, col];
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
            }
        }
    }

    private void TrySetSealedTile(Vector2Int position, bool isSealed)
    {
        if (position.x < 0 || position.x >= 3 || position.y < 0 || position.y >= 3)
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
}
