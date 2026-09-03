using UnityEngine;

public enum WinConditionType
{
    AllEqualToTarget,
    PercentageAtTarget
}

[System.Serializable]
public class PuzzleWinCondition
{
    public WinConditionType type = WinConditionType.AllEqualToTarget;
    public int targetValue = 5;
    [Range(0f, 1f)]
    public float requiredPercent = 1f;

    public bool IsMet(int[,] grid, Vector2Int sealedPosition)
    {
        if (grid == null)
        {
            return false;
        }

        bool[,] sealedTiles = new bool[grid.GetLength(0), grid.GetLength(1)];
        if (IsWithinGrid(sealedPosition, grid.GetLength(0), grid.GetLength(1)))
        {
            sealedTiles[sealedPosition.y, sealedPosition.x] = true;
        }

        return IsMet(grid, sealedTiles);
    }

    public bool IsMet(int[,] grid, bool[,] sealedTiles)
    {
        if (grid == null)
        {
            return false;
        }

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        int total = 0;
        int met = 0;
        bool useSealedMap = sealedTiles != null
            && sealedTiles.GetLength(0) == rows
            && sealedTiles.GetLength(1) == cols;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (useSealedMap && sealedTiles[row, col])
                {
                    continue;
                }

                total++;
                if (grid[row, col] == targetValue)
                {
                    met++;
                }
            }
        }

        // BUGFIX: Return false (not true) when total == 0. Previously, a puzzle
        // with all tiles sealed would be vacuously "solved", auto-clearing
        // degenerate puzzles instead of treating them as incomplete.
        if (total == 0)
        {
            return false;
        }

        switch (type)
        {
            case WinConditionType.AllEqualToTarget:
                return met == total;
            case WinConditionType.PercentageAtTarget:
                return (float)met / total >= requiredPercent;
            default:
                return false;
        }
    }

    private static bool IsWithinGrid(Vector2Int position, int rows, int cols)
    {
        return position.x >= 0 && position.x < cols
            && position.y >= 0 && position.y < rows;
    }
}

[CreateAssetMenu(fileName = "New Puzzle", menuName = "TIDE/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    [Header("Grid Dimensions")]
    [Tooltip("Number of columns in the puzzle grid.")]
    public int gridCols = 4;

    [Tooltip("Number of rows in the puzzle grid.")]
    public int gridRows = 4;

    [Tooltip("Tide values for each tile in row-major order. Must contain exactly gridRows * gridCols. Use 0 for sealed/empty slots.")]
    public int[] tileValues = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

    [Header("Sealed Tiles")]
    [Tooltip("Positions of sealed (impassable) tiles. These tiles cannot be interacted with.")]
    public Vector2Int[] sealedPositions = new Vector2Int[0];

    [Tooltip("(Legacy) Single sealed position. Prefer sealedPositions array for multiple sealed tiles.")]
    public Vector2Int sealedPosition = new Vector2Int(-1, -1);

    [Header("Locked Tile")]
    [Tooltip("Position of the locked tile (sealed until combat cleared). Set to (-1,-1) if none.")]
    public Vector2Int lockedPosition = new Vector2Int(-1, -1);

    [Tooltip("Encounter ID that must be cleared to unlock the locked tile. Leave empty if no locked tile.")]
    public string lockedTileEncounterId = "";

    [Tooltip("Optional island ID scope for locked-tile encounter checks. Leave empty to use active island context.")]
    public string lockedTileIslandId = "";

    [Header("Win Condition")]
    [Tooltip("Win condition for this puzzle.")]
    public PuzzleWinCondition winCondition = new PuzzleWinCondition();

    [Header("Decay")]
    [Min(0)]
    [Tooltip("Number of tiles above 5 allowed before instability decay kicks in.")]
    public int instabilityThreshold = 3;

    [Header("Greed Consumption")]
    [Tooltip("When enabled, Greed consumption removes tide after a valid placement.")]
    public bool enableConsumption;

    [Min(1)]
    [Tooltip("How much tide is consumed from the placed tile after a valid move.")]
    public int consumptionAmount = 1;

    [Header("Greed Economy")]
    [Tooltip("When enabled, Greed economy adds coin yield to an adjacent tile after a valid placement.")]
    public bool enableGreedEconomy;

    [Min(1)]
    [Tooltip("How much tide is added to one orthogonally-adjacent tile when Greed economy is active.")]
    public int coinTileYield = 2;

    /// <summary>
    /// Returns all sealed positions, combining the legacy single position with the array.
    /// </summary>
    public Vector2Int[] GetAllSealedPositions()
    {
        var result = new System.Collections.Generic.List<Vector2Int>();

        if (sealedPositions != null)
        {
            result.AddRange(sealedPositions);
        }

        // Include legacy single position if valid and not already in array
        if (sealedPosition.x >= 0 && sealedPosition.y >= 0)
        {
            bool found = false;
            foreach (var pos in result)
            {
                if (pos == sealedPosition) { found = true; break; }
            }
            if (!found) result.Add(sealedPosition);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Returns a 2D bool array marking sealed tiles. Dimensions match the resolved grid size.
    /// </summary>
    public bool[,] GetSealedMap()
    {
        Vector2Int dimensions = GetResolvedGridDimensions();
        int resolvedRows = dimensions.y;
        int resolvedCols = dimensions.x;
        long cellCount = (long)resolvedRows * resolvedCols;
        if (resolvedRows <= 0 || resolvedCols <= 0 || cellCount > int.MaxValue)
        {
            Debug.LogWarning($"[PuzzleData] Invalid grid dimensions ({resolvedCols}x{resolvedRows}). Returning an empty sealed map.");
            return new bool[0, 0];
        }

        bool[,] map = new bool[resolvedRows, resolvedCols];
        Vector2Int[] positions = GetAllSealedPositions();

        foreach (var pos in positions)
        {
            if (pos.y >= 0 && pos.y < resolvedRows && pos.x >= 0 && pos.x < resolvedCols)
            {
                map[pos.y, pos.x] = true;
            }
        }

        // Zero is the serialized sentinel for a sealed/empty slot. Treat it as
        // part of the sealed map as well as the explicit position lists so the
        // logic board, traversal, and win condition all agree on tile state.
        if (tileValues != null && tileValues.LongLength >= cellCount)
        {
            for (int row = 0; row < resolvedRows; row++)
            {
                for (int col = 0; col < resolvedCols; col++)
                {
                    if (tileValues[row * resolvedCols + col] == 0)
                    {
                        map[row, col] = true;
                    }
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Returns the grid as a 2D int array. Values are clamped 1-10; sealed slots become 0.
    /// </summary>
    public int[,] GetGrid()
    {
        Vector2Int dimensions = GetResolvedGridDimensions();
        int resolvedRows = dimensions.y;
        int resolvedCols = dimensions.x;
        long cellCount = (long)resolvedRows * resolvedCols;
        if (resolvedRows <= 0 || resolvedCols <= 0 || cellCount > int.MaxValue)
        {
            Debug.LogWarning($"[PuzzleData] Invalid grid dimensions ({resolvedCols}x{resolvedRows}). Returning an empty grid.");
            return new int[0, 0];
        }

        int[,] grid = new int[resolvedRows, resolvedCols];

        if (tileValues == null || tileValues.LongLength < cellCount)
        {
            Debug.LogWarning($"[PuzzleData] tileValues has fewer than {cellCount} entries. Filling with 5s.");
            for (int r = 0; r < resolvedRows; r++)
                for (int c = 0; c < resolvedCols; c++)
                    grid[r, c] = 5;
            return grid;
        }

        bool[,] sealedMap = GetSealedMap();

        for (int row = 0; row < resolvedRows; row++)
        {
            for (int col = 0; col < resolvedCols; col++)
            {
                int val = tileValues[row * resolvedCols + col];
                if (sealedMap[row, col] || val == 0)
                {
                    grid[row, col] = 0; // sealed/empty
                }
                else
                {
                    grid[row, col] = Mathf.Clamp(val, 1, 10);
                }
            }
        }

        return grid;
    }

    public bool HasSealedTile => (sealedPositions != null && sealedPositions.Length > 0)
        || (sealedPosition.x >= 0 && sealedPosition.y >= 0)
        || HasImplicitSealedSlot();
    public bool HasLockedTile => lockedPosition.x >= 0 && lockedPosition.y >= 0;

    public Vector2Int GetResolvedGridDimensions()
    {
        // Canonical 3x3 assets created before dynamic dimensions were added
        // have nine values but inherit the newer 4x4 field defaults.
        if (gridRows == 4 && gridCols == 4 && tileValues != null && tileValues.Length == 9)
        {
            return new Vector2Int(3, 3);
        }

        return new Vector2Int(gridCols, gridRows);
    }

    public bool IsValid()
    {
        Vector2Int dimensions = GetResolvedGridDimensions();
        if (dimensions.x <= 0 || dimensions.y <= 0 || tileValues == null)
        {
            return false;
        }

        long expectedValues = (long)dimensions.x * dimensions.y;
        return expectedValues <= int.MaxValue && tileValues.LongLength >= expectedValues;
    }

    private bool HasImplicitSealedSlot()
    {
        if (tileValues == null)
        {
            return false;
        }

        Vector2Int dimensions = GetResolvedGridDimensions();
        long cellCount = (long)dimensions.x * dimensions.y;
        if (dimensions.x <= 0
            || dimensions.y <= 0
            || cellCount > int.MaxValue
            || tileValues.LongLength < cellCount)
        {
            return false;
        }

        for (int i = 0; i < (int)cellCount; i++)
        {
            if (tileValues[i] == 0)
            {
                return true;
            }
        }

        return false;
    }
}
