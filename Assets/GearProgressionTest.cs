using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GearProgressionTest : MonoBehaviour
{
    [ContextMenu("Run All Gear Progression Tests")]
    public void RunAllTests()
    {
        TestGearInstanceXpAndLevelUp();
        TestGearInstanceRandomBonusBoundariesAndNoDuplicates();
        TestGearInstanceDuplicatePreservesRolls();
        TestBattleXpGrantsGearXpToActiveAndReserve();
        TestSmithyDuplicationRequiresFinalizedRolls();
        TestGearSnapshotRoundTrip();
        Debug.Log("=== All gear progression tests passed ===");
    }

    [ContextMenu("Test Gear XP and Slot Unlock Milestones")]
    public void TestGearInstanceXpAndLevelUp()
    {
        GearInstance instance = CreateFreshInstance();

        Assert.AreEqual(1, instance.level, "Fresh gear should start at Lv.1.");
        Assert.AreEqual(0, instance.UnlockedSlotCount, "Fresh gear should start with 0 unlocked slots.");

        bool leveled = instance.GrantXp(50);
        Assert.IsTrue(leveled, "Lv.1->2 should level at 50 XP.");
        Assert.AreEqual(2, instance.level, "Gear should be Lv.2 after first milestone.");
        Assert.AreEqual(1, instance.UnlockedSlotCount, "Lv.2 should unlock first slot.");

        leveled = instance.GrantXp(75);
        Assert.IsTrue(leveled, "Lv.2->3 should level at 75 XP.");
        Assert.AreEqual(3, instance.level, "Gear should be Lv.3 after second milestone.");
        Assert.AreEqual(2, instance.UnlockedSlotCount, "Lv.3 should unlock second slot.");

        leveled = instance.GrantXp(100);
        Assert.IsTrue(leveled, "Lv.3->4 should level at 100 XP.");
        Assert.AreEqual(4, instance.level, "Gear should be capped at Lv.4 (3 slot unlocks).");
        Assert.AreEqual(3, instance.UnlockedSlotCount, "Lv.4 should unlock third slot.");

        leveled = instance.GrantXp(999);
        Assert.IsFalse(leveled, "Gear should not level beyond configured cap.");
        Assert.AreEqual(4, instance.level, "Gear should stay at cap.");
        Assert.AreEqual(3, instance.UnlockedSlotCount, "Unlocked slots should remain capped.");

        Debug.Log("[GearProgressionTest] TestGearInstanceXpAndLevelUp passed.");
    }

    [ContextMenu("Test Random Bonus Boundaries + No Duplicates")]
    public void TestGearInstanceRandomBonusBoundariesAndNoDuplicates()
    {
        GearInstance instance = CreateFreshInstance();
        instance.GrantXp(500);

        Assert.AreEqual(3, instance.unlockedSlots.Count, "Max level gear should have exactly 3 rolled slots.");

        HashSet<GearBonusStatType> seenStats = new HashSet<GearBonusStatType>();
        for (int i = 0; i < instance.unlockedSlots.Count; i++)
        {
            GearSlotBonus slot = instance.unlockedSlots[i];
            Assert.IsTrue(slot.percentValue >= GearInstance.MinBonusPercent,
                $"Slot {i} bonus should be >= {GearInstance.MinBonusPercent:P0}.");
            Assert.IsTrue(slot.percentValue <= GearInstance.MaxBonusPercent,
                $"Slot {i} bonus should be <= {GearInstance.MaxBonusPercent:P0}.");
            Assert.IsTrue(seenStats.Add(slot.statType),
                $"Slot {i} rolled duplicate stat {slot.statType}, expected duplicate prevention.");
        }

        Debug.Log("[GearProgressionTest] TestGearInstanceRandomBonusBoundariesAndNoDuplicates passed.");
    }

    [ContextMenu("Test Duplicate Preserves Finalized Rolls")]
    public void TestGearInstanceDuplicatePreservesRolls()
    {
        GearInstance source = CreateFreshInstance();
        source.GrantXp(500);

        GearInstance duplicate = source.Duplicate();

        Assert.AreNotEqual(source.instanceId, duplicate.instanceId, "Duplicate must get a unique instance id.");
        Assert.AreEqual(source.setId, duplicate.setId, "Duplicate set id should match source.");
        Assert.AreEqual(source.level, duplicate.level, "Duplicate level should match source.");
        Assert.AreEqual(source.currentXp, duplicate.currentXp, "Duplicate XP should match source.");
        Assert.AreEqual(source.UnlockedSlotCount, duplicate.UnlockedSlotCount, "Duplicate slot count should match source.");

        for (int i = 0; i < source.unlockedSlots.Count; i++)
        {
            Assert.AreEqual(source.unlockedSlots[i].statType, duplicate.unlockedSlots[i].statType,
                $"Duplicate slot {i} stat type should match source.");
            Assert.AreEqual(source.unlockedSlots[i].percentValue, duplicate.unlockedSlots[i].percentValue,
                $"Duplicate slot {i} value should match source.");
        }

        Debug.Log("[GearProgressionTest] TestGearInstanceDuplicatePreservesRolls passed.");
    }

    [ContextMenu("Test Battle XP -> Gear XP Pipeline")]
    public void TestBattleXpGrantsGearXpToActiveAndReserve()
    {
        if (HeroProgressionManager.Instance != null)
        {
            DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        GameObject managerObject = new GameObject("GearPipelineMgr_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            GearSetData gearSet = CreateTestGearSet();
            SetAvailableGearSets(manager, new[] { gearSet });

            HeroData activeHero = CreateHero("hero_fire", "ActiveFire");
            HeroData reserveHero = CreateHero("hero_water", "ReserveWater");

            manager.EquipGearSet(activeHero.heroId, gearSet);
            manager.EquipGearSet(reserveHero.heroId, gearSet);

            GearInstance activeGear = manager.GetEquippedGearInstance(activeHero.heroId);
            GearInstance reserveGear = manager.GetEquippedGearInstance(reserveHero.heroId);
            Assert.IsNotNull(activeGear, "Active hero should have equipped gear instance.");
            Assert.IsNotNull(reserveGear, "Reserve hero should have equipped gear instance.");

            manager.GrantBattleXp(100, new[] { activeHero }, new[] { reserveHero });

            Assert.AreEqual(20, activeGear.currentXp, "Active hero gear should receive full per-battle gear XP (20).");
            Assert.AreEqual(10, reserveGear.currentXp, "Reserve hero gear should receive reduced gear XP (10).");

            Debug.Log("[GearProgressionTest] TestBattleXpGrantsGearXpToActiveAndReserve passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Smithy Duplication Finalized Requirement")]
    public void TestSmithyDuplicationRequiresFinalizedRolls()
    {
        GearInstance notFinalized = CreateFreshInstance();
        Assert.IsTrue(notFinalized.UnlockedSlotCount < GearInstance.MaxBonusSlots,
            "Fresh gear should not be considered finalized.");

        GearInstance finalized = CreateFreshInstance();
        finalized.GrantXp(500);
        Assert.AreEqual(GearInstance.MaxBonusSlots, finalized.UnlockedSlotCount,
            "Maxed gear should be considered finalized.");

        Debug.Log("[GearProgressionTest] TestSmithyDuplicationRequiresFinalizedRolls passed.");
    }

    [ContextMenu("Test Gear Snapshot Persistence")]
    public void TestGearSnapshotRoundTrip()
    {
        if (HeroProgressionManager.Instance != null)
        {
            DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        GameObject managerObject = new GameObject("GearSnapshotMgr_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            GearSetData gearSet = CreateTestGearSet();
            SetAvailableGearSets(manager, new[] { gearSet });

            manager.SetCurrency(200);
            manager.EnsureHero("hero_fire");
            manager.EquipGearSet("hero_fire", gearSet);

            GearInstance equipped = manager.GetEquippedGearInstance("hero_fire");
            Assert.IsNotNull(equipped, "Hero should have equipped instance before snapshot.");
            equipped.GrantXp(500);

            GearProgressionSaveData snapshot = manager.CaptureGearSnapshot();

            manager.SetCurrency(0);
            manager.UnequipGearSet("hero_fire");
            manager.ApplyGearSnapshot(snapshot);

            Assert.AreEqual(200, manager.Currency, "Currency should round-trip through snapshot.");
            GearInstance loaded = manager.GetEquippedGearInstance("hero_fire");
            Assert.IsNotNull(loaded, "Equipped instance should restore from snapshot.");
            Assert.AreEqual(equipped.level, loaded.level, "Gear level should persist across snapshot.");
            Assert.AreEqual(equipped.UnlockedSlotCount, loaded.UnlockedSlotCount, "Unlocked slots should persist across snapshot.");

            for (int i = 0; i < equipped.unlockedSlots.Count; i++)
            {
                Assert.AreEqual(equipped.unlockedSlots[i].statType, loaded.unlockedSlots[i].statType,
                    $"Loaded slot {i} stat type should match saved slot.");
                Assert.AreEqual(equipped.unlockedSlots[i].percentValue, loaded.unlockedSlots[i].percentValue,
                    $"Loaded slot {i} value should match saved slot.");
            }

            Debug.Log("[GearProgressionTest] TestGearSnapshotRoundTrip passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    private static GearInstance CreateFreshInstance()
    {
        return new GearInstance
        {
            instanceId = System.Guid.NewGuid().ToString(),
            setId = "iron_guard",
            template = CreateTestGearSet(),
            level = 1,
            currentXp = 0,
            unlockedSlots = new List<GearSlotBonus>()
        };
    }

    private static HeroData CreateHero(string heroId, string displayName)
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        hero.heroId = heroId;
        hero.displayName = displayName;
        hero.baseMaxHP = 100;
        hero.baseMaxMP = 25;
        hero.baseAttack = 12;
        hero.baseDefense = 8;
        hero.baseSpeed = 10;
        return hero;
    }

    private static GearSetData CreateTestGearSet()
    {
        GearSetData gear = ScriptableObject.CreateInstance<GearSetData>();
        gear.setId = "iron_guard";
        gear.displayName = "Iron Guard Set";
        gear.description = "Heavy iron armor for testing.";
        gear.attackBonusPercent = 0.05f;
        gear.defenseBonusPercent = 0.15f;
        gear.hpBonusPercent = 0.10f;
        gear.setBonusAttackPercent = 0.05f;
        gear.setBonusDefensePercent = 0.10f;
        gear.setBonusHpPercent = 0.0f;
        gear.setBonusDescription = "Iron Resolve: +5% ATK, +10% DEF";
        return gear;
    }

    private static LevelingConfig CreateDefaultLevelingConfig()
    {
        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();
        config.baseXpToLevel = 100;
        config.xpPerLevelIncrement = 50;
        config.hpPerLevel = 5;
        config.mpPerLevel = 2;
        config.attackPerLevel = 1;
        config.defensePerLevel = 1;
        config.speedPerLevel = 1;
        config.reserveXpMultiplier = 0.5f;
        config.maxLevel = 20;
        return config;
    }

    private static void SetLevelingConfig(HeroProgressionManager manager, LevelingConfig config)
    {
        FieldInfo field = typeof(HeroProgressionManager).GetField("levelingConfig", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(manager, config);
    }

    private static void SetAvailableGearSets(HeroProgressionManager manager, GearSetData[] gearSets)
    {
        FieldInfo field = typeof(HeroProgressionManager).GetField("availableGearSets", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(manager, gearSets);
    }
}
