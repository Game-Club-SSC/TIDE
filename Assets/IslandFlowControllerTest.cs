using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandFlowControllerTest : MonoBehaviour
{
    [ContextMenu("Run Island Flow Controller Tests")]
    public void RunTests()
    {
        TestUnlockedBossDoesNotSkipEarlierEncounter();
        Debug.Log("=== All Island Flow Controller Tests Passed ===");
    }

    private void TestUnlockedBossDoesNotSkipEarlierEncounter()
    {
        Debug.Log("Testing unlocked boss preserves earlier encounter order...");

        GameObject trackerObject = null;
        GameObject controllerObject = null;
        IslandConfig config = null;

        try
        {
            if (IslandRestorationTracker.Instance != null)
            {
                DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
            }

            trackerObject = new GameObject("TestTracker_IslandFlowOrder");
            IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
            Assert.AreSame(tracker, IslandRestorationTracker.Instance,
                "Test tracker should own the restoration singleton.");

            tracker.SetIslandRestorationPercentForDebug("island_lust", 75f);

            controllerObject = new GameObject("TestIslandFlowController_Order");
            IslandFlowController controller = controllerObject.AddComponent<IslandFlowController>();
            config = ScriptableObject.CreateInstance<IslandConfig>();
            config.islandId = "island_lust";
            config.encounters = new EncounterDefinition[]
            {
                new EncounterDefinition
                {
                    encounterId = "c1",
                    type = EncounterType.Combat,
                    restorationValue = 0.4f
                },
                new EncounterDefinition
                {
                    encounterId = "p1",
                    type = EncounterType.Puzzle,
                    restorationValue = 0.35f
                },
                new EncounterDefinition
                {
                    encounterId = "lust_boss",
                    type = EncounterType.Combat,
                    isBossEncounter = true,
                    restorationValue = 0.25f
                }
            };

            SetPrivateField(controller, "islandConfig", config);
            SetPrivateField(controller, "activeIslandId", "island_lust");

            int nextIndex = (int)InvokePrivate(controller, "GetNextIncompleteEncounterIndex");
            Assert.AreEqual(0, nextIndex,
                "An unlocked boss must not skip an earlier incomplete combat encounter.");

            tracker.RecordEncounterCompletion("island_lust", "c1", EncounterType.Combat, 0.4f);
            nextIndex = (int)InvokePrivate(controller, "GetNextIncompleteEncounterIndex");
            Assert.AreEqual(1, nextIndex,
                "After the first encounter is complete, the next incomplete encounter should load before the boss.");

        }
        finally
        {
            if (config != null)
            {
                DestroyImmediate(config);
            }

            if (controllerObject != null)
            {
                DestroyImmediate(controllerObject);
            }

            if (trackerObject != null)
            {
                DestroyImmediate(trackerObject);
            }
        }
    }

    private static object InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");
        return method.Invoke(target, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        field.SetValue(target, value);
    }
}
