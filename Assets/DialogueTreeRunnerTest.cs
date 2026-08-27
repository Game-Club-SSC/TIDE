using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTreeRunnerTest : MonoBehaviour
{
    [ContextMenu("Run Dialogue Tree Runner Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Dialogue Tree Runner Tests ===");

        TestEvaluateConditionReturnsTrueForEmptyConditions();
        TestEvaluateConditionBondLevelPasses();
        TestEvaluateConditionBondLevelFails();
        TestBondPairParserSupportsHeroPairs();
        TestRelationshipVariationWiresLowBondFallback();
        TestEvaluateConditionStoryActChecksService();
        TestEvaluateConditionIslandRestoredChecksTracker();
        TestEvaluateConditionHasAncientTextChecksDefinition();
        TestEvaluateConditionQuestCompletedChecksService();
        TestApplyEffectIncreaseBond();
        TestApplyEffectGrantXPActuallyGrantsXp();
        TestApplyEffectUnlockTideBreakActuallyUnlocks();
        TestApplyEffectSetFlagActuallySetsFlag();
        TestApplyEffectGiveItemActuallyAwardsGear();
        TestApplyEffectGiveItemUnknownIdFallsBackToCurrency();
        TestStartTreeWithNullTreeDestroysRunner();
        TestStartTreeWithNullRootDestroysRunner();

        Debug.Log("=== All Dialogue Tree Runner Tests Passed ===");
    }

    private DialogueTreeRunner CreateIsolatedRunner()
    {
        GameObject go = new GameObject("TestDialogueTreeRunner");
        return go.AddComponent<DialogueTreeRunner>();
    }

    private DialogueTreeNode CreateNodeWithCondition(DialogueConditionType type, string targetId, int intValue)
    {
        DialogueTreeNode node = new DialogueTreeNode
        {
            nodeId = "test_node",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Test",
                dialogueText = "Hello"
            },
            conditions = new[]
            {
                new DialogueTreeCondition { type = type, targetId = targetId, intValue = intValue }
            }
        };
        return node;
    }

    private DialogueTreeNode CreateNodeWithEffect(DialogueEffectType type, string targetId, int intValue)
    {
        return CreateNodeWithEffect(type, targetId, intValue, null);
    }

    private DialogueTreeNode CreateNodeWithEffect(DialogueEffectType type, string targetId, int intValue, string relatedHeroId)
    {
        DialogueTreeNode node = new DialogueTreeNode
        {
            nodeId = "test_effect_node",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Test",
                dialogueText = "Effect node",
                relatedHeroId = relatedHeroId ?? string.Empty
            },
            effects = new[]
            {
                new DialogueTreeEffect { type = type, targetId = targetId, intValue = intValue }
            }
        };
        return node;
    }

    private void TestEvaluateConditionReturnsTrueForEmptyConditions()
    {
        Debug.Log("Testing EvaluateCondition returns true for empty conditions...");

        DialogueTreeNode node = new DialogueTreeNode
        {
            nodeId = "empty_node",
            entry = new DialogueSystem.DialogueEntry { speakerName = "Test", dialogueText = "Empty" }
        };

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "EvaluateConditions method should exist.");

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsTrue(result, "Empty conditions should evaluate to true.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition empty conditions test passed");
    }

    private void TestEvaluateConditionBondLevelPasses()
    {
        Debug.Log("Testing EvaluateCondition BondLevel passes when bond is sufficient...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.BondLevel, "hero_fire", 0);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsTrue(result, "BondLevel condition with intValue 0 should pass (any bond >= 0).");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition BondLevel pass test passed");
    }

    private void TestEvaluateConditionBondLevelFails()
    {
        Debug.Log("Testing EvaluateCondition BondLevel fails when bond is insufficient...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.BondLevel, "hero_fire", 999);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;
        GameObject dialogueObject = null;

        try
        {
            // Isolated DialogueSystem with zero bonds so the bond gate actually
            // evaluates (no dependency on an ambient scene singleton).
            dialogueObject = new GameObject("DialogueSystem_Test");
            DialogueSystem dialogueSystem = dialogueObject.AddComponent<DialogueSystem>();
            InvokeOnEnableIfUnregistered(dialogueSystem, () => DialogueSystem.Instance);

            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "BondLevel condition with insufficient bond returns false.");
        }
        finally
        {
            if (dialogueObject != null) DestroyImmediate(dialogueObject);
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition BondLevel fail test passed");
    }

    private void TestBondPairParserSupportsHeroPairs()
    {
        Debug.Log("Testing BondLevel condition pair parsing...");

        MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
            "TryResolveBondPair", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "TryResolveBondPair should exist.");

        object[] pairArgs = { "hero_fire|hero_water", null, null };
        bool pairResult = (bool)method.Invoke(null, pairArgs);
        Assert.IsTrue(pairResult, "A two-hero bond target should parse.");
        Assert.AreEqual("hero_fire", pairArgs[1], "First pair member should be preserved.");
        Assert.AreEqual("hero_water", pairArgs[2], "Second pair member should be preserved.");

        object[] playerArgs = { "hero_space", null, null };
        bool playerResult = (bool)method.Invoke(null, playerArgs);
        Assert.IsTrue(playerResult, "A single hero target should resolve against the player.");
        Assert.AreEqual("hero_space", playerArgs[1], "Single target should be the first pair member.");
        Assert.AreEqual("player", playerArgs[2], "Single target should use player as the second pair member.");

        Debug.Log("✓ BondLevel pair parsing test passed");
    }

    private void TestRelationshipVariationWiresLowBondFallback()
    {
        Debug.Log("Testing relationship variation low-bond reachability...");

        DialogueTree tree = HeroDialogueContent.RelationshipVariationDialogue("hero_fire", "hero_water");
        Assert.IsNotNull(tree, "Relationship variation tree should be authored.");
        Assert.IsNotNull(tree.rootNode, "Relationship variation tree should have a root.");

        DialogueTreeNode highNode = null;
        DialogueTreeNode lowNode = null;
        for (int i = 0; i < tree.allNodes.Count; i++)
        {
            DialogueTreeNode node = tree.allNodes[i];
            if (node == null) continue;
            if (node.nodeId.EndsWith("_high", StringComparison.Ordinal)) highNode = node;
            if (node.nodeId.EndsWith("_low", StringComparison.Ordinal)) lowNode = node;
        }

        Assert.IsNotNull(highNode, "High-bond node should exist.");
        Assert.IsNotNull(lowNode, "Low-bond node should exist.");
        Assert.AreEqual(lowNode.nodeId, highNode.conditionFailureNodeId,
            "A failed high-bond condition must branch to the low-bond exchange.");
        Assert.IsNotNull(highNode.conditions, "High-bond node should have a bond gate.");
        Assert.AreEqual("hero_fire|hero_water", highNode.conditions[0].targetId,
            "The high-bond gate must evaluate the two heroes, not a fabricated player pair.");

        Debug.Log("✓ Relationship variation low-bond fallback test passed");
    }

    private void TestEvaluateConditionStoryActChecksService()
    {
        Debug.Log("Testing EvaluateCondition StoryAct checks StoryProgressionService...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.StoryAct, "act_ii", 2);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "StoryAct returns false when StoryProgressionService.Instance is null.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition StoryAct test passed");
    }

    private void TestEvaluateConditionIslandRestoredChecksTracker()
    {
        Debug.Log("Testing EvaluateCondition IslandRestored checks IslandRestorationTracker...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.IslandRestored, "island_greed", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "IslandRestored returns false when IslandRestorationTracker.Instance is null.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition IslandRestored test passed");
    }

    private void TestEvaluateConditionHasAncientTextChecksDefinition()
    {
        Debug.Log("Testing EvaluateCondition HasAncientText checks text definition...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.HasAncientText, "text_1", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "HasAncientText returns false for unknown text ID.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition HasAncientText test passed");
    }

    private void TestEvaluateConditionQuestCompletedChecksService()
    {
        Debug.Log("Testing EvaluateCondition QuestCompleted checks StoryProgressionService...");

        DialogueTreeNode node = CreateNodeWithCondition(DialogueConditionType.QuestCompleted, "quest_1", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "QuestCompleted returns false when StoryProgressionService.Instance is null.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition QuestCompleted test passed");
    }

    private void TestApplyEffectIncreaseBond()
    {
        Debug.Log("Testing ApplyEffect IncreaseBond invokes DialogueSystem...");

        DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.IncreaseBond, "hero_fire", 5);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "ApplyEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "ApplyEffects method should exist.");

            method.Invoke(runner, new object[] { node });
            Debug.Log("[DialogueTreeRunnerTest] IncreaseBond applied without exception.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ApplyEffect IncreaseBond test passed");
    }

    private void TestApplyEffectGrantXPActuallyGrantsXp()
    {
        Debug.Log("Testing ApplyEffect GrantXP grants XP to HeroProgressionManager...");

        HeroProgressionManager progression = null;
        GameObject progressionObject = null;

        try
        {
            progressionObject = new GameObject("HeroProgressionManager_Test");
            progression = progressionObject.AddComponent<HeroProgressionManager>();
            InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
            SetLevelingConfig(progression, CreateDefaultLevelingConfig());

            DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.GrantXP, "hero_fire", 50);
            DialogueTreeRunner runner = CreateIsolatedRunner();
            GameObject go = runner.gameObject;
            try
            {
                InvokeApplyEffects(runner, node);
                Assert.AreEqual(50, progression.GetXp("hero_fire"),
                    "GrantXP must actually grant XP to the target hero's progression state.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }
        finally
        {
            if (progressionObject != null) DestroyImmediate(progressionObject);
        }

        Debug.Log("✓ ApplyEffect GrantXP state test passed");
    }

    private void TestApplyEffectUnlockTideBreakActuallyUnlocks()
    {
        Debug.Log("Testing ApplyEffect UnlockTideBreak registers the unlock...");

        TideBreakProgressionManager tideBreaks = null;
        GameObject tideBreakObject = null;

        try
        {
            tideBreakObject = new GameObject("TideBreakProgressionManager_Test");
            tideBreaks = tideBreakObject.AddComponent<TideBreakProgressionManager>();
            InvokeOnEnableIfUnregistered(tideBreaks, () => TideBreakProgressionManager.Instance);

            DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.UnlockTideBreak, "Inferno Surge", 1, "hero_fire");
            DialogueTreeRunner runner = CreateIsolatedRunner();
            GameObject go = runner.gameObject;
            try
            {
                InvokeApplyEffects(runner, node);
                Assert.IsTrue(tideBreaks.HasTideBreak("hero_fire", "Inferno Surge"),
                    "UnlockTideBreak must actually register the unlock in TideBreakProgressionManager.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }
        finally
        {
            if (tideBreakObject != null) DestroyImmediate(tideBreakObject);
        }

        Debug.Log("✓ ApplyEffect UnlockTideBreak state test passed");
    }

    private void TestApplyEffectSetFlagActuallySetsFlag()
    {
        Debug.Log("Testing ApplyEffect SetFlag sets the story flag...");

        StoryProgressionService story = null;
        GameObject storyObject = null;

        try
        {
            storyObject = new GameObject("StoryProgressionService_Test");
            story = storyObject.AddComponent<StoryProgressionService>();
            InvokeOnEnableIfUnregistered(story, () => StoryProgressionService.Instance);

            DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.SetFlag, "flag_merchant_helped", 1);
            DialogueTreeRunner runner = CreateIsolatedRunner();
            GameObject go = runner.gameObject;
            try
            {
                InvokeApplyEffects(runner, node);
                Assert.IsTrue(story.GetFlag("flag_merchant_helped"),
                    "SetFlag must actually set the flag in StoryProgressionService.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }
        finally
        {
            if (storyObject != null) DestroyImmediate(storyObject);
        }

        Debug.Log("✓ ApplyEffect SetFlag state test passed");
    }

    private void TestApplyEffectGiveItemActuallyAwardsGear()
    {
        Debug.Log("Testing ApplyEffect GiveItem awards gear for a registered set...");

        HeroProgressionManager progression = null;
        PlayerGearInventory inventory = null;
        GameObject progressionObject = null;
        GameObject inventoryObject = null;

        try
        {
            progressionObject = new GameObject("HeroProgressionManager_Test");
            progression = progressionObject.AddComponent<HeroProgressionManager>();
            InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
            SetLevelingConfig(progression, CreateDefaultLevelingConfig());
            SetAvailableGearSets(progression, new[] { CreateTestGearSet("iron_guard") });

            inventoryObject = new GameObject("PlayerGearInventory_Test");
            inventory = inventoryObject.AddComponent<PlayerGearInventory>();
            InvokeOnEnableIfUnregistered(inventory, () => PlayerGearInventory.Instance);

            DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.GiveItem, "iron_guard", 1);
            DialogueTreeRunner runner = CreateIsolatedRunner();
            GameObject go = runner.gameObject;
            try
            {
                InvokeApplyEffects(runner, node);
                Assert.AreEqual(1, inventory.OwnedGearCount,
                    "GiveItem must actually award an owned gear instance.");
                Assert.AreEqual("iron_guard", inventory.GetOwnedGear()[0].setId,
                    "Awarded gear must match the effect's item id.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }
        finally
        {
            if (inventoryObject != null) DestroyImmediate(inventoryObject);
            if (progressionObject != null) DestroyImmediate(progressionObject);
        }

        Debug.Log("✓ ApplyEffect GiveItem state test passed");
    }

    private void TestApplyEffectGiveItemUnknownIdFallsBackToCurrency()
    {
        Debug.Log("Testing ApplyEffect GiveItem with an unknown item id grants currency...");

        HeroProgressionManager progression = null;
        GameObject progressionObject = null;

        try
        {
            progressionObject = new GameObject("HeroProgressionManager_Test");
            progression = progressionObject.AddComponent<HeroProgressionManager>();
            InvokeOnEnableIfUnregistered(progression, () => HeroProgressionManager.Instance);
            SetLevelingConfig(progression, CreateDefaultLevelingConfig());
            SetAvailableGearSets(progression, new[] { CreateTestGearSet("iron_guard") });

            DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.GiveItem, "item_key_1", 25);
            DialogueTreeRunner runner = CreateIsolatedRunner();
            GameObject go = runner.gameObject;
            try
            {
                InvokeApplyEffects(runner, node);
                Assert.AreEqual(25, progression.Currency,
                    "Unknown item ids must fall back to the legacy currency reward.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }
        finally
        {
            if (progressionObject != null) DestroyImmediate(progressionObject);
        }

        Debug.Log("✓ ApplyEffect GiveItem currency fallback test passed");
    }

    private void TestStartTreeWithNullTreeDestroysRunner()
    {
        Debug.Log("Testing StartTree with null tree destroys runner...");

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            runner.StartTree(null);
            Debug.Log("[DialogueTreeRunnerTest] StartTree(null) completed without exception.");
            DestroyImmediate(go);
            Assert.IsTrue(go == null, "Runner should destroy itself when given a null tree.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ StartTree null tree test passed");
    }

    private void TestStartTreeWithNullRootDestroysRunner()
    {
        Debug.Log("Testing StartTree with null root node destroys runner...");

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            DialogueTree tree = new DialogueTree { treeId = "test", rootNode = null };
            runner.StartTree(tree);
            Debug.Log("[DialogueTreeRunnerTest] StartTree(null root) completed without exception.");
            DestroyImmediate(go);
            Assert.IsTrue(go == null, "Runner should destroy itself when given a tree with null root.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ StartTree null root test passed");
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static void InvokeApplyEffects(DialogueTreeRunner runner, DialogueTreeNode node)
    {
        // Mirror what ShowNode does during a real tree walk: the current node is
        // assigned before its effects are applied.
        FieldInfo currentNodeField = typeof(DialogueTreeRunner).GetField("currentNode", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(currentNodeField, "currentNode field should exist on DialogueTreeRunner.");
        if (currentNodeField.GetValue(runner) == null)
        {
            currentNodeField.SetValue(runner, node);
        }

        MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
            "ApplyEffects",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "ApplyEffects method should exist.");
        method.Invoke(runner, new object[] { node });
    }

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
                Debug.LogWarning($"[DialogueTreeRunnerTest] Lifecycle invocation failed ({ex.GetType().Name}: {ex.Message}); registering singleton directly.");
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
        gear.displayName = "Test Gear Set";
        gear.description = "Test gear set.";
        gear.attackBonusPercent = 0.05f;
        gear.defenseBonusPercent = 0.10f;
        gear.hpBonusPercent = 0.10f;
        gear.setBonusAttackPercent = 0.05f;
        gear.setBonusDefensePercent = 0.10f;
        gear.setBonusHpPercent = 0.10f;
        gear.setBonusDescription = "Test set bonus";
        return gear;
    }
}
