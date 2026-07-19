using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandProgressionTravelTest : MonoBehaviour
{
    [ContextMenu("Run Island Progression + Travel Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Island Progression + Travel Tests ===");

        TestDefaultStateHasStartingIslandUnlocked();
        TestIslandRestorationUnlocksNextDestination();
        TestSnapshotRoundTripPreservesUnlocksAndReturnPositions();
        TestLegacyIslandAliasResolvesToCanonicalIds();
        TestBoatTravelRefusesUnrestoredDestination();
        TestBoatTravelAllowsRestoredDestination();
        TestBoatTravelPipelineRoutesThroughGameStateManager();

        Debug.Log("=== All Island Progression + Travel Tests Passed ===");
    }

    private void TestDefaultStateHasStartingIslandUnlocked()
    {
        Debug.Log("Testing default progression bootstrap state...");

        GameObject trackerObject = null;
        GameObject progressionObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_ProgressionDefault");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_Default");
            progressionObject = progression.gameObject;

            string startingIsland = IslandThemeRegistry.ResolveIslandId(IslandThemeRegistry.DefaultIslandId);
            Assert.AreEqual(startingIsland, progression.ActiveIslandId,
                "Progression manager should start on the default island.");
            Assert.IsTrue(progression.IsIslandUnlocked(startingIsland),
                "Default island should always be unlocked.");

            string[] unlocked = progression.GetUnlockedIslandIds();
            Assert.IsNotNull(unlocked, "Unlocked island list should not be null.");
            Assert.GreaterOrEqual(unlocked.Length, 1, "There should be at least one unlocked island at startup.");

            Debug.Log("  Default progression bootstrap state test passed");
        }
        finally
        {
            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestIslandRestorationUnlocksNextDestination()
    {
        Debug.Log("Testing restoration-driven destination unlock...");

        GameObject trackerObject = null;
        GameObject progressionObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_ProgressionUnlock");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_Unlock");
            progressionObject = progression.gameObject;

            const string currentIsland = "island_lust";
            const string nextIsland = "island_greed";

            int unlockEvents = 0;
            progression.OnIslandUnlocked += islandId =>
            {
                if (islandId == nextIsland)
                {
                    unlockEvents++;
                }
            };

            tracker.RecordEncounterCompletion(currentIsland, "unlock_c1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion(currentIsland, "unlock_p1", EncounterType.Puzzle, 0.5f);

            Assert.IsTrue(tracker.IsIslandRestored(currentIsland),
                "Current island should be marked restored at 100% completion.");
            Assert.IsTrue(progression.IsIslandUnlocked(nextIsland),
                "Restoring the current island should unlock the intended next island.");
            Assert.GreaterOrEqual(unlockEvents, 1,
                "Unlock event should fire when the next island becomes available.");

            Debug.Log("  Restoration-driven destination unlock test passed");
        }
        finally
        {
            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestSnapshotRoundTripPreservesUnlocksAndReturnPositions()
    {
        Debug.Log("Testing progression snapshot round-trip...");

        GameObject trackerObject = null;
        GameObject progressionObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_ProgressionSnapshot");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_Snapshot");
            progressionObject = progression.gameObject;

            const string restoredIsland = "island_lust";
            const string unlockedIsland = "island_greed";
            Vector3 expectedReturnPosition = new Vector3(18.25f, 31.54f, -4.5f);

            tracker.RecordEncounterCompletion(restoredIsland, "snapshot_c1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion(restoredIsland, "snapshot_p1", EncounterType.Puzzle, 0.5f);

            progression.RecordIslandReturnPosition(unlockedIsland, expectedReturnPosition);
            progression.SetActiveIsland(unlockedIsland);

            IslandProgressionManager.ProgressionSnapshot snapshot = progression.CaptureSnapshot();
            Assert.IsNotNull(snapshot, "Captured progression snapshot should not be null.");

            progression.ApplySnapshot(snapshot);

            Assert.AreEqual(unlockedIsland, progression.ActiveIslandId,
                "Active island should survive snapshot round-trip.");
            Assert.IsTrue(progression.IsIslandUnlocked(unlockedIsland),
                "Unlocked destination should survive snapshot round-trip.");

            Assert.IsTrue(progression.TryGetIslandReturnPosition(unlockedIsland, out Vector3 restoredPosition),
                "Return position should survive snapshot round-trip.");
            Assert.AreEqual(expectedReturnPosition.x, restoredPosition.x, 0.0001f,
                "Return position X should be preserved.");
            Assert.AreEqual(expectedReturnPosition.y, restoredPosition.y, 0.0001f,
                "Return position Y should be preserved.");
            Assert.AreEqual(expectedReturnPosition.z, restoredPosition.z, 0.0001f,
                "Return position Z should be preserved.");

            Debug.Log("  Progression snapshot round-trip test passed");
        }
        finally
        {
            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestLegacyIslandAliasResolvesToCanonicalIds()
    {
        Debug.Log("Testing legacy island-id alias resolution...");

        Assert.AreEqual("island_lust", IslandThemeRegistry.ResolveIslandId("island_1"),
            "Legacy alias 'island_1' should resolve to 'island_lust'.");
        Assert.AreEqual("island_anger", IslandThemeRegistry.ResolveIslandId("island_2"),
            "Legacy alias 'island_2' should resolve to 'island_anger'.");
        Assert.IsTrue(IslandThemeRegistry.IsKnownIslandId("island_1"),
            "Legacy alias should be recognized as a known island id.");
        Assert.IsTrue(IslandThemeRegistry.IsKnownIslandId("island_6"),
            "Legacy alias for the final island should be recognized.");

        Debug.Log("  Legacy island-id alias resolution test passed");
    }

    private void TestBoatTravelRefusesUnrestoredDestination()
    {
        Debug.Log("Testing boat travel refuses unrestored destinations...");

        GameStateManager manager = null;
        GameObject trackerObject = null;
        GameObject progressionObject = null;
        GameObject boatObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BoatUnrestored");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_BoatUnrestored");
            progressionObject = progression.gameObject;
            progression.UnlockAllIslandsForDebug();

            manager = CreateIsolatedManager("TestGameStateManager_BoatUnrestored");

            IslandBoatInteractable boat = CreateBoat("TestBoat_Unrestored");
            boatObject = boat.gameObject;

            InvokePrivate(boat, "EnsureDestinationList");
            InvokePrivate(boat, "RefreshDestinationOrder");

            string unrestoredIsland = "island_greed";
            bool canTravel = (bool)InvokePrivate(boat, "IsIslandTravelEligible", progression, unrestoredIsland);
            Assert.IsFalse(canTravel,
                "Boat should refuse to travel to an unrestored destination even when unlocked.");

            Debug.Log("  Boat travel unrestored rejection test passed");
        }
        finally
        {
            if (boatObject != null)
            {
                DestroyImmediate(boatObject);
            }

            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestBoatTravelAllowsRestoredDestination()
    {
        Debug.Log("Testing boat travel allows restored destinations...");

        GameStateManager manager = null;
        GameObject trackerObject = null;
        GameObject progressionObject = null;
        GameObject boatObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BoatRestored");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_BoatRestored");
            progressionObject = progression.gameObject;
            progression.UnlockAllIslandsForDebug();

            string restoredIsland = "island_greed";
            tracker.RecordEncounterCompletion(restoredIsland, "boat_c1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion(restoredIsland, "boat_p1", EncounterType.Puzzle, 0.5f);
            Assert.IsTrue(tracker.IsIslandRestored(restoredIsland),
                "Setup precondition: greed should be marked restored.");

            manager = CreateIsolatedManager("TestGameStateManager_BoatRestored");

            IslandBoatInteractable boat = CreateBoat("TestBoat_Restored");
            boatObject = boat.gameObject;

            bool canTravel = (bool)InvokePrivate(boat, "IsIslandTravelEligible", progression, restoredIsland);
            Assert.IsTrue(canTravel,
                "Boat should allow travel to a restored destination.");

            Debug.Log("  Boat travel restored acceptance test passed");
        }
        finally
        {
            if (boatObject != null)
            {
                DestroyImmediate(boatObject);
            }

            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestBoatTravelPipelineRoutesThroughGameStateManager()
    {
        Debug.Log("Testing GameStateManager.TravelToIsland routes through the fade pipeline...");

        GameStateManager manager = null;
        GameObject trackerObject = null;
        GameObject progressionObject = null;
        GameObject playerObject = null;

        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BoatPipeline");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_BoatPipeline");
            progressionObject = progression.gameObject;
            progression.UnlockAllIslandsForDebug();

            string destinationIsland = "island_greed";
            tracker.RecordEncounterCompletion(destinationIsland, "pipeline_c1", EncounterType.Combat, 0.5f);
            tracker.RecordEncounterCompletion(destinationIsland, "pipeline_p1", EncounterType.Puzzle, 0.5f);

            manager = CreateIsolatedManager("TestGameStateManager_BoatPipeline");

            playerObject = new GameObject("TestPlayerForBoat");
            playerObject.transform.position = Vector3.zero;
            IsometricPlayer player = playerObject.AddComponent<IsometricPlayer>();

            Vector3 destinationSpawn = new Vector3(20f, 31.54f, 5f);
            bool travelInitiated = manager.TravelToIsland(destinationIsland, destinationSpawn);
            Assert.IsTrue(travelInitiated,
                "GameStateManager.TravelToIsland should succeed for an unlocked + restored destination.");
            Assert.IsTrue(manager.IsTransitioning,
                "TravelToIsland should mark the manager as transitioning for the fade pipeline.");

            Debug.Log("  Boat travel fade pipeline routing test passed");
        }
        finally
        {
            if (playerObject != null)
            {
                DestroyImmediate(playerObject);
            }

            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            if (progressionObject != null)
            {
                DestroyImmediate(progressionObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private static IslandBoatInteractable CreateBoat(string boatName)
    {
        GameObject boatObject = new GameObject(boatName);
        boatObject.AddComponent<MeshRenderer>();
        boatObject.AddComponent<BoxCollider>();
        IslandBoatInteractable boat = boatObject.AddComponent<IslandBoatInteractable>();
        return boat;
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        SetPrivateField(manager, "enablePersistentSaveData", false);
        return manager;
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        System.Reflection.MethodInfo method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");
        return method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        field.SetValue(target, value);
    }

    private static IslandRestorationTracker CreateIsolatedTracker(string trackerName)
    {
        if (IslandProgressionManager.Instance != null)
        {
            DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        GameObject trackerObject = new GameObject(trackerName);
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        trackerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference isolated test tracker instance.");
        return tracker;
    }

    private static IslandProgressionManager CreateIsolatedProgressionManager(string progressionName)
    {
        if (IslandProgressionManager.Instance != null)
        {
            DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        GameObject progressionObject = new GameObject(progressionName);
        IslandProgressionManager progression = progressionObject.AddComponent<IslandProgressionManager>();
        progressionObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(progression, IslandProgressionManager.Instance,
            "Progression singleton should reference isolated test progression instance.");
        return progression;
    }
}
