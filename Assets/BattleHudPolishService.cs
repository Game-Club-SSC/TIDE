using UnityEngine;

public static class BattleHudPolishService
{
    public static Color GetCritFlashColor()
    {
        return new Color(1f, 0.93f, 0.42f, 1f);
    }

    public static float GetCritFlashDuration()
    {
        return 0.4f;
    }

    public static Color GetStatusEffectIconColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.BuffAttack: return new Color(1f, 0.45f, 0.2f, 1f);
            case StatusEffectType.BuffDefense: return new Color(0.4f, 0.7f, 1f, 1f);
            case StatusEffectType.DebuffAttack: return new Color(0.5f, 0.5f, 0.5f, 1f);
            case StatusEffectType.DebuffDefense: return new Color(0.4f, 0.3f, 0.3f, 1f);
            case StatusEffectType.Poison: return new Color(0.4f, 0.85f, 0.2f, 1f);
            case StatusEffectType.Slow: return new Color(0.5f, 0.6f, 0.95f, 1f);
            case StatusEffectType.Drowsy: return new Color(0.7f, 0.55f, 0.95f, 1f);
            default: return new Color(0.9f, 0.9f, 0.9f, 1f);
        }
    }

    public static string GetStatusEffectLabel(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.BuffAttack: return "ATK Up";
            case StatusEffectType.BuffDefense: return "DEF Up";
            case StatusEffectType.DebuffAttack: return "ATK Down";
            case StatusEffectType.DebuffDefense: return "DEF Down";
            case StatusEffectType.Poison: return "Poison";
            case StatusEffectType.Slow: return "Slow";
            case StatusEffectType.Drowsy: return "Drowsy";
            default: return type.ToString();
        }
    }

    public static Color GetMomentumBarColor(float momentumValue)
    {
        if (momentumValue >= 1f) return new Color(1f, 0.85f, 0.3f, 1f);
        if (momentumValue >= 0.5f) return new Color(0.95f, 0.65f, 0.4f, 1f);
        return new Color(0.4f, 0.7f, 0.95f, 1f);
    }
}
