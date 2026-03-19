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
        // Diagonal should still work (no corner-cutting restriction)
        bool[,] sealedArr = new bool[3, 3];
        sealedArr[0, 1] = true;
        sealedArr[1, 0] = true;
        SetSealedTiles(sealedArr);

        TideTile source = CreateTile(1, 1, false);
        TideTile dest = CreateTile(0, 0, false);

        Assert.IsTrue(CanReach(source, dest), "Diagonal movement should work even when adjacent cardinal tiles are sealed.");

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
}
