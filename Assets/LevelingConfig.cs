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
        int normalizedLevel = Mathf.Max(1, currentLevel);
        if (normalizedLevel >= maxLevel) return 0;
        long required = (long)Mathf.Max(1, baseXpToLevel)
            + (long)(normalizedLevel - 1) * Mathf.Max(0, xpPerLevelIncrement);
        return (int)System.Math.Min(int.MaxValue, required);
    }

    public int GetTotalXpForLevel(int level)
    {
        // Sum the authored arithmetic curve without walking an untrusted level
        // count. Levels above the cap cannot require additional XP.
        int normalizedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        decimal steps = normalizedLevel - 1;
        decimal total = steps * Mathf.Max(1, baseXpToLevel)
            + steps * (steps - 1) / 2 * Mathf.Max(0, xpPerLevelIncrement);
        return (int)System.Math.Min(int.MaxValue, total);
    }

    public int GetExpectedStatGrowth(int currentLevel, int targetLevel)
    {
        if (targetLevel <= currentLevel)
        {
            return 0;
        }
        long levelSpan = (long)targetLevel - currentLevel;
        decimal perLevel = (long)Mathf.Max(0, hpPerLevel) + Mathf.Max(0, attackPerLevel)
            + Mathf.Max(0, defensePerLevel) + Mathf.Max(0, speedPerLevel) + Mathf.Max(0, mpPerLevel);
        return (int)System.Math.Min(int.MaxValue, levelSpan * perLevel);
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
            && xpPerLevelIncrement >= 0
            && !float.IsNaN(reserveXpMultiplier) && !float.IsInfinity(reserveXpMultiplier)
            && reserveXpMultiplier >= 0f && reserveXpMultiplier <= 1f;
    }
}
