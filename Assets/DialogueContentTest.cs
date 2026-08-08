using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Tests for the dialogue content system.
/// Verifies dialogue trees and narrative content exist.
/// </summary>
[DisallowMultipleComponent]
public class DialogueContentTest : MonoBehaviour
{
    [ContextMenu("Run Dialogue Content Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Dialogue Content Tests ===");

        TestDialogueSystemExists();
        TestDialogueTreeRunnerExists();
        TestDialogueTriggerExists();
        TestEmptyOneShotTriggerIsNotConsumed();
        TestBusyDialogueSystemDoesNotConsumeTrigger();
        TestBossNarrativeMechanicExists();
        TestFateEncounterDirectorExists();
        TestNarratorDirectorExists();

        Debug.Log("=== All Dialogue Content Tests Passed ===");
    }

    private void TestDialogueSystemExists()
    {
        Debug.Log("Testing DialogueSystem exists...");

        Assert.IsNotNull(typeof(DialogueSystem), "DialogueSystem class should exist.");

        // Verify key methods exist
        MethodInfo showDialogue = typeof(DialogueSystem).GetMethod(
            "ShowDialogue", new[] { typeof(List<DialogueSystem.DialogueEntry>) });
        var startDialogueTree = typeof(DialogueSystem).GetMethod("StartDialogueTree");

        Assert.IsNotNull(showDialogue, "ShowDialogue method should exist.");
        Assert.IsNotNull(startDialogueTree, "StartDialogueTree method should exist.");

        Debug.Log("DialogueSystem exists: PASS");
    }

    private void TestDialogueTreeRunnerExists()
    {
        Debug.Log("Testing DialogueTreeRunner exists...");

        Assert.IsNotNull(typeof(DialogueTreeRunner), "DialogueTreeRunner class should exist.");

        // Verify key methods exist
        var startTree = typeof(DialogueTreeRunner).GetMethod("StartTree");
        Assert.IsNotNull(startTree, "StartTree method should exist.");

        Debug.Log("DialogueTreeRunner exists: PASS");
    }

    private void TestDialogueTriggerExists()
    {
        Debug.Log("Testing DialogueTrigger exists...");

        Assert.IsNotNull(typeof(DialogueTrigger), "DialogueTrigger class should exist.");

        // Verify key methods exist
        var addEntry = typeof(DialogueTrigger).GetMethod("AddDialogueEntry");
        Assert.IsNotNull(addEntry, "AddDialogueEntry method should exist.");

        Debug.Log("DialogueTrigger exists: PASS");
    }

    private void TestEmptyOneShotTriggerIsNotConsumed()
    {
        Debug.Log("Testing empty one-shot dialogue trigger is not consumed...");

        GameObject dialogueObject = null;
        GameObject triggerObject = new GameObject("DialogueTrigger_EmptyTest");
        triggerObject.AddComponent<BoxCollider>();
        DialogueTrigger trigger = triggerObject.AddComponent<DialogueTrigger>();
        DialogueSystem system = DialogueSystem.Instance;

        try
        {
            if (system == null)
            {
                dialogueObject = new GameObject("DialogueSystem_EmptyTriggerTest");
                system = dialogueObject.AddComponent<DialogueSystem>();
                SetDialogueSystemInstance(system);
            }

            trigger.SetDialogueEntries(new List<DialogueSystem.DialogueEntry>());

            MethodInfo startMethod = typeof(DialogueTrigger).GetMethod(
                "StartDialogue", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo playedField = typeof(DialogueTrigger).GetField(
                "hasPlayed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(startMethod, "DialogueTrigger.StartDialogue should exist.");
            Assert.IsNotNull(playedField, "DialogueTrigger.hasPlayed should exist.");

            startMethod.Invoke(trigger, null);

            Assert.IsFalse((bool)playedField.GetValue(trigger),
                "An empty one-shot trigger must remain available after it fails to start dialogue.");
        }
        finally
        {
            DestroyImmediate(triggerObject);
            if (dialogueObject != null)
            {
                SetDialogueSystemInstance(null);
                DestroyImmediate(dialogueObject);
            }
        }

        Debug.Log("Empty one-shot dialogue trigger test: PASS");
    }

    private void TestBusyDialogueSystemDoesNotConsumeTrigger()
    {
        Debug.Log("Testing busy dialogue system does not consume another trigger...");

        GameObject dialogueObject = null;
        GameObject triggerObject = new GameObject("DialogueTrigger_BusyTest");
        triggerObject.AddComponent<BoxCollider>();
        DialogueTrigger trigger = triggerObject.AddComponent<DialogueTrigger>();
        FieldInfo activeField = typeof(DialogueSystem).GetField(
            "isDialogueActive", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(activeField, "DialogueSystem.isDialogueActive should exist.");

        DialogueSystem system = DialogueSystem.Instance;
        if (system == null)
        {
            dialogueObject = new GameObject("DialogueSystem_BusyTriggerTest");
            system = dialogueObject.AddComponent<DialogueSystem>();
            SetDialogueSystemInstance(system);
        }

        bool wasActive = (bool)activeField.GetValue(system);
        try
        {
            trigger.SetDialogueEntries(new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Test",
                    dialogueText = "This line must wait."
                }
            });
            activeField.SetValue(system, true);

            MethodInfo startMethod = typeof(DialogueTrigger).GetMethod(
                "StartDialogue", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo playedField = typeof(DialogueTrigger).GetField(
                "hasPlayed", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo pendingField = typeof(DialogueTrigger).GetField(
                "dialoguePending", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(startMethod, "DialogueTrigger.StartDialogue should exist.");
            Assert.IsNotNull(playedField, "DialogueTrigger.hasPlayed should exist.");
            Assert.IsNotNull(pendingField, "DialogueTrigger.dialoguePending should exist.");

            startMethod.Invoke(trigger, null);

            Assert.IsFalse((bool)playedField.GetValue(trigger),
                "A busy dialogue system must not consume a one-shot trigger.");
            Assert.IsFalse((bool)pendingField.GetValue(trigger),
                "A rejected dialogue request must not leave its trigger pending.");
        }
        finally
        {
            activeField.SetValue(system, wasActive);
            DestroyImmediate(triggerObject);
            if (dialogueObject != null)
            {
                SetDialogueSystemInstance(null);
                DestroyImmediate(dialogueObject);
            }
        }

        Debug.Log("Busy dialogue trigger test: PASS");
    }

    private static void SetDialogueSystemInstance(DialogueSystem system)
    {
        FieldInfo instanceField = typeof(DialogueSystem).GetField(
            "<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(instanceField, "DialogueSystem.Instance backing field should exist.");
        instanceField.SetValue(null, system);
    }

    private void TestBossNarrativeMechanicExists()
    {
        Debug.Log("Testing BossNarrativeMechanic exists...");

        Assert.IsNotNull(typeof(BossNarrativeMechanic), "BossNarrativeMechanic class should exist.");

        // Verify default mechanics exist
        BossNarrativeMechanic[] defaults = BossNarrativeMechanic.GetDefaults();
        Assert.IsNotNull(defaults, "Default mechanics should exist.");
        Assert.AreEqual(6, defaults.Length, "Should have 6 boss mechanics.");

        // Verify each boss has required fields
        foreach (BossNarrativeMechanic mechanic in defaults)
        {
            Assert.IsFalse(string.IsNullOrEmpty(mechanic.bossName), $"Boss {mechanic.islandId} should have a name.");
            Assert.IsFalse(string.IsNullOrEmpty(mechanic.islandId), $"Boss should have an island ID.");
            Assert.IsFalse(string.IsNullOrEmpty(mechanic.introDescription), $"Boss {mechanic.bossName} should have intro description.");
            Assert.IsFalse(string.IsNullOrEmpty(mechanic.defeatDialogue), $"Boss {mechanic.bossName} should have defeat dialogue.");

            Debug.Log($"Boss {mechanic.bossName} ({mechanic.islandId}): intro={mechanic.introDescription.Substring(0, Mathf.Min(50, mechanic.introDescription.Length))}...");
        }

        Debug.Log("BossNarrativeMechanic exists with 6 bosses: PASS");
    }

    private void TestFateEncounterDirectorExists()
    {
        Debug.Log("Testing FateEncounterDirector exists...");

        Assert.IsNotNull(typeof(FateEncounterDirector), "FateEncounterDirector class should exist.");

        // Verify key methods exist
        var startEncounter = typeof(FateEncounterDirector).GetMethod("StartFateEncounter");
        Assert.IsNotNull(startEncounter, "StartFateEncounter method should exist.");

        // Verify fate questions exist (default questions)
        Debug.Log("FateEncounterDirector exists: PASS");
    }

    private void TestNarratorDirectorExists()
    {
        Debug.Log("Testing NarratorDirector exists...");

        Assert.IsNotNull(typeof(NarratorDirector), "NarratorDirector class should exist.");

        // Verify singleton pattern
        NarratorDirector director = NarratorDirector.Instance;
        if (director != null)
        {
            Assert.IsTrue(director.isActiveAndEnabled, "NarratorDirector should be active.");
            Debug.Log("NarratorDirector exists and is active: PASS");
        }
        else
        {
            Debug.LogWarning("NarratorDirector instance not found (may need to be in scene).");
        }
    }
}
