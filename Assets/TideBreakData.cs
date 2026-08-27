using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "TB_Ability", menuName = "TIDE/TideBreak Ability")]
public class TideBreakData : ScriptableObject
{
    [Header("Info")]
    public string abilityName;
    [TextArea] public string description;
    [Tooltip("Optional heroId this TideBreak belongs to. Empty = shared.")]
    public string heroId;
    
    [Header("Combat")]
    public float damageMultiplier = 2f;
    public SkillTarget targetType = SkillTarget.AllEnemies; // SingleEnemy or AllEnemies

    [Header("Element")]
    [Tooltip("Element this TideBreak belongs to. Matches CombatUnit.Element values (1=Fire, 2=Water, 3=Earth, 4=Air, 5=Space).")]
    public int element; // 0=None, 1=Fire, 2=Water, 3=Earth, 4=Air, 5=Space

    [Header("Unlock")]
    public int unlockLevel = 1;

    [Tooltip("Element the hero must match to unlock this TideBreak. Empty string = any element.")]
    public string requiredHeroElement = "";

    [TextArea(1, 3)]
    [Tooltip("Human-readable description shown to the player (e.g. 'Reach Level 5').")]
    public string unlockDescription = "";

    [Tooltip("If true, this TideBreak is hidden until revealed by an ancient text.")]
    public bool isHidden;

    private static TideBreakData[] allCached;

    public static List<TideBreakData> GetForElement(int elementId, int heroLevel)
    {
        if (elementId < (int)CombatUnit.Element.Fire || elementId > (int)CombatUnit.Element.Space)
        {
            return new List<TideBreakData>();
        }

        if (allCached == null)
        {
            allCached = Resources.LoadAll<TideBreakData>("TideBreakData");
        }

        if (allCached == null || allCached.Length == 0)
        {
            return new List<TideBreakData>();
        }

        return allCached
            .Where(tb => tb != null
                && tb.IsValid()
                && !tb.isHidden
                && tb.element == elementId
                && tb.unlockLevel <= Mathf.Max(1, heroLevel))
            .ToList();
    }

    public static void ClearCache()
    {
        allCached = null;
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(abilityName)
            && !float.IsNaN(damageMultiplier)
            && !float.IsInfinity(damageMultiplier)
            && damageMultiplier >= 0f
            && (int)targetType >= (int)SkillTarget.SingleEnemy
            && (int)targetType <= (int)SkillTarget.AllAllies
            && element >= (int)CombatUnit.Element.None
            && element <= (int)CombatUnit.Element.Space
            && unlockLevel >= 1;
    }
}
