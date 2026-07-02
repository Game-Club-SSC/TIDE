using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the balance pass system.
/// Verifies element advantage feels meaningful and no single element dominates.
/// </summary>
[DisallowMultipleComponent]
public class BalancePassTest : MonoBehaviour
{
    [ContextMenu("Run Balance Pass Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Balance Pass Tests ===");

        TestElementAdvantageMultipliers();
        TestNoSingleElementDominance();
        TestGearSetBonusBalance();
        TestLevelingConfigBalance();

        Debug.Log("=== All Balance Pass Tests Passed ===");
    }

    private void TestElementAdvantageMultipliers()
    {
        Debug.Log("Testing element advantage multipliers...");

        // Strong multiplier should be 1.5x (noticeable but not dominant)
        float strongMultiplier = ElementMatchup.StrongMultiplier;
        Assert.AreEqual(1.5f, strongMultiplier, 0.01f, "Strong multiplier should be 1.5x.");

        // Weak multiplier should be 0.67x (noticeable disadvantage)
        float weakMultiplier = ElementMatchup.WeakMultiplier;
        Assert.AreEqual(0.67f, weakMultiplier, 0.01f, "Weak multiplier should be 0.67x.");

        // Neutral should be 1.0x
        float neutralMultiplier = ElementMatchup.NeutralMultiplier;
        Assert.AreEqual(1.0f, neutralMultiplier, 0.01f, "Neutral multiplier should be 1.0x.");

        Debug.Log($"Element multipliers: strong={strongMultiplier}x, weak={weakMultiplier}x, neutral={neutralMultiplier}x");
    }

    private void TestNoSingleElementDominance()
    {
        Debug.Log("Testing no single element dominates...");

        // Each element should beat exactly 2 other elements
        CombatUnit.Element[] allElements = {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        foreach (CombatUnit.Element element in allElements)
        {
            int strongCount = 0;
            int weakCount = 0;

            foreach (CombatUnit.Element opponent in allElements)
            {
                if (element == opponent) continue;

                MatchupResult result = ElementMatchup.GetResult(element, opponent);
                if (result == MatchupResult.Strong) strongCount++;
                if (result == MatchupResult.Weak) weakCount++;
            }

            // Each element should beat exactly 2 and lose to exactly 2
            Assert.AreEqual(2, strongCount, $"{element} should beat exactly 2 elements.");
            Assert.AreEqual(2, weakCount, $"{element} should lose to exactly 2 elements.");

            Debug.Log($"{element}: beats {strongCount}, loses to {weakCount}");
        }

        Debug.Log("No single element dominance: PASS");
    }

    private void TestGearSetBonusBalance()
    {
        Debug.Log("Testing gear set bonus balance...");

        // Create a test gear set
        GearSetData testGear = GearSetFactory.Build(
            "test_set", "Test Set", CombatUnit.Element.Fire, 0,
            0.05f, 0.10f, 0.10f,
            0.05f, 0.10f, 0.10f,
            "Test gear set.");

        // Verify bonuses are within reasonable ranges
        Assert.GreaterOrEqual(testGear.attackBonusPercent, 0f, "Attack bonus should be non-negative.");
        Assert.LessOrEqual(testGear.attackBonusPercent, 0.25f, "Attack bonus should not exceed 25%.");

        Assert.GreaterOrEqual(testGear.defenseBonusPercent, 0f, "Defense bonus should be non-negative.");
        Assert.LessOrEqual(testGear.defenseBonusPercent, 0.25f, "Defense bonus should not exceed 25%.");

        Assert.GreaterOrEqual(testGear.hpBonusPercent, 0f, "HP bonus should be non-negative.");
        Assert.LessOrEqual(testGear.hpBonusPercent, 0.25f, "HP bonus should not exceed 25%.");

        // Set bonuses should be slightly higher than individual bonuses
        Assert.GreaterOrEqual(testGear.setBonusAttackPercent, testGear.attackBonusPercent,
            "Set bonus should be at least as high as individual bonus.");

        Debug.Log($"Gear set bonuses: ATK={testGear.attackBonusPercent:P0}, DEF={testGear.defenseBonusPercent:P0}, HP={testGear.hpBonusPercent:P0}");
        Debug.Log("Gear set bonus balance: PASS");
    }

    private void TestLevelingConfigBalance()
    {
        Debug.Log("Testing leveling config balance...");

        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();

        // Verify leveling curve is reasonable
        int xpAtLevel2 = config.GetXpToNextLevel(1);
        int xpAtLevel3 = config.GetXpToNextLevel(2);

        Assert.Greater(xpAtLevel2, 0, "XP to level 2 should be positive.");
        Assert.Greater(xpAtLevel3, xpAtLevel2, "XP curve should increase with level.");

        // Verify stat growth is positive
        int growth = config.GetExpectedStatGrowth(1, 3);
        Assert.Greater(growth, 0, "Stat growth should be positive.");

        // Verify max level is reasonable
        Assert.GreaterOrEqual(config.maxLevel, 10, "Max level should be at least 10.");
        Assert.LessOrEqual(config.maxLevel, 50, "Max level should not exceed 50.");

        Debug.Log($"Leveling config: baseXP={config.baseXpToLevel}, increment={config.xpPerLevelIncrement}, maxLevel={config.maxLevel}");
        Debug.Log("Leveling config balance: PASS");
    }
}
