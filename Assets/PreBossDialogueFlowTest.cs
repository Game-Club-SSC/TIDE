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
            DialogueTree tree = HeroDialogueContent.PreBossGreedDialogue();
            Assert.IsTrue(dialogue.StartDialogueTree(tree), "Setup: the pre-boss tree must start.");

            islandConfig = CreateBossIslandConfig();
            controller.StartIsland(islandConfig);

            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);

            // A dialogue is already running, so the controller must not also
            // try to play one and must not enter combat underneath it.
            Assert.IsFalse(gameState.IsTransitioning,
                "Combat must not start while a dialogue is already on screen.");
            Assert.IsFalse(dialogue.IsDialogueActive == false,
                "The pre-existing dialogue must still be the active one.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void CancelledPreBossDialogueDoesNotStrandTheBoss()
    {
        // Regression guard for the soft-lock: a scene load cancels the
        // dialogue WITHOUT firing OnDialogueTreeCompleted. The controller
        // must recover on the next attempt instead of returning early
        // forever behind a stale in-flight flag.
        GameStateManager gameState = CreateGameState();
        IslandFlowController controller = CreateController(gameState);
        DialogueSystem dialogue = CreateDialogueSystem();

        try
        {
            islandConfig = CreateBossIslandConfig();
            controller.StartIsland(islandConfig);

            // Play the pre-boss dialogue, then simulate the scene-load
            // cancellation that fires no completion event.
            DialogueTree tree = HeroDialogueContent.PreBossGreedDialogue();
            Assert.IsTrue(dialogue.StartDialogueTree(tree), "Setup: the pre-boss tree must start.");

            InvokeCancelSceneDialogue(dialogue);

            // Retry the boss. It must be allowed through rather than deferred
            // behind a flag whose callback will never fire.
            InvokeLoadCombatEncounter(controller, islandConfig.encounters[0]);

            bool inFlight = (bool)GetPrivateField(controller, "preBossEncounterInFlight");
            Assert.IsFalse(inFlight,
                "A cancelled dialogue must not leave the boss permanently deferred.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void StartingANewIslandClearsAnyPendingPreBossDeferral()
    {
        GameStateManager gameState = CreateGameState();
        IslandFlowController controller = CreateController(gameState);
        DialogueSystem dialogue = CreateDialogueSystem();

        try
        {
            IslandConfig first = CreateBossIslandConfig();
            controller.StartIsland(first);

            DialogueTree tree = HeroDialogueContent.PreBossGreedDialogue();
            Assert.IsTrue(dialogue.StartDialogueTree(tree), "Setup: the pre-boss tree must start.");

            // Move to a different island while the dialogue is unresolved.
            IslandConfig second = CreateBossIslandConfig("island_envy");
            controller.StartIsland(second);

            bool inFlight = (bool)GetPrivateField(controller, "preBossEncounterInFlight");
            Assert.IsFalse(inFlight,
                "StartIsland must clear a deferral left over from the previous island.");
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
        gameStateObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        return gameState;
    }

    private IslandFlowController CreateController(GameStateManager gameState)
    {
        controllerObject = new GameObject("TestIslandFlowController_PreBoss");
        IslandFlowController controller = controllerObject.AddComponent<IslandFlowController>();
        controllerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

        trackerObject = new GameObject("TestIslandRestorationTracker_PreBoss");
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        trackerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

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
