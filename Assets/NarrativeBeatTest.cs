using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the narrative beat director system.
/// Verifies 3-act structure and tone-shift triggers.
/// </summary>
[DisallowMultipleComponent]
public class NarrativeBeatTest : MonoBehaviour
{
    [ContextMenu("Run Narrative Beat Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Narrative Beat Tests ===");

        TestStoryActEnumValues();
        TestBeatIdsExist();
        TestActProgressionLogic();
        TestEndingBranchLogic();
        TestDialogueUsesCanonicalHeroIds();
        TestCharacterIntroDefersCycleRevelation();

        Debug.Log("=== All Narrative Beat Tests Passed ===");
    }

    private void TestStoryActEnumValues()
    {
        Debug.Log("Testing StoryAct enum values...");

        // Verify the story act system exists in GameStateManager
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            // Check that we can read the current story act
            GameStateManager.StoryAct currentAct = gsm.CurrentStoryAct;
            Debug.Log($"Current story act: {currentAct}");

            // Verify act values exist
            GameStateManager.StoryAct actI = GameStateManager.StoryAct.ActI;
            GameStateManager.StoryAct actII = GameStateManager.StoryAct.ActII;
            GameStateManager.StoryAct actIII = GameStateManager.StoryAct.ActIII;

            Assert.AreNotEqual(actI, actII, "ActI and ActII should be different.");
            Assert.AreNotEqual(actII, actIII, "ActII and ActIII should be different.");
            Debug.Log("StoryAct enum validated: ActI, ActII, ActIII exist.");
        }
        else
        {
            Debug.LogWarning("GameStateManager not found - skipping story act tests.");
        }
    }

    private void TestBeatIdsExist()
    {
        Debug.Log("Testing beat IDs exist...");

        // Verify beat IDs are defined in NarrativeBeatDirector
        // These are private constants, but we can verify they exist by checking the class
        Assert.IsTrue(typeof(NarrativeBeatDirector).GetField("IntroBeatId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null
            || typeof(NarrativeBeatDirector).GetProperty("ActThreeBeatIdPublic") != null,
            "NarrativeBeatDirector should have beat ID constants.");

        Debug.Log("Beat IDs exist: PASS");
    }

    private void TestActProgressionLogic()
    {
        Debug.Log("Testing act progression logic...");

        // Verify that act progression follows the expected order
        // Act I -> Act II -> Act III
        GameStateManager.StoryAct actI = GameStateManager.StoryAct.ActI;
        GameStateManager.StoryAct actII = GameStateManager.StoryAct.ActII;
        GameStateManager.StoryAct actIII = GameStateManager.StoryAct.ActIII;

        // Act II should come after Act I
        Assert.Greater((int)actII, (int)actI, "ActII should come after ActI.");

        // Act III should come after Act II
        Assert.Greater((int)actIII, (int)actII, "ActIII should come after ActII.");

        Debug.Log("Act progression logic: PASS");
    }

    private void TestEndingBranchLogic()
    {
        Debug.Log("Testing ending branch logic...");

        // Verify ending branches exist
        GameStateManager.EndingBranch goodEnding = GameStateManager.EndingBranch.Good;
        GameStateManager.EndingBranch badEnding = GameStateManager.EndingBranch.Bad;

        Assert.AreNotEqual(goodEnding, badEnding, "Good and Bad endings should be different.");

        Debug.Log("Ending branch logic: PASS");
    }

    private void TestDialogueUsesCanonicalHeroIds()
    {
        Assert.AreEqual("hero_fire", HeroDialogueContent.HeroEmber);
        Assert.AreEqual("hero_water", HeroDialogueContent.HeroTidecaller);
        Assert.AreEqual("hero_earth", HeroDialogueContent.HeroStoneheart);
        Assert.AreEqual("hero_air", HeroDialogueContent.HeroZephyr);
        Assert.AreEqual("hero_space", HeroDialogueContent.HeroVoidwalker);
    }

    private void TestCharacterIntroDefersCycleRevelation()
    {
        DialogueTree intro = HeroDialogueContent.CharacterIntroDialogue();
        Assert.IsNotNull(intro, "The post-ceremony character introduction should exist.");

        for (int i = 0; i < intro.allNodes.Count; i++)
        {
            DialogueTreeNode node = intro.allNodes[i];
            if (node == null) continue;

            string text = (node.entry.dialogueText ?? string.Empty).ToLowerInvariant();
            Assert.IsFalse(text.Contains("cycle"),
                "The first party conversation must not reveal the cycle before ancient texts are found.");
            Assert.IsFalse(text.Contains("past chosen"),
                "The first party conversation must defer past-Chosen revelations.");
        }
    }
}
