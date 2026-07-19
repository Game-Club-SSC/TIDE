using UnityEngine;

public static class DesireStatusEffectSet
{
    public static StatusEffect CreateSlowEffect(string sourceName, int duration, float magnitude)
    {
        StatusEffect effect = new StatusEffect(StatusEffectType.Slow, Mathf.Max(1, duration), Mathf.Max(0f, magnitude), sourceName);
        return effect;
    }

    public static StatusEffect CreateDrowsyEffect(string sourceName, int duration)
    {
        StatusEffect effect = new StatusEffect(StatusEffectType.Drowsy, Mathf.Max(1, duration), 0.5f, sourceName);
        return effect;
    }

    public static bool IsDesireEffectType(StatusEffectType type)
    {
        return type == StatusEffectType.Slow || type == StatusEffectType.Drowsy;
    }
}
