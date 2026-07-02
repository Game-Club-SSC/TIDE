using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central balance configuration for TIDE. Holds every tuning knob that
/// affects element matchups, stat curves, boss scaling, Tide Break damage,
/// and difficulty modifiers.
/// </summary>
[CreateAssetMenu(fileName = "BalanceConfig", menuName = "TIDE/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    // ---------------------------------------------------------------
    // 1. Element Advantage Multipliers
    // ---------------------------------------------------------------

    [Header("Element Advantage")]
    [Tooltip("Damage multiplier when attacker has element advantage. " +
             "1.5x = 50% bonus. Range 1.2x-2.0x recommended.")]
    [Range(1.2f, 2.0f)]
    public float elementStrongMultiplier = 1.5f;

    [Tooltip("Damage multiplier when attacker has element disadvantage. " +
             "0.67x = 33% reduction. Range 0.4x-0.8x recommended.")]
    [Range(0.4f, 0.8f)]
    public float elementWeakMultiplier = 0.67f;

    [Tooltip("Multiplier when elements are neutral. Should always be 1.0.")]
    public float elementNeutralMultiplier = 1.0f;

    [Tooltip("Maximum allowed ratio between strong and weak multipliers. " +
             "Prevents either end from feeling insignificant.")]
    [Range(1.5f, 4.0f)]
    public float elementRatioCeiling = 2.5f;

    // ---------------------------------------------------------------
    // 2. Stat Growth Curves
    // ---------------------------------------------------------------

    [Header("Hero Stat Growth Per Level")]
    [Tooltip("Base HP gain per level. Scales linearly to level 30.")]
    [Min(1)]
    public int hpPerLevel = 8;

    [Tooltip("Base MP gain per level.")]
    [Min(1)]
    public int mpPerLevel = 3;

    [Tooltip("Base Attack gain per level.")]
    [Min(1)]
    public int attackPerLevel = 2;

    [Tooltip("Base Defense gain per level.")]
    [Min(1)]
    public int defensePerLevel = 1;

    [Tooltip("Base Speed gain per level.")]
    [Min(1)]
    public int speedPerLevel = 1;

    [Tooltip("Percentage bonus applied to stat growth from level 16 onward " +
             "to keep late-game rewarding. 1.15 = +15% growth in the back half.")]
    [Range(1.0f, 1.5f)]
    public float lateGameGrowthMultiplier = 1.15f;

    [Tooltip("Level at which the late-game growth bonus kicks in.")]
    [Min(2)]
    public int lateGameThresholdLevel = 16;

    [Tooltip("Maximum hero level. Issue #265 targets 1-30.")]
    [Min(2)]
    public int maxHeroLevel = 30;

    [Tooltip("Base XP required for level 2. Each subsequent level adds xpPerLevelIncrement.")]
    [Min(10)]
    public int baseXpToLevel = 120;

    [Tooltip("Additional XP required per level above 2.")]
    [Min(10)]
    public int xpPerLevelIncrement = 60;

    [Tooltip("XP budget per island to control how many levels a player gains per island. " +
             "120-320 yields 1-3 levels depending on current level.")]
    public int islandXpBudget = 320;

    // ---------------------------------------------------------------
    // 3. Boss Scaling Per Island Tier
    // ---------------------------------------------------------------

    [Header("Boss Scaling")]
    [Tooltip("Base boss HP at tier 1 (first island).")]
    [Min(100)]
    public int bossBaseHp = 600;

    [Tooltip("Base boss attack at tier 1.")]
    [Min(10)]
    public int bossBaseAttack = 40;

    [Tooltip("Base boss defense at tier 1.")]
    [Min(5)]
    public int bossBaseDefense = 15;

    [Tooltip("HP multiplier at the final tier. Lerp from 1.0 to this value.")]
    [Range(1.2f, 3.0f)]
    public float bossHpScaleCeiling = 1.65f;

    [Tooltip("Attack multiplier at the final tier. Lerp from 1.0 to this value.")]
    [Range(1.0f, 2.5f)]
    public float bossAttackScaleCeiling = 1.50f;

    [Tooltip("Defense multiplier at the final tier. Lerp from 1.0 to this value.")]
    [Range(1.0f, 2.5f)]
    public float bossDefenseScaleCeiling = 1.40f;

    [Tooltip("Boss HP pool should require this many Tide Break combos to defeat on Standard. " +
             "Range 3-6 keeps fights tense without dragging.")]
    [Min(2)]
    public int bossTideBreakCombosToKill = 4;

    // ---------------------------------------------------------------
    // 4. Tide Break Damage Formulas
    // ---------------------------------------------------------------

    [Header("Tide Break")]
    [Tooltip("Base damage multiplier for player Tide Break. " +
             "Applied to attacker's ATK before element/defense scaling.")]
    [Range(1.5f, 3.0f)]
    public float tideBreakPlayerMultiplier = 2.0f;

    [Tooltip("Base damage multiplier for enemy Tide Break.")]
    [Range(1.5f, 3.0f)]
    public float tideBreakEnemyMultiplier = 2.0f;

    [Tooltip("Minimum number of momentum shifts before Tide Break becomes available. " +
             "Higher = slower build, more earned.")]
    [Min(3)]
    public int momentumThresholdForTideBreak = 5;

    [Tooltip("Variance range for Tide Break damage (0.1 = +/-10%).")]
    [Range(0f, 0.3f)]
    public float tideBreakVariance = 0.2f;

    // ---------------------------------------------------------------
    // 5. Gear Set Bonus Limits
    // ---------------------------------------------------------------

    [Header("Gear Set Bonuses")]
    [Tooltip("Maximum combined percent bonus (base + set) for any single stat " +
             "from a full gear set. Prevents any stat from being too dominant.")]
    [Range(0.10f, 0.50f)]
    public float maxGearSetBonusPerStat = 0.30f;

    [Tooltip("Tier multiplier applied to gear set bonuses. Higher tier = slightly better bonuses.")]
    [Range(1.0f, 1.5f)]
    public float gearTierScaling = 1.1f;

    // ---------------------------------------------------------------
    // 6. Difficulty Mode Modifiers
    // ---------------------------------------------------------------

    [Header("Difficulty - Player Modifiers")]
    [Tooltip("Player damage multiplier on Story mode.")]
    [Range(0.8f, 2.0f)]
    public float storyPlayerDamageMultiplier = 1.2f;

    [Tooltip("Player damage multiplier on Standard mode (baseline = 1.0).")]
    [Range(0.8f, 1.2f)]
    public float standardPlayerDamageMultiplier = 1.0f;

    [Tooltip("Player damage multiplier on Hardcore mode.")]
    [Range(0.5f, 1.0f)]
    public float hardcorePlayerDamageMultiplier = 0.8f;

    [Header("Difficulty - Enemy Modifiers")]
    [Tooltip("Enemy damage multiplier on Story mode.")]
    [Range(0.3f, 1.0f)]
    public float storyEnemyDamageMultiplier = 0.7f;

    [Tooltip("Enemy damage multiplier on Standard mode (baseline = 1.0).")]
    [Range(0.8f, 1.2f)]
    public float standardEnemyDamageMultiplier = 1.0f;

    [Tooltip("Enemy damage multiplier on Hardcore mode.")]
    [Range(1.0f, 2.0f)]
    public float hardcoreEnemyDamageMultiplier = 1.35f;

    [Header("Difficulty - Economy Modifiers")]
    [Tooltip("XP multiplier on Story mode.")]
    [Range(0.5f, 1.5f)]
    public float storyXpMultiplier = 0.8f;

    [Tooltip("XP multiplier on Standard mode.")]
    [Range(0.8f, 1.2f)]
    public float standardXpMultiplier = 1.0f;

    [Tooltip("XP multiplier on Hardcore mode.")]
    [Range(1.0f, 3.0f)]
    public float hardcoreXpMultiplier = 1.5f;

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns the stat growth for a given level, applying the late-game bonus
    /// when the hero reaches lateGameThresholdLevel.
    /// </summary>
    public StatGrowth GetStatGrowthAtLevel(int level)
    {
        float multiplier = level >= lateGameThresholdLevel ? lateGameGrowthMultiplier : 1f;
        return new StatGrowth
        {
            hp = Mathf.RoundToInt(hpPerLevel * multiplier),
            mp = Mathf.RoundToInt(mpPerLevel * multiplier),
            attack = Mathf.RoundToInt(attackPerLevel * multiplier),
            defense = Mathf.RoundToInt(defensePerLevel * multiplier),
            speed = Mathf.RoundToInt(speedPerLevel * multiplier)
        };
    }

    /// <summary>
    /// Sums up total stat growth from level 1 to targetLevel.
    /// Useful for previewing how strong a hero should be at a given level.
    /// </summary>
    public StatGrowth GetCumulativeStatGrowth(int targetLevel)
    {
        StatGrowth total = default;
        int cap = Mathf.Min(targetLevel, maxHeroLevel);
        for (int level = 2; level <= cap; level++)
        {
            StatGrowth growth = GetStatGrowthAtLevel(level);
            total.hp += growth.hp;
            total.mp += growth.mp;
            total.attack += growth.attack;
            total.defense += growth.defense;
            total.speed += growth.speed;
        }
        return total;
    }

    /// <summary>
    /// XP required to advance from currentLevel to currentLevel + 1.
    /// </summary>
    public int GetXpToNextLevel(int currentLevel)
    {
        if (currentLevel >= maxHeroLevel) return 0;
        return baseXpToLevel + (currentLevel - 1) * xpPerLevelIncrement;
    }

    /// <summary>
    /// Total XP needed to reach the given level from level 1.
    /// </summary>
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

    /// <summary>
    /// Computes the boss stat multiplier for a given island tier (1-based).
    /// Uses separate ceilings for HP, ATK, and DEF to allow tuning independently.
    /// </summary>
    public BossStats GetBossStatsForTier(int tier, int totalTiers)
    {
        if (totalTiers <= 1) totalTiers = 2;
        float t = Mathf.Clamp01((tier - 1) / (float)(totalTiers - 1));

        return new BossStats
        {
            hp = Mathf.RoundToInt(bossBaseHp * Mathf.Lerp(1f, bossHpScaleCeiling, t)),
            attack = Mathf.RoundToInt(bossBaseAttack * Mathf.Lerp(1f, bossAttackScaleCeiling, t)),
            defense = Mathf.RoundToInt(bossBaseDefense * Mathf.Lerp(1f, bossDefenseScaleCeiling, t))
        };
    }

    /// <summary>
    /// Returns the element matchup multiplier. Intended to replace direct reads
    /// of ElementMatchup.StrongMultiplier / WeakMultiplier so that all tuning
    /// flows through this single config.
    /// </summary>
    public float GetElementMultiplier(MatchupResult result)
    {
        switch (result)
        {
            case MatchupResult.Strong: return elementStrongMultiplier;
            case MatchupResult.Weak: return elementWeakMultiplier;
            default: return elementNeutralMultiplier;
        }
    }

    /// <summary>
    /// Returns the Tide Break multiplier for the given unit type.
    /// </summary>
    public float GetTideBreakMultiplier(bool isPlayer)
    {
        return isPlayer ? tideBreakPlayerMultiplier : tideBreakEnemyMultiplier;
    }

    /// <summary>
    /// Returns player damage multiplier for the given difficulty.
    /// </summary>
    public float GetPlayerDamageMultiplierForDifficulty(DifficultyModeService.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case DifficultyModeService.Difficulty.Story: return storyPlayerDamageMultiplier;
            case DifficultyModeService.Difficulty.Hardcore: return hardcorePlayerDamageMultiplier;
            default: return standardPlayerDamageMultiplier;
        }
    }

    /// <summary>
    /// Returns enemy damage multiplier for the given difficulty.
    /// </summary>
    public float GetEnemyDamageMultiplierForDifficulty(DifficultyModeService.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case DifficultyModeService.Difficulty.Story: return storyEnemyDamageMultiplier;
            case DifficultyModeService.Difficulty.Hardcore: return hardcoreEnemyDamageMultiplier;
            default: return standardEnemyDamageMultiplier;
        }
    }

    /// <summary>
    /// Returns XP multiplier for the given difficulty.
    /// </summary>
    public float GetXpMultiplierForDifficulty(DifficultyModeService.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case DifficultyModeService.Difficulty.Story: return storyXpMultiplier;
            case DifficultyModeService.Difficulty.Hardcore: return hardcoreXpMultiplier;
            default: return standardXpMultiplier;
        }
    }

    /// <summary>
    /// Returns the effective gear bonus percent, clamped to maxGearSetBonusPerStat.
    /// </summary>
    public float GetClampedGearBonus(float rawPercent, int tier)
    {
        float scaled = rawPercent * Mathf.Pow(gearTierScaling, tier);
        return Mathf.Min(scaled, maxGearSetBonusPerStat);
    }

    // ---------------------------------------------------------------
    // Nested types
    // ---------------------------------------------------------------

    [Serializable]
    public struct StatGrowth
    {
        public int hp;
        public int mp;
        public int attack;
        public int defense;
        public int speed;

        public int Total => hp + mp + attack + defense + speed;

        public override string ToString()
        {
            return $"HP+{hp} MP+{mp} ATK+{attack} DEF+{defense} SPD+{speed} (Total {Total})";
        }
    }

    [Serializable]
    public struct BossStats
    {
        public int hp;
        public int attack;
        public int defense;

        public override string ToString()
        {
            return $"Boss: HP {hp} ATK {attack} DEF {defense}";
        }
    }
}

/// <summary>
/// Static validator that checks a BalanceConfig against acceptable ranges.
/// Call BalanceValidator.Validate(config) after loading or editing the config
/// to catch tuning mistakes before they reach the build.
/// </summary>
public static class BalanceValidator
{
    public struct ValidationReport
    {
        public bool IsValid;
        public List<string> Warnings;
        public List<string> Errors;

        public override string ToString()
        {
            if (IsValid) return "Balance config is valid.";
            string result = "Balance config has issues:\n";
            foreach (string e in Errors) result += $"  [ERROR] {e}\n";
            foreach (string w in Warnings) result += $"  [WARN]  {w}\n";
            return result;
        }
    }

    /// <summary>
    /// Validates the given BalanceConfig and returns a report.
    /// </summary>
    public static ValidationReport Validate(BalanceConfig config)
    {
        ValidationReport report = new ValidationReport
        {
            IsValid = true,
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        if (config == null)
        {
            report.Errors.Add("BalanceConfig is null.");
            report.IsValid = false;
            return report;
        }

        ValidateElementMultipliers(config, report);
        ValidateStatGrowth(config, report);
        ValidateBossScaling(config, report);
        ValidateTideBreak(config, report);
        ValidateGearBonuses(config, report);
        ValidateDifficultyModifiers(config, report);

        if (report.Errors.Count > 0)
        {
            report.IsValid = false;
        }

        return report;
    }

    private static void ValidateElementMultipliers(BalanceConfig config, ValidationReport report)
    {
        float ratio = config.elementStrongMultiplier / Mathf.Max(0.01f, config.elementWeakMultiplier);

        if (ratio < 1.5f)
        {
            report.Warnings.Add(
                $"Element ratio {ratio:F2}x is too low. Advantage won't feel meaningful. " +
                $"Recommend 2.0x-2.5x (currently strong={config.elementStrongMultiplier}x / weak={config.elementWeakMultiplier}x).");
        }

        if (ratio > config.elementRatioCeiling)
        {
            report.Errors.Add(
                $"Element ratio {ratio:F2}x exceeds ceiling of {config.elementRatioCeiling}x. " +
                "Single-element strategies will dominate.");
        }

        if (config.elementStrongMultiplier < 1.2f)
        {
            report.Warnings.Add("Strong multiplier below 1.2x may feel negligible in combat.");
        }

        if (config.elementWeakMultiplier > 0.8f)
        {
            report.Warnings.Add("Weak multiplier above 0.8x may not discourage wrong-element attacks.");
        }

        if (config.elementWeakMultiplier < 0.4f)
        {
            report.Warnings.Add("Weak multiplier below 0.4x may make wrong elements feel useless.");
        }
    }

    private static void ValidateStatGrowth(BalanceConfig config, ValidationReport report)
    {
        if (config.maxHeroLevel < 20)
        {
            report.Errors.Add($"maxHeroLevel ({config.maxHeroLevel}) must be at least 20 for issue #265.");
        }

        if (config.lateGameThresholdLevel < 2 || config.lateGameThresholdLevel >= config.maxHeroLevel)
        {
            report.Errors.Add(
                $"lateGameThresholdLevel ({config.lateGameThresholdLevel}) must be between 2 and maxHeroLevel ({config.maxHeroLevel}).");
        }

        // Check that cumulative stat growth at max level is within a sane range.
        // At level 30 with default values: HP ~280, ATK ~70, DEF ~35, SPD ~35, MP ~105
        BalanceConfig.StatGrowth maxGrowth = config.GetCumulativeStatGrowth(config.maxHeroLevel);
        if (maxGrowth.hp < 100)
        {
            report.Warnings.Add($"Cumulative HP at max level ({maxGrowth.hp}) is very low. Heroes may feel fragile.");
        }

        if (maxGrowth.hp > 600)
        {
            report.Warnings.Add($"Cumulative HP at max level ({maxGrowth.hp}) is very high. Combat may feel slow.");
        }

        // Verify XP curve is monotonically increasing
        for (int level = 2; level < Mathf.Min(10, config.maxHeroLevel); level++)
        {
            int xpA = config.GetXpToNextLevel(level - 1);
            int xpB = config.GetXpToNextLevel(level);
            if (xpB <= xpA)
            {
                report.Errors.Add($"XP curve is not increasing at level {level}: {xpA} -> {xpB}.");
                break;
            }
        }

        // Verify island XP budget yields 1-3 levels at the start
        int levelsInBudget = 0;
        int xpAccum = 0;
        for (int level = 1; level < config.maxHeroLevel && xpAccum < config.islandXpBudget; level++)
        {
            xpAccum += config.GetXpToNextLevel(level);
            if (xpAccum <= config.islandXpBudget)
            {
                levelsInBudget++;
            }
        }

        if (levelsInBudget < 1)
        {
            report.Warnings.Add($"Island XP budget ({config.islandXpBudget}) is too low for even 1 level-up at level 1.");
        }

        if (levelsInBudget > 4)
        {
            report.Warnings.Add(
                $"Island XP budget ({config.islandXpBudget}) grants {levelsInBudget} levels at the start. " +
                "May outpace content. Consider reducing budget or increasing XP thresholds.");
        }
    }

    private static void ValidateBossScaling(BalanceConfig config, ValidationReport report)
    {
        if (config.bossHpScaleCeiling < 1.2f)
        {
            report.Warnings.Add("Boss HP scale ceiling below 1.2x means final bosses barely scale.");
        }

        if (config.bossHpScaleCeiling > 3.0f)
        {
            report.Warnings.Add("Boss HP scale ceiling above 3.0x may make late bosses bullet-spongy.");
        }

        float attackScaleRatio = config.bossAttackScaleCeiling / Mathf.Max(0.01f, config.bossDefenseScaleCeiling);
        if (attackScaleRatio > 1.5f)
        {
            report.Warnings.Add(
                $"Boss ATK scales {attackScaleRatio:F2}x faster than DEF. " +
                "Late bosses may deal too much damage relative to player defense.");
        }

        if (config.bossBaseHp < 300)
        {
            report.Warnings.Add("Boss base HP below 300 may die too quickly on early islands.");
        }
    }

    private static void ValidateTideBreak(BalanceConfig config, ValidationReport report)
    {
        if (config.tideBreakPlayerMultiplier < 1.5f)
        {
            report.Warnings.Add("Player Tide Break multiplier below 1.5x may not feel like a payoff.");
        }

        if (config.tideBreakPlayerMultiplier > 3.0f)
        {
            report.Warnings.Add("Player Tide Break multiplier above 3.0x may trivialize encounters.");
        }

        if (config.momentumThresholdForTideBreak < 3)
        {
            report.Warnings.Add("Tide Break builds in fewer than 3 momentum shifts. May fire too often.");
        }

        if (config.momentumThresholdForTideBreak > 8)
        {
            report.Warnings.Add("Tide Break requires 8+ momentum shifts. Players may rarely see it.");
        }

        // Sanity check: boss should survive bossTideBreakCombosToKill Tide Breaks
        // Estimate: player ATK at level ~midgame is roughly base + cumulative growth / 2
        // This is approximate - real validation needs actual encounter data.
        int estimatedPlayerAttack = 20 + config.attackPerLevel * (config.maxHeroLevel / 2);
        float tideBreakDamagePerHit = estimatedPlayerAttack * config.tideBreakPlayerMultiplier;
        int estimatedBossMaxHp = Mathf.RoundToInt(config.bossBaseHp * config.bossHpScaleCeiling);
        int estimatedCombos = Mathf.CeilToInt(estimatedBossMaxHp / Mathf.Max(1f, tideBreakDamagePerHit));

        if (estimatedCombos < 2)
        {
            report.Warnings.Add(
                $"Estimated Tide Break combos to kill final boss: {estimatedCombos}. " +
                "Boss may die too fast. Increase bossBaseHp or reduce tideBreakPlayerMultiplier.");
        }

        if (estimatedCombos > 8)
        {
            report.Warnings.Add(
                $"Estimated Tide Break combos to kill final boss: {estimatedCombos}. " +
                "Fight may drag. Reduce bossBaseHp or increase tideBreakPlayerMultiplier.");
        }
    }

    private static void ValidateGearBonuses(BalanceConfig config, ValidationReport report)
    {
        if (config.maxGearSetBonusPerStat > 0.50f)
        {
            report.Errors.Add(
                $"maxGearSetBonusPerStat ({config.maxGearSetBonusPerStat:P0}) exceeds 50%. " +
                "Gear would overshadow level-up stat gains.");
        }

        if (config.maxGearSetBonusPerStat < 0.10f)
        {
            report.Warnings.Add("maxGearSetBonusPerStat below 10% makes gear feel insignificant.");
        }

        if (config.gearTierScaling > 1.5f)
        {
            report.Warnings.Add("gearTierScaling above 1.5x makes high-tier gear dominant over skill.");
        }

        // Check that tier-3 gear with max scaling stays within limits.
        // Raw max in current data is ~0.22 (Cosmic Lattice setAtk + atk = 0.24, clamped).
        float worstCase = 0.30f * Mathf.Pow(config.gearTierScaling, 3);
        if (worstCase > config.maxGearSetBonusPerStat)
        {
            report.Warnings.Add(
                $"Tier-3 gear bonus after scaling ({worstCase:P0}) exceeds maxGearSetBonusPerStat " +
                $"({config.maxGearSetBonusPerStat:P0}). Clamping will apply but tuning may be off.");
        }
    }

    private static void ValidateDifficultyModifiers(BalanceConfig config, ValidationReport report)
    {
        // Story should feel easier than Standard
        if (config.storyPlayerDamageMultiplier < config.standardPlayerDamageMultiplier)
        {
            report.Warnings.Add("Story player damage is lower than Standard. Story should deal more damage.");
        }

        if (config.storyEnemyDamageMultiplier > config.standardEnemyDamageMultiplier)
        {
            report.Warnings.Add("Story enemy damage is higher than Standard. Story should deal less damage.");
        }

        // Hardcore should feel harder than Standard
        if (config.hardcorePlayerDamageMultiplier > config.standardPlayerDamageMultiplier)
        {
            report.Warnings.Add("Hardcore player damage is higher than Standard. Hardcore should deal less.");
        }

        if (config.hardcoreEnemyDamageMultiplier < config.standardEnemyDamageMultiplier)
        {
            report.Warnings.Add("Hardcore enemy damage is lower than Standard. Hardcore should deal more.");
        }

        // Sanity: Hardcore shouldn't be impossibly punishing
        float hardcoreSwing = config.hardcoreEnemyDamageMultiplier / Mathf.Max(0.01f, config.hardcorePlayerDamageMultiplier);
        if (hardcoreSwing > 3.0f)
        {
            report.Warnings.Add(
                $"Hardcore damage swing ({hardcoreSwing:F1}x enemy/player ratio) is extreme. " +
                "Players may feel it's unfair rather than challenging.");
        }

        // Story shouldn't be trivial
        float storySwing = config.storyPlayerDamageMultiplier / Mathf.Max(0.01f, config.storyEnemyDamageMultiplier);
        if (storySwing > 3.0f)
        {
            report.Warnings.Add(
                $"Story damage swing ({storySwing:F1}x player/enemy ratio) is extreme. " +
                "Combat may feel inconsequential.");
        }
    }
}
