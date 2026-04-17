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
    Poison
}

public class StatusEffect
{
    public StatusEffectType Type { get; set; }
    public int Duration { get; set; }
    public float Magnitude { get; set; }
    public string SourceName { get; set; }

    public StatusEffect(StatusEffectType type, int duration, float magnitude, string sourceName)
    {
        Type = type;
        Duration = duration;
        Magnitude = magnitude;
        SourceName = sourceName;
    }

    public void Tick()
    {
        Duration = Mathf.Max(0, Duration - 1);
    }

    public bool IsValid()
    {
        return Type != StatusEffectType.None && Duration > 0;
    }
}