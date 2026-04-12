using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GameStateManagerSingletonVerificationTest : MonoBehaviour
{
    private static readonly FieldInfo EnablePersistentSaveDataField =
        typeof(GameStateManager).GetField("enablePersistentSaveData", BindingFlags.Instance | BindingFlags.NonPublic);

    [ContextMenu("Run GameStateManager Singleton Verification")]
    public void RunTests()
    {
        Debug.Log("=== Starting GameStateManager Singleton Verification ===");

        TestDuplicateGuardPreservesOriginalManager();
        TestSingletonClearsInstanceOnDestroy();

        Debug.Log("=== GameStateManager Singleton Verification Passed ===");
    }

    private void TestDuplicateGuardPreservesOriginalManager()
    {
        Debug.Log("Testing GameStateManager duplicate guard...");

        GameObject duplicateObject = null;

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_Primary");
            GameObject managerObject = manager.gameObject;

            duplicateObject = new GameObject("TestGameStateManager_Duplicate");
            GameStateManager duplicate = duplicateObject.AddComponent<GameStateManager>();

            Assert.AreSame(manager, GameStateManager.Instance,
                "Duplicate manager should not replace the active singleton.");
            Assert.IsTrue(managerObject != null,
                "Original manager owner GameObject should remain after duplicate creation.");
            Assert.IsTrue(duplicateObject != null,
                "Duplicate manager owner GameObject should remain after component-only cleanup.");

            if (Application.isPlaying)
            {
                Assert.AreNotSame(duplicate, GameStateManager.Instance,
                    "Duplicate manager should never become the active singleton.");
            }
            else
            {
                Assert.IsNull(duplicateObject.GetComponent<GameStateManager>(),
                    "Duplicate manager component should be removed without destroying its GameObject.");
                Assert.IsTrue(duplicate == null,
                    "Duplicate manager component should be destroyed immediately during verification runs.");
            }
        }
        finally
        {
            if (duplicateObject != null)
            {
                DestroyImmediate(duplicateObject);
            }

            CleanupManagerEnvironment();
        }

        Debug.Log("GameStateManager duplicate guard test passed");
    }

    private void TestSingletonClearsInstanceOnDestroy()
    {
        Debug.Log("Testing GameStateManager singleton destroy cleanup...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_Destroy");
            GameObject managerObject = manager.gameObject;

            DestroyImmediate(managerObject);

            Assert.IsNull(GameStateManager.Instance,
                "Destroying the active manager should clear the singleton instance.");
        }
        finally
        {
            CleanupManagerEnvironment();
        }

        Debug.Log("GameStateManager singleton destroy cleanup test passed");
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        CleanupManagerEnvironment();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        DisablePersistentSave(manager);

        Assert.AreSame(manager, GameStateManager.Instance,
            "GameStateManager singleton should reference the isolated test manager instance.");
        return manager;
    }

    private static void DisablePersistentSave(GameStateManager manager)
    {
        if (manager == null)
        {
            return;
        }

        Assert.IsNotNull(EnablePersistentSaveDataField,
            "GameStateManager persistent save field was not found. Update the singleton verification helper.");
        EnablePersistentSaveDataField.SetValue(manager, false);
    }

    private static void CleanupManagerEnvironment()
    {
        DestroyImmediateIfPresent(GameStateManager.Instance);
        DestroyImmediateIfPresent(IslandRestorationTracker.Instance);
        DestroyImmediateIfPresent(IslandProgressionManager.Instance);
        DestroyImmediateIfPresent(HeroProgressionManager.Instance);

        DevModeController[] devModeControllers = FindObjectsByType<DevModeController>(FindObjectsSortMode.None);
        for (int i = 0; i < devModeControllers.Length; i++)
        {
            if (devModeControllers[i] != null)
            {
                DestroyImmediate(devModeControllers[i].gameObject);
            }
        }
    }

    private static void DestroyImmediateIfPresent(MonoBehaviour behaviour)
    {
        if (behaviour != null)
        {
            DestroyImmediate(behaviour.gameObject);
        }
    }
}
