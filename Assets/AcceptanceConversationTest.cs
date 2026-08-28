using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class AcceptanceConversationTest : MonoBehaviour
{
    [ContextMenu("Run Acceptance Conversation Tests")]
    public void RunTests()
    {
        TestVisibleDialogueEntriesCoverEveryLine();
        TestVisibleDialogueUsesSharedDialogueSystem();
        TestCompletionWaitsForAcceptanceSequence();
        TestDebugPlaybackRemainsSynchronous();

        Debug.Log("[AcceptanceConversationTest] All tests passed.");
    }

    [Test]
    public void TestVisibleDialogueEntriesCoverEveryLine()
    {
        GameObject host = new GameObject("AcceptanceConversation_EntryTest");
        try
        {
            AcceptanceConversation conversation = host.AddComponent<AcceptanceConversation>();
            List<DialogueSystem.DialogueEntry> entries = GetDialogueEntries(conversation);
            Assert.AreEqual(AcceptanceConversation.LineCount, entries.Count,
                "Every authored acceptance line must be shown in the dialogue UI.");

            for (int i = 0; i < entries.Count; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entries[i].speakerName),
                    $"Acceptance line {i} needs a speaker.");
                Assert.IsFalse(string.IsNullOrEmpty(entries[i].dialogueText),
                    $"Acceptance line {i} needs visible text.");
            }
        }
        finally
        {
            DestroyImmediate(host);
        }
    }

    [Test]
    public void TestVisibleDialogueUsesSharedDialogueSystem()
    {
        string sourcePath = Path.Combine(Application.dataPath, "AcceptanceConversation.cs");
        Assert.IsTrue(File.Exists(sourcePath), "Acceptance conversation source must be present in the Assets folder.");

        string source = File.ReadAllText(sourcePath);
        Assert.IsTrue(source.Contains("activeDialogueSystem.ShowDialogue(activeDialogueEntries)"),
            "Acceptance must use DialogueSystem so the player can advance every line.");
        Assert.IsTrue(source.Contains("HandleVisibleDialogueCompleted"),
            "Acceptance must wait for the dialogue completion callback before Fate starts.");
    }

    [Test]
    public void TestCompletionWaitsForAcceptanceSequence()
    {
        GameObject host = new GameObject("AcceptanceConversation_CompletionTest");
        try
        {
            AcceptanceConversation conversation = host.AddComponent<AcceptanceConversation>();
            List<DialogueSystem.DialogueEntry> entries = GetDialogueEntries(conversation);
            FieldInfo activeEntries = typeof(AcceptanceConversation).GetField(
                "activeDialogueEntries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo isPlaying = typeof(AcceptanceConversation).GetField(
                "isPlaying",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo completed = typeof(AcceptanceConversation).GetMethod(
                "HandleVisibleDialogueCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(activeEntries, "Acceptance must track its active dialogue sequence.");
            Assert.IsNotNull(isPlaying, "Acceptance must track whether it is active.");
            Assert.IsNotNull(completed, "Acceptance must handle the dialogue completion callback.");

            activeEntries.SetValue(conversation, entries);
            isPlaying.SetValue(conversation, true);

            bool finished = false;
            conversation.OnAcceptanceConversationFinished += () => finished = true;

            completed.Invoke(conversation, new object[] { new List<DialogueSystem.DialogueEntry>() });
            Assert.IsFalse(finished, "Another dialogue sequence must not start Fate.");
            Assert.IsTrue(conversation.IsPlaying, "Another dialogue sequence must not end Acceptance.");

            completed.Invoke(conversation, new object[] { entries });
            Assert.IsTrue(finished, "The acceptance sequence should start Fate only after it completes.");
            Assert.IsTrue(conversation.HasPlayed, "The matching sequence should mark Acceptance complete.");
            Assert.IsFalse(conversation.IsPlaying, "The matching sequence should clear the active state.");
        }
        finally
        {
            DestroyImmediate(host);
        }
    }

    [Test]
    public void TestDebugPlaybackRemainsSynchronous()
    {
        GameObject host = new GameObject("AcceptanceConversation_DebugTest");
        try
        {
            AcceptanceConversation conversation = host.AddComponent<AcceptanceConversation>();
            int lineCount = 0;
            bool finished = false;

            conversation.OnAcceptanceLinePresented += (index, line) => lineCount++;
            conversation.OnAcceptanceConversationFinished += () => finished = true;
            conversation.ForcePlayForDebug();

            Assert.AreEqual(AcceptanceConversation.LineCount, lineCount,
                "Debug playback must still emit all acceptance lines immediately.");
            Assert.IsTrue(finished, "Debug playback must still emit the completion event.");
            Assert.IsTrue(conversation.HasPlayed, "Debug playback should mark the conversation complete.");
            Assert.IsFalse(conversation.IsPlaying, "Debug playback should not leave the conversation active.");
        }
        finally
        {
            DestroyImmediate(host);
        }
    }

    private static List<DialogueSystem.DialogueEntry> GetDialogueEntries(AcceptanceConversation conversation)
    {
        MethodInfo buildEntries = typeof(AcceptanceConversation).GetMethod(
            "BuildDialogueEntries",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(buildEntries, "Acceptance dialogue must build player-facing entries.");

        List<DialogueSystem.DialogueEntry> entries = buildEntries.Invoke(
            conversation,
            null) as List<DialogueSystem.DialogueEntry>;
        Assert.IsNotNull(entries, "Acceptance dialogue entries should be created.");
        return entries;
    }
}
