using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "TB_Ability", menuName = "TIDE/TideBreak Ability")]
public class TideBreakData : ScriptableObject
{
    [Header("Info")]
    public string abilityName;
    [TextArea] public string description;
    
    [Header("Combat")]
    public float damageMultiplier = 2f;
    public SkillTarget targetType = SkillTarget.AllEnemies; // SingleEnemy or AllEnemies

    [Header("Element")]
    [Tooltip("Element this TideBreak belongs to. Matches CombatUnit.Element values (1=Fire, 2=Water, 3=Earth, 4=Air, 5=Space).")]
    public int element; // 0=None, 1=Fire, 2=Water, 3=Earth, 4=Air, 5=Space

    [Header("Unlock")]
    public int unlockLevel = 1;

    private static TideBreakData[] allCached;

    public static List<TideBreakData> GetForElement(int elementId, int heroLevel)
    {
        if (allCached == null)
        {
            allCached = Resources.LoadAll<TideBreakData>("TideBreakData");
        }

        if (allCached == null || allCached.Length == 0)
        {
            return new List<TideBreakData>();
        }

        return allCached
            .Where(tb => tb != null && tb.element == elementId && tb.unlockLevel <= heroLevel)
            .ToList();
    }

    public static void ClearCache()
    {
        allCached = null;
    }
}