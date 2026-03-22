using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GearSystemTest : MonoBehaviour
{
    [SerializeField] private LevelingConfig testLevelingConfig;

    [ContextMenu("Run All Gear Tests")]
    public void RunAllTests()
    {
        TestEquipGearSet();
        TestUnequipGearSet();
        TestFullSetBonus();
        TestGearWithLevelGrowth();
        TestGearStatBonuses();
        Debug.Log("=== All gear tests passed ===");
    }

    [ContextMenu("Test Equip Gear Set")]
    public void TestEquipGearSet()
    {
        GameObject managerObject = new GameObject("GearManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            SetAvailableGearSets(manager, new GearSetData[] { CreateTestGearSet() });

            manager.EnsureHero("hero_fire");
            manager.EquipGearSet("hero_fire", CreateTestGearSet());

            GearSetData equipped = manager.GetEquippedGearSet("hero_fire");
            Assert.IsNotNull(equipped, "Hero should have gear equipped.");
            Assert.AreEqual("iron_guard", equipped.setId, "Equipped set should be iron_guard.");

            Debug.Log("[GearSystemTest] TestEquipGearSet passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Unequip Gear Set")]
    public void TestUnequipGearSet()
    {
        GameObject managerObject = new GameObject("GearManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            SetAvailableGearSets(manager, new GearSetData[] { CreateTestGearSet() });

            manager.EnsureHero("hero_water");
            manager.EquipGearSet("hero_water", CreateTestGearSet());

            GearSetData equipped = manager.GetEquippedGearSet("hero_water");
            Assert.IsNotNull(equipped, "Should have gear before unequipping.");

            manager.UnequipGearSet("hero_water");

            equipped = manager.GetEquippedGearSet("hero_water");
            Assert.IsNull(equipped, "Should have no gear after unequipping.");

            Assert.AreEqual(0f, manager.GetAttackBonusPercent("hero_water"), "ATK bonus should be 0 after unequip.");
            Assert.AreEqual(0f, manager.GetDefenseBonusPercent("hero_water"), "DEF bonus should be 0 after unequip.");

            Debug.Log("[GearSystemTest] TestUnequipGearSet passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Full Set Bonus")]
    public void TestFullSetBonus()
    {
        GameObject managerObject = new GameObject("GearManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            GearSetData gearSet = CreateTestGearSet();
            SetAvailableGearSets(manager, new GearSetData[] { gearSet });

            manager.EnsureHero("hero_earth");
            manager.EquipGearSet("hero_earth", gearSet);

            float atkPercent = manager.GetAttackBonusPercent("hero_earth");
            float defPercent = manager.GetDefenseBonusPercent("hero_earth");

            Assert.AreEqual(0.10f, atkPercent, "Total ATK bonus should be base 5% + set bonus 5% = 10%.");
            Assert.AreEqual(0.25f, defPercent, "Total DEF bonus should be base 15% + set bonus 10% = 25%.");

            Debug.Log("[GearSystemTest] TestFullSetBonus passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Gear With Level Growth")]
    public void TestGearWithLevelGrowth()
    {
        GameObject managerObject = new GameObject("GearManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            SetAvailableGearSets(manager, new GearSetData[] { CreateTestGearSet() });

            HeroData hero = ScriptableObject.CreateInstance<HeroData>();
            hero.heroId = "hero_fire";
            hero.displayName = "Andrian";
            hero.baseMaxHP = 100;
            hero.baseAttack = 14;
            hero.baseDefense = 5;
            hero.baseSpeed = 12;

            manager.EnsureHero("hero_fire");

            // Level up to 2
            manager.GrantXp("hero_fire", 100);
            Assert.AreEqual(2, manager.GetLevel("hero_fire"), "Should be level 2.");

            // Equip gear
            manager.EquipGearSet("hero_fire", CreateTestGearSet());

            // Create unit and apply stats
            GameObject unitObject = new GameObject("TestUnit");
            CombatUnit unit = unitObject.AddComponent<CombatUnit>();
            manager.ApplyStatGrowth(unit, hero);

            // Level 2 base: HP=105, ATK=15, DEF=6, SPD=13
            // Gear (10% ATK, 25% DEF, 10% HP):
            //   HP = 105 + 10% = 115.5 -> 116 (rounding)
            //   ATK = 15 + 10% = 16.5 -> 17
            //   DEF = 6 + 25% = 7.5 -> 8
            int expectedHp = 105 + Mathf.RoundToInt(105 * 0.10f);
            int expectedAtk = 15 + Mathf.RoundToInt(15 * 0.10f);
            int expectedDef = 6 + Mathf.RoundToInt(6 * 0.25f);

            Assert.AreEqual(expectedHp, unit.MaxHP, $"HP should be {expectedHp} (level 2 + gear).");
            Assert.AreEqual(expectedAtk, unit.Attack, $"ATK should be {expectedAtk} (level 2 + gear).");
            Assert.AreEqual(expectedDef, unit.Defense, $"DEF should be {expectedDef} (level 2 + gear).");

            Debug.Log("[GearSystemTest] TestGearWithLevelGrowth passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Gear Stat Bonuses")]
    public void TestGearStatBonuses()
    {
        GameObject managerObject = new GameObject("GearManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetLevelingConfig(manager, CreateDefaultLevelingConfig());
            SetAvailableGearSets(manager, new GearSetData[] { CreateTestGearSet() });

            manager.EnsureHero("hero_air");

            // No gear at level 1
            HeroData hero = ScriptableObject.CreateInstance<HeroData>();
            hero.heroId = "hero_air";
            hero.displayName = "McManus";
            hero.baseMaxHP = 80;
            hero.baseAttack = 12;
            hero.baseDefense = 5;
            hero.baseSpeed = 14;

            GameObject unitObject = new GameObject("TestUnit");
            CombatUnit unit = unitObject.AddComponent<CombatUnit>();
            manager.ApplyStatGrowth(unit, hero);

            Assert.AreEqual(80, unit.MaxHP, "Level 1 no gear should have base HP.");
            Assert.AreEqual(12, unit.Attack, "Level 1 no gear should have base ATK.");

            // Equip gear
            manager.EquipGearSet("hero_air", CreateTestGearSet());
            manager.ApplyStatGrowth(unit, hero);

            int expectedHp = 80 + Mathf.RoundToInt(80 * 0.10f);
            int expectedAtk = 12 + Mathf.RoundToInt(12 * 0.10f);

            Assert.AreEqual(expectedHp, unit.MaxHP, "Level 1 with gear should have base + 10% HP.");
            Assert.AreEqual(expectedAtk, unit.Attack, "Level 1 with gear should have base + 10% ATK.");

            Debug.Log("[GearSystemTest] TestGearStatBonuses passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
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
