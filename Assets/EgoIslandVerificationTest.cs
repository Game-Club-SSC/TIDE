using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class EgoIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_ego";
    private const string ExpectedViceName = "Ego";
    private const string SceneRelativePath = "Scenes/level_ego.unity";

    [ContextMenu("Run Ego Island Verification")]
    public void RunTests()
    {
        Debug.Log("=== Starting Ego Island Verification ===");

        IslandConfig config = TestIslandConfigExists();
        TestEgoIsFinalIsland();
        TestEncounterLayout(config);
        TestRestorationBudget(config);
        TestBossEncounter(config);
        TestPuzzleEncounters(config);
        TestSceneExists();

        Debug.Log("=== Ego Island Verification Passed ===");
    }

    private IslandConfig TestIslandConfigExists()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Ego island config should load from Resources/Islands.");
        Assert.AreEqual(ExpectedViceName, config.viceName, "Ego island should expose the correct vice name.");
        Assert.IsNotNull(config.encounters, "Ego island should define an ordered encounter list.");
        return config;
    }

    private void TestEgoIsFinalIsland()
    {
        Assert.AreEqual(IslandId, IslandThemeRegistry.GetNextIslandId(IslandId),
            "Ego is the final island; progression should not advance past it.");
    }

    private void TestEncounterLayout(IslandConfig config)
    {
        Assert.AreEqual(9, config.encounters.Length, "Ego should have 9 ordered encounters (4 combat, 4 puzzle, 1 boss).");

        int combat = 0;
        int puzzle = 0;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            if (config.encounters[i].type == EncounterType.Combat)
            {
                combat++;
            }
            else
            {
                puzzle++;
            }
        }

        Assert.AreEqual(5, combat, "Ego should have 5 combat encounters (4 standard plus the boss).");
        Assert.AreEqual(4, puzzle, "Ego should have 4 puzzle encounters.");
    }

    private void TestRestorationBudget(IslandConfig config)
    {
        float total = 0f;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            total += config.encounters[i].restorationValue;
        }

        Assert.AreEqual(1f, total, 0.001f, "Ego restoration budget should sum to 100%.");
    }

    private void TestBossEncounter(IslandConfig config)
    {
        EncounterDefinition boss = config.encounters[config.encounters.Length - 1];
        Assert.IsTrue(!string.IsNullOrEmpty(boss.encounterId)
            && boss.encounterId.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0,
            "Ego's final encounter should be the boss (its id should contain 'boss').");
        Assert.AreEqual(EncounterType.Combat, boss.type, "Ego boss should be a combat encounter.");
        Assert.AreEqual(0.25f, boss.restorationValue, 0.001f, "Ego boss should contribute 25% restoration.");
        Assert.IsNotNull(boss.encounterConfig, "Ego boss should reference an EncounterConfig.");
        Assert.Greater(boss.encounterConfig.EnemyCount, 0, "Ego boss encounter should include at least one enemy.");
    }

    private void TestPuzzleEncounters(IslandConfig config)
    {
        int puzzleCount = 0;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter.type != EncounterType.Puzzle)
            {
                continue;
            }

            puzzleCount++;
            Assert.IsNotNull(encounter.puzzleData, $"Puzzle encounter '{encounter.encounterId}' should reference PuzzleData.");
        }

        Assert.AreEqual(4, puzzleCount, "Ego should contain 4 puzzle encounters.");
    }

    private void TestSceneExists()
    {
        string scenePath = System.IO.Path.Combine(Application.dataPath, SceneRelativePath);
        Assert.IsTrue(System.IO.File.Exists(scenePath),
            $"Ego scene file should exist at Assets/{SceneRelativePath}.");
    }
}
