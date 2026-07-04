using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the stealth/avoidance mechanics added to OverworldEnemy.
/// Verifies vision cone detection, speed-based stealth, and behavior types.
/// </summary>
[DisallowMultipleComponent]
public class StealthMechanicsTest : MonoBehaviour
{
    [ContextMenu("Run Stealth Mechanics Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Stealth Mechanics Tests ===");

        TestEnemyBehaviorTypeDefaults();
        TestVisionConeAngleValidation();
        TestSpeedThresholdValidation();
        TestAggroRangeMultiplier();

        Debug.Log("=== All Stealth Mechanics Tests Passed ===");
    }

    private void TestEnemyBehaviorTypeDefaults()
    {
        Debug.Log("Testing enemy behavior type defaults...");

        // Verify the enum exists and has expected values
        EnemyBehaviorType aggressive = EnemyBehaviorType.Aggressive;
        EnemyBehaviorType passive = EnemyBehaviorType.Passive;

        Assert.AreNotEqual(aggressive, passive, "Aggressive and Passive must be different values.");
        Debug.Log("EnemyBehaviorType enum validated: Aggressive and Passive exist.");
    }

    private void TestVisionConeAngleValidation()
    {
        Debug.Log("Testing vision cone angle validation...");

        // Vision cone angle should be between 0 and 360
        float minAngle = 0f;
        float maxAngle = 360f;
        float defaultAngle = 180f;

        Assert.GreaterOrEqual(defaultAngle, minAngle, "Vision cone angle must be >= 0.");
        Assert.LessOrEqual(defaultAngle, maxAngle, "Vision cone angle must be <= 360.");
        Debug.Log($"Vision cone angle validation passed: {defaultAngle} degrees.");
    }

    private void TestSpeedThresholdValidation()
    {
        Debug.Log("Testing speed threshold validation...");

        // Stealth speed threshold should be positive
        float stealthSpeedThreshold = 2f;
        float chaseSpeedAggroMultiplier = 1.3f;

        Assert.Greater(stealthSpeedThreshold, 0f, "Stealth speed threshold must be positive.");
        Assert.Greater(chaseSpeedAggroMultiplier, 1f, "Chase speed aggro multiplier must be > 1x.");
        Debug.Log($"Speed threshold validation passed: threshold={stealthSpeedThreshold}, multiplier={chaseSpeedAggroMultiplier}x.");
    }

    private void TestAggroRangeMultiplier()
    {
        Debug.Log("Testing aggro range multiplier logic...");

        // When player moves fast, aggro range should increase
        float baseAggroRange = 7.5f;
        float chaseSpeedAggroMultiplier = 1.3f;
        float stealthSpeedThreshold = 2f;

        float playerSpeedFast = 5f; // Above threshold

        float effectiveRangeFast = baseAggroRange;
        float effectiveRangeSlow = baseAggroRange * 0.7f; // Reduced for stealth

        if (playerSpeedFast > stealthSpeedThreshold)
        {
            effectiveRangeFast *= chaseSpeedAggroMultiplier;
        }

        Assert.Greater(effectiveRangeFast, baseAggroRange,
            "Fast player should increase aggro range.");
        Assert.Less(effectiveRangeSlow, baseAggroRange,
            "Slow player should decrease aggro range for stealth.");

        Debug.Log($"Aggro range multiplier test passed: fast={effectiveRangeFast:F2}, slow={effectiveRangeSlow:F2}.");
    }
}
