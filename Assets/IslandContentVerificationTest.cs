using System;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

[DisallowMultipleComponent]
public class IslandContentVerificationTest : MonoBehaviour
{
    private const float ExpectedBossThresholdPercent = 75f;
    private const float ExpectedPreBossContribution = 0.75f;
    private const float ExpectedBossContribution = 0.25f;
    private const float Epsilon = 0.001f;

    private static readonly string[] ExpectedIslandIds =
    {
        "island_lust",
        "island_greed",
        "island_greed",
        "island_desire",
        "island_anger",
        "island_envy",
        "island_ego"
    };

    private static readonly Dictionary<string, Vector2> ExpectedPreBossBuckets = new Dictionary<string, Vector2>
    {
        { "island_greed", new Vector2(0.375f, 0.375f) }
    };

    [ContextMenu("Run Island Content Verification")]
    public void RunTests()
    {
        Debug.Log("=== Starting Island Content Verification ===");

        TestLoadExactIslandSet();
        TestEncounterSequenceAndRestorationBudgets();
        TestDistinctViceColors();
        TestBossEncounterUniqueness();
        TestPuzzleDataVariation();
        TestAncientTextsPerIslandVice();

        Debug.Log("=== All Island Content Verification Tests Passed ===");
    }

    private void TestLoadExactIslandSet()
    {
        Debug.Log("Testing exact island set...");

        IslandConfig[] configs = LoadConfigs();
        Assert.AreEqual(ExpectedIslandIds.Length, configs.Length,
            "Should have exactly seven island configs for the current vertical slice islands.");

        HashSet<string> observedIds = new HashSet<string>();
        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig config = configs[i];
            Assert.IsNotNull(config, $"Island config at index {i} is null.");
            Assert.False(string.IsNullOrEmpty(config.islandId), $"Island config at index {i} has empty islandId.");
            Assert.False(string.IsNullOrEmpty(config.viceName), $"Island '{config.islandId}' has empty viceName.");
            Assert.True(observedIds.Add(config.islandId), $"Duplicate islandId '{config.islandId}'.");
        }

        for (int i = 0; i < ExpectedIslandIds.Length; i++)
        {
            Assert.True(observedIds.Contains(ExpectedIslandIds[i]),
                $"Missing expected island config '{ExpectedIslandIds[i]}'.");
        }

        Debug.Log("  Exact island set verified");
    }

    private void TestEncounterSequenceAndRestorationBudgets()
    {
        Debug.Log("Testing encounter sequence + 75% boss gate budgets...");

        IslandConfig[] configs = LoadConfigs();
        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig config = configs[i];
            Assert.IsNotNull(config.encounters, $"Island '{config.islandId}' has null encounters.");
            Assert.AreEqual(9, config.encounters.Length,
                $"Island '{config.islandId}' should have exactly 9 encounters (4 combat + 4 puzzle + boss).");

            float combatContribution = 0f;
            float puzzleContribution = 0f;

            for (int j = 0; j < 8; j++)
            {
                EncounterDefinition encounter = config.encounters[j];
                Assert.False(string.IsNullOrEmpty(encounter.encounterId),
                    $"Island '{config.islandId}' encounter at index {j} has empty encounterId.");

                if (j % 2 == 0)
                {
                    Assert.AreEqual(EncounterType.Combat, encounter.type,
                        $"Island '{config.islandId}' encounter {j} should be Combat.");
                    Assert.IsNotNull(encounter.encounterConfig,
                        $"Island '{config.islandId}' combat encounter '{encounter.encounterId}' is missing EncounterConfig.");
                    combatContribution += encounter.restorationValue;
                }
                else
                {
                    Assert.AreEqual(EncounterType.Puzzle, encounter.type,
                        $"Island '{config.islandId}' encounter {j} should be Puzzle.");
                    Assert.IsNotNull(encounter.puzzleData,
                        $"Island '{config.islandId}' puzzle encounter '{encounter.encounterId}' is missing PuzzleData.");
                    puzzleContribution += encounter.restorationValue;
                }
            }

            EncounterDefinition bossEncounter = config.encounters[8];
            Assert.AreEqual(EncounterType.Combat, bossEncounter.type,
                $"Island '{config.islandId}' final encounter should be a Combat boss.");
            Assert.True(bossEncounter.encounterId.Contains("boss", StringComparison.OrdinalIgnoreCase),
                $"Island '{config.islandId}' final encounter id should contain 'boss'.");
            Assert.IsNotNull(bossEncounter.encounterConfig,
                $"Island '{config.islandId}' boss encounter is missing EncounterConfig.");

            float preBoss = combatContribution + puzzleContribution;
            float total = preBoss + bossEncounter.restorationValue;
            Vector2 expectedBuckets = GetExpectedPreBossBuckets(config.islandId);

            AssertApproximately(combatContribution, expectedBuckets.x,
                $"Island '{config.islandId}' combat contribution mismatch before boss");
            AssertApproximately(puzzleContribution, expectedBuckets.y,
                $"Island '{config.islandId}' puzzle contribution mismatch before boss");
            AssertApproximately(preBoss, ExpectedBossThresholdPercent / 100f,
                $"Island '{config.islandId}' should unlock boss at 75% restoration");
            AssertApproximately(preBoss, ExpectedPreBossContribution,
                $"Island '{config.islandId}' pre-boss contribution mismatch");
            AssertApproximately(bossEncounter.restorationValue, ExpectedBossContribution,
                $"Island '{config.islandId}' boss contribution should be 25%");
            AssertApproximately(total, 1f,
                $"Island '{config.islandId}' total contribution should equal 100%");
        }

        Debug.Log("  Encounter ordering and restoration budgets verified");
    }

    private void TestDistinctViceColors()
    {
        Debug.Log("Testing distinct vice colors...");

        IslandConfig[] configs = LoadConfigs();
        List<Color> usedColors = new List<Color>();

        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig config = configs[i];
            bool isDistinct = true;

            for (int j = 0; j < usedColors.Count; j++)
            {
                if (ColorEquals(usedColors[j], config.vicePrimaryColor, 0.01f))
                {
                    isDistinct = false;
                    break;
                }
            }

            Assert.True(isDistinct, $"Island '{config.islandId}' has duplicate primary color with another island.");
            usedColors.Add(config.vicePrimaryColor);
        }

        Debug.Log("  Distinct vice color theming verified");
    }

    private void TestBossEncounterUniqueness()
    {
        Debug.Log("Testing unique boss encounter configs...");

        IslandConfig[] configs = LoadConfigs();
        HashSet<EncounterConfig> uniqueBossConfigs = new HashSet<EncounterConfig>();

        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig config = configs[i];
            EncounterDefinition boss = config.encounters[8];
            Assert.IsNotNull(boss.encounterConfig,
                $"Island '{config.islandId}' boss encounter config should not be null.");
            Assert.True(uniqueBossConfigs.Add(boss.encounterConfig),
                $"Island '{config.islandId}' reuses another island boss EncounterConfig.");
        }

        Debug.Log("  Distinct boss encounters verified");
    }

    private void TestPuzzleDataVariation()
    {
        Debug.Log("Testing puzzle variation per island...");

        IslandConfig[] configs = LoadConfigs();
        for (int i = 0; i < configs.Length; i++)
        {
            IslandConfig config = configs[i];
            HashSet<PuzzleData> uniquePuzzleData = new HashSet<PuzzleData>();
            bool hasAllEqualToTarget = false;
            bool hasPercentageAtTarget = false;

            for (int j = 0; j < config.encounters.Length; j++)
            {
                EncounterDefinition encounter = config.encounters[j];
                if (encounter.type != EncounterType.Puzzle)
                {
                    continue;
                }

                Assert.IsNotNull(encounter.puzzleData,
                    $"Island '{config.islandId}' puzzle encounter '{encounter.encounterId}' has null PuzzleData.");
                uniquePuzzleData.Add(encounter.puzzleData);

                PuzzleWinCondition winCondition = encounter.puzzleData.winCondition;
                Assert.IsNotNull(winCondition,
                    $"Island '{config.islandId}' puzzle encounter '{encounter.encounterId}' has null winCondition.");

                if (winCondition.type == WinConditionType.AllEqualToTarget)
                {
                    hasAllEqualToTarget = true;
                }
                else if (winCondition.type == WinConditionType.PercentageAtTarget)
                {
                    hasPercentageAtTarget = true;
                }
            }

            Assert.GreaterOrEqual(uniquePuzzleData.Count, 2,
                $"Island '{config.islandId}' should reference at least 2 distinct puzzle layouts.");
            Assert.True(hasPercentageAtTarget,
                $"Island '{config.islandId}' should include at least one PercentageAtTarget puzzle.");
            Assert.True(hasAllEqualToTarget,
                $"Island '{config.islandId}' should include at least one AllEqualToTarget puzzle.");
        }

        Debug.Log("  Puzzle difficulty and win-condition variation verified");
    }

    private void TestAncientTextsPerIslandVice()
    {
        Debug.Log("Testing ancient text coverage by vice...");

        AncientTextData[] texts = Resources.LoadAll<AncientTextData>("AncientTexts");
        Assert.GreaterOrEqual(texts.Length, ExpectedIslandIds.Length, "Should have at least one ancient text per vice island.");

        for (int i = 0; i < texts.Length; i++)
        {
            Assert.True(texts[i].IsValid(), $"AncientTextData '{texts[i].name}' is invalid.");
        }

        for (int i = 0; i < ExpectedIslandIds.Length; i++)
        {
            string vicePrefix = ExpectedIslandIds[i].Substring("island_".Length);
            bool found = false;

            for (int j = 0; j < texts.Length; j++)
            {
                AncientTextData text = texts[j];
                if (!string.IsNullOrEmpty(text.textId)
                    && text.textId.StartsWith(vicePrefix + "_", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found,
                $"Missing ancient text whose textId starts with '{vicePrefix}_'.");
        }

        Debug.Log("  Ancient text narrative coverage verified");
    }

    private static IslandConfig[] LoadConfigs()
    {
        IslandConfig[] configs = Resources.LoadAll<IslandConfig>("Islands");
        Assert.IsNotNull(configs, "Should load island configs from Resources/Islands/.");
        return configs;
    }

    private static void AssertApproximately(float actual, float expected, string message)
    {
        Assert.LessOrEqual(Mathf.Abs(actual - expected), Epsilon,
            $"{message}. Expected {expected:F4}, got {actual:F4}.");
    }

    private static Vector2 GetExpectedPreBossBuckets(string islandId)
    {
        if (!string.IsNullOrEmpty(islandId) && ExpectedPreBossBuckets.TryGetValue(islandId, out Vector2 buckets))
        {
            return buckets;
        }

        return new Vector2(0.5f, 0.25f);
    }

    private static bool ColorEquals(Color a, Color b, float epsilon)
    {
        return Mathf.Abs(a.r - b.r) < epsilon
            && Mathf.Abs(a.g - b.g) < epsilon
            && Mathf.Abs(a.b - b.b) < epsilon;
    }
}
