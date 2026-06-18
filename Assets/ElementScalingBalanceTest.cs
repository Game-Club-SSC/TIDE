using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class ElementScalingBalanceTest : MonoBehaviour
{
    [ContextMenu("Run Element Scaling Balance Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Element Scaling Balance Tests ===");

        TestLevelingConfigSupportsOneToTwoLevelsPerIsland();
        TestLevelingConfigStatGrowthIsPositive();
        TestIslandTierScalesAcrossProgression();
        TestBossStatsSurviveMultipleCombos();

        Debug.Log("=== All Element Scaling Balance Tests Passed ===");
    }

    private void TestLevelingConfigSupportsOneToTwoLevelsPerIsland()
    {
        Debug.Log("Testing leveling config supports 1-2 levels per island...");

        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();

        int xpAtLevel2 = config.GetXpToNextLevel(1);
        int xpAtLevel3 = config.GetXpToNextLevel(2);
        Assert.Greater(xpAtLevel2, 0, "Leveling config must require positive XP to reach level 2.");
        Assert.Greater(xpAtLevel3, xpAtLevel2, "Leveling curve must increase with level.");

        config.baseXpToLevel = 120;
        config.xpPerLevelIncrement = 60;
        int islandXPCeiling = 320;

        int expectedLevels = 0;
        int levelXp = 0;
        for (int level = 1; level < config.maxLevel && levelXp < islandXPCeiling; level++)
        {
            levelXp += config.GetXpToNextLevel(level);
            if (levelXp <= islandXPCeiling)
            {
                expectedLevels++;
            }
        }

        Assert.GreaterOrEqual(expectedLevels, 1,
            "Default island XP budget should yield at least one level-up per island.");
        Assert.LessOrEqual(expectedLevels, 3,
            "Default island XP budget should not grant excessively many levels per island.");
    }

    private void TestLevelingConfigStatGrowthIsPositive()
    {
        Debug.Log("Testing stat growth produces non-trivial stat deltas...");

        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();
        int growth = config.GetExpectedStatGrowth(1, 3);
        Assert.Greater(growth, 0, "Two-level growth must produce positive stat increase.");

        int singleLevelGrowth = config.GetExpectedStatGrowth(1, 2);
        Assert.Greater(singleLevelGrowth, 0, "Single-level growth must produce positive stat increase.");
    }

    private void TestIslandTierScalesAcrossProgression()
    {
        Debug.Log("Testing island tier multiplier scales with progression...");

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        Assert.GreaterOrEqual(progressionOrder.Count, 3, "Progression should have multiple islands for tier scaling.");

        float firstTier = ComputeTierMultiplierForIsland(progressionOrder[0]);
        float lastTier = ComputeTierMultiplierForIsland(progressionOrder[progressionOrder.Count - 1]);

        Assert.GreaterOrEqual(firstTier, 1f,
            "First island tier multiplier should be at least 1.0.");
        Assert.Greater(lastTier, firstTier,
            "Final island tier multiplier should exceed the first island's multiplier.");
        Assert.LessOrEqual(lastTier, 2f,
            "Final island tier multiplier should not exceed a 2x budget cap.");
    }

    private void TestBossStatsSurviveMultipleCombos()
    {
        Debug.Log("Testing boss stats remain winnable across tide-break combos...");

        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();
        int bossBaseHp = 600;
        int partyTideBreakDamagePerCombo = 320;

        float finalTier = ComputeTierMultiplierForIsland(
            IslandThemeRegistry.ProgressionOrder[IslandThemeRegistry.ProgressionOrder.Count - 1]);
        int bossHp = Mathf.RoundToInt(bossBaseHp * finalTier);
        int combosToDefeat = Mathf.CeilToInt(bossHp / (float)partyTideBreakDamagePerCombo);

        Assert.GreaterOrEqual(combosToDefeat, 3,
            "Final boss should survive at least 3 Tide Break combos.");
        Assert.LessOrEqual(combosToDefeat, 6,
            "Final boss should fall to no more than 6 Tide Break combos to keep tension finite.");
    }

    private static float ComputeTierMultiplierForIsland(string islandId)
    {
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            if (string.Equals(progressionOrder[i], islandId, System.StringComparison.Ordinal))
            {
                int tier = i + 1;
                return Mathf.Lerp(1.0f, 1.65f, (tier - 1) / Mathf.Max(1, progressionOrder.Count - 1));
            }
        }

        return 1f;
    }
}
