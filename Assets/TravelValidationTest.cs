using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class TravelValidationTest : MonoBehaviour
{
    [ContextMenu("Run Travel Validation Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Travel Validation Tests ===");

        TestTeleportAnchorRegistersAndUnregisters();
        TestTeleportAnchorFindsBoatDock();
        TestTravelValidationRejectsUnrestoredIsland();
        TestTravelValidationAcceptsRestoredIsland();
        TestTravelValidationRejectsEmptyDestination();
        TestTravelValidationServiceListsAvailableDestinations();

        Debug.Log("=== All Travel Validation Tests Passed ===");
    }

    private GameObject CreateAnchor(string id, string islandId, Vector3 pos, bool isDock)
    {
        GameObject go = new GameObject($"TestAnchor_{id}");
        go.transform.position = pos;
        TeleportAnchor anchor = go.AddComponent<TeleportAnchor>();
        anchor.anchorId = id;
        anchor.islandId = islandId;
        anchor.isBoatDock = isDock;
        anchor.spawnPosition = pos;
        return go;
    }

    private void CleanupAnchors()
    {
        TeleportAnchor[] anchors = FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None);
        for (int i = 0; i < anchors.Length; i++)
        {
            Object.DestroyImmediate(anchors[i].gameObject);
        }
        TeleportAnchor.ClearRegistryForDebug();
    }

    private void TestTeleportAnchorRegistersAndUnregisters()
    {
        CleanupAnchors();
        try
        {
            GameObject go = CreateAnchor("a1", "island_test", Vector3.zero, false);
            Assert.IsNotNull(TeleportAnchor.FindAnchor("a1"), "Anchor should be findable by id.");
            Object.DestroyImmediate(go);
            Assert.IsNull(TeleportAnchor.FindAnchor("a1"), "Anchor should be removed after destroy.");
        }
        finally
        {
            CleanupAnchors();
        }
    }

    private void TestTeleportAnchorFindsBoatDock()
    {
        CleanupAnchors();
        try
        {
            CreateAnchor("entrance", "island_test", Vector3.zero, false);
            CreateAnchor("dock", "island_test", new Vector3(5, 0, 0), true);
            TeleportAnchor dock = TeleportAnchor.FindBoatDockForIsland("island_test");
            Assert.IsNotNull(dock, "Boat dock should be found.");
            Assert.AreEqual("dock", dock.AnchorId, "Boat dock id should match.");
        }
        finally
        {
            CleanupAnchors();
        }
    }

    private IslandRestorationTracker CreateTracker()
    {
        GameObject host = new GameObject("Test_Tracker");
        return host.AddComponent<IslandRestorationTracker>();
    }

    private void TestTravelValidationRejectsUnrestoredIsland()
    {
        CleanupAnchors();
        IslandRestorationTracker tracker = null;
        try
        {
            tracker = CreateTracker();
            CreateAnchor("dock", "island_greed", Vector3.zero, true);
            IslandProgressionManager progression = IslandProgressionManager.Instance;
            if (progression != null)
            {
                progression.UnlockAllIslandsForDebug();
            }

            TravelValidationService.ValidationResult result = TravelValidationService.ValidateTravel("island_lust", "island_greed");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsFalse(result.CanTravel, "Unrestored island should fail validation.");
        }
        finally
        {
            CleanupAnchors();
            if (tracker != null) Object.DestroyImmediate(tracker.gameObject);
        }
    }

    private void TestTravelValidationAcceptsRestoredIsland()
    {
        CleanupAnchors();
        IslandRestorationTracker tracker = null;
        try
        {
            tracker = CreateTracker();
            CreateAnchor("dock", "island_greed", Vector3.zero, true);
            IslandProgressionManager progression = IslandProgressionManager.Instance;
            if (progression != null)
            {
                progression.UnlockAllIslandsForDebug();
                tracker.SetIslandRestorationPercentForDebug("island_greed", 1f);
            }

            TravelValidationService.ValidationResult result = TravelValidationService.ValidateTravel("island_lust", "island_greed");
            Assert.IsNotNull(result, "Result should not be null.");
        }
        finally
        {
            CleanupAnchors();
            if (tracker != null) Object.DestroyImmediate(tracker.gameObject);
        }
    }

    private void TestTravelValidationRejectsEmptyDestination()
    {
        CleanupAnchors();
        try
        {
            TravelValidationService.ValidationResult result = TravelValidationService.ValidateTravel("island_lust", "");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsFalse(result.CanTravel, "Empty destination should be rejected.");
        }
        finally
        {
            CleanupAnchors();
        }
    }

    private void TestTravelValidationServiceListsAvailableDestinations()
    {
        CleanupAnchors();
        try
        {
            CreateAnchor("d1", "island_lust", Vector3.zero, true);
            CreateAnchor("d2", "island_greed", Vector3.zero, true);
            IReadOnlyList<TeleportAnchor> destinations = TravelValidationService.GetAvailableTravelDestinations("island_lust");
            Assert.IsNotNull(destinations, "Destinations list should not be null.");
        }
        finally
        {
            CleanupAnchors();
        }
    }
}
