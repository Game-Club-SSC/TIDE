using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandBacktrackingManagerTest : MonoBehaviour
{
    [ContextMenu("Run Island Backtracking Manager Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Island Backtracking Manager Tests ===");

        TestSingletonCreation();
        TestSingletonDuplicateGuard();
        TestSingletonClearsOnDestroy();
        TestDontDestroyOnLoad();
        TestDesireCompletionUnlocksEarlierIslands();
        TestEnvyCompletionUnlocksEarlierIslands();
        TestEgoCompletionUnlocksEarlierIslands();
        TestAngerCompletionUnlocksEarlierIslands();
        TestOnBacktrackingNarrativeAvailableFires();
        TestDuplicateUnlockIsIdempotent();
        TestCanVisitIslandReturnsFalseForUnknown();

        Debug.Log("=== All Island Backtracking Manager Tests Passed ===");
    }

    private IslandBacktrackingManager CreateIsolatedManager()
    {
        if (IslandBacktrackingManager.Instance != null)
        {
            DestroyImmediate(IslandBacktrackingManager.Instance.gameObject);
        }

        GameObject go = new GameObject("TestBacktrackingManager");
        IslandBacktrackingManager manager = go.AddComponent<IslandBacktrackingManager>();
        Assert.AreSame(manager, IslandBacktrackingManager.Instance,
            "Manager singleton should reference the isolated test instance.");
        return manager;
    }

    private void TestSingletonCreation()
    {
        Debug.Log("Testing IslandBacktrackingManager singleton creation...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            Assert.IsNotNull(IslandBacktrackingManager.Instance, "Instance should be set.");
            Assert.AreSame(manager, IslandBacktrackingManager.Instance, "Instance should be the created manager.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Singleton creation test passed");
    }

    private void TestSingletonDuplicateGuard()
    {
        Debug.Log("Testing IslandBacktrackingManager duplicate guard...");

        IslandBacktrackingManager first = CreateIsolatedManager();
        GameObject firstGo = first.gameObject;

        try
        {
            GameObject secondGo = new GameObject("TestBacktrackingManager_Duplicate");
            secondGo.AddComponent<IslandBacktrackingManager>();

            Assert.IsTrue(secondGo == null, "Duplicate instance should be destroyed.");
            Assert.AreSame(first, IslandBacktrackingManager.Instance, "Original instance should remain.");
        }
        finally
        {
            if (firstGo != null) DestroyImmediate(firstGo);
        }

        Debug.Log("✓ Duplicate guard test passed");
    }

    private void TestSingletonClearsOnDestroy()
    {
        Debug.Log("Testing IslandBacktrackingManager clears Instance on destroy...");

        IslandBacktrackingManager manager = CreateIsolatedManager();

        DestroyImmediate(manager.gameObject);

        Assert.IsNull(IslandBacktrackingManager.Instance, "Instance should be null after destroy.");

        Debug.Log("✓ Singleton clear on destroy test passed");
    }

    private void TestDontDestroyOnLoad()
    {
        Debug.Log("Testing IslandBacktrackingManager uses DontDestroyOnLoad...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "IslandBacktrackingManager.cs"));

        Assert.IsTrue(sourceCode.Contains("DontDestroyOnLoad"),
            "IslandBacktrackingManager should use DontDestroyOnLoad.");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            Assert.IsTrue(go.scene.IsValid(), "GameObject should be valid.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ DontDestroyOnLoad test passed");
    }

    private void TestDesireCompletionUnlocksEarlierIslands()
    {
        Debug.Log("Testing Desire completion unlocks earlier islands (greed, greed, lust)...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            manager.UnlockBacktracking("island_desire");

            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Desire completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Desire completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_lust"),
                "Desire completion should unlock lust for backtracking.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Desire completion unlocks earlier islands test passed");
    }

    private void TestEnvyCompletionUnlocksEarlierIslands()
    {
        Debug.Log("Testing Envy completion unlocks earlier islands (anger, desire, greed, greed, lust)...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            manager.UnlockBacktracking("island_envy");

            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_anger"),
                "Envy completion should unlock anger for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_desire"),
                "Envy completion should unlock desire for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Envy completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Envy completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_lust"),
                "Envy completion should unlock lust for backtracking.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Envy completion unlocks earlier islands test passed");
    }

    private void TestEgoCompletionUnlocksEarlierIslands()
    {
        Debug.Log("Testing Ego completion unlocks earlier islands (envy, anger, desire, greed, greed, lust)...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            manager.UnlockBacktracking("island_ego");

            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_envy"),
                "Ego completion should unlock envy for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_anger"),
                "Ego completion should unlock anger for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_desire"),
                "Ego completion should unlock desire for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Ego completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Ego completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_lust"),
                "Ego completion should unlock lust for backtracking.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Ego completion unlocks earlier islands test passed");
    }

    private void TestAngerCompletionUnlocksEarlierIslands()
    {
        Debug.Log("Testing Anger completion unlocks earlier islands (desire, greed, greed, lust)...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            manager.UnlockBacktracking("island_anger");

            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_desire"),
                "Anger completion should unlock desire for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Anger completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_greed"),
                "Anger completion should unlock greed for backtracking.");
            Assert.IsTrue(manager.IsBacktrackingUnlocked("island_lust"),
                "Anger completion should unlock lust for backtracking.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Anger completion unlocks earlier islands test passed");
    }

    private void TestOnBacktrackingNarrativeAvailableFires()
    {
        Debug.Log("Testing OnBacktrackingNarrativeAvailable event fires on unlock...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            string capturedNarrative = null;
            manager.OnBacktrackingNarrativeAvailable += narrative => capturedNarrative = narrative;

            manager.UnlockBacktracking("island_desire");

            Assert.IsNotNull(capturedNarrative, "OnBacktrackingNarrativeAvailable should have fired.");
            Assert.IsTrue(capturedNarrative.Contains("texts mention"),
                "Narrative reason should match the desire unlock narrative.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ OnBacktrackingNarrativeAvailable event test passed");
    }

    private void TestDuplicateUnlockIsIdempotent()
    {
        Debug.Log("Testing duplicate unlock calls are idempotent...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            int fireCount = 0;
            manager.OnBacktrackingIslandAccessible += _ => fireCount++;

            manager.UnlockBacktracking("island_desire");
            int firstCount = fireCount;

            manager.UnlockBacktracking("island_desire");

            Assert.AreEqual(firstCount, fireCount,
                "Duplicate unlock should not fire additional events.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Duplicate unlock idempotent test passed");
    }

    private void TestCanVisitIslandReturnsFalseForUnknown()
    {
        Debug.Log("Testing CanVisitIsland returns false for unknown island...");

        IslandBacktrackingManager manager = CreateIsolatedManager();
        GameObject go = manager.gameObject;

        try
        {
            bool canVisit = manager.CanVisitIsland("island_nonexistent");
            Assert.IsFalse(canVisit, "CanVisitIsland should return false for unknown island.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ CanVisitIsland unknown island test passed");
    }
}
