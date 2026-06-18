using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroTideBreakFactoryTest : MonoBehaviour
{
    [ContextMenu("Run Hero Tide Break Factory Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Hero Tide Break Factory Tests ===");

        TestGetTideBreaksForHeroFire();
        TestGetTideBreaksForHeroWater();
        TestGetTideBreaksForHeroSpace();
        TestGetTideBreaksForHeroEarth();
        TestGetTideBreaksForHeroAir();
        TestHeroLevelFiltersOutUnlockedBreaks();
        TestAllFiveHeroesHaveAtLeastOneTideBreak();
        TestTargetTypeFiltering();

        Debug.Log("=== All Hero Tide Break Factory Tests Passed ===");
    }

    private void TestGetTideBreaksForHeroFire()
    {
        IReadOnlyList<TideBreakData> fireBreaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_fire", CombatUnit.Element.Fire, 5);
        Assert.GreaterOrEqual(fireBreaks.Count, 2, "Fire hero should have at least 2 tide breaks at level 5.");
        for (int i = 0; i < fireBreaks.Count; i++)
        {
            Assert.IsTrue(fireBreaks[i].abilityName.Contains("Inferno") || fireBreaks[i].abilityName.Contains("Pyre"),
                $"Fire break name '{fireBreaks[i].abilityName}' should contain fire-themed word.");
        }
    }

    private void TestGetTideBreaksForHeroWater()
    {
        IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_water", CombatUnit.Element.Water, 5);
        Assert.GreaterOrEqual(breaks.Count, 2, "Water hero should have at least 2 tide breaks.");
    }

    private void TestGetTideBreaksForHeroSpace()
    {
        IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_space", CombatUnit.Element.Space, 5);
        Assert.GreaterOrEqual(breaks.Count, 2, "Space hero should have at least 2 tide breaks.");
    }

    private void TestGetTideBreaksForHeroEarth()
    {
        IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_earth", CombatUnit.Element.Earth, 5);
        Assert.GreaterOrEqual(breaks.Count, 2, "Earth hero should have at least 2 tide breaks.");
    }

    private void TestGetTideBreaksForHeroAir()
    {
        IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_air", CombatUnit.Element.Air, 5);
        Assert.GreaterOrEqual(breaks.Count, 2, "Air hero should have at least 2 tide breaks.");
    }

    private void TestHeroLevelFiltersOutUnlockedBreaks()
    {
        IReadOnlyList<TideBreakData> atL1 = HeroTideBreakFactory.GetTideBreaksForHero("hero_fire", CombatUnit.Element.Fire, 1);
        IReadOnlyList<TideBreakData> atL5 = HeroTideBreakFactory.GetTideBreaksForHero("hero_fire", CombatUnit.Element.Fire, 5);
        Assert.GreaterOrEqual(atL5.Count, atL1.Count, "Higher level should unlock at least as many breaks.");
    }

    private void TestAllFiveHeroesHaveAtLeastOneTideBreak()
    {
        string[] heroIds = { "hero_fire", "hero_water", "hero_earth", "hero_air", "hero_space" };
        CombatUnit.Element[] elements = { CombatUnit.Element.Fire, CombatUnit.Element.Water, CombatUnit.Element.Earth, CombatUnit.Element.Air, CombatUnit.Element.Space };
        for (int i = 0; i < heroIds.Length; i++)
        {
            IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero(heroIds[i], elements[i], 5);
            Assert.GreaterOrEqual(breaks.Count, 1, $"Hero {heroIds[i]} should have at least one tide break.");
        }
    }

    private void TestTargetTypeFiltering()
    {
        IReadOnlyList<TideBreakData> breaks = HeroTideBreakFactory.GetTideBreaksForHero("hero_fire", CombatUnit.Element.Fire, 5);
        bool hasAoe = false;
        bool hasSingle = false;
        for (int i = 0; i < breaks.Count; i++)
        {
            if (breaks[i].targetType == SkillTarget.AllEnemies) hasAoe = true;
            if (breaks[i].targetType == SkillTarget.SingleEnemy) hasSingle = true;
        }
        Assert.IsTrue(hasAoe || hasSingle, "At least one supported target type should be present.");
    }
}
