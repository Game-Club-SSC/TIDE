using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Edit Mode test for CombatUnit functionality.
/// </summary>
public class CombatUnitTestSuite
{
    private static readonly FieldInfo TideBreakAbilitiesField = typeof(CombatUnit).GetField("tideBreakAbilities", BindingFlags.Instance | BindingFlags.NonPublic);

    private GameObject testObject;
    private CombatUnit testUnit;
    private List<TideBreakData> createdTideBreaks;
    
    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestCombatUnit");
        testUnit = testObject.AddComponent<CombatUnit>();
        createdTideBreaks = new List<TideBreakData>();
    }

    [TearDown]
    public void TearDown()
    {
        if (createdTideBreaks != null)
        {
            for (int i = 0; i < createdTideBreaks.Count; i++)
            {
                if (createdTideBreaks[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdTideBreaks[i]);
                }
            }
        }

        if (testObject != null)
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }
    
    [Test]
    public void InitialState()
    {
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
        Assert.IsNotNull(testUnit.Skills, "Default skills collection should not be null.");
        Assert.AreEqual(0, testUnit.Skills.Count, "Default skills collection should be empty.");
    }

    [Test]
    public void SetSkillsNullStoresEmptyReadOnlyCollection()
    {
        testUnit.SetSkills(null);

        Assert.IsNotNull(testUnit.Skills, "Skills collection should not be null after SetSkills(null).");
        Assert.AreEqual(0, testUnit.Skills.Count, "Skills collection should be empty after SetSkills(null).");

        IList<SkillData> exposedSkills = testUnit.Skills as IList<SkillData>;
        Assert.IsNotNull(exposedSkills, "Skills collection should expose a list interface.");
        SkillData attemptedSkill = ScriptableObject.CreateInstance<SkillData>();
        Assert.Throws<NotSupportedException>(() => exposedSkills.Add(attemptedSkill),
            "Skills collection should be read-only.");
        UnityEngine.Object.DestroyImmediate(attemptedSkill);
    }

    [Test]
    public void SetSkillsClonesSourceArray()
    {
        SkillData originalSkill = ScriptableObject.CreateInstance<SkillData>();
        SkillData replacementSkill = ScriptableObject.CreateInstance<SkillData>();
        SkillData[] sourceSkills = new SkillData[] { originalSkill };

        testUnit.SetSkills(sourceSkills);
        sourceSkills[0] = replacementSkill;

        Assert.AreEqual(1, testUnit.Skills.Count, "Skills collection should preserve the original source length.");
        Assert.AreSame(originalSkill, testUnit.Skills[0], "Skills collection should not be affected by source array mutation.");

        UnityEngine.Object.DestroyImmediate(originalSkill);
        UnityEngine.Object.DestroyImmediate(replacementSkill);
    }
    
    [Test]
    public void TakingDamage()
    {
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
    }
    
    [Test]
    public void Healing()
    {
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
    }
    
    [Test]
    public void MpManagement()
    {
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
    }
    
    [Test]
    public void DeathState()
    {
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
    }
    
    [Test]
    public void ReviveLogic()
    {
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
    }

    [Test]
    public void AddTideBreakIgnoresNullInput()
    {
        testUnit.AddTideBreak(null);

        Assert.IsNotNull(testUnit.TideBreakAbilities, "TideBreakAbilities should remain non-null after null add.");
        Assert.AreEqual(0, testUnit.TideBreakAbilities.Count, "Null TideBreak should not be added.");
    }

    [Test]
    public void SetTideBreaksNullResetsToEmptyList()
    {
        testUnit.SetTideBreaks(null);

        Assert.IsNotNull(testUnit.TideBreakAbilities, "TideBreakAbilities should not be null after reset.");
        Assert.AreEqual(0, testUnit.TideBreakAbilities.Count, "Null list should reset TideBreak abilities to empty.");
    }

    [Test]
    public void SetTideBreaksCreatesDefensiveCopy()
    {
        TideBreakData first = CreateTideBreak("First");
        TideBreakData second = CreateTideBreak("Second");
        List<TideBreakData> source = new List<TideBreakData> { first };

        testUnit.SetTideBreaks(source);
        source.Add(second);

        Assert.AreEqual(1, testUnit.TideBreakAbilities.Count, "CombatUnit should keep its own TideBreak list copy.");
        Assert.AreSame(first, testUnit.TideBreakAbilities[0], "Copied list should preserve existing entries.");
    }

    [Test]
    public void SetTideBreaksFiltersNullEntries()
    {
        TideBreakData first = CreateTideBreak("First");
        TideBreakData second = CreateTideBreak("Second");

        testUnit.SetTideBreaks(new List<TideBreakData> { first, null, second });

        Assert.AreEqual(2, testUnit.TideBreakAbilities.Count, "Null TideBreak entries should be filtered out.");
        Assert.AreSame(first, testUnit.TideBreakAbilities[0], "First non-null TideBreak should be preserved.");
        Assert.AreSame(second, testUnit.TideBreakAbilities[1], "Second non-null TideBreak should be preserved.");
    }

    [Test]
    public void TideBreakAbilitiesInitializesWhenBackingFieldIsNull()
    {
        Assert.IsNotNull(TideBreakAbilitiesField, "Private field 'tideBreakAbilities' should exist.");
        TideBreakAbilitiesField.SetValue(testUnit, null);

        IReadOnlyList<TideBreakData> abilities = testUnit.TideBreakAbilities;

        Assert.IsNotNull(abilities, "TideBreakAbilities should initialize a null backing list.");
        Assert.AreEqual(0, abilities.Count, "Initialized TideBreakAbilities should be empty.");
        Assert.IsNotNull(TideBreakAbilitiesField.GetValue(testUnit), "Backing field should be repaired when accessed.");
    }

    private TideBreakData CreateTideBreak(string abilityName)
    {
        TideBreakData tideBreak = ScriptableObject.CreateInstance<TideBreakData>();
        tideBreak.abilityName = abilityName;
        createdTideBreaks.Add(tideBreak);
        return tideBreak;
    }
}
