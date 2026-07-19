using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class AncientTextRevealDirectorTest : MonoBehaviour
{
    private AncientTextRevealDirector previousDirectorInstance;

    [ContextMenu("Run Ancient Text Reveal Director Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Ancient Text Reveal Director Tests ===");

        TestSingletonCreation();
        TestSingletonDuplicateGuard();
        TestSingletonClearsOnDestroy();
        TestDontDestroyOnLoad();
        TestForceDiscoverFragmentReturnsTrue();
        TestForceDiscoverFragmentDuplicateReturnsFalse();
        TestForceDiscoverFragmentNullIdReturnsFalse();
        TestTriggerNarrativeBeatForFragmentIsNotNoop();
        TestHeroBondingStateTracksBondLevel();
        TestHeroBondingStateAffectsGameplay();
        TestGetRevealStageIncrements();
        TestGetOverallNarrativeStateChanges();
        TestSaveDataRoundTrip();
        TestResetForDebug();

        Debug.Log("=== All Ancient Text Reveal Director Tests Passed ===");
    }

    private AncientTextRevealDirector CreateIsolatedDirector()
    {
        previousDirectorInstance = AncientTextRevealDirector.Instance;
        SetDirectorInstance(null);

        GameObject go = new GameObject("TestAncientTextRevealDirector");
        AncientTextRevealDirector director = go.AddComponent<AncientTextRevealDirector>();
        go.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(director, AncientTextRevealDirector.Instance,
            "Director singleton should reference the isolated test instance.");
        return director;
    }

    private void CleanupIsolatedDirector(GameObject go)
    {
        if (go != null)
        {
            DestroyImmediate(go);
        }

        RestorePreviousDirectorInstance();
    }

    private void RestorePreviousDirectorInstance()
    {
        SetDirectorInstance(previousDirectorInstance);
        previousDirectorInstance = null;
    }

    private static void SetDirectorInstance(AncientTextRevealDirector value)
    {
        System.Reflection.FieldInfo field = typeof(AncientTextRevealDirector).GetField(
            "<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Director singleton backing field should exist.");
        field.SetValue(null, value);
    }

    private void SetFragments(AncientTextRevealDirector director, AncientTextRevealDirector.AncientTextFragment[] frags)
    {
        System.Reflection.FieldInfo field = typeof(AncientTextRevealDirector).GetField(
            "fragments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(director, frags);
    }

    private void TestSingletonCreation()
    {
        Debug.Log("Testing AncientTextRevealDirector singleton creation...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            Assert.IsNotNull(AncientTextRevealDirector.Instance, "Instance should be set.");
            Assert.AreSame(director, AncientTextRevealDirector.Instance, "Instance should match.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ Singleton creation test passed");
    }

    private void TestSingletonDuplicateGuard()
    {
        Debug.Log("Testing AncientTextRevealDirector duplicate guard...");

        AncientTextRevealDirector first = CreateIsolatedDirector();
        GameObject firstGo = first.gameObject;
        GameObject secondGo = null;

        try
        {
            secondGo = new GameObject("TestAncientTextRevealDirector_Duplicate");
            secondGo.AddComponent<AncientTextRevealDirector>();
            secondGo.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            Assert.AreSame(first, AncientTextRevealDirector.Instance, "Original should remain.");
        }
        finally
        {
            if (secondGo != null) DestroyImmediate(secondGo);
            CleanupIsolatedDirector(firstGo);
        }

        Debug.Log("✓ Duplicate guard test passed");
    }

    private void TestSingletonClearsOnDestroy()
    {
        Debug.Log("Testing AncientTextRevealDirector clears Instance on destroy...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        try
        {
            director.SendMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
            DestroyImmediate(director.gameObject);
            Assert.IsNull(AncientTextRevealDirector.Instance, "Instance should be null after destroy.");
        }
        finally
        {
            RestorePreviousDirectorInstance();
        }

        Debug.Log("✓ Singleton clear on destroy test passed");
    }

    private void TestDontDestroyOnLoad()
    {
        Debug.Log("Testing AncientTextRevealDirector uses DontDestroyOnLoad...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            string sourceCode = System.IO.File.ReadAllText(
                System.IO.Path.Combine(Application.dataPath, "AncientTextRevealDirector.cs"));

            Assert.IsTrue(sourceCode.Contains("DontDestroyOnLoad(gameObject)"),
                "AncientTextRevealDirector should use DontDestroyOnLoad(gameObject) in OnEnable.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ DontDestroyOnLoad test passed");
    }

    private void TestForceDiscoverFragmentReturnsTrue()
    {
        Debug.Log("Testing ForceDiscoverFragment returns true for new fragment...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "test_frag_1",
                    title = "Test Fragment",
                    body = "Test body",
                    requiredIslandIndex = 0,
                    requiredRestorationPercent = 0f,
                    relatedHeroId = "hero_fire"
                }
            });

            bool result = director.ForceDiscoverFragment("test_frag_1");
            Assert.IsTrue(result, "ForceDiscoverFragment should return true for a new fragment.");
            CollectionAssert.Contains(director.DiscoveredFragmentIds, "test_frag_1",
                "Fragment should be in discovered set.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ ForceDiscoverFragment returns true test passed");
    }

    private void TestForceDiscoverFragmentDuplicateReturnsFalse()
    {
        Debug.Log("Testing ForceDiscoverFragment returns false for duplicate...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "test_frag_dup",
                    title = "Dup Fragment",
                    body = "Dup body"
                }
            });

            director.ForceDiscoverFragment("test_frag_dup");
            bool secondResult = director.ForceDiscoverFragment("test_frag_dup");

            Assert.IsFalse(secondResult, "ForceDiscoverFragment should return false for already-discovered fragment.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ ForceDiscoverFragment duplicate test passed");
    }

    private void TestForceDiscoverFragmentNullIdReturnsFalse()
    {
        Debug.Log("Testing ForceDiscoverFragment returns false for null id...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            bool result = director.ForceDiscoverFragment(null);
            Assert.IsFalse(result, "ForceDiscoverFragment should return false for null id.");

            result = director.ForceDiscoverFragment("");
            Assert.IsFalse(result, "ForceDiscoverFragment should return false for empty id.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ ForceDiscoverFragment null id test passed");
    }

    private void TestTriggerNarrativeBeatForFragmentIsNotNoop()
    {
        Debug.Log("Testing TriggerNarrativeBeatForFragment is not a pure noop...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "AncientTextRevealDirector.cs"));

        Assert.IsTrue(sourceCode.Contains("DialogueSystem.Instance"),
            "TriggerNarrativeBeatForFragment should use DialogueSystem.Instance.");
        Assert.IsTrue(sourceCode.Contains("StartDialogueTree"),
            "TriggerNarrativeBeatForFragment should call StartDialogueTree.");

        Debug.Log("✓ TriggerNarrativeBeatForFragment not noop test passed");
    }

    private void TestHeroBondingStateTracksBondLevel()
    {
        Debug.Log("Testing HeroBondingState tracks bond level correctly...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "bond_frag_1",
                    title = "Bond Fragment 1",
                    body = "Body",
                    relatedHeroId = "hero_fire"
                },
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "bond_frag_2",
                    title = "Bond Fragment 2",
                    body = "Body 2",
                    relatedHeroId = "hero_fire"
                }
            });

            director.ForceDiscoverFragment("bond_frag_1");

            AncientTextRevealDirector.HeroBondingState state = director.GetHeroBonding("hero_fire");
            Assert.IsNotNull(state, "Hero bonding state should exist after fragment discovery.");
            Assert.AreEqual(1, state.bondLevel, "Bond level should be 1 after one fragment.");
            Assert.IsTrue(state.IsBonded, "Hero should be marked as bonded.");

            director.ForceDiscoverFragment("bond_frag_2");

            state = director.GetHeroBonding("hero_fire");
            Assert.AreEqual(2, state.bondLevel, "Bond level should be 2 after two fragments.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ HeroBondingState tracks bond level test passed");
    }

    private void TestHeroBondingStateAffectsGameplay()
    {
        Debug.Log("Testing HeroBondingState affects gameplay via OnHeroBondLevelChanged event...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "event_frag_1",
                    title = "Event Fragment",
                    body = "Body",
                    relatedHeroId = "hero_earth"
                }
            });

            string eventHeroId = null;
            int eventBondLevel = -1;
            director.OnHeroBondLevelChanged += (heroId, level) =>
            {
                eventHeroId = heroId;
                eventBondLevel = level;
            };

            director.ForceDiscoverFragment("event_frag_1");

            Assert.AreEqual("hero_earth", eventHeroId, "Event should pass the correct hero ID.");
            Assert.AreEqual(1, eventBondLevel, "Event should pass the correct bond level.");

            List<AncientTextRevealDirector.HeroBondingState> allBonding = director.GetAllHeroBonding();
            Assert.AreEqual(1, allBonding.Count, "Should have one hero in bonding list.");
            Assert.AreEqual("hero_earth", allBonding[0].heroId, "Bonded hero should be hero_earth.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ HeroBondingState affects gameplay test passed");
    }

    private void TestGetRevealStageIncrements()
    {
        Debug.Log("Testing GetRevealStage increments with discoveries...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "stage_frag_1",
                    title = "Stage 1",
                    body = "Body"
                },
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "stage_frag_2",
                    title = "Stage 2",
                    body = "Body"
                }
            });

            Assert.AreEqual(0, director.GetRevealStage(), "Initial reveal stage should be 0.");

            director.ForceDiscoverFragment("stage_frag_1");
            Assert.AreEqual(1, director.GetRevealStage(), "Reveal stage should be 1 after first discovery.");

            director.ForceDiscoverFragment("stage_frag_2");
            Assert.AreEqual(2, director.GetRevealStage(), "Reveal stage should be 2 after second discovery.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ GetRevealStage increments test passed");
    }

    private void TestGetOverallNarrativeStateChanges()
    {
        Debug.Log("Testing GetOverallNarrativeState changes with stage...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            string initialState = director.GetOverallNarrativeState();
            Assert.IsTrue(initialState.Contains("completely unknown"),
                "Initial narrative state should indicate nothing is known.");

            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "narrative_frag_1",
                    title = "Narrative 1",
                    body = "Body"
                }
            });

            director.ForceDiscoverFragment("narrative_frag_1");

            string afterState = director.GetOverallNarrativeState();
            Assert.IsTrue(afterState.Contains("whispers"),
                "After one discovery, narrative state should mention whispers.");
            Assert.AreNotEqual(initialState, afterState,
                "Narrative state should change after discovery.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ GetOverallNarrativeState changes test passed");
    }

    private void TestSaveDataRoundTrip()
    {
        Debug.Log("Testing save data round trip...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "save_frag_1",
                    title = "Save Fragment",
                    body = "Body",
                    relatedHeroId = "hero_water"
                }
            });

            director.ForceDiscoverFragment("save_frag_1");

            AncientTextRevealDirector.RevealDirectorSaveData saveData = director.CaptureSaveData();
            Assert.IsNotNull(saveData, "Save data should not be null.");
            Assert.AreEqual(1, saveData.discoveredFragmentIds.Count, "Should have 1 discovered fragment.");
            Assert.AreEqual("save_frag_1", saveData.discoveredFragmentIds[0], "Fragment ID should match.");
            Assert.AreEqual(1, saveData.bondingEntries.Count, "Should have 1 bonding entry.");
            Assert.AreEqual("hero_water", saveData.bondingEntries[0].heroId, "Bonding hero should match.");

            director.ResetForDebug();
            Assert.AreEqual(0, director.DiscoveredFragmentIds.Count, "Reset should clear fragments.");

            director.ApplySaveData(saveData);
            Assert.AreEqual(1, director.DiscoveredFragmentIds.Count, "Restore should bring back fragments.");
            CollectionAssert.Contains(director.DiscoveredFragmentIds, "save_frag_1",
                "Restored data should contain the fragment.");

            AncientTextRevealDirector.HeroBondingState state = director.GetHeroBonding("hero_water");
            Assert.IsNotNull(state, "Bonding state should be restored.");
            Assert.AreEqual(1, state.bondLevel, "Bond level should be restored.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ Save data round trip test passed");
    }

    private void TestResetForDebug()
    {
        Debug.Log("Testing ResetForDebug clears all state...");

        AncientTextRevealDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            SetFragments(director, new[]
            {
                new AncientTextRevealDirector.AncientTextFragment
                {
                    fragmentId = "reset_frag_1",
                    title = "Reset Fragment",
                    body = "Body",
                    relatedHeroId = "hero_air"
                }
            });

            director.ForceDiscoverFragment("reset_frag_1");
            Assert.AreEqual(1, director.DiscoveredFragmentIds.Count, "Should have 1 fragment before reset.");

            director.ResetForDebug();

            Assert.AreEqual(0, director.DiscoveredFragmentIds.Count, "Should have 0 fragments after reset.");
            Assert.AreEqual(0, director.GetRevealStage(), "Reveal stage should be 0 after reset.");
            Assert.IsNull(director.GetHeroBonding("hero_air"), "Bonding state should be null after reset.");
        }
        finally
        {
            CleanupIsolatedDirector(go);
        }

        Debug.Log("✓ ResetForDebug test passed");
    }
}
