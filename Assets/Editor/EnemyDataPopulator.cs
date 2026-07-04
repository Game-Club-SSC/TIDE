using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to batch-generate enemy ScriptableObjects.
/// Access via: TIDE > Populate Enemy Data (All 7 Islands)
/// </summary>
public static class EnemyDataPopulator
{
    private const string OutputFolder = "Assets/Resources/EnemyData";

    private static readonly (string id, string name, CombatUnit.Element element, int hp, int mp, int atk, int def, int spd, int xp)[] AllEnemies = new[]
    {
        // === Island 1: Greed (Earth) ===
        ("enemy_greed_avarice",      "Avarice",           CombatUnit.Element.Earth, 80,  3,  15, 8,  8,  25),
        ("enemy_greed_hoarding",     "Hoarding",          CombatUnit.Element.Earth, 85,  4,  16, 9,  9,  26),
        ("enemy_greed_tax",          "Tax Collector",     CombatUnit.Element.Earth, 88,  4,  18, 9,  9,  28),
        ("enemy_greed_debt",         "Debt",              CombatUnit.Element.Earth, 90,  5,  19, 10, 10, 30),
        ("enemy_greed_boss",         "Golden Idol",       CombatUnit.Element.Earth, 400, 20, 40, 25, 18, 150),

        // === Island 2: Lust (Water) ===
        ("enemy_lust_siren",         "Siren",             CombatUnit.Element.Water, 95,  6,  21, 11, 11, 32),
        ("enemy_lust_charmer",       "Charmer",           CombatUnit.Element.Water, 100, 6,  23, 12, 11, 33),
        ("enemy_lust_phantom",       "Phantom",           CombatUnit.Element.Water, 102, 7,  24, 13, 12, 34),
        ("enemy_lust_whisperer",     "Whisperer",         CombatUnit.Element.Water, 105, 8,  25, 14, 12, 35),
        ("enemy_lust_boss",          "Coral Queen",       CombatUnit.Element.Water, 450, 24, 45, 28, 19, 175),

        // === Island 3: Wrath (Fire) ===
        ("enemy_wrath_brute",        "Brute",             CombatUnit.Element.Fire,   110, 9,  26, 15, 13, 36),
        ("enemy_wrath_fiend",        "Fiend",             CombatUnit.Element.Fire,   115, 10, 28, 16, 14, 37),
        ("enemy_wrath_berzerker",    "Berzerker",         CombatUnit.Element.Fire,   120, 10, 29, 16, 14, 39),
        ("enemy_wrath_pyre",         "Pyre Spirit",       CombatUnit.Element.Fire,   125, 11, 30, 17, 15, 40),
        ("enemy_wrath_boss",         "Crimson Warlord",   CombatUnit.Element.Fire,   500, 28, 49, 31, 21, 200),

        // === Island 4: Sloth (Air) ===
        ("enemy_sloth_dreamer",      "Dreamer",           CombatUnit.Element.Air,    130, 12, 31, 18, 16, 41),
        ("enemy_sloth_slumberer",    "Slumberer",         CombatUnit.Element.Air,    135, 13, 32, 19, 16, 43),
        ("enemy_sloth_void",         "Void Walker",       CombatUnit.Element.Air,    140, 13, 34, 20, 17, 44),
        ("enemy_sloth_haze",         "Haze",              CombatUnit.Element.Air,    145, 14, 35, 21, 18, 45),
        ("enemy_sloth_boss",         "The Somnolent",     CombatUnit.Element.Air,    550, 31, 53, 34, 23, 225),

        // === Island 5: Pride (Space) ===
        ("enemy_pride_sentinel",     "Sentinel",          CombatUnit.Element.Space,  150, 15, 36, 22, 19, 46),
        ("enemy_pride_mirror",       "Mirror Knight",     CombatUnit.Element.Space,  155, 16, 37, 22, 19, 47),
        ("enemy_pride_arrogant",     "Arrogant One",      CombatUnit.Element.Space,  160, 16, 39, 23, 20, 49),
        ("enemy_pride_veil",         "Veil",              CombatUnit.Element.Space,  165, 17, 40, 24, 20, 50),
        ("enemy_pride_boss",         "Grand Monarch",     CombatUnit.Element.Space,  600, 34, 57, 38, 25, 250),

        // === Island 6: Envy (Air) ===
        ("enemy_envy_stalker",       "Stalker",           CombatUnit.Element.Air,    170, 18, 41, 25, 21, 51),
        ("enemy_envy_mimic",         "Mimic",             CombatUnit.Element.Air,    175, 18, 42, 25, 21, 52),
        ("enemy_envy_covet",         "Covetous One",      CombatUnit.Element.Air,    180, 19, 44, 26, 22, 54),
        ("enemy_envy_shade",         "Shade",             CombatUnit.Element.Air,    185, 19, 45, 27, 22, 55),
        ("enemy_envy_boss",          "The Usurper",       CombatUnit.Element.Air,    650, 37, 61, 41, 27, 275),

        // === Island 7: Gluttony (Earth) ===
        ("enemy_gluttony_feast",     "Feast",             CombatUnit.Element.Earth, 190, 20, 46, 28, 23, 56),
        ("enemy_gluttony_gourmand",  "Gourmand",          CombatUnit.Element.Earth, 195, 20, 48, 29, 24, 58),
        ("enemy_gluttony_maw",       "Maw",               CombatUnit.Element.Earth, 198, 20, 49, 29, 25, 59),
        ("enemy_gluttony_syrup",     "Syrup",             CombatUnit.Element.Earth, 200, 20, 50, 30, 25, 60),
        ("enemy_gluttony_boss",      "The Devourer",      CombatUnit.Element.Earth, 700, 40, 65, 45, 28, 300),
    };

    [MenuItem("TIDE/Populate Enemy Data (All 7 Islands)")]
    public static void PopulateAll()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = System.IO.Path.GetDirectoryName(OutputFolder);
            string folderName = System.IO.Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        int skipped = 0;

        foreach (var def in AllEnemies)
        {
            string path = $"{OutputFolder}/{def.id}.asset";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) != null)
            {
                skipped++;
                continue;
            }

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyId = def.id;
            data.displayName = def.name;
            data.element = def.element;
            data.baseMaxHP = def.hp;
            data.baseMaxMP = def.mp;
            data.baseAttack = def.atk;
            data.baseDefense = def.def;
            data.baseSpeed = def.spd;
            data.baseCritRate = 0.05f;
            data.baseCritDamage = 1.5f;
            data.xpReward = def.xp;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EnemyDataPopulator] Created {created} new, skipped {skipped} existing");
        EditorUtility.DisplayDialog("Enemy Data Populated",
            $"Created {created} new enemy assets.\nSkipped {skipped} already existing.\n\nTotal: {AllEnemies.Length} enemies across 7 islands.",
            "OK");
    }

    [MenuItem("TIDE/Populate Enemy Data (All 7 Islands)", true)]
    public static bool PopulateAllValidate()
    {
        return !EditorApplication.isPlaying;
    }
}
