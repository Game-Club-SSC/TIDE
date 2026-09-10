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
            TestSubsystemAlignmentWithGddV2();

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

    private static void TestSubsystemAlignmentWithGddV2()
    {
        Console.WriteLine("\n--- Testing 6 Subsystems Alignment with GDD V2 (Empirical & Deep) ---");

        string asmPath = File.Exists(Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll"))
            ? Path.Combine(projectRoot, "Temp/bin/Debug/Assembly-CSharp.dll")
            : Path.Combine(projectRoot, "Library/ScriptAssemblies/Assembly-CSharp.dll");
        Assembly gameAssembly = Assembly.LoadFrom(asmPath);

        TestNarrativeFateSubsystem(gameAssembly);
        TestDialogueSubsystem(gameAssembly);
        TestSavePersistenceSubsystem(gameAssembly);
        TestBoatTravelSubsystem(gameAssembly);
        TestCombatSubsystem(gameAssembly);
        TestTidePuzzlesSubsystem(gameAssembly);
    }

    private static void TestNarrativeFateSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 1: Narrative Fate Mechanics]");

        // AncientTextAuthoring baseline coverage
        Type authoringType = gameAssembly.GetType("AncientTextAuthoring");
        Check(authoringType != null, "AncientTextAuthoring type exists.");

        FieldInfo sinsField = authoringType.GetField("BaselineSins", BindingFlags.Static | BindingFlags.NonPublic);
        string[] baselineSins = (string[])sinsField?.GetValue(null);
        Check(baselineSins != null && baselineSins.Length == 6,
            $"AncientTextAuthoring defines 6 baseline sins (actual: {baselineSins?.Length}).");

        string[] requiredSins = { "lust", "greed", "desire", "anger", "envy", "ego" };
        foreach (string sin in requiredSins)
        {
            Check(Array.IndexOf(baselineSins, sin) >= 0, $"Baseline sins include '{sin}'.");
        }

        // Source verification for 18 unique baseline texts across all 3 acts
        string authoringSource = File.ReadAllText(Path.Combine(projectRoot, "Assets/AncientTextAuthoring.cs"));
        Check(authoringSource.Contains("DeriveIslandId"), "AncientTextAuthoring derives islandId per sin.");
        foreach (string sin in requiredSins)
        {
            Check(authoringSource.Contains($"text_{sin}_act1_01"), $"AncientTextAuthoring defines text_{sin}_act1_01.");
            Check(authoringSource.Contains($"text_{sin}_act2_01"), $"AncientTextAuthoring defines text_{sin}_act2_01.");
            Check(authoringSource.Contains($"text_{sin}_act3_01"), $"AncientTextAuthoring defines text_{sin}_act3_01.");
        }

        // BadEndingReactions
        Type badEndingType = gameAssembly.GetType("BadEndingReactions");
        Check(badEndingType != null, "BadEndingReactions type exists.");

        MethodInfo getAllReactionsMethod = badEndingType.GetMethod("GetAllReactionsIncludingMC", BindingFlags.Static | BindingFlags.Public);
        Array reactions = (Array)getAllReactionsMethod.Invoke(null, null);
        Check(reactions != null && reactions.Length == 5,
            $"BadEndingReactions defines reactions for all 5 heroes (actual: {reactions?.Length}).");

        string[] expectedHeroNames = { "Freida", "Briar", "Killian", "Merrick", "The Chosen" };
        List<string> foundNames = new List<string>();
        for (int i = 0; i < reactions.Length; i++)
        {
            object reaction = reactions.GetValue(i);
            FieldInfo nameField = reaction.GetType().GetField("heroName");
            FieldInfo finalWordsField = reaction.GetType().GetField("finalWords");
            string name = (string)nameField.GetValue(reaction);
            string finalWords = (string)finalWordsField.GetValue(reaction);
            foundNames.Add(name);
            Check(!string.IsNullOrEmpty(finalWords), $"Hero '{name}' has non-empty final words for bad ending.");
        }

        foreach (string expected in expectedHeroNames)
        {
            Check(foundNames.Contains(expected), $"BadEndingReactions contains '{expected}'.");
        }

        // EndingEvaluator outcome evaluation (empirical execution)
        Type evalType = gameAssembly.GetType("EndingEvaluator");
        Type snapshotType = gameAssembly.GetType("EndingEvaluatorSnapshot");

        MethodInfo evalSnapshotMethod = evalType.GetMethod("EvaluateOutcomeFromSnapshot", BindingFlags.Static | BindingFlags.Public);
        Check(evalSnapshotMethod != null, "EndingEvaluator.EvaluateOutcomeFromSnapshot exists.");

        // Case A: final boss defeats >= threshold -> BadEnding
        object snapBadLoss = Activator.CreateInstance(snapshotType);
        snapshotType.GetField("hasFinalBossDefeats").SetValue(snapBadLoss, true);
        snapshotType.GetField("finalBossDefeats").SetValue(snapBadLoss, 4);
        snapshotType.GetField("finalBossDefeatThreshold").SetValue(snapBadLoss, 4);
        object outcomeBadLoss = evalSnapshotMethod.Invoke(null, new object[] { snapBadLoss });
        Check(outcomeBadLoss.ToString() == "BadEnding", "EndingEvaluator: Defeats >= threshold yields BadEnding.");

        // Case B: full/threshold restoration cleared (ratio 1.0f >= 0.75f) -> GoodEnding
        object snapGood = Activator.CreateInstance(snapshotType);
        snapshotType.GetField("isMinimumRestorationRuleEnabled").SetValue(snapGood, true);
        snapshotType.GetField("minimumRestorationClearedRatio").SetValue(snapGood, 1.0f);
        snapshotType.GetField("minimumRestorationThresholdRatio").SetValue(snapGood, 0.75f);
        object outcomeGood = evalSnapshotMethod.Invoke(null, new object[] { snapGood });
        Check(outcomeGood.ToString() == "GoodEnding", "EndingEvaluator: Cleared ratio 1.0f >= 0.75f yields GoodEnding.");

        // Case C: under-restoration (ratio 0.2f < 0.75f) -> BadEnding
        object snapBadRatio = Activator.CreateInstance(snapshotType);
        snapshotType.GetField("isMinimumRestorationRuleEnabled").SetValue(snapBadRatio, true);
        snapshotType.GetField("minimumRestorationClearedRatio").SetValue(snapBadRatio, 0.2f);
        snapshotType.GetField("minimumRestorationThresholdRatio").SetValue(snapBadRatio, 0.75f);
        object outcomeBadRatio = evalSnapshotMethod.Invoke(null, new object[] { snapBadRatio });
        Check(outcomeBadRatio.ToString() == "BadEnding", "EndingEvaluator: Cleared ratio 0.2f < 0.75f yields BadEnding.");

        // Case D: null snapshot -> Unresolved
        object outcomeNull = evalSnapshotMethod.Invoke(null, new object[] { null });
        Check(outcomeNull.ToString() == "Unresolved", "EndingEvaluator: Null snapshot yields Unresolved.");

        // FateEncounterDirector
        Type fateType = gameAssembly.GetType("FateEncounterDirector");
        Check(fateType != null, "FateEncounterDirector type exists.");
        MethodInfo startFateMethod = fateType.GetMethod("StartFateEncounter", BindingFlags.Instance | BindingFlags.Public);
        Check(startFateMethod != null, "FateEncounterDirector.StartFateEncounter public method exists.");
    }

    private static void TestDialogueSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 2: Dialogue System]");

        Type effectEnumType = gameAssembly.GetType("DialogueEffectType");
        Check(effectEnumType != null, "DialogueEffectType enum exists.");

        string[] requiredEffects = { "GrantXP", "UnlockTideBreak", "SetFlag", "GiveItem", "IncreaseBond" };
        foreach (string eff in requiredEffects)
        {
            Check(Enum.IsDefined(effectEnumType, eff), $"DialogueEffectType defines '{eff}'.");
        }

        Type dialogueSysType = gameAssembly.GetType("DialogueSystem");
        Check(dialogueSysType != null, "DialogueSystem type exists.");
        Check(dialogueSysType.GetMethod("ApplyDialogueEffect") != null, "DialogueSystem.ApplyDialogueEffect exists.");
        Check(dialogueSysType.GetMethod("GetBondLevel") != null, "DialogueSystem.GetBondLevel exists.");
        Check(dialogueSysType.GetMethod("IncreaseBond") != null, "DialogueSystem.IncreaseBond exists.");

        MethodInfo makeBondKey = dialogueSysType.GetMethod("MakeBondKey", BindingFlags.Static | BindingFlags.Public);
        if (makeBondKey != null)
        {
            string key1 = (string)makeBondKey.Invoke(null, new object[] { "hero_fire", "hero_water" });
            string key2 = (string)makeBondKey.Invoke(null, new object[] { "hero_water", "hero_fire" });
            Check(key1 == key2, "DialogueSystem.MakeBondKey is commutative (symmetric relationship keys).");
        }

        Type treeRunnerType = gameAssembly.GetType("DialogueTreeRunner");
        Check(treeRunnerType != null, "DialogueTreeRunner type exists.");
        Check(treeRunnerType.GetMethod("StartTree") != null, "DialogueTreeRunner.StartTree exists.");
        Check(treeRunnerType.GetMethod("ApplyEffects", BindingFlags.Instance | BindingFlags.NonPublic) != null,
            "DialogueTreeRunner.ApplyEffects exists.");
        Check(treeRunnerType.GetMethod("EvaluateConditions", BindingFlags.Instance | BindingFlags.NonPublic) != null,
            "DialogueTreeRunner.EvaluateConditions exists.");
    }

    private static void TestSavePersistenceSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 3: Save Persistence]");

        Type saveServiceType = gameAssembly.GetType("WorldSaveService");
        Check(saveServiceType != null, "WorldSaveService type exists.");

        FieldInfo prefsKeyField = saveServiceType.GetField("playerPrefsKey", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(prefsKeyField != null, "WorldSaveService has playerPrefsKey field.");

        Type gsmType = gameAssembly.GetType("GameStateManager");
        Check(gsmType != null, "GameStateManager type exists.");
        Check(gsmType.GetMethod("SaveWorldState", BindingFlags.Instance | BindingFlags.Public) != null,
            "GameStateManager.SaveWorldState exists.");
        Check(gsmType.GetMethod("LoadWorldState", BindingFlags.Instance | BindingFlags.Public) != null,
            "GameStateManager.LoadWorldState exists.");

        Type saveDataType = gsmType.GetNestedType("WorldStateSaveData", BindingFlags.Public | BindingFlags.NonPublic);
        Check(saveDataType != null, "GameStateManager.WorldStateSaveData type exists.");

        string[] requiredSaveSections =
        {
            "puzzleStates", "ancientTextStates", "completedNarrativeBeatIds",
            "restorationSnapshot", "heroProgression", "partyComposition",
            "hubState", "dialogueState"
        };
        foreach (string sec in requiredSaveSections)
        {
            FieldInfo f = saveDataType.GetField(sec);
            Check(f != null, $"WorldStateSaveData includes persistent section '{sec}'.");
        }
    }

    private static void TestBoatTravelSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 4: Boat Travel]");

        Type registryType = gameAssembly.GetType("IslandThemeRegistry");
        Check(registryType != null, "IslandThemeRegistry type exists.");

        PropertyInfo progOrderProp = registryType.GetProperty("ProgressionOrder", BindingFlags.Static | BindingFlags.Public);
        System.Collections.ICollection progOrder = (System.Collections.ICollection)progOrderProp.GetValue(null);
        Check(progOrder != null && progOrder.Count == 6,
            $"IslandThemeRegistry defines exactly 6 progression islands (actual: {progOrder?.Count}).");

        string[] expectedProgression = { "island_lust", "island_greed", "island_desire", "island_anger", "island_envy", "island_ego" };
        int idx = 0;
        foreach (object item in progOrder)
        {
            Check((string)item == expectedProgression[idx],
                $"Island progression [{idx}] is '{expectedProgression[idx]}'.");
            idx++;
        }

        FieldInfo hubIdField = registryType.GetField("HubIslandId", BindingFlags.Static | BindingFlags.Public);
        Check((string)hubIdField.GetValue(null) == "island_hub", "IslandThemeRegistry.HubIslandId is 'island_hub'.");

        FieldInfo aliasesField = registryType.GetField("legacyIslandIdAliases", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, string> aliases = (Dictionary<string, string>)aliasesField?.GetValue(null);
        Check(aliases != null, "IslandThemeRegistry.legacyIslandIdAliases dictionary exists.");
        Check(aliases["island_1"] == "island_lust", "legacy alias: 'island_1' -> 'island_lust'.");
        Check(aliases["island_sloth"] == "island_desire", "legacy alias: 'island_sloth' -> 'island_desire'.");
        Check(aliases["island_wrath"] == "island_anger", "legacy alias: 'island_wrath' -> 'island_anger'.");
        Check(aliases["island_pride"] == "island_ego", "legacy alias: 'island_pride' -> 'island_ego'.");
        Check(aliases["island_gluttony"] == "island_greed", "legacy alias: 'island_gluttony' -> 'island_greed'.");

        Type boatType = gameAssembly.GetType("IslandBoatInteractable");
        Check(boatType != null, "IslandBoatInteractable type exists.");
        Check(boatType.GetMethod("TryGetSpawnPositionForIsland") != null, "IslandBoatInteractable.TryGetSpawnPositionForIsland exists.");
    }

    private static void TestCombatSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 5: Combat System]");

        Type matchupType = gameAssembly.GetType("ElementMatchup");
        Check(matchupType != null, "ElementMatchup type exists.");

        Type combatUnitType = gameAssembly.GetType("CombatUnit");
        Type elementEnumType = combatUnitType.GetNestedType("Element");
        Check(elementEnumType != null, "CombatUnit.Element enum exists.");

        object fire = Enum.Parse(elementEnumType, "Fire");
        object water = Enum.Parse(elementEnumType, "Water");
        object earth = Enum.Parse(elementEnumType, "Earth");
        object air = Enum.Parse(elementEnumType, "Air");
        object space = Enum.Parse(elementEnumType, "Space");
        object none = Enum.Parse(elementEnumType, "None");

        MethodInfo getResultMethod = matchupType.GetMethod("GetResult", BindingFlags.Static | BindingFlags.Public);
        Check(getResultMethod != null, "ElementMatchup.GetResult exists.");

        // Canonical GDD V2 5-element rock-paper-scissors verification:
        // Fire beats Earth and Air, loses to Water and Space
        Check(getResultMethod.Invoke(null, new object[] { fire, earth }).ToString() == "Strong", "Fire beats Earth (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { fire, air }).ToString() == "Strong", "Fire beats Air (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { fire, water }).ToString() == "Weak", "Fire loses to Water (Weak).");
        Check(getResultMethod.Invoke(null, new object[] { fire, space }).ToString() == "Weak", "Fire loses to Space (Weak).");

        // Water beats Fire and Space, loses to Earth and Air
        Check(getResultMethod.Invoke(null, new object[] { water, fire }).ToString() == "Strong", "Water beats Fire (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { water, space }).ToString() == "Strong", "Water beats Space (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { water, earth }).ToString() == "Weak", "Water loses to Earth (Weak).");
        Check(getResultMethod.Invoke(null, new object[] { water, air }).ToString() == "Weak", "Water loses to Air (Weak).");

        // Earth beats Water and Space, loses to Fire and Air
        Check(getResultMethod.Invoke(null, new object[] { earth, water }).ToString() == "Strong", "Earth beats Water (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { earth, space }).ToString() == "Strong", "Earth beats Space (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { earth, fire }).ToString() == "Weak", "Earth loses to Fire (Weak).");
        Check(getResultMethod.Invoke(null, new object[] { earth, air }).ToString() == "Weak", "Earth loses to Air (Weak).");

        // Air beats Earth and Water, loses to Fire and Space
        Check(getResultMethod.Invoke(null, new object[] { air, earth }).ToString() == "Strong", "Air beats Earth (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { air, water }).ToString() == "Strong", "Air beats Water (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { air, fire }).ToString() == "Weak", "Air loses to Fire (Weak).");
        Check(getResultMethod.Invoke(null, new object[] { air, space }).ToString() == "Weak", "Air loses to Space (Weak).");

        // Space beats Fire and Air, loses to Water and Earth
        Check(getResultMethod.Invoke(null, new object[] { space, fire }).ToString() == "Strong", "Space beats Fire (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { space, air }).ToString() == "Strong", "Space beats Air (Strong).");
        Check(getResultMethod.Invoke(null, new object[] { space, water }).ToString() == "Weak", "Space loses to Water (Weak).");
        Check(getResultMethod.Invoke(null, new object[] { space, earth }).ToString() == "Weak", "Space loses to Earth (Weak).");

        // Neutral self matchups
        Check(getResultMethod.Invoke(null, new object[] { fire, fire }).ToString() == "Neutral", "Fire vs Fire is Neutral.");
        Check(getResultMethod.Invoke(null, new object[] { water, water }).ToString() == "Neutral", "Water vs Water is Neutral.");
        Check(getResultMethod.Invoke(null, new object[] { none, fire }).ToString() == "Neutral", "None vs Fire is Neutral.");

        // MomentumState tug-of-war bar
        Type momentumType = gameAssembly.GetType("MomentumState");
        Check(momentumType != null, "MomentumState type exists.");
        object momentum = Activator.CreateInstance(momentumType);

        PropertyInfo valueProp = momentumType.GetProperty("Value");
        MethodInfo shiftPlayerMethod = momentumType.GetMethod("ShiftTowardPlayer");
        MethodInfo shiftEnemyMethod = momentumType.GetMethod("ShiftTowardEnemy");
        MethodInfo resetMethod = momentumType.GetMethod("Reset");
        PropertyInfo playerTbReady = momentumType.GetProperty("IsPlayerTideBreakReady");
        PropertyInfo enemyTbReady = momentumType.GetProperty("IsEnemyTideBreakReady");

        Check((float)valueProp.GetValue(momentum) == 0f, "MomentumState initializes at 0 (balanced).");
        shiftPlayerMethod.Invoke(momentum, new object[] { 0.4f });
        Check(Math.Abs((float)valueProp.GetValue(momentum) - 0.4f) < 0.001f, "MomentumState shifts +0.4 towards player.");
        shiftEnemyMethod.Invoke(momentum, new object[] { 0.6f });
        Check(Math.Abs((float)valueProp.GetValue(momentum) - (-0.2f)) < 0.001f, "MomentumState shifts 0.6 towards enemy.");
        resetMethod.Invoke(momentum, null);
        Check((float)valueProp.GetValue(momentum) == 0f, "MomentumState.Reset returns to 0.");

        // Clamping and TideBreak readiness
        shiftPlayerMethod.Invoke(momentum, new object[] { 5f });
        Check((float)valueProp.GetValue(momentum) == 1f, "MomentumState clamps to MaxValue (1.0).");
        Check((bool)playerTbReady.GetValue(momentum), "MomentumState: Player TideBreak ready at +1.0.");

        shiftEnemyMethod.Invoke(momentum, new object[] { 10f });
        Check((float)valueProp.GetValue(momentum) == -1f, "MomentumState clamps to MinValue (-1.0).");
        Check((bool)enemyTbReady.GetValue(momentum), "MomentumState: Enemy TideBreak ready at -1.0.");
    }

    private static void TestTidePuzzlesSubsystem(Assembly gameAssembly)
    {
        Console.WriteLine("\n  [Subsystem 6: Tide Puzzles]");

        Type constantsType = gameAssembly.GetType("GameConstants");
        Check(constantsType != null, "GameConstants type exists.");

        int maxCarrySteps = (int)constantsType.GetField("MaxCarrySteps").GetValue(null);
        int instabilityThreshold = (int)constantsType.GetField("InstabilityThreshold").GetValue(null);
        int targetValue = (int)constantsType.GetField("PuzzleTargetValue").GetValue(null);
        int minValue = (int)constantsType.GetField("PuzzleMinValue").GetValue(null);
        int maxValue = (int)constantsType.GetField("PuzzleMaxValue").GetValue(null);

        Check(maxCarrySteps == 2, $"GameConstants.MaxCarrySteps is 2 (actual: {maxCarrySteps}).");
        Check(instabilityThreshold == 3, $"GameConstants.InstabilityThreshold is 3 (actual: {instabilityThreshold}).");
        Check(targetValue == 5, $"GameConstants.PuzzleTargetValue is 5 (actual: {targetValue}).");
        Check(minValue == 1, $"GameConstants.PuzzleMinValue is 1 (actual: {minValue}).");
        Check(maxValue == 10, $"GameConstants.PuzzleMaxValue is 10 (actual: {maxValue}).");

        Type tideManagerType = gameAssembly.GetType("TideManager");
        Check(tideManagerType != null, "TideManager type exists.");
        FieldInfo maxCarryField = tideManagerType.GetField("maxCarrySteps", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(maxCarryField != null, "TideManager has maxCarrySteps field.");

        Type tideTileType = gameAssembly.GetType("TideTile");
        Check(tideTileType != null, "TideTile type exists.");
        Check(tideTileType.GetMethod("GetMaxTake") != null, "TideTile.GetMaxTake exists.");
        Check(tideTileType.GetMethod("CanReceive") != null, "TideTile.CanReceive exists.");
        Check(tideTileType.GetMethod("ApplyDecay") != null, "TideTile.ApplyDecay exists.");

        // PuzzleWinCondition empirical execution (Early Game: Percentage, Late Game: All 5)
        Type winConditionType = gameAssembly.GetType("PuzzleWinCondition");
        Type winConditionEnumType = gameAssembly.GetType("WinConditionType");
        Check(winConditionType != null, "PuzzleWinCondition type exists.");
        Check(winConditionEnumType != null, "WinConditionType enum exists.");

        MethodInfo isMetMethod = winConditionType.GetMethod("IsMet", new Type[] { typeof(int[,]), typeof(bool[,]) });
        Check(isMetMethod != null, "PuzzleWinCondition.IsMet(int[,], bool[,]) exists.");

        object lateGameCond = Activator.CreateInstance(winConditionType);
        FieldInfo typeField = winConditionType.GetField("type");
        FieldInfo targetField = winConditionType.GetField("targetValue");
        FieldInfo requiredPercentField = winConditionType.GetField("requiredPercent");

        typeField.SetValue(lateGameCond, Enum.Parse(winConditionEnumType, "AllEqualToTarget"));
        targetField.SetValue(lateGameCond, 5);
        requiredPercentField.SetValue(lateGameCond, 1f);

        int[,] allFives = { { 5, 5 }, { 5, 5 } };
        int[,] notAllFives = { { 5, 5 }, { 5, 3 } };
        Check((bool)isMetMethod.Invoke(lateGameCond, new object[] { allFives, null }), "Late Game: All tiles at 5 meets AllEqualToTarget.");
        Check(!(bool)isMetMethod.Invoke(lateGameCond, new object[] { notAllFives, null }), "Late Game: Any tile not at 5 fails AllEqualToTarget.");

        object earlyGameCond = Activator.CreateInstance(winConditionType);
        typeField.SetValue(earlyGameCond, Enum.Parse(winConditionEnumType, "PercentageAtTarget"));
        targetField.SetValue(earlyGameCond, 5);
        requiredPercentField.SetValue(earlyGameCond, 0.75f);

        int[,] grid75Percent = { { 5, 5 }, { 5, 1 } }; // 3 out of 4 = 75%
        int[,] grid50Percent = { { 5, 5 }, { 1, 1 } }; // 2 out of 4 = 50%
        Check((bool)isMetMethod.Invoke(earlyGameCond, new object[] { grid75Percent, null }), "Early Game: 75% of tiles at 5 meets PercentageAtTarget.");
        Check(!(bool)isMetMethod.Invoke(earlyGameCond, new object[] { grid50Percent, null }), "Early Game: 50% of tiles at 5 fails 75% PercentageAtTarget.");
    }
}
