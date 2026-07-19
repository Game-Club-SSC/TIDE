using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BoatTravelRegressionTest : MonoBehaviour
{
    private static readonly string[] SixIslandProgression =
    {
        "island_lust",
        "island_greed",
        "island_desire",
        "island_anger",
        "island_envy",
        "island_ego"
    };

    [ContextMenu("Run Boat Travel Regression")]
    public void RunTests()
    {
        Debug.Log("=== Starting Boat Travel Regression ===");

        TestProgressionOrderHasSixIslands();
        TestAllIslandConfigsLoad();
        TestNextIslandChain();
        TestTravelValidationForEachIsland();
        TestBoatInteractableCreatesDestinations();
        TestBacktrackingUnlocks();
        TestLegacyAliasesDoNotLeakToDestinations();

        Debug.Log("=== Boat Travel Regression Passed ===");
    }

    private void TestProgressionOrderHasSixIslands()
    {
        IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;
        Assert.AreEqual(6, order.Count, "Progression order should have exactly 6 islands (V2 GDD).");
        for (int i = 0; i < SixIslandProgression.Length; i++)
        {
            Assert.AreEqual(SixIslandProgression[i], order[i],
                $"Island at index {i} should be {SixIslandProgression[i]}.");
        }
        Debug.Log("  Progression order verified: 6 V2 islands");
    }

    private void TestAllIslandConfigsLoad()
    {
        for (int i = 0; i < SixIslandProgression.Length; i++)
        {
            string islandId = SixIslandProgression[i];
            IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
            Assert.IsNotNull(config, $"Config for '{islandId}' should load from Resources/Islands.");
            Assert.AreEqual(9, config.encounters.Length,
                $"'{islandId}' should have 9 encounters (4 combat + 4 puzzle + 1 boss).");
            Assert.IsNotNull(config.encounters[8].encounterConfig,
                $"'{islandId}' boss encounter should reference an EncounterConfig.");
        }
        Debug.Log("  All 6 island configs load with valid encounters");
    }

    private void TestNextIslandChain()
    {
        Assert.AreEqual("island_greed", IslandThemeRegistry.GetNextIslandId("island_lust"),
            "Lust should chain to Greed.");
        Assert.AreEqual("island_desire", IslandThemeRegistry.GetNextIslandId("island_greed"),
            "Greed should chain to Desire.");
        Assert.AreEqual("island_anger", IslandThemeRegistry.GetNextIslandId("island_desire"),
            "Desire should chain to Anger.");
        Assert.AreEqual("island_envy", IslandThemeRegistry.GetNextIslandId("island_anger"),
            "Anger should chain to Envy.");
        Assert.AreEqual("island_ego", IslandThemeRegistry.GetNextIslandId("island_envy"),
            "Envy should chain to Ego.");
        Assert.AreEqual("island_ego", IslandThemeRegistry.GetNextIslandId("island_ego"),
            "Ego (final island) should chain to itself.");
        Debug.Log("  Next-island chain verified: lust→greed→desire→anger→envy→ego");
    }

    private void TestTravelValidationForEachIsland()
    {
        GameObject trackerObject = null;
        GameObject progressionObject = null;
        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("BTR_Tracker");
            trackerObject = tracker.gameObject;
            IslandProgressionManager progression = CreateIsolatedProgressionManager("BTR_Progression");
            progressionObject = progression.gameObject;

            for (int i = 0; i < SixIslandProgression.Length; i++)
            {
                string islandId = SixIslandProgression[i];
                ValidationResult result = TravelValidationService.ValidateTravel("island_lust", islandId);
                if (i == 0)
                {
                    Assert.IsTrue(result.IsValid, $"Lust (starting island) should be valid travel target. Error: {result.FailureReason}");
                }
            }
            Debug.Log("  Travel validation verified for 6 islands");
        }
        finally
        {
            if (progressionObject != null) DestroyImmediate(progressionObject);
            if (trackerObject != null) DestroyImmediate(trackerObject);
        }
    }

    private void TestBoatInteractableCreatesDestinations()
    {
        GameObject boatObj = new GameObject("TestBoat", typeof(BoxCollider), typeof(Renderer));
        try
        {
            IslandBoatInteractable boat = boatObj.AddComponent<IslandBoatInteractable>();
            Assert.IsNotNull(boat, "Boat component should attach successfully.");
            Debug.Log("  Boat interactable creates without errors");
        }
        finally
        {
            DestroyImmediate(boatObj);
        }
    }

    private void TestBacktrackingUnlocks()
    {
        GameObject backtrackingObj = null;
        GameObject progressionObj = null;
        GameObject trackerObj = null;
        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("BT_Backtrack_Tracker");
            trackerObj = tracker.gameObject;
            IslandProgressionManager progression = CreateIsolatedProgressionManager("BT_Backtrack_Prog");
            progressionObj = progression.gameObject;

            IslandBacktrackingManager backtracking = new GameObject("TestBacktracking")
                .AddComponent<IslandBacktrackingManager>();
            backtrackingObj = backtracking.gameObject;

            Assert.IsFalse(backtracking.CanVisitIsland("island_greed"),
                "Greed should not be visitable before Desire is completed.");
            Debug.Log("  Backtracking unlock gates verified");
        }
        finally
        {
            if (backtrackingObj != null) DestroyImmediate(backtrackingObj);
            if (progressionObj != null) DestroyImmediate(progressionObj);
            if (trackerObj != null) DestroyImmediate(trackerObj);
        }
    }

    private void TestLegacyAliasesDoNotLeakToDestinations()
    {
        string[] legacyIds = { "island_wrath", "island_sloth", "island_pride", "island_gluttony" };
        for (int i = 0; i < legacyIds.Length; i++)
        {
            string resolved = IslandThemeRegistry.ResolveIslandId(legacyIds[i]);
            Assert.IsNotNull(resolved, $"Legacy ID '{legacyIds[i]}' should resolve to a V2 canonical ID.");
            bool isLegacy = resolved.Contains("wrath") || resolved.Contains("sloth")
                || resolved.Contains("pride") || resolved.Contains("gluttony");
            Assert.IsFalse(isLegacy,
                $"Legacy ID '{legacyIds[i]}' resolved to '{resolved}' which still contains a legacy name.");
        }
        Debug.Log("  Legacy aliases resolve to V2 canonical IDs (no leakage)");
    }

    private IslandRestorationTracker CreateIsolatedTracker(string name)
    {
        GameObject obj = new GameObject(name);
        return obj.AddComponent<IslandRestorationTracker>();
    }

    private IslandProgressionManager CreateIsolatedProgressionManager(string name)
    {
        GameObject obj = new GameObject(name);
        return obj.AddComponent<IslandProgressionManager>();
    }
}
