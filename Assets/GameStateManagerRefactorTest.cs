using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GameStateManagerRefactorTest : MonoBehaviour
{
    [ContextMenu("Run GameStateManager Refactor Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting GameStateManager Refactor Tests ===");

        TestWorldSaveServiceWritesAndReads();
        TestWorldSaveServiceRejectsEmptyJson();
        TestWorldSaveServiceClears();
        TestWorldSaveServiceTogglePersistsDisabled();
        TestStoryProgressionServiceTracksAct();
        TestStoryProgressionServiceTracksHighestAct();
        TestStoryProgressionServiceEndingTrigger();
        TestStoryProgressionServiceReset();

        Debug.Log("=== All GameStateManager Refactor Tests Passed ===");
    }

    private void TestWorldSaveServiceWritesAndReads()
    {
        GameObject host = new GameObject("Test_Save");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.Clear();
            string key = "TEST_KEY_" + System.Guid.NewGuid().ToString("N");
            service.SetPersistentSaveEnabled(true);
            Assert.IsTrue(service.TryWriteJson("{\"foo\":42}"), "Write should succeed.");
            Assert.IsTrue(service.HasPersistedData, "Should have persisted data.");
            service.TryLoadJson(out string json);
            Assert.AreEqual("{\"foo\":42}", json, "Should read back same JSON.");
            service.Clear();
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestWorldSaveServiceRejectsEmptyJson()
    {
        GameObject host = new GameObject("Test_Save2");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.Clear();
            Assert.IsFalse(service.TryWriteJson(""), "Empty JSON should be rejected.");
            Assert.IsFalse(service.TryWriteJson(null), "Null JSON should be rejected.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestWorldSaveServiceClears()
    {
        GameObject host = new GameObject("Test_Save3");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.TryWriteJson("{\"a\":1}");
            Assert.IsTrue(service.HasPersistedData, "Should have data after write.");
            service.Clear();
            Assert.IsFalse(service.HasPersistedData, "Should be cleared.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestWorldSaveServiceTogglePersistsDisabled()
    {
        GameObject host = new GameObject("Test_Save4");
        WorldSaveService service = host.AddComponent<WorldSaveService>();
        try
        {
            service.SetPersistentSaveEnabled(false);
            Assert.IsFalse(service.TryWriteJson("{\"a\":1}"), "Write should fail when disabled.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestStoryProgressionServiceTracksAct()
    {
        GameObject host = new GameObject("Test_Story");
        StoryProgressionService service = host.AddComponent<StoryProgressionService>();
        try
        {
            service.SetCurrentAct(StoryProgressionService.StoryAct.ActI);
            Assert.AreEqual(StoryProgressionService.StoryAct.ActI, service.CurrentAct);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestStoryProgressionServiceTracksHighestAct()
    {
        GameObject host = new GameObject("Test_Story2");
        StoryProgressionService service = host.AddComponent<StoryProgressionService>();
        try
        {
            service.SetCurrentAct(StoryProgressionService.StoryAct.ActI);
            service.SetCurrentAct(StoryProgressionService.StoryAct.ActII);
            service.SetCurrentAct(StoryProgressionService.StoryAct.ActI);
            Assert.AreEqual(StoryProgressionService.StoryAct.ActII, service.HighestActReached, "Highest should remain ActII.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestStoryProgressionServiceEndingTrigger()
    {
        GameObject host = new GameObject("Test_Story3");
        StoryProgressionService service = host.AddComponent<StoryProgressionService>();
        try
        {
            service.SetEndingBranch(StoryProgressionService.EndingBranch.Good);
            Assert.AreEqual(StoryProgressionService.EndingBranch.Good, service.ResolvedEndingBranch);
            service.TriggerEnding();
            Assert.IsTrue(service.IsEndingTriggered, "Should be triggered.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestStoryProgressionServiceReset()
    {
        GameObject host = new GameObject("Test_Story4");
        StoryProgressionService service = host.AddComponent<StoryProgressionService>();
        try
        {
            service.SetCurrentAct(StoryProgressionService.StoryAct.ActII);
            service.SetEndingBranch(StoryProgressionService.EndingBranch.Bad);
            service.TriggerEnding();
            service.ResetForDebug();
            Assert.AreEqual(StoryProgressionService.StoryAct.None, service.CurrentAct);
            Assert.IsFalse(service.IsEndingTriggered, "Should be reset.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}
