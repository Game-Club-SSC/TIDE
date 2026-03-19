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
    public string islandId = "island_1";
    public string viceName = "Greed";
    public Color vicePrimaryColor = new Color(1f, 0.84f, 0f);
    public Color viceSecondaryColor = new Color(0.55f, 0.41f, 0.08f);

    [Tooltip("Ordered list of encounters. For 5 subsections, provide 10 entries: Combat, Puzzle, Combat, Puzzle, ...")]
    public EncounterDefinition[] encounters;
}

[Serializable]
public class EncounterDefinition
{
    public string encounterId = "";
    public EncounterType type;
    [Range(0f, 1f)]
    public float restorationValue;
    public EnemyComposition enemyComposition;
    public PuzzleData puzzleData;
}

[Serializable]
public class EnemyComposition
{
    public string[] names = Array.Empty<string>();
    public CombatUnit.Element[] elements = Array.Empty<CombatUnit.Element>();
    public int[] attackModifiers = Array.Empty<int>();
    public int[] defenseModifiers = Array.Empty<int>();
    public int[] maxHpModifiers = Array.Empty<int>();

    public int Count => Math.Min(Math.Min(Math.Min(Math.Min(names.Length, elements.Length), attackModifiers.Length), defenseModifiers.Length), maxHpModifiers.Length);

    public bool IsValidIndex(int index)
    {
        return index >= 0
            && index < names.Length
            && index < elements.Length
            && index < attackModifiers.Length
            && index < defenseModifiers.Length
            && index < maxHpModifiers.Length;
    }

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
