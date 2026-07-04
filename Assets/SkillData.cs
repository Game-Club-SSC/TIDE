using UnityEngine;

public enum SkillTarget
{
    SingleEnemy,
    AllEnemies,
    Self,
    SingleAlly,
    AllAllies
}

[CreateAssetMenu(fileName = "SkillData", menuName = "TIDE/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Skill Info")]
    public string skillName;
    [TextArea(2, 4)]
    public string description;

    [Header("Cost")]
    public int mpCost;

    [Header("Combat")]
    [Min(0f)]
    [Tooltip("Multiplier applied to base damage. 1.0 = normal, 2.0 = double damage.")]
    public float damageMultiplier = 1f;
    [Range(0f, 1f)]
    [Tooltip("Percent of damage dealt restored to the caster after this skill resolves.")]
    public float restoreCasterPercentOfDamage = 0f;
    [Min(0f)]
    [Tooltip("Multiplier applied to base damage as healing to the target (for ally-targeting heal skills). 0 = no heal, 1.5 = 150% of base as heal.")]
    public float healMultiplier = 0f;
    public SkillTarget target;

    [Header("Element")]
    [Tooltip("Element used for elemental matchup. Falls back to the actor's element when set to None.")]
    public CombatUnit.Element element = CombatUnit.Element.None;

    [Header("Status Effect")]
    public StatusEffectType appliedEffectType = StatusEffectType.None;
    public int effectDuration = 0;
    public float effectMagnitude = 0f;

    [Header("Greed Economy")]
    [Min(0)]
    [Tooltip("Amount of currency stolen from the player when an enemy uses this skill.")]
    public int currencyStealAmount = 0;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(skillName)
            && mpCost >= 0
            && damageMultiplier >= 0f
            && restoreCasterPercentOfDamage >= 0f
            && restoreCasterPercentOfDamage <= 1f
            && healMultiplier >= 0f;
    }
}
