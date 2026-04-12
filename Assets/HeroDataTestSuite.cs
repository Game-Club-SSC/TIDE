using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class HeroDataTestSuite
{
    private HeroDatabase heroDatabase;
    private PartyData partyData;

    [SetUp]
    public void SetUp()
    {
        heroDatabase = Resources.Load<HeroDatabase>("HeroDatabase");
        partyData = Resources.Load<PartyData>("PartyData/DefaultParty");
    }

    private void AssertCoreDataLoaded()
    {
        Assert.IsNotNull(heroDatabase, "HeroDatabase must load from Resources/HeroDatabase.");
        Assert.IsNotNull(partyData, "PartyData must load from Resources/PartyData/DefaultParty.");
    }

    [Test]
    public void AllHeroesLoaded()
    {
        AssertCoreDataLoaded();
        Assert.IsNotNull(heroDatabase, "HeroDatabase must be loaded.");
        Assert.AreEqual(5, heroDatabase.allHeroes.Length, "HeroDatabase must contain exactly 5 heroes.");

        for (int i = 0; i < heroDatabase.allHeroes.Length; i++)
        {
            HeroData hero = heroDatabase.allHeroes[i];
            Assert.IsNotNull(hero, $"Hero slot {i} must not be null.");
            Assert.IsFalse(string.IsNullOrEmpty(hero.heroId), $"Hero slot {i} must have a heroId.");
            Assert.IsFalse(string.IsNullOrEmpty(hero.displayName), $"Hero slot {i} must have a displayName.");
        }
    }

    [Test]
    public void HeroStatRanges()
    {
        AssertCoreDataLoaded();
        for (int i = 0; i < heroDatabase.allHeroes.Length; i++)
        {
            HeroData hero = heroDatabase.allHeroes[i];
            Assert.Greater(hero.baseMaxHP, 0, $"{hero.displayName}: baseMaxHP must be > 0.");
            Assert.GreaterOrEqual(hero.baseMaxMP, 0, $"{hero.displayName}: baseMaxMP must be >= 0.");
            Assert.GreaterOrEqual(hero.baseAttack, 0, $"{hero.displayName}: baseAttack must be >= 0.");
            Assert.GreaterOrEqual(hero.baseDefense, 0, $"{hero.displayName}: baseDefense must be >= 0.");
            Assert.GreaterOrEqual(hero.baseSpeed, 0, $"{hero.displayName}: baseSpeed must be >= 0.");
        }
    }

    [Test]
    public void ElementCoverage()
    {
        AssertCoreDataLoaded();
        HashSet<CombatUnit.Element> elements = new HashSet<CombatUnit.Element>();
        for (int i = 0; i < heroDatabase.allHeroes.Length; i++)
        {
            HeroData hero = heroDatabase.allHeroes[i];
            Assert.AreNotEqual(CombatUnit.Element.None, hero.element, $"{hero.displayName}: element must not be None.");
            elements.Add(hero.element);
        }

        Assert.AreEqual(5, elements.Count, "All 5 elements (Fire, Water, Earth, Air, Space) must be represented.");
        Assert.IsTrue(elements.Contains(CombatUnit.Element.Fire), "Missing Fire element hero.");
        Assert.IsTrue(elements.Contains(CombatUnit.Element.Water), "Missing Water element hero.");
        Assert.IsTrue(elements.Contains(CombatUnit.Element.Earth), "Missing Earth element hero.");
        Assert.IsTrue(elements.Contains(CombatUnit.Element.Air), "Missing Air element hero.");
        Assert.IsTrue(elements.Contains(CombatUnit.Element.Space), "Missing Space element hero.");
    }

    [Test]
    public void MainCharacterFlag()
    {
        AssertCoreDataLoaded();
        int mainCharCount = 0;
        for (int i = 0; i < heroDatabase.allHeroes.Length; i++)
        {
            if (heroDatabase.allHeroes[i].isMainCharacter)
            {
                mainCharCount++;
            }
        }

        Assert.LessOrEqual(mainCharCount, 1, "At most 1 hero should be marked as main character.");
    }

    [Test]
    public void StarterSkillsNotNull()
    {
        AssertCoreDataLoaded();
        for (int i = 0; i < heroDatabase.allHeroes.Length; i++)
        {
            HeroData hero = heroDatabase.allHeroes[i];
            Assert.IsNotNull(hero.starterSkills, $"{hero.displayName}: starterSkills array must not be null.");
            Assert.Greater(hero.starterSkills.Length, 0, $"{hero.displayName}: must have at least 1 starter skill.");

            for (int s = 0; s < hero.starterSkills.Length; s++)
            {
                SkillData skill = hero.starterSkills[s];
                Assert.IsNotNull(skill, $"{hero.displayName}: starter skill [{s}] must not be null.");
                Assert.IsFalse(string.IsNullOrEmpty(skill.skillName), $"{hero.displayName}: skill [{s}] must have a name.");
                Assert.GreaterOrEqual(skill.mpCost, 0, $"{hero.displayName}: skill [{s}] mpCost must be >= 0.");
                Assert.Greater(skill.damageMultiplier, 0f, $"{hero.displayName}: skill [{s}] damageMultiplier must be > 0.");
            }
        }
    }

    [Test]
    public void PartyDataIntegrity()
    {
        AssertCoreDataLoaded();
        Assert.IsNotNull(partyData, "PartyData must be assigned.");
        Assert.AreEqual(3, partyData.activeSlots.Length, "Active party must have 3 slots.");
        Assert.AreEqual(2, partyData.reserveSlots.Length, "Reserve party must have 2 slots.");
        Assert.AreEqual(3, partyData.GetActiveCount(), "All 3 active slots must be filled.");
        Assert.AreEqual(2, partyData.GetReserveCount(), "All 2 reserve slots must be filled.");

        HeroData[] allHeroes = partyData.GetAllHeroes();
        Assert.AreEqual(5, allHeroes.Length, "Party must contain all 5 heroes.");
    }

    [Test]
    public void PartyDataNoDuplicates()
    {
        AssertCoreDataLoaded();
        HashSet<string> heroIds = new HashSet<string>();
        HeroData[] allHeroes = partyData.GetAllHeroes();

        for (int i = 0; i < allHeroes.Length; i++)
        {
            Assert.IsTrue(heroIds.Add(allHeroes[i].heroId),
                $"Duplicate heroId found: {allHeroes[i].heroId}");
        }
    }

    [Test]
    public void ApplyHeroToCombatUnit()
    {
        AssertCoreDataLoaded();
        GameObject testObject = new GameObject("TestHeroUnit");
        CombatUnit unit = testObject.AddComponent<CombatUnit>();
        HeroData hero = heroDatabase.allHeroes[0];

        PartyManager.ApplyHeroToUnitStatic(unit, hero);

        Assert.AreEqual(hero.displayName, unit.UnitName, "Unit name should match hero displayName.");
        Assert.AreEqual(hero.element, unit.ElementType, "Unit element should match hero element.");
        Assert.AreEqual(hero.baseMaxHP, unit.MaxHP, "Unit MaxHP should match hero baseMaxHP.");
        Assert.AreEqual(hero.baseMaxHP, unit.HP, "Unit HP should equal hero baseMaxHP on spawn.");
        Assert.AreEqual(hero.baseAttack, unit.Attack, "Unit Attack should match hero baseAttack.");
        Assert.AreEqual(hero.baseDefense, unit.Defense, "Unit Defense should match hero baseDefense.");
        Assert.AreEqual(hero.baseSpeed, unit.Speed, "Unit Speed should match hero baseSpeed.");
        Assert.AreEqual(hero.starterSkills.Length, unit.Skills.Count, "Unit skills count should match hero starterSkills count.");

        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void MainCharacterElementOverride()
    {
        AssertCoreDataLoaded();
        HeroData mainChar = partyData.GetMainCharacter();
        if (mainChar == null)
        {
            Assert.Ignore("No main-character hero configured in PartyData; skipping override test.");
            return;
        }

        Assert.IsTrue(mainChar.isMainCharacter, "Hero must be marked as main character.");

        GameObject testObject = new GameObject("TestMainCharUnit");
        CombatUnit unit = testObject.AddComponent<CombatUnit>();

        // Without element chosen: should use default element
        PartyManager.ApplyHeroToUnitStatic(unit, mainChar);
        Assert.AreEqual(mainChar.element, unit.ElementType, "Without chosen element, should use default.");

        // With element chosen: should use chosen element
        CombatUnit.Element chosenElement = CombatUnit.Element.Space;
        if (mainChar.element == CombatUnit.Element.Space)
        {
            chosenElement = CombatUnit.Element.Fire;
        }

        PartyManager.ApplyHeroToUnitWithElement(unit, mainChar, chosenElement);
        Assert.AreEqual(chosenElement, unit.ElementType, "With chosen element, should override default.");

        Object.DestroyImmediate(testObject);
    }
}
