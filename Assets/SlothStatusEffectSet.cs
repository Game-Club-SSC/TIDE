using UnityEngine;

public static class SlothStatusEffectSet
{
    public static StatusEffect CreateSlowEffect(string sourceName, int duration, float magnitude)
    {
        StatusEffect effect = new StatusEffect(StatusEffectType.Slow, Mathf.Max(1, duration), Mathf.Max(0f, magnitude), sourceName);
        return effect;
    }

    public static StatusEffect CreateDrowsyEffect(string sourceName, int duration)
    {
        StatusEffect effect = new StatusEffect(StatusEffectType.Drowsy, Mathf.Max(1, duration), 0f, sourceName);
        return effect;
    }

    public static bool IsSlothEffectType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        return string.Equals(typeName, "Slow", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(typeName, "Drowsy", System.StringComparison.OrdinalIgnoreCase);
    }
}
