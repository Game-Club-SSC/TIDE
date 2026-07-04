using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class VerticalSliceRegressionRunnerTest : MonoBehaviour
{
    [ContextMenu("Run Vertical Slice Regression Runner Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Vertical Slice Regression Runner Tests ===");

        TestRunnerRegistersAll32Checks();
        TestRunnerExecutesAllChecks();
        TestRunnerTracksPassedAndFailedCounts();
        TestPerIslandContentRegistryCoverage();
        TestPuzzleVariantServiceGluttony();
        TestPuzzleVariantServiceGreed();
        TestSlothStatusEffectSlow();
        TestSlothStatusEffectDrowsy();
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

    private void TestRunnerRegistersAll32Checks()
    {
        GameObject host = new GameObject("Test_Regression");
        VerticalSliceRegressionRunner runner = host.AddComponent<VerticalSliceRegressionRunner>();
        try
        {
            Assert.GreaterOrEqual(runner.TotalCount, 32, "Runner should have at least 32 checks.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestRunnerExecutesAllChecks()
    {
        GameObject host = new GameObject("Test_Runner2");
        VerticalSliceRegressionRunner runner = host.AddComponent<VerticalSliceRegressionRunner>();
        try
        {
            runner.RunRegression();
            Assert.AreEqual(runner.TotalCount, runner.PassedCount + runner.FailedCount, "All checks should be tallied.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestRunnerTracksPassedAndFailedCounts()
    {
        GameObject host = new GameObject("Test_Runner3");
        VerticalSliceRegressionRunner runner = host.AddComponent<VerticalSliceRegressionRunner>();
        try
        {
            Assert.GreaterOrEqual(runner.PassedCount, 0, "PassedCount should be non-negative.");
            Assert.GreaterOrEqual(runner.FailedCount, 0, "FailedCount should be non-negative.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestPerIslandContentRegistryCoverage()
    {
        IReadOnlyList<PerIslandContentRegistry.IslandContentPack> packs = PerIslandContentRegistry.GetAllPacks();
        Assert.GreaterOrEqual(packs.Count, 6, "Should have 6 island content packs.");
        Assert.IsNotNull(PerIslandContentRegistry.GetPackForIsland("island_pride"), "Should find pride pack.");
    }

    private void TestPuzzleVariantServiceGluttony()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        try
        {
            data.enableConsumption = true;
            data.consumptionAmount = 3;
            Assert.IsTrue(PuzzleVariantService.IsGluttonyConsumptionEnabled(data), "Gluttony should be on.");
            Assert.AreEqual(3, PuzzleVariantService.GetConsumptionAmount(data), "Consumption amount should be 3.");
            Assert.AreEqual("gluttony", PuzzleVariantService.GetVariantLabel(data), "Label should be gluttony.");
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

    private void TestSlothStatusEffectSlow()
    {
        StatusEffect slow = SlothStatusEffectSet.CreateSlowEffect("test", 3, 0.5f);
        Assert.IsNotNull(slow, "Slow should be created.");
        Assert.AreEqual(StatusEffectType.Slow, slow.Type, "Type should be Slow.");
        Assert.AreEqual(3, slow.Duration, "Duration should be 3.");
        Assert.AreEqual(0.5f, slow.Magnitude, 0.001f, "Magnitude should be 0.5.");
    }

    private void TestSlothStatusEffectDrowsy()
    {
        StatusEffect drowsy = SlothStatusEffectSet.CreateDrowsyEffect("test", 2);
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
        Assert.IsFalse(PlayerCustomizationCatalog.IsPaletteUnlocked("palette_cosmic"), "Cosmic should start locked.");
        Assert.IsTrue(PlayerCustomizationCatalog.UnlockPalette("palette_cosmic"), "Should unlock cosmic.");
        Assert.IsTrue(PlayerCustomizationCatalog.IsPaletteUnlocked("palette_cosmic"), "Cosmic should be unlocked.");
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
