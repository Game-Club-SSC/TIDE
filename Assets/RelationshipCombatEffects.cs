using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Static class that provides combat modifiers based on hero relationship (bond) levels.
/// Reads bond data from <see cref="DialogueSystem"/> and returns multipliers that
/// BattleManager applies during damage resolution.
///
/// Design from v2 GDD:
/// Low bonds  (0-20)  = poor team dynamic, team ups and skills don't work as well.
/// Medium bonds (21-60) = normal team dynamic.
/// High bonds  (61-80)  = good team dynamic, slight bonus.
/// Very high bonds (81-100) = strong synergy, significant bonus.
/// </summary>
public static class RelationshipCombatEffects
{
    // ------------------------------------------------------------------ //
    //  Bond tier thresholds
    // ------------------------------------------------------------------ //

    private const int LowBondMax = 20;
    private const int MediumBondMax = 60;
    private const int HighBondMax = 80;
    // 81-100 = Very High

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns a damage multiplier based on the average bond level between all living ally heroes.
    /// Low bonds (0-20):   0.8x  (team doesn't work well together)
    /// Medium bonds (21-60): 1.0x (normal)
    /// High bonds (61-80):  1.1x  (slight bonus)
    /// Very high bonds (81-100): 1.25x (strong synergy)
    /// </summary>
    public static float GetTeamDamageMultiplier(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return 0.8f;
        if (avg <= MediumBondMax) return 1.0f;
        if (avg <= HighBondMax) return 1.1f;
        return 1.25f;
    }

    /// <summary>
    /// Returns a defense multiplier based on the average bond level between all living ally heroes.
    /// Same scale as damage multiplier.
    /// </summary>
    public static float GetTeamDefenseMultiplier(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return 0.8f;
        if (avg <= MediumBondMax) return 1.0f;
        if (avg <= HighBondMax) return 1.1f;
        return 1.25f;
    }

    /// <summary>
    /// Returns a healing multiplier based on the average bond level between all living ally heroes.
    /// High bonds make healing more effective; low bonds make it less effective.
    /// </summary>
    public static float GetTeamHealingMultiplier(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return 0.8f;
        if (avg <= MediumBondMax) return 1.0f;
        if (avg <= HighBondMax) return 1.15f;
        return 1.3f;
    }

    /// <summary>
    /// Returns a tide break damage multiplier based on the average bond level.
    /// Tide breaks are the team's ultimate technique -- bonds matter most here.
    /// </summary>
    public static float GetTideBreakMultiplier(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return 0.75f;
        if (avg <= MediumBondMax) return 1.0f;
        if (avg <= HighBondMax) return 1.15f;
        return 1.35f;
    }

    /// <summary>
    /// Returns a bonus to clash QTE success chance.
    /// Higher bonds = better coordination = easier to win clashes.
    /// Value is added to the base clash chance (0.0 to 0.3).
    /// </summary>
    public static float GetClashBonus(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return -0.1f;
        if (avg <= MediumBondMax) return 0.0f;
        if (avg <= HighBondMax) return 0.1f;
        return 0.2f;
    }

    /// <summary>
    /// Returns the probability (0-1) of team-up attacks triggering.
    /// Higher bonds = more frequent spontaneous team-up attacks.
    /// </summary>
    public static float GetTeamUpChance(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);
        if (avg <= LowBondMax)  return 0.0f;
        if (avg <= MediumBondMax) return 0.05f;
        if (avg <= HighBondMax) return 0.12f;
        return 0.2f;
    }

    /// <summary>
    /// Returns a human-readable description of the current team dynamic.
    /// Shown in the battle HUD at the start of combat.
    /// </summary>
    public static string GetTeamDynamicDescription(IReadOnlyList<CombatUnit> allies)
    {
        float avg = ComputeAverageBondLevel(allies);

        if (avg <= LowBondMax)
        {
            return $"Team Dynamic: Strained ({avg:F0} bond). Damage and defense reduced. Teamwork falters.";
        }

        if (avg <= MediumBondMax)
        {
            return $"Team Dynamic: Stable ({avg:F0} bond). The team fights competently together.";
        }

        if (avg <= HighBondMax)
        {
            return $"Team Dynamic: Strong ({avg:F0} bond). Bonds fuel coordination. Slight bonuses to damage and defense.";
        }

        return $"Team Dynamic: Unbreakable ({avg:F0} bond). Deep trust amplifies every action. Significant combat bonuses.";
    }

    // ------------------------------------------------------------------ //
    //  Bond calculation
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Computes the average bond level across all unique pairs of living allies.
    /// Returns 50 (neutral) if no DialogueSystem is available or no allies exist.
    /// </summary>
    private static float ComputeAverageBondLevel(IReadOnlyList<CombatUnit> allies)
    {
        if (allies == null || allies.Count < 2)
        {
            return 50f;
        }

        DialogueSystem dialogue = DialogueSystem.Instance;
        if (dialogue == null)
        {
            return 50f;
        }

        int totalBond = 0;
        int pairCount = 0;

        for (int i = 0; i < allies.Count; i++)
        {
            CombatUnit a = allies[i];
            if (a == null || !a.IsAlive)
            {
                continue;
            }

            for (int j = i + 1; j < allies.Count; j++)
            {
                CombatUnit b = allies[j];
                if (b == null || !b.IsAlive)
                {
                    continue;
                }

                totalBond += dialogue.GetBondLevel(a.UnitName, b.UnitName);
                pairCount++;
            }
        }

        if (pairCount == 0)
        {
            return 50f;
        }

        return (float)totalBond / pairCount;
    }
}
