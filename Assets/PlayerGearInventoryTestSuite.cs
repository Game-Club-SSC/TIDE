using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode tests for PlayerGearInventory (issue #295): drop ownership,
/// rarity preservation, duplicate gear, save/load round-trip, and stat
/// recalculation on equip/unequip.
/// </summary>
public class PlayerGearInventoryTestSuite
{
    private GameObject progressionObject;
    private HeroProgressionManager progression;
    private GameObject inventoryObject;
    private PlayerGearInventory inventory;
    private readonly List<GearSetData> createdGearSets = new List<GearSetData>();

    [SetUp]
    public void SetUp()
    {
        CleanupSingletons();

        progressionObject = new GameObject("HeroProgressionManager_Test");
        progression = progressionObject.AddComponent<HeroProgressionManager>();
        InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
        SetLevelingConfig(progression, CreateDefaultLevelingConfig());
        GearSetData testSet = CreateTestGearSet("iron_guard");
        createdGearSets.Add(testSet);
        SetAvailableGearSets(progression, new[] { testSet });

        inventoryObject = new GameObject("PlayerGearInventory_Test");
        inventory = inventoryObject.AddComponent<PlayerGearInventory>();
        InvokeOnEnableIfUnregistered(inventory, () => PlayerGearInventory.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (inventoryObject != null)
        {
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            inventoryObject = null;
        }

        if (progressionObject != null)
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            progressionObject = null;
        }

        CleanupSingletons();
        for (int i = 0; i < createdGearSets.Count; i++)
        {
            if (createdGearSets[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdGearSets[i]);
            }
        }

        createdGearSets.Clear();
    }

    /// <summary>Wipes runtime progression/inventory state so a test starts clean without destroying singletons.</summary>
    private void ResetRuntimeState()
    {
        if (progression != null)
        {
            progression.ResetProgressionForDebug();
        }

        if (inventory != null)
        {
            inventory.ResetInventoryForDebug();
        }
    }

    [Test]
    public void DropBecomesOwnedInstanceWithRarity()
    {
        ResetRuntimeState();
        GearInstance instance = inventory.AddGear("iron_guard", GearDropService.GearRarity.Epic);

        Assert.IsNotNull(instance, "A successful drop should award an owned instance.");
        Assert.AreEqual("iron_guard", instance.setId, "Instance set id should match the rolled set.");
        Assert.AreEqual(GearDropService.GearRarity.Epic, instance.rarity, "Rarity should be preserved on the instance.");
        Assert.IsFalse(string.IsNullOrEmpty(instance.instanceId), "Instance should have a unique id.");
        Assert.AreEqual(1, inventory.OwnedGearCount, "Inventory should own exactly one instance.");
        Assert.AreEqual(1, instance.level, "Fresh drops should start at level 1.");
    }

    [Test]
    public void DuplicateGearCreatesDistinctInstances()
    {
        ResetRuntimeState();
        GearInstance first = inventory.AddGear("iron_guard", GearDropService.GearRarity.Rare);
        GearInstance second = inventory.AddGear("iron_guard", GearDropService.GearRarity.Legendary);

        Assert.IsNotNull(first, "First drop should be awarded.");
        Assert.IsNotNull(second, "Second drop should be awarded.");
        Assert.AreNotEqual(first.instanceId, second.instanceId, "Duplicate drops must be distinct instances.");
        Assert.AreEqual(2, inventory.OwnedGearCount, "Two drops of the same set should both be owned.");

        IReadOnlyList<GearInstance> owned = inventory.GetOwnedGear();
        bool sawFirst = false;
        bool sawSecond = false;
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].instanceId == first.instanceId) sawFirst = true;
            if (owned[i].instanceId == second.instanceId) sawSecond = true;
        }

        Assert.IsTrue(sawFirst && sawSecond, "Both duplicate instances should appear in the owned listing.");
    }

    [Test]
    public void UnknownSetIdIsNotAwarded()
    {
        ResetRuntimeState();
        GearInstance instance = inventory.AddGear("nonexistent_set", GearDropService.GearRarity.Common);

        Assert.IsNull(instance, "Unknown set ids should not award gear.");
        Assert.AreEqual(0, inventory.OwnedGearCount, "Inventory should stay empty for an unknown set.");
    }

    [Test]
    public void SaveLoadRoundTripPreservesOwnershipRarityAndRolls()
    {
        ResetRuntimeState();
        GearInstance dropped = inventory.AddGear("iron_guard", GearDropService.GearRarity.Legendary);
        Assert.IsNotNull(dropped, "Setup: drop must award an instance.");
        dropped.GrantXp(500);
        Assert.AreEqual(3, dropped.UnlockedSlotCount, "Setup: maxed gear should have 3 rolled slots.");

        progression.EnsureHero("hero_fire");
        Assert.IsTrue(inventory.TryEquip("hero_fire", dropped.instanceId), "Setup: equip should succeed.");

        PlayerGearInventory.GearInventorySaveData snapshot = inventory.CaptureGearInventorySnapshot();
        Assert.AreEqual(1, snapshot.entries.Count, "Snapshot should contain one owned entry.");
        string json = JsonUtility.ToJson(snapshot);
        PlayerGearInventory.GearInventorySaveData reloaded = JsonUtility.FromJson<PlayerGearInventory.GearInventorySaveData>(json);
        Assert.IsNotNull(reloaded, "Snapshot should survive JSON round-trip.");

        // Simulate a fresh session: wipe runtime state, then load the snapshot.
        ResetRuntimeState();
        inventory.ApplyGearInventorySnapshot(reloaded);

        Assert.AreEqual(1, inventory.OwnedGearCount, "Ownership should survive the round-trip.");
        GearInstance loaded = inventory.GetOwnedGear()[0];
        Assert.AreEqual(dropped.instanceId, loaded.instanceId, "Instance id should survive the round-trip.");
        Assert.AreEqual("iron_guard", loaded.setId, "Set id should survive the round-trip.");
        Assert.AreEqual(GearDropService.GearRarity.Legendary, loaded.rarity, "Rarity should survive the round-trip.");
        Assert.AreEqual(dropped.level, loaded.level, "Level should survive the round-trip.");
        Assert.AreEqual(dropped.UnlockedSlotCount, loaded.UnlockedSlotCount, "Rolled slot count should survive the round-trip.");

        for (int i = 0; i < dropped.unlockedSlots.Count; i++)
        {
            Assert.AreEqual(dropped.unlockedSlots[i].statType, loaded.unlockedSlots[i].statType,
                $"Slot {i} stat type should survive the round-trip.");
            Assert.AreEqual(dropped.unlockedSlots[i].percentValue, loaded.unlockedSlots[i].percentValue,
                $"Slot {i} percent value should survive the round-trip.");
        }

        GearInstance reEquipped = inventory.GetEquipped("hero_fire");
        Assert.IsNotNull(reEquipped, "Equipped mapping should survive the round-trip.");
        Assert.AreEqual(dropped.instanceId, reEquipped.instanceId, "Equipped instance should survive the round-trip.");
    }

    [Test]
    public void EquipChangesStatsAndUnequipRestores()
    {
        ResetRuntimeState();
        GearInstance instance = inventory.AddGear("iron_guard", GearDropService.GearRarity.Uncommon);
        Assert.IsNotNull(instance, "Setup: drop must award an instance.");
        Assert.AreEqual(0, instance.UnlockedSlotCount, "Setup: fresh instance has no bonus slots, so stats are deterministic.");

        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        hero.heroId = "hero_fire";
        hero.displayName = "Andrian";
        hero.baseMaxHP = 100;
        hero.baseAttack = 14;
        hero.baseDefense = 5;
        hero.baseSpeed = 12;

        GameObject unitObject = new GameObject("TestUnit");
        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        try
        {
            progression.ApplyStatGrowth(unit, hero);
            Assert.AreEqual(100, unit.MaxHP, "No gear: HP should be base 100.");
            Assert.AreEqual(14, unit.Attack, "No gear: ATK should be base 14.");
            Assert.AreEqual(5, unit.Defense, "No gear: DEF should be base 5.");

            Assert.IsTrue(inventory.TryEquip("hero_fire", instance.instanceId), "Equip should succeed.");
            progression.ApplyStatGrowth(unit, hero);

            int expectedHp = 100 + Mathf.RoundToInt(100 * 0.20f);
            int expectedAtk = 14 + Mathf.RoundToInt(14 * 0.10f);
            int expectedDef = 5 + Mathf.RoundToInt(5 * 0.20f);
            Assert.AreEqual(expectedHp, unit.MaxHP, "Equipped iron_guard should add its full-set HP bonus (20%).");
            Assert.AreEqual(expectedAtk, unit.Attack, "Equipped iron_guard should add its ATK bonus (10%).");
            Assert.AreEqual(expectedDef, unit.Defense, "Equipped iron_guard should add its DEF bonus (20%).");

            Assert.IsTrue(inventory.TryUnequip("hero_fire"), "Unequip should succeed.");
            progression.ApplyStatGrowth(unit, hero);
            Assert.AreEqual(100, unit.MaxHP, "Unequip should restore base HP.");
            Assert.AreEqual(14, unit.Attack, "Unequip should restore base ATK.");
            Assert.AreEqual(5, unit.Defense, "Unequip should restore base DEF.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitObject);
            UnityEngine.Object.DestroyImmediate(hero);
        }
    }

    [Test]
    public void EquipMovesInstanceOffOtherHeroes()
    {
        ResetRuntimeState();
        GearInstance instance = inventory.AddGear("iron_guard", GearDropService.GearRarity.Common);
        Assert.IsNotNull(instance, "Setup: drop must award an instance.");

        Assert.IsTrue(inventory.TryEquip("hero_fire", instance.instanceId), "First equip should succeed.");
        Assert.IsTrue(inventory.TryEquip("hero_water", instance.instanceId), "Equipping the same instance on another hero should succeed.");
        Assert.IsNull(inventory.GetEquipped("hero_fire"), "Instance should no longer be equipped on the previous hero.");
        Assert.AreEqual(instance.instanceId, inventory.GetEquipped("hero_water").instanceId, "Instance should now be equipped on the new hero.");
    }

    [Test]
    public void LegacySaveJsonWithoutGearInventoryStillParses()
    {
        string legacyJson = "{\"puzzleStates\":[],\"ancientTextStates\":[],\"completedNarrativeBeatIds\":[],\"restorationSnapshot\":null,\"gearProgression\":null,\"progressionSnapshot\":null,\"storyProgression\":null}";

        GameStateManager.WorldStateSaveData parsed = JsonUtility.FromJson<GameStateManager.WorldStateSaveData>(legacyJson);
        Assert.IsNotNull(parsed, "Legacy save JSON should still deserialize cleanly with the new gearInventory field.");
        // Unity 6 JsonUtility instantiates class-type fields even when JSON carries null,
        // so the contract is "empty and safe to apply", not "null".
        Assert.IsNotNull(parsed.gearInventory, "gearInventory section should deserialize without crashing.");
        Assert.AreEqual(0, parsed.gearInventory.entries.Count, "Legacy saves carry no owned gear entries.");
        Assert.AreEqual(0, parsed.gearInventory.equippedHeroIds.Count, "Legacy saves carry no equipped mapping.");
        Assert.AreEqual(0, parsed.gearInventory.equippedInstanceIds.Count, "Legacy saves carry no equipped mapping.");

        ResetRuntimeState();
        inventory.ApplyGearInventorySnapshot(parsed.gearInventory);
        Assert.AreEqual(0, inventory.OwnedGearCount, "Applying an empty legacy gearInventory must be a no-op.");
    }

    [Test]
    public void BattleDropsExposedForResults()
    {
        ResetRuntimeState();
        GearInstance first = inventory.AddGear("iron_guard", GearDropService.GearRarity.Common);
        inventory.RecordBattleDrop(first);
        GearInstance second = inventory.AddGear("iron_guard", GearDropService.GearRarity.Epic);
        inventory.RecordBattleDrop(second);

        Assert.AreEqual(2, inventory.LastBattleDrops.Count, "Both drops of the battle should be exposed.");
        Assert.AreEqual(second.instanceId, inventory.LastBattleDrops[1].instanceId, "Second drop should be last.");

        inventory.BeginNewCombat();
        Assert.AreEqual(0, inventory.LastBattleDrops.Count, "A new combat should clear the previous battle's drops.");
    }

    private static void InvokeOnEnableIfUnregistered(MonoBehaviour component, System.Func<object> instanceGetter)
    {
        if (component == null || instanceGetter() != null)
        {
            return;
        }

        // Edit-mode batchmode does not fire OnEnable synchronously on AddComponent
        // (see MobileTouchInputManagerTest's reflection lifecycle pattern).
        MethodInfo onEnable = component.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        if (onEnable != null)
        {
            onEnable.Invoke(component, null);
        }
    }

    private static void CleanupSingletons()
    {
        if (PlayerGearInventory.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(PlayerGearInventory.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }
    }

    private static GearSetData CreateTestGearSet(string setId)
    {
        GearSetData gear = ScriptableObject.CreateInstance<GearSetData>();
        gear.setId = setId;
        gear.displayName = "Iron Guard Set";
        gear.description = "Heavy iron armor for testing.";
        gear.attackBonusPercent = 0.05f;
        gear.defenseBonusPercent = 0.10f;
        gear.hpBonusPercent = 0.10f;
        gear.setBonusAttackPercent = 0.05f;
        gear.setBonusDefensePercent = 0.10f;
        gear.setBonusHpPercent = 0.10f;
        gear.setBonusDescription = "Iron Resolve: +5% ATK, +10% DEF, +10% HP";
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
