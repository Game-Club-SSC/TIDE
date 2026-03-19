using UnityEngine;

public enum SkillTarget
{
    SingleEnemy,
    AllEnemies,
    Self,
    SingleAlly
}

[System.Serializable]
public class SkillData
{
    public string skillName;
    public int mpCost;
    public float damageMultiplier;
    public SkillTarget target;

    public SkillData(string name, int mpCost, float multiplier, SkillTarget target)
    {
        this.skillName = name;
        this.mpCost = mpCost;
        this.damageMultiplier = multiplier;
        this.target = target;
    }

    public static SkillData PowerStrike => new SkillData("Power Strike", 8, 1.5f, SkillTarget.SingleEnemy);
    public static SkillData ArcaneBlast => new SkillData("Arcane Blast", 12, 1.8f, SkillTarget.SingleEnemy);
    public static SkillData QuickShot => new SkillData("Quick Shot", 5, 1.2f, SkillTarget.SingleEnemy);
}
