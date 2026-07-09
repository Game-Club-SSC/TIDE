using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnvyMirrorService
{
    private static bool mirrorEnabled;
    private static CombatUnit.Element mirroredElement = CombatUnit.Element.None;
    private static float skillDamageMultiplier = 0.7f;
    private static bool subscribedToSceneUnload;

    public static bool IsMirrorEnabled => mirrorEnabled;
    public static CombatUnit.Element MirroredElement => mirroredElement;
    public static float SkillDamageMultiplier => skillDamageMultiplier;

    public static void SetMirrorEnabled(bool enabled)
    {
        EnsureSceneUnloadSubscription();
        mirrorEnabled = enabled;
    }

    public static void SetMirroredElement(CombatUnit.Element element)
    {
        EnsureSceneUnloadSubscription();
        mirroredElement = element;
    }

    public static void SetSkillDamageMultiplier(float multiplier)
    {
        EnsureSceneUnloadSubscription();
        skillDamageMultiplier = Mathf.Max(0f, multiplier);
    }

    public static CombatUnit.Element GetMirrorElementFor(CombatUnit.Element lastPlayerElement)
    {
        if (!mirrorEnabled)
        {
            return lastPlayerElement;
        }

        return mirroredElement != CombatUnit.Element.None ? mirroredElement : lastPlayerElement;
    }

    public static SkillData GetMirrorSkillFor(SkillData lastSkill, float damageMultiplier)
    {
        if (lastSkill == null)
        {
            return null;
        }

        SkillData mirror = ScriptableObject.CreateInstance<SkillData>();
        mirror.skillName = $"Mirror_{lastSkill.skillName}";
        mirror.description = lastSkill.description;
        mirror.mpCost = Mathf.Max(1, Mathf.RoundToInt(lastSkill.mpCost * 0.5f));
        mirror.damageMultiplier = lastSkill.damageMultiplier * Mathf.Max(0f, damageMultiplier);
        mirror.target = lastSkill.target;
        mirror.appliedEffectType = lastSkill.appliedEffectType;
        mirror.effectDuration = lastSkill.effectDuration;
        mirror.effectMagnitude = lastSkill.effectMagnitude;
        mirror.element = lastSkill.element;
        mirror.name = $"MirrorSkill_{lastSkill.skillName}";
        return mirror;
    }

    public static void ReleaseMirrorSkill(SkillData mirrorSkill)
    {
        if (mirrorSkill != null)
        {
            Object.Destroy(mirrorSkill);
        }
    }

    public static void ResetForDebug()
    {
        mirrorEnabled = false;
        mirroredElement = CombatUnit.Element.None;
        skillDamageMultiplier = 0.7f;
    }

    private static void EnsureSceneUnloadSubscription()
    {
        if (subscribedToSceneUnload)
        {
            return;
        }

        SceneManager.sceneUnloaded += OnSceneUnloaded;
        subscribedToSceneUnload = true;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        mirrorEnabled = false;
        mirroredElement = CombatUnit.Element.None;
        skillDamageMultiplier = 0.7f;
    }
}
