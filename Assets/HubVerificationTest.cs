using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Edit Mode tests for the central hub island (issue #293).
/// Verifies the canonical hub id, progression independence, hub travel
/// gating (locked vs unlocked), the hub boat destination list, and the
/// hubState save/load round-trip.
/// </summary>
public class HubVerificationTest
{
    private GameObject progressionObject;
    private GameObject trackerObject;
    private readonly List<GameObject> anchorObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        CleanupSingletons();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupSingletons();
    }

    private void CleanupSingletons()
    {
        if (IslandProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        if (GameStateManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (DialogueSystem.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(DialogueSystem.Instance.gameObject);
        }

        for (int i = 0; i < anchorObjects.Count; i++)
        {
            if (anchorObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(anchorObjects[i]);
            }
        }

        anchorObjects.Clear();

        if (progressionObject != null)
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            progressionObject = null;
        }

        if (trackerObject != null)
        {
            UnityEngine.Object.DestroyImmediate(trackerObject);
            trackerObject = null;
        }
    }

    [Test]
    public void HubIslandIdIsKnownAndHasConfig()
    {
        Assert.AreEqual("island_hub", IslandThemeRegistry.HubIslandId, "Canonical hub id should be island_hub.");
        Assert.IsTrue(IslandThemeRegistry.IsKnownIslandId(IslandThemeRegistry.HubIslandId),
            "The hub island id must be a known island id (backed by an IslandConfig).");
        Assert.IsTrue(IslandThemeRegistry.IsHubIslandId("island_hub"), "IsHubIslandId should resolve island_hub.");
        Assert.IsFalse(IslandThemeRegistry.IsHubIslandId("island_lust"), "IsHubIslandId must reject non-hub ids.");

        IslandConfig config = IslandThemeRegistry.GetConfig(IslandThemeRegistry.HubIslandId);
        Assert.IsNotNull(config, "A hub IslandConfig asset must exist in Resources/Islands.");
        Assert.AreEqual("island_hub", config.islandId, "Hub config islandId should match the canonical hub id.");
        Assert.IsFalse(string.IsNullOrEmpty(config.viceName), "Hub config should have a display name.");
    }

    [Test]
    public void HubIslandIsNotInProgressionOrder()
    {
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            Assert.AreNotEqual(IslandThemeRegistry.HubIslandId, progressionOrder[i],
                "The hub must not appear in ProgressionOrder: it is not a corrupted island.");
        }

        Assert.AreEqual(6, progressionOrder.Count, "Progression order should remain the six corrupted islands.");
    }

    [Test]
    public void HubIsAlwaysUnlockedIndependentOfProgression()
    {
        IslandProgressionManager progression = CreateIsolatedProgressionManager();

        // Fresh progression only unlocks the first corrupted island.
        Assert.IsTrue(progression.IsIslandUnlocked("island_lust"),
            "First corrupted island should be unlocked by default.");
        Assert.IsFalse(progression.IsIslandUnlocked("island_anger"),
            "A not-yet-unlocked corrupted island should remain locked.");

        // The hub never requires an unlock.
        Assert.IsTrue(progression.IsIslandUnlocked(IslandThemeRegistry.HubIslandId),
            "Hub travel must not require unlock progression.");

        // Setting the hub as the active travel island must be allowed.
        Assert.IsTrue(progression.TrySetActiveIslandForTravel(IslandThemeRegistry.HubIslandId),
            "Travel to the hub should be accepted regardless of unlock state.");
        Assert.AreEqual(IslandThemeRegistry.HubIslandId, progression.ActiveIslandId,
            "Active island should become the hub after hub travel.");

        // Locked corrupted islands remain blocked.
        Assert.IsFalse(progression.TrySetActiveIslandForTravel("island_anger"),
            "Travel to a locked corrupted island should be rejected.");
    }

    [Test]
    public void HubTravelValidationBypassesUnlockGate()
    {
        // No progression manager present: hub travel still validates because the
        // hub is always reachable; corrupted islands require the manager.
        TravelValidationService.ValidationResult hubResult =
            TravelValidationService.ValidateTravel("island_lust", IslandThemeRegistry.HubIslandId);
        Assert.IsTrue(hubResult.CanTravel, "Travel to the hub must always be allowed.");

        TravelValidationService.ValidationResult islandResult =
            TravelValidationService.ValidateTravel("island_lust", "island_greed");
        Assert.IsFalse(islandResult.CanTravel,
            "Corrupted island travel must fail without an initialized progression manager.");

        // With a progression manager: unlocked + docked island passes, locked fails.
        IslandProgressionManager progression = CreateIsolatedProgressionManager();
        CreateBoatDock("island_greed", new Vector3(20f, 31.54f, 5f));
        Assert.IsTrue(progression.TryUnlockIsland("island_greed"), "Test setup should unlock island_greed.");

        hubResult = TravelValidationService.ValidateTravel("island_lust", IslandThemeRegistry.HubIslandId);
        Assert.IsTrue(hubResult.CanTravel, "Hub travel must stay allowed with a progression manager present.");

        TravelValidationService.ValidationResult unlockedResult =
            TravelValidationService.ValidateTravel("island_lust", "island_greed");
        Assert.IsTrue(unlockedResult.CanTravel,
            "Travel to an unlocked, docked corrupted island should succeed.");
        Assert.IsNotNull(unlockedResult.Destination, "Docked destination should resolve a TeleportAnchor.");

        TravelValidationService.ValidationResult lockedResult =
            TravelValidationService.ValidateTravel("island_lust", "island_anger");
        Assert.IsFalse(lockedResult.CanTravel,
            "Travel to a locked corrupted island should be rejected even with the manager present.");
        StringAssert.Contains("not yet unlocked", lockedResult.FailureReason);
    }

    [Test]
    public void HubIsAutomaticallyABoatDestination()
    {
        IslandBoatInteractable boat = CreateBoat("TestBoat_WithHub");
        InvokePrivate(boat, "EnsureDestinationList");

        List<string> destinationIds = GetPrivateField<List<string>>(boat, "orderedDestinationIds");
        Assert.IsNotNull(destinationIds, "Boat destination list should exist.");
        Assert.Contains(IslandThemeRegistry.HubIslandId, destinationIds,
            "Island boats must automatically list the hub as a return destination.");

        // A boat that opts out (e.g. the hub's own boat) must not list the hub.
        IslandBoatInteractable hubBoat = CreateBoat("TestBoat_HubOptOut");
        SetPrivateField(hubBoat, "includeHubDestination", false);
        InvokePrivate(hubBoat, "EnsureDestinationList");

        List<string> hubDestinationIds = GetPrivateField<List<string>>(hubBoat, "orderedDestinationIds");
        Assert.IsNotNull(hubDestinationIds, "Hub boat destination list should exist.");
        Assert.IsFalse(hubDestinationIds.Contains(IslandThemeRegistry.HubIslandId),
            "The hub's own boat must not list the hub as a destination.");
    }

    [Test]
    public void HubStateSaveSectionRoundTrips()
    {
        GameObject managerObject = new GameObject("TestGameStateManager_Hub");
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        SetPrivateField(manager, "enablePersistentSaveData", false);
        try
        {
            Vector3 hubLocation = new Vector3(16.5f, 31.54f, 2.69f);
            manager.RecordHubReturnPosition(hubLocation);

            Assert.IsTrue(manager.TryGetHubReturnPosition(out Vector3 captured),
                "Recorded hub return position should be retrievable.");
            Assert.AreEqual(hubLocation, captured, "Hub return position should round-trip through memory.");

            // Serialize the hubState section exactly like SaveWorldState does.
            GameStateManager.HubStateSaveData saveData = (GameStateManager.HubStateSaveData)
                InvokePrivate(manager, "CaptureHubState");
            Assert.IsNotNull(saveData, "CaptureHubState should produce a save payload.");
            Assert.IsTrue(saveData.hasReturnPosition, "Captured hub state should carry the return position.");

            string json = JsonUtility.ToJson(saveData);
            StringAssert.Contains("hasReturnPosition", json, "hubState section should serialize its fields.");
            StringAssert.Contains("returnX", json, "hubState section should serialize coordinates.");

            GameStateManager.HubStateSaveData restored =
                JsonUtility.FromJson<GameStateManager.HubStateSaveData>(json);
            Assert.IsNotNull(restored, "hubState JSON should deserialize.");
            Assert.IsTrue(restored.hasReturnPosition, "Restored hub state should flag the return position.");
            Assert.AreEqual(hubLocation.x, restored.returnX, 0.001f, "Restored X should match.");
            Assert.AreEqual(hubLocation.y, restored.returnY, 0.001f, "Restored Y should match.");
            Assert.AreEqual(hubLocation.z, restored.returnZ, 0.001f, "Restored Z should match.");

            // Wipe runtime state, then apply the restored payload.
            manager.RecordHubReturnPosition(Vector3.zero);
            Assert.IsTrue(manager.TryGetHubReturnPosition(out _),
                "Zero is still a valid finite position; keep the check realistic.");
            InvokePrivate(manager, "ApplyHubState", restored);
            Assert.IsTrue(manager.TryGetHubReturnPosition(out Vector3 applied),
                "Applied hub state should restore the return position.");
            Assert.AreEqual(hubLocation, applied, "Hub return position should survive the save/load round-trip.");
        }
        finally
        {
            if (managerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }
    }

    [Test]
    public void LegacySaveJsonWithoutHubStateStillLoads()
    {
        // Saves written before the hub feature have no hubState section.
        // JsonUtility.ToJson always emits the field (empty object for null), so
        // build a legacy payload that genuinely lacks the key.
        const string legacyJson = "{\"puzzleStates\":[],\"ceremonyIntroCompleted\":false}";
        Assert.IsFalse(legacyJson.Contains("hubState"), "Legacy save JSON must not contain the hubState section.");

        GameStateManager.WorldStateSaveData parsed =
            JsonUtility.FromJson<GameStateManager.WorldStateSaveData>(legacyJson);
        Assert.IsNotNull(parsed, "Legacy save JSON should still deserialize cleanly with the new field.");

        // Unity 6 JsonUtility materializes absent class-typed fields as empty
        // instances, so the backward-compat contract is that no hub return
        // position is restored from a legacy save.
        Assert.IsNotNull(parsed.hubState, "Absent hubState materializes as an empty instance on Unity 6.");
        Assert.IsFalse(parsed.hubState.hasReturnPosition,
            "Legacy saves must restore no hub return position.");

        // Applying the empty state must not fabricate a return position, and a
        // null payload must be a safe no-op.
        GameObject managerObject = new GameObject("TestGameStateManager_LegacyHub");
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        SetPrivateField(manager, "enablePersistentSaveData", false);
        try
        {
            manager.RecordHubReturnPosition(new Vector3(12f, 31.54f, 2.69f));
            InvokePrivate(manager, "ApplyHubState", parsed.hubState);
            Assert.IsFalse(manager.TryGetHubReturnPosition(out _),
                "Applying a legacy (empty) hubState must clear any hub return position.");

            InvokePrivate(manager, "ApplyHubState", (object)null);
            Assert.IsFalse(manager.TryGetHubReturnPosition(out _),
                "Applying a null hubState must be a safe no-op.");
        }
        finally
        {
            if (managerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        // New saves carry the section additively with the return position.
        GameStateManager.WorldStateSaveData fresh = new GameStateManager.WorldStateSaveData
        {
            hubState = new GameStateManager.HubStateSaveData
            {
                hasReturnPosition = true,
                returnX = 12f,
                returnY = 31.54f,
                returnZ = 2.69f
            }
        };
        string freshJson = JsonUtility.ToJson(fresh);
        StringAssert.Contains("hubState", freshJson, "New saves must include the additive hubState section.");
    }

    private IslandProgressionManager CreateIsolatedProgressionManager()
    {
        if (IslandProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        GameObject progressionGameObject = new GameObject("TestProgression_Hub");
        IslandProgressionManager progression = progressionGameObject.AddComponent<IslandProgressionManager>();
        // SendMessage is gated by ShouldRunBehaviour() outside play mode, so
        // invoke the lifecycle callback directly for edit-mode verification.
        MethodInfo onEnable = typeof(IslandProgressionManager).GetMethod("OnEnable",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(onEnable, "IslandProgressionManager.OnEnable should exist.");
        onEnable.Invoke(progression, null);
        Assert.AreSame(progression, IslandProgressionManager.Instance,
            "Progression singleton should reference the isolated test instance.");
        progressionObject = progressionGameObject;
        return progression;
    }

    private void CreateBoatDock(string islandId, Vector3 spawnPosition)
    {
        GameObject dockObject = new GameObject($"TestDock_{islandId}");
        TeleportAnchor anchor = dockObject.AddComponent<TeleportAnchor>();
        anchor.Configure(null, islandId, spawnPosition, true);
        anchorObjects.Add(dockObject);
    }

    private static IslandBoatInteractable CreateBoat(string boatName)
    {
        GameObject boatObject = new GameObject(boatName);
        boatObject.AddComponent<MeshRenderer>();
        boatObject.AddComponent<BoxCollider>();
        return boatObject.AddComponent<IslandBoatInteractable>();
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");
        return method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        return (T)field.GetValue(target);
    }
}

/// <summary>
/// Batchmode entry point (Unity -executeMethod HubVerificationRunner.Run)
/// that exercises the hub verification suite without relying on the
/// EditMode test-runner discovery.
/// </summary>
public static class HubVerificationRunner
{
    public static void Run()
    {
        HubVerificationTest test = new HubVerificationTest();
        test.SetUp();
        try
        {
            test.HubIslandIdIsKnownAndHasConfig();
            test.HubIslandIsNotInProgressionOrder();
            test.HubIsAlwaysUnlockedIndependentOfProgression();
            test.HubTravelValidationBypassesUnlockGate();
            test.HubIsAutomaticallyABoatDestination();
            test.HubStateSaveSectionRoundTrips();
            test.LegacySaveJsonWithoutHubStateStillLoads();
            Debug.Log("[HubVerificationRunner] ALL HUB VERIFICATION TESTS PASSED");
        }
        finally
        {
            test.TearDown();
        }
    }
}
