using UnityEngine;

public enum SkillTarget
{
    SingleEnemy,
    AllEnemies,
    Self,
    SingleAlly
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
    public float damageMultiplier;
    public SkillTarget target;
}
