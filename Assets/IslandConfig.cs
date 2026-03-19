using System;
using UnityEngine;

public enum EncounterType
{
    Combat,
    Puzzle
}

[CreateAssetMenu(fileName = "IslandConfig", menuName = "TIDE/Island Config")]
public class IslandConfig : ScriptableObject
{
    public string viceName = "Greed";
    public Color vicePrimaryColor = new Color(1f, 0.84f, 0f);
    public Color viceSecondaryColor = new Color(0.55f, 0.41f, 0.08f);

    [Tooltip("Ordered list of encounters. For 5 subsections, provide 10 entries: Combat, Puzzle, Combat, Puzzle, ...")]
    public EncounterDefinition[] encounters;
}

[Serializable]
public class EncounterDefinition
{
    public EncounterType type;
    [Range(0f, 1f)]
    public float restorationValue;
    public EnemyComposition enemyComposition;
    public PuzzleLayout puzzleLayout;
}

[Serializable]
public class EnemyComposition
{
    public string[] names = Array.Empty<string>();
    public CombatUnit.Element[] elements = Array.Empty<CombatUnit.Element>();
    public int[] attackModifiers = Array.Empty<int>();
    public int[] defenseModifiers = Array.Empty<int>();
    public int[] maxHpModifiers = Array.Empty<int>();

    public int Count => names.Length;

    public static EnemyComposition Create(string[] names, CombatUnit.Element[] elements,
        int[] atkMods, int[] defMods, int[] hpMods)
    {
        return new EnemyComposition
        {
            names = names,
            elements = elements,
            attackModifiers = atkMods,
            defenseModifiers = defMods,
            maxHpModifiers = hpMods
        };
    }
}

[Serializable]
public class PuzzleLayout
{
    [Tooltip("3x3 grid of Tide values (row-major order). Must contain exactly 9 values.")]
    public int[] values = { 9, 1, 10, 7, 5, 2, 5, 3, 3 };

    [Tooltip("Row of the sealed (impassable) tile.")]
    public int sealedRow = 1;

    [Tooltip("Column of the sealed (impassable) tile.")]
    public int sealedCol = 1;

    [Tooltip("Row of the locked tile. Set to -1 if no locked tile. Locked tiles become normal after combat.")]
    public int lockedRow = -1;

    [Tooltip("Column of the locked tile. Set to -1 if no locked tile.")]
    public int lockedCol = -1;

    public int[,] GetGrid()
    {
        int[,] grid = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = values[row * 3 + col];
            }
        }

        return grid;
    }

    public Vector2Int GetSealedPosition()
    {
        return new Vector2Int(sealedCol, sealedRow);
    }

    public Vector2Int GetLockedPosition()
    {
        return new Vector2Int(lockedCol, lockedRow);
    }

    public bool HasLockedTile => lockedRow >= 0 && lockedCol >= 0;

    public static PuzzleLayout Create(int[] values, int sealedRow, int sealedCol, int lockedRow = -1, int lockedCol = -1)
    {
        return new PuzzleLayout
        {
            values = values,
            sealedRow = sealedRow,
            sealedCol = sealedCol,
            lockedRow = lockedRow,
            lockedCol = lockedCol
        };
    }
}
