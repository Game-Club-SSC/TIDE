using UnityEngine;

[CreateAssetMenu(fileName = "GearSetData", menuName = "TIDE/Gear Set Data")]
public class GearSetData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key for lookup (e.g. iron_guard).")]
    public string setId;

    [Tooltip("Human-readable display name (e.g. Iron Guard Set).")]
    public string displayName;

    [Tooltip("Element this gear set is themed for. None = universal.")]
    public CombatUnit.Element element = CombatUnit.Element.None;

    [Tooltip("Tier for progression gating. Higher = unlocked later.")]
    [Min(0)]
    public int tier;

    [TextArea]
    public string description;

    [Header("Base Stat Bonuses (%)")]
    [Tooltip("Percentage bonus to Attack (0.10 = 10%).")]
    [Range(0f, 1f)]
    public float attackBonusPercent;

    [Tooltip("Percentage bonus to Defense (0.10 = 10%).")]
    [Range(0f, 1f)]
    public float defenseBonusPercent;

    [Tooltip("Percentage bonus to MaxHP (0.10 = 10%).")]
    [Range(0f, 1f)]
    public float hpBonusPercent;

    [Header("Full Set Bonus (%)")]
    [Tooltip("Additional % to Attack when full set is equipped.")]
    [Range(0f, 1f)]
    public float setBonusAttackPercent;

    [Tooltip("Additional % to Defense when full set is equipped.")]
    [Range(0f, 1f)]
    public float setBonusDefensePercent;

    [Tooltip("Additional % to MaxHP when full set is equipped.")]
    [Range(0f, 1f)]
    public float setBonusHpPercent;

    [TextArea]
    public string setBonusDescription;

    public float TotalAttackPercent => attackBonusPercent + setBonusAttackPercent;
    public float TotalDefensePercent => defenseBonusPercent + setBonusDefensePercent;
    public float TotalHpPercent => hpBonusPercent + setBonusHpPercent;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(setId)
            && !string.IsNullOrEmpty(displayName);
    }

    public bool MatchesElement(CombatUnit.Element heroElement)
    {
        return element == CombatUnit.Element.None || element == heroElement;
    }
}
