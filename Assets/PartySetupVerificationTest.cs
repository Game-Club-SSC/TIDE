using UnityEngine;
using NUnit.Framework;

public class PartySetupVerificationTest : MonoBehaviour
{
    [ContextMenu("Run Party Setup Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Party Setup Verification Tests ===");

        TestToggleHeroActive();
        TestMaxActiveEnforced();
        TestValidateActiveParty();
        TestReserveTrackedSeparately();
        TestPartyStatePersistsAcrossToggles();
        TestSetActiveParty();

        Debug.Log("=== Party Setup Verification Tests Complete ===");
    }

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

    private void TestToggleHeroActive()
    {
        Debug.Log("Testing toggle hero active/reserve...");

        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount(), "Should start with 3 active");
        Assert.AreEqual(2, party.GetReserveCount(), "Should start with 2 reserve");

        bool toggled = party.ToggleHeroActive("hero_4");
        Assert.IsTrue(toggled, "Should be able to move reserve hero to active");
        Assert.IsTrue(party.IsHeroActive("hero_4"), "hero_4 should now be active");
        Assert.IsTrue(party.IsHeroInReserve("hero_1"), "hero_1 should now be in reserve (moved to make room)");
    }

    private void TestMaxActiveEnforced()
    {
        Debug.Log("Testing max 3 active enforced...");

        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount());

        bool toggled = party.ToggleHeroActive("hero_4");
        Assert.IsTrue(toggled, "Should swap hero_4 into active (hero_1 moves to reserve)");

        party = CreateTestParty();
        party.ToggleHeroActive("hero_1");
        Assert.IsTrue(party.IsHeroInReserve("hero_1"), "hero_1 should now be in reserve");
        Assert.AreEqual(2, party.GetActiveCount(), "Should have 2 active after removing one");

        bool addedBack = party.ToggleHeroActive("hero_1");
        Assert.IsTrue(addedBack, "Should be able to add hero back to active");
        Assert.AreEqual(3, party.GetActiveCount(), "Should have 3 active again");
    }

    private void TestValidateActiveParty()
    {
        Debug.Log("Testing validate active party...");

        PartyData party = CreateTestParty();
        Assert.AreEqual(3, party.GetActiveCount(), "Full party should have 3 active");

        party.ToggleHeroActive("hero_1");
        Assert.AreEqual(2, party.GetActiveCount(), "After removing one, should have 2 active");
    }

    private void TestReserveTrackedSeparately()
    {
        Debug.Log("Testing reserve tracked separately...");

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

    private void TestPartyStatePersistsAcrossToggles()
    {
        Debug.Log("Testing party state persists across toggles...");

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

    private void TestSetActiveParty()
    {
        Debug.Log("Testing SetActiveParty...");

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
}
