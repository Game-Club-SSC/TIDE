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

            HeroProgressionManager.HeroProgressionSnapshot sourceSnapshot =
                sourceManager.GetComponent<HeroProgressionManager>() != null
                    ? HeroProgressionManager.Instance.CapturePartyCompositionSnapshot()
                    : new HeroProgressionManager.PartyCompositionSnapshot();

            Assert.IsNotNull(sourceSnapshot, "Captured party composition snapshot should not be null.");

            targetManager = CreateIsolatedManager("TestGameStateManager_PartyTarget");
            HeroProgressionManager.Instance.ApplyPartyCompositionSnapshot(sourceSnapshot);

            HeroProgressionManager.PartyCompositionSnapshot reloadedSnapshot =
                HeroProgressionManager.Instance.CapturePartyCompositionSnapshot();

            Assert.AreEqual(sourceSnapshot.activeHeroIds.Count, reloadedSnapshot.activeHeroIds.Count,
                "Active hero slot count should survive the round-trip.");
            Assert.AreEqual(sourceSnapshot.reserveHeroIds.Count, reloadedSnapshot.reserveHeroIds.Count,
                "Reserve hero slot count should survive the round-trip.");
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

        try
        {
            manager = CreateIsolatedManager("TestGameStateManager_HeroProgression");
            HeroProgressionManager progression = HeroProgressionManager.Instance;
            Assert.IsNotNull(progression, "HeroProgressionManager should be bootstrapped.");

            progression.EnsureHero("hero_fire");
            progression.GrantXp("hero_fire", 200);
            int levelAfter = progression.GetLevel("hero_fire");
            Assert.GreaterOrEqual(levelAfter, 2, "Granting XP should level the hero up at least once.");

            HeroProgressionManager.HeroProgressionSnapshot snapshot = progression.CaptureHeroProgressionSnapshot();
            Assert.IsTrue(snapshot.heroIds.Contains("hero_fire"),
                "Snapshot should record hero_fire's progression state.");

            int reloadedLevel = snapshot.levels[snapshot.heroIds.IndexOf("hero_fire")];
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

            WorldStateSaveData parsed = JsonUtility.FromJson<WorldStateSaveData>(legacyJson);
            Assert.IsNotNull(parsed, "Legacy save JSON should still deserialize cleanly with new fields.");
            Assert.IsNull(parsed.heroProgression,
                "New heroProgression field should default to null on legacy save data.");
            Assert.IsNull(parsed.partyComposition,
                "New partyComposition field should default to null on legacy save data.");
        }
        finally
        {
            Cleanup();
        }
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        return manager;
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

        if (PartyManager.Instance != null)
        {
            DestroyImmediate(PartyManager.Instance.gameObject);
        }
    }

    private sealed class DontDestroyMe : MonoBehaviour
    {
    }
}
