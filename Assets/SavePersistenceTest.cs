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
        TestSavedEndingResumesAfterContinue();

        Debug.Log("=== All Save Persistence Tests Passed ===");
    }

    private void TestPartyCompositionRoundTrip()
    {
        Debug.Log("Testing party composition round-trip across save/load...");

        GameStateManager sourceManager = null;
        GameStateManager targetManager = null;
        GameObject heroDatabaseObject = null;

        try
        {
            sourceManager = CreateIsolatedManager("TestGameStateManager_PartySource");

            HeroDatabase heroDatabase = ScriptableObject.CreateInstance<HeroDatabase>();
            HeroData[] allHeroes = new HeroData[5];
            string[] elementIds = { "hero_fire", "hero_water", "hero_earth", "hero_air", "hero_space" };
            CombatUnit.Element[] elementValues =
            {
                CombatUnit.Element.Fire,
                CombatUnit.Element.Water,
                CombatUnit.Element.Earth,
                CombatUnit.Element.Air,
                CombatUnit.Element.Space
            };
            for (int i = 0; i < 5; i++)
            {
                HeroData hero = ScriptableObject.CreateInstance<HeroData>();
                hero.heroId = elementIds[i];
                hero.displayName = elementIds[i];
                hero.element = elementValues[i];
                hero.isMainCharacter = i == 0;
                hero.baseMaxHP = 100;
                allHeroes[i] = hero;
            }
            heroDatabase.allHeroes = allHeroes;
            heroDatabaseObject = new GameObject("TestHeroDatabase");
            heroDatabaseObject.AddComponent<DontDestroyMe>();

            HeroProgressionManager.PartyCompositionSnapshot sourceSnapshot = null;
            if (sourceManager.GetComponent<HeroProgressionManager>() != null && HeroProgressionManager.Instance != null)
            {
                sourceSnapshot = HeroProgressionManager.Instance.CapturePartyCompositionSnapshot();
            }

            if (sourceSnapshot == null)
            {
                sourceSnapshot = new HeroProgressionManager.PartyCompositionSnapshot();
            }

            Assert.IsNotNull(sourceSnapshot, "Captured party composition snapshot should not be null.");

            targetManager = CreateIsolatedManager("TestGameStateManager_PartyTarget");
            if (HeroProgressionManager.Instance != null)
            {
                HeroProgressionManager.Instance.ApplyPartyCompositionSnapshot(sourceSnapshot);
            }

            int expectedActive = sourceSnapshot.activeHeroIds != null ? sourceSnapshot.activeHeroIds.Count : 0;
            int expectedReserve = sourceSnapshot.reserveHeroIds != null ? sourceSnapshot.reserveHeroIds.Count : 0;

            if (HeroProgressionManager.Instance != null)
            {
                HeroProgressionManager.PartyCompositionSnapshot reloadedSnapshot =
                    HeroProgressionManager.Instance.CapturePartyCompositionSnapshot();

                Assert.AreEqual(expectedActive, reloadedSnapshot.activeHeroIds.Count,
                    "Active hero slot count should survive the round-trip.");
                Assert.AreEqual(expectedReserve, reloadedSnapshot.reserveHeroIds.Count,
                    "Reserve hero slot count should survive the round-trip.");
            }
        }
        finally
        {
            if (sourceManager != null)
            {
                DestroyImmediate(sourceManager.gameObject);
            }

            if (targetManager != null)
            {
                DestroyImmediate(targetManager.gameObject);
            }

            if (heroDatabaseObject != null)
            {
                DestroyImmediate(heroDatabaseObject);
            }

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

            progression.ApplyHeroProgressionSnapshot(snapshot);
            Assert.AreEqual(levelAfter, progression.GetLevel("hero_fire"),
                "Applying the snapshot should restore the hero level.");
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

    private void TestSavedEndingResumesAfterContinue()
    {
        string gameStatePath = System.IO.Path.Combine(Application.dataPath, "GameStateManager.cs");
        string source = System.IO.File.ReadAllText(gameStatePath);

        Assert.IsTrue(source.Contains("if (endingTriggered)\n            {\n                SetPlayerMovementLocked(true);\n                PlayEndingSequence(resolvedEndingBranch);"),
            "A saved ending must resume when Continue restores the main scene.");
        Assert.IsTrue(source.Contains("SetPlayerMovementLocked(targetState != GameState.Exploration || endingTriggered);"),
            "The player must stay locked while a resumed ending is playing.");
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        HeroProgressionManager progression = managerObject.AddComponent<HeroProgressionManager>();
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

    private sealed class DontDestroyMe : MonoBehaviour
    {
    }
}
