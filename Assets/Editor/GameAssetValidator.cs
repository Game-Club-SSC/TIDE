using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive editor validation tool that scans all TIDE ScriptableObjects
/// and reports broken references, empty arrays, missing assets, and data issues.
///
/// Access via: TIDE > Validate All Assets
/// </summary>
public static class GameAssetValidator
{
    private static readonly List<string> warnings = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static readonly List<string> infos = new List<string>();

    [MenuItem("TIDE/Validate All Assets")]
    public static void ValidateAll()
    {
        warnings.Clear();
        errors.Clear();
        infos.Clear();

        ValidateEnemyData();
        ValidateIslandConfigs();
        ValidateEncounterConfigs();
        ValidatePuzzleData();
        ValidateSkillData();
        ValidateHeroData();
        ValidateAncientTexts();
        ValidateDialogueData();
        ValidateAudioManager();
        ValidateBuildScenes();

        string report = GenerateReport();

        if (errors.Count > 0)
        {
            Debug.LogError($"[GameAssetValidator] Validation failed with {errors.Count} errors:\n{report}");
            EditorUtility.DisplayDialog("Validation FAILED", $"{errors.Count} errors found.\n\nSee Console for details.", "OK");
        }
        else if (warnings.Count > 0)
        {
            Debug.LogWarning($"[GameAssetValidator] Validation passed with {warnings.Count} warnings:\n{report}");
            EditorUtility.DisplayDialog("Validation OK (with warnings)", $"{warnings.Count} warnings found.\n\nSee Console for details.", "OK");
        }
        else
        {
            Debug.Log($"[GameAssetValidator] All checks passed:\n{report}");
            EditorUtility.DisplayDialog("Validation Passed", "All asset checks passed!", "OK");
        }
    }

    [MenuItem("TIDE/Validate All Assets", true)]
    public static bool ValidateAllValidate()
    {
        return !EditorApplication.isPlaying;
    }

    // ======================================================================
    //  Enemy Data
    // ======================================================================

    private static void ValidateEnemyData()
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/Resources" });
        int valid = 0, invalid = 0, emptySkills = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.enemyId) || string.IsNullOrEmpty(data.displayName))
            {
                errors.Add($"[EnemyData] Invalid: {path} — missing required fields");
                invalid++;
                continue;
            }

            if (data.skills == null || data.skills.Length == 0)
            {
                warnings.Add($"[EnemyData] {data.enemyId} ({data.displayName}) — no skills assigned");
                emptySkills++;
            }

            valid++;
        }

        infos.Add($"EnemyData: {valid} valid, {invalid} invalid, {emptySkills} with no skills");
    }

    // ======================================================================
    //  Island Configs
    // ======================================================================

    private static void ValidateIslandConfigs()
    {
        string[] guids = AssetDatabase.FindAssets("t:IslandConfig", new[] { "Assets/Resources" });
        int valid = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IslandConfig data = AssetDatabase.LoadAssetAtPath<IslandConfig>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.islandId))
            {
                errors.Add($"[IslandConfig] Missing islandId: {path}");
                continue;
            }

            if (data.encounters == null || data.encounters.Length == 0)
            {
                errors.Add($"[IslandConfig] {data.islandId} — no encounters defined");
                continue;
            }

            // Check encounter sequence
            int combatCount = 0, puzzleCount = 0;
            bool hasBoss = false;
            foreach (var enc in data.encounters)
            {
                if (enc.type == 0) combatCount++;
                else puzzleCount++;
                if (enc.encounterId.Contains("boss")) hasBoss = true;
            }

            if (combatCount < 2)
            {
                warnings.Add($"[IslandConfig] {data.islandId} — only {combatCount} combat encounters (expect 3-4)");
            }

            if (!hasBoss)
            {
                warnings.Add($"[IslandConfig] {data.islandId} — no boss encounter");
            }

            valid++;
        }

        infos.Add($"IslandConfig: {valid} islands configured");
    }

    // ======================================================================
    //  Encounter Configs
    // ======================================================================

    private static void ValidateEncounterConfigs()
    {
        string[] guids = AssetDatabase.FindAssets("t:EncounterConfig", new[] { "Assets/Resources" });
        int valid = 0, emptyEnemies = 0, missingRefs = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterConfig data = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.encounterId))
            {
                errors.Add($"[EncounterConfig] Missing encounterId: {path}");
                continue;
            }

            if (data.enemies == null || data.enemies.Length == 0)
            {
                warnings.Add($"[EncounterConfig] {data.encounterId} — no enemies assigned");
                emptyEnemies++;
            }
            else
            {
                foreach (var enemy in data.enemies)
                {
                    if (enemy == null)
                    {
                        warnings.Add($"[EncounterConfig] {data.encounterId} — has null enemy reference");
                        missingRefs++;
                    }
                }
            }

            valid++;
        }

        infos.Add($"EncounterConfig: {valid} encounters, {emptyEnemies} empty, {missingRefs} null refs");
    }

    // ======================================================================
    //  Puzzle Data
    // ======================================================================

    private static void ValidatePuzzleData()
    {
        string[] guids = AssetDatabase.FindAssets("t:PuzzleData", new[] { "Assets/Resources" });
        int valid = 0, emptyTiles = 0, badGrid = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PuzzleData data = AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
            if (data == null) continue;

            int expected = data.gridRows * data.gridCols;
            if (data.tileValues == null || data.tileValues.Length == 0)
            {
                warnings.Add($"[PuzzleData] {path} — no tile data");
                emptyTiles++;
            }
            else if (data.tileValues.Length != expected)
            {
                warnings.Add($"[PuzzleData] {path} — tileValues has {data.tileValues.Length} entries, expected {expected} ({data.gridCols}x{data.gridRows})");
                badGrid++;
            }

            // Validate sealed positions are within grid
            if (data.sealedPositions != null)
            {
                foreach (var pos in data.sealedPositions)
                {
                    if (pos.x < 0 || pos.x >= data.gridCols || pos.y < 0 || pos.y >= data.gridRows)
                    {
                        errors.Add($"[PuzzleData] {path} — sealed position ({pos.x},{pos.y}) out of grid bounds ({data.gridCols}x{data.gridRows})");
                    }
                }
            }

            valid++;
        }

        infos.Add($"PuzzleData: {valid} puzzles, {emptyTiles} empty, {badGrid} grid mismatch");
    }

    // ======================================================================
    //  Skill Data
    // ======================================================================

    private static void ValidateSkillData()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Resources" });
        int valid = 0, invalid = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.skillName))
            {
                errors.Add($"[SkillData] Missing skillName: {path}");
                invalid++;
                continue;
            }

            if (data.mpCost < 0)
            {
                warnings.Add($"[SkillData] {data.skillName} — negative MP cost");
            }

            if (data.damageMultiplier < 0f)
            {
                warnings.Add($"[SkillData] {data.skillName} — negative damage multiplier");
            }

            valid++;
        }

        infos.Add($"SkillData: {valid} valid, {invalid} invalid");

        if (valid < 15)
        {
            warnings.Add($"[SkillData] Only {valid} skills — expected 25 for 5 heroes with 5 abilities each");
        }
    }

    // ======================================================================
    //  Hero Data
    // ======================================================================

    private static void ValidateHeroData()
    {
        string[] guids = AssetDatabase.FindAssets("t:HeroData", new[] { "Assets/Resources" });
        int valid = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            HeroData data = AssetDatabase.LoadAssetAtPath<HeroData>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.heroId))
            {
                errors.Add($"[HeroData] Missing heroId: {path}");
                continue;
            }

            if (data.starterSkills == null || data.starterSkills.Length == 0)
            {
                warnings.Add($"[HeroData] {data.heroId} ({data.displayName}) — no starter skills assigned");
            }

            valid++;
        }

        infos.Add($"HeroData: {valid} heroes defined");

        if (valid < 5)
        {
            warnings.Add($"[HeroData] Only {valid} heroes — expected 5 (Fire, Water, Earth, Air, Space)");
        }
    }

    // ======================================================================
    //  Ancient Texts
    // ======================================================================

    private static void ValidateAncientTexts()
    {
        string[] guids = AssetDatabase.FindAssets("t:AncientTextData", new[] { "Assets/Resources" });
        int valid = 0, emptyBody = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AncientTextData data = AssetDatabase.LoadAssetAtPath<AncientTextData>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.textId))
            {
                errors.Add($"[AncientTextData] Missing textId: {path}");
                continue;
            }

            if (string.IsNullOrEmpty(data.body))
            {
                warnings.Add($"[AncientTextData] {data.textId} — empty body text");
                emptyBody++;
            }

            valid++;
        }

        infos.Add($"AncientTexts: {valid} texts, {emptyBody} empty");

        if (valid < 7)
        {
            warnings.Add($"[AncientTexts] Only {valid} texts — expected at least 10 (intro + deep for most islands)");
        }
    }

    // ======================================================================
    //  Dialogue Data (NEW)
    // ======================================================================

    private static void ValidateDialogueData()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueData", new[] { "Assets/Resources" });
        int valid = 0, emptyBeats = 0, missingIsland = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null) continue;

            if (string.IsNullOrEmpty(data.chapterId))
            {
                errors.Add($"[DialogueData] Missing chapterId: {path}");
                continue;
            }

            if (string.IsNullOrEmpty(data.islandId))
            {
                warnings.Add($"[DialogueData] {data.chapterId} — no islandId assigned");
                missingIsland++;
            }

            if (data.storyBeats == null || data.storyBeats.Length == 0)
            {
                warnings.Add($"[DialogueData] {data.chapterId} — no story beats");
                emptyBeats++;
            }
            else
            {
                int totalLines = 0;
                foreach (var beat in data.storyBeats)
                {
                    if (beat.lines != null) totalLines += beat.lines.Length;
                }
                if (totalLines == 0)
                {
                    warnings.Add($"[DialogueData] {data.chapterId} — story beats have no dialogue lines");
                }
            }

            valid++;
        }

        infos.Add($"DialogueData: {valid} chapters, {emptyBeats} empty, {missingIsland} no island");

        if (valid < 7)
        {
            warnings.Add($"[DialogueData] Only {valid} chapters — expected 7 (one per island)");
        }
    }

    // ======================================================================
    //  Audio Manager
    // ======================================================================

    private static void ValidateAudioManager()
    {
        AudioManager am = Object.FindFirstObjectByType<AudioManager>();
        if (am == null)
        {
            warnings.Add("[AudioManager] No AudioManager found in scene — audio won't play");
            return;
        }

        // Count assigned clips via SerializedObject
        SerializedObject so = new SerializedObject(am);
        int totalSlots = 0, assignedSlots = 0;

        SerializedProperty prop = so.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.type == "AudioClip")
            {
                totalSlots++;
                if (prop.objectReferenceValue != null)
                {
                    assignedSlots++;
                }
            }
        }

        infos.Add($"AudioManager: {assignedSlots}/{totalSlots} audio clip slots assigned");

        if (assignedSlots == 0)
        {
            warnings.Add("[AudioManager] No audio clips assigned — will use procedural fallbacks");
        }
        else if (assignedSlots < totalSlots * 0.5f)
        {
            warnings.Add($"[AudioManager] Only {assignedSlots}/{totalSlots} clips assigned — many sounds will be silent");
        }
    }

    // ======================================================================
    //  Build Scenes
    // ======================================================================

    private static void ValidateBuildScenes()
    {
        var scenes = new List<string>(EditorBuildSettings.scenes.Select(s => s.path));
        int totalScenes = scenes.Count;

        string[] requiredScenes = {
            "Assets/Scenes/level_1.unity",
            "Assets/Scenes/CombatScene.unity",
            "Assets/Scenes/PuzzleScene.unity",
            "Assets/Scenes/level_greed.unity",
            "Assets/Scenes/level_lust.unity",
            "Assets/Scenes/level_wrath.unity",
            "Assets/Scenes/level_sloth.unity",
            "Assets/Scenes/level_pride.unity",
            "Assets/Scenes/level_envy.unity",
            "Assets/Scenes/level_gluttony.unity"
        };

        int missing = 0;
        foreach (string scene in requiredScenes)
        {
            if (!scenes.Contains(scene))
            {
                warnings.Add($"[BuildScenes] Missing from build: {scene}");
                missing++;
            }
        }

        infos.Add($"BuildScenes: {totalScenes} scenes in build, {missing} missing");

        // Check if CombatScene is enabled
        var combatScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.path.Contains("CombatScene"));
        if (combatScene != null && !combatScene.enabled)
        {
            warnings.Add("[BuildScenes] CombatScene exists but is DISABLED in build settings");
        }
    }

    // ======================================================================
    //  Report Generation
    // ======================================================================

    private static string GenerateReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TIDE Asset Validation Report ===");
        sb.AppendLine();

        if (infos.Count > 0)
        {
            sb.AppendLine("--- Summary ---");
            foreach (string info in infos) sb.AppendLine($"  {info}");
            sb.AppendLine();
        }

        if (errors.Count > 0)
        {
            sb.AppendLine($"--- ERRORS ({errors.Count}) ---");
            foreach (string err in errors) sb.AppendLine($"  [ERROR] {err}");
            sb.AppendLine();
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine($"--- WARNINGS ({warnings.Count}) ---");
            foreach (string warn in warnings) sb.AppendLine($"  [WARN] {warn}");
            sb.AppendLine();
        }

        if (errors.Count == 0 && warnings.Count == 0)
        {
            sb.AppendLine("All checks passed!");
        }

        return sb.ToString();
    }
}
