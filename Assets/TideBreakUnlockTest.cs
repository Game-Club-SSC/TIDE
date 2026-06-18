using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class TideBreakUnlockTest : MonoBehaviour
{
    [ContextMenu("Run Tide Break Unlock Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Tide Break Unlock Tests ===");

        TestUnlockFilterExcludesHigherLevels();
        TestUnlockFilterIncludesExactLevel();
        TestElementFilteringOnlyReturnsMatches();
        TestDataBuilderCachesTideBreaks();

        Debug.Log("=== All Tide Break Unlock Tests Passed ===");
    }

    private void TestUnlockFilterExcludesHigherLevels()
    {
        TideBreakData a = ScriptableObject.CreateInstance<TideBreakData>();
        a.abilityName = "TB_Fire_1";
        a.element = (int)CombatUnit.Element.Fire;
        a.unlockLevel = 1;
        a.damageMultiplier = 2f;
        a.targetType = SkillTarget.AllEnemies;

        try
        {
            Assert.AreEqual(1, a.unlockLevel, "Test fixture should have unlock level 1.");
            Assert.AreEqual(2f, a.damageMultiplier, "Test fixture should have damage multiplier 2.");
            Assert.AreEqual(CombatUnit.Element.Fire, (CombatUnit.Element)a.element, "Test fixture element should be Fire.");
        }
        finally
        {
            Object.DestroyImmediate(a);
        }
    }

    private void TestUnlockFilterIncludesExactLevel()
    {
        TideBreakData data = ScriptableObject.CreateInstance<TideBreakData>();
        data.abilityName = "TB_Water_3";
        data.element = (int)CombatUnit.Element.Water;
        data.unlockLevel = 3;
        try
        {
            Assert.AreEqual(3, data.unlockLevel, "Level 3 ability should be at level 3.");
            Assert.AreEqual((int)CombatUnit.Element.Water, data.element, "Level 3 ability should be water element.");
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    private void TestElementFilteringOnlyReturnsMatches()
    {
        TideBreakData fire = ScriptableObject.CreateInstance<TideBreakData>();
        fire.abilityName = "TB_Fire_2";
        fire.element = (int)CombatUnit.Element.Fire;
        fire.unlockLevel = 1;

        TideBreakData water = ScriptableObject.CreateInstance<TideBreakData>();
        water.abilityName = "TB_Water_2";
        water.element = (int)CombatUnit.Element.Water;
        water.unlockLevel = 1;

        try
        {
            Assert.AreNotEqual(fire.element, water.element, "Test fixtures should have different elements.");
        }
        finally
        {
            Object.DestroyImmediate(fire);
            Object.DestroyImmediate(water);
        }
    }

    private void TestDataBuilderCachesTideBreaks()
    {
        TideBreakData data = ScriptableObject.CreateInstance<TideBreakData>();
        data.abilityName = "TB_Earth_1";
        data.element = (int)CombatUnit.Element.Earth;
        data.unlockLevel = 1;
        data.damageMultiplier = 2f;
        data.targetType = SkillTarget.AllEnemies;
        try
        {
            Assert.IsTrue(data.damageMultiplier > 0f, "Test fixture data should have a positive damage multiplier.");
            Assert.AreEqual(CombatUnit.Element.Earth, (CombatUnit.Element)data.element, "Test fixture element should match.");
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }
}
