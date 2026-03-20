using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class RestorationThresholdGateTest : MonoBehaviour
{
    [ContextMenu("Run Restoration Threshold Gate Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Restoration Threshold Gate Tests ===");

        TestStartupSyncBelowThreshold();
        TestStartupSyncAboveThreshold();

        Debug.Log("=== All Restoration Threshold Gate Tests Passed ===");
    }

    private void TestStartupSyncBelowThreshold()
    {
        Debug.Log("Testing threshold gate startup sync below threshold...");

        GameObject trackerObject = new GameObject("TestTracker_ThresholdLow");
        GameObject gateObject = new GameObject("TestGate_ThresholdLow");
        GameObject objectToEnable = new GameObject("Enable_Low");
        GameObject objectToDisable = new GameObject("Disable_Low");

        try
        {
            IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
            tracker.RecordEncounterCompletion("island_low", "c1", EncounterType.Combat, 0.3f);

            gateObject.SetActive(false);
            RestorationThresholdGate gate = gateObject.AddComponent<RestorationThresholdGate>();
            SetPrivateField(gate, "islandId", "island_low");
            SetPrivateField(gate, "thresholdPercent", 75f);
            SetPrivateField(gate, "objectToEnable", objectToEnable);
            SetPrivateField(gate, "objectToDisable", objectToDisable);

            objectToEnable.SetActive(true);
            objectToDisable.SetActive(false);

            int reachedCount = 0;
            int lostCount = 0;
            gate.OnThresholdReached.AddListener(() => reachedCount++);
            gate.OnThresholdLost.AddListener(() => lostCount++);

            gateObject.SetActive(true);

            Assert.IsFalse(gate.ThresholdMet, "Threshold should be unmet at 30%.");
            Assert.IsFalse(objectToEnable.activeSelf, "Startup sync should disable the gated object below threshold.");
            Assert.IsTrue(objectToDisable.activeSelf, "Startup sync should enable the blocker below threshold.");
            Assert.AreEqual(0, reachedCount, "Startup sync should not emit a false threshold reached event.");
            Assert.AreEqual(0, lostCount, "Startup sync should not emit a false threshold lost event.");

            tracker.RecordEncounterCompletion("island_low", "p1", EncounterType.Puzzle, 0.5f);

            Assert.IsTrue(gate.ThresholdMet, "Threshold should become met at 80%.");
            Assert.IsTrue(objectToEnable.activeSelf, "Threshold crossing should enable the gated object.");
            Assert.IsFalse(objectToDisable.activeSelf, "Threshold crossing should disable the blocker.");
            Assert.AreEqual(1, reachedCount, "Threshold reached event should fire exactly once on a real unlock.");
            Assert.AreEqual(0, lostCount, "Threshold lost event should not fire while unlocking.");

            Debug.Log("  Threshold gate startup sync below threshold test passed");
        }
        finally
        {
            Object.DestroyImmediate(gateObject);
            Object.DestroyImmediate(objectToEnable);
            Object.DestroyImmediate(objectToDisable);
            Object.DestroyImmediate(trackerObject);
        }
    }

    private void TestStartupSyncAboveThreshold()
    {
        Debug.Log("Testing threshold gate startup sync above threshold...");

        GameObject trackerObject = new GameObject("TestTracker_ThresholdHigh");
        GameObject gateObject = new GameObject("TestGate_ThresholdHigh");
        GameObject objectToEnable = new GameObject("Enable_High");
        GameObject objectToDisable = new GameObject("Disable_High");

        try
        {
            IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
            tracker.RecordEncounterCompletion("island_high", "c1", EncounterType.Combat, 0.8f);

            gateObject.SetActive(false);
            RestorationThresholdGate gate = gateObject.AddComponent<RestorationThresholdGate>();
            SetPrivateField(gate, "islandId", "island_high");
            SetPrivateField(gate, "thresholdPercent", 75f);
            SetPrivateField(gate, "objectToEnable", objectToEnable);
            SetPrivateField(gate, "objectToDisable", objectToDisable);

            objectToEnable.SetActive(false);
            objectToDisable.SetActive(true);

            int reachedCount = 0;
            int lostCount = 0;
            gate.OnThresholdReached.AddListener(() => reachedCount++);
            gate.OnThresholdLost.AddListener(() => lostCount++);

            gateObject.SetActive(true);

            Assert.IsTrue(gate.ThresholdMet, "Threshold should be met at 80%.");
            Assert.IsTrue(objectToEnable.activeSelf, "Startup sync should enable the gated object above threshold.");
            Assert.IsFalse(objectToDisable.activeSelf, "Startup sync should disable the blocker above threshold.");
            Assert.AreEqual(0, reachedCount, "Startup sync should not emit a false threshold reached event.");
            Assert.AreEqual(0, lostCount, "Startup sync should not emit a false threshold lost event.");

            tracker.ResetIsland("island_high");

            Assert.IsFalse(gate.ThresholdMet, "Threshold should clear after the island is reset.");
            Assert.IsFalse(objectToEnable.activeSelf, "Dropping below threshold should disable the gated object.");
            Assert.IsTrue(objectToDisable.activeSelf, "Dropping below threshold should re-enable the blocker.");
            Assert.AreEqual(0, reachedCount, "Threshold reached should not backfill during startup sync.");
            Assert.AreEqual(1, lostCount, "Threshold lost event should fire exactly once on a real lock.");

            Debug.Log("  Threshold gate startup sync above threshold test passed");
        }
        finally
        {
            Object.DestroyImmediate(gateObject);
            Object.DestroyImmediate(objectToEnable);
            Object.DestroyImmediate(objectToDisable);
            Object.DestroyImmediate(trackerObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
