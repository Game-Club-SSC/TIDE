using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class NarrativeSystemsTest : MonoBehaviour
{
    [ContextMenu("Run Narrative Systems Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Narrative Systems Tests ===");

        TestAcceptanceConversationRequiresFinalBossIsland();
        TestAcceptanceConversationPlaysAllLinesAndFiresFinished();
        TestAcceptanceConversationRequiresRestorationThreshold();
        TestSelfHarmBeatGatesOnBadEnding();
        TestSelfHarmBeatPlaysFourLinesAndFiresFinished();
        TestSelfHarmBeatPlaysOnceAndIgnoresRepeat();
        TestAncientTextAuthoringProvidesAtLeastEighteenEntries();
        TestAncientTextAuthoringCoversAllSixSinsWithTwoLinesEach();
        TestAncientTextAuthoringCoversAllThreeActs();
        TestRelationshipTrackerAffinityClamped();
        TestRelationshipTrackerTierThresholds();
        TestRelationshipTrackerTierChangedFiresOnTransition();
        TestPowerBudgetTrackerDefaultBudgetSeedsAllIslands();
        TestPowerBudgetTrackerTryConsumeSucceedsWithinBudget();
        TestPowerBudgetTrackerTryConsumeRejectsWhenInsufficient();
        TestNarrativeBeatsDataContainsAllConstants();

        Debug.Log("=== All Narrative Systems Tests Passed ===");
    }

    private GameObject CreateSingleton<T>() where T : Component
    {
        GameObject host = new GameObject($"Test_{typeof(T).Name}");
        return host;
    }

    private void Cleanup()
    {
        if (AcceptanceConversation.Instance != null)
        {
            Object.DestroyImmediate(AcceptanceConversation.Instance.gameObject);
        }
        if (SelfHarmBeat.Instance != null)
        {
            Object.DestroyImmediate(SelfHarmBeat.Instance.gameObject);
        }
        if (RelationshipTracker.Instance != null)
        {
            Object.DestroyImmediate(RelationshipTracker.Instance.gameObject);
        }
        if (PowerBudgetTracker.Instance != null)
        {
            Object.DestroyImmediate(PowerBudgetTracker.Instance.gameObject);
        }
    }

    private void TestAcceptanceConversationRequiresFinalBossIsland()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<AcceptanceConversation>();
            AcceptanceConversation conv = host.AddComponent<AcceptanceConversation>();
            Assert.IsNotNull(AcceptanceConversation.Instance, "AcceptanceConversation.Instance should be set.");
            Assert.IsFalse(conv.CanPlayAcceptanceConversation(), "Without a final boss island, conversation should be gated.");
            Assert.IsFalse(conv.PlayAcceptanceConversation(), "Should return false when prerequisite isn't met.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestAcceptanceConversationPlaysAllLinesAndFiresFinished()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<AcceptanceConversation>();
            AcceptanceConversation conv = host.AddComponent<AcceptanceConversation>();

            int lineCount = 0;
            bool finished = false;
            conv.OnAcceptanceLinePresented += (idx, body) => lineCount++;
            conv.OnAcceptanceConversationFinished += () => finished = true;

            conv.ForcePlayForDebug();
            Assert.AreEqual(AcceptanceConversation.LineCount, lineCount, "Should fire all line events.");
            Assert.IsTrue(finished, "Finished event should fire.");
            Assert.IsTrue(conv.HasPlayed, "HasPlayed should be true after force play.");
            Assert.IsFalse(conv.IsPlaying, "IsPlaying should clear after finish.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestAcceptanceConversationRequiresRestorationThreshold()
    {
        Cleanup();
        IslandRestorationTracker tracker = null;
        IslandProgressionManager progression = null;
        try
        {
            if (IslandRestorationTracker.Instance == null)
            {
                GameObject trackerHost = new GameObject("Test_Tracker");
                tracker = trackerHost.AddComponent<IslandRestorationTracker>();
            }
            if (IslandProgressionManager.Instance == null)
            {
                GameObject progressionHost = new GameObject("Test_Progression");
                progression = progressionHost.AddComponent<IslandProgressionManager>();
            }

            GameObject host = CreateSingleton<AcceptanceConversation>();
            host.AddComponent<AcceptanceConversation>();

            Assert.IsFalse(AcceptanceConversation.Instance.CanPlayAcceptanceConversation(), "Pre-test: gated without proper state.");
        }
        finally
        {
            Cleanup();
            if (tracker != null) Object.DestroyImmediate(tracker.gameObject);
            if (progression != null) Object.DestroyImmediate(progression.gameObject);
        }
    }

    private void TestSelfHarmBeatGatesOnBadEnding()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<SelfHarmBeat>();
            SelfHarmBeat beat = host.AddComponent<SelfHarmBeat>();
            Assert.IsNotNull(SelfHarmBeat.Instance, "SelfHarmBeat.Instance should be set.");
            Assert.IsFalse(beat.CanPlaySelfHarmSequence(), "Without a bad ending, sequence should be gated.");
            Assert.IsFalse(beat.PlaySelfHarmSequence(), "Should return false when gate fails.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestSelfHarmBeatPlaysFourLinesAndFiresFinished()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<SelfHarmBeat>();
            SelfHarmBeat beat = host.AddComponent<SelfHarmBeat>();

            int lineCount = 0;
            bool finished = false;
            beat.OnSelfHarmLinePresented += (idx, body) => lineCount++;
            beat.OnSelfHarmSequenceFinished += () => finished = true;

            beat.ForcePlayForDebug();
            Assert.AreEqual(SelfHarmBeat.LineCount, lineCount, "Should fire 4 line events.");
            Assert.IsTrue(finished, "Finished event should fire.");
            Assert.IsTrue(beat.HasPlayed, "HasPlayed should be true after force play.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestSelfHarmBeatPlaysOnceAndIgnoresRepeat()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<SelfHarmBeat>();
            SelfHarmBeat beat = host.AddComponent<SelfHarmBeat>();

            int lineCount = 0;
            beat.OnSelfHarmLinePresented += (idx, body) => lineCount++;
            beat.ForcePlayForDebug();
            int firstCount = lineCount;
            beat.ForcePlayForDebug();
            Assert.AreEqual(firstCount, lineCount, "Second force play should not refire events.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestAncientTextAuthoringProvidesAtLeastEighteenEntries()
    {
        IReadOnlyList<AncientTextData> baseline = AncientTextAuthoring.GetBaselineAuthoredTexts();
        Assert.GreaterOrEqual(baseline.Count, AncientTextAuthoring.MinimumRequiredCount, "Baseline should meet or exceed minimum.");

        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < baseline.Count; i++)
        {
            AncientTextData entry = baseline[i];
            Assert.IsNotNull(entry, "Baseline entry should not be null.");
            Assert.IsTrue(entry.IsValid(), "Baseline entry should be valid.");
            Assert.IsTrue(ids.Add(entry.textId), $"textId '{entry.textId}' should be unique.");
        }
    }

    private void TestAncientTextAuthoringCoversAllSixSinsWithTwoLinesEach()
    {
        string[] sins = { "gluttony", "greed", "sloth", "wrath", "envy", "pride" };
        for (int i = 0; i < sins.Length; i++)
        {
            int count = AncientTextAuthoring.CountEntriesForSin(sins[i]);
            Assert.GreaterOrEqual(count, 2, $"Sin '{sins[i]}' should have at least 2 entries.");
        }
    }

    private void TestAncientTextAuthoringCoversAllThreeActs()
    {
        IReadOnlyList<AncientTextData> baseline = AncientTextAuthoring.GetBaselineAuthoredTexts();
        int act1 = 0, act2 = 0, act3 = 0;
        for (int i = 0; i < baseline.Count; i++)
        {
            string id = baseline[i].textId;
            if (id.Contains("act1")) act1++;
            if (id.Contains("act2")) act2++;
            if (id.Contains("act3")) act3++;
        }
        Assert.Greater(act1, 0, "At least one Act I entry should exist.");
        Assert.Greater(act2, 0, "At least one Act II entry should exist.");
        Assert.Greater(act3, 0, "At least one Act III entry should exist.");
    }

    private void TestRelationshipTrackerAffinityClamped()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<RelationshipTracker>();
            RelationshipTracker tracker = host.AddComponent<RelationshipTracker>();

            tracker.SetAffinity("hero_fire", 250);
            Assert.AreEqual(100, tracker.GetAffinity("hero_fire"), "Affinity should clamp to 100.");

            tracker.SetAffinity("hero_fire", -10);
            Assert.AreEqual(0, tracker.GetAffinity("hero_fire"), "Affinity should clamp to 0.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestRelationshipTrackerTierThresholds()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<RelationshipTracker>();
            RelationshipTracker tracker = host.AddComponent<RelationshipTracker>();

            Assert.AreEqual(RelationshipTracker.RelationshipTier.Stranger, tracker.GetRelationshipTier(0), "0 should be Stranger.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Acquaintance, tracker.GetRelationshipTier(20), "20 should be Acquaintance.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Friend, tracker.GetRelationshipTier(40), "40 should be Friend.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Close, tracker.GetRelationshipTier(60), "60 should be Close.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Bonded, tracker.GetRelationshipTier(80), "80 should be Bonded.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Bonded, tracker.GetRelationshipTier(100), "100 should be Bonded.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestRelationshipTrackerTierChangedFiresOnTransition()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<RelationshipTracker>();
            RelationshipTracker tracker = host.AddComponent<RelationshipTracker>();

            RelationshipTracker.RelationshipTier? oldTier = null;
            RelationshipTracker.RelationshipTier? newTier = null;
            int callCount = 0;
            tracker.OnTierChanged += (id, o, n) =>
            {
                oldTier = o;
                newTier = n;
                callCount++;
            };

            tracker.SetAffinity("hero_fire", 19);
            Assert.AreEqual(0, callCount, "Should not fire when staying in same tier.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Stranger, tracker.GetRelationshipTier("hero_fire"));

            tracker.SetAffinity("hero_fire", 41);
            Assert.AreEqual(1, callCount, "Should fire on tier transition.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Stranger, oldTier, "Old tier should be Stranger.");
            Assert.AreEqual(RelationshipTracker.RelationshipTier.Friend, newTier, "New tier should be Friend.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestPowerBudgetTrackerDefaultBudgetSeedsAllIslands()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<PowerBudgetTracker>();
            host.AddComponent<PowerBudgetTracker>();

            IReadOnlyList<string> islands = IslandThemeRegistry.ProgressionOrder;
            Assert.Greater(islands.Count, 0, "There should be islands in the progression order.");
            for (int i = 0; i < islands.Count; i++)
            {
                float remaining = PowerBudgetTracker.Instance.GetRemainingBudget(islands[i]);
                Assert.Greater(remaining, 0f, $"Island '{islands[i]}' should have a default budget.");
            }
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestPowerBudgetTrackerTryConsumeSucceedsWithinBudget()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<PowerBudgetTracker>();
            PowerBudgetTracker tracker = host.AddComponent<PowerBudgetTracker>();
            tracker.SetBudget("island_gluttony", 2f);
            Assert.IsTrue(tracker.TryConsumeBudget("island_gluttony", 1.5f), "Should succeed within budget.");
            Assert.AreEqual(0.5f, tracker.GetRemainingBudget("island_gluttony"), 0.001f, "Should leave 0.5 after consuming 1.5.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestPowerBudgetTrackerTryConsumeRejectsWhenInsufficient()
    {
        Cleanup();
        try
        {
            GameObject host = CreateSingleton<PowerBudgetTracker>();
            PowerBudgetTracker tracker = host.AddComponent<PowerBudgetTracker>();
            tracker.SetBudget("island_wrath", 1f);
            Assert.IsFalse(tracker.TryConsumeBudget("island_wrath", 2f), "Should reject when cost exceeds budget.");
            Assert.AreEqual(1f, tracker.GetRemainingBudget("island_wrath"), 0.001f, "Budget should be unchanged on rejected consume.");
            Assert.IsFalse(tracker.TryConsumeBudget("island_wrath", -1f), "Negative cost should be rejected.");
        }
        finally
        {
            Cleanup();
        }
    }

    private void TestNarrativeBeatsDataContainsAllConstants()
    {
        Assert.IsTrue(NarrativeBeatsData.ContainsId(NarrativeBeatsData.GoodEndingBeatId), "Should contain good ending beat id.");
        Assert.IsTrue(NarrativeBeatsData.ContainsId(NarrativeBeatsData.BadEndingBeatId), "Should contain bad ending beat id.");
        Assert.IsTrue(NarrativeBeatsData.ContainsId(NarrativeBeatsData.SelfHarmBeatId), "Should contain self harm beat id.");
        Assert.IsTrue(NarrativeBeatsData.ContainsId(NarrativeBeatsData.AcceptanceConversationId), "Should contain acceptance conversation id.");
        Assert.IsTrue(NarrativeBeatsData.ContainsId(NarrativeBeatsData.PreFinalBossConversationId), "Should contain pre final boss conversation id.");
        Assert.IsFalse(NarrativeBeatsData.ContainsId("narrative_does_not_exist"), "Should not contain a non-existent id.");
        Assert.GreaterOrEqual(NarrativeBeatsData.GetAllBeats().Count, 5, "Should return at least 5 beat definitions.");
    }
}
