using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the element matchup system.
/// Verifies the 5-element rock-paper-scissors advantage system.
/// </summary>
[DisallowMultipleComponent]
public class ElementMatchupTest : MonoBehaviour
{
    [ContextMenu("Run Element Matchup Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Element Matchup Tests ===");

        TestFireBeatsEarthAndAir();
        TestWaterBeatsFireAndSpace();
        TestEarthBeatsWaterAndSpace();
        TestAirBeatsEarthAndWater();
        TestSpaceBeatsFireAndAir();
        TestNeutralMatchups();
        TestDamageMultipliers();
        TestSymmetryOfWeakness();

        Debug.Log("=== All Element Matchup Tests Passed ===");
    }

    private void TestFireBeatsEarthAndAir()
    {
        Debug.Log("Testing Fire beats Earth and Air...");

        MatchupResult fireVsEarth = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Earth);
        MatchupResult fireVsAir = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Air);

        Assert.AreEqual(MatchupResult.Strong, fireVsEarth, "Fire should beat Earth.");
        Assert.AreEqual(MatchupResult.Strong, fireVsAir, "Fire should beat Air.");
        Debug.Log("Fire beats Earth and Air: PASS");
    }

    private void TestWaterBeatsFireAndSpace()
    {
        Debug.Log("Testing Water beats Fire and Space...");

        MatchupResult waterVsFire = ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Fire);
        MatchupResult waterVsSpace = ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Space);

        Assert.AreEqual(MatchupResult.Strong, waterVsFire, "Water should beat Fire.");
        Assert.AreEqual(MatchupResult.Strong, waterVsSpace, "Water should beat Space.");
        Debug.Log("Water beats Fire and Space: PASS");
    }

    private void TestEarthBeatsWaterAndSpace()
    {
        Debug.Log("Testing Earth beats Water and Space...");

        MatchupResult earthVsWater = ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Water);
        MatchupResult earthVsSpace = ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Space);

        Assert.AreEqual(MatchupResult.Strong, earthVsWater, "Earth should beat Water.");
        Assert.AreEqual(MatchupResult.Strong, earthVsSpace, "Earth should beat Space.");
        Debug.Log("Earth beats Water and Space: PASS");
    }

    private void TestAirBeatsEarthAndWater()
    {
        Debug.Log("Testing Air beats Earth and Water...");

        MatchupResult airVsEarth = ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Earth);
        MatchupResult airVsWater = ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Water);

        Assert.AreEqual(MatchupResult.Strong, airVsEarth, "Air should beat Earth.");
        Assert.AreEqual(MatchupResult.Strong, airVsWater, "Air should beat Water.");
        Debug.Log("Air beats Earth and Water: PASS");
    }

    private void TestSpaceBeatsFireAndAir()
    {
        Debug.Log("Testing Space beats Fire and Air...");

        MatchupResult spaceVsFire = ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Fire);
        MatchupResult spaceVsAir = ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Air);

        Assert.AreEqual(MatchupResult.Strong, spaceVsFire, "Space should beat Fire.");
        Assert.AreEqual(MatchupResult.Strong, spaceVsAir, "Space should beat Air.");
        Debug.Log("Space beats Fire and Air: PASS");
    }

    private void TestNeutralMatchups()
    {
        Debug.Log("Testing neutral matchups...");

        // Same element should be neutral
        MatchupResult fireVsFire = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Fire);
        Assert.AreEqual(MatchupResult.Neutral, fireVsFire, "Same element should be neutral.");

        // Element vs None should be neutral
        MatchupResult fireVsNone = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.None);
        Assert.AreEqual(MatchupResult.Neutral, fireVsNone, "Element vs None should be neutral.");

        Debug.Log("Neutral matchups: PASS");
    }

    private void TestDamageMultipliers()
    {
        Debug.Log("Testing damage multipliers...");

        float strongMultiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Fire, CombatUnit.Element.Earth);
        float weakMultiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Fire, CombatUnit.Element.Water);
        float neutralMultiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Fire, CombatUnit.Element.Fire);

        Assert.AreEqual(1.5f, strongMultiplier, 0.01f, "Strong multiplier should be 1.5x.");
        Assert.AreEqual(0.67f, weakMultiplier, 0.01f, "Weak multiplier should be 0.67x.");
        Assert.AreEqual(1.0f, neutralMultiplier, 0.01f, "Neutral multiplier should be 1.0x.");

        Debug.Log($"Damage multipliers: strong={strongMultiplier}x, weak={weakMultiplier}x, neutral={neutralMultiplier}x");
    }

    private void TestSymmetryOfWeakness()
    {
        Debug.Log("Testing symmetry of weakness (if A beats B, B is weak to A)...");

        // Fire beats Earth, so Earth should be weak to Fire
        MatchupResult fireVsEarth = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Earth);
        MatchupResult earthVsFire = ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Fire);

        Assert.AreEqual(MatchupResult.Strong, fireVsEarth, "Fire should be strong vs Earth.");
        Assert.AreEqual(MatchupResult.Weak, earthVsFire, "Earth should be weak vs Fire.");

        // Water beats Fire, so Fire should be weak to Water
        MatchupResult waterVsFire = ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Fire);
        MatchupResult fireVsWater = ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Water);

        Assert.AreEqual(MatchupResult.Strong, waterVsFire, "Water should be strong vs Fire.");
        Assert.AreEqual(MatchupResult.Weak, fireVsWater, "Fire should be weak vs Water.");

        Debug.Log("Symmetry of weakness: PASS");
    }
}
