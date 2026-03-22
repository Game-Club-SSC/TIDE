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
    
    [Header("Unlock")]
    public int unlockLevel = 1;
}