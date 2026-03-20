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

        if (total == 0)
        {
            return true;
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
    [Tooltip("Tide values for each tile in row-major order. Must contain exactly gridRows * gridCols values.")]
    public int[] tileValues = { 5, 5, 5, 5, 5, 5, 5, 5, 5 };

    [Tooltip("Position of the sealed (impassable) tile. Set to (-1,-1) if none.")]
    public Vector2Int sealedPosition = new Vector2Int(-1, -1);

    [Tooltip("Position of the locked tile (sealed until combat cleared). Set to (-1,-1) if none.")]
    public Vector2Int lockedPosition = new Vector2Int(-1, -1);

    [Tooltip("Encounter ID that must be cleared to unlock the locked tile. Leave empty if no locked tile.")]
    public string lockedTileEncounterId = "";

    [Tooltip("Win condition for this puzzle.")]
    public PuzzleWinCondition winCondition = new PuzzleWinCondition();

    [Tooltip("Number of tiles above 5 allowed before instability decay kicks in.")]
    public int instabilityThreshold = 3;

    public int[,] GetGrid()
    {
        int[,] grid =
        {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };

        if (tileValues == null || tileValues.Length < 9)
        {
            Debug.LogWarning("[PuzzleData] tileValues has fewer than 9 entries. Using default 5s.");
            return grid;
        }

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = Mathf.Clamp(tileValues[row * 3 + col], 1, 10);
            }
        }

        return grid;
    }

    public bool HasSealedTile => sealedPosition.x >= 0 && sealedPosition.y >= 0;
    public bool HasLockedTile => lockedPosition.x >= 0 && lockedPosition.y >= 0;
}
