using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BossEncounterGateTest : MonoBehaviour
{
    [ContextMenu("Run Boss Encounter Gate Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Boss Encounter Gate Tests ===");

        TestBossLockedBelowThreshold();
        TestBossUnlockedAtThreshold();
        TestBossUnlockedAboveThreshold();
        TestBossStateOnReenable();
        TestBossEventsFired();
        TestBossEventsNotFiredWhenAlreadyUnlocked();
        TestIslandIdFiltering();
        TestThresholdTuning();

        Debug.Log("=== All Boss Encounter Gate Tests Passed ===");
    }

    private static IslandRestorationTracker CreateIsolatedTracker(string trackerName)
    {
        if (IslandRestorationTracker.Instance != null)
        {
            Object.DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        GameObject trackerObject = new GameObject(trackerName);
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference the isolated test tracker instance.");
        return tracker;
    }

    private void TestBossLockedBelowThreshold()
    {
        Debug.Log("Testing boss locked below threshold...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossLock");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossLock");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_test", "c1", EncounterType.Combat, 0.3f);

        Assert.IsFalse(gate.IsBossUnlocked, "Boss should be locked at 30%.");
        Assert.IsFalse(bossVisuals.activeSelf, "Boss visuals should be inactive at 30%.");
        Assert.IsFalse(interactionCollider.enabled, "Interaction collider should be disabled at 30%.");
        Assert.IsFalse(bossTrigger.enabled, "Boss trigger should be disabled at 30%.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss locked below threshold test passed");
    }

    private void TestBossUnlockedAtThreshold()
    {
        Debug.Log("Testing boss unlocked at threshold...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossUnlockAt");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossUnlockAt");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_At");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_test", "c1", EncounterType.Combat, 0.75f);

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should be unlocked at 75%.");
        Assert.IsTrue(bossVisuals.activeSelf, "Boss visuals should be active at 75%.");
        Assert.IsTrue(interactionCollider.enabled, "Interaction collider should be enabled at 75%.");
        Assert.IsTrue(bossTrigger.enabled, "Boss trigger should be enabled at 75%.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss unlocked at threshold test passed");
    }

    private void TestBossUnlockedAboveThreshold()
    {
        Debug.Log("Testing boss unlocked above threshold...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossUnlockAbove");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossUnlockAbove");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_Above");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_test", "c1", EncounterType.Combat, 0.5f);
        tracker.RecordEncounterCompletion("island_test", "p1", EncounterType.Puzzle, 0.5f);

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should be unlocked at 100%.");
        Assert.IsTrue(bossVisuals.activeSelf, "Boss visuals should be active at 100%.");
        Assert.IsTrue(interactionCollider.enabled, "Interaction collider should be enabled at 100%.");
        Assert.IsTrue(bossTrigger.enabled, "Boss trigger should be enabled at 100%.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss unlocked above threshold test passed");
    }

    private void TestBossStateOnReenable()
    {
        Debug.Log("Testing boss state on re-enable (reload consistency)...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossReenable");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_reload", "c1", EncounterType.Combat, 0.4f);
        tracker.RecordEncounterCompletion("island_reload", "p1", EncounterType.Puzzle, 0.4f);

        GameObject gateObject = new GameObject("TestGate_BossReenable");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_Reenable");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should be unlocked on re-enable when restoration is 80%.");
        Assert.IsTrue(bossVisuals.activeSelf, "Boss visuals should be active on re-enable.");
        Assert.IsTrue(interactionCollider.enabled, "Interaction collider should be enabled on re-enable.");
        Assert.IsTrue(bossTrigger.enabled, "Boss trigger should be enabled on re-enable.");

        gateObject.SendMessage("OnDisable");

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should remain unlocked after second re-enable.");
        Assert.IsTrue(bossVisuals.activeSelf, "Boss visuals should be active after second re-enable.");
        Assert.IsTrue(interactionCollider.enabled, "Interaction collider should be enabled after second re-enable.");
        Assert.IsTrue(bossTrigger.enabled, "Boss trigger should be enabled after second re-enable.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss state on re-enable test passed");
    }

    private void TestBossEventsFired()
    {
        Debug.Log("Testing boss unlock/lock events...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossEvents");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossEvents");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_Events");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        int unlockCount = 0;
        int lockCount = 0;
        gate.OnBossUnlocked.AddListener(() => unlockCount++);
        gate.OnBossLocked.AddListener(() => lockCount++);

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_evt", "c1", EncounterType.Combat, 0.5f);
        Assert.AreEqual(0, unlockCount, "OnBossUnlocked should not fire at 50%.");
        Assert.AreEqual(0, lockCount, "OnBossLocked should not fire when staying locked.");

        tracker.RecordEncounterCompletion("island_evt", "p1", EncounterType.Puzzle, 0.3f);
        Assert.AreEqual(1, unlockCount, "OnBossUnlocked should fire once at 80%.");
        Assert.AreEqual(0, lockCount, "OnBossLocked should not fire after unlock.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss events test passed");
    }

    private void TestBossEventsNotFiredWhenAlreadyUnlocked()
    {
        Debug.Log("Testing boss events not re-fired when already unlocked...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossNoRepeat");
        GameObject trackerObject = tracker.gameObject;

        tracker.RecordEncounterCompletion("island_norepeat", "c1", EncounterType.Combat, 0.8f);

        GameObject gateObject = new GameObject("TestGate_BossNoRepeat");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_NoRepeat");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        int unlockCount = 0;
        gate.OnBossUnlocked.AddListener(() => unlockCount++);

        gateObject.SendMessage("OnEnable");

        Assert.AreEqual(0, unlockCount, "OnBossUnlocked should not fire on initial enable (already unlocked).");

        tracker.RecordEncounterCompletion("island_norepeat", "p1", EncounterType.Puzzle, 0.2f);
        Assert.AreEqual(0, unlockCount, "OnBossUnlocked should not re-fire when already unlocked.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Boss no re-fire test passed");
    }

    private void TestIslandIdFiltering()
    {
        Debug.Log("Testing island ID filtering...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossFilter");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossFilter");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_Filter");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 75f);
        SetPrivateField(gate, "islandId", "island_a");

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_b", "c1", EncounterType.Combat, 0.8f);

        Assert.IsFalse(gate.IsBossUnlocked, "Boss should stay locked when other island reaches threshold.");

        tracker.RecordEncounterCompletion("island_a", "c1", EncounterType.Combat, 0.8f);

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should unlock when correct island reaches threshold.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Island ID filtering test passed");
    }

    private void TestThresholdTuning()
    {
        Debug.Log("Testing threshold tuning...");

        IslandRestorationTracker tracker = CreateIsolatedTracker("TestTracker_BossTune");
        GameObject trackerObject = tracker.gameObject;

        GameObject gateObject = new GameObject("TestGate_BossTune");
        BossEncounterGate gate = gateObject.AddComponent<BossEncounterGate>();

        GameObject bossVisuals = new GameObject("BossVisuals_Tune");
        BoxCollider interactionCollider = bossVisuals.AddComponent<BoxCollider>();
        EnemyTrigger bossTrigger = bossVisuals.AddComponent<EnemyTrigger>();

        SetPrivateField(gate, "bossVisuals", bossVisuals);
        SetPrivateField(gate, "bossInteractionCollider", interactionCollider);
        SetPrivateField(gate, "bossTrigger", bossTrigger);
        SetPrivateField(gate, "bossUnlockThresholdPercent", 50f);

        bossVisuals.SetActive(false);
        interactionCollider.enabled = false;
        bossTrigger.enabled = false;

        gateObject.SendMessage("OnEnable");

        tracker.RecordEncounterCompletion("island_tune", "c1", EncounterType.Combat, 0.4f);

        Assert.IsFalse(gate.IsBossUnlocked, "Boss should be locked at 40% with 50% threshold.");

        tracker.RecordEncounterCompletion("island_tune", "p1", EncounterType.Puzzle, 0.1f);

        Assert.IsTrue(gate.IsBossUnlocked, "Boss should unlock at 50% with 50% threshold.");

        DestroyImmediate(gateObject);
        DestroyImmediate(bossVisuals);
        DestroyImmediate(trackerObject);
        Debug.Log("  Threshold tuning test passed");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
