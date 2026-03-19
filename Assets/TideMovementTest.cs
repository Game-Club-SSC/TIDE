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
            TestPuzzleDataInitializePuzzle();

            Debug.Log("=== Tide movement tests passed ===");
        }
        finally
        {
            Cleanup();
        }
    }

    private void Setup()
    {
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
        FieldInfo field = typeof(TideManager).GetField("sealedTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "sealedTiles field should exist on TideManager.");
        field.SetValue(manager, sealedArr);
    }

    private bool CanReach(TideTile source, TideTile destination)
    {
        MethodInfo method = typeof(TideManager).GetMethod("CanReachWithinCarrySteps", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "CanReachWithinCarrySteps should exist on TideManager.");
        return (bool)method.Invoke(manager, new object[] { source, destination });
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

        FieldInfo valuesField = typeof(TideManager).GetField("puzzleValues", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(valuesField, "puzzleValues field should exist.");
        int[,] values = (int[,])valuesField.GetValue(manager);
        Assert.AreEqual(8, values[0, 0], "Tile (0,0) should be 8.");
        Assert.AreEqual(5, values[0, 1], "Tile (0,1) should be 5.");
        Assert.AreEqual(3, values[0, 2], "Tile (0,2) should be 3.");

        FieldInfo sealedField = typeof(TideManager).GetField("sealedTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(sealedField, "sealedTiles field should exist.");
        bool[,] sealedArr = (bool[,])sealedField.GetValue(manager);
        Assert.IsTrue(sealedArr[0, 2], "Tile at row 0 col 2 should be sealed.");
        Assert.IsFalse(sealedArr[1, 1], "Tile at row 1 col 1 should not be sealed.");

        FieldInfo condField = typeof(TideManager).GetField("winCondition", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(condField, "winCondition field should exist.");
        PuzzleWinCondition cond = (PuzzleWinCondition)condField.GetValue(manager);
        Assert.AreEqual(WinConditionType.PercentageAtTarget, cond.type, "Win condition type should be PercentageAtTarget.");
        Assert.AreEqual(0.6f, cond.requiredPercent, "Required percent should be 0.6.");

        Object.DestroyImmediate(data);
        Debug.Log("  [PASS] TestPuzzleDataInitializePuzzle");
    }
}
