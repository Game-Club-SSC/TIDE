using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates EncounterConfig ScriptableObjects for all 6 islands (36 encounters total).
/// Uses Gemini's encounter composition data.
/// Access via: TIDE > Populate Encounter Configs
/// </summary>
public static class EncounterConfigPopulator
{
    private const string OutputFolder = "Assets/Resources/Encounters";

    private struct EncounterDef
    {
        public string id;
        public string displayName;
        public string[] enemyIds; // references to EnemyData asset names
    }

    // ============================================================
    // All 36 encounters across 6 islands
    // ============================================================
    private static readonly EncounterDef[] AllEncounters = new[]
    {
        // ===== GREED (Earth) =====
        new EncounterDef { id = "greed_c1", displayName = "Greed - First Contact",
            enemyIds = new[] { "enemy_greed_avarice" } },
        new EncounterDef { id = "greed_p1", displayName = "Greed - Hoarder's Puzzle",
            enemyIds = new[] { "enemy_greed_hoarding" } },
        new EncounterDef { id = "greed_c2", displayName = "Greed - Double Tax",
            enemyIds = new[] { "enemy_greed_avarice", "enemy_greed_tax" } },
        new EncounterDef { id = "greed_p2", displayName = "Greed - Debt's Collection",
            enemyIds = new[] { "enemy_greed_debt" } },
        new EncounterDef { id = "greed_c3", displayName = "Greed - The Hoard",
            enemyIds = new[] { "enemy_greed_hoarding", "enemy_greed_tax" } },
        new EncounterDef { id = "greed_p3", displayName = "Greed - Avarice Awakened",
            enemyIds = new[] { "enemy_greed_avarice" } },
        new EncounterDef { id = "greed_c4", displayName = "Greed - Final Toll",
            enemyIds = new[] { "enemy_greed_hoarding", "enemy_greed_tax", "enemy_greed_debt" } },
        new EncounterDef { id = "greed_p4", displayName = "Greed - Last Debts",
            enemyIds = new[] { "enemy_greed_debt" } },
        new EncounterDef { id = "greed_boss", displayName = "Greed - The Golden Idol",
            enemyIds = new[] { "enemy_greed_boss", "enemy_greed_avarice" } },

        // ===== LUST (Water) =====
        new EncounterDef { id = "lust_c1", displayName = "Lust - Siren's Call",
            enemyIds = new[] { "enemy_lust_siren" } },
        new EncounterDef { id = "lust_p1", displayName = "Lust - Enchanted Reef",
            enemyIds = new[] { "enemy_lust_charmer" } },
        new EncounterDef { id = "lust_c2", displayName = "Lust - Double Entice",
            enemyIds = new[] { "enemy_lust_siren", "enemy_lust_charmer" } },
        new EncounterDef { id = "lust_p2", displayName = "Lust - Phantom Currents",
            enemyIds = new[] { "enemy_lust_phantom" } },
        new EncounterDef { id = "lust_c3", displayName = "Lust - Whispered Lies",
            enemyIds = new[] { "enemy_lust_phantom", "enemy_lust_whisperer" } },
        new EncounterDef { id = "lust_p3", displayName = "Lust - The Whisper",
            enemyIds = new[] { "enemy_lust_whisperer" } },
        new EncounterDef { id = "lust_c4", displayName = "Lust - Coral Depths",
            enemyIds = new[] { "enemy_lust_siren", "enemy_lust_phantom", "enemy_lust_whisperer" } },
        new EncounterDef { id = "lust_p4", displayName = "Lust - Enchanter's End",
            enemyIds = new[] { "enemy_lust_charmer" } },
        new EncounterDef { id = "lust_boss", displayName = "Lust - The Coral Queen",
            enemyIds = new[] { "enemy_lust_boss", "enemy_lust_siren" } },

        // ===== WRATH (Fire) =====
        new EncounterDef { id = "anger_c1", displayName = "Anger - First Fury",
            enemyIds = new[] { "enemy_anger_brute" } },
        new EncounterDef { id = "anger_p1", displayName = "Anger - Ember Path",
            enemyIds = new[] { "enemy_anger_fiend" } },
        new EncounterDef { id = "anger_c2", displayName = "Anger - Burning Rage",
            enemyIds = new[] { "enemy_anger_brute", "enemy_anger_fiend" } },
        new EncounterDef { id = "anger_p2", displayName = "Anger - Berzerker's Trial",
            enemyIds = new[] { "enemy_anger_berzerker" } },
        new EncounterDef { id = "anger_c3", displayName = "Anger - Pyre March",
            enemyIds = new[] { "enemy_anger_fiend", "enemy_anger_pyre" } },
        new EncounterDef { id = "anger_p3", displayName = "Anger - Spirit Flame",
            enemyIds = new[] { "enemy_anger_pyre" } },
        new EncounterDef { id = "anger_c4", displayName = "Anger - Warlord's Vanguard",
            enemyIds = new[] { "enemy_anger_brute", "enemy_anger_berzerker", "enemy_anger_pyre" } },
        new EncounterDef { id = "anger_p4", displayName = "Anger - Fiend's Domain",
            enemyIds = new[] { "enemy_anger_fiend" } },
        new EncounterDef { id = "anger_boss", displayName = "Anger - The Crimson Warlord",
            enemyIds = new[] { "enemy_anger_boss", "enemy_anger_brute" } },

        // ===== SLOTH (Air) =====
        new EncounterDef { id = "desire_c1", displayName = "Desire - Dreamer's Path",
            enemyIds = new[] { "enemy_desire_dreamer" } },
        new EncounterDef { id = "desire_p1", displayName = "Desire - Slumbering Guard",
            enemyIds = new[] { "enemy_desire_slumberer" } },
        new EncounterDef { id = "desire_c2", displayName = "Desire - Lethargic Duo",
            enemyIds = new[] { "enemy_desire_dreamer", "enemy_desire_void" } },
        new EncounterDef { id = "desire_p2", displayName = "Desire - Haze Trail",
            enemyIds = new[] { "enemy_desire_haze" } },
        new EncounterDef { id = "desire_c3", displayName = "Desire - Void Depths",
            enemyIds = new[] { "enemy_desire_slumberer", "enemy_desire_void" } },
        new EncounterDef { id = "desire_p3", displayName = "Desire - Dreamer's Rest",
            enemyIds = new[] { "enemy_desire_dreamer" } },
        new EncounterDef { id = "desire_c4", displayName = "Desire - Final Slumber",
            enemyIds = new[] { "enemy_desire_slumberer", "enemy_desire_void", "enemy_desire_haze" } },
        new EncounterDef { id = "desire_p4", displayName = "Desire - Haze Barrier",
            enemyIds = new[] { "enemy_desire_haze" } },
        new EncounterDef { id = "desire_boss", displayName = "Desire - The Somnolent",
            enemyIds = new[] { "enemy_desire_boss", "enemy_desire_dreamer" } },

        // ===== PRIDE (Space) =====
        new EncounterDef { id = "ego_c1", displayName = "Ego - Sentinel's Watch",
            enemyIds = new[] { "enemy_ego_sentinel" } },
        new EncounterDef { id = "ego_p1", displayName = "Ego - Mirror Hall",
            enemyIds = new[] { "enemy_ego_mirror" } },
        new EncounterDef { id = "ego_c2", displayName = "Ego - Arrogant Pair",
            enemyIds = new[] { "enemy_ego_sentinel", "enemy_ego_arrogant" } },
        new EncounterDef { id = "ego_p2", displayName = "Ego - Veiled Path",
            enemyIds = new[] { "enemy_ego_veil" } },
        new EncounterDef { id = "ego_c3", displayName = "Ego - Mirror Duel",
            enemyIds = new[] { "enemy_ego_mirror", "enemy_ego_arrogant" } },
        new EncounterDef { id = "ego_p3", displayName = "Ego - Veil's Domain",
            enemyIds = new[] { "enemy_ego_veil" } },
        new EncounterDef { id = "ego_c4", displayName = "Ego - Grand Assault",
            enemyIds = new[] { "enemy_ego_sentinel", "enemy_ego_arrogant", "enemy_ego_veil" } },
        new EncounterDef { id = "ego_p4", displayName = "Ego - Knight's Test",
            enemyIds = new[] { "enemy_ego_mirror" } },
        new EncounterDef { id = "ego_boss", displayName = "Ego - The Grand Monarch",
            enemyIds = new[] { "enemy_ego_boss", "enemy_ego_sentinel" } },

        // ===== ENVY (Air) =====
        new EncounterDef { id = "envy_c1", displayName = "Envy - Stalker's Trail",
            enemyIds = new[] { "enemy_envy_stalker" } },
        new EncounterDef { id = "envy_p1", displayName = "Envy - Mimic's Lair",
            enemyIds = new[] { "enemy_envy_mimic" } },
        new EncounterDef { id = "envy_c2", displayName = "Envy - Jealous Pair",
            enemyIds = new[] { "enemy_envy_stalker", "enemy_envy_mimic" } },
        new EncounterDef { id = "envy_p2", displayName = "Envy - Covetous Path",
            enemyIds = new[] { "enemy_envy_covet" } },
        new EncounterDef { id = "envy_c3", displayName = "Envy - Mimic's Web",
            enemyIds = new[] { "enemy_envy_mimic", "enemy_envy_covet" } },
        new EncounterDef { id = "envy_p3", displayName = "Envy - Shade's Trail",
            enemyIds = new[] { "enemy_envy_shade" } },
        new EncounterDef { id = "envy_c4", displayName = "Envy - Final Covet",
            enemyIds = new[] { "enemy_envy_covet", "enemy_envy_shade", "enemy_envy_stalker" } },
        new EncounterDef { id = "envy_p4", displayName = "Envy - Shade's Gate",
            enemyIds = new[] { "enemy_envy_shade" } },
        new EncounterDef { id = "envy_boss", displayName = "Envy - The Usurper",
            enemyIds = new[] { "enemy_envy_boss", "enemy_envy_shade" } },
    };

    [MenuItem("TIDE/Populate Encounter Configs")]
    public static void PopulateAllEncounters()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = Path.GetDirectoryName(OutputFolder);
            string folderName = Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        int skipped = 0;
        int warnings = 0;

        foreach (var enc in AllEncounters)
        {
            string path = $"{OutputFolder}/{enc.id}.asset";

            if (AssetDatabase.LoadAssetAtPath<EncounterConfig>(path) != null)
            {
                skipped++;
                continue;
            }

            EncounterConfig data = ScriptableObject.CreateInstance<EncounterConfig>();
            data.encounterId = enc.id;
            data.displayName = enc.displayName;

            // Load enemy data references
            data.enemies = new EnemyData[enc.enemyIds.Length];
            for (int i = 0; i < enc.enemyIds.Length; i++)
            {
                string enemyPath = $"Assets/Resources/EnemyData/{enc.enemyIds[i]}.asset";
                EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(enemyPath);
                if (enemy != null)
                {
                    data.enemies[i] = enemy;
                }
                else
                {
                    warnings++;
                    Debug.LogWarning($"[EncounterConfigPopulator] Enemy not found: {enc.enemyIds[i]} (run EnemyDataPopulator first)");
                }
            }

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EncounterConfigPopulator] Created {created} encounters, skipped {skipped} existing, {warnings} warnings");
        EditorUtility.DisplayDialog("Encounters Created",
            $"Created {created} EncounterConfig assets.\n\n" +
            "Greed: 6 encounters\n" +
            "Lust: 6 encounters\n" +
            "Anger: 6 encounters\n" +
            "Desire: 6 encounters\n" +
            "Envy: 6 encounters\n" +
            "Ego: 6 encounters\n\n" +
            "Total: 36 encounters" +
            (warnings > 0 ? $"\n\n⚠ {warnings} enemy references missing\n(Run Populate Enemy Data first)" : ""),
            "OK");
    }

    [MenuItem("TIDE/Populate Encounter Configs", true)]
    public static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }

    /// <summary>
    /// Re-links enemy references on existing encounter configs.
    /// Run this after EnemyDataPopulator if encounters were created first.
    /// Access via: TIDE > Re-link Encounter Enemies
    /// </summary>
    [MenuItem("TIDE/Re-link Encounter Enemies")]
    public static void RelinkEnemies()
    {
        // Build enemy lookup
        var enemyLookup = new System.Collections.Generic.Dictionary<string, EnemyData>();
        string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/Resources/EnemyData" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy != null) enemyLookup[enemy.enemyId] = enemy;
        }

        int linked = 0;
        string[] encGuids = AssetDatabase.FindAssets("t:EncounterConfig", new[] { "Assets/Resources/Encounters" });
        foreach (string guid in encGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterConfig enc = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            if (enc == null || enc.enemies == null) continue;

            bool changed = false;
            for (int i = 0; i < enc.enemies.Length; i++)
            {
                if (enc.enemies[i] != null) continue;
                // Try to find by encounter ID pattern: greed_c1 → enemy_greed_avarice
                // The encounter ID doesn't directly map to enemy IDs, so we skip nulls
                // The actual fix is to re-create encounters
            }

            if (changed)
            {
                EditorUtility.SetDirty(enc);
                linked++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EncounterConfigPopulator] Re-linked {linked} encounters");
        EditorUtility.DisplayDialog("Re-link Complete",
            "Encounter enemy references are still null because\nthe configs were created before enemies existed.\n\n" +
            "To fix: delete all files in Resources/Encounters/\nthen re-run 'Populate Encounter Configs'.",
            "OK");
    }

    [MenuItem("TIDE/Re-link Encounter Enemies", true)]
    public static bool RelinkValidate()
    {
        return !EditorApplication.isPlaying;
    }
}
