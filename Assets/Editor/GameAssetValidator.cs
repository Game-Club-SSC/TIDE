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

    // Island travel happens inside the shared level_1 scene. The V2 islands
    // are selected through IslandThemeRegistry and their IslandConfig assets,
    // not through one Unity scene per island.
    private static readonly string[] CanonicalV2RuntimeScenes =
    {
        "Assets/Scenes/TitleScene.unity",
        "Assets/Scenes/level_1.unity",
        "Assets/Scenes/HubScene.unity",
        "Assets/Scenes/PuzzleScene.unity",
        "Assets/Scenes/CombatScene.unity"
    };

    private static readonly string[] CanonicalV2IslandIds =
    {
        "island_lust",
        "island_greed",
        "island_desire",
        "island_anger",
        "island_envy",
        "island_ego"
    };

    private const string LegacyGluttonyScene = "Assets/Scenes/level_gluttony.unity";

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

            // The central hub is a travel and story space, not a corrupted
            // island. Its empty encounter list is intentional.
            if (IslandThemeRegistry.IsHubIslandId(data.islandId))
            {
                valid++;
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

            Vector2Int dimensions = data.GetResolvedGridDimensions();
            if (dimensions.x <= 0 || dimensions.y <= 0)
            {
                errors.Add($"[PuzzleData] {path} — invalid grid dimensions ({dimensions.x}x{dimensions.y})");
                badGrid++;
                continue;
            }

            long expected = (long)dimensions.y * dimensions.x;
            if (expected > int.MaxValue)
            {
                errors.Add($"[PuzzleData] {path} — grid dimensions are too large ({dimensions.x}x{dimensions.y})");
                badGrid++;
                continue;
            }

            if (data.tileValues == null || data.tileValues.Length == 0)
            {
                warnings.Add($"[PuzzleData] {path} — no tile data");
                emptyTiles++;
            }
            else if (data.tileValues.Length != expected)
            {
                warnings.Add($"[PuzzleData] {path} — tileValues has {data.tileValues.Length} entries, expected {expected} ({dimensions.x}x{dimensions.y})");
                badGrid++;
            }

            // Validate sealed positions are within grid
            if (data.sealedPositions != null)
            {
                foreach (var pos in data.sealedPositions)
                {
                    if (pos.x < 0 || pos.x >= dimensions.x || pos.y < 0 || pos.y >= dimensions.y)
                    {
                        errors.Add($"[PuzzleData] {path} — sealed position ({pos.x},{pos.y}) out of grid bounds ({dimensions.x}x{dimensions.y})");
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
            if (prop.type == "PPtr<AudioClip>")
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
        List<string> releaseIssues = GetCanonicalV2BuildSceneIssues();
        foreach (string issue in releaseIssues)
        {
            errors.Add($"[BuildScenes] {issue}");
        }

        int enabledSceneCount = EditorBuildSettings.scenes.Count(scene => scene != null && scene.enabled);
        infos.Add($"BuildScenes: {enabledSceneCount} enabled, {CanonicalV2RuntimeScenes.Length} required runtime scenes, {CanonicalV2IslandIds.Length} V2 island configs, {releaseIssues.Count} release blockers");
    }

    internal static bool HasCanonicalV2BuildSceneSet(out string report)
    {
        List<string> issues = GetCanonicalV2BuildSceneIssues();
        report = string.Join("\n", issues);
        return issues.Count == 0;
    }

    private static List<string> GetCanonicalV2BuildSceneIssues()
    {
        var issues = new List<string>();
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        foreach (string scenePath in CanonicalV2RuntimeScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                issues.Add($"Required V2 scene asset is missing: {scenePath}");
                continue;
            }

            EditorBuildSettingsScene buildScene = buildScenes.FirstOrDefault(scene => scene != null && scene.path == scenePath);
            if (buildScene == null)
            {
                issues.Add($"Required V2 scene is not in Build Settings: {scenePath}");
            }
            else if (!buildScene.enabled)
            {
                issues.Add($"Required V2 scene is disabled in Build Settings: {scenePath}");
            }
        }

        foreach (string islandId in CanonicalV2IslandIds)
        {
            IslandConfig islandConfig = Resources.Load<IslandConfig>($"Islands/{islandId}");
            if (islandConfig == null)
            {
                issues.Add($"Required V2 island config is missing: Resources/Islands/{islandId}");
            }
            else if (islandConfig.islandId != islandId)
            {
                issues.Add($"V2 island config ID does not match its expected route: {islandId}");
            }
            else if (!islandConfig.IsValid())
            {
                issues.Add($"V2 island config is invalid: Resources/Islands/{islandId}");
            }
        }

        foreach (EditorBuildSettingsScene buildScene in buildScenes)
        {
            if (buildScene == null)
            {
                issues.Add("Build Settings contains a null scene entry.");
                continue;
            }

            if (string.IsNullOrEmpty(buildScene.path))
            {
                issues.Add("Build Settings contains an empty scene path.");
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path) == null)
            {
                issues.Add($"Build Settings references a missing scene asset: {buildScene.path}");
            }

            if (buildScene.enabled && buildScene.path == LegacyGluttonyScene)
            {
                issues.Add($"Legacy Gluttony scene must not be enabled for a V2 release: {LegacyGluttonyScene}");
            }
            else if (buildScene.enabled && !CanonicalV2RuntimeScenes.Contains(buildScene.path))
            {
                issues.Add($"Unexpected enabled release scene: {buildScene.path}");
            }
        }

        return issues;
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
