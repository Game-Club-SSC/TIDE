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

    [Min(1)]
    public int baseMaxMP = 1;

    [Min(1)]
    public int baseAttack = 1;

    [Min(1)]
    public int baseDefense = 1;

    [Min(1)]
    public int baseSpeed = 1;

    // Backing field for baseCritRate; serialized privately.
    [SerializeField] [Range(0f, 1f)]
    [Tooltip("Base crit rate (0-1).")]
    private float baseCritRateValue = 0.03f;

    // BUGFIX: property now clamps to [0, 1] on set, matching CombatUnit.CritRate
    public float baseCritRate
    {
        get => baseCritRateValue; // read the raw serialized value
        set => baseCritRateValue = Mathf.Clamp01(value); // clamp to valid probability range
    }

    // Backing field for baseCritDamage; serialized privately.
    [SerializeField] [Min(0f)]
    [Tooltip("Base crit damage multiplier. 1.5 = 150% damage on crit.")]
    private float baseCritDamageValue = 1.5f;

    // BUGFIX: property now floors at 1f on set (1x = no crit penalty),
    // matching CombatUnit.CritDamage and CritStatsTest expectations
    public float baseCritDamage
    {
        get => baseCritDamageValue; // read the raw serialized value
        set => baseCritDamageValue = Mathf.Max(1f, value); // floor at 1x (enemies never get a crit penalty)
    }

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
