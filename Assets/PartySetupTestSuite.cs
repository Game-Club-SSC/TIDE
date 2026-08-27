using UnityEngine;
using NUnit.Framework;

public class PartySetupTestSuite
{
    private PartyData CreateTestParty()
    {
        PartyData party = ScriptableObject.CreateInstance<PartyData>();
        party.activeSlots = new HeroData[3];
        party.reserveSlots = new HeroData[2];

        party.activeSlots[0] = CreateTestHero("hero_1", "Hero1", CombatUnit.Element.Fire);
        party.activeSlots[1] = CreateTestHero("hero_2", "Hero2", CombatUnit.Element.Water);
        party.activeSlots[2] = CreateTestHero("hero_3", "Hero3", CombatUnit.Element.Earth);
        party.reserveSlots[0] = CreateTestHero("hero_4", "Hero4", CombatUnit.Element.Air);
        party.reserveSlots[1] = CreateTestHero("hero_5", "Hero5", CombatUnit.Element.Space);

        return party;
    }

    private HeroData CreateTestHero(string id, string name, CombatUnit.Element element)
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        hero.heroId = id;
        hero.displayName = name;
        hero.element = element;
        hero.baseMaxHP = 100;
        hero.baseAttack = 10;
        hero.baseDefense = 5;
        hero.baseSpeed = 10;
        return hero;
    }

    [Test]
    public void ToggleHeroActive()
    {
        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount(), "Should start with 3 active");
        Assert.AreEqual(2, party.GetReserveCount(), "Should start with 2 reserve");

        bool toggled = party.ToggleHeroActive("hero_4");
        Assert.IsTrue(toggled, "Should be able to move reserve hero to active");
        Assert.IsTrue(party.IsHeroActive("hero_4"), "hero_4 should now be active");
        Assert.IsTrue(party.IsHeroInReserve("hero_1"), "hero_1 should now be in reserve (moved to make room)");
    }

    [Test]
    public void MaxActiveEnforced()
    {
        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount());

        bool toggled = party.ToggleHeroActive("hero_4");
        Assert.IsTrue(toggled, "Should swap hero_4 into active (hero_1 moves to reserve)");

        party = CreateTestParty();
        party.ToggleHeroActive("hero_1");
        Assert.IsTrue(party.IsHeroInReserve("hero_1"), "hero_1 should now be in reserve");
        Assert.AreEqual(3, party.GetActiveCount(), "Should have 3 active after swapping one out when reserve is full");

        bool addedBack = party.ToggleHeroActive("hero_1");
        Assert.IsTrue(addedBack, "Should be able to add hero back to active");
        Assert.AreEqual(3, party.GetActiveCount(), "Should have 3 active again");
    }

    [Test]
    public void ValidateActiveParty()
    {
        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount(), "Full party should have 3 active");

        party.ToggleHeroActive("hero_1");
        Assert.AreEqual(3, party.GetActiveCount(), "After toggling one with full reserve, should swap and still have 3 active");
    }

    [Test]
    public void ReserveTrackedSeparately()
    {
        PartyData party = CreateTestParty();

        Assert.IsTrue(party.IsHeroActive("hero_1"));
        Assert.IsTrue(party.IsHeroActive("hero_2"));
        Assert.IsTrue(party.IsHeroActive("hero_3"));
        Assert.IsFalse(party.IsHeroActive("hero_4"));
        Assert.IsFalse(party.IsHeroActive("hero_5"));

        Assert.IsTrue(party.IsHeroInReserve("hero_4"));
        Assert.IsTrue(party.IsHeroInReserve("hero_5"));
        Assert.IsFalse(party.IsHeroInReserve("hero_1"));

        party.ToggleHeroActive("hero_4");
        Assert.IsTrue(party.IsHeroActive("hero_4"), "hero_4 should be active after toggle");
        Assert.IsFalse(party.IsHeroInReserve("hero_4"), "hero_4 should no longer be in reserve");
    }

    [Test]
    public void PartyStatePersistsAcrossToggles()
    {
        PartyData party = CreateTestParty();
        party.ToggleHeroActive("hero_4");

        HeroData[] active = party.activeSlots;
        Assert.AreEqual(3, active.Length, "Should still have 3 active slots");

        int nonNullCount = 0;
        for (int i = 0; i < active.Length; i++)
        {
            if (active[i] != null) nonNullCount++;
        }

        Assert.AreEqual(3, nonNullCount, "All 3 active slots should be filled");

        HeroData[] reserve = party.reserveSlots;
        int reserveCount = 0;
        for (int i = 0; i < reserve.Length; i++)
        {
            if (reserve[i] != null) reserveCount++;
        }

        Assert.AreEqual(2, reserveCount, "All 2 reserve slots should be filled");
    }

    [Test]
    public void SetActiveParty()
    {
        PartyData party = CreateTestParty();

        bool set = party.SetActiveParty(new string[] { "hero_4", "hero_5", "hero_1" });
        Assert.IsTrue(set, "Should be able to set active party");
        Assert.IsTrue(party.IsHeroActive("hero_4"), "hero_4 should be active");
        Assert.IsTrue(party.IsHeroActive("hero_5"), "hero_5 should be active");
        Assert.IsTrue(party.IsHeroActive("hero_1"), "hero_1 should be active");
        Assert.IsTrue(party.IsHeroInReserve("hero_2"), "hero_2 should be in reserve");
        Assert.IsTrue(party.IsHeroInReserve("hero_3"), "hero_3 should be in reserve");
        Assert.AreEqual(3, party.GetActiveCount());
        Assert.AreEqual(2, party.GetReserveCount());
    }

    [Test]
    public void MalformedSlotArraysNormalizeToThreeActiveAndTwoReserve()
    {
        PartyData party = ScriptableObject.CreateInstance<PartyData>();
        HeroData reserveHero = CreateTestHero("hero_reserve", "Reserve", CombatUnit.Element.Water);
        party.activeSlots = null;
        party.reserveSlots = new HeroData[4];
        party.reserveSlots[0] = reserveHero;

        Assert.AreEqual(0, party.GetActiveCount());
        Assert.AreEqual(1, party.GetReserveCount());
        Assert.IsNotNull(party.activeSlots, "Active slots should repair a null serialized array.");
        Assert.AreEqual(PartyData.ActiveSlotCount, party.activeSlots.Length,
            "PartyData must preserve the GDD's three active battle slots.");
        Assert.AreEqual(PartyData.ReserveSlotCount, party.reserveSlots.Length,
            "PartyData must preserve the two reserve slots for the five-hero roster.");
        Assert.AreSame(reserveHero, party.reserveSlots[0], "Normalization should preserve in-range heroes.");

        Object.DestroyImmediate(reserveHero);
        Object.DestroyImmediate(party);
    }

    [Test]
    public void ApplyingHeroClearsStaleTideBreaksAndCopiesCritStats()
    {
        GameObject unitObject = new GameObject("PartyApplyHero_Test");
        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        HeroData hero = CreateTestHero("hero_none", "No Element", CombatUnit.Element.None);
        hero.baseCritRate = 0.25f;
        hero.baseCritDamage = 1.8f;
        TideBreakData stale = ScriptableObject.CreateInstance<TideBreakData>();
        stale.abilityName = "Stale Break";
        stale.damageMultiplier = 2f;
        stale.unlockLevel = 1;
        unit.AddTideBreak(stale);

        try
        {
            PartyManager.ApplyHeroToUnitWithElement(unit, hero, CombatUnit.Element.None);

            Assert.AreEqual(0, unit.TideBreakAbilities.Count,
                "Reusing a unit for a hero without an element must clear the prior hero's Tide Breaks.");
            Assert.AreEqual(0.25f, unit.CritRate, 0.0001f, "Hero crit rate should be copied even without a progression manager.");
            Assert.AreEqual(1.8f, unit.CritDamage, 0.0001f, "Hero crit damage should be copied even without a progression manager.");
        }
        finally
        {
            Object.DestroyImmediate(stale);
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void ApplyingHeroIncludesPerHeroTideBreakProgression()
    {
        GameObject unitObject = new GameObject("PartyApplyTideBreaks_Test");
        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        HeroData hero = CreateTestHero("hero_fire", "Fire Hero", CombatUnit.Element.Fire);

        try
        {
            PartyManager.ApplyHeroToUnitWithElement(unit, hero, CombatUnit.Element.Fire);

            Assert.GreaterOrEqual(unit.TideBreakAbilities.Count, 2,
                "The fire hero should receive the resource Tide Break plus their level-1 per-hero ability.");
        }
        finally
        {
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(unitObject);
        }
    }
}
