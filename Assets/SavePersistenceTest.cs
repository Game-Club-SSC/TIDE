#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class SavePersistenceTest : MonoBehaviour
{
    [ContextMenu("Run Save Persistence Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Save Persistence Tests ===");

        TestPartyCompositionRoundTrip();
        TestHeroProgressionRoundTrip();
        TestPersistentSaveDataEnabledByDefault();
        TestExistingWorldStateSaveSchemaExtendsCleanly();
        TestCorruptWorldStateLoadFailsClosed();
        TestLiveSaveBackupRecoversCorruptRuntimeWorldState();
        TestSavedEndingStateRestores();
        TestLiveSaveRejectsMalformedAndFutureSchemas();
        TestLiveSaveRecoversMissingPrimary();
        TestCorruptNewGamePlusFailsClosed();
        TestNewGamePlusRunSnapshotRestoresSavedMode();

        Debug.Log("=== All Save Persistence Tests Passed ===");
    }

    private void TestPartyCompositionRoundTrip()
    {
        GameStateManager manager = null;
        GameObject partyObject = null;
        PartyData party = null;
        try
        {
            manager = CreateIsolatedManager("TestGameStateManager_PartyRoundTrip");
            HeroDatabase database = Resources.Load<HeroDatabase>("HeroDatabase");
            Assert.IsNotNull(database, "Authored hero database must be available for restore resolution.");
            party = ScriptableObject.CreateInstance<PartyData>();
            party.activeSlots = new[] { database.GetHero("hero_water"), database.GetHero("hero_fire"), database.GetHero("hero_space") };
            party.reserveSlots = new[] { database.GetHero("hero_air"), database.GetHero("hero_earth") };
            foreach (HeroData hero in party.GetAllHeroes()) Assert.IsNotNull(hero);
            Assert.AreEqual(5, party.GetAllHeroes().Length, "The fixture must contain all five real heroes.");
            partyObject = new GameObject("TestParty_SaveRoundTrip");
            PartyManager partyManager = partyObject.AddComponent<PartyManager>();
            SetAutoPropertyBackingField(typeof(PartyManager), "Instance", partyManager);
            partyManager.Initialize(party, database);

            manager.SaveWorldState();
            Assert.IsTrue(partyManager.SwapActiveReserve(0, 0), "Fixture mutation must alter the active party.");
            Assert.AreEqual("hero_air", party.activeSlots[0].heroId);
            manager.LoadWorldState();

            CollectionAssert.AreEqual(new[] { "hero_water", "hero_fire", "hero_space" },
                HeroProgressionManager.Instance.CapturePartyCompositionSnapshot().activeHeroIds,
                "Loading the world save restores every active identity and ordering.");
            CollectionAssert.AreEqual(new[] { "hero_air", "hero_earth" },
                HeroProgressionManager.Instance.CapturePartyCompositionSnapshot().reserveHeroIds,
                "Loading the world save restores every reserve identity and ordering.");
        }
        finally
        {
            if (manager != null) DestroyImmediate(manager.gameObject);
            if (partyObject != null) DestroyImmediate(partyObject);
            if (party != null) DestroyImmediate(party);
            Cleanup();
        }
    }

    private void TestHeroProgressionRoundTrip()
    {
        Debug.Log("Testing hero XP and level round-trip...");

        GameStateManager manager = null;
        LevelingConfig levelingConfig = null;

        try
        {
            manager = CreateIsolatedManager("TestGameStateManager_HeroProgression");
            HeroProgressionManager progression = HeroProgressionManager.Instance;
            Assert.IsNotNull(progression, "HeroProgressionManager should be bootstrapped.");

            levelingConfig = ScriptableObject.CreateInstance<LevelingConfig>();
            FieldInfo configField = typeof(HeroProgressionManager).GetField(
                "levelingConfig", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(configField, "HeroProgressionManager levelingConfig field should exist.");
            configField.SetValue(progression, levelingConfig);

            progression.EnsureHero("hero_fire");
            progression.GrantXp("hero_fire", 200);
            int levelAfter = progression.GetLevel("hero_fire");
            Assert.GreaterOrEqual(levelAfter, 2, "Granting XP should level the hero up at least once.");

            HeroProgressionManager.HeroProgressionSnapshot snapshot = progression.CaptureHeroProgressionSnapshot();
            Assert.IsTrue(snapshot.heroIds.Contains("hero_fire"),
                "Snapshot should record hero_fire's progression state.");

            int fireIndex = snapshot.heroIds.IndexOf("hero_fire");
            Assert.IsTrue(fireIndex >= 0, "hero_fire should be findable in snapshot heroIds.");
            int reloadedLevel = snapshot.levels[fireIndex];
            Assert.AreEqual(levelAfter, reloadedLevel, "Snapshot should preserve the captured level.");

            int xpAfter = progression.GetXp("hero_fire");
            manager.SaveWorldState();
            progression.ResetProgressionForDebug();
            Assert.AreEqual(1, progression.GetLevel("hero_fire"), "Fixture reset must erase the gained levels.");
            manager.LoadWorldState();
            Assert.AreEqual(levelAfter, progression.GetLevel("hero_fire"),
                "Loading the serialized world snapshot restores the hero level.");
            Assert.AreEqual(xpAfter, progression.GetXp("hero_fire"),
                "Loading also restores partial XP toward the next level.");
        }
        finally
        {
            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            if (levelingConfig != null)
            {
                DestroyImmediate(levelingConfig);
            }

            Cleanup();
        }
    }

    private void TestPersistentSaveDataEnabledByDefault()
    {
        Debug.Log("Testing persistent save data is enabled by default...");

        GameStateManager manager = null;
        try
        {
            manager = CreateIsolatedManager("TestGameStateManager_DefaultSaveData");

            FieldInfo field = typeof(GameStateManager).GetField(
                "enablePersistentSaveData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "enablePersistentSaveData field should exist on GameStateManager.");
            bool value = (bool)field.GetValue(manager);
            Assert.IsTrue(value,
                "Issue #225 requires persistent save data to be enabled by default.");
        }
        finally
        {
            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            Cleanup();
        }
    }

    private void TestExistingWorldStateSaveSchemaExtendsCleanly()
    {
        Debug.Log("Testing new save fields extend the schema without breaking existing fields...");

        try
        {
            string legacyJson = "{\"puzzleStates\":[],\"ancientTextStates\":[],\"completedNarrativeBeatIds\":[],\"restorationSnapshot\":null,\"gearProgression\":null,\"progressionSnapshot\":null,\"storyProgression\":null}";

            GameStateManager.WorldStateSaveData parsed = JsonUtility.FromJson<GameStateManager.WorldStateSaveData>(legacyJson);
            Assert.IsNotNull(parsed, "Legacy save JSON should still deserialize cleanly with new fields.");
            if (parsed.heroProgression != null)
            {
                Assert.IsTrue(parsed.heroProgression.heroIds == null || parsed.heroProgression.heroIds.Count == 0,
                    "Legacy save data should not invent hero progression entries.");
            }

            if (parsed.partyComposition != null)
            {
                Assert.IsTrue(parsed.partyComposition.activeHeroIds == null || parsed.partyComposition.activeHeroIds.Count == 0,
                    "Legacy save data should not invent active party entries.");
                Assert.IsTrue(parsed.partyComposition.reserveHeroIds == null || parsed.partyComposition.reserveHeroIds.Count == 0,
                    "Legacy save data should not invent reserve party entries.");
            }
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestCorruptWorldStateLoadFailsClosed()
    {
        Debug.Log("Testing corrupt world state load fails closed...");

        GameStateManager manager = null;
        try
        {
            manager = CreateIsolatedManager("TestGameStateManager_CorruptLoad");
            PlayerPrefs.SetString("TIDE_WORLD_STATE_V1", "{\"puzzleStates\":[");
            PlayerPrefs.Save();

            Assert.DoesNotThrow(manager.LoadWorldState,
                "A direct load request must not throw when persisted JSON is corrupt.");
            Assert.IsFalse(manager.HasLoadableWorldState(),
                "Corrupt persisted JSON must remain unavailable for loading.");
        }
        finally
        {
            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            Cleanup();
        }
    }

    private void TestLiveSaveBackupRecoversCorruptRuntimeWorldState()
    {
        Debug.Log("Testing live-save backup recovery for a corrupt runtime world state...");

        GameObject serviceObject = null;
        GameStateManager manager = null;
        try
        {
            Cleanup();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            serviceObject = new GameObject("TestWorldSaveService_BackupRecovery");
            WorldSaveService liveSave = serviceObject.AddComponent<WorldSaveService>();
            string validPayload = "{\"puzzleStates\":[],\"ancientTextStates\":[],\"completedNarrativeBeatIds\":[]}";
            Assert.IsTrue(liveSave.TryWriteJson(validPayload), "The initial live save should write.");
            Assert.IsTrue(liveSave.TryWriteJson(validPayload), "The second live save should create a backup.");

            PlayerPrefs.SetString(liveSave.PlayerPrefsKey, "{\"puzzleStates\":[}");
            PlayerPrefs.SetString("TIDE_WORLD_STATE_V1", "{\"puzzleStates\":[}");
            PlayerPrefs.Save();

            GameObject managerObject = new GameObject("TestGameStateManager_BackupRecovery");
            manager = managerObject.AddComponent<GameStateManager>();

            Assert.IsTrue(manager.HasLoadableWorldState(),
                "A valid live-save backup must recover a corrupt runtime world state.");
            Assert.DoesNotThrow(manager.LoadWorldState,
                "Recovery must produce a payload GameStateManager can load.");

            string restoredPayload = PlayerPrefs.GetString("TIDE_WORLD_STATE_V1", string.Empty);
            Assert.IsTrue(restoredPayload.Contains("\"puzzleStates\""),
                "Recovery must repair the runtime world-state key.");
        }
        finally
        {
            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            if (serviceObject != null)
            {
                DestroyImmediate(serviceObject);
            }

            Cleanup();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }

    private void TestSavedEndingStateRestores()
    {
        GameStateManager manager = CreateIsolatedManager("TestGameStateManager_EndingRestore");
        try
        {
            GameStateManager.WorldStateSaveData saved = new GameStateManager.WorldStateSaveData
            {
                storyProgression = new GameStateManager.StoryProgressionSaveData
                {
                    currentAct = (int)GameStateManager.StoryAct.ActIII,
                    highestActReached = (int)GameStateManager.StoryAct.ActIII,
                    endingBranch = GameStateManager.EndingBranch.Good.ToString(),
                    endingTriggered = true
                }
            };
            PlayerPrefs.SetString("TIDE_WORLD_STATE_V1", JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
            manager.LoadWorldState();
            Assert.IsTrue(manager.IsEndingTriggered, "The ending lock must survive serialized state restoration.");
            Assert.AreEqual(GameStateManager.EndingBranch.Good, manager.ResolvedEndingBranch);
            manager.ResetRuntimeWorldStateForDebug();
            Assert.IsFalse(manager.IsEndingTriggered, "Fixture reset must remove the live ending before reload.");
            manager.LoadWorldState();
            Assert.IsTrue(manager.IsEndingTriggered);
            Assert.AreEqual(GameStateManager.EndingBranch.Good, manager.ResolvedEndingBranch);
        }
        finally { DestroyImmediate(manager.gameObject); Cleanup(); }
    }

    private void TestLiveSaveRejectsMalformedAndFutureSchemas()
    {
        Cleanup();
        GameObject host = new GameObject("TestLiveSave_Validation");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.Clear();
            Assert.IsTrue(service.TryWriteJson("{\"puzzleStates\":[]}"));
            string previous = service.ReadJson();
            foreach (string invalid in new[]
            {
                "{\"puzzleStates\":[}", "{\"puzzleStates\":[]} trailing", "{\"puzzleStates\":null}",
                "{\"saveSchemaVersion\":2,\"puzzleStates\":[]}",
                "{\"saveSchemaVersion\":1,\"saveSchemaVersion\":2,\"puzzleStates\":[]}"
            })
            {
                Assert.IsFalse(service.TryWriteJson(invalid), "Malformed and unsupported saves must be rejected.");
                Assert.AreEqual(previous, service.ReadJson(), "Invalid writes cannot damage the valid generation.");
            }
        }
        finally { service.Clear(); DestroyImmediate(host); Cleanup(); }
    }

    private void TestLiveSaveRecoversMissingPrimary()
    {
        Cleanup();
        GameObject host = new GameObject("TestLiveSave_OrphanedBackup");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.Clear();
            const string valid = "{\"puzzleStates\":[]}";
            PlayerPrefs.SetString(service.PlayerPrefsKey + "_backup", valid);
            PlayerPrefs.Save();
            Assert.IsTrue(service.HasPersistedData, "A surviving backup counts as persisted data.");
            Assert.IsTrue(service.TryLoadJson(out string restored), "Missing primary recovers from its valid backup.");
            Assert.AreEqual(valid, restored);
            Assert.AreEqual(valid, PlayerPrefs.GetString(service.PlayerPrefsKey));
        }
        finally { service.Clear(); DestroyImmediate(host); Cleanup(); }
    }

    private void TestCorruptNewGamePlusFailsClosed()
    {
        Cleanup();
        PlayerPrefs.SetString("NewGamePlusService", "{\"completedRuns\":-5,\"loopIndex\":2147483647,\"isInNewGamePlus\":true}");
        GameObject host = new GameObject("TestNGPlus_Corrupt");
        try
        {
            NewGamePlusService service = host.AddComponent<NewGamePlusService>();
            MethodInfo load = typeof(NewGamePlusService).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => load.Invoke(service, null), "Damaged NG+ metadata cannot throw during bootstrap.");
            Assert.AreEqual(0, service.CompletedRuns);
            Assert.AreEqual(1f, service.GetEnemyStatMultiplier());
            Assert.IsFalse(service.IsInNewGamePlus);
        }
        finally { DestroyImmediate(host); PlayerPrefs.DeleteKey("NewGamePlusService"); PlayerPrefs.Save(); Cleanup(); }
    }

    private void TestNewGamePlusRunSnapshotRestoresSavedMode()
    {
        Cleanup();
        PlayerPrefs.DeleteKey("NewGamePlusService");
        GameObject host = new GameObject("TestNGPlus_RunSnapshot");
        try
        {
            NewGamePlusService service = host.AddComponent<NewGamePlusService>();
            service.ResetForDebug();
            service.RegisterCompletion();
            service.RegisterCompletion();
            Assert.AreEqual(1, service.CompletedRuns, "One run's ending cannot count twice.");
            NewGamePlusService.RunSnapshot completedNormalRun = service.CaptureSnapshot();
            Assert.IsTrue(service.StartNewGamePlus());
            Assert.IsFalse(service.StartNewGamePlus(), "Repeated start cannot skip loops.");
            service.ApplySnapshot(completedNormalRun);
            Assert.IsFalse(service.IsInNewGamePlus, "Loading the normal run clears a later NG+ multiplier.");
            Assert.AreEqual(1f, service.GetEnemyStatMultiplier());
            Assert.AreEqual(1, service.CompletedRuns, "Account completion history is retained.");
            service.ApplySnapshot(null);
            Assert.IsFalse(service.IsInNewGamePlus, "Legacy saves cannot inherit the live run's NG+ mode.");
        }
        finally { DestroyImmediate(host); PlayerPrefs.DeleteKey("NewGamePlusService"); PlayerPrefs.Save(); Cleanup(); }
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        HeroProgressionManager progression = managerObject.GetComponent<HeroProgressionManager>();
        if (progression == null) progression = managerObject.AddComponent<HeroProgressionManager>();
        SetAutoPropertyBackingField(typeof(HeroProgressionManager), "Instance", progression);
        return manager;
    }

    private static void SetAutoPropertyBackingField(System.Type type, string propertyName, object value)
    {
        FieldInfo field = type.GetField(
            "<" + propertyName + ">k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{type.Name}.{propertyName} backing field should exist.");
        field.SetValue(null, value);
    }

    private static void Cleanup()
    {
        if (NewGamePlusService.Instance != null) DestroyImmediate(NewGamePlusService.Instance.gameObject);
        if (GameStateManager.Instance != null)
        {
            DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        if (IslandProgressionManager.Instance != null)
        {
            DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (DevCheatService.Instance != null)
        {
            DestroyImmediate(DevCheatService.Instance.gameObject);
        }

        if (EndingEvaluator.Instance != null)
        {
            DestroyImmediate(EndingEvaluator.Instance.gameObject);
        }

        if (AudioManager.Instance != null)
        {
            DestroyImmediate(AudioManager.Instance.gameObject);
        }

        if (WorldSaveService.Instance != null)
        {
            DestroyImmediate(WorldSaveService.Instance.gameObject);
        }

        if (PartyManager.Instance != null)
        {
            DestroyImmediate(PartyManager.Instance.gameObject);
        }
    }

}
#endif
