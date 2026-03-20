using UnityEngine;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Verification test for CombatUnit functionality.
/// This test can be run in Edit Mode to verify the CombatUnit works correctly.
/// </summary>
public class CombatUnitVerificationTest : MonoBehaviour
{
    private CombatUnit testUnit;
    
    [ContextMenu("Run Combat Unit Tests")]
    public void RunTests()
    {
        // Create a test GameObject with CombatUnit component
        GameObject testObject = new GameObject("TestCombatUnit");
        try
        {
            testUnit = testObject.AddComponent<CombatUnit>();

            Debug.Log("=== Starting Combat Unit Verification Tests ===");

            TestInitialState();
            TestTakingDamage();
            TestHealing();
            TestMpManagement();
            TestDeathState();
            TestReviveLogic();

            Debug.Log("=== Combat Unit Verification Tests Complete ===");
        }
        finally
        {
            DestroyImmediate(testObject);
            testUnit = null;
        }
    }
    
    private void TestInitialState()
    {
        Debug.Log("Testing initial state...");
        
        // Test default values
        Assert.AreEqual(100, testUnit.MaxHP, "Default max HP should be 100");
        Assert.AreEqual(100, testUnit.HP, "Default HP should be 100");
        Assert.AreEqual(50, testUnit.MaxMP, "Default max MP should be 50");
        Assert.AreEqual(50, testUnit.MP, "Default MP should be 50");
        Assert.AreEqual(10, testUnit.Attack, "Default attack should be 10");
        Assert.AreEqual(5, testUnit.Defense, "Default defense should be 5");
        Assert.AreEqual(10, testUnit.Speed, "Default speed should be 10");
        Assert.AreEqual(CombatUnit.Element.None, testUnit.ElementType, "Default element should be None");
        Assert.AreEqual("Combat Unit", testUnit.UnitName, "Default unit name should be 'Combat Unit'");
        Assert.IsTrue(testUnit.IsAlive, "Unit should be alive by default");
        
        Debug.Log("✓ Initial state test passed");
    }
    
    private void TestTakingDamage()
    {
        Debug.Log("Testing damage taking...");
        
        // Reset to known state
        testUnit.DebugHP = 100;
        testUnit.DebugIsAlive = true;
        
        // Test basic damage
        testUnit.TakeDamage(15);
        Assert.AreEqual(90, testUnit.HP, "HP should be 90 after taking 15 damage");
        
        // Test damage with defense reduction
        testUnit.TakeDamage(20); // 20 damage - 5 defense = 15 actual damage
        Assert.AreEqual(75, testUnit.HP, "HP should be 75 after taking 20 damage with 5 defense");
        
        // Test minimum damage (should always do at least 1)
        testUnit.DebugDefense = 100; // Very high defense
        testUnit.TakeDamage(5); // Should still do 1 damage due to Mathf.Max(1, damage - defense)
        Assert.AreEqual(74, testUnit.HP, "HP should be 74 after taking 5 damage with 100 defense (minimum 1)");
        
        // Test lethal damage
        testUnit.TakeDamage(1000);
        Assert.AreEqual(0, testUnit.HP, "HP should be 0 after lethal damage");
        Assert.IsFalse(testUnit.IsAlive, "Unit should be dead after lethal damage");
        
        Debug.Log("✓ Damage taking test passed");
    }
    
    private void TestHealing()
    {
        Debug.Log("Testing healing...");
        
        // Set up damaged unit
        testUnit.DebugHP = 50;
        testUnit.DebugMaxHP = 100;
        testUnit.DebugIsAlive = true;
        
        // Test basic healing
        testUnit.Heal(30);
        Assert.AreEqual(80, testUnit.HP, "HP should be 80 after healing 30 from 50");
        
        // Test healing to max
        testUnit.Heal(50); // Try to heal more than needed
        Assert.AreEqual(100, testUnit.HP, "HP should be 100 (max) after over-healing");
        
        // Test healing when dead (should not work)
        testUnit.DebugIsAlive = false;
        testUnit.DebugHP = 0;
        testUnit.Heal(50);
        Assert.AreEqual(0, testUnit.HP, "HP should remain 0 when healing dead unit");
        
        Debug.Log("✓ Healing test passed");
    }
    
    private void TestMpManagement()
    {
        Debug.Log("Testing MP management...");
        
        // Reset to known state
        testUnit.DebugMP = 50;
        testUnit.DebugMaxMP = 50;
        testUnit.DebugIsAlive = true;
        
        // Test spending MP
        bool spent = testUnit.SpendMp(20);
        Assert.IsTrue(spent, "Should be able to spend 20 MP when having 50");
        Assert.AreEqual(30, testUnit.MP, "MP should be 30 after spending 20");
        
        // Test insufficient MP
        spent = testUnit.SpendMp(40);
        Assert.IsFalse(spent, "Should NOT be able to spend 40 MP when only having 30");
        Assert.AreEqual(30, testUnit.MP, "MP should remain 30 when unable to spend");
        
        // Test restoring MP
        testUnit.RestoreMp(25);
        Assert.AreEqual(50, testUnit.MP, "MP should be 50 after restoring 25");
        
        // Test over-restoring (should cap at max)
        testUnit.RestoreMp(100);
        Assert.AreEqual(50, testUnit.MP, "MP should remain 50 (max) after over-restoring");
        
        // Test MP actions when dead (should not work)
        testUnit.DebugIsAlive = false;
        spent = testUnit.SpendMp(10);
        Assert.IsFalse(spent, "Should not be able to spend MP when dead");
        Assert.AreEqual(50, testUnit.MP, "MP should remain unchanged when trying to spend while dead");
        
        testUnit.RestoreMp(10);
        Assert.AreEqual(50, testUnit.MP, "MP should remain unchanged when trying to restore while dead");
        
        Debug.Log("✓ MP management test passed");
    }
    
    private void TestDeathState()
    {
        Debug.Log("Testing death state...");
        
        // Test explicit death check
        testUnit.DebugHP = 0;
        testUnit.DebugIsAlive = true; // Manually set to alive to test CheckDeathState
        testUnit.CheckDeathState();
        Assert.IsFalse(testUnit.IsAlive, "Unit should be dead after CheckDeathState with 0 HP");
        
        // Test death check with negative HP
        testUnit.DebugHP = -10;
        testUnit.DebugIsAlive = true; // Manually set to alive
        testUnit.CheckDeathState();
        Assert.IsFalse(testUnit.IsAlive, "Unit should be dead after CheckDeathState with negative HP");
        Assert.AreEqual(0, testUnit.HP, "HP should be clamped to 0 after death");
        
        // Test that alive units with HP > 0 stay alive
        testUnit.DebugHP = 50;
        testUnit.DebugIsAlive = true;
        testUnit.CheckDeathState();
        Assert.IsTrue(testUnit.IsAlive, "Unit with 50 HP should remain alive");
        
        Debug.Log("✓ Death state test passed");
    }
    
    private void TestReviveLogic()
    {
        Debug.Log("Testing revive logic...");
        
        // Dead unit with 0 HP
        testUnit.DebugHP = 0;
        testUnit.DebugIsAlive = false;
        testUnit.CheckDeathState(); // Should remain dead
        Assert.IsFalse(testUnit.IsAlive, "Dead unit with 0 HP should remain dead after CheckDeathState");
        
        // Now manually set HP to positive value (simulating revive)
        testUnit.DebugHP = 25;
        testUnit.CheckDeathState(); // Should revive
        Assert.IsTrue(testUnit.IsAlive, "Unit should revive when HP > 0 and CheckDeathState is called");
        Assert.AreEqual(25, testUnit.HP, "HP should remain 25 after revive");
        
        Debug.Log("✓ Revive logic test passed");
    }
}
