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
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        int total = 0;
        int met = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (sealedPosition.x >= 0 && sealedPosition.y >= 0
                    && col == sealedPosition.x && row == sealedPosition.y)
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

    [Tooltip("Win condition for this puzzle.")]
    public PuzzleWinCondition winCondition = new PuzzleWinCondition();

    public int[,] GetGrid()
    {
        int[,] grid = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = tileValues[row * 3 + col];
            }
        }

        return grid;
    }

    public bool HasSealedTile => sealedPosition.x >= 0 && sealedPosition.y >= 0;
    public bool HasLockedTile => lockedPosition.x >= 0 && lockedPosition.y >= 0;
}
