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
    public string islandId = "island_lust";
    public string viceName = "Greed";
    public Color vicePrimaryColor = new Color(1f, 0.84f, 0f);
    public Color viceSecondaryColor = new Color(0.55f, 0.41f, 0.08f);

    [Tooltip("Ordered list of encounters for the current slice. Typical layout is 4 combat/puzzle pairs followed by a boss combat.")]
    public EncounterDefinition[] encounters;
}

[Serializable]
public class EncounterDefinition
{
    public string encounterId = "";
    public EncounterType type;
    [Tooltip("Marks this encounter as a boss encounter for progression gating.")]
    public bool isBossEncounter;
    [Range(0f, 1f)]
    public float restorationValue;
    public EncounterConfig encounterConfig;
    public EnemyComposition enemyComposition;
    public PuzzleData puzzleData;
}

[Serializable]
public class EnemyComposition
{
    [Tooltip("Direct EnemyData references. Preferred over modifier arrays.")]
    public EnemyData[] enemyDataSlots = Array.Empty<EnemyData>();

    public string[] names = Array.Empty<string>();
    public CombatUnit.Element[] elements = Array.Empty<CombatUnit.Element>();
    public int[] attackModifiers = Array.Empty<int>();
    public int[] defenseModifiers = Array.Empty<int>();
    public int[] maxHpModifiers = Array.Empty<int>();

    public bool HasEnemyDataSlots
    {
        get
        {
            if (enemyDataSlots == null) return false;
            for (int i = 0; i < enemyDataSlots.Length; i++)
            {
                if (enemyDataSlots[i] != null) return true;
            }
            return false;
        }
    }

    public int Count
    {
        get
        {
            if (HasEnemyDataSlots) return enemyDataSlots.Length;
            return Math.Min(Math.Min(Math.Min(Math.Min(names.Length, elements.Length), attackModifiers.Length), defenseModifiers.Length), maxHpModifiers.Length);
        }
    }

    public bool IsValidIndex(int index)
    {
        if (HasEnemyDataSlots)
        {
            return index >= 0 && index < enemyDataSlots.Length;
        }

        return index >= 0
            && index < names.Length
            && index < elements.Length
            && index < attackModifiers.Length
            && index < defenseModifiers.Length
            && index < maxHpModifiers.Length;
    }

    public EnemyData GetEnemyData(int index)
    {
        if (enemyDataSlots == null || index < 0 || index >= enemyDataSlots.Length)
        {
            return null;
        }

        return enemyDataSlots[index];
    }

    public static EnemyComposition FromEncounterConfig(EncounterConfig config)
    {
        if (config == null || config.enemies == null)
        {
            return new EnemyComposition();
        }

        return new EnemyComposition
        {
            enemyDataSlots = config.enemies
        };
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

    public string GetName(int index)
    {
        if (!IsValidIndex(index)) return null;
        return names[index];
    }

    public CombatUnit.Element GetElement(int index)
    {
        if (!IsValidIndex(index)) return CombatUnit.Element.Fire;
        return elements[index];
    }

    public int GetAttackModifier(int index)
    {
        if (!IsValidIndex(index)) return 0;
        return attackModifiers[index];
    }

    public int GetDefenseModifier(int index)
    {
        if (!IsValidIndex(index)) return 0;
        return defenseModifiers[index];
    }

    public int GetMaxHpModifier(int index)
    {
        if (!IsValidIndex(index)) return 0;
        return maxHpModifiers[index];
    }
}
