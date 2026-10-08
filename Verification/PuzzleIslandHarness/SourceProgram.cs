using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

internal static class SourceProgram
{
    private static int checks;
    private static int failures;
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    private static int Main(string[] args)
    {
        Console.WriteLine("Linked-source puzzle harness. Unity rendering, coroutines and scene loading are stubbed.");
        try
        {
            TileContracts();
            PuzzleContracts();
            ManagerContracts();
            MovementAndDecayContracts();
            GuardAndSceneContracts();
            if (!args.Contains("--logic-only")) AuthoredAssetContracts();
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine("ERROR " + ex);
        }
        Console.WriteLine($"RESULT {checks - failures}/{checks} checks passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(bool ok, string label)
    {
        checks++;
        if (!ok)
        {
            failures++;
            Console.WriteLine("FAIL " + label);
        }
    }

    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, PrivateInstance).SetValue(obj, value);
    private static object Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, PrivateInstance).Invoke(obj, args);
    private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, PrivateInstance).GetValue(obj);

    private static TideTile Tile(int value, bool sealedTile = false)
    {
        var tile = new GameObject("tile").AddComponent<TideTile>();
        tile.Configure(new Vector2Int(), value, sealedTile);
        return tile;
    }

    private static void TileContracts()
    {
        for (int value = 1; value <= 10; value++)
        {
            TideTile tile = Tile(value);
            Check(tile.GetMaxTake() == (value > 5 ? value - 5 : value < 5 ? value - 1 : 0), "GDD take allowance " + value);
            for (int amount = -1; amount <= 11; amount++)
            {
                Check(tile.CanReceive(amount) == (amount > 0 && amount <= 10 - value), $"capacity {value}/{amount}");
                TideTile take = Tile(value);
                take.ApplyTake(amount);
                Check(take.CurrentTideValue >= 1 && take.CurrentTideValue <= 10, "take range");
                TideTile place = Tile(value);
                place.ApplyPlace(amount);
                Check(place.CurrentTideValue >= 1 && place.CurrentTideValue <= 10, "place range");
            }
            for (int decay = 0; decay <= 15; decay++)
            {
                TideTile decayed = Tile(value);
                decayed.ApplyDecay(decay);
                Check(decayed.CurrentTideValue == (value > 5 ? Math.Max(5, value - decay) : value), "decay floor and low tile immunity");
            }
        }
        TideTile sealedTile = Tile(8, true);
        sealedTile.ApplyTake(3);
        sealedTile.ApplyPlace(2);
        sealedTile.ApplyDecay(4);
        Check(sealedTile.CurrentTideValue == 8 && sealedTile.GetMaxTake() == 0 && !sealedTile.CanReceive(1), "sealed tile rejects all interactions");
        TideTile huge = Tile(9);
        Check(!huge.CanReceive(int.MaxValue), "capacity rejects overflow");
        huge.ApplyPlace(int.MaxValue);
        Check(huge.CurrentTideValue == 10, "placement cannot overflow");
        huge.ApplyTake(int.MaxValue);
        Check(huge.CurrentTideValue == 1, "take cannot overflow");
    }

    private static void PuzzleContracts()
    {
        PuzzleData data = new PuzzleData();
        Check(data.IsValid(), "default puzzle valid");
        data.tileValues = new[] { 5, 5, 5, 5, 5, 5, 5, 5, 5 };
        Check(data.GetResolvedGridDimensions() == new Vector2Int(3, 3), "legacy nine-value asset resolves to 3x3");
        data.gridRows = 1;
        data.gridCols = 2;
        data.tileValues = new[] { 0, 0 };
        Check(!data.IsValid(), "all-sealed board invalid");
        data.tileValues = new[] { 0, 8 };
        Check(data.IsValid() && data.GetSealedMap()[0, 0] && data.GetGrid()[0, 0] == 0, "implicit sealed sentinel aligns grid and traversal");
        data.gridCols = int.MaxValue;
        Check(!data.IsValid() && data.GetGrid().Length == 0 && data.GetSealedMap().Length == 0, "oversized data rejected before allocation");
        PuzzleWinCondition condition = new PuzzleWinCondition { type = WinConditionType.PercentageAtTarget };
        foreach (float percent in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, 1.01f })
        {
            condition.requiredPercent = percent;
            Check(!condition.IsMet(new int[,] { { 5, 5 } }, (bool[,])null), "invalid percent " + percent);
        }
        condition.requiredPercent = .75f;
        Check(condition.IsMet(new int[,] { { 5, 5, 5, 1 } }, (bool[,])null), "percentage equality");
        Check(!condition.IsMet(new int[,] { { 5, 5, 1, 1 } }, (bool[,])null), "percentage below threshold");
        Check(!condition.IsMet(new int[,] { { 5, 5 } }, new bool[,] { { true, true } }), "all sealed never vacuously complete");
        condition.targetValue = 11;
        Check(!condition.IsMet(new int[,] { { 11, 11 } }, (bool[,])null), "out-of-range target invalid");
    }

    private static TideManager Manager(int[,] grid)
    {
        TideManager manager = new GameObject("manager").AddComponent<TideManager>();
        manager.InitializePuzzle(grid, new Vector2Int(-1, -1));
        Invoke(manager, "StoreInitialValues");
        Invoke(manager, "GenerateHiddenLogicBoard");
        return manager;
    }

    private static TideManager Manager(PuzzleData data)
    {
        TideManager manager = new GameObject("manager").AddComponent<TideManager>();
        manager.InitializePuzzle(data);
        Invoke(manager, "StoreInitialValues");
        Invoke(manager, "GenerateHiddenLogicBoard");
        return manager;
    }

    private static void Click(TideManager manager, int row, int col) => Invoke(manager, "OnUiTileClicked", row, col);
    private static bool Same(int[,] left, int[,] right)
    {
        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1)) return false;
        for (int row = 0; row < left.GetLength(0); row++)
            for (int col = 0; col < left.GetLength(1); col++)
                if (left[row, col] != right[row, col]) return false;
        return true;
    }

    private static void ManagerContracts()
    {
        GameStateManager.Instance = new GameStateManager { PendingPuzzleBoxId = "recovery_box" };
        int[,] authored = { { 8, 1, 5 }, { 5, 5, 5 } };
        TideManager manager = Manager(authored);
        manager.SetRequestedTakeAmount(2);
        Click(manager, 0, 0);
        Check(manager.CarriedAmount == 2, "partial pickup");
        Check(Get<TideTile[,]>(manager, "activeTiles")[0, 0].CurrentTideValue == 6, "source debited");
        Check(Same(manager.CaptureCurrentGrid(), authored), "capture excludes in-flight pickup");
        Click(manager, 0, 0);
        Check(Same(manager.CaptureCurrentGrid(), authored) && !manager.IsCarrying, "cancel restores tide");
        manager.SetRequestedTakeAmount(3);
        Click(manager, 0, 0);
        Click(manager, 0, 1);
        Check(manager.CaptureCurrentGrid()[0, 0] == 5 && manager.CaptureCurrentGrid()[0, 1] == 4, "valid placement");
        Check(Same(GameStateManager.Instance.Grids["recovery_box"], manager.CaptureCurrentGrid()), "completed move updates persisted snapshot");
        manager.UndoLastMove();
        Check(Same(manager.CaptureCurrentGrid(), authored), "undo reverses completed move");
        Check(Same(GameStateManager.Instance.Grids["recovery_box"], authored), "undo updates persisted snapshot");
        Click(manager, 0, 0);
        manager.UndoLastMove();
        Check(Same(manager.CaptureCurrentGrid(), authored) && !manager.IsCarrying, "undo cancels pickup first");
        Click(manager, 0, 0);
        Click(manager, 0, 1);
        manager.ResetPuzzle();
        Check(Same(manager.CaptureCurrentGrid(), authored), "reset authored layout");
        manager.UndoLastMove();
        Check(Same(manager.CaptureCurrentGrid(), authored), "reset clears history");

        TideManager resume = new GameObject("resume").AddComponent<TideManager>();
        resume.ConfigureOverlaySession(null, authored, new Vector2Int(-1, -1), new int[,] { { 1, 1, 1 }, { 1, 1, 1 } }, "box", "island_lust", "p", .2f, Vector3.zero);
        Invoke(resume, "InitializeOverlaySession");
        Check(resume.CaptureCurrentGrid()[0, 0] == 1, "resume applies persisted grid");
        resume.ResetPuzzle();
        Check(Same(resume.CaptureCurrentGrid(), authored), "reset recovers saved soft lock");
        resume.SetRequestedTakeAmount(1);
        Click(resume, 0, 0);
        int exits = 0;
        resume.OverlayExitRequested += () => exits++;
        resume.RequestOverlayClose();
        resume.RequestOverlayClose();
        Check(exits == 1 && !resume.IsCarrying && Same(resume.CaptureCurrentGrid(), authored), "overlay close cancels carry once");

        TideManager solved = Manager(new int[,] { { 8, 2 } });
        int before = GameStateManager.Instance.Completions;
        solved.SetRequestedTakeAmount(3);
        Click(solved, 0, 0);
        Click(solved, 0, 1);
        Invoke(solved, "EvaluatePuzzleCompletion");
        Check(solved.IsPuzzleSolved && GameStateManager.Instance.Completions == before + 1, "completion exactly once");
        solved.ResetPuzzle();
        solved.UndoLastMove();
        Check(Same(solved.CaptureCurrentGrid(), new int[,] { { 5, 5 } }), "completed puzzle immutable");
    }

    private static void MovementAndDecayContracts()
    {
        GameStateManager.Instance = new GameStateManager();
        TideManager manager = Manager(new int[,] { { 8, 2, 5, 5 }, { 5, 5, 5, 5 }, { 5, 5, 5, 5 } });
        TideTile[,] tiles = Get<TideTile[,]>(manager, "activeTiles");
        Check((bool)Invoke(manager, "CanReachWithinCarrySteps", tiles[0, 0], tiles[2, 2]), "two diagonal carry steps");
        Check(!(bool)Invoke(manager, "CanReachWithinCarrySteps", tiles[0, 0], tiles[0, 3]), "three steps rejected");
        Invoke(manager, "TrySetSealedTile", new Vector2Int(1, 0), true);
        Invoke(manager, "TrySetSealedTile", new Vector2Int(0, 1), true);
        Check(!(bool)Invoke(manager, "CanReachWithinCarrySteps", tiles[0, 0], tiles[1, 1]), "diagonal cannot cut sealed corners");
        Check(manager.CaptureCurrentGrid()[0, 1] == 0, "sealed captures preserve sentinel");

        int[,] unstable = { { 8, 7, 6 }, { 9, 2, 5 } };
        manager = Manager(unstable);
        manager.SetRequestedTakeAmount(1);
        Click(manager, 0, 0);
        Check(Get<TideTile[,]>(manager, "activeTiles")[1, 0].CurrentTideValue == 9, "pickup never triggers decay");
        Click(manager, 0, 0);
        Check(Same(manager.CaptureCurrentGrid(), unstable), "cancel never triggers decay");
        Click(manager, 0, 0);
        Click(manager, 1, 1);
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 6, 6, 5 }, { 8, 3, 5 } }), "all four high tiles decay after placement only");
        manager.UndoLastMove();
        Check(Same(manager.CaptureCurrentGrid(), unstable), "undo restores tide removed by decay");

        PuzzleData economyData = new PuzzleData { gridRows = 1, gridCols = 4, tileValues = new[] { 6, 3, 5, 1 }, enableConsumption = true, consumptionAmount = 1, enableGreedEconomy = true, coinTileYield = 2 };
        manager = Manager(economyData);
        manager.SetRequestedTakeAmount(1);
        Click(manager, 0, 0);
        Click(manager, 0, 1);
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 7, 3, 5, 1 } }), "consumption then direct-data economy uses bound variant config");
        manager.UndoLastMove();
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 6, 3, 5, 1 } }), "undo reverses consumption and economy");
        economyData.coinTileYield = int.MaxValue;
        manager = Manager(economyData);
        manager.SetRequestedTakeAmount(1);
        Click(manager, 0, 0);
        Click(manager, 0, 1);
        Check(manager.CaptureCurrentGrid()[0, 0] == 10, "economy yield safely saturates on malformed large input");

        for (int source = 1; source <= 10; source++)
            for (int destination = 1; destination <= 10; destination++)
                for (int amount = 1; amount <= 5; amount++)
                {
                    int[,] grid = { { source, destination, 1 } };
                    TideManager candidate = Manager(grid);
                    candidate.SetRequestedTakeAmount(amount);
                    Click(candidate, 0, 0);
                    bool canTake = amount <= (source > 5 ? source - 5 : source < 5 ? source - 1 : 0);
                    Check(candidate.IsCarrying == canTake, "exhaustive take permission");
                    if (!canTake) continue;
                    Click(candidate, 0, 1);
                    bool canPlace = amount <= 10 - destination;
                    Check(candidate.IsCarrying == !canPlace, "exhaustive placement permission");
                    if (canPlace)
                    {
                        int[,] moved = candidate.CaptureCurrentGrid();
                        Check(moved[0, 0] + moved[0, 1] == source + destination, "normal move conserves tide");
                        candidate.UndoLastMove();
                    }
                    else candidate.CancelCarriedTide();
                    Check(Same(candidate.CaptureCurrentGrid(), grid), "undo/cancel round trip");
                }
    }

    private static void GuardAndSceneContracts()
    {
        IslandRestorationTracker.Instance = new IslandRestorationTracker();
        GameStateManager.Instance = new GameStateManager { PendingPuzzleEncounterId = "puzzle_a", PendingPuzzleBoxId = "box_a", PendingPuzzleIslandId = "island_lust" };
        PuzzleData data = new PuzzleData { gridRows = 1, gridCols = 3, tileValues = new[] { 8, 0, 2 }, sealedPositions = new[] { new Vector2Int(1, 0) }, lockedTileIslandId = "island_lust" };
        IslandRestorationTracker.Instance.Cleared.Add("island_lust::puzzle_a_sealed_1_0_guard");
        TideManager manager = Manager(data);
        Check(!Get<TideTile[,]>(manager, "activeTiles")[0, 1].IsSealed && manager.CaptureCurrentGrid()[0, 1] == 5, "generic guard clear opens balanced sentinel tile");
        GameStateManager.Instance.PendingPuzzleEncounterId = "puzzle_b";
        manager = Manager(data);
        Check(Get<TideTile[,]>(manager, "activeTiles")[0, 1].IsSealed, "guard IDs scoped to puzzle encounter");
        GameStateManager.Instance.PendingPuzzleEncounterId = "puzzle_a";
        GameStateManager.Instance.PendingPuzzleData = data;
        GameStateManager.Instance.Grids["box_a"] = new int[,] { { 6, 0, 4 } };
        manager = new GameObject("scene").AddComponent<TideManager>();
        Invoke(manager, "Start");
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 6, 5, 4 } }), "scene resume retains progress and opens saved sealed tile at five");
        manager.ResetPuzzle();
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 8, 5, 2 } }), "scene reset returns authored grid after resume");
        PuzzleData lockedData = new PuzzleData { gridRows = 1, gridCols = 3, tileValues = new[] { 8, 8, 1 }, lockedPosition = new Vector2Int(1, 0), lockedTileIslandId = "island_lust", lockedTileEncounterId = "locked_guard" };
        GameStateManager.Instance.PendingPuzzleData = lockedData;
        GameStateManager.Instance.Grids["box_a"] = new int[,] { { 6, 0, 3 } };
        IslandRestorationTracker.Instance.Cleared.Add("island_lust::locked_guard");
        manager = new GameObject("locked-scene").AddComponent<TideManager>();
        Invoke(manager, "Start");
        Check(Same(manager.CaptureCurrentGrid(), new int[,] { { 6, 8, 3 } }), "guard return preserves authored Tide hidden by sealed snapshot");
        IslandRestorationTracker.Instance = null;
        GameStateManager.Instance = new GameStateManager();
    }

    private static PuzzleData ReadAsset(string path)
    {
        string source = File.ReadAllText(path);
        int IntField(string field, int fallback) { Match match = Regex.Match(source, @"^  " + field + @": (\d+)", RegexOptions.Multiline); return match.Success ? int.Parse(match.Groups[1].Value) : fallback; }
        Vector2Int Position(string field) { Match match = Regex.Match(source, @"^  " + field + @": \{x: (-?\d+), y: (-?\d+)\}", RegexOptions.Multiline); return match.Success ? new Vector2Int(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : new Vector2Int(-1, -1); }
        var data = new PuzzleData { gridRows = IntField("gridRows", 4), gridCols = IntField("gridCols", 4), sealedPosition = Position("sealedPosition"), lockedPosition = Position("lockedPosition"), instabilityThreshold = IntField("instabilityThreshold", 3), enableConsumption = IntField("enableConsumption", 0) != 0, consumptionAmount = IntField("consumptionAmount", 1), enableGreedEconomy = IntField("enableGreedEconomy", 0) != 0, coinTileYield = IntField("coinTileYield", 2) };
        string valueText = Regex.Match(source, @"^  tileValues:(.*?)(?=^  [a-zA-Z])", RegexOptions.Singleline | RegexOptions.Multiline).Groups[1].Value.Trim();
        if (valueText.StartsWith("-")) data.tileValues = Regex.Matches(valueText, @"- (\d+)").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        else { data.tileValues = new int[valueText.Length / 8]; for (int index = 0; index < data.tileValues.Length; index++) data.tileValues[index] = BitConverter.ToInt32(Convert.FromHexString(valueText.Substring(index * 8, 8))); }
        string positions = Regex.Match(source, @"^  sealedPositions:(.*?)(?=^  [a-zA-Z])", RegexOptions.Singleline | RegexOptions.Multiline).Groups[1].Value;
        data.sealedPositions = Regex.Matches(positions, @"x: (\d+), y: (\d+)").Select(m => new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToArray();
        data.winCondition.type = (WinConditionType)int.Parse(Regex.Match(source, @"    type: (\d+)").Groups[1].Value);
        data.winCondition.targetValue = int.Parse(Regex.Match(source, @"    targetValue: (\d+)").Groups[1].Value);
        data.winCondition.requiredPercent = float.Parse(Regex.Match(source, @"    requiredPercent: ([0-9.]+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return data;
    }

    private static void AuthoredAssetContracts()
    {
        foreach (string path in Directory.GetFiles(Path.Combine(ProjectRoot, "Assets/Resources/Puzzles"), "*.asset").OrderBy(p => p))
        {
            PuzzleData data = ReadAsset(path);
            string name = Path.GetFileNameWithoutExtension(path);
            Check(data.IsValid(), name + " valid");
            TideManager manager = Manager(data);
            int[,] initial = manager.CaptureCurrentGrid();
            if (data.HasLockedTile) Invoke(manager, "TrySetSealedTile", data.lockedPosition, false);
            int[,] unlocked = manager.CaptureCurrentGrid();
            if (data.winCondition.type == WinConditionType.AllEqualToTarget && !data.enableGreedEconomy)
            {
                int requiredTotal = unlocked.Cast<int>().Count(v => v > 0) * data.winCondition.targetValue;
                int total = unlocked.Cast<int>().Sum();
                int minimumLoss = MinimumNextMoveLoss(manager, unlocked);
                Console.WriteLine($"ASSET {name}: tide={total}, all-target={requiredTotal}, minimum first-move loss={minimumLoss}");
                Check(total >= requiredTotal, name + " sufficient tide after guard clear");
                Check(data.enableConsumption || total == requiredTotal || total - requiredTotal >= data.instabilityThreshold + 1, name + " surplus can be removed by a decay event");
                Check(total == requiredTotal || total - minimumLoss >= requiredTotal, name + " first move does not make completion impossible");
            }
            if (unlocked.Length <= 16)
            {
                bool solved = SolveActualSource(manager, unlocked, 40000, out int visited);
                Console.WriteLine($"SOLVER {name}: {(solved ? "witness found" : "search capped/exhausted")}, states={visited}");
                Check(solved, name + " source-produced solution witness within bounded search");
            }
        }
    }

    private static void Restore(TideManager manager, int[,] grid)
    {
        Set(manager, "puzzleSolved", false);
        manager.CancelCarriedTide();
        TideTile[,] tiles = Get<TideTile[,]>(manager, "activeTiles");
        for (int row = 0; row < grid.GetLength(0); row++)
            for (int col = 0; col < grid.GetLength(1); col++)
                if (!tiles[row, col].IsSealed) tiles[row, col].CurrentTideValue = grid[row, col];
    }

    private static IEnumerable<int[,]> NextStates(TideManager manager, int[,] grid)
    {
        TideTile[,] tiles = Get<TideTile[,]>(manager, "activeTiles");
        for (int sourceRow = 0; sourceRow < grid.GetLength(0); sourceRow++)
            for (int sourceCol = 0; sourceCol < grid.GetLength(1); sourceCol++)
            {
                Restore(manager, grid);
                int max = tiles[sourceRow, sourceCol].GetMaxTake();
                for (int destinationRow = 0; destinationRow < grid.GetLength(0); destinationRow++)
                    for (int destinationCol = 0; destinationCol < grid.GetLength(1); destinationCol++)
                    {
                        if (!(bool)Invoke(manager, "CanReachWithinCarrySteps", tiles[sourceRow, sourceCol], tiles[destinationRow, destinationCol])) continue;
                        for (int amount = 1; amount <= max; amount++)
                        {
                            Restore(manager, grid);
                            if (!tiles[destinationRow, destinationCol].CanReceive(amount)) continue;
                            manager.SetRequestedTakeAmount(amount);
                            Click(manager, sourceRow, sourceCol);
                            Click(manager, destinationRow, destinationCol);
                            yield return manager.CaptureCurrentGrid();
                        }
                    }
            }
    }

    private static int MinimumNextMoveLoss(TideManager manager, int[,] grid)
    {
        int total = grid.Cast<int>().Sum();
        int minimumLoss = int.MaxValue;
        foreach (int[,] next in NextStates(manager, grid)) minimumLoss = Math.Min(minimumLoss, total - next.Cast<int>().Sum());
        return minimumLoss == int.MaxValue ? 0 : minimumLoss;
    }

    private static bool SolveActualSource(TideManager manager, int[,] initial, int cap, out int visited)
    {
        string Key(int[,] grid) => string.Join(",", grid.Cast<int>());
        var queue = new PriorityQueue<int[,], int>();
        var seen = new HashSet<string> { Key(initial) };
        queue.Enqueue(initial, 0);
        bool[,] seals = Get<bool[,]>(manager, "sealedTiles");
        PuzzleWinCondition condition = Get<PuzzleWinCondition>(manager, "winCondition");
        while (queue.Count > 0 && seen.Count < cap)
        {
            int[,] current = queue.Dequeue();
            if (condition.IsMet(current, seals)) { visited = seen.Count; return true; }
            foreach (int[,] next in NextStates(manager, current))
            {
                if (!seen.Add(Key(next))) continue;
                if (condition.IsMet(next, seals)) { visited = seen.Count; return true; }
                if (condition.type == WinConditionType.AllEqualToTarget && !Get<bool>(manager, "enableGreedEconomy") && next.Cast<int>().Sum() < next.Cast<int>().Count(v => v > 0) * condition.targetValue) continue;
                int priority = next.Cast<int>().Count(v => v > 0 && v != condition.targetValue) * 20 + next.Cast<int>().Where(v => v > 0).Sum(v => Math.Abs(v - condition.targetValue));
                queue.Enqueue(next, priority);
            }
        }
        visited = seen.Count;
        return false;
    }
}
