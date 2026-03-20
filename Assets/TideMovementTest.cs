using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class TideMovementTest : MonoBehaviour
{
    private TideManager manager;
    private GameObject managerObject;
    private GameObject tilesRoot;

    [ContextMenu("Run Tide Movement Tests")]
    public void RunTests()
    {
        try
        {
            TestAdjacentCardinalOpen();
            TestDiagonalOpen();
            TestBlockedPathThruSealed();
            TestDiagonalWithAdjacentSealed();
            TestSealedAsDestination();
            TestMaxCarryStepsLimitsRange();
            TestDiagonalEdgeCornerToCorner();
            TestWinConditionAllEqualToTargetMet();
            TestWinConditionAllEqualToTargetNotMet();
            TestWinConditionPercentageMet();
            TestWinConditionPercentageNotMet();
            TestWinConditionSkipsSealedTile();
            TestCompletionIgnoresPermanentAndLockedSealedTiles();
            TestLockedTileCountsAfterEncounterCleared();
            TestPuzzleDataInitializePuzzle();
            TestDecayTriggersAboveThreshold();
            TestDecayDoesNotTriggerAtThreshold();
            TestDecayClampsToMinimum5();
            TestElementAdvantageAllPairs();
            TestElementNeutralSameElement();
            TestElementNeutralNone();
            TestDamageMultiplierStrong();
            TestDamageMultiplierWeak();

            Debug.Log("=== Tide movement tests passed ===");
        }
        finally
        {
            Cleanup();
        }
    }

    private void Setup()
    {
        Cleanup();
        managerObject = new GameObject("TideManager_Test");
        manager = managerObject.AddComponent<TideManager>();
        tilesRoot = new GameObject("TestTiles");
    }

    private void Cleanup()
    {
        if (tilesRoot != null) DestroyImmediate(tilesRoot);
        if (managerObject != null) DestroyImmediate(managerObject);
    }

    private TideTile CreateTile(int col, int row, bool isSealed)
    {
        GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObj.name = $"Tile_{col}_{row}";
        tileObj.transform.SetParent(tilesRoot.transform, false);
        TideTile tile = tileObj.AddComponent<TideTile>();
        tile.Configure(new Vector2Int(col, row), 5, isSealed);
        return tile;
    }

    private void SetSealedTiles(bool[,] sealedArr)
    {
        SetPrivateFieldValue("sealedTiles", sealedArr);
    }

    private bool CanReach(TideTile source, TideTile destination)
    {
        MethodInfo method = typeof(TideManager).GetMethod("CanReachWithinCarrySteps", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "CanReachWithinCarrySteps should exist on TideManager.");
        return (bool)method.Invoke(manager, new object[] { source, destination });
    }

    private object GetPrivateFieldValue(string fieldName)
    {
        FieldInfo field = typeof(TideManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist on TideManager.");
        return field.GetValue(manager);
    }

    private void SetPrivateFieldValue(string fieldName, object value)
    {
        FieldInfo field = typeof(TideManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist on TideManager.");
        field.SetValue(manager, value);
    }

    private void InvokePrivateMethod(string methodName)
    {
        MethodInfo method = typeof(TideManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist on TideManager.");
        method.Invoke(manager, null);
    }

    private TideTile[,] CreateActiveTiles(int[,] values, bool[,] sealedArr)
    {
        TideTile[,] tiles = new TideTile[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                bool isSealed = sealedArr != null && sealedArr[row, col];
                tiles[row, col] = CreateTile(col, row, isSealed);
                tiles[row, col].currentTideValue = values[row, col];
            }
        }

        return tiles;
    }

    private static PuzzleData CreateLockedTilePuzzleData()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        data.tileValues = new[] { 5, 5, 2, 5, 1, 5, 5, 5, 5 };
        data.sealedPosition = new Vector2Int(1, 1);
        data.lockedPosition = new Vector2Int(2, 0);
        data.lockedTileEncounterId = "guard_1";
        data.winCondition = new PuzzleWinCondition
        {
            type = WinConditionType.AllEqualToTarget,
            targetValue = 5
        };
        return data;
    }

    private static IslandRestorationTracker CreateIsolatedTracker(string trackerName)
    {
        if (IslandRestorationTracker.Instance != null)
        {
            Object.DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        GameObject trackerObject = new GameObject(trackerName);
        IslandRestorationTracker tracker = trackerObject.AddComponent<IslandRestorationTracker>();
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference the isolated test tracker instance.");
        return tracker;
    }

    private void TestAdjacentCardinalOpen()
    {
        Setup();

        // 0 sealed tiles, maxCarrySteps=2
        // Source: center(1,1), Dest: north(1,0)
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(1, 0, false);

        Assert.IsTrue(CanReach(source, dest), "Adjacent cardinal tile should be reachable.");

        Debug.Log("  [PASS] TestAdjacentCardinalOpen");
    }

    private void TestDiagonalOpen()
    {
        Setup();

        // 0 sealed tiles, maxCarrySteps=2
        // Source: center(1,1), Dest: (0,0) diagonal
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(0, 0, false);

        Assert.IsTrue(CanReach(source, dest), "Diagonal tile should be reachable when no sealed tiles block the path.");

        Debug.Log("  [PASS] TestDiagonalOpen");
    }

    private void TestBlockedPathThruSealed()
    {
        Setup();

        // Sealed row: (1,0), (1,1), (1,2)
        // Source: (0,0), Dest: (2,2)
        // All paths must cross row 1, which is fully sealed
        bool[,] sealedArr = new bool[3, 3];
        sealedArr[1, 0] = true;
        sealedArr[1, 1] = true;
        sealedArr[1, 2] = true;
        SetSealedTiles(sealedArr);

        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(2, 2, false);

        Assert.IsFalse(CanReach(source, dest), "Path through sealed tiles should be blocked.");

        Debug.Log("  [PASS] TestBlockedPathThruSealed");
    }

    private void TestDiagonalWithAdjacentSealed()
    {
        Setup();

        // Sealed: (0,1) and (1,0) - the two cardinal neighbors between center and (0,0)
        // Source: center(1,1), Dest: (0,0) diagonal
        // Diagonal is blocked because corner-cutting requires both cardinal neighbors open
        bool[,] sealedArr = new bool[3, 3];
        sealedArr[0, 1] = true;
        sealedArr[1, 0] = true;
        SetSealedTiles(sealedArr);

        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(0, 0, false);

        Assert.IsFalse(CanReach(source, dest), "Diagonal movement should be blocked when adjacent cardinal tiles are sealed.");

        Debug.Log("  [PASS] TestDiagonalWithAdjacentSealed");
    }

    private void TestSealedAsDestination()
    {
        Setup();

        // Default sealedTiles: center(1,1) is sealed
        // Source: (0,0), Dest: center(1,1) sealed
        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(1, 1, true);

        Assert.IsFalse(CanReach(source, dest), "Sealed tile should not be reachable as a destination.");

        Debug.Log("  [PASS] TestSealedAsDestination");
    }

    private void TestMaxCarryStepsLimitsRange()
    {
        Setup();

        // 0 sealed tiles, but maxCarrySteps overridden via reflection to 1
        // Source: (0,0), Dest: (2,2) - minimum 2 steps away
        SetSealedTiles(new bool[3, 3]);

        FieldInfo stepsField = typeof(TideManager).GetField("maxCarrySteps", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(stepsField, "maxCarrySteps field should exist on TideManager.");
        stepsField.SetValue(manager, 1);

        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(2, 2, false);

        Assert.IsFalse(CanReach(source, dest), "Tile beyond maxCarrySteps should not be reachable.");

        Debug.Log("  [PASS] TestMaxCarryStepsLimitsRange");
    }

    private void TestDiagonalEdgeCornerToCorner()
    {
        Setup();

        // 0 sealed tiles, maxCarrySteps=2
        // Source: (0,0), Dest: (2,2) - diagonal corner to corner, 2 steps
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(2, 2, false);

        Assert.IsTrue(CanReach(source, dest), "Corner-to-corner diagonal should be reachable within 2 steps.");

        Debug.Log("  [PASS] TestDiagonalEdgeCornerToCorner");
    }

    private void TestWinConditionAllEqualToTargetMet()
    {
        int[,] grid = {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };
        PuzzleWinCondition wc = new PuzzleWinCondition();
        wc.type = WinConditionType.AllEqualToTarget;
        wc.targetValue = 5;

        Assert.IsTrue(wc.IsMet(grid, new Vector2Int(-1, -1)), "All tiles at 5 should satisfy AllEqualToTarget.");

        Debug.Log("  [PASS] TestWinConditionAllEqualToTargetMet");
    }

    private void TestWinConditionAllEqualToTargetNotMet()
    {
        int[,] grid = {
            { 5, 5, 5 },
            { 5, 3, 5 },
            { 5, 5, 5 }
        };
        PuzzleWinCondition wc = new PuzzleWinCondition();
        wc.type = WinConditionType.AllEqualToTarget;
        wc.targetValue = 5;

        Assert.IsFalse(wc.IsMet(grid, new Vector2Int(-1, -1)), "One tile not at 5 should fail AllEqualToTarget.");

        Debug.Log("  [PASS] TestWinConditionAllEqualToTargetNotMet");
    }

    private void TestWinConditionPercentageMet()
    {
        int[,] grid = {
            { 5, 5, 5 },
            { 5, 3, 5 },
            { 5, 5, 5 }
        };
        PuzzleWinCondition wc = new PuzzleWinCondition();
        wc.type = WinConditionType.PercentageAtTarget;
        wc.targetValue = 5;
        wc.requiredPercent = 0.75f;

        Assert.IsTrue(wc.IsMet(grid, new Vector2Int(-1, -1)), "8/9 tiles at 5 (89%) should satisfy 75% threshold.");

        Debug.Log("  [PASS] TestWinConditionPercentageMet");
    }

    private void TestWinConditionPercentageNotMet()
    {
        int[,] grid = {
            { 5, 5, 3 },
            { 5, 3, 5 },
            { 3, 5, 5 }
        };
        PuzzleWinCondition wc = new PuzzleWinCondition();
        wc.type = WinConditionType.PercentageAtTarget;
        wc.targetValue = 5;
        wc.requiredPercent = 0.75f;

        Assert.IsFalse(wc.IsMet(grid, new Vector2Int(-1, -1)), "6/9 tiles at 5 (67%) should fail 75% threshold.");

        Debug.Log("  [PASS] TestWinConditionPercentageNotMet");
    }

    private void TestWinConditionSkipsSealedTile()
    {
        int[,] grid = {
            { 5, 5, 5 },
            { 5, 2, 5 },
            { 5, 5, 5 }
        };
        PuzzleWinCondition wc = new PuzzleWinCondition();
        wc.type = WinConditionType.AllEqualToTarget;
        wc.targetValue = 5;

        Assert.IsTrue(wc.IsMet(grid, new Vector2Int(1, 1)),
            "Sealed tile at (1,1) should be excluded from win condition check.");

        Debug.Log("  [PASS] TestWinConditionSkipsSealedTile");
    }

    private void TestCompletionIgnoresPermanentAndLockedSealedTiles()
    {
        IslandRestorationTracker tracker = CreateIsolatedTracker("TideManager_LockedTileTracker_Default");
        GameObject trackerObject = tracker.gameObject;
        Setup();

        PuzzleData data = CreateLockedTilePuzzleData();
        try
        {
            manager.InitializePuzzle(data);

            bool[,] sealedArr = (bool[,])GetPrivateFieldValue("sealedTiles");
            Assert.IsTrue(sealedArr[1, 1], "Permanent sealed tile should remain sealed.");
            Assert.IsTrue(sealedArr[0, 2], "Locked tile should stay sealed until its combat encounter is cleared.");

            SetPrivateFieldValue("activeTiles", CreateActiveTiles(data.GetGrid(), sealedArr));
            InvokePrivateMethod("EvaluatePuzzleCompletion");

            Assert.IsTrue((bool)GetPrivateFieldValue("puzzleSolved"),
                "Puzzle should complete when only the currently sealed tiles are off-target.");

            Debug.Log("  [PASS] TestCompletionIgnoresPermanentAndLockedSealedTiles");
        }
        finally
        {
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(trackerObject);
        }
    }

    private void TestLockedTileCountsAfterEncounterCleared()
    {
        IslandRestorationTracker tracker = CreateIsolatedTracker("TideManager_LockedTileTracker");
        GameObject trackerObject = tracker.gameObject;
        tracker.RecordEncounterCompletion("island_lock", "guard_1", EncounterType.Combat, 0.2f);

        Setup();

        PuzzleData data = CreateLockedTilePuzzleData();
        try
        {
            manager.InitializePuzzle(data);

            bool[,] sealedArr = (bool[,])GetPrivateFieldValue("sealedTiles");
            Assert.IsTrue(sealedArr[1, 1], "Permanent sealed tile should still be sealed.");
            Assert.IsFalse(sealedArr[0, 2], "Locked tile should open once its guarding encounter is cleared.");

            TideTile[,] tiles = CreateActiveTiles(data.GetGrid(), sealedArr);
            SetPrivateFieldValue("activeTiles", tiles);

            InvokePrivateMethod("EvaluatePuzzleCompletion");
            Assert.IsFalse((bool)GetPrivateFieldValue("puzzleSolved"),
                "Unlocked locked tiles should count toward completion while off-target.");

            tiles[0, 2].currentTideValue = 5;
            InvokePrivateMethod("EvaluatePuzzleCompletion");

            Assert.IsTrue((bool)GetPrivateFieldValue("puzzleSolved"),
                "Puzzle should complete once the unlocked tile reaches the target value.");

            Debug.Log("  [PASS] TestLockedTileCountsAfterEncounterCleared");
        }
        finally
        {
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(trackerObject);
        }
    }

    private void TestPuzzleDataInitializePuzzle()
    {
        Setup();

        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        data.tileValues = new int[] { 8, 5, 3, 6, 5, 4, 5, 5, 5 };
        data.sealedPosition = new Vector2Int(2, 0);
        data.winCondition = new PuzzleWinCondition
        {
            type = WinConditionType.PercentageAtTarget,
            targetValue = 5,
            requiredPercent = 0.6f
        };

        manager.InitializePuzzle(data);

        int[,] values = (int[,])GetPrivateFieldValue("puzzleValues");
        Assert.AreEqual(8, values[0, 0], "Tile (0,0) should be 8.");
        Assert.AreEqual(5, values[0, 1], "Tile (0,1) should be 5.");
        Assert.AreEqual(3, values[0, 2], "Tile (0,2) should be 3.");

        bool[,] sealedArr = (bool[,])GetPrivateFieldValue("sealedTiles");
        Assert.IsTrue(sealedArr[0, 2], "Tile at row 0 col 2 should be sealed.");
        Assert.IsFalse(sealedArr[1, 1], "Tile at row 1 col 1 should not be sealed.");

        PuzzleWinCondition cond = (PuzzleWinCondition)GetPrivateFieldValue("winCondition");
        Assert.AreEqual(WinConditionType.PercentageAtTarget, cond.type, "Win condition type should be PercentageAtTarget.");
        Assert.AreEqual(0.6f, cond.requiredPercent, "Required percent should be 0.6.");

        Object.DestroyImmediate(data);
        Debug.Log("  [PASS] TestPuzzleDataInitializePuzzle");
    }

    private void TestDecayTriggersAboveThreshold()
    {
        Setup();

        // 4 tiles above 5, threshold=3, so decay = 4-3 = 1
        // Create tiles at values: 8, 7, 6, 6 (all > 5), rest at 5
        TideTile[,] tiles = new TideTile[3, 3];
        int[,] values = {
            { 8, 5, 5 },
            { 7, 5, 5 },
            { 6, 6, 5 }
        };

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                tiles[r, c] = CreateTile(c, r, false);
                tiles[r, c].currentTideValue = values[r, c];
            }
        }

        SetSealedTiles(new bool[3, 3]);

        FieldInfo thresholdField = typeof(TideManager).GetField("instabilityThreshold", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(thresholdField, "instabilityThreshold field should exist.");
        thresholdField.SetValue(manager, 3);

        FieldInfo tilesField = typeof(TideManager).GetField("activeTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(tilesField, "activeTiles field should exist.");
        tilesField.SetValue(manager, tiles);

        MethodInfo decayMethod = typeof(TideManager).GetMethod("ApplyInstabilityDecay", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(decayMethod, "ApplyInstabilityDecay should exist.");
        decayMethod.Invoke(manager, null);

        // After decay=1: 8->7, 7->6, 6->5, 6->5
        Assert.AreEqual(7, tiles[0, 0].CurrentTideValue, "Tile (0,0) should decay from 8 to 7.");
        Assert.AreEqual(6, tiles[1, 0].CurrentTideValue, "Tile (1,0) should decay from 7 to 6.");
        Assert.AreEqual(5, tiles[2, 0].CurrentTideValue, "Tile (2,0) should decay from 6 to 5.");
        Assert.AreEqual(5, tiles[2, 1].CurrentTideValue, "Tile (2,1) should decay from 6 to 5.");

        Debug.Log("  [PASS] TestDecayTriggersAboveThreshold");
    }

    private void TestDecayDoesNotTriggerAtThreshold()
    {
        Setup();

        // Exactly 3 tiles above 5, threshold=3, no decay
        TideTile[,] tiles = new TideTile[3, 3];
        int[,] values = {
            { 8, 5, 5 },
            { 7, 5, 5 },
            { 6, 5, 5 }
        };

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                tiles[r, c] = CreateTile(c, r, false);
                tiles[r, c].currentTideValue = values[r, c];
            }
        }

        SetSealedTiles(new bool[3, 3]);

        FieldInfo thresholdField = typeof(TideManager).GetField("instabilityThreshold", BindingFlags.Instance | BindingFlags.NonPublic);
        thresholdField.SetValue(manager, 3);

        FieldInfo tilesField = typeof(TideManager).GetField("activeTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        tilesField.SetValue(manager, tiles);

        MethodInfo decayMethod = typeof(TideManager).GetMethod("ApplyInstabilityDecay", BindingFlags.Instance | BindingFlags.NonPublic);
        decayMethod.Invoke(manager, null);

        Assert.AreEqual(8, tiles[0, 0].CurrentTideValue, "Tile (0,0) should remain 8.");
        Assert.AreEqual(7, tiles[1, 0].CurrentTideValue, "Tile (1,0) should remain 7.");
        Assert.AreEqual(6, tiles[2, 0].CurrentTideValue, "Tile (2,0) should remain 6.");

        Debug.Log("  [PASS] TestDecayDoesNotTriggerAtThreshold");
    }

    private void TestDecayClampsToMinimum5()
    {
        Setup();

        // 6 tiles above 5, threshold=3, decay = 6-3 = 3
        // A tile at 6 would go to 3 but must clamp to 5
        TideTile[,] tiles = new TideTile[3, 3];
        int[,] values = {
            { 6, 6, 6 },
            { 6, 5, 5 },
            { 6, 5, 5 }
        };

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                tiles[r, c] = CreateTile(c, r, false);
                tiles[r, c].currentTideValue = values[r, c];
            }
        }

        SetSealedTiles(new bool[3, 3]);

        FieldInfo thresholdField = typeof(TideManager).GetField("instabilityThreshold", BindingFlags.Instance | BindingFlags.NonPublic);
        thresholdField.SetValue(manager, 3);

        FieldInfo tilesField = typeof(TideManager).GetField("activeTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        tilesField.SetValue(manager, tiles);

        MethodInfo decayMethod = typeof(TideManager).GetMethod("ApplyInstabilityDecay", BindingFlags.Instance | BindingFlags.NonPublic);
        decayMethod.Invoke(manager, null);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Assert.IsTrue(tiles[r, c].CurrentTideValue >= 5,
                    $"Tile ({r},{c}) should not drop below 5. Got {tiles[r, c].CurrentTideValue}.");
            }
        }

        Debug.Log("  [PASS] TestDecayClampsToMinimum5");
    }

    private void TestElementAdvantageAllPairs()
    {
        // Fire beats Earth, Air
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Earth));
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Air));
        // Water beats Fire, Space
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Space));
        // Earth beats Water, Space
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Water));
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Space));
        // Air beats Earth, Water
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Earth));
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Water));
        // Space beats Fire, Air
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Strong, ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Air));

        // Reverse pairs should be Weak
        Assert.AreEqual(MatchupResult.Weak, ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Weak, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Water));
        Assert.AreEqual(MatchupResult.Weak, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Earth));
        Assert.AreEqual(MatchupResult.Weak, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Air));
        Assert.AreEqual(MatchupResult.Weak, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Space));

        Debug.Log("  [PASS] TestElementAdvantageAllPairs");
    }

    private void TestElementNeutralSameElement()
    {
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Water));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Earth));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Air));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Space));

        Debug.Log("  [PASS] TestElementNeutralSameElement");
    }

    private void TestElementNeutralNone()
    {
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.None, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.None));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.None, CombatUnit.Element.None));

        Debug.Log("  [PASS] TestElementNeutralNone");
    }

    private void TestDamageMultiplierStrong()
    {
        float multiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Fire, CombatUnit.Element.Earth);
        Assert.AreEqual(1.5f, multiplier, 0.01f, "Strong matchup should give 1.5x multiplier.");

        int baseDamage = 10;
        int modified = Mathf.RoundToInt(baseDamage * multiplier);
        Assert.AreEqual(15, modified, "10 damage at 1.5x should be 15.");

        Debug.Log("  [PASS] TestDamageMultiplierStrong");
    }

    private void TestDamageMultiplierWeak()
    {
        float multiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Earth, CombatUnit.Element.Fire);
        Assert.AreEqual(0.67f, multiplier, 0.01f, "Weak matchup should give 0.67x multiplier.");

        int baseDamage = 10;
        int modified = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
        Assert.AreEqual(7, modified, "10 damage at 0.67x should be 7.");

        Debug.Log("  [PASS] TestDamageMultiplierWeak");
    }
}
