using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the puzzle win condition system.
/// Verifies both early-game (percentage) and late-game (all tiles exactly 5) conditions.
/// </summary>
[DisallowMultipleComponent]
public class PuzzleWinConditionTest : MonoBehaviour
{
    [ContextMenu("Run Puzzle Win Condition Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Puzzle Win Condition Tests ===");

        TestAllEqualToTargetWinCondition();
        TestPercentageAtTargetWinCondition();
        TestSealedTilesExcludedFromCount();
        TestEarlyGameVsLateGameConditions();
        TestEdgeCases();

        Debug.Log("=== All Puzzle Win Condition Tests Passed ===");
    }

    private void TestAllEqualToTargetWinCondition()
    {
        Debug.Log("Testing AllEqualToTarget win condition...");

        PuzzleWinCondition condition = new PuzzleWinCondition
        {
            type = WinConditionType.AllEqualToTarget,
            targetValue = 5,
            requiredPercent = 1f
        };

        // All tiles at 5 should win
        int[,] allBalanced = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };
        Assert.IsTrue(condition.IsMet(allBalanced, null), "All tiles at 5 should meet win condition.");

        // Some tiles not at 5 should fail
        int[,] partiallyBalanced = {
            { 5, 5, 5 },
            { 5, 6, 5 },
            { 5, 5, 5 }
        };
        Assert.IsFalse(condition.IsMet(partiallyBalanced, null), "Partially balanced grid should not meet win condition.");

        Debug.Log("AllEqualToTarget win condition: PASS");
    }

    private void TestPercentageAtTargetWinCondition()
    {
        Debug.Log("Testing PercentageAtTarget win condition...");

        PuzzleWinCondition condition = new PuzzleWinCondition
        {
            type = WinConditionType.PercentageAtTarget,
            targetValue = 5,
            requiredPercent = 0.75f // 75% of tiles must be at 5
        };

        // 75% of 9 tiles = 6.75, so 7 tiles at 5 should pass
        int[,] seventyFivePercent = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 3, 7 }
        };
        Assert.IsTrue(condition.IsMet(seventyFivePercent, null), "75% of tiles at 5 should meet win condition.");

        // Only 5 tiles at 5 (55%) should fail
        int[,] fiftyFivePercent = {
            { 5, 5, 5 },
            { 5, 3, 7 },
            { 4, 6, 5 }
        };
        Assert.IsFalse(condition.IsMet(fiftyFivePercent, null), "55% of tiles at 5 should not meet win condition.");

        Debug.Log("PercentageAtTarget win condition: PASS");
    }

    private void TestSealedTilesExcludedFromCount()
    {
        Debug.Log("Testing sealed tiles are excluded from count...");

        PuzzleWinCondition condition = new PuzzleWinCondition
        {
            type = WinConditionType.AllEqualToTarget,
            targetValue = 5,
            requiredPercent = 1f
        };

        // Grid with one sealed tile - all non-sealed tiles should be at 5
        int[,] gridWithSealed = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };

        bool[,] sealedTiles = {
            { false, false, false },
            { false, true, false },
            { false, false, false }
        };

        // Even though center tile is "6", it's sealed so should be excluded
        int[,] gridWithSealedAndNonBalanced = {
            { 5, 5, 5 },
            { 5, 6, 5 },
            { 5, 5, 5 }
        };

        Assert.IsTrue(condition.IsMet(gridWithSealedAndNonBalanced, sealedTiles),
            "Sealed tiles should be excluded from win condition check.");

        Debug.Log("Sealed tiles excluded: PASS");
    }

    private void TestEarlyGameVsLateGameConditions()
    {
        Debug.Log("Testing early-game vs late-game win conditions...");

        // Early game: percentage of tiles at 5
        PuzzleWinCondition earlyGame = new PuzzleWinCondition
        {
            type = WinConditionType.PercentageAtTarget,
            targetValue = 5,
            requiredPercent = 0.75f
        };

        // Late game: all tiles exactly 5
        PuzzleWinCondition lateGame = new PuzzleWinCondition
        {
            type = WinConditionType.AllEqualToTarget,
            targetValue = 5,
            requiredPercent = 1f
        };

        // Grid where 75% are at 5 but not all
        int[,] seventyFivePercentGrid = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 3, 7 }
        };

        Assert.IsTrue(earlyGame.IsMet(seventyFivePercentGrid, null),
            "Early game should pass with 75% at target.");
        Assert.IsFalse(lateGame.IsMet(seventyFivePercentGrid, null),
            "Late game should fail if not all tiles are at target.");

        Debug.Log("Early game vs late game: PASS");
    }

    private void TestEdgeCases()
    {
        Debug.Log("Testing edge cases...");

        PuzzleWinCondition condition = new PuzzleWinCondition
        {
            type = WinConditionType.AllEqualToTarget,
            targetValue = 5,
            requiredPercent = 1f
        };

        // Null grid should return false
        Assert.IsFalse(condition.IsMet(null, null), "Null grid should return false.");

        // Empty grid (0 tiles) should return true (vacuously true)
        int[,] emptyGrid = new int[0, 0];
        Assert.IsTrue(condition.IsMet(emptyGrid, null), "Empty grid should return true (vacuously).");

        // Grid with all sealed tiles should return true (no tiles to check)
        int[,] allSealed = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };
        bool[,] allSealedTiles = {
            { true, true, true },
            { true, true, true },
            { true, true, true }
        };
        Assert.IsTrue(condition.IsMet(allSealed, allSealedTiles), "All sealed tiles should return true.");

        Debug.Log("Edge cases: PASS");
    }
}
