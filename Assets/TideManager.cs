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
    private PuzzleWinCondition winCondition = new PuzzleWinCondition();

    private Transform runtimeBoardRoot;
    private TideTile hoveredTile;
    private TideTile carryingSource;
    private int carriedAmount;
    private bool puzzleSolved;

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

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                puzzleValues[row, col] = layout[row, col];
                sealedTiles[row, col] = sealedTile.x >= 0 && sealedTile.y >= 0
                    && row == sealedTile.y && col == sealedTile.x;
            }
        }

        sealedPosition = sealedTile;
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

        sealedTiles = new bool[3, 3];
        if (data.HasSealedTile)
        {
            sealedTiles[data.sealedPosition.y, data.sealedPosition.x] = true;
        }

        if (data.HasLockedTile)
        {
            bool lockedCleared = IslandRestorationTracker.Instance != null
                && IslandRestorationTracker.Instance.HasClearedEncounter(lockedEncounterId);

            if (!lockedCleared)
            {
                sealedTiles[data.lockedPosition.y, data.lockedPosition.x] = true;
            }
        }
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
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleData);
            GameStateManager.Instance.PendingPuzzleData = null;
        }
        else if (GameStateManager.Instance != null && GameStateManager.Instance.PendingPuzzleLayout != null)
        {
            InitializePuzzle(GameStateManager.Instance.PendingPuzzleLayout, GameStateManager.Instance.PendingPuzzleSealedTile);
            GameStateManager.Instance.PendingPuzzleLayout = null;
        }

        GenerateBoard();
        StoreInitialValues();
        UpdateTileVisuals();

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
            }
        }
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

        if (!winCondition.IsMet(grid, sealedPosition))
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
