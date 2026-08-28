using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroProgressionTest : MonoBehaviour
{
    [SerializeField] private LevelingConfig testConfig;

    [ContextMenu("Run All Progression Tests")]
    public void RunAllTests()
    {
        TestXpGranting();
        TestLevelUp();
        TestStatGrowth();
        TestActiveVsReserveXp();
        TestMaxLevelCap();
        TestBattleXpIntegration();
        TestDuplicatePartyEntriesAwardXpOnce();
        TestMalformedProgressionSnapshotIsClamped();
        TestNormalSkillUnlockProgression();
        Debug.Log("=== All progression tests passed ===");
    }

    [ContextMenu("Test XP Granting")]
    public void TestXpGranting()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetConfig(manager, CreateDefaultConfig());

            manager.EnsureHero("hero_fire");
            Assert.AreEqual(1, manager.GetLevel("hero_fire"), "Hero should start at level 1.");
            Assert.AreEqual(0, manager.GetXp("hero_fire"), "Hero should start with 0 XP.");

            manager.GrantXp("hero_fire", 50);
            Assert.AreEqual(50, manager.GetXp("hero_fire"), "XP should be 50 after granting 50.");
            Assert.AreEqual(1, manager.GetLevel("hero_fire"), "Hero should still be level 1 at 50 XP (need 100).");

            manager.GrantXp("hero_fire", 30);
            Assert.AreEqual(80, manager.GetXp("hero_fire"), "XP should be 80 after granting 30 more.");

            Debug.Log("[HeroProgressionTest] TestXpGranting passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Level Up")]
    public void TestLevelUp()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            SetConfig(manager, CreateDefaultConfig());

            manager.EnsureHero("hero_water");
            bool leveledUp = manager.GrantXp("hero_water", 100);

            Assert.IsTrue(leveledUp, "Should have leveled up at 100 XP.");
            Assert.AreEqual(2, manager.GetLevel("hero_water"), "Hero should be level 2.");
            Assert.AreEqual(0, manager.GetXp("hero_water"), "XP should overflow and reset to 0.");

            leveledUp = manager.GrantXp("hero_water", 149);
            Assert.IsFalse(leveledUp, "Should not level up at 149 XP (need 150 for level 3).");
            Assert.AreEqual(2, manager.GetLevel("hero_water"), "Hero should still be level 2.");

            leveledUp = manager.GrantXp("hero_water", 1);
            Assert.IsTrue(leveledUp, "Should level up at 150 XP.");
            Assert.AreEqual(3, manager.GetLevel("hero_water"), "Hero should be level 3.");
            Assert.AreEqual(0, manager.GetXp("hero_water"), "XP should reset after level up.");

            Debug.Log("[HeroProgressionTest] TestLevelUp passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Stat Growth")]
    public void TestStatGrowth()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            LevelingConfig config = CreateDefaultConfig();
            SetConfig(manager, config);

            HeroData hero = ScriptableObject.CreateInstance<HeroData>();
            hero.heroId = "hero_earth";
            hero.displayName = "Clinton";
            hero.baseMaxHP = 120;
            hero.baseMaxMP = 35;
            hero.baseAttack = 10;
            hero.baseDefense = 12;
            hero.baseSpeed = 8;

            manager.EnsureHero("hero_earth");

            GameObject unitObject = new GameObject("TestUnit");
            CombatUnit unit = unitObject.AddComponent<CombatUnit>();
            unit.MaxHP = hero.baseMaxHP;
            unit.MaxMP = hero.baseMaxMP;
            unit.Attack = hero.baseAttack;
            unit.Defense = hero.baseDefense;
            unit.Speed = hero.baseSpeed;

            manager.ApplyStatGrowth(unit, hero);
            Assert.AreEqual(120, unit.MaxHP, "Level 1 should have base HP.");

            manager.GrantXp("hero_earth", 100);
            Assert.AreEqual(2, manager.GetLevel("hero_earth"), "Should be level 2.");

            manager.ApplyStatGrowth(unit, hero);
            Assert.AreEqual(125, unit.MaxHP, "Level 2 should have base + 5 HP.");
            Assert.AreEqual(37, unit.MaxMP, "Level 2 should have base + 2 MP.");
            Assert.AreEqual(11, unit.Attack, "Level 2 should have base + 1 attack.");
            Assert.AreEqual(13, unit.Defense, "Level 2 should have base + 1 defense.");
            Assert.AreEqual(9, unit.Speed, "Level 2 should have base + 1 speed.");

            manager.GrantXp("hero_earth", 150);
            Assert.AreEqual(3, manager.GetLevel("hero_earth"), "Should be level 3.");

            manager.ApplyStatGrowth(unit, hero);
            Assert.AreEqual(130, unit.MaxHP, "Level 3 should have base + 10 HP.");
            Assert.AreEqual(12, unit.Attack, "Level 3 should have base + 2 attack.");

            Debug.Log("[HeroProgressionTest] TestStatGrowth passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Active vs Reserve XP")]
    public void TestActiveVsReserveXp()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            LevelingConfig config = CreateDefaultConfig();
            config.reserveXpMultiplier = 0.5f;
            SetConfig(manager, config);

            HeroData active1 = ScriptableObject.CreateInstance<HeroData>();
            active1.heroId = "hero_fire";
            active1.displayName = "Andrian";

            HeroData active2 = ScriptableObject.CreateInstance<HeroData>();
            active2.heroId = "hero_water";
            active2.displayName = "Ryan";

            HeroData reserve1 = ScriptableObject.CreateInstance<HeroData>();
            reserve1.heroId = "hero_air";
            reserve1.displayName = "McManus";

            HeroData[] active = { active1, active2 };
            HeroData[] reserve = { reserve1 };

            manager.GrantBattleXp(100, active, reserve);

            Assert.AreEqual(2, manager.GetLevel("hero_fire"), "Active member should level up after receiving 100 XP.");
            Assert.AreEqual(0, manager.GetXp("hero_fire"), "Active member XP should roll over after leveling.");
            Assert.AreEqual(2, manager.GetLevel("hero_water"), "Second active member should also level up.");
            Assert.AreEqual(0, manager.GetXp("hero_water"), "Second active member XP should roll over after leveling.");
            Assert.AreEqual(50, manager.GetXp("hero_air"), "Reserve member should get 50 XP (50% of 100).");

            Debug.Log("[HeroProgressionTest] TestActiveVsReserveXp passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Max Level Cap")]
    public void TestMaxLevelCap()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            LevelingConfig config = CreateDefaultConfig();
            config.maxLevel = 3;
            SetConfig(manager, config);

            manager.EnsureHero("hero_space");

            bool leveledUp = manager.GrantXp("hero_space", 99999);
            Assert.IsTrue(leveledUp, "Should have leveled up.");
            Assert.AreEqual(3, manager.GetLevel("hero_space"), "Should be at max level 3.");

            leveledUp = manager.GrantXp("hero_space", 99999);
            Assert.IsFalse(leveledUp, "Should not level up past max level.");
            Assert.AreEqual(3, manager.GetLevel("hero_space"), "Should still be at max level 3.");

            Debug.Log("[HeroProgressionTest] TestMaxLevelCap passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Battle XP Integration")]
    public void TestBattleXpIntegration()
    {
        GameObject managerObject = new GameObject("ProgressionManager_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        GameObject battleObject = new GameObject("BattleManager_Test");
        BattleManager battleManager = battleObject.AddComponent<BattleManager>();

        GameObject unitsRoot = new GameObject("TestUnits");

        try
        {
            SetConfig(manager, CreateDefaultConfig());

            CombatUnit enemy1 = CreateUnit(unitsRoot.transform, "Imp1", CombatUnit.UnitType.Enemy, xpReward: 25);
            CombatUnit enemy2 = CreateUnit(unitsRoot.transform, "Imp2", CombatUnit.UnitType.Enemy, xpReward: 25);
            CombatUnit enemy3 = CreateUnit(unitsRoot.transform, "Imp3", CombatUnit.UnitType.Enemy, xpReward: 25);

            battleManager.RegisterUnit(enemy1);
            battleManager.RegisterUnit(enemy2);
            battleManager.RegisterUnit(enemy3);

            int totalXp = manager.GetTotalXpFromEnemies(battleManager);
            Assert.AreEqual(75, totalXp, "3 Imps with 25 XP each should give 75 total XP.");

            HeroData activeHero = ScriptableObject.CreateInstance<HeroData>();
            activeHero.heroId = "hero_fire";
            activeHero.displayName = "Andrian";

            HeroData[] active = { activeHero };
            HeroData[] reserve = System.Array.Empty<HeroData>();

            manager.GrantBattleXp(totalXp, active, reserve);
            Assert.AreEqual(75, manager.GetXp("hero_fire"), "Active hero should have 75 XP.");
            Assert.AreEqual(1, manager.GetLevel("hero_fire"), "Should still be level 1 at 75 XP.");

            manager.GrantBattleXp(totalXp, active, reserve);
            Assert.AreEqual(2, manager.GetLevel("hero_fire"), "Should level up to 2 after second battle (150 total XP).");
            Assert.AreEqual(50, manager.GetXp("hero_fire"), "Should have 50 XP remaining after level up (150 - 100).");

            Debug.Log("[HeroProgressionTest] TestBattleXpIntegration passed.");
        }
        finally
        {
            DestroyImmediate(unitsRoot);
            DestroyImmediate(battleObject);
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Duplicate Party XP Guard")]
    public void TestDuplicatePartyEntriesAwardXpOnce()
    {
        GameObject managerObject = new GameObject("ProgressionDuplicateParty_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();
        HeroData activeHero = ScriptableObject.CreateInstance<HeroData>();
        HeroData duplicateReserve = ScriptableObject.CreateInstance<HeroData>();

        try
        {
            SetConfig(manager, CreateDefaultConfig());
            activeHero.heroId = "hero_duplicate";
            activeHero.displayName = "Active Copy";
            duplicateReserve.heroId = "hero_duplicate";
            duplicateReserve.displayName = "Reserve Copy";

            manager.GrantBattleXp(50,
                new[] { activeHero, activeHero },
                new[] { duplicateReserve });

            Assert.AreEqual(50, manager.GetXp("hero_duplicate"),
                "Malformed duplicate party slots must not grant the same hero full XP twice or add reserve XP.");
        }
        finally
        {
            DestroyImmediate(activeHero);
            DestroyImmediate(duplicateReserve);
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Malformed Progression Snapshot Clamping")]
    public void TestMalformedProgressionSnapshotIsClamped()
    {
        GameObject managerObject = new GameObject("ProgressionSnapshotClamp_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();

        try
        {
            LevelingConfig config = CreateDefaultConfig();
            config.maxLevel = 3;
            SetConfig(manager, config);

            HeroProgressionManager.HeroProgressionSnapshot snapshot =
                new HeroProgressionManager.HeroProgressionSnapshot
                {
                    heroIds = new System.Collections.Generic.List<string> { "hero_max", "hero_mid" },
                    levels = new System.Collections.Generic.List<int> { 999, 2 },
                    currentXpValues = new System.Collections.Generic.List<int> { int.MaxValue, int.MaxValue }
                };

            manager.ApplyHeroProgressionSnapshot(snapshot);

            Assert.AreEqual(3, manager.GetLevel("hero_max"), "Loaded hero level should clamp to the configured cap.");
            Assert.AreEqual(0, manager.GetXp("hero_max"), "Max-level heroes must not retain unusable overflow XP.");
            Assert.AreEqual(2, manager.GetLevel("hero_mid"));
            Assert.AreEqual(149, manager.GetXp("hero_mid"),
                "Current XP should clamp below the 150 XP level-up threshold for level 2.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test Normal Skill Unlock Progression")]
    public void TestNormalSkillUnlockProgression()
    {
        GameObject managerObject = new GameObject("ProgressionSkillUnlock_Test");
        HeroProgressionManager manager = managerObject.AddComponent<HeroProgressionManager>();
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        SkillData starter = ScriptableObject.CreateInstance<SkillData>();
        SkillData levelTwo = ScriptableObject.CreateInstance<SkillData>();

        try
        {
            SetConfig(manager, CreateDefaultConfig());
            hero.heroId = "hero_skill_unlock";
            hero.displayName = "Skill Tester";
            hero.element = CombatUnit.Element.Fire;
            starter.skillName = "Starter";
            levelTwo.skillName = "Level Two";
            hero.starterSkills = new[] { starter };
            hero.normalSkillUnlocks = new[]
            {
                new HeroSkillUnlock { skill = levelTwo, unlockLevel = 2 }
            };

            Assert.AreEqual(1, manager.GetUnlockedNormalSkills(hero).Length,
                "A level-one hero should only have its starter skill.");

            manager.GrantXp(hero.heroId, 100);
            SkillData[] unlocked = manager.GetUnlockedNormalSkills(hero);
            Assert.AreEqual(2, unlocked.Length, "Level two should add the configured normal skill.");
            Assert.AreSame(levelTwo, unlocked[1], "The configured level-two skill should become usable.");
        }
        finally
        {
            DestroyImmediate(levelTwo);
            DestroyImmediate(starter);
            DestroyImmediate(hero);
            DestroyImmediate(managerObject);
        }
    }

    private static LevelingConfig CreateDefaultConfig()
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

    private static void SetConfig(HeroProgressionManager manager, LevelingConfig config)
    {
        FieldInfo field = typeof(HeroProgressionManager).GetField("levelingConfig", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(manager, config);
    }

    private static CombatUnit CreateUnit(Transform parent, string name, CombatUnit.UnitType type, int xpReward = 0)
    {
        GameObject unitObject = new GameObject(name);
        unitObject.transform.SetParent(parent, false);

        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        unit.UnitName = name;
        unit.Type = type;
        unit.MaxHP = 50;
        unit.HP = 50;
        unit.XpReward = xpReward;

        return unit;
    }
}
