using System;

/// <summary>Bounded integer conversion for scaled combat stats and damage.</summary>
public static class CombatMath
{
    public static int RoundToNonNegativeInt(double value, int minimum = 0)
    {
        minimum = Math.Max(0, minimum);
        if (double.IsNaN(value) || value <= minimum) return minimum;
        if (value >= int.MaxValue) return int.MaxValue;
        return Math.Max(minimum, (int)Math.Round(value, MidpointRounding.ToEven));
    }

    public static float SanitizeStatMultiplier(float multiplier)
    {
        return float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f ? 1f : multiplier;
    }
}
