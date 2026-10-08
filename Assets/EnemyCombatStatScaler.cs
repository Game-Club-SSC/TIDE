/// <summary>Applies encounter scaling from base enemy data without compounding live stats.</summary>
public static class EnemyCombatStatScaler
{
    public static void ApplyEnemyData(CombatUnit unit, EnemyData data, float islandMultiplier, float newGamePlusMultiplier)
    {
        if (unit == null || data == null) return;
        double islandScale = CombatMath.SanitizeStatMultiplier(islandMultiplier);
        double combatScale = islandScale * CombatMath.SanitizeStatMultiplier(newGamePlusMultiplier);
        unit.UnitName = data.displayName;
        unit.ElementType = data.element;
        unit.MaxHP = CombatMath.RoundToNonNegativeInt(data.baseMaxHP * combatScale, 1);
        unit.MaxMP = CombatMath.RoundToNonNegativeInt(data.baseMaxMP * combatScale);
        unit.Attack = CombatMath.RoundToNonNegativeInt(data.baseAttack * combatScale, 1);
        unit.Defense = CombatMath.RoundToNonNegativeInt(data.baseDefense * combatScale);
        unit.Speed = CombatMath.RoundToNonNegativeInt(data.baseSpeed * combatScale, 1);
        unit.CritRate = data.baseCritRate;
        unit.CritDamage = data.baseCritDamage;
        unit.SetSkills(data.skills);
        // NG+ XP is applied once when party XP is granted. Inflating the unit
        // reward here would multiply that bonus a second time.
        unit.XpReward = CombatMath.RoundToNonNegativeInt(data.xpReward * islandScale);
        unit.Revive(unit.MaxHP);
        unit.MP = unit.MaxMP;
    }

    /// <summary>Scales a newly instantiated legacy/fallback enemy once.</summary>
    public static void ScaleSpawnedUnit(CombatUnit unit, float newGamePlusMultiplier)
    {
        if (unit == null) return;
        double scale = CombatMath.SanitizeStatMultiplier(newGamePlusMultiplier);
        unit.MaxHP = CombatMath.RoundToNonNegativeInt(unit.MaxHP * scale, 1);
        unit.MaxMP = CombatMath.RoundToNonNegativeInt(unit.MaxMP * scale);
        unit.Attack = CombatMath.RoundToNonNegativeInt(unit.Attack * scale, 1);
        unit.Defense = CombatMath.RoundToNonNegativeInt(unit.Defense * scale);
        unit.Speed = CombatMath.RoundToNonNegativeInt(unit.Speed * scale, 1);
        unit.Revive(unit.MaxHP);
        unit.MP = unit.MaxMP;
    }
}
