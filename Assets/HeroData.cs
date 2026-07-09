using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "TIDE/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key for save/load (e.g. hero_fire).")]
    public string heroId;

    [Tooltip("Human-readable display name (e.g. Ember).")]
    public string displayName;

    [Tooltip("Marks the main character whose element is chosen at game start.")]
    public bool isMainCharacter;

    [Header("Element")]
    [Tooltip("Default element. Overridden at runtime if this is the main character.")]
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

    [Range(0f, 1f)]
    [Tooltip("Base crit rate (0-1). Combined with level scaling at runtime.")]
    public float baseCritRate = 0.05f;

    [Min(0f)]
    [Tooltip("Base crit damage multiplier. 1.5 = 150% damage on crit.")]
    public float baseCritDamage = 1.5f;

    [Header("Visuals")]
    [Tooltip("Portrait sprite shown in HUD, menus, and dialogue.")]
    public Sprite portrait;

    [Tooltip("Optional animator controller for character-specific animations.")]
    public RuntimeAnimatorController animatorController;

    [Header("Starter Skills")]
    public SkillData[] starterSkills = System.Array.Empty<SkillData>();

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(heroId)
            && !string.IsNullOrEmpty(displayName)
            && (isMainCharacter || element != CombatUnit.Element.None)
            && baseMaxHP > 0;
    }
}
