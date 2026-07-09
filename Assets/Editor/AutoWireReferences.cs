using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Automatically wires cross-references between TIDE ScriptableObjects.
/// After running all populators, run this to connect:
///   - HeroData.starterSkills → SkillData assets
///   - IslandConfig.encounters → EncounterConfig + PuzzleData assets
///
/// Access via: TIDE > Auto-Wire All References
/// </summary>
public static class AutoWireReferences
{
    [MenuItem("TIDE/Auto-Wire All References")]
    public static void WireAll()
    {
        int heroWired = WireHeroSkills();
        int islandWired = WireIslandEncounters();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AutoWire] Done: {heroWired} heroes wired, {islandWired} islands wired");
        EditorUtility.DisplayDialog("Auto-Wire Complete",
            $"Wired {heroWired} heroes with skills.\n" +
            $"Wired {islandWired} islands with encounters.\n\n" +
            "Run 'Validate All Assets' to check for issues.",
            "OK");
    }

    [MenuItem("TIDE/Auto-Wire All References", true)]
    public static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }

    // ======================================================================
    //  Hero → Skill Wiring
    // ======================================================================

    // Maps hero asset names to their skill ID prefixes
    private static readonly Dictionary<string, string[]> HeroSkillMap = new Dictionary<string, string[]>
    {
        { "hero_fire",  new[] { "skill_killian_basic", "skill_killian_searing_arc", "skill_killian_blaze_flurry", "skill_killian_cinder_vent", "skill_killian_immolation" } },
        { "hero_water", new[] { "skill_merrick_basic", "skill_merrick_soothing", "skill_merrick_rolling_wave", "skill_merrick_pain_absorb", "skill_merrick_undertow" } },
        { "hero_earth", new[] { "skill_freida_basic", "skill_freida_root_snare", "skill_freida_bramble_wall", "skill_freida_seismic_volley", "skill_freida_ancient_grove" } },
        { "hero_air",   new[] { "skill_briar_basic", "skill_briar_gale_redirect", "skill_briar_silencing_waltz", "skill_briar_scattering_step", "skill_briar_tempest_dance" } },
        { "hero_space", new[] { "skill_aether_basic", "skill_aether_astral_lance", "skill_aether_nebula_burst", "skill_aether_gravity_well", "skill_aether_event_horizon" } },
    };

    private static int WireHeroSkills()
    {
        string skillFolder = "Assets/Resources/Skills";
        int wired = 0;

        // Load all skills into a lookup
        var skillLookup = new Dictionary<string, SkillData>();
        if (AssetDatabase.IsValidFolder(skillFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { skillFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (skill != null && !string.IsNullOrEmpty(skill.skillName))
                {
                    // Use filename as key
                    string key = Path.GetFileNameWithoutExtension(path);
                    skillLookup[key] = skill;
                }
            }
        }

        if (skillLookup.Count == 0)
        {
            Debug.LogWarning("[AutoWire] No SkillData assets found. Run 'Populate Hero Skills' first.");
            return 0;
        }

        Debug.Log($"[AutoWire] Loaded {skillLookup.Count} skills");

        // Wire each hero
        foreach (var kvp in HeroSkillMap)
        {
            string heroPath = $"Assets/Resources/HeroData/{kvp.Key}.asset";
            HeroData hero = AssetDatabase.LoadAssetAtPath<HeroData>(heroPath);
            if (hero == null)
            {
                Debug.LogWarning($"[AutoWire] Hero not found: {heroPath}");
                continue;
            }

            var skills = new List<SkillData>();
            foreach (string skillId in kvp.Value)
            {
                if (skillLookup.TryGetValue(skillId, out SkillData skill))
                {
                    skills.Add(skill);
                }
                else
                {
                    Debug.LogWarning($"[AutoWire] Skill not found: {skillId} for hero {hero.heroId}");
                }
            }

            if (skills.Count > 0)
            {
                hero.starterSkills = skills.ToArray();
                EditorUtility.SetDirty(hero);
                wired++;
                Debug.Log($"[AutoWire] Wired {hero.heroId} → {skills.Count} skills");
            }
        }

        return wired;
    }

    // ======================================================================
    //  Island → Encounter + Puzzle Wiring
    // ======================================================================

    // Maps island asset names to their encounter ID prefixes
    // Note: island_anger = Anger, island_desire = Desire, island_ego = Ego
    private static readonly Dictionary<string, string[]> IslandEncounterMap = new Dictionary<string, string[]>
    {
        { "island_greed", new[] { "greed_c1", "greed_p1", "greed_c2", "greed_p2", "greed_c3", "greed_p3", "greed_c4", "greed_p4", "greed_boss" } },
        { "island_lust",  new[] { "lust_c1", "lust_p1", "lust_c2", "lust_p2", "lust_c3", "lust_p3", "lust_c4", "lust_p4", "lust_boss" } },
        { "island_anger", new[] { "anger_c1", "anger_p1", "anger_c2", "anger_p2", "anger_c3", "anger_p3", "anger_c4", "anger_p4", "anger_boss" } },
        { "island_desire",new[] { "desire_c1", "desire_p1", "desire_c2", "desire_p2", "desire_c3", "desire_p3", "desire_c4", "desire_p4", "desire_boss" } },
        { "island_ego",   new[] { "ego_c1", "ego_p1", "ego_c2", "ego_p2", "ego_c3", "ego_p3", "ego_c4", "ego_p4", "ego_boss" } },
        { "island_envy",  new[] { "envy_c1", "envy_p1", "envy_c2", "envy_p2", "envy_c3", "envy_p3", "envy_c4", "envy_p4", "envy_boss" } },
        { "island_greed", new[] { "greed_c1", "greed_p1", "greed_c2", "greed_p2", "greed_c3", "greed_p3", "greed_c4", "greed_p4", "greed_boss" } },
    };

    // Maps encounter IDs to their puzzle data asset names
    private static readonly Dictionary<string, string> EncounterPuzzleMap = new Dictionary<string, string>
    {
        { "greed_p1", "puzzle_greed_p1" }, { "greed_p2", "puzzle_greed_p2" },
        { "greed_p3", "puzzle_greed_p3" }, { "greed_p4", "puzzle_greed_p4" },
        { "lust_p1", "puzzle_lust_p1" }, { "lust_p2", "puzzle_lust_p2" },
        { "lust_p3", "puzzle_lust_p3" }, { "lust_p4", "puzzle_lust_p4" },
        { "anger_p1", "puzzle_anger_p1" }, { "anger_p2", "puzzle_anger_p2" },
        { "anger_p3", "puzzle_anger_p3" }, { "anger_p4", "puzzle_anger_p4" },
        { "desire_p1", "puzzle_desire_p1" }, { "desire_p2", "puzzle_desire_p2" },
        { "desire_p3", "puzzle_desire_p3" }, { "desire_p4", "puzzle_desire_p4" },
        { "ego_p1", "puzzle_ego_p1" }, { "ego_p2", "puzzle_ego_p2" },
        { "ego_p3", "puzzle_ego_p3" }, { "ego_p4", "puzzle_ego_p4" },
        { "envy_p1", "puzzle_envy_p1" }, { "envy_p2", "puzzle_envy_p2" },
        { "envy_p3", "puzzle_envy_p3" }, { "envy_p4", "puzzle_envy_p4" },
        { "greed_p1", "puzzle_greed_p1" }, { "greed_p2", "puzzle_greed_p2" },
        { "greed_p3", "puzzle_greed_p3" }, { "greed_p4", "puzzle_greed_p4" },
    };

    // Restoration values per encounter type
    private static float GetRestoration(string encId)
    {
        if (encId.Contains("_boss")) return 0.25f;
        if (encId.Contains("_p")) return 0.0625f;
        return 0.125f;
    }

    private static int WireIslandEncounters()
    {
        int wired = 0;

        // Load all encounter configs into lookup
        var encLookup = new Dictionary<string, EncounterConfig>();
        string[] encGuids = AssetDatabase.FindAssets("t:EncounterConfig", new[] { "Assets/Resources/Encounters" });
        foreach (string guid in encGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterConfig enc = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            if (enc != null)
            {
                encLookup[enc.encounterId] = enc;
            }
        }

        // Load all puzzle data into lookup
        var puzzleLookup = new Dictionary<string, PuzzleData>();
        string[] puzzleGuids = AssetDatabase.FindAssets("t:PuzzleData", new[] { "Assets/Resources/Puzzles" });
        foreach (string guid in puzzleGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PuzzleData puzzle = AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
            if (puzzle != null)
            {
                string key = Path.GetFileNameWithoutExtension(path);
                puzzleLookup[key] = puzzle;
            }
        }

        Debug.Log($"[AutoWire] Loaded {encLookup.Count} encounters, {puzzleLookup.Count} puzzles");

        // Wire each island
        foreach (var kvp in IslandEncounterMap)
        {
            string islandPath = $"Assets/Resources/Islands/{kvp.Key}.asset";
            IslandConfig island = AssetDatabase.LoadAssetAtPath<IslandConfig>(islandPath);
            if (island == null)
            {
                Debug.LogWarning($"[AutoWire] Island not found: {islandPath}");
                continue;
            }

            var definitions = new List<EncounterDefinition>();

            foreach (string encId in kvp.Value)
            {
                var def = new EncounterDefinition
                {
                    encounterId = encId,
                    restorationValue = GetRestoration(encId),
                    isBossEncounter = encId.Contains("_boss"),
                    type = encId.Contains("_p") ? EncounterType.Puzzle : EncounterType.Combat,
                };

                // Wire EncounterConfig reference
                if (encLookup.TryGetValue(encId, out EncounterConfig encConfig))
                {
                    def.encounterConfig = encConfig;
                    def.enemyComposition = EnemyComposition.FromEncounterConfig(encConfig);
                }
                else
                {
                    Debug.LogWarning($"[AutoWire] EncounterConfig not found: {encId}");
                }

                // Wire PuzzleData reference for puzzle encounters
                if (def.type == EncounterType.Puzzle && EncounterPuzzleMap.TryGetValue(encId, out string puzzleId))
                {
                    if (puzzleLookup.TryGetValue(puzzleId, out PuzzleData puzzle))
                    {
                        def.puzzleData = puzzle;
                    }
                    else
                    {
                        Debug.LogWarning($"[AutoWire] PuzzleData not found: {puzzleId}");
                    }
                }

                definitions.Add(def);
            }

            island.encounters = definitions.ToArray();
            EditorUtility.SetDirty(island);
            wired++;
            Debug.Log($"[AutoWire] Wired {kvp.Key} → {definitions.Count} encounters");
        }

        return wired;
    }
}
