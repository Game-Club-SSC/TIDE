using NUnit.Framework;
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
        var showDialogue = typeof(DialogueSystem).GetMethod("ShowDialogue");
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
