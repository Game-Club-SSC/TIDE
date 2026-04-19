using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GluttonyIslandVerificationTest : MonoBehaviour
{
    private const string GluttonyIslandId = "island_gluttony";

    [ContextMenu("Run Gluttony Island Verification")]
    public void RunTests()
    {
        Debug.Log("=== Starting Gluttony Island Verification ===");

        TestGluttonySceneExists();
        TestGluttonyIslandConfigExists();
        TestGluttonyAliasResolvesWithoutRemappingLegacyProgress();
        TestGluttonyProgressionUnlocksGreed();
        TestGluttonyRestorationBudgets();
        TestGluttonyBossUsesDevourSkill();
        TestGluttonyPuzzlesUseConsumption();
        TestGluttonyAncientTextExists();

        Debug.Log("=== Gluttony Island Verification Passed ===");
    }

    private void TestGluttonySceneExists()
    {
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "Scenes/level_gluttony.unity")),
            "Gluttony scene file should exist at Assets/Scenes/level_gluttony.unity.");
    }

    private void TestGluttonyIslandConfigExists()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(GluttonyIslandId);
        Assert.IsNotNull(config, "Gluttony island config should load from Resources/Islands.");
        Assert.AreEqual("Gluttony", config.viceName, "Gluttony island should expose the correct vice name.");
        Assert.AreEqual(9, config.encounters.Length, "Gluttony should have 9 ordered encounters.");
    }

    private void TestGluttonyAliasResolvesWithoutRemappingLegacyProgress()
    {
        Assert.AreEqual("island_greed", IslandThemeRegistry.ResolveIslandId("island_3"),
            "Legacy alias 'island_3' should continue resolving to Greed for save compatibility.");
        Assert.AreEqual(GluttonyIslandId, IslandThemeRegistry.ResolveIslandId("island_8"),
            "Gluttony should be addressable through its dedicated legacy-safe alias.");
    }

    private void TestGluttonyProgressionUnlocksGreed()
    {
        Assert.AreEqual("island_greed", IslandThemeRegistry.GetNextIslandId(GluttonyIslandId),
            "Clearing Gluttony should unlock Greed next in progression order.");
    }

    private void TestGluttonyRestorationBudgets()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(GluttonyIslandId);
        Assert.IsNotNull(config, "Gluttony island config should exist before testing restoration budgets.");

        float combat = 0f;
        float puzzle = 0f;
        for (int i = 0; i < config.encounters.Length - 1; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter.type == EncounterType.Combat)
            {
                combat += encounter.restorationValue;
            }
            else
            {
                puzzle += encounter.restorationValue;
            }
        }

        Assert.AreEqual(0.375f, combat, 0.0001f, "Gluttony combat contribution should be 37.5% before the boss.");
        Assert.AreEqual(0.375f, puzzle, 0.0001f, "Gluttony puzzle contribution should be 37.5% before the boss.");
        Assert.AreEqual(0.25f, config.encounters[8].restorationValue, 0.0001f, "Gluttony boss should contribute 25% restoration.");
    }

    private void TestGluttonyBossUsesDevourSkill()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(GluttonyIslandId);
        EncounterDefinition bossEncounter = config.encounters[8];
        Assert.IsNotNull(bossEncounter.encounterConfig, "Gluttony boss encounter should reference an EncounterConfig.");
        Assert.Greater(bossEncounter.encounterConfig.EnemyCount, 0, "Gluttony boss encounter should include at least one enemy.");

        EnemyData boss = bossEncounter.encounterConfig.GetEnemy(0);
        Assert.IsNotNull(boss, "Gluttony boss enemy should be configured.");
        Assert.AreEqual("The Devourer", boss.displayName, "Gluttony boss should be The Devourer.");
        Assert.IsNotNull(boss.skills, "Gluttony boss should have a configured skill array.");
        Assert.Greater(boss.skills.Length, 0, "Gluttony boss should have at least one skill.");

        bool hasLifeStealSkill = false;
        for (int i = 0; i < boss.skills.Length; i++)
        {
            SkillData skill = boss.skills[i];
            if (skill != null && skill.restoreCasterPercentOfDamage > 0f)
            {
                hasLifeStealSkill = true;
                break;
            }
        }

        Assert.IsTrue(hasLifeStealSkill, "The Devourer should have an HP-steal skill configured.");
    }

    private void TestGluttonyPuzzlesUseConsumption()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(GluttonyIslandId);
        int gluttonyPuzzleCount = 0;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter.type != EncounterType.Puzzle)
            {
                continue;
            }

            gluttonyPuzzleCount++;
            Assert.IsNotNull(encounter.puzzleData, $"Puzzle encounter '{encounter.encounterId}' should reference PuzzleData.");
            Assert.IsTrue(encounter.puzzleData.enableConsumption,
                $"Puzzle encounter '{encounter.encounterId}' should enable the Consumption mechanic.");
            Assert.GreaterOrEqual(encounter.puzzleData.consumptionAmount, 1,
                $"Puzzle encounter '{encounter.encounterId}' should consume at least 1 tide.");
        }

        Assert.GreaterOrEqual(gluttonyPuzzleCount, 3, "Gluttony should contain at least 3 puzzle encounters using Consumption.");
    }

    private void TestGluttonyAncientTextExists()
    {
        AncientTextData text = Resources.Load<AncientTextData>("AncientTexts/text_gluttony_intro");
        Assert.IsNotNull(text, "Gluttony should have an intro ancient text asset.");
        Assert.IsTrue(text.IsValid(), "Gluttony ancient text asset should be valid.");
    }
}
