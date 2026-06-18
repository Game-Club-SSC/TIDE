using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GearSetFactoryTest : MonoBehaviour
{
    [ContextMenu("Run Gear Set Factory Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Gear Set Factory Tests ===");

        TestStarterGearCoversAllElements();
        TestElementMatchingLogic();
        TestAllStarterSetsAreValid();
        TestGetGearSetForElementPicksLowestTier();
        TestBuildAppliesClamps();
        TestCreateDefaultForElementIsUniversalWhenNone();

        Debug.Log("=== All Gear Set Factory Tests Passed ===");
    }

    private void TestStarterGearCoversAllElements()
    {
        List<CombatUnit.Element> expected = new List<CombatUnit.Element>
        {
            CombatUnit.Element.Earth,
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };
        IReadOnlyList<GearSetData> starters = GearSetFactory.CreateStarterGearSets();
        HashSet<CombatUnit.Element> found = new HashSet<CombatUnit.Element>();
        for (int i = 0; i < starters.Count; i++)
        {
            if (starters[i] != null)
            {
                found.Add(starters[i].element);
            }
        }
        foreach (CombatUnit.Element e in expected)
        {
            Assert.IsTrue(found.Contains(e), $"Starter sets should cover element {e}.");
        }
    }

    private void TestElementMatchingLogic()
    {
        GearSetData fireSet = GearSetFactory.Build("fire_test", "Fire Test", CombatUnit.Element.Fire, 0, 0.1f, 0.1f, 0.1f, 0f, 0f, 0f, "test");
        Assert.IsTrue(fireSet.MatchesElement(CombatUnit.Element.Fire), "Fire set should match Fire hero.");
        Assert.IsFalse(fireSet.MatchesElement(CombatUnit.Element.Water), "Fire set should not match Water hero.");

        GearSetData universal = GearSetFactory.Build("univ", "Universal", CombatUnit.Element.None, 0, 0.05f, 0.05f, 0.05f, 0f, 0f, 0f, "test");
        Assert.IsTrue(universal.MatchesElement(CombatUnit.Element.Fire), "Universal set should match Fire hero.");
        Assert.IsTrue(universal.MatchesElement(CombatUnit.Element.Space), "Universal set should match Space hero.");
    }

    private void TestAllStarterSetsAreValid()
    {
        IReadOnlyList<GearSetData> starters = GearSetFactory.CreateStarterGearSets();
        for (int i = 0; i < starters.Count; i++)
        {
            Assert.IsNotNull(starters[i], $"Starter at {i} should not be null.");
            Assert.IsTrue(starters[i].IsValid(), $"Starter {starters[i].setId} should be valid.");
        }
    }

    private void TestGetGearSetForElementPicksLowestTier()
    {
        HeroProgressionManager manager = HeroProgressionManager.Instance;
        Assert.IsNotNull(manager, "HeroProgressionManager.Instance must be available.");
        manager.EnsureStarterGearRegistry();
        GearSetData waterSet = manager.GetGearSetForElement(CombatUnit.Element.Water);
        Assert.IsNotNull(waterSet, "Should return a gear set for Water element.");
        Assert.AreEqual(CombatUnit.Element.Water, waterSet.element, "Returned set should be a Water set.");
    }

    private void TestBuildAppliesClamps()
    {
        GearSetData gear = GearSetFactory.Build("clamp_test", "Clamp", CombatUnit.Element.Fire, 0,
            5f, -1f, 99f, -2f, 99f, 99f, "test");
        Assert.AreEqual(1f, gear.attackBonusPercent, "ATK percent should be clamped to 1.");
        Assert.AreEqual(0f, gear.defenseBonusPercent, "DEF percent should be clamped to 0.");
        Assert.AreEqual(1f, gear.hpBonusPercent, "HP percent should be clamped to 1.");
        Assert.AreEqual(0f, gear.setBonusAttackPercent, "Set ATK should be clamped to 0.");
    }

    private void TestCreateDefaultForElementIsUniversalWhenNone()
    {
        GearSetData defaultNone = GearSetFactory.CreateDefaultForElement(CombatUnit.Element.None);
        Assert.IsNotNull(defaultNone, "Default for None should still produce a set.");
        Assert.AreEqual(CombatUnit.Element.None, defaultNone.element, "Default for None should be universal.");
    }
}
