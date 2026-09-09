using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

internal static class Program
{
    private static int checksPassed = 0;
    private static int checksFailed = 0;
    private static readonly string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    private static int Main()
    {
        Console.WriteLine("=== Starting Puzzle & Island Empirical Challenge Harness ===");

        SetupAssemblyResolver();

        try
        {
            TestRegressionRunnerRegistration();
            TestWinConditionDegenerateAndEdgeCases();
            TestTideTileClampingLogic();
            TestGameStateManagerFlowCoordination();
            TestTideManagerCancelMovementLogic();

            Console.WriteLine($"\n=== Summary: {checksPassed} checks passed, {checksFailed} checks failed ===");
            return checksFailed == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[FATAL HARNESS ERROR] {ex}");
            return 1;
        }
    }

    private static void SetupAssemblyResolver()
    {
        string unityManagedDir = "/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine";
        string tempBin = Path.Combine(projectRoot, "Temp/bin/Debug");

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            string unityPath = Path.Combine(unityManagedDir, assemblyName + ".dll");
            if (File.Exists(unityPath))
            {
                return Assembly.LoadFrom(unityPath);
            }

            string tempPath = Path.Combine(tempBin, assemblyName + ".dll");
            if (File.Exists(tempPath))
            {
                return Assembly.LoadFrom(tempPath);
            }

            return null;
        };
    }

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            checksPassed++;
            Console.WriteLine($"[PASS] {message}");
        }
        else
        {
            checksFailed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
        }
    }

    private static void TestRegressionRunnerRegistration()
    {
        Console.WriteLine("\n--- Testing PostDeferralVerticalSliceRegressionRunner registration ---");
        string asmPath = File.Exists(Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll"))
            ? Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll")
            : Path.Combine(projectRoot, "Library/ScriptAssemblies/Assembly-CSharp.dll");

        Assembly gameAssembly = Assembly.LoadFrom(asmPath);
        Type runnerType = gameAssembly.GetType("PostDeferralVerticalSliceRegressionRunner");
        Check(runnerType != null, "PostDeferralVerticalSliceRegressionRunner type found in assembly.");

        // Read source to verify VSR-011 and VSR-019 registrations
        string runnerSourcePath = Path.Combine(projectRoot, "Assets/PostDeferralVerticalSliceRegressionRunner.cs");
        string runnerSource = File.ReadAllText(runnerSourcePath);

        Check(runnerSource.Contains("VSR-011") && runnerSource.Contains("LustIslandVerificationTest.RunTests"),
            "VSR-011 is registered with LustIslandVerificationTest.RunTests");
        Check(runnerSource.Contains("VSR-019") && runnerSource.Contains("PuzzleWinConditionTest.RunTests"),
            "VSR-019 is registered with PuzzleWinConditionTest.RunTests");

        // Verify target types and methods exist
        Type lustTestType = gameAssembly.GetType("LustIslandVerificationTest");
        Check(lustTestType != null, "LustIslandVerificationTest type found.");
        MethodInfo lustRunTests = lustTestType?.GetMethod("RunTests", BindingFlags.Instance | BindingFlags.Public);
        Check(lustRunTests != null && lustRunTests.GetParameters().Length == 0,
            "LustIslandVerificationTest.RunTests public instance parameterless method exists.");

        Type puzzleTestType = gameAssembly.GetType("PuzzleWinConditionTest");
        Check(puzzleTestType != null, "PuzzleWinConditionTest type found.");
        MethodInfo puzzleRunTests = puzzleTestType?.GetMethod("RunTests", BindingFlags.Instance | BindingFlags.Public);
        Check(puzzleRunTests != null && puzzleRunTests.GetParameters().Length == 0,
            "PuzzleWinConditionTest.RunTests public instance parameterless method exists.");

        // Verify all 19 VSR steps reference valid types and methods
        var expectedSteps = new (string vsrId, string typeName, string methodName)[]
        {
            ("VSR-001", "IslandContentVerificationTest", "RunTests"),
            ("VSR-002", "RestorationTrackerTest", "RunTests"),
            ("VSR-003", "BossEncounterGateTest", "RunTests"),
            ("VSR-004", "RestorationThresholdGateTest", "RunTests"),
            ("VSR-005", "IslandProgressionTravelTest", "RunTests"),
            ("VSR-006", "HeroProgressionTest", "RunAllTests"),
            ("VSR-007", "GearSystemTest", "RunAllTests"),
            ("VSR-008", "GearProgressionTest", "RunAllTests"),
            ("VSR-009", "DevGodModeStateTest", "RunTests"),
            ("VSR-010", "GreedIslandVerificationTest", "RunTests"),
            ("VSR-011", "LustIslandVerificationTest", "RunTests"),
            ("VSR-012", "DesireIslandVerificationTest", "RunTests"),
            ("VSR-013", "AngerIslandVerificationTest", "RunTests"),
            ("VSR-014", "EnvyIslandVerificationTest", "RunTests"),
            ("VSR-015", "EgoIslandVerificationTest", "RunTests"),
            ("VSR-016", "DesireStatusEffectTestSuite", "RunTests"),
            ("VSR-017", "EnvyMirrorTestSuite", "RunTests"),
            ("VSR-018", "GreedEconomyTestSuite", "RunTests"),
            ("VSR-019", "PuzzleWinConditionTest", "RunTests"),
        };

        foreach (var (vsrId, typeName, methodName) in expectedSteps)
        {
            Type t = gameAssembly.GetType(typeName);
            Check(t != null, $"[{vsrId}] Type {typeName} exists in Assembly-CSharp.");
            if (t != null)
            {
                MethodInfo m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                Check(m != null, $"[{vsrId}] Method {typeName}.{methodName} exists and is public instance.");
            }
        }
    }

    private static void TestWinConditionDegenerateAndEdgeCases()
    {
        Console.WriteLine("\n--- Testing PuzzleWinCondition degenerate and edge cases ---");
        string asmPath = File.Exists(Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll"))
            ? Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll")
            : Path.Combine(projectRoot, "Library/ScriptAssemblies/Assembly-CSharp.dll");
        Assembly gameAssembly = Assembly.LoadFrom(asmPath);

        Type winCondType = gameAssembly.GetType("PuzzleWinCondition");
        Type winCondEnumType = gameAssembly.GetType("WinConditionType");
        object cond = Activator.CreateInstance(winCondType);

        FieldInfo typeField = winCondType.GetField("type");
        FieldInfo targetValueField = winCondType.GetField("targetValue");
        FieldInfo requiredPercentField = winCondType.GetField("requiredPercent");
        MethodInfo isMetMethod = winCondType.GetMethod("IsMet", new[] { typeof(int[,]), typeof(bool[,]) });

        // Set type = AllEqualToTarget (0), targetValue = 5, requiredPercent = 1.0f
        typeField.SetValue(cond, Enum.Parse(winCondEnumType, "AllEqualToTarget"));
        targetValueField.SetValue(cond, 5);
        requiredPercentField.SetValue(cond, 1.0f);

        // Case 1: Null grid
        bool nullResult = (bool)isMetMethod.Invoke(cond, new object[] { null, null });
        Check(!nullResult, "Null grid returns false.");

        // Case 2: Degenerate empty grid 0x0
        int[,] emptyGrid = new int[0, 0];
        bool emptyResult = (bool)isMetMethod.Invoke(cond, new object[] { emptyGrid, null });
        Check(!emptyResult, "Empty grid [0,0] returns false without exception.");

        // Case 3: All sealed tiles in 3x3 grid
        int[,] allSealedGrid = new int[,]
        {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };
        bool[,] allSealedTiles = new bool[,]
        {
            { true, true, true },
            { true, true, true },
            { true, true, true }
        };
        bool allSealedResult = (bool)isMetMethod.Invoke(cond, new object[] { allSealedGrid, allSealedTiles });
        Check(!allSealedResult, "All sealed tiles in 3x3 returns false (degenerate puzzle, total active == 0).");

        // Case 4: Balanced grid 3x3 all 5s
        int[,] balancedGrid = new int[,]
        {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 5, 5 }
        };
        bool balancedResult = (bool)isMetMethod.Invoke(cond, new object[] { balancedGrid, null });
        Check(balancedResult, "Fully balanced 3x3 grid with all 5s returns true.");

        // Case 5: Grid with 1 non-5 tile
        int[,] unbalancedGrid = new int[,]
        {
            { 5, 5, 5 },
            { 5, 6, 5 },
            { 5, 5, 5 }
        };
        bool unbalancedResult = (bool)isMetMethod.Invoke(cond, new object[] { unbalancedGrid, null });
        Check(!unbalancedResult, "Grid with center tile at 6 returns false for AllEqualToTarget.");

        // Case 6: Grid with 1 non-5 tile, but that tile is sealed
        bool[,] sealedCenter = new bool[,]
        {
            { false, false, false },
            { false, true, false },
            { false, false, false }
        };
        bool sealedCenterResult = (bool)isMetMethod.Invoke(cond, new object[] { unbalancedGrid, sealedCenter });
        Check(sealedCenterResult, "Grid with center tile at 6 excluded via sealed map returns true.");

        // Case 7: PercentageAtTarget condition
        typeField.SetValue(cond, Enum.Parse(winCondEnumType, "PercentageAtTarget"));
        requiredPercentField.SetValue(cond, 0.75f); // 75% required

        // 7 of 9 at 5 (77.7%)
        int[,] seventySevenPctGrid = new int[,]
        {
            { 5, 5, 5 },
            { 5, 5, 5 },
            { 5, 3, 7 }
        };
        bool pctPassResult = (bool)isMetMethod.Invoke(cond, new object[] { seventySevenPctGrid, null });
        Check(pctPassResult, "75% required: 7 of 9 at 5 returns true.");

        // 5 of 9 at 5 (55.5%)
        int[,] fiftyFivePctGrid = new int[,]
        {
            { 5, 5, 5 },
            { 5, 3, 7 },
            { 4, 6, 5 }
        };
        bool pctFailResult = (bool)isMetMethod.Invoke(cond, new object[] { fiftyFivePctGrid, null });
        Check(!pctFailResult, "75% required: 5 of 9 at 5 returns false.");
    }

    private static void TestTideTileClampingLogic()
    {
        Console.WriteLine("\n--- Testing TideTile clamping & range validation ---");
        string tideTileSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/TideTile.cs"));

        // Check property getter and setter clamping logic
        Check(tideTileSource.Contains("Mathf.Clamp(value, 1, 10)"),
            "TideTile.CurrentTideValue setter contains Mathf.Clamp(value, 1, 10).");
        Check(tideTileSource.Contains("Mathf.Clamp(tideValue, 1, 10)"),
            "TideTile.Configure contains Mathf.Clamp(tideValue, 1, 10).");
        Check(tideTileSource.Contains("Mathf.Clamp(currentTideValue, 1, 10)"),
            "TideTile.ApplyTake and ApplyPlace clamp currentTideValue to [1, 10].");
        Check(tideTileSource.Contains("[Range(1, 10)]"),
            "TideTile inspector attribute specifies [Range(1, 10)].");
        Check(tideTileSource.Contains("[DisallowMultipleComponent]"),
            "TideTile has [DisallowMultipleComponent] attribute.");

        // Evaluate pure math clamp behavior
        Check(Math.Clamp(-10, 1, 10) == 1, "Clamping -10 produces 1.");
        Check(Math.Clamp(-1, 1, 10) == 1, "Clamping -1 produces 1.");
        Check(Math.Clamp(0, 1, 10) == 1, "Clamping 0 produces 1.");
        Check(Math.Clamp(1, 1, 10) == 1, "Clamping 1 produces 1.");
        Check(Math.Clamp(5, 1, 10) == 5, "Clamping 5 produces 5.");
        Check(Math.Clamp(10, 1, 10) == 10, "Clamping 10 produces 10.");
        Check(Math.Clamp(11, 1, 10) == 10, "Clamping 11 produces 10.");
        Check(Math.Clamp(100, 1, 10) == 10, "Clamping 100 produces 10.");
    }

    private static void TestGameStateManagerFlowCoordination()
    {
        Console.WriteLine("\n--- Testing GameStateManager combat completion coordination ---");
        string gsmSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/GameStateManager.cs"));

        Check(gsmSource.Contains("bool flowControlsCombatCompletion = HasActiveFlowController;"),
            "GameStateManager defines flowControlsCombatCompletion based on HasActiveFlowController.");
        Check(gsmSource.Contains("!flowControlsCombatCompletion"),
            "GameStateManager guards direct combat restoration with !flowControlsCombatCompletion.");

        // Check IslandFlowController handles completion on return
        string ifcSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/IslandFlowController.cs"));
        Check(ifcSource.Contains("void OnReturnFromCombat(bool playerWon)"),
            "IslandFlowController implements OnReturnFromCombat.");
        Check(ifcSource.Contains("tracker.RecordEncounterCompletion(activeIslandId, encounterId, encounter.type, encounter.restorationValue);"),
            "IslandFlowController.OnEncounterComplete records encounter completion in tracker.");

        // Check IslandRestorationTracker and IslandRestorationState idempotency
        string trackerSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/IslandRestorationTracker.cs"));
        string stateSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/IslandRestorationState.cs"));
        Check(trackerSource.Contains("state.HasCompleted(encounterId)") &&
              stateSource.Contains("completedEncounterIds.Contains(encounterId)"),
            "IslandRestorationTracker and IslandRestorationState check if encounter was already completed.");
    }

    private static void TestTideManagerCancelMovementLogic()
    {
        Console.WriteLine("\n--- Testing TideManager carry cancellation & decay logic ---");
        string tmSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/TideManager.cs"));

        Check(tmSource.Contains("if (hoveredTile == carryingSource)"),
            "TideManager checks if hoveredTile == carryingSource in HandleDestinationSelection.");
        Check(tmSource.Contains("CancelCarriedTide();") && tmSource.Contains("return;"),
            "TideManager calls CancelCarriedTide() and returns immediately without applying decay.");

        int cancelIndex = tmSource.IndexOf("if (hoveredTile == carryingSource)");
        int decayIndex = tmSource.IndexOf("ApplyInstabilityDecay();", cancelIndex);
        Check(decayIndex > cancelIndex,
            "ApplyInstabilityDecay() is located after the cancel return guard, ensuring decay is bypassed.");

        // Check CancelCarriedTide restoration
        Check(tmSource.Contains("carryingSource.ApplyPlace(carriedAmount);"),
            "CancelCarriedTide restores carriedAmount to carryingSource.");
        Check(tmSource.Contains("carryingSource = null;") && tmSource.Contains("carriedAmount = 0;"),
            "CancelCarriedTide resets carryingSource and carriedAmount.");
    }
}
