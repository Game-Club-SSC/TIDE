using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class WrathIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_wrath";
    private const string ExpectedViceName = "Wrath";
    private const string ExpectedNextIslandId = "island_envy";
    private const string SceneRelativePath = "Scenes/level_wrath.unity";

    [ContextMenu("Run Wrath Island Verification")]
    public void RunTests()
    {
        Debug.Log("=== Starting Wrath Island Verification ===");

        IslandConfig config = TestIslandConfigExists();
        TestProgressionUnlocksNext();
        TestEncounterLayout(config);
        TestRestorationBudget(config);
        TestBossEncounter(config);
        TestPuzzleEncounters(config);
        TestSceneExists();

        Debug.Log("=== Wrath Island Verification Passed ===");
    }

    private IslandConfig TestIslandConfigExists()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Wrath island config should load from Resources/Islands.");
        Assert.AreEqual(ExpectedViceName, config.viceName, "Wrath island should expose the correct vice name.");
        Assert.IsNotNull(config.encounters, "Wrath island should define an ordered encounter list.");
        return config;
    }

    private void TestProgressionUnlocksNext()
    {
        Assert.AreEqual(ExpectedNextIslandId, IslandThemeRegistry.GetNextIslandId(IslandId),
            "Clearing Wrath should unlock Envy next in progression order.");
    }

    private void TestEncounterLayout(IslandConfig config)
    {
        Assert.AreEqual(9, config.encounters.Length, "Wrath should have 9 ordered encounters (4 combat, 4 puzzle, 1 boss).");

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

        Assert.AreEqual(5, combat, "Wrath should have 5 combat encounters (4 standard plus the boss).");
        Assert.AreEqual(4, puzzle, "Wrath should have 4 puzzle encounters.");
    }

    private void TestRestorationBudget(IslandConfig config)
    {
        float total = 0f;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            total += config.encounters[i].restorationValue;
        }

        Assert.AreEqual(1f, total, 0.001f, "Wrath restoration budget should sum to 100%.");
    }

    private void TestBossEncounter(IslandConfig config)
    {
        EncounterDefinition boss = config.encounters[config.encounters.Length - 1];
        Assert.IsTrue(!string.IsNullOrEmpty(boss.encounterId)
            && boss.encounterId.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0,
            "Wrath's final encounter should be the boss (its id should contain 'boss').");
        Assert.AreEqual(EncounterType.Combat, boss.type, "Wrath boss should be a combat encounter.");
        Assert.AreEqual(0.25f, boss.restorationValue, 0.001f, "Wrath boss should contribute 25% restoration.");
        Assert.IsNotNull(boss.encounterConfig, "Wrath boss should reference an EncounterConfig.");
        Assert.Greater(boss.encounterConfig.EnemyCount, 0, "Wrath boss encounter should include at least one enemy.");
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

        Assert.AreEqual(4, puzzleCount, "Wrath should contain 4 puzzle encounters.");
    }

    private void TestSceneExists()
    {
        string scenePath = System.IO.Path.Combine(Application.dataPath, SceneRelativePath);
        Assert.IsTrue(System.IO.File.Exists(scenePath),
            $"Wrath scene file should exist at Assets/{SceneRelativePath}.");
    }
}
