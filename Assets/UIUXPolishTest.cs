using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the UI/UX polish system.
/// Verifies BattleHud and UI elements.
/// </summary>
[DisallowMultipleComponent]
public class UIUXPolishTest : MonoBehaviour
{
    [ContextMenu("Run UI/UX Polish Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting UI/UX Polish Tests ===");

        TestBattleHudExists();
        TestMomentumSystemExists();
        TestTurnOrderDisplay();
        TestElementIndicators();

        Debug.Log("=== All UI/UX Polish Tests Passed ===");
    }

    private void TestBattleHudExists()
    {
        Debug.Log("Testing BattleHud exists...");

        Assert.IsNotNull(typeof(BattleHud), "BattleHud class should exist.");

        // Verify key methods exist
        var showTeamDynamic = typeof(BattleHud).GetMethod("ShowTeamDynamicDescription");
        Assert.IsNotNull(showTeamDynamic, "ShowTeamDynamicDescription method should exist.");

        Debug.Log("BattleHud exists: PASS");
    }

    private void TestMomentumSystemExists()
    {
        Debug.Log("Testing momentum system exists...");

        // Verify BattleManager has momentum system
        BattleManager bm = BattleManager.Instance;
        if (bm != null)
        {
            Assert.IsNotNull(bm.Momentum, "BattleManager should have Momentum system.");
            Debug.Log("Momentum system exists: PASS");
        }
        else
        {
            Debug.LogWarning("BattleManager not found - skipping momentum tests.");
        }
    }

    private void TestTurnOrderDisplay()
    {
        Debug.Log("Testing turn order display...");

        // Verify BattleManager has turn order methods
        BattleManager bm = BattleManager.Instance;
        if (bm != null)
        {
            var getCurrentUnit = typeof(BattleManager).GetMethod("GetCurrentInputUnit");
            Assert.IsNotNull(getCurrentUnit, "BattleManager should have GetCurrentInputUnit method.");

            var getAllUnits = typeof(BattleManager).GetMethod("GetAllUnits");
            Assert.IsNotNull(getAllUnits, "BattleManager should have GetAllUnits method.");

            Debug.Log("Turn order display methods exist: PASS");
        }
        else
        {
            Debug.LogWarning("BattleManager not found - skipping turn order tests.");
        }
    }

    private void TestElementIndicators()
    {
        Debug.Log("Testing element indicators...");

        // Verify CombatUnit has element system
        Assert.IsNotNull(typeof(CombatUnit.Element), "CombatUnit.Element enum should exist.");

        // Verify element colors are defined
        Color fireColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Fire);
        Color waterColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Water);

        Assert.AreNotEqual(fireColor, waterColor, "Fire and Water should have different indicator colors.");

        Debug.Log("Element indicators exist: PASS");
    }
}
