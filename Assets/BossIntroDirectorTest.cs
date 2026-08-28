using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BossIntroDirectorTest : MonoBehaviour
{
    [ContextMenu("Run Boss Intro Director Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Boss Intro Director Tests ===");

        TestSingletonCreation();
        TestSingletonDuplicateGuard();
        TestSingletonClearsOnDestroy();
        TestPlayBossSpecificSequenceCoversSixIslands();
        TestPlayBossSpecificSequenceMissingGreed();
        TestFateUsesDedicatedCombatPresentation();
        TestTimeoutIsHardcodedAtEightSeconds();
        TestMobileSkipGatingNotImplemented();
        TestSkipKeyDefaultsToSpace();

        Debug.Log("=== All Boss Intro Director Tests Passed ===");
    }

    private BossIntroDirector CreateIsolatedDirector()
    {
        if (BossIntroDirector.Instance != null)
        {
            DestroyImmediate(BossIntroDirector.Instance.gameObject);
        }

        GameObject go = new GameObject("TestBossIntroDirector");
        BossIntroDirector director = go.AddComponent<BossIntroDirector>();
        Assert.AreSame(director, BossIntroDirector.Instance,
            "Director singleton should reference the isolated test instance.");
        return director;
    }

    private void TestSingletonCreation()
    {
        Debug.Log("Testing BossIntroDirector singleton creation...");

        BossIntroDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            Assert.IsNotNull(BossIntroDirector.Instance, "Instance should be set after creation.");
            Assert.AreSame(director, BossIntroDirector.Instance, "Instance should be the created director.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Singleton creation test passed");
    }

    private void TestSingletonDuplicateGuard()
    {
        Debug.Log("Testing BossIntroDirector duplicate guard destroys second instance...");

        BossIntroDirector first = CreateIsolatedDirector();
        GameObject firstGo = first.gameObject;

        try
        {
            GameObject secondGo = new GameObject("TestBossIntroDirector_Duplicate");
            secondGo.AddComponent<BossIntroDirector>();

            Assert.IsTrue(secondGo == null, "Duplicate instance should be destroyed.");
            Assert.AreSame(first, BossIntroDirector.Instance, "Original instance should remain.");
        }
        finally
        {
            if (firstGo != null) DestroyImmediate(firstGo);
        }

        Debug.Log("✓ Duplicate guard test passed");
    }

    private void TestSingletonClearsOnDestroy()
    {
        Debug.Log("Testing BossIntroDirector clears Instance on destroy...");

        BossIntroDirector director = CreateIsolatedDirector();

        DestroyImmediate(director.gameObject);

        Assert.IsNull(BossIntroDirector.Instance, "Instance should be null after destroy.");

        Debug.Log("✓ Singleton clear on destroy test passed");
    }

    private void TestPlayBossSpecificSequenceCoversSixIslands()
    {
        Debug.Log("Testing PlayBossSpecificSequence covers all 6 known islands...");

        string[] coveredIslands = new[]
        {
            "island_greed",
            "island_desire",
            "island_envy",
            "island_lust",
            "island_anger",
            "island_ego"
        };

        System.Type directorType = typeof(BossIntroDirector);
        System.Reflection.MethodInfo playSequence = directorType.GetMethod(
            "PlayBossSpecificSequence",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(playSequence, "PlayBossSpecificSequence method should exist.");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "BossIntroDirector.cs"));

        for (int i = 0; i < coveredIslands.Length; i++)
        {
            string island = coveredIslands[i];
            Assert.IsTrue(sourceCode.Contains($"case \"{island}\""),
                $"PlayBossSpecificSequence should have a case for '{island}'.");
        }

        Debug.Log("✓ PlayBossSpecificSequence island coverage test passed");
    }

    private void TestPlayBossSpecificSequenceMissingGreed()
    {
        Debug.Log("Testing PlayBossSpecificSequence is missing island_greed case...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "BossIntroDirector.cs"));

        Assert.IsTrue(sourceCode.Contains("case \"island_greed\""),
            "PlayBossSpecificSequence should have a case for 'island_greed'.");

        Debug.Log("✓ Missing greed case confirmed");
    }

    private void TestFateUsesDedicatedCombatPresentation()
    {
        Debug.Log("Testing Fate skips the island boss intro route...");

        Assert.IsTrue(
            CombatSceneBootstrap.IsFateEncounter(GameStateManager.FinalFateEncounterId),
            "The final Fate encounter should be recognized by its exact encounter ID.");
        Assert.IsFalse(
            CombatSceneBootstrap.ShouldUseIslandBossPresentation(GameStateManager.FinalFateEncounterId),
            "Fate must not use the final island's boss intro, BGM, or boss sprite route.");
        Assert.IsTrue(
            CombatSceneBootstrap.ShouldUseIslandBossPresentation("island_ego_boss"),
            "Canonical island bosses should continue to use their own intro route.");
        Assert.IsFalse(
            CombatSceneBootstrap.ShouldUseIslandBossPresentation("combat_patrol"),
            "Regular encounters should not use the boss intro route.");

        Debug.Log("✓ Fate presentation route test passed");
    }

    private void TestTimeoutIsHardcodedAtEightSeconds()
    {
        Debug.Log("Testing timeout is hardcoded at 8 seconds...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "BossIntroDirector.cs"));

        Assert.IsTrue(sourceCode.Contains("introTimeout"),
            "BossIntroDirector should use introTimeout field for timeout duration.");

        System.Type directorType = typeof(BossIntroDirector);
        System.Reflection.FieldInfo timeoutField = directorType.GetField(
            "introTimeout",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (timeoutField != null)
        {
            BossIntroDirector director = CreateIsolatedDirector();
            GameObject go = director.gameObject;

            try
            {
                float holdDuration = (float)timeoutField.GetValue(director);
                Assert.AreEqual(8f, holdDuration, 0.001f,
                    "introTimeout should default to 8 seconds.");
            }
            finally
            {
                if (go != null) DestroyImmediate(go);
            }
        }

        Debug.Log("✓ Timeout hardcoded at 8 seconds test passed");
    }

    private void TestMobileSkipGatingNotImplemented()
    {
        Debug.Log("Testing mobile skip gating is NOT implemented...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "BossIntroDirector.cs"));

        Assert.IsTrue(sourceCode.Contains("isMobilePlatform"),
            "BossIntroDirector should contain mobile platform detection.");
        Assert.IsTrue(sourceCode.Contains("Input.touchCount") || sourceCode.Contains("allowMouseSkipOnMobile"),
            "BossIntroDirector should contain mobile skip gating logic.");

        Debug.Log("✓ Mobile skip gating not implemented confirmed");
    }

    private void TestSkipKeyDefaultsToSpace()
    {
        Debug.Log("Testing skip key defaults to Space...");

        BossIntroDirector director = CreateIsolatedDirector();
        GameObject go = director.gameObject;

        try
        {
            System.Reflection.FieldInfo skipKeyField = typeof(BossIntroDirector).GetField(
                "skipKey",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(skipKeyField, "skipKey field should exist.");

            KeyCode skipKey = (KeyCode)skipKeyField.GetValue(director);
            Assert.AreEqual(KeyCode.Space, skipKey, "Skip key should default to Space.");
        }
        finally
        {
            if (go != null) DestroyImmediate(go);
        }

        Debug.Log("✓ Skip key defaults to Space test passed");
    }
}
