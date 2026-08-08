using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TideMovementTest
{
    private TideManager manager;
    private GameObject managerObject;
    private GameObject tilesRoot;

    [SetUp]
    public void SetUp()
    {
        Cleanup();
        managerObject = new GameObject("TideManager_Test");
        manager = managerObject.AddComponent<TideManager>();
        tilesRoot = new GameObject("TestTiles");
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (tilesRoot != null) Object.DestroyImmediate(tilesRoot);
        if (managerObject != null) Object.DestroyImmediate(managerObject);
        if (IslandRestorationTracker.Instance != null)
        {
            Object.DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }
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
        trackerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.AreSame(tracker, IslandRestorationTracker.Instance,
            "Tracker singleton should reference the isolated test tracker instance.");
        return tracker;
    }

    [Test]
    public void AdjacentCardinalOpen()
    {

        // 0 sealed tiles, maxCarrySteps=2
        // Source: center(1,1), Dest: north(1,0)
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(1, 0, false);

        Assert.IsTrue(CanReach(source, dest), "Adjacent cardinal tile should be reachable.");

    }

    [Test]
    public void DiagonalOpen()
    {

        // 0 sealed tiles, maxCarrySteps=2
        // Source: center(1,1), Dest: (0,0) diagonal
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(0, 0, false);

        Assert.IsTrue(CanReach(source, dest), "Diagonal tile should be reachable when no sealed tiles block the path.");

    }

    [Test]
    public void BlockedPathThruSealed()
    {

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

    }

    [Test]
    public void DiagonalWithAdjacentSealed()
    {

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

    }

    [Test]
    public void SealedAsDestination()
    {

        // Default sealedTiles: center(1,1) is sealed
        // Source: (0,0), Dest: center(1,1) sealed
        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(1, 1, true);

        Assert.IsFalse(CanReach(source, dest), "Sealed tile should not be reachable as a destination.");

    }

    [Test]
    public void MaxCarryStepsLimitsRange()
    {

        // 0 sealed tiles, but maxCarrySteps overridden via reflection to 1
        // Source: (0,0), Dest: (2,2) - minimum 2 steps away
        SetSealedTiles(new bool[3, 3]);

        FieldInfo stepsField = typeof(TideManager).GetField("maxCarrySteps", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(stepsField, "maxCarrySteps field should exist on TideManager.");
        stepsField.SetValue(manager, 1);

        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(2, 2, false);

        Assert.IsFalse(CanReach(source, dest), "Tile beyond maxCarrySteps should not be reachable.");

    }

    [Test]
    public void DiagonalEdgeCornerToCorner()
    {

        // 0 sealed tiles, maxCarrySteps=2
        // Source: (0,0), Dest: (2,2) - diagonal corner to corner, 2 steps
        SetSealedTiles(new bool[3, 3]);
        TideTile source = CreateTile(0, 0, false);
        TideTile dest = CreateTile(2, 2, false);

        Assert.IsTrue(CanReach(source, dest), "Corner-to-corner diagonal should be reachable within 2 steps.");

    }

    [Test]
    public void WinConditionAllEqualToTargetMet()
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

    }

    [Test]
    public void WinConditionAllEqualToTargetNotMet()
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

    }

    [Test]
    public void WinConditionPercentageMet()
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

    }

    [Test]
    public void WinConditionPercentageNotMet()
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

    }

    [Test]
    public void WinConditionSkipsSealedTile()
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

    }

    [Test]
    public void CompletionIgnoresPermanentAndLockedSealedTiles()
    {
        IslandRestorationTracker tracker = CreateIsolatedTracker("TideManager_LockedTileTracker_Default_Ign");
        GameObject trackerObject = tracker.gameObject;

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

        }
        finally
        {
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(trackerObject);
        }
    }

    [Test]
    public void LockedTileCountsAfterEncounterCleared()
    {
        IslandRestorationTracker tracker = CreateIsolatedTracker("TideManager_LockedTileTracker_C");
        GameObject trackerObject = tracker.gameObject;
        tracker.RecordEncounterCompletion("island_lock", "guard_1", EncounterType.Combat, 0.2f);

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

        }
        finally
        {
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(trackerObject);
        }
    }

    [Test]
    public void PuzzleDataInitializePuzzle()
    {

        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        data.gridRows = 3;
        data.gridCols = 3;
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
        Assert.AreEqual(0, values[0, 2], "Sealed tile (0,2) should have a zero tide value.");

        bool[,] sealedArr = (bool[,])GetPrivateFieldValue("sealedTiles");
        Assert.IsTrue(sealedArr[0, 2], "Tile at row 0 col 2 should be sealed.");
        Assert.IsFalse(sealedArr[1, 1], "Tile at row 1 col 1 should not be sealed.");

        PuzzleWinCondition cond = (PuzzleWinCondition)GetPrivateFieldValue("winCondition");
        Assert.AreEqual(WinConditionType.PercentageAtTarget, cond.type, "Win condition type should be PercentageAtTarget.");
        Assert.AreEqual(0.6f, cond.requiredPercent, "Required percent should be 0.6.");

        Object.DestroyImmediate(data);
    }

    [Test]
    public void InvalidPuzzleDataKeepsCurrentPuzzle()
    {
        int[,] originalLayout =
        {
            { 8, 5 },
            { 3, 6 }
        };
        manager.InitializePuzzle(originalLayout, new Vector2Int(-1, -1));

        PuzzleData invalidData = ScriptableObject.CreateInstance<PuzzleData>();
        invalidData.gridRows = -1;
        invalidData.gridCols = 2;
        invalidData.tileValues = new int[0];

        try
        {
            Assert.DoesNotThrow(() => manager.InitializePuzzle(invalidData),
                "PuzzleData with invalid dimensions should not throw during puzzle setup.");

            int[,] currentValues = (int[,])GetPrivateFieldValue("puzzleValues");
            Assert.AreEqual(2, currentValues.GetLength(0), "Invalid data should not replace the current row count.");
            Assert.AreEqual(2, currentValues.GetLength(1), "Invalid data should not replace the current column count.");
            Assert.AreEqual(8, currentValues[0, 0], "Invalid data should not replace current tile values.");
            Assert.AreEqual(6, currentValues[1, 1], "Invalid data should not replace current tile values.");
        }
        finally
        {
            Object.DestroyImmediate(invalidData);
        }
    }

    [Test]
    public void ShortPuzzleDataKeepsCurrentPuzzle()
    {
        int[,] originalLayout =
        {
            { 8, 5 },
            { 3, 6 }
        };
        manager.InitializePuzzle(originalLayout, new Vector2Int(-1, -1));

        PuzzleData invalidData = ScriptableObject.CreateInstance<PuzzleData>();
        invalidData.gridRows = 3;
        invalidData.gridCols = 3;
        invalidData.tileValues = new int[8];

        try
        {
            manager.InitializePuzzle(invalidData);

            int[,] currentValues = (int[,])GetPrivateFieldValue("puzzleValues");
            Assert.AreEqual(2, currentValues.GetLength(0), "Short data should not replace the current row count.");
            Assert.AreEqual(2, currentValues.GetLength(1), "Short data should not replace the current column count.");
            Assert.AreEqual(8, currentValues[0, 0], "Short data should not replace current tile values.");
            Assert.AreEqual(6, currentValues[1, 1], "Short data should not replace current tile values.");
        }
        finally
        {
            Object.DestroyImmediate(invalidData);
        }
    }

    [Test]
    public void OversizedPuzzleDimensionsAreInvalid()
    {
        PuzzleData invalidData = ScriptableObject.CreateInstance<PuzzleData>();
        invalidData.gridRows = int.MaxValue;
        invalidData.gridCols = 2;
        invalidData.tileValues = new int[1];

        try
        {
            Assert.IsFalse(invalidData.IsValid(),
                "Puzzle dimensions whose tile count exceeds array limits must be invalid.");
            Assert.DoesNotThrow(() => manager.InitializePuzzle(invalidData),
                "Oversized puzzle dimensions must be rejected before array allocation.");
        }
        finally
        {
            Object.DestroyImmediate(invalidData);
        }
    }

    [Test]
    public void OversizedPuzzleDimensionsReturnEmptyGrids()
    {
        PuzzleData invalidData = ScriptableObject.CreateInstance<PuzzleData>();
        invalidData.gridRows = int.MaxValue;
        invalidData.gridCols = 2;
        invalidData.tileValues = new int[1];

        try
        {
            int[,] grid = null;
            bool[,] sealedMap = null;
            Assert.DoesNotThrow(() => grid = invalidData.GetGrid(),
                "GetGrid must reject oversized dimensions before allocation.");
            Assert.DoesNotThrow(() => sealedMap = invalidData.GetSealedMap(),
                "GetSealedMap must reject oversized dimensions before allocation.");
            Assert.AreEqual(0, grid.GetLength(0), "Invalid grids should have no rows.");
            Assert.AreEqual(0, grid.GetLength(1), "Invalid grids should have no columns.");
            Assert.AreEqual(0, sealedMap.GetLength(0), "Invalid sealed maps should have no rows.");
            Assert.AreEqual(0, sealedMap.GetLength(1), "Invalid sealed maps should have no columns.");
        }
        finally
        {
            Object.DestroyImmediate(invalidData);
        }
    }

    [Test]
    public void LegacyNineValuePuzzleDataInfersThreeByThreeDimensions()
    {
        PuzzleData legacyData = ScriptableObject.CreateInstance<PuzzleData>();
        legacyData.tileValues = new int[] { 8, 5, 3, 6, 5, 4, 5, 5, 7 };
        legacyData.sealedPosition = new Vector2Int(2, 0);

        try
        {
            manager.InitializePuzzle(legacyData);

            int[,] currentValues = (int[,])GetPrivateFieldValue("puzzleValues");
            Assert.AreEqual(3, currentValues.GetLength(0),
                "Legacy nine-value assets should resolve to three rows.");
            Assert.AreEqual(3, currentValues.GetLength(1),
                "Legacy nine-value assets should resolve to three columns.");
            Assert.AreEqual(8, currentValues[0, 0],
                "Legacy puzzle values should not be replaced by fallback values.");
            Assert.AreEqual(7, currentValues[2, 2],
                "Legacy puzzle values should preserve row-major layout.");

            bool[,] sealedMap = (bool[,])GetPrivateFieldValue("sealedTiles");
            Assert.IsTrue(sealedMap[0, 2],
                "Legacy sealed positions should use the resolved three-by-three bounds.");
        }
        finally
        {
            Object.DestroyImmediate(legacyData);
        }
    }

    [Test]
    public void DecayTriggersAboveThreshold()
    {

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

    }

    [Test]
    public void DecayDoesNotTriggerAtThreshold()
    {

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

    }

    [Test]
    public void DecayClampsToMinimum5()
    {

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

    }

    [Test]
    public void ElementAdvantageAllPairs()
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

    }

    [Test]
    public void ElementNeutralSameElement()
    {
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Water, CombatUnit.Element.Water));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Earth, CombatUnit.Element.Earth));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Air, CombatUnit.Element.Air));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Space, CombatUnit.Element.Space));

    }

    [Test]
    public void ElementNeutralNone()
    {
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.None, CombatUnit.Element.Fire));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.Fire, CombatUnit.Element.None));
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult(CombatUnit.Element.None, CombatUnit.Element.None));

    }

    [Test]
    public void DamageMultiplierStrong()
    {
        float multiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Fire, CombatUnit.Element.Earth);
        Assert.AreEqual(1.5f, multiplier, 0.01f, "Strong matchup should give 1.5x multiplier.");

        int baseDamage = 10;
        int modified = Mathf.RoundToInt(baseDamage * multiplier);
        Assert.AreEqual(15, modified, "10 damage at 1.5x should be 15.");

    }

    [Test]
    public void DamageMultiplierWeak()
    {
        float multiplier = ElementMatchup.GetDamageMultiplier(CombatUnit.Element.Earth, CombatUnit.Element.Fire);
        Assert.AreEqual(0.67f, multiplier, 0.01f, "Weak matchup should give 0.67x multiplier.");

        int baseDamage = 10;
        int modified = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
        Assert.AreEqual(7, modified, "10 damage at 0.67x should be 7.");

    }
}
