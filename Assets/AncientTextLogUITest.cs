using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class AncientTextLogUITest : MonoBehaviour
{
    [ContextMenu("Run Ancient Text Log UI Tests")]
    public void RunTests()
    {
        Debug.Log("[AncientTextLogUITest] Starting Ancient Text Log UI Tests.");

        BuildDialoguePagesSplitsSpeakerLines();
        BuildDialoguePagesUsesFallbackForNarration();
        BuildDialoguePagesUsesReadableEmptyState();
        BuildDialoguePagesUsesNarratorWhenFallbackMissing();
        BuildDialoguePagesKeepsMalformedSpeakerPrefixInText();

        Debug.Log("[AncientTextLogUITest] All Ancient Text Log UI Tests passed.");
    }

    private void BuildDialoguePagesSplitsSpeakerLines()
    {
        AncientTextLogUI.DialoguePage[] pages = AncientTextLogUI.BuildDialoguePages(
            "Campfire Friction",
            "Fire: We move now.\nWater: We move with care.");

        Assert.AreEqual(2, pages.Length, "Each speaker line should become a separate dialogue page.");
        Assert.AreEqual("Fire", pages[0].Speaker);
        Assert.AreEqual("We move now.", pages[0].Text);
        Assert.AreEqual("Water", pages[1].Speaker);
        Assert.AreEqual("We move with care.", pages[1].Text);
    }

    private void BuildDialoguePagesUsesFallbackForNarration()
    {
        AncientTextLogUI.DialoguePage[] pages = AncientTextLogUI.BuildDialoguePages(
            "Sunset In Balance",
            "The island settles into quiet.");

        Assert.AreEqual(1, pages.Length, "Narration without an explicit speaker should stay as one page.");
        Assert.AreEqual("Sunset In Balance", pages[0].Speaker);
        Assert.AreEqual("The island settles into quiet.", pages[0].Text);
    }

    private void BuildDialoguePagesUsesReadableEmptyState()
    {
        AncientTextLogUI.DialoguePage[] pages = AncientTextLogUI.BuildDialoguePages("Archive", "");

        Assert.AreEqual(1, pages.Length, "Empty text should still produce one readable page.");
        Assert.AreEqual("Archive", pages[0].Speaker);
        Assert.AreEqual("No readable inscription remains on this fragment.", pages[0].Text);
    }

    private void BuildDialoguePagesUsesNarratorWhenFallbackMissing()
    {
        AncientTextLogUI.DialoguePage[] pages = AncientTextLogUI.BuildDialoguePages(null, "A nameless line.");

        Assert.AreEqual(1, pages.Length, "A missing fallback speaker should still produce one page.");
        Assert.AreEqual("Narrator", pages[0].Speaker);
        Assert.AreEqual("A nameless line.", pages[0].Text);
    }

    private void BuildDialoguePagesKeepsMalformedSpeakerPrefixInText()
    {
        AncientTextLogUI.DialoguePage[] pages = AncientTextLogUI.BuildDialoguePages(
            "Archive",
            "Fire/Water: This is a combined note.");

        Assert.AreEqual(1, pages.Length, "Malformed speaker labels should not be split.");
        Assert.AreEqual("Archive", pages[0].Speaker);
        Assert.AreEqual("Fire/Water: This is a combined note.", pages[0].Text);
    }
}
