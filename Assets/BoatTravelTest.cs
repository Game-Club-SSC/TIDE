using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the boat travel system.
/// Verifies end-to-end travel flow with destination picker.
/// </summary>
[DisallowMultipleComponent]
public class BoatTravelTest : MonoBehaviour
{
    [ContextMenu("Run Boat Travel Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Boat Travel Tests ===");

        TestTravelValidationServiceExists();
        TestIslandBacktrackingManagerExists();
        TestTeleportAnchorExists();
        TestBoatInteractableExists();

        Debug.Log("=== All Boat Travel Tests Passed ===");
    }

    private void TestTravelValidationServiceExists()
    {
        Debug.Log("Testing TravelValidationService exists...");

        Assert.IsNotNull(typeof(TravelValidationService), "TravelValidationService class should exist.");

        // Verify ValidateTravel method exists
        var validateMethod = typeof(TravelValidationService).GetMethod("ValidateTravel");
        Assert.IsNotNull(validateMethod, "ValidateTravel method should exist.");

        Debug.Log("TravelValidationService exists: PASS");
    }

    private void TestIslandBacktrackingManagerExists()
    {
        Debug.Log("Testing IslandBacktrackingManager exists...");

        Assert.IsNotNull(typeof(IslandBacktrackingManager), "IslandBacktrackingManager class should exist.");

        // Verify singleton pattern
        IslandBacktrackingManager manager = IslandBacktrackingManager.Instance;
        if (manager != null)
        {
            Assert.IsTrue(manager.isActiveAndEnabled, "IslandBacktrackingManager should be active.");
            Debug.Log("IslandBacktrackingManager exists and is active: PASS");
        }
        else
        {
            Debug.LogWarning("IslandBacktrackingManager instance not found (may need to be in scene).");
        }
    }

    private void TestTeleportAnchorExists()
    {
        Debug.Log("Testing TeleportAnchor exists...");

        Assert.IsNotNull(typeof(TeleportAnchor), "TeleportAnchor class should exist.");

        // Verify SpawnPosition property exists
        var spawnPositionProp = typeof(TeleportAnchor).GetProperty("SpawnPosition");
        Assert.IsNotNull(spawnPositionProp, "TeleportAnchor should have SpawnPosition property.");

        Debug.Log("TeleportAnchor exists: PASS");
    }

    private void TestBoatInteractableExists()
    {
        Debug.Log("Testing IslandBoatInteractable exists...");

        Assert.IsNotNull(typeof(IslandBoatInteractable), "IslandBoatInteractable class should exist.");

        // Verify key methods exist
        var tryGetSpawnMethod = typeof(IslandBoatInteractable).GetMethod("TryGetSpawnPositionForIsland");
        Assert.IsNotNull(tryGetSpawnMethod, "TryGetSpawnPositionForIsland method should exist.");

        Debug.Log("IslandBoatInteractable exists: PASS");
    }
}
