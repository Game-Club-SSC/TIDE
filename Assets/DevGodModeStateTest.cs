using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class DevGodModeStateTest : MonoBehaviour
{
    [ContextMenu("Run Dev God Mode State Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Dev God Mode State Tests ===");

        TestTrackerDebugResetAndSetPercent();
        TestProgressionDebugUnlockAndReset();

        Debug.Log("=== Dev God Mode State Tests Passed ===");
    }

    private void TestTrackerDebugResetAndSetPercent()
    {
        GameObject trackerObject = null;
        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_DevGod");
            trackerObject = tracker.gameObject;

            tracker.SetIslandRestorationPercentForDebug("island_lust", 100f);
            Assert.AreEqual(100f, tracker.GetRestorationPercent("island_lust"), 0.001f,
                "Debug set percent should drive restoration to expected value.");

            tracker.ResetAllIslandsForDebug();
            Assert.AreEqual(0f, tracker.GetRestorationPercent("island_lust"), 0.001f,
                "Debug reset should clear lust island restoration.");
            Assert.AreEqual(0f, tracker.GetRestorationPercent("island_anger"), 0.001f,
                "Debug reset should clear other tracked islands too.");
        }
        finally
        {
            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private void TestProgressionDebugUnlockAndReset()
    {
        GameObject trackerObject = null;
        GameObject progressionObject = null;
        try
        {
            IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_DevProgression");
            trackerObject = tracker.gameObject;

            IslandProgressionManager progression = CreateIsolatedProgressionManager("TestProgression_DevGod");
            progressionObject = progression.gameObject;

            progression.UnlockAllIslandsForDebug();
            string[] unlocked = progression.GetUnlockedIslandIds();
            Assert.GreaterOrEqual(unlocked.Length, IslandThemeRegistry.ProgressionOrder.Count,
                "Unlock all should include every progression island.");

            Assert.IsTrue(progression.ForceSetActiveIslandForDebug("island_envy"),
                "Force set active island should accept known island IDs.");
            Assert.AreEqual("island_envy", progression.ActiveIslandId,
                "Active island should switch to forced island.");

            progression.ResetProgressionForDebug();
            Assert.AreEqual(IslandThemeRegistry.ResolveIslandId(IslandThemeRegistry.DefaultIslandId), progression.ActiveIslandId,
                "Reset should return active island to default.");
            Assert.IsTrue(progression.IsIslandUnlocked(IslandThemeRegistry.DefaultIslandId),
                "Default island should remain unlocked after reset.");
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
        Assert.AreSame(progression, IslandProgressionManager.Instance,
            "Progression singleton should reference isolated test progression instance.");
        return progression;
    }
}
