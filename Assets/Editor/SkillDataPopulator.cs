using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates SkillData ScriptableObjects for all 5 heroes (25 abilities total).
/// Uses Opus 4.7's design output with playstyle-tested values.
/// Access via: TIDE > Populate Hero Skills
/// </summary>
public static class SkillDataPopulator
{
    private const string OutputFolder = "Assets/Resources/Skills";

    private static readonly (string id, string name, string desc, int mp, float dmg, float heal,
        SkillTarget target, CombatUnit.Element element, StatusEffectType effect, int effectDur, float effectMag)[] AllSkills = new[]
    {
        // ===== FIRE — Killian (DPS) =====
        ("skill_killian_basic", "Chakram Slash",
         "Basic dual-ring strike.",
         0, 1.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Fire, StatusEffectType.None, 0, 0f),

        ("skill_killian_searing_arc", "Searing Arc",
         "Hurls both chakram in a burning arc; +0.4 multiplier while Rage is active.",
         20, 1.8f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Fire, StatusEffectType.Burn, 2, 0.1f),

        ("skill_killian_blaze_flurry", "Blaze Flurry",
         "Three rapid slashes; each hit that lands during Rage refunds 5 MP.",
         30, 1.3f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Fire, StatusEffectType.None, 0, 0f),

        ("skill_killian_cinder_vent", "Cinder Vent",
         "Deliberately absorbs a fraction of incoming damage to self-trigger Rage early (10% self HP), granting +30% ATK for 3 turns.",
         25, 0f, 0f, SkillTarget.Self, CombatUnit.Element.Fire, StatusEffectType.BuffAttack, 3, 0.3f),

        ("skill_killian_immolation", "Immolation",
         "Detonates all built rage into a fire nova; damage scales +50% if Rage is active, then consumes Rage.",
         60, 3.5f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Fire, StatusEffectType.Burn, 3, 0.15f),

        // ===== WATER — Merrick (Healer/Support) =====
        ("skill_merrick_basic", "Tidal Tap",
         "Basic staff jab of pressurized water.",
         0, 1.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Water, StatusEffectType.None, 0, 0f),

        ("skill_merrick_soothing", "Soothing Current",
         "Restores HP to one ally. Overhealing Killian past 80% suppresses his Rage trigger.",
         20, 0f, 1.5f, SkillTarget.SingleAlly, CombatUnit.Element.Water, StatusEffectType.Regeneration, 2, 0.05f),

        ("skill_merrick_rolling_wave", "Rolling Wave",
         "Gentle heal to the whole party.",
         35, 0f, 1.0f, SkillTarget.AllAllies, CombatUnit.Element.Water, StatusEffectType.Regeneration, 1, 0.03f),

        ("skill_merrick_pain_absorb", "Pain Absorption",
         "Revives one downed ally to 50% HP by taking their pain — costs Merrick 30% of his max HP.",
         40, 0f, 0.5f, SkillTarget.SingleAlly, CombatUnit.Element.Water, StatusEffectType.None, 0, 0f),

        ("skill_merrick_undertow", "Undertow",
         "Massive party heal + cleanses all debuffs; if Merrick is below 30% HP, heal is doubled.",
         55, 0f, 2.0f, SkillTarget.AllAllies, CombatUnit.Element.Water, StatusEffectType.None, 0, 0f),

        // ===== EARTH — Freida (Tank/Controller) =====
        ("skill_freida_basic", "Thorn Shot",
         "Basic barbed arrow.",
         0, 1.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Earth, StatusEffectType.None, 0, 0f),

        ("skill_freida_root_snare", "Root Snare",
         "Vines erupt, rooting the target (cannot move/act) for 1 turn.",
         20, 0.8f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Earth, StatusEffectType.Stun, 1, 0f),

        ("skill_freida_bramble_wall", "Bramble Wall",
         "Grows a living wall granting the party +40% DEF for 2 turns.",
         25, 0f, 0f, SkillTarget.AllAllies, CombatUnit.Element.Earth, StatusEffectType.BuffDefense, 2, 0.4f),

        ("skill_freida_seismic_volley", "Seismic Volley",
         "Arrow storm that pushes all enemies back (disrupts positioning) and slows them 1 turn.",
         30, 1.2f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Earth, StatusEffectType.Slow, 1, 0.3f),

        ("skill_freida_ancient_grove", "Ancient Grove",
         "Summons a colossal tree that stuns all enemies 1 turn and heals the party for 20% over 2 turns.",
         55, 1.5f, 0.2f, SkillTarget.AllEnemies, CombatUnit.Element.Earth, StatusEffectType.Stun, 1, 0f),

        // ===== AIR — Briar (Support/Debuffer) =====
        ("skill_briar_basic", "Fan Cut",
         "Basic slicing gust.",
         0, 1.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Air, StatusEffectType.None, 0, 0f),

        ("skill_briar_gale_redirect", "Gale Redirect",
         "A defensive dance that redirects the next incoming attack back at its caster (30% of its damage).",
         20, 0f, 0f, SkillTarget.Self, CombatUnit.Element.Air, StatusEffectType.Shield, 1, 0.3f),

        ("skill_briar_silencing_waltz", "Silencing Waltz",
         "Dance that silences a target (no abilities, basic attacks only) for 2 turns.",
         25, 0.6f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Air, StatusEffectType.Drowsy, 2, 0f),

        ("skill_briar_scattering_step", "Scattering Step",
         "Sweeping fans push a single enemy far back and reduce its ATK by 25% for 2 turns.",
         25, 0.9f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Air, StatusEffectType.DebuffAttack, 2, 0.25f),

        ("skill_briar_tempest_dance", "Tempest Dance",
         "A whirling cyclone dealing damage to all enemies and reducing their accuracy 30% for 2 turns.",
         55, 2.2f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Air, StatusEffectType.AccuracyDown, 2, 0.3f),

        // ===== SPACE — Aether (Magic DPS) =====
        ("skill_aether_basic", "Void Bolt",
         "Basic lance of purple magic.",
         0, 1.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Space, StatusEffectType.None, 0, 0f),

        ("skill_aether_astral_lance", "Astral Lance",
         "A focused beam of cosmic energy that ignores 20% of DEF.",
         25, 2.0f, 0f, SkillTarget.SingleEnemy, CombatUnit.Element.Space, StatusEffectType.None, 0, 0f),

        ("skill_aether_nebula_burst", "Nebula Burst",
         "Explosion of stardust damaging all enemies.",
         35, 1.4f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Space, StatusEffectType.None, 0, 0f),

        ("skill_aether_gravity_well", "Gravity Well",
         "Collapses space around enemies, pulling them together and dealing damage over 2 turns.",
         30, 1.1f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Space, StatusEffectType.Slow, 2, 0.4f),

        ("skill_aether_event_horizon", "Event Horizon",
         "Opens a rift consuming all enemies; deals massive AoE and removes their buffs.",
         65, 3.0f, 0f, SkillTarget.AllEnemies, CombatUnit.Element.Space, StatusEffectType.None, 0, 0f),
    };

    [MenuItem("TIDE/Populate Hero Skills")]
    public static void PopulateAllSkills()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = Path.GetDirectoryName(OutputFolder);
            string folderName = Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        int skipped = 0;

        foreach (var s in AllSkills)
        {
            string path = $"{OutputFolder}/{s.id}.asset";

            if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null)
            {
                skipped++;
                continue;
            }

            SkillData data = ScriptableObject.CreateInstance<SkillData>();
            data.skillName = s.name;
            data.description = s.desc;
            data.mpCost = s.mp;
            data.damageMultiplier = s.dmg;
            data.restoreCasterPercentOfDamage = s.heal;
            data.target = s.target;
            data.element = s.element;
            data.appliedEffectType = s.effect;
            data.effectDuration = s.effectDur;
            data.effectMagnitude = s.effectMag;
            data.currencyStealAmount = 0;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillDataPopulator] Created {created} skills, skipped {skipped} existing");
        EditorUtility.DisplayDialog("Hero Skills Created",
            $"Created {created} SkillData assets ({skipped} skipped).\n\n" +
            "Fire (Killian): 5 skills\n" +
            "Water (Merrick): 5 skills\n" +
            "Earth (Freida): 5 skills\n" +
            "Air (Briar): 5 skills\n" +
            "Space (Aether): 5 skills\n\n" +
            "Total: 25 abilities",
            "OK");
    }

    [MenuItem("TIDE/Populate Hero Skills", true)]
    public static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }
}
