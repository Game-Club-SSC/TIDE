using System;
using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
internal static class Program
{
    private static int Main()
    {
        try
        {
            new HeroProgressionTest().RunAllTests();
            new GearSystemTest().RunAllTests();
            new GearProgressionTest().RunAllTests();
            TestReadinessEdges();
            TestPartyRestoreEdges();
            Console.WriteLine($"Hero/gear actual-source harness: {Assert.Checks} assertions passed. Unity adapters used; no engine verification.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void TestReadinessEdges()
    {
        var owner = new GameObject("ReadinessHero");
        var manager = owner.AddComponent<HeroProgressionManager>();
        var config = ScriptableObject.CreateInstance<LevelingConfig>();
        typeof(HeroProgressionManager).GetField("levelingConfig",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,config);
        try
        {
            manager.SetCurrency(int.MaxValue-2); manager.AddCurrency(5);
            Assert.AreEqual(int.MaxValue,manager.Currency,"Currency cannot wrap to a negative balance");
            Assert.IsTrue(manager.TrySpendCurrency(5));
            Assert.AreEqual(int.MaxValue-5,manager.Currency);
            HeroProgressionManager.ConfigureRuntimeCosmeticProgressionEconomy(true);
            manager.GrantCosmeticXp(int.MaxValue); manager.GrantCosmeticXp(1);
            Assert.AreEqual(int.MaxValue,manager.GetCosmeticXp(),"Cosmetic funds saturate");
            var gear=ScriptableObject.CreateInstance<GearSetData>(); gear.setId="legacy";gear.displayName="Legacy Gear";
            manager.RegisterGearSet(gear); manager.EnsureHero("legacyHero");
            manager.GetState("legacyHero").equippedGearSetId=gear.setId;
            var snapshot=manager.CaptureGearSnapshot();
            Assert.AreEqual(1,snapshot.instances.Count,"Legacy migrated gear is captured in same generation");
            Assert.AreEqual(snapshot.instances[0].instanceId,snapshot.equippedInstanceIds[0]);
            snapshot.heroIds.Add("secondHero"); snapshot.equippedInstanceIds.Add(snapshot.instances[0].instanceId);
            manager.ApplyGearSnapshot(snapshot);
            Assert.IsNotNull(manager.GetEquippedGearInstance("legacyHero"));
            Assert.IsNull(manager.GetEquippedGearInstance("secondHero"),"One physical item cannot equip twice after load");
            config.baseXpToLevel=100; config.xpPerLevelIncrement=50; config.maxLevel=3;
            Assert.AreEqual(250,config.GetTotalXpForLevel(int.MaxValue),"Out-of-range levels cap without looping");
            Assert.AreEqual(100,config.GetXpToNextLevel(int.MinValue));
            config.baseXpToLevel=int.MaxValue;config.xpPerLevelIncrement=int.MaxValue;config.maxLevel=int.MaxValue;
            Assert.AreEqual(int.MaxValue,config.GetXpToNextLevel(100));
            Assert.AreEqual(int.MaxValue,config.GetTotalXpForLevel(int.MaxValue));
            config.reserveXpMultiplier=float.NaN;
            Assert.IsFalse(config.IsValid(),"Invalid reserve multiplier rejected");
            config.reserveXpMultiplier=.5f;config.baseXpToLevel=100;config.xpPerLevelIncrement=50;config.maxLevel=20;
            NewGamePlusService.Instance=new NewGamePlusService { XpMultiplier=1.25f };
            var hero=ScriptableObject.CreateInstance<HeroData>();hero.heroId="ngHero";
            manager.GrantBattleXp(40,new[]{hero},Array.Empty<HeroData>());
            Assert.AreEqual(50,manager.GetXp(hero.heroId),"NG+ XP scaling applied once");
            DifficultyModeService.Instance=new DifficultyModeService { XpMultiplier=1.5f, CurrencyMultiplier=0 };
            int currencyBefore=manager.Currency;
            var hardcoreHero=ScriptableObject.CreateInstance<HeroData>(); hardcoreHero.heroId="hardcoreHero";
            manager.GrantBattleXp(40,new[]{hardcoreHero},Array.Empty<HeroData>());
            Assert.AreEqual(75,manager.GetXp(hardcoreHero.heroId),"Difficulty and NG+ multiply once before distribution");
            Assert.AreEqual(currencyBefore,manager.Currency,"Hardcore currency suppression honored");
            DifficultyModeService.Instance.XpMultiplier=float.NaN;
            var invalidHero=ScriptableObject.CreateInstance<HeroData>(); invalidHero.heroId="invalidMultiplierHero";
            manager.GrantBattleXp(40,new[]{invalidHero},Array.Empty<HeroData>());
            Assert.AreEqual(40,manager.GetXp(invalidHero.heroId),"Non-finite authored XP multiplier safely defaults");
            DifficultyModeService.Instance=null; NewGamePlusService.Instance=null;
            config.reserveXpMultiplier=1f;
            var reserveHero=ScriptableObject.CreateInstance<HeroData>(); reserveHero.heroId="extremeReserve";
            manager.GrantBattleXp(int.MaxValue,Array.Empty<HeroData>(),new[]{reserveHero});
            Assert.AreEqual(20,manager.GetLevel(reserveHero.heroId),"Extreme full reserve XP still reaches cap without float overflow");
            var extremeGear=manager.CreateGearInstance(gear);
            manager.EquipGearInstance(reserveHero.heroId,extremeGear);
            manager.GrantGearXpToAllEquipped(int.MaxValue,Array.Empty<HeroData>(),new[]{reserveHero});
            Assert.AreEqual(extremeGear.MaxLevel,extremeGear.level,"Extreme direct reserve gear award does not wrap");
            manager.MaxOutHeroForDebug(hero.heroId);
            Assert.IsTrue(manager.TryGrantXp(hero.heroId,10,out bool leveled),"Capped hero reward is an applied no-op");
            Assert.IsFalse(leveled);
        }
        finally
        {
            NewGamePlusService.Instance=null;
            DifficultyModeService.Instance=null;
            HeroProgressionManager.ConfigureRuntimeCosmeticProgressionEconomy(false);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    private static void TestPartyRestoreEdges()
    {
        var owner=new GameObject("PartyRestore");
        var manager=owner.AddComponent<HeroProgressionManager>();
        var party=ScriptableObject.CreateInstance<PartyData>();
        var heroes=new HeroData[5];
        for(int i=0;i<heroes.Length;i++)
        {
            heroes[i]=ScriptableObject.CreateInstance<HeroData>();heroes[i].heroId="party_"+i;
            HeroDatabase.Heroes[heroes[i].heroId]=heroes[i];
        }
        party.activeSlots=new[]{heroes[0],heroes[1],heroes[2]};party.reserveSlots=new[]{heroes[3],heroes[4]};
        PartyManager.Instance=new PartyManager{PartyData=party};
        try
        {
            manager.ApplyPartyCompositionSnapshot(new HeroProgressionManager.PartyCompositionSnapshot {
                activeHeroIds=new List<string>{"unknown",heroes[0].heroId,heroes[1].heroId},
                reserveHeroIds=new List<string>{heroes[3].heroId,heroes[4].heroId}
            });
            var seen=new HashSet<string>();
            foreach(var hero in party.GetAllHeroes()) Assert.IsTrue(seen.Add(hero.heroId),"Unknown fallback must not duplicate a saved hero");
            Assert.AreEqual(heroes[0],party.activeSlots[1]);
            Assert.AreEqual(3,party.activeSlots.Length);Assert.AreEqual(2,party.reserveSlots.Length);
            manager.ApplyPartyCompositionSnapshot(new HeroProgressionManager.PartyCompositionSnapshot {
                activeHeroIds=new List<string>{heroes[0].heroId,null,heroes[2].heroId},
                reserveHeroIds=new List<string>{heroes[0].heroId,heroes[4].heroId}
            });
            Assert.IsNull(party.activeSlots[1],"Explicit empty slot survives restore");
            seen.Clear();foreach(var hero in party.GetAllHeroes()) Assert.IsTrue(seen.Add(hero.heroId),"Combined active/reserve roster remains unique");
            var snapshot=manager.CapturePartyCompositionSnapshot();
            Assert.AreEqual(3,snapshot.activeHeroIds.Count);Assert.AreEqual(2,snapshot.reserveHeroIds.Count);
            Assert.IsTrue(string.IsNullOrEmpty(snapshot.activeHeroIds[1]));
            party.activeSlots=null;party.reserveSlots=null;
            manager.ApplyPartyCompositionSnapshot(snapshot);
            Assert.AreEqual(3,party.activeSlots.Length);Assert.AreEqual(2,party.reserveSlots.Length);
            Assert.IsNull(party.activeSlots[1]);
            manager.ApplyPartyCompositionSnapshot(new HeroProgressionManager.PartyCompositionSnapshot {
                activeHeroIds=new List<string>(),reserveHeroIds=new List<string>()
            });
            Assert.AreEqual(0,party.GetAllHeroes().Length,"Explicit empty roster is not filled from current session");
        }
        finally { PartyManager.Instance=null;HeroDatabase.Heroes.Clear();UnityEngine.Object.DestroyImmediate(owner); }
    }
}
