#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for the pre-boss encounter sequence introduced when
/// NarrativeBeatDirector.PlayPreBossDialogue was wired into the island flow.
///
/// These tests drive the real controller, the real DialogueSystem and the
/// real dialogue trees. They exist because the original wiring was broken
/// twice: once because the combat scene load destroyed the scene-owned
/// dialogue runner 0.2s in, and once because a cancelled dialogue left the
/// boss permanently deferred behind a flag that never cleared.
/// </summary>
[DisallowMultipleComponent]
public class PreBossDialogueFlowTest : MonoBehaviour
{
    private GameObject gameStateObject;
    private GameObject controllerObject;
    private GameObject trackerObject;
    private IslandConfig islandConfig;

    [SetUp]
    public void SetUp()
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [Test]
    public void PreBossDialogueMustHaveARootAndResolvableMapEntry()
    {
        // Every island that ships a boss needs a pre-boss tree that can
        // actually start, otherwise the encounter waits on a callback that
        // can never fire.
        string[] bossIslands = { "island_greed", "island_desire", "island_envy", "island_lust", "island_anger", "island_ego" };

        for (int i = 0; i < bossIslands.Length; i++)
        {
            string resolved = IslandThemeRegistry.ResolveIslandId(bossIslands[i]);
            Assert.IsNotEmpty(resolved, $"'{bossIslands[i]}' must resolve to a known island id.");

            DialogueTree probe = HeroDialogueContent.PreBossGreedDialogue();
            Assert.IsNotNull(probe, "Pre-boss tree factory must build a tree.");
            Assert.IsNotNull(probe.rootNode, $"Pre-boss tree '{probe.treeId}' must have a root node.");

            DialogueSystem dialogue = CreateDialogueSystem();
            try
            {
                Assert.AreSame(dialogue, DialogueSystem.Instance,
                    "The dialogue system used for the check must be the registered singleton.");
                bool started = NarrativeBeatDirector.PlayPreBossDialogue(resolved);
                Assert.IsTrue(started, $"Pre-boss dialogue for '{resolved}' must start (root present: {probe.rootNode != null}, instance null: {DialogueSystem.Instance == null}).");
                Assert.IsTrue(dialogue.IsDialogueActive,
                    $"Pre-boss dialogue for '{resolved}' must leave a tree active.");
            }
            finally
            {
                Object.DestroyImmediate(dialogue.gameObject);
            }
        }
    }

    [Test]
    public void PlayPreBossDialogueReportsFalseWhenItCannotStart()
    {
        // The caller defers the encounter on `true`. A silent no-op that
        // returns true would strand the boss forever.
        DialogueSystem dialogue = CreateDialogueSystem();
        try
        {
            Assert.IsFalse(NarrativeBeatDirector.PlayPreBossDialogue("island_not_a_real_island"),
                "An island with no authored pre-boss tree must report false, not claim ownership.");
            Assert.IsFalse(NarrativeBeatDirector.PlayPreBossDialogue(string.Empty),
                "An empty island id must report false.");
            Assert.IsFalse(dialogue.IsDialogueActive,
                "A failed request must not leave a dialogue active.");
        }
        finally
        {
            Object.DestroyImmediate(dialogue.gameObject);
        }
    }

    [Test]
    public void BossEncounterIsDeferredWhileDialogueIsPlaying()
    {
        GameStateManager gameState = CreateGameState();
        IslandFlowController controller = CreateController(gameState);
        DialogueSystem dialogue = CreateDialogueSystem();

        try
        {
            islandConfig = CreateBossIslandConfig();
            RestoreIslandToBossThreshold(islandConfig.islandId);
            controller.StartIsland(islandConfig);

            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);

            // The observable outcome: the pre-boss dialogue is on screen and
            // the combat context has not been queued yet.
            Assert.IsTrue(dialogue.IsDialogueActive,
                "A boss encounter must play its pre-boss dialogue before entering combat.");
            Assert.IsNull(gameState.PendingCombatEncounterId,
                "Combat must not be queued while the pre-boss dialogue is still on screen.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void CancelledPreBossDialogueDoesNotStrandTheBoss()
    {
        // Regression guard for the release-blocking soft-lock. A scene load
        // cancels the dialogue WITHOUT firing OnDialogueTreeCompleted, so the
        // controller's completion callback never arrives. The retry must
        // recover and queue the encounter; asserting on a private flag is
        // not enough, because the field was previously never set in the first
        // place (the boss was locked below the restoration threshold, so the
        // pre-boss path was never reached).
        GameStateManager gameState = CreateGameState();
        IslandFlowController controller = CreateController(gameState);
        DialogueSystem dialogue = CreateDialogueSystem();

        try
        {
            islandConfig = CreateBossIslandConfig();
            RestoreIslandToBossThreshold(islandConfig.islandId);
            controller.StartIsland(islandConfig);

            // First attempt: the controller starts the pre-boss dialogue and
            // defers the encounter, so the pending context is NOT set yet.
            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);

            Assert.IsTrue(dialogue.IsDialogueActive,
                "The first attempt must start the pre-boss dialogue.");
            Assert.IsNull(gameState.PendingCombatEncounterId,
                "The encounter must be deferred while the pre-boss dialogue is unresolved.");

            // A scene load cancels the dialogue and fires no completion event.
            InvokeCancelSceneDialogue(dialogue);
            Assert.IsFalse(dialogue.IsDialogueActive, "Setup: the scene load must cancel the dialogue.");

            // Retry: the controller must recover.
            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);

            // Recovery restarts the conversation rather than queuing combat
            // immediately: the player never saw it, so replaying is correct
            // and combat must still be deferred.
            Assert.IsTrue(dialogue.IsDialogueActive,
                "After a cancelled dialogue the retry must restart the pre-boss conversation instead of wedging the boss.");
            Assert.IsNull(gameState.PendingCombatEncounterId,
                "Combat must stay deferred while the restarted pre-boss dialogue is on screen.");
            Assert.AreEqual(islandConfig.encounters[0].encounterId,
                GetPrivateField(controller, "preBossEncounterIdPlayed"),
                "The restarted conversation must be tracked against the same boss encounter.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void StartingANewIslandClearsAnyPendingPreBossDeferral()
    {
        // The deferral must genuinely exist first, otherwise this test cannot
        // distinguish "StartIsland cleared it" from "there was never one".
        GameStateManager gameState = CreateGameState();
        IslandFlowController controller = CreateController(gameState);
        DialogueSystem dialogue = CreateDialogueSystem();

        try
        {
            islandConfig = CreateBossIslandConfig();
            RestoreIslandToBossThreshold(islandConfig.islandId);
            controller.StartIsland(islandConfig);

            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);
            Assert.IsTrue(dialogue.IsDialogueActive, "Setup: a pre-boss dialogue must be running.");
            Assert.IsNull(gameState.PendingCombatEncounterId, "Setup: the encounter must be deferred.");
            Assert.IsNotNull(GetPrivateField(controller, "preBossDialogueCallback"),
                "Setup: a pending deferral callback should be registered.");

            // Move to a different island while the dialogue is unresolved.
            IslandConfig second = CreateBossIslandConfig("island_envy");
            RestoreIslandToBossThreshold(second.islandId);
            controller.StartIsland(second);

            Assert.IsNull(GetPrivateField(controller, "preBossDialogueCallback"),
                "StartIsland must release a deferral left over from the previous island.");
        }
        finally
        {
            Cleanup();
        }
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private GameStateManager CreateGameState()
    {
        gameStateObject = new GameObject("TestGameStateManager_PreBoss");
        GameStateManager gameState = gameStateObject.AddComponent<GameStateManager>();

        // GameStateManager registers in Awake (it has no OnEnable), and its
        // Awake calls DontDestroyOnLoad, which throws outside play mode.
        // Invoke it, then ensure the singleton is set so LoadCombatEncounter
        // does not bail out at its GameStateManager guard.
        try
        {
            InvokePrivate(gameState, "Awake");
        }
        catch (System.Exception)
        {
            // Expected in edit mode; the property is the fallback.
        }

        if (GameStateManager.Instance != gameState)
        {
            PropertyInfo instance = typeof(GameStateManager).GetProperty(
                "Instance", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(instance, "GameStateManager.Instance should exist.");
            instance.GetSetMethod(true).Invoke(gameState, new object[] { gameState });
        }

        return gameState;
    }

    /// <summary>
    /// Drives the island's restoration to the boss unlock threshold through the
    /// real tracker, so the boss gate opens and LoadCombatEncounter is actually
    /// reached. Without this the controller returns at the IsBossUnlocked gate
    /// before touching the pre-boss path at all, which is why an earlier
    /// version of this test asserted a field that was never written.
    /// </summary>
    private void RestoreIslandToBossThreshold(string islandId)
    {
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        Assert.IsNotNull(tracker, "Setup: the restoration tracker must exist.");

        tracker.SetIslandRestorationPercentForDebug(islandId, 100f);
        Assert.IsTrue(
            tracker.IsRestorationAtOrAbove(islandId, IslandRestorationTracker.DefaultBossUnlockThresholdPercent),
            $"Setup: '{islandId}' should be at or above the boss unlock threshold.");
    }

    private IslandFlowController CreateController(GameStateManager gameState)
    {
        controllerObject = new GameObject("TestIslandFlowController_PreBoss");
        IslandFlowController controller = controllerObject.AddComponent<IslandFlowController>();
        controllerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

        trackerObject = new GameObject("TestIslandRestorationTracker_PreBoss");
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        trackerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

        // EnterCombatScene preserves the pending encounter context only when a
        // flow controller owns it, and clears it otherwise. Registering here
        // mirrors the runtime wiring done by IslandFlowController itself.
        gameState.FlowController = controller;

        return controller;
    }

    private DialogueSystem CreateDialogueSystem()
    {
        // A DialogueSystem left over from a previous test is a destroyed
        // Unity object that still compares != null, and StartDialogueTree
        // refuses to start while isDialogueActive is set.
        if (DialogueSystem.Instance != null)
        {
            Object.DestroyImmediate(DialogueSystem.Instance.gameObject);
        }

        GameObject dialogueObject = new GameObject("TestDialogueSystem_PreBoss");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();

        // DialogueSystem registers itself in Awake, which AddComponent does not
        // run in edit mode. Awake also calls DontDestroyOnLoad, which throws
        // outside play mode; Instance is then set directly, mirroring
        // DialogueEffectVerificationTest.
        try
        {
            InvokePrivate(dialogue, "Awake");
        }
        catch (System.Exception)
        {
            PropertyInfo instance = typeof(DialogueSystem).GetProperty(
                "Instance", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(instance, "DialogueSystem.Instance should exist.");
            instance.GetSetMethod(true).Invoke(dialogue, new object[] { dialogue });
        }

        Assert.AreSame(dialogue, DialogueSystem.Instance, "DialogueSystem.Instance should exist.");
        return dialogue;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
        method.Invoke(target, null);
    }

    private static IslandConfig CreateBossIslandConfig(string islandId = "island_greed")
    {
        IslandConfig config = ScriptableObject.CreateInstance<IslandConfig>();
        config.islandId = islandId;
        config.viceName = "Greed";
        config.encounters = new[]
        {
            new EncounterDefinition
            {
                encounterId = $"{islandId}_boss",
                type = EncounterType.Combat,
                isBossEncounter = true,
                restorationValue = 0.5f
            }
        };
        return config;
    }

    private static void InvokeLoadCombatEncounter(IslandFlowController controller, EncounterDefinition encounter)
    {
        MethodInfo method = typeof(IslandFlowController).GetMethod(
            "LoadCombatEncounter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "IslandFlowController.LoadCombatEncounter should exist.");
        method.Invoke(controller, new object[] { encounter });
    }

    private static void InvokeCancelSceneDialogue(DialogueSystem dialogue)
    {
        // Mirrors DialogueSystem.HandleSceneLoaded, which unsubscribes the
        // completion handler and clears isDialogueActive without firing
        // OnDialogueTreeCompleted. SceneManager.CreateScene is play-mode
        // only, so any scene handle works as the argument is unused.
        MethodInfo method = typeof(DialogueSystem).GetMethod(
            "HandleSceneLoaded", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "DialogueSystem.HandleSceneLoaded should exist.");
        method.Invoke(dialogue, new object[] { default(UnityEngine.SceneManagement.Scene), UnityEngine.SceneManagement.LoadSceneMode.Single });
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private void Cleanup()
    {
        if (DialogueSystem.Instance != null)
        {
            Object.DestroyImmediate(DialogueSystem.Instance.gameObject);
        }

        if (controllerObject != null)
        {
            Object.DestroyImmediate(controllerObject);
            controllerObject = null;
        }

        if (trackerObject != null)
        {
            Object.DestroyImmediate(trackerObject);
            trackerObject = null;
        }

        if (gameStateObject != null)
        {
            Object.DestroyImmediate(gameStateObject);
            gameStateObject = null;
        }

        if (islandConfig != null)
        {
            Object.DestroyImmediate(islandConfig);
            islandConfig = null;
        }
    }
}
#endif
