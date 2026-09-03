using UnityEngine;

[CreateAssetMenu(fileName = "LevelingConfig", menuName = "TIDE/Leveling Config")]
public class LevelingConfig : ScriptableObject
{
    [Header("XP Requirements")]
    [Tooltip("Base XP needed for level 2. Each subsequent level adds xpPerLevelIncrement.")]
    [Min(1)]
    public int baseXpToLevel = 120;

    [Tooltip("Additional XP required per level above 2.")]
    [Min(0)]
    public int xpPerLevelIncrement = 60;

    [Header("Stat Growth Per Level")]
    [Min(0)]
    public int hpPerLevel = 8;

    [Min(0)]
    public int mpPerLevel = 3;

    [Min(0)]
    public int attackPerLevel = 2;

    [Min(0)]
    public int defensePerLevel = 1;

    [Min(0)]
    public int speedPerLevel = 1;

    [Header("XP Rewards")]
    [Tooltip("Active party members gain 100% of enemy XP. Reserve members gain this fraction.")]
    [Range(0f, 1f)]
    public float reserveXpMultiplier = 0.5f;

    [Header("Level Cap")]
    [Min(2)]
    public int maxLevel = 20;

    public int GetXpToNextLevel(int currentLevel)
    {
        if (currentLevel >= maxLevel) return 0;
        return baseXpToLevel + (currentLevel - 1) * xpPerLevelIncrement;
    }

    public int GetTotalXpForLevel(int level)
    {
        if (level <= 1) return 0;
        int total = 0;
        for (int i = 2; i <= level; i++)
        {
            total += GetXpToNextLevel(i - 1);
        }
        return total;
    }

    public int GetExpectedStatGrowth(int currentLevel, int targetLevel)
    {
        if (targetLevel <= currentLevel)
        {
            return 0;
        }
        int levelSpan = Mathf.Max(0, targetLevel - currentLevel);
        return levelSpan * (hpPerLevel + attackPerLevel + defensePerLevel + speedPerLevel + mpPerLevel);
    }

    public bool IsValid()
    {
        return baseXpToLevel > 0
            && maxLevel >= 2
            && hpPerLevel >= 0
            && mpPerLevel >= 0
            && attackPerLevel >= 0
            && defensePerLevel >= 0
            && speedPerLevel >= 0
            // BUGFIX: Added xpPerLevelIncrement >= 0 check. The field has a
            // [Min(0)] attribute on the serialized property, but IsValid()
            // was missing this validation, so a corrupted/inspected negative
            // value could slip through without being caught.
            && xpPerLevelIncrement >= 0;
    }
}
