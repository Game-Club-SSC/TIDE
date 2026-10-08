#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Durable narrative state regressions, isolated from the current player save.</summary>
[DisallowMultipleComponent]
public sealed class NarrativeReplayVerificationTest : MonoBehaviour
{
    private readonly Dictionary<Type, object> previousInstances = new Dictionary<Type, object>();
    private GameObject dialogueObject;
    private GameObject storyObject;

    [ContextMenu("Run Narrative Replay Regressions")]
    public void RunTests()
    {
        int count = 0;
        foreach (MethodInfo test in GetType().GetMethods())
        {
            if (!Attribute.IsDefined(test, typeof(TestAttribute))) continue;
            SetUp();
            try
            {
                test.Invoke(this, null);
                count++;
            }
            finally
            {
                TearDown();
            }
        }
        Debug.Log($"[NarrativeReplayVerificationTest] {count} narrative replay regressions passed.");
    }

    [SetUp]
    public void SetUp()
    {
        foreach (Type type in new[] { typeof(GameStateManager), typeof(HeroProgressionManager), typeof(DialogueSystem), typeof(StoryProgressionService) })
        {
            previousInstances[type] = type.GetProperty("Instance").GetValue(null);
            SetInstance(type, null);
        }
        dialogueObject = new GameObject("NarrativeReplayVerification_Dialogue");
        DialogueSystem dialogue = dialogueObject.AddComponent<DialogueSystem>();
        SetInstance(typeof(DialogueSystem), dialogue);
    }

    [TearDown]
    public void TearDown()
    {
        if (dialogueObject != null) UnityEngine.Object.DestroyImmediate(dialogueObject);
        if (storyObject != null) UnityEngine.Object.DestroyImmediate(storyObject);
        foreach (KeyValuePair<Type, object> pair in previousInstances)
        {
            SetInstance(pair.Key, pair.Value);
        }
        previousInstances.Clear();
    }

    [Test]
    public void RepeatedUnavailableRewardKeepsOneDetachedPendingEntry()
    {
        DialogueTreeEffect effect = new DialogueTreeEffect { type = DialogueEffectType.GrantXP, targetId = "hero_fire", intValue = 50 };
        for (int i = 0; i < 2; i++)
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[DialogueSystem\] Dialogue effect not applied:.*HeroProgressionManager is not available"));
            Assert.IsFalse(DialogueSystem.Instance.ApplyDialogueEffect(effect, "hero_fire", "replay", "reward", 0));
        }
        DialogueSystem.DialogueStateSaveData saved = DialogueSystem.Instance.CaptureDialogueStateSaveData();
        Assert.AreEqual(1, saved.pendingRewards.Count);
        saved.pendingRewards[0].intValue = 999;
        Assert.AreEqual(50, DialogueSystem.Instance.CaptureDialogueStateSaveData().pendingRewards[0].intValue,
            "A captured payload must not mutate the live pending ledger.");
    }

    [Test]
    public void LatestSavedFlagWinsOverAnOlderDeferredWrite()
    {
        CreateStoryService();
        DialogueSystem.Instance.ApplyDialogueStateSaveData(new DialogueSystem.DialogueStateSaveData
        {
            setFlagIds = new List<string> { "quest" },
            setFlagValues = new List<bool> { false },
            pendingRewards = new List<DialogueSystem.DialoguePendingRewardEntry> { PendingFlag(1) }
        });
        Assert.IsFalse(StoryProgressionService.Instance.GetFlag("quest"));
        Assert.IsFalse(DialogueSystem.Instance.IsDialogueFlagSet("quest"));
        Assert.AreEqual(0, DialogueSystem.Instance.CaptureDialogueStateSaveData().pendingRewards.Count);
    }

    [Test]
    public void LegacyPendingOnlyFlagsUseTheirLastWrite()
    {
        CreateStoryService();
        DialogueSystem.Instance.ApplyDialogueStateSaveData(new DialogueSystem.DialogueStateSaveData
        {
            pendingRewards = new List<DialogueSystem.DialoguePendingRewardEntry> { PendingFlag(1), PendingFlag(0) }
        });
        Assert.IsFalse(StoryProgressionService.Instance.GetFlag("quest"));
        Assert.IsFalse(DialogueSystem.Instance.IsDialogueFlagSet("quest"));
    }

    [Test]
    public void BondOverflowSaturatesAndMissingSnapshotClearsPriorRun()
    {
        DialogueSystem.Instance.IncreaseBond("hero_fire", "hero_water", 100);
        DialogueSystem.Instance.IncreaseBond("hero_fire", "hero_water", int.MaxValue);
        Assert.AreEqual(100, DialogueSystem.Instance.GetBondLevel("hero_fire", "hero_water"));
        DialogueSystem.Instance.ApplyDialogueStateSaveData(null);
        Assert.AreEqual(0, DialogueSystem.Instance.GetBondLevel("hero_fire", "hero_water"));
    }

    private void CreateStoryService()
    {
        storyObject = new GameObject("NarrativeReplayVerification_Story");
        StoryProgressionService story = storyObject.AddComponent<StoryProgressionService>();
        SetInstance(typeof(StoryProgressionService), story);
    }

    private static DialogueSystem.DialoguePendingRewardEntry PendingFlag(int value)
    {
        return new DialogueSystem.DialoguePendingRewardEntry
        {
            treeId = "flags", nodeId = "node", effectIndex = value,
            effectType = (int)DialogueEffectType.SetFlag, targetId = "quest", intValue = value
        };
    }

    private static void SetInstance(Type type, object value)
    {
        type.GetProperty("Instance").GetSetMethod(true).Invoke(null, new[] { value });
    }
}
#endif
