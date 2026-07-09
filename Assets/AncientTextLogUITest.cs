using System.Collections.Generic;
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
        ActGateMatchesConfiguredStoryAct();
        ActGateAnyAlwaysMatches();
        IslandGateMatchesActiveIsland();
        ActCoverageSpansAllThreeActs();

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

    private void ActGateMatchesConfiguredStoryAct()
    {
        Debug.Log("[AncientTextLogUITest] Act-gated discoverable matches current story act...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_ActGate");
            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();
            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_lust");
            manager.RefreshStoryProgressionForDebug();

            Assert.AreEqual(GameStateManager.StoryAct.ActI, manager.CurrentStoryAct,
                "First island should keep the story in Act I.");

            GameObject discoverableObject = new GameObject("TestDiscoverable_ActI");
            AncientTextDiscoverable discoverable = discoverableObject.AddComponent<AncientTextDiscoverable>();
            SetPrivateField(discoverable, "requiredAct", NarrativeAct.ActI);

            Assert.IsTrue(discoverable.IsActMatched(),
                "Act-gated discoverable should match when story is in Act I.");

            manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActIII);
            Assert.IsFalse(discoverable.IsActMatched(),
                "Act-gated discoverable should not match when story moves beyond Act I.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void ActGateAnyAlwaysMatches()
    {
        Debug.Log("[AncientTextLogUITest] Any-act discoverable matches every story act...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_ActAny");
            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();

            GameObject discoverableObject = new GameObject("TestDiscoverable_ActAny");
            AncientTextDiscoverable discoverable = discoverableObject.AddComponent<AncientTextDiscoverable>();
            SetPrivateField(discoverable, "requiredAct", NarrativeAct.Any);

            manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActI);
            Assert.IsTrue(discoverable.IsActMatched(), "Any-act discoverable should match Act I.");

            manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActII);
            Assert.IsTrue(discoverable.IsActMatched(), "Any-act discoverable should match Act II.");

            manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActIII);
            Assert.IsTrue(discoverable.IsActMatched(), "Any-act discoverable should match Act III.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void IslandGateMatchesActiveIsland()
    {
        Debug.Log("[AncientTextLogUITest] Island-gated discoverable matches active island...");

        try
        {
            GameStateManager manager = CreateIsolatedManager("TestGameStateManager_IslandGate");
            IslandProgressionManager.Instance.UnlockAllIslandsForDebug();
            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_greed");

            GameObject discoverableObject = new GameObject("TestDiscoverable_IslandGate");
            AncientTextDiscoverable discoverable = discoverableObject.AddComponent<AncientTextDiscoverable>();
            SetPrivateField(discoverable, "targetIslandId", "island_greed");
            SetPrivateField(discoverable, "requiredAct", NarrativeAct.Any);

            Assert.IsTrue(discoverable.IsIslandMatched(),
                "Island-gated discoverable should match when its target island is active.");

            IslandProgressionManager.Instance.ForceSetActiveIslandForDebug("island_desire");
            Assert.IsFalse(discoverable.IsIslandMatched(),
                "Island-gated discoverable should not match when a different island is active.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void ActCoverageSpansAllThreeActs()
    {
        Debug.Log("[AncientTextLogUITest] Island list covers at least one entry per act...");

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        HashSet<NarrativeAct> actsCovered = new HashSet<NarrativeAct>();
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            actsCovered.Add(AncientTextDiscoverable.DetermineActForIsland(progressionOrder[i]));
        }

        Assert.IsTrue(actsCovered.Contains(NarrativeAct.ActI),
            "Act I coverage should exist for early-island discoverables.");
        Assert.IsTrue(actsCovered.Contains(NarrativeAct.ActII),
            "Act II coverage should exist for mid-game discoverables.");
        Assert.IsTrue(actsCovered.Contains(NarrativeAct.ActIII),
            "Act III coverage should exist for the final island discoverable.");
    }

    private static GameStateManager CreateIsolatedManager(string managerName)
    {
        Cleanup();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameObject managerObject = new GameObject(managerName);
        GameStateManager manager = managerObject.AddComponent<GameStateManager>();
        SetPersistentSaveDisabled(manager);
        return manager;
    }

    private static void SetPersistentSaveDisabled(GameStateManager manager)
    {
        System.Reflection.FieldInfo field = typeof(GameStateManager).GetField(
            "enablePersistentSaveData",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(manager, false);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void Cleanup()
    {
        if (GameStateManager.Instance != null)
        {
            DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (IslandProgressionManager.Instance != null)
        {
            DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (DevCheatService.Instance != null)
        {
            DestroyImmediate(DevCheatService.Instance.gameObject);
        }

        AncientTextDiscoverable[] discoverables = Object.FindObjectsByType<AncientTextDiscoverable>(FindObjectsSortMode.None);
        for (int i = 0; i < discoverables.Length; i++)
        {
            if (discoverables[i] != null)
            {
                DestroyImmediate(discoverables[i].gameObject);
            }
        }
    }
}
