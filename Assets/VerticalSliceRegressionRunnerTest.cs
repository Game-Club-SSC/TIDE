using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class VerticalSliceRegressionRunnerTest : MonoBehaviour
{
    private static VerticalSliceRegressionRunner previousRunnerInstance;

    [ContextMenu("Run Vertical Slice Regression Runner Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Vertical Slice Regression Runner Tests ===");

        TestRunnerRegistersAll31Checks();
        TestRunnerExecutesAllChecks();
        TestRunnerTracksPassedAndFailedCounts();
        TestPerIslandContentRegistryCoverage();
        TestPuzzleVariantServiceConsumption();
        TestPuzzleVariantServiceGreed();
        TestDesireStatusEffectSlow();
        TestDesireStatusEffectDrowsy();
        TestEnvyMirrorToggle();
        TestEnvyMirrorSkillBuilder();
        TestBattleHudCritFlashColor();
        TestBattleHudStatusEffectLabels();
        TestPlayerCustomizationDefaultPalettes();
        TestPlayerCustomizationPremiumUnlocks();
        TestPhoneControllerAuthGenerateAndValidate();
        TestPhoneControllerAuthRevoke();
        TestDevCheatFeatureFlagsCoverage();

        Debug.Log("=== All Vertical Slice Regression Runner Tests Passed ===");
    }

    private void TestRunnerRegistersAll31Checks()
    {
        VerticalSliceRegressionRunner runner = CreateIsolatedRunner("Test_Regression");
        GameObject host = runner.gameObject;
        try
        {
            Assert.AreEqual(31, runner.TotalCount, "Runner should have one check for each issue from 10 through 40.");
        }
        finally
        {
            CleanupIsolatedRunner(host);
        }
    }

    private void TestRunnerExecutesAllChecks()
    {
        VerticalSliceRegressionRunner runner = CreateIsolatedRunner("Test_Runner2");
        GameObject host = runner.gameObject;
        try
        {
            runner.RunRegression();
            Assert.AreEqual(runner.TotalCount, runner.PassedCount + runner.FailedCount, "All checks should be tallied.");
        }
        finally
        {
            CleanupIsolatedRunner(host);
        }
    }

    private void TestRunnerTracksPassedAndFailedCounts()
    {
        VerticalSliceRegressionRunner runner = CreateIsolatedRunner("Test_Runner3");
        GameObject host = runner.gameObject;
        try
        {
            Assert.GreaterOrEqual(runner.PassedCount, 0, "PassedCount should be non-negative.");
            Assert.GreaterOrEqual(runner.FailedCount, 0, "FailedCount should be non-negative.");
        }
        finally
        {
            CleanupIsolatedRunner(host);
        }
    }

    private static VerticalSliceRegressionRunner CreateIsolatedRunner(string objectName)
    {
        previousRunnerInstance = VerticalSliceRegressionRunner.Instance;
        SetRunnerInstance(null);

        GameObject host = new GameObject(objectName);
        VerticalSliceRegressionRunner runner = host.AddComponent<VerticalSliceRegressionRunner>();
        host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(runner, VerticalSliceRegressionRunner.Instance,
            "Regression runner singleton should reference the isolated test instance.");
        return runner;
    }

    private static void CleanupIsolatedRunner(GameObject host)
    {
        if (host != null)
        {
            Object.DestroyImmediate(host);
        }

        SetRunnerInstance(previousRunnerInstance);
        previousRunnerInstance = null;
    }

    private static void SetRunnerInstance(VerticalSliceRegressionRunner value)
    {
        System.Reflection.FieldInfo field = typeof(VerticalSliceRegressionRunner).GetField(
            "<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Regression runner singleton backing field should exist.");
        field.SetValue(null, value);
    }

    private void TestPerIslandContentRegistryCoverage()
    {
        IReadOnlyList<PerIslandContentRegistry.IslandContentPack> packs = PerIslandContentRegistry.GetAllPacks();
        Assert.GreaterOrEqual(packs.Count, 6, "Should have 6 island content packs.");
        Assert.IsNotNull(PerIslandContentRegistry.GetPackForIsland("island_ego"), "Should find ego pack.");
    }

    private void TestPuzzleVariantServiceConsumption()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        try
        {
            data.enableConsumption = true;
            data.consumptionAmount = 3;
            Assert.IsTrue(PuzzleVariantService.IsGreedConsumptionEnabled(data), "Greed should be on.");
            Assert.AreEqual(3, PuzzleVariantService.GetConsumptionAmount(data), "Consumption amount should be 3.");
            Assert.AreEqual("greed", PuzzleVariantService.GetVariantLabel(data), "Label should be greed.");
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    private void TestPuzzleVariantServiceGreed()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        try
        {
            data.enableGreedEconomy = true;
            data.coinTileYield = 5;
            Assert.IsTrue(PuzzleVariantService.IsGreedEconomyEnabled(data), "Greed should be on.");
            Assert.AreEqual(5, PuzzleVariantService.GetCoinTileYield(data), "Coin yield should be 5.");
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    private void TestDesireStatusEffectSlow()
    {
        StatusEffect slow = DesireStatusEffectSet.CreateSlowEffect("test", 3, 0.5f);
        Assert.IsNotNull(slow, "Slow should be created.");
        Assert.AreEqual(StatusEffectType.Slow, slow.Type, "Type should be Slow.");
        Assert.AreEqual(3, slow.Duration, "Duration should be 3.");
        Assert.AreEqual(0.5f, slow.Magnitude, 0.001f, "Magnitude should be 0.5.");
    }

    private void TestDesireStatusEffectDrowsy()
    {
        StatusEffect drowsy = DesireStatusEffectSet.CreateDrowsyEffect("test", 2);
        Assert.IsNotNull(drowsy, "Drowsy should be created.");
        Assert.AreEqual(StatusEffectType.Drowsy, drowsy.Type, "Type should be Drowsy.");
        Assert.AreEqual(2, drowsy.Duration, "Duration should be 2.");
    }

    private void TestEnvyMirrorToggle()
    {
        EnvyMirrorService.ResetForDebug();
        Assert.IsFalse(EnvyMirrorService.IsMirrorEnabled, "Default disabled.");
        EnvyMirrorService.SetMirrorEnabled(true);
        EnvyMirrorService.SetMirroredElement(CombatUnit.Element.Water);
        Assert.IsTrue(EnvyMirrorService.IsMirrorEnabled, "Should be enabled.");
        Assert.AreEqual(CombatUnit.Element.Water, EnvyMirrorService.MirroredElement, "Should be Water.");
        EnvyMirrorService.ResetForDebug();
    }

    private void TestEnvyMirrorSkillBuilder()
    {
        SkillData original = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            original.skillName = "TestSkill";
            original.mpCost = 20;
            original.damageMultiplier = 1.5f;
            original.target = SkillTarget.SingleEnemy;
            SkillData mirror = EnvyMirrorService.GetMirrorSkillFor(original, 0.7f);
            Assert.IsNotNull(mirror, "Mirror should be created.");
            Assert.IsTrue(mirror.skillName.Contains("Mirror"), "Mirror name should be marked.");
            Assert.Less(mirror.mpCost, original.mpCost, "Mirror should cost less MP.");
        }
        finally
        {
            Object.DestroyImmediate(original);
        }
    }

    private void TestBattleHudCritFlashColor()
    {
        Color color = BattleHudPolishService.GetCritFlashColor();
        Assert.Greater(color.r + color.g + color.b, 0f, "Crit flash color should be visible.");
    }

    private void TestBattleHudStatusEffectLabels()
    {
        Assert.AreEqual("Slow", BattleHudPolishService.GetStatusEffectLabel(StatusEffectType.Slow), "Slow label should be Slow.");
        Assert.AreEqual("Drowsy", BattleHudPolishService.GetStatusEffectLabel(StatusEffectType.Drowsy), "Drowsy label should be Drowsy.");
    }

    private void TestPlayerCustomizationDefaultPalettes()
    {
        int defaultCount = PlayerCustomizationCatalog.GetDefaultPaletteCount();
        Assert.GreaterOrEqual(defaultCount, 3, "Should have at least 3 default palettes.");
    }

    private void TestPlayerCustomizationPremiumUnlocks()
    {
        PlayerCustomizationCatalog.ResetForDebug();
        try
        {
            Assert.IsFalse(PlayerCustomizationCatalog.IsPaletteUnlocked("palette_cosmic"), "Cosmic should start locked.");
            Assert.IsTrue(PlayerCustomizationCatalog.UnlockPalette("palette_cosmic"), "Should unlock cosmic.");
            Assert.IsTrue(PlayerCustomizationCatalog.IsPaletteUnlocked("palette_cosmic"), "Cosmic should be unlocked.");
        }
        finally
        {
            PlayerCustomizationCatalog.ResetForDebug();
        }
    }

    private void TestPhoneControllerAuthGenerateAndValidate()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        string token = PhoneControllerAuthService.GenerateToken();
        Assert.IsNotNull(token, "Token should be generated.");
        Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token), "Token should validate.");
        Assert.AreEqual(1, PhoneControllerAuthService.GetActiveTokenCount(), "Should have 1 active token.");
    }

    private void TestPhoneControllerAuthRevoke()
    {
        PhoneControllerAuthService.RevokeAllTokens();
        string token = PhoneControllerAuthService.GenerateToken();
        PhoneControllerAuthService.RevokeAllTokens();
        Assert.IsFalse(PhoneControllerAuthService.ValidateToken(token), "Token should not validate after revoke.");
    }

    private void TestDevCheatFeatureFlagsCoverage()
    {
        Assert.AreEqual(32, DevCheatFeatureFlags.TotalFlagCount, "Should track 32 flags.");
        Assert.IsTrue(DevCheatFeatureFlags.LogicOk(), "Flags should be queryable.");
    }
}
