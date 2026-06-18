using System.Collections.Generic;
using UnityEngine;

public static class GearSetFactory
{
    public static IReadOnlyList<GearSetData> CreateStarterGearSets()
    {
        return new List<GearSetData>
        {
            Build("iron_guard", "Iron Guard", CombatUnit.Element.Earth, 0,
                0.05f, 0.10f, 0.10f,
                0.05f, 0.10f, 0.10f,
                "Worn steel armor favored by earth binders."),
            Build("ember_weave", "Ember Weave", CombatUnit.Element.Fire, 1,
                0.10f, 0.04f, 0.05f,
                0.08f, 0.04f, 0.06f,
                "Heat-treated cloth that channels flame will."),
            Build("tide_charm", "Tide Charm", CombatUnit.Element.Water, 1,
                0.06f, 0.08f, 0.04f,
                0.06f, 0.08f, 0.06f,
                "Coral-etched charm that hums near the tide."),
            Build("zephyr_mail", "Zephyr Mail", CombatUnit.Element.Air, 2,
                0.10f, 0.04f, 0.04f,
                0.10f, 0.05f, 0.04f,
                "Feather-light chain mail woven with cloudthread."),
            Build("cosmic_lattice", "Cosmic Lattice", CombatUnit.Element.Space, 3,
                0.12f, 0.10f, 0.08f,
                0.12f, 0.10f, 0.10f,
                "Star-forged lattice that bends space around the wearer.")
        };
    }

    public static GearSetData CreateDefaultForElement(CombatUnit.Element element)
    {
        switch (element)
        {
            case CombatUnit.Element.Fire: return Build("default_fire", "Flame Initiate", element, 0, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, "Starter flame set.");
            case CombatUnit.Element.Water: return Build("default_water", "Tide Initiate", element, 0, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, "Starter tide set.");
            case CombatUnit.Element.Earth: return Build("default_earth", "Stone Initiate", element, 0, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, "Starter earth set.");
            case CombatUnit.Element.Air: return Build("default_air", "Gust Initiate", element, 0, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, "Starter air set.");
            case CombatUnit.Element.Space: return Build("default_space", "Void Initiate", element, 0, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f, "Starter void set.");
            default: return Build("default_universal", "Wanderer", CombatUnit.Element.None, 0, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, "Universal starter set.");
        }
    }

    public static GearSetData Build(
        string setId,
        string displayName,
        CombatUnit.Element element,
        int tier,
        float atk, float def, float hp,
        float setAtk, float setDef, float setHp,
        string description)
    {
        GearSetData gear = ScriptableObject.CreateInstance<GearSetData>();
        gear.setId = setId;
        gear.displayName = displayName;
        gear.element = element;
        gear.tier = tier;
        gear.attackBonusPercent = Mathf.Clamp01(atk);
        gear.defenseBonusPercent = Mathf.Clamp01(def);
        gear.hpBonusPercent = Mathf.Clamp01(hp);
        gear.setBonusAttackPercent = Mathf.Clamp01(setAtk);
        gear.setBonusDefensePercent = Mathf.Clamp01(setDef);
        gear.setBonusHpPercent = Mathf.Clamp01(setHp);
        gear.description = description;
        gear.setBonusDescription = $"Full set bonus: +{Mathf.RoundToInt(setAtk * 100f)}% ATK, +{Mathf.RoundToInt(setDef * 100f)}% DEF, +{Mathf.RoundToInt(setHp * 100f)}% HP";
        gear.name = $"GearSet_{setId}";
        return gear;
    }
}
