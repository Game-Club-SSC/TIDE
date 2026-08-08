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
        new EncounterDef { id = "wrath_c1", displayName = "Wrath - First Fury",
            enemyIds = new[] { "enemy_wrath_brute" } },
        new EncounterDef { id = "wrath_p1", displayName = "Wrath - Ember Path",
            enemyIds = new[] { "enemy_wrath_fiend" } },
        new EncounterDef { id = "wrath_c2", displayName = "Wrath - Burning Rage",
            enemyIds = new[] { "enemy_wrath_brute", "enemy_wrath_fiend" } },
        new EncounterDef { id = "wrath_p2", displayName = "Wrath - Berzerker's Trial",
            enemyIds = new[] { "enemy_wrath_berzerker" } },
        new EncounterDef { id = "wrath_c3", displayName = "Wrath - Pyre March",
            enemyIds = new[] { "enemy_wrath_fiend", "enemy_wrath_pyre" } },
        new EncounterDef { id = "wrath_p3", displayName = "Wrath - Spirit Flame",
            enemyIds = new[] { "enemy_wrath_pyre" } },
        new EncounterDef { id = "wrath_c4", displayName = "Wrath - Warlord's Vanguard",
            enemyIds = new[] { "enemy_wrath_brute", "enemy_wrath_berzerker", "enemy_wrath_pyre" } },
        new EncounterDef { id = "wrath_p4", displayName = "Wrath - Fiend's Domain",
            enemyIds = new[] { "enemy_wrath_fiend" } },
        new EncounterDef { id = "wrath_boss", displayName = "Wrath - The Crimson Warlord",
            enemyIds = new[] { "enemy_wrath_boss", "enemy_wrath_brute" } },

        // ===== SLOTH (Air) =====
        new EncounterDef { id = "sloth_c1", displayName = "Sloth - Dreamer's Path",
            enemyIds = new[] { "enemy_sloth_dreamer" } },
        new EncounterDef { id = "sloth_p1", displayName = "Sloth - Slumbering Guard",
            enemyIds = new[] { "enemy_sloth_slumberer" } },
        new EncounterDef { id = "sloth_c2", displayName = "Sloth - Lethargic Duo",
            enemyIds = new[] { "enemy_sloth_dreamer", "enemy_sloth_void" } },
        new EncounterDef { id = "sloth_p2", displayName = "Sloth - Haze Trail",
            enemyIds = new[] { "enemy_sloth_haze" } },
        new EncounterDef { id = "sloth_c3", displayName = "Sloth - Void Depths",
            enemyIds = new[] { "enemy_sloth_slumberer", "enemy_sloth_void" } },
        new EncounterDef { id = "sloth_p3", displayName = "Sloth - Dreamer's Rest",
            enemyIds = new[] { "enemy_sloth_dreamer" } },
        new EncounterDef { id = "sloth_c4", displayName = "Sloth - Final Slumber",
            enemyIds = new[] { "enemy_sloth_slumberer", "enemy_sloth_void", "enemy_sloth_haze" } },
        new EncounterDef { id = "sloth_p4", displayName = "Sloth - Haze Barrier",
            enemyIds = new[] { "enemy_sloth_haze" } },
        new EncounterDef { id = "sloth_boss", displayName = "Sloth - The Somnolent",
            enemyIds = new[] { "enemy_sloth_boss", "enemy_sloth_dreamer" } },

        // ===== PRIDE (Space) =====
        new EncounterDef { id = "pride_c1", displayName = "Pride - Sentinel's Watch",
            enemyIds = new[] { "enemy_pride_sentinel" } },
        new EncounterDef { id = "pride_p1", displayName = "Pride - Mirror Hall",
            enemyIds = new[] { "enemy_pride_mirror" } },
        new EncounterDef { id = "pride_c2", displayName = "Pride - Arrogant Pair",
            enemyIds = new[] { "enemy_pride_sentinel", "enemy_pride_arrogant" } },
        new EncounterDef { id = "pride_p2", displayName = "Pride - Veiled Path",
            enemyIds = new[] { "enemy_pride_veil" } },
        new EncounterDef { id = "pride_c3", displayName = "Pride - Mirror Duel",
            enemyIds = new[] { "enemy_pride_mirror", "enemy_pride_arrogant" } },
        new EncounterDef { id = "pride_p3", displayName = "Pride - Veil's Domain",
            enemyIds = new[] { "enemy_pride_veil" } },
        new EncounterDef { id = "pride_c4", displayName = "Pride - Grand Assault",
            enemyIds = new[] { "enemy_pride_sentinel", "enemy_pride_arrogant", "enemy_pride_veil" } },
        new EncounterDef { id = "pride_p4", displayName = "Pride - Knight's Test",
            enemyIds = new[] { "enemy_pride_mirror" } },
        new EncounterDef { id = "pride_boss", displayName = "Pride - The Grand Monarch",
            enemyIds = new[] { "enemy_pride_boss", "enemy_pride_sentinel" } },

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

        var encounterLookup = new System.Collections.Generic.Dictionary<string, EncounterDef>();
        for (int i = 0; i < AllEncounters.Length; i++)
        {
            encounterLookup[AllEncounters[i].id] = AllEncounters[i];
        }

        int linked = 0;
        int unresolved = 0;
        string[] encGuids = AssetDatabase.FindAssets("t:EncounterConfig", new[] { "Assets/Resources/Encounters" });
        foreach (string guid in encGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterConfig enc = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            if (enc == null || !encounterLookup.TryGetValue(enc.encounterId, out EncounterDef definition))
            {
                continue;
            }

            if (HasCompleteEnemyArray(enc.enemies))
            {
                continue;
            }

            EnemyData[] expectedEnemies = new EnemyData[definition.enemyIds.Length];
            bool canRelink = true;
            for (int i = 0; i < definition.enemyIds.Length; i++)
            {
                if (!enemyLookup.TryGetValue(definition.enemyIds[i], out expectedEnemies[i]))
                {
                    Debug.LogWarning($"[EncounterConfigPopulator] Cannot re-link '{enc.encounterId}': missing EnemyData '{definition.enemyIds[i]}'.");
                    canRelink = false;
                    unresolved++;
                }
            }

            if (!canRelink)
            {
                continue;
            }

            enc.enemies = expectedEnemies;
            EditorUtility.SetDirty(enc);
            linked++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EncounterConfigPopulator] Re-linked {linked} encounters; {unresolved} enemy IDs unresolved.");
        EditorUtility.DisplayDialog("Re-link Complete",
            $"Re-linked {linked} encounter configs.\nUnresolved enemy IDs: {unresolved}.",
            "OK");
    }

    private static bool HasCompleteEnemyArray(EnemyData[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    [MenuItem("TIDE/Re-link Encounter Enemies", true)]
    public static bool RelinkValidate()
    {
        return !EditorApplication.isPlaying;
    }
}
