using UnityEngine;

[System.Serializable]
public class HeroSkillUnlock
{
    [Tooltip("Normal skill granted when the hero reaches this level.")]
    public SkillData skill;

    [Min(1)]
    [Tooltip("Hero level needed to use this normal skill.")]
    public int unlockLevel = 1;

    public bool IsValid()
    {
        return skill != null && skill.IsValid() && unlockLevel >= 1;
    }
}

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

    [Header("Normal Skill Unlocks")]
    [Tooltip("Optional skills added at the listed hero level. Leave empty to keep this legacy hero's starter skills unchanged.")]
    public HeroSkillUnlock[] normalSkillUnlocks = System.Array.Empty<HeroSkillUnlock>();

    public SkillData[] GetSkillsForLevel(int level)
    {
        int resolvedLevel = Mathf.Max(1, level);
        var result = new System.Collections.Generic.List<SkillData>();

        AddUniqueValidSkills(result, starterSkills);

        if (normalSkillUnlocks != null)
        {
            for (int i = 0; i < normalSkillUnlocks.Length; i++)
            {
                HeroSkillUnlock unlock = normalSkillUnlocks[i];
                if (unlock == null || unlock.unlockLevel > resolvedLevel)
                {
                    continue;
                }

                AddUniqueValidSkill(result, unlock.skill);
            }
        }

        return result.ToArray();
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(heroId)
            && !string.IsNullOrEmpty(displayName)
            && (isMainCharacter || element != CombatUnit.Element.None)
            && baseMaxHP > 0
            && HasValidNormalSkillUnlocks();
    }

    private bool HasValidNormalSkillUnlocks()
    {
        if (normalSkillUnlocks == null)
        {
            return false;
        }

        var seenSkills = new System.Collections.Generic.HashSet<SkillData>();
        for (int i = 0; i < normalSkillUnlocks.Length; i++)
        {
            HeroSkillUnlock unlock = normalSkillUnlocks[i];
            if (unlock == null || !unlock.IsValid() || !seenSkills.Add(unlock.skill))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddUniqueValidSkills(System.Collections.Generic.List<SkillData> target, SkillData[] skills)
    {
        if (skills == null)
        {
            return;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            AddUniqueValidSkill(target, skills[i]);
        }
    }

    private static void AddUniqueValidSkill(System.Collections.Generic.List<SkillData> target, SkillData skill)
    {
        if (target == null || skill == null || !skill.IsValid() || target.Contains(skill))
        {
            return;
        }

        target.Add(skill);
    }
}
