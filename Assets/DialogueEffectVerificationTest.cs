using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode tests for issue #297 — authored dialogue effects wired to durable
/// gameplay state. Covers: effects actually mutating state (XP, story flags,
/// gear rewards, Tide Break unlocks), the pending-reward ledger for missing
/// services, the additive "dialogueState" save section round-trip through the
/// real GameStateManager save pipeline, replay protection (no double rewards),
/// and legacy save compatibility.
/// </summary>
public class DialogueEffectVerificationTest
{
    private const string WorldStateSaveKey = "TIDE_WORLD_STATE_V1";

    private GameObject progressionObject;
    private GameObject inventoryObject;
    private GameObject storyObject;
    private GameObject tideBreakObject;
    private GameObject dialogueObject;
    private readonly List<GearSetData> createdGearSets = new List<GearSetData>();

    [SetUp]
    public void SetUp()
    {
        CleanupSingletons();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupSingletons();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [Test]
    public void NarrativeEffectsUpdateDurableStateAndSurviveSaveLoad()
    {
        CreateServices();

        HeroProgressionManager progression = HeroProgressionManager.Instance;
        PlayerGearInventory inventory = PlayerGearInventory.Instance;
        StoryProgressionService story = StoryProgressionService.Instance;
        TideBreakProgressionManager tideBreaks = TideBreakProgressionManager.Instance;
        DialogueSystem dialogue = DialogueSystem.Instance;

        Assert.IsNotNull(progression, "Setup: HeroProgressionManager must exist.");
        Assert.IsNotNull(inventory, "Setup: PlayerGearInventory must exist.");
        Assert.IsNotNull(story, "Setup: StoryProgressionService must exist.");
        Assert.IsNotNull(tideBreaks, "Setup: TideBreakProgressionManager must exist.");
        Assert.IsNotNull(dialogue, "Setup: DialogueSystem must exist.");

        GameObject gsmObject = new GameObject("TestGameStateManager_Dialogue297");
        GameStateManager gsm = gsmObject.AddComponent<GameStateManager>();
        DialogueTreeRunner runner = null;
        GameObject runnerObject = null;
        try
        {
            // One node carrying all four durable effect types.
            DialogueTree tree = new DialogueTree { treeId = "tree_e2e_narrative" };
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = "node_all_effects",
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = "Narrator",
                    dialogueText = "The old texts stir.",
                    relatedHeroId = "hero_fire"
                },
                effects = new DialogueTreeEffect[]
                {
                    new DialogueTreeEffect { type = DialogueEffectType.GrantXP, targetId = "hero_fire", intValue = 50 },
                    new DialogueTreeEffect { type = DialogueEffectType.SetFlag, targetId = "flag_merchant_helped", intValue = 1 },
                    new DialogueTreeEffect { type = DialogueEffectType.GiveItem, targetId = "iron_guard", intValue = 1 },
                    new DialogueTreeEffect { type = DialogueEffectType.UnlockTideBreak, targetId = "Inferno Surge", intValue = 1 }
                }
            };
            tree.rootNode = node;
            tree.allNodes = new List<DialogueTreeNode> { node };

            // Run the tree's effects — the exact code path ShowNode executes
            // when the tree is started (tree + currentNode are wired the way
            // StartTree wires them before the UI walk).
            runnerObject = new GameObject("DialogueTreeRunner_E2E");
            runner = runnerObject.AddComponent<DialogueTreeRunner>();
            SetPrivateField(runner, "tree", tree);
            SetPrivateField(runner, "currentNode", node);
            InvokeApplyEffects(runner, node);

            Assert.AreEqual(50, progression.GetXp("hero_fire"),
                "GrantXP must actually grant XP to the hero's progression state.");
            Assert.IsTrue(story.GetFlag("flag_merchant_helped"),
                "SetFlag must actually set the story flag.");
            Assert.AreEqual(1, inventory.OwnedGearCount,
                "GiveItem must actually award an owned gear instance.");
            Assert.AreEqual("iron_guard", inventory.GetOwnedGear()[0].setId,
                "Awarded gear must match the effect's item id.");
            Assert.IsTrue(tideBreaks.HasTideBreak("hero_fire", "Inferno Surge"),
                "UnlockTideBreak must actually register the unlock.");

            // Persist through the real save pipeline.
            gsm.SaveWorldState();
            Assert.IsTrue(PlayerPrefs.HasKey(WorldStateSaveKey), "Save must write the world state.");
            string payload = PlayerPrefs.GetString(WorldStateSaveKey, string.Empty);
            StringAssert.Contains("dialogueState", payload,
                "The save payload must carry the additive dialogueState section.");

            // Fresh session: wipe every singleton, recreate services, load.
            CleanupSingletons();
            CreateServices();

            progression = HeroProgressionManager.Instance;
            inventory = PlayerGearInventory.Instance;
            story = StoryProgressionService.Instance;
            tideBreaks = TideBreakProgressionManager.Instance;

            GameObject reloadObject = new GameObject("TestGameStateManager_Dialogue297_Reload");
            GameStateManager reloadManager = reloadObject.AddComponent<GameStateManager>();
            try
            {
                reloadManager.LoadWorldState();

                Assert.AreEqual(50, progression.GetXp("hero_fire"),
                    "XP must survive the save/load round-trip.");
                Assert.IsTrue(story.GetFlag("flag_merchant_helped"),
                    "Story flags must survive the save/load round-trip.");
                Assert.AreEqual(1, inventory.OwnedGearCount,
                    "Gear ownership must survive the save/load round-trip.");
                Assert.AreEqual("iron_guard", inventory.GetOwnedGear()[0].setId,
                    "Restored gear must keep its set id.");
                Assert.IsTrue(tideBreaks.HasTideBreak("hero_fire", "Inferno Surge"),
                    "Tide Break unlocks must survive the save/load round-trip.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reloadObject);
            }

            // Re-running the tree must not double-deliver one-shot rewards.
            DialogueTreeRunner runner2 = null;
            GameObject runnerObject2 = new GameObject("DialogueTreeRunner_E2E_Replay");
            try
            {
                runner2 = runnerObject2.AddComponent<DialogueTreeRunner>();
                SetPrivateField(runner2, "tree", tree);
                SetPrivateField(runner2, "currentNode", node);
                InvokeApplyEffects(runner2, node);

                Assert.AreEqual(50, progression.GetXp("hero_fire"),
                    "Re-running the tree must not grant XP twice.");
                Assert.AreEqual(1, inventory.OwnedGearCount,
                    "Re-running the tree must not award gear twice.");
                Assert.AreEqual(1, inventory.GetOwnedGear().Count,
                    "Re-running the tree must not duplicate owned gear.");
            }
            finally
            {
                if (runnerObject2 != null) UnityEngine.Object.DestroyImmediate(runnerObject2);
            }
        }
        finally
        {
            if (runnerObject != null) UnityEngine.Object.DestroyImmediate(runnerObject);
            if (gsmObject != null) UnityEngine.Object.DestroyImmediate(gsmObject);
        }
    }

    [Test]
    public void MissingServiceEffectsAreQueuedNotDiscarded()
    {
        // DialogueSystem present, but NO HeroProgressionManager.
        dialogueObject = new GameObject("DialogueSystem_Test");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();
        InvokeOnEnableIfUnregistered(dialogue, () => DialogueSystem.Instance);
        try
        {
            DialogueTreeEffect xpEffect = new DialogueTreeEffect
            {
                type = DialogueEffectType.GrantXP,
                targetId = "hero_fire",
                intValue = 50
            };

            bool applied = dialogue.ApplyDialogueEffect(xpEffect, "hero_fire", "tree_missing_service", "node_xp", 0);

            Assert.IsFalse(applied,
                "GrantXP with no HeroProgressionManager must report that it could not be applied.");
            Assert.IsFalse(dialogue.HasGrantedReward("tree_missing_service|node_xp|0"),
                "A failed effect must not be marked as granted.");

            DialogueSystem.DialogueStateSaveData saveData = dialogue.CaptureDialogueStateSaveData();
            Assert.AreEqual(1, saveData.pendingRewards.Count,
                "The failed effect must be recorded in the pending ledger, never silently discarded.");
            Assert.AreEqual((int)DialogueEffectType.GrantXP, saveData.pendingRewards[0].effectType,
                "Pending entry must carry the effect type.");
            Assert.AreEqual("hero_fire", saveData.pendingRewards[0].targetId,
                "Pending entry must carry the effect's target id.");
            Assert.AreEqual(50, saveData.pendingRewards[0].intValue,
                "Pending entry must carry the effect's int value.");
            Assert.AreEqual("tree_missing_service", saveData.pendingRewards[0].treeId,
                "Pending entry must carry the tree id.");
        }
        finally
        {
            if (dialogueObject != null) UnityEngine.Object.DestroyImmediate(dialogueObject);
            dialogueObject = null;
        }
    }

    [Test]
    public void PendingRewardsRetriedWhenServicesAppear()
    {
        dialogueObject = new GameObject("DialogueSystem_Test");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();
        InvokeOnEnableIfUnregistered(dialogue, () => DialogueSystem.Instance);
        try
        {
            dialogue.ApplyDialogueEffect(
                new DialogueTreeEffect { type = DialogueEffectType.GrantXP, targetId = "hero_fire", intValue = 50 },
                "hero_fire", "tree_pending", "node_xp", 0);

            DialogueSystem.DialogueStateSaveData saveData = dialogue.CaptureDialogueStateSaveData();
            Assert.AreEqual(1, saveData.pendingRewards.Count, "Setup: the effect must start pending.");

            // The service appears before the save data is applied.
            progressionObject = new GameObject("HeroProgressionManager_Test");
            HeroProgressionManager progression = progressionObject.AddComponent<HeroProgressionManager>();
            InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
            SetLevelingConfig(progression, CreateDefaultLevelingConfig());

            dialogue.ApplyDialogueStateSaveData(saveData);

            Assert.AreEqual(50, progression.GetXp("hero_fire"),
                "Pending XP reward must be delivered once the service is available.");

            DialogueSystem.DialogueStateSaveData after = dialogue.CaptureDialogueStateSaveData();
            Assert.AreEqual(0, after.pendingRewards.Count,
                "Delivered pending rewards must leave the ledger.");
            Assert.IsTrue(after.grantedRewardKeys.Contains("tree_pending|node_xp|0"),
                "Delivered rewards must move to the granted ledger.");
        }
        finally
        {
            if (dialogueObject != null) UnityEngine.Object.DestroyImmediate(dialogueObject);
            dialogueObject = null;
        }
    }

    [Test]
    public void DuplicatePendingRewardsAreDeliveredOnlyOnce()
    {
        CreateServices();

        DialogueSystem dialogue = DialogueSystem.Instance;
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        Assert.IsNotNull(dialogue, "Setup: DialogueSystem must exist.");
        Assert.IsNotNull(progression, "Setup: HeroProgressionManager must exist.");

        DialogueSystem.DialoguePendingRewardEntry pending = new DialogueSystem.DialoguePendingRewardEntry
        {
            treeId = "tree_duplicate_pending",
            nodeId = "node_xp",
            effectIndex = 0,
            effectType = (int)DialogueEffectType.GrantXP,
            targetId = "hero_fire",
            intValue = 50,
            heroId = "hero_fire"
        };
        DialogueSystem.DialogueStateSaveData saveData = new DialogueSystem.DialogueStateSaveData
        {
            pendingRewards = new List<DialogueSystem.DialoguePendingRewardEntry> { pending, pending }
        };

        dialogue.ApplyDialogueStateSaveData(saveData);

        Assert.AreEqual(50, progression.GetXp("hero_fire"),
            "Duplicate pending entries for one reward key must be delivered once.");
        DialogueSystem.DialogueStateSaveData captured = dialogue.CaptureDialogueStateSaveData();
        Assert.AreEqual(0, captured.pendingRewards.Count,
            "A successfully delivered duplicate must not remain pending.");
        Assert.IsTrue(captured.grantedRewardKeys.Contains("tree_duplicate_pending|node_xp|0"),
            "The delivered pending reward must enter the granted ledger.");
    }

    [Test]
    public void DialogueStateSaveSectionJsonRoundTrips()
    {
        CreateServices();

        DialogueSystem dialogue = DialogueSystem.Instance;
        Assert.IsNotNull(dialogue, "Setup: DialogueSystem must exist.");

        dialogue.ApplyDialogueEffect(
            new DialogueTreeEffect { type = DialogueEffectType.SetFlag, targetId = "flag_chosen", intValue = 1 },
            null, "tree_json", "node_flag", 0);
        dialogue.ApplyDialogueEffect(
            new DialogueTreeEffect { type = DialogueEffectType.UnlockTideBreak, targetId = "Inferno Surge", intValue = 1 },
            "hero_fire", "tree_json", "node_tidebreak", 1);
        dialogue.IncreaseBond("hero_fire", "hero_water", 65);

        DialogueSystem.DialogueStateSaveData saveData = dialogue.CaptureDialogueStateSaveData();
        string json = JsonUtility.ToJson(saveData);
        StringAssert.Contains("setFlagIds", json, "dialogueState must serialize its flag list.");
        StringAssert.Contains("unlockedTideBreakHeroIds", json, "dialogueState must serialize unlock hero ids.");
        StringAssert.Contains("unlockedTideBreakNames", json, "dialogueState must serialize unlock names.");
        StringAssert.Contains("grantedRewardKeys", json, "dialogueState must serialize the granted ledger.");
        StringAssert.Contains("pendingRewards", json, "dialogueState must serialize the pending ledger.");
        StringAssert.Contains("bondKeys", json, "dialogueState must serialize relationship pair keys.");
        StringAssert.Contains("bondLevels", json, "dialogueState must serialize relationship levels.");

        DialogueSystem.DialogueStateSaveData restored =
            JsonUtility.FromJson<DialogueSystem.DialogueStateSaveData>(json);
        Assert.IsNotNull(restored, "dialogueState JSON should deserialize.");
        Assert.AreEqual(1, restored.setFlagIds.Count, "Flag ids must round-trip.");
        Assert.AreEqual("flag_chosen", restored.setFlagIds[0], "Flag id must match.");
        Assert.AreEqual(1, restored.grantedRewardKeys.Count, "Granted reward keys must round-trip.");
        Assert.IsTrue(restored.grantedRewardKeys.Contains("tree_json|node_tidebreak|1"),
            "UnlockTideBreak must be recorded as a granted one-shot reward.");
        Assert.AreEqual(1, restored.unlockedTideBreakNames.Count, "Tide Break names must round-trip.");
        Assert.AreEqual("Inferno Surge", restored.unlockedTideBreakNames[0], "Tide Break name must match.");
        Assert.AreEqual("hero_fire", restored.unlockedTideBreakHeroIds[0], "Tide Break hero must match.");
        Assert.AreEqual(0, restored.pendingRewards.Count, "Applied effects must not be pending.");
        Assert.AreEqual(1, restored.bondKeys.Count, "Relationship keys must round-trip.");
        Assert.AreEqual(DialogueSystem.MakeBondKey("hero_fire", "hero_water"), restored.bondKeys[0],
            "Relationship pairs must use the canonical sorted key.");
        Assert.AreEqual(65, restored.bondLevels[0], "Relationship levels must round-trip.");

        dialogue.ResetDialogueStateForDebug();
        Assert.AreEqual(0, dialogue.GetBondLevel("hero_fire", "hero_water"),
            "Reset must clear relationship state before restore.");
        dialogue.ApplyDialogueStateSaveData(restored);
        Assert.AreEqual(65, dialogue.GetBondLevel("hero_fire", "hero_water"),
            "Applying dialogue save data must restore relationship state.");
    }

    [Test]
    public void NewGameResetClearsDialogueLedgerAndStoryFlags()
    {
        CreateServices();

        DialogueSystem dialogue = DialogueSystem.Instance;
        StoryProgressionService story = StoryProgressionService.Instance;
        Assert.IsNotNull(dialogue, "Setup: DialogueSystem must exist.");
        Assert.IsNotNull(story, "Setup: StoryProgressionService must exist.");

        dialogue.ApplyDialogueEffect(
            new DialogueTreeEffect { type = DialogueEffectType.SetFlag, targetId = "flag_old_game", intValue = 1 },
            null, "tree_reset", "node_reset", 0);
        Assert.IsTrue(story.GetFlag("flag_old_game"), "Setup: the flag must be live before the reset.");

        GameObject gsmObject = new GameObject("TestGameStateManager_Reset");
        GameStateManager gsm = gsmObject.AddComponent<GameStateManager>();
        try
        {
            gsm.ResetRuntimeWorldStateForDebug();

            Assert.IsFalse(dialogue.IsDialogueFlagSet("flag_old_game"),
                "New-game reset must clear the dialogue ledger flags.");
            Assert.IsFalse(story.GetFlag("flag_old_game"),
                "New-game reset must clear the live story flags so they cannot leak into a fresh playthrough.");
        }
        finally
        {
            if (gsmObject != null) UnityEngine.Object.DestroyImmediate(gsmObject);
        }
    }

    [Test]
    public void LegacySaveJsonWithoutDialogueStateStillLoads()
    {
        const string legacyJson = "{\"puzzleStates\":[],\"ancientTextStates\":[],\"completedNarrativeBeatIds\":[],\"restorationSnapshot\":null,\"gearProgression\":null,\"progressionSnapshot\":null,\"storyProgression\":null,\"ceremonyIntroCompleted\":false}";
        Assert.IsFalse(legacyJson.Contains("dialogueState"),
            "Legacy save JSON must not contain the dialogueState section.");

        GameStateManager.WorldStateSaveData parsed =
            JsonUtility.FromJson<GameStateManager.WorldStateSaveData>(legacyJson);
        Assert.IsNotNull(parsed, "Legacy save JSON should still deserialize cleanly with the new field.");

        // Unity 6 JsonUtility materializes absent class-typed fields as empty
        // instances, so the backward-compat contract is "empty and safe to
        // apply", not "null".
        Assert.IsNotNull(parsed.dialogueState,
            "An absent dialogueState must deserialize as an empty instance on Unity 6.");
        Assert.AreEqual(0, parsed.dialogueState.setFlagIds.Count,
            "Legacy saves carry no dialogue flags.");
        Assert.AreEqual(0, parsed.dialogueState.unlockedTideBreakNames.Count,
            "Legacy saves carry no Tide Break unlocks.");
        Assert.AreEqual(0, parsed.dialogueState.grantedRewardKeys.Count,
            "Legacy saves carry no granted rewards.");
        Assert.AreEqual(0, parsed.dialogueState.pendingRewards.Count,
            "Legacy saves carry no pending rewards.");
        Assert.AreEqual(0, parsed.dialogueState.bondKeys.Count,
            "Legacy saves carry no relationship keys.");
        Assert.AreEqual(0, parsed.dialogueState.bondLevels.Count,
            "Legacy saves carry no relationship levels.");

        // Applying the empty section must be a safe no-op.
        dialogueObject = new GameObject("DialogueSystem_Test");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();
        InvokeOnEnableIfUnregistered(dialogue, () => DialogueSystem.Instance);
        try
        {
            dialogue.ApplyDialogueStateSaveData(parsed.dialogueState);
            dialogue.ApplyDialogueStateSaveData(null);
            Assert.IsFalse(dialogue.IsDialogueFlagSet("anything"),
                "Applying a legacy (empty) dialogueState must not fabricate flags.");
        }
        finally
        {
            if (dialogueObject != null) UnityEngine.Object.DestroyImmediate(dialogueObject);
            dialogueObject = null;
        }
    }

    // ------------------------------------------------------------------ //
    //  Service scaffolding
    // ------------------------------------------------------------------ //

    private void CreateServices()
    {
        progressionObject = new GameObject("HeroProgressionManager_Test");
        HeroProgressionManager progression = progressionObject.AddComponent<HeroProgressionManager>();
        InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
        SetLevelingConfig(progression, CreateDefaultLevelingConfig());

        GearSetData gearSet = CreateTestGearSet("iron_guard");
        createdGearSets.Add(gearSet);
        SetAvailableGearSets(progression, new[] { gearSet });

        inventoryObject = new GameObject("PlayerGearInventory_Test");
        PlayerGearInventory inventory = inventoryObject.AddComponent<PlayerGearInventory>();
        InvokeOnEnableIfUnregistered(inventory, () => PlayerGearInventory.Instance);

        storyObject = new GameObject("StoryProgressionService_Test");
        StoryProgressionService story = storyObject.AddComponent<StoryProgressionService>();
        InvokeOnEnableIfUnregistered(story, () => StoryProgressionService.Instance);

        tideBreakObject = new GameObject("TideBreakProgressionManager_Test");
        TideBreakProgressionManager tideBreaks = tideBreakObject.AddComponent<TideBreakProgressionManager>();
        InvokeOnEnableIfUnregistered(tideBreaks, () => TideBreakProgressionManager.Instance);

        dialogueObject = new GameObject("DialogueSystem_Test");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();
        InvokeOnEnableIfUnregistered(dialogue, () => DialogueSystem.Instance);
    }

    private void CleanupSingletons()
    {
        if (dialogueObject != null)
        {
            UnityEngine.Object.DestroyImmediate(dialogueObject);
            dialogueObject = null;
        }

        if (tideBreakObject != null)
        {
            UnityEngine.Object.DestroyImmediate(tideBreakObject);
            tideBreakObject = null;
        }

        if (storyObject != null)
        {
            UnityEngine.Object.DestroyImmediate(storyObject);
            storyObject = null;
        }

        if (inventoryObject != null)
        {
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            inventoryObject = null;
        }

        if (progressionObject != null)
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            progressionObject = null;
        }

        if (PlayerGearInventory.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(PlayerGearInventory.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (StoryProgressionService.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(StoryProgressionService.Instance.gameObject);
        }

        if (TideBreakProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(TideBreakProgressionManager.Instance.gameObject);
        }

        if (DialogueSystem.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(DialogueSystem.Instance.gameObject);
        }

        for (int i = 0; i < createdGearSets.Count; i++)
        {
            if (createdGearSets[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdGearSets[i]);
            }
        }

        createdGearSets.Clear();
    }

    private static void InvokeApplyEffects(DialogueTreeRunner runner, DialogueTreeNode node)
    {
        MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
            "ApplyEffects", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "DialogueTreeRunner.ApplyEffects should exist.");
        method.Invoke(runner, new object[] { node });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    /// <summary>
    /// Edit-mode batchmode does not fire Awake/OnEnable synchronously on
    /// AddComponent, so invoke the lifecycle callback directly; if that throws
    /// (e.g. DontDestroyOnLoad outside play mode), register the singleton via
    /// its backing field instead.
    /// </summary>
    /// <summary>
    /// Edit-mode batchmode does not fire Awake/OnEnable synchronously on
    /// AddComponent, so invoke the lifecycle callback directly; if that throws
    /// (e.g. DontDestroyOnLoad outside play mode), register the singleton via
    /// its backing field instead. The getter returns the typed MonoBehaviour so
    /// the null check uses Unity's destroyed-object semantics.
    /// </summary>
    private static void InvokeOnEnableIfUnregistered(MonoBehaviour component, Func<MonoBehaviour> instanceGetter)
    {
        if (component == null || instanceGetter() != null)
        {
            return;
        }

        MethodInfo lifecycle = component.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        if (lifecycle == null)
        {
            lifecycle = component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (lifecycle != null)
        {
            try
            {
                lifecycle.Invoke(component, null);
            }
            catch (Exception ex)
            {
                Exception root = ex;
                while (root is TargetInvocationException && root.InnerException != null)
                {
                    root = root.InnerException;
                }

                Debug.LogWarning($"[DialogueEffectVerificationTest] Lifecycle invocation of {component.GetType().Name} failed ({root.GetType().Name}: {root.Message}); registering singleton directly.");
            }
        }

        if (instanceGetter() == null)
        {
            FieldInfo backing = component.GetType().GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            if (backing != null)
            {
                backing.SetValue(null, component);
            }
        }
    }

    private static LevelingConfig CreateDefaultLevelingConfig()
    {
        LevelingConfig config = ScriptableObject.CreateInstance<LevelingConfig>();
        config.baseXpToLevel = 100;
        config.xpPerLevelIncrement = 50;
        config.hpPerLevel = 5;
        config.mpPerLevel = 2;
        config.attackPerLevel = 1;
        config.defensePerLevel = 1;
        config.speedPerLevel = 1;
        config.reserveXpMultiplier = 0.5f;
        config.maxLevel = 20;
        return config;
    }

    private static void SetLevelingConfig(HeroProgressionManager manager, LevelingConfig config)
    {
        FieldInfo field = typeof(HeroProgressionManager).GetField("levelingConfig", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "levelingConfig field should exist on HeroProgressionManager.");
        field.SetValue(manager, config);
    }

    private static void SetAvailableGearSets(HeroProgressionManager manager, GearSetData[] gearSets)
    {
        FieldInfo field = typeof(HeroProgressionManager).GetField("availableGearSets", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "availableGearSets field should exist on HeroProgressionManager.");
        field.SetValue(manager, gearSets);
    }

    private static GearSetData CreateTestGearSet(string setId)
    {
        GearSetData gear = ScriptableObject.CreateInstance<GearSetData>();
        gear.setId = setId;
        gear.displayName = "Iron Guard Set";
        gear.description = "Heavy iron armor for testing.";
        gear.attackBonusPercent = 0.05f;
        gear.defenseBonusPercent = 0.10f;
        gear.hpBonusPercent = 0.10f;
        gear.setBonusAttackPercent = 0.05f;
        gear.setBonusDefensePercent = 0.10f;
        gear.setBonusHpPercent = 0.10f;
        gear.setBonusDescription = "Iron Resolve: +5% ATK, +10% DEF, +10% HP";
        return gear;
    }
}
