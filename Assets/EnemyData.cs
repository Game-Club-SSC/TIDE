using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "TIDE/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key for lookup (e.g. enemy_imp).")]
    public string enemyId;

    [Tooltip("Human-readable display name (e.g. Imp).")]
    public string displayName;

    [Header("Element")]
    public CombatUnit.Element element = CombatUnit.Element.None;

    [Header("Base Stats")]
    [Min(1)]
    public int baseMaxHP = 100;

    [Min(0)]
    public int baseMaxMP = 50;

    [Min(0)]
    public int baseAttack = 10;

    [Min(0)]
    public int baseDefense = 5;

    [Min(0)]
    public int baseSpeed = 10;

    [Header("Skills")]
    public SkillData[] skills = System.Array.Empty<SkillData>();

    [Header("XP Reward")]
    [Tooltip("XP awarded to the party when this enemy is defeated.")]
    [Min(0)]
    public int xpReward = 25;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(enemyId)
            && !string.IsNullOrEmpty(displayName)
            && element != CombatUnit.Element.None
            && baseMaxHP > 0;
    }
}
