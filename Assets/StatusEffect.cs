using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusEffectType
{
    None,
    BuffAttack,
    BuffDefense,
    DebuffAttack,
    DebuffDefense,
    Poison,
    Slow,
    Drowsy,
    Stun,
    Burn,
    Regeneration,
    Shield,
    Berserk,
    Taunt,
    AccuracyDown,
    AccuracyBuff
}

public class StatusEffect
{
    public StatusEffectType Type { get; set; }
    public int Duration { get; set; }
    public float Magnitude { get; set; }
    public string SourceName { get; set; }
    public int Stacks { get; set; } = 1;

    public StatusEffect(StatusEffectType type, int duration, float magnitude, string sourceName)
    {
        Type = type;
        Duration = duration;
        Magnitude = magnitude;
        SourceName = sourceName;
    }

    public StatusEffect(StatusEffectType type, int duration, float magnitude, string sourceName, int stacks)
        : this(type, duration, magnitude, sourceName)
    {
        Stacks = Mathf.Max(1, stacks);
    }

    /// <summary>
    /// Returns true if this effect is a negative (debuff) effect.
    /// </summary>
    public bool IsDebuff
    {
        get
        {
            switch (Type)
            {
                case StatusEffectType.DebuffAttack:
                case StatusEffectType.DebuffDefense:
                case StatusEffectType.Poison:
                case StatusEffectType.Slow:
                case StatusEffectType.Drowsy:
                case StatusEffectType.Stun:
                case StatusEffectType.Burn:
                case StatusEffectType.AccuracyDown:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Returns true if this effect is a positive (buff) effect.
    /// </summary>
    public bool IsBuff
    {
        get
        {
            switch (Type)
            {
                case StatusEffectType.BuffAttack:
                case StatusEffectType.BuffDefense:
                case StatusEffectType.Regeneration:
                case StatusEffectType.Shield:
                case StatusEffectType.Berserk:
                case StatusEffectType.Taunt:
                case StatusEffectType.AccuracyBuff:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Returns an appropriate color for UI display based on the effect type.
    /// </summary>
    public Color GetEffectColor()
    {
        switch (Type)
        {
            // Buffs - blue / green tones
            case StatusEffectType.BuffAttack:
                return new Color(0.3f, 0.6f, 1f);       // Light blue
            case StatusEffectType.BuffDefense:
                return new Color(0.4f, 0.7f, 1f);       // Sky blue
            case StatusEffectType.AccuracyBuff:
                return new Color(0.4f, 0.85f, 0.4f);    // Light green

            // Healing / protective - green / cyan tones
            case StatusEffectType.Regeneration:
                return new Color(0.2f, 0.8f, 0.3f);     // Green
            case StatusEffectType.Shield:
                return new Color(0.2f, 0.75f, 0.9f);    // Cyan

            // Offensive self-buffs - red / orange tones
            case StatusEffectType.Berserk:
                return new Color(0.85f, 0.15f, 0.15f);  // Crimson red
            case StatusEffectType.Taunt:
                return new Color(1f, 0.55f, 0.1f);      // Orange

            // Debuffs - red / warm tones
            case StatusEffectType.DebuffAttack:
                return new Color(1f, 0.35f, 0.25f);     // Red-orange
            case StatusEffectType.DebuffDefense:
                return new Color(0.9f, 0.3f, 0.2f);     // Red
            case StatusEffectType.AccuracyDown:
                return new Color(0.9f, 0.45f, 0.15f);   // Orange-red

            // Damage-over-time - purple / fire tones
            case StatusEffectType.Poison:
                return new Color(0.6f, 0.2f, 0.8f);     // Purple
            case StatusEffectType.Burn:
                return new Color(1f, 0.45f, 0f);        // Orange

            // Crowd-control - yellow / gray tones
            case StatusEffectType.Stun:
                return new Color(1f, 0.85f, 0.2f);      // Yellow
            case StatusEffectType.Slow:
                return new Color(0.65f, 0.5f, 0.8f);    // Light purple
            case StatusEffectType.Drowsy:
                return new Color(0.6f, 0.6f, 0.65f);    // Gray

            default:
                return Color.white;
        }
    }

    public void Tick()
    {
        // Negative Duration represents infinite/permanent effects — don't decrement
        if (Duration > 0)
        {
            Duration--;
        }
    }

    public bool IsValid()
    {
        return Type != StatusEffectType.None && Duration > 0;
    }
}
