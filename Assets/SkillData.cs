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
    [Min(0f)]
    [Tooltip("Multiplier applied to base damage. 1.0 = normal, 2.0 = double damage.")]
    public float damageMultiplier = 1f;
    public SkillTarget target;
}
