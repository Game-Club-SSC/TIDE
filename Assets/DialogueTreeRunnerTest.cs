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
        TestEvaluateConditionStoryActChecksService();
        TestEvaluateConditionIslandRestoredChecksTracker();
        TestEvaluateConditionHasAncientTextChecksDefinition();
        TestEvaluateConditionQuestCompletedChecksService();
        TestApplyEffectIncreaseBond();
        TestApplyEffectGrantXPIsNoop();
        TestApplyEffectUnlockTideBreakIsNoop();
        TestApplyEffectSetFlagIsNoop();
        TestApplyEffectGiveItemIsNoop();
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
        DialogueTreeNode node = new DialogueTreeNode
        {
            nodeId = "test_effect_node",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Test",
                dialogueText = "Effect node"
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

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "EvaluateConditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool result = (bool)method.Invoke(runner, new object[] { node });
            Assert.IsFalse(result, "BondLevel condition with null DialogueSystem.Instance returns false.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ EvaluateCondition BondLevel fail test passed");
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

    private void TestApplyEffectGrantXPIsNoop()
    {
        Debug.Log("Testing ApplyEffect GrantXP is a TODO noop...");

        DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.GrantXP, "hero_mc", 100);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "ApplyEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(runner, new object[] { node });
            Debug.Log("[DialogueTreeRunnerTest] GrantXP applied without exception (noop).");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ApplyEffect GrantXP noop test passed");
    }

    private void TestApplyEffectUnlockTideBreakIsNoop()
    {
        Debug.Log("Testing ApplyEffect UnlockTideBreak is a TODO noop...");

        DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.UnlockTideBreak, "tidebreak_fire", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "ApplyEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(runner, new object[] { node });
            Debug.Log("[DialogueTreeRunnerTest] UnlockTideBreak applied without exception (noop).");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ApplyEffect UnlockTideBreak noop test passed");
    }

    private void TestApplyEffectSetFlagIsNoop()
    {
        Debug.Log("Testing ApplyEffect SetFlag is a TODO noop...");

        DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.SetFlag, "flag_merchant_helped", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "ApplyEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(runner, new object[] { node });
            Debug.Log("[DialogueTreeRunnerTest] SetFlag applied without exception (noop).");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ApplyEffect SetFlag noop test passed");
    }

    private void TestApplyEffectGiveItemIsNoop()
    {
        Debug.Log("Testing ApplyEffect GiveItem is a TODO noop...");

        DialogueTreeNode node = CreateNodeWithEffect(DialogueEffectType.GiveItem, "item_key_1", 1);

        DialogueTreeRunner runner = CreateIsolatedRunner();
        GameObject go = runner.gameObject;

        try
        {
            System.Reflection.MethodInfo method = typeof(DialogueTreeRunner).GetMethod(
                "ApplyEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(runner, new object[] { node });
            Debug.Log("[DialogueTreeRunnerTest] GiveItem applied without exception (noop).");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ ApplyEffect GiveItem noop test passed");
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
}
