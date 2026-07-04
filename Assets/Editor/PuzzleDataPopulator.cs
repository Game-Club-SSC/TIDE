using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Generates PuzzleData ScriptableObjects for all 7 islands (28 puzzles total).
/// Uses Opus 4.7's mathematically verified puzzle layouts.
/// Access via: TIDE > Populate All Puzzles
/// </summary>
public static class PuzzleDataPopulator
{
    private const string OutputFolder = "Assets/Resources/Puzzles";

    private struct PuzzleDef
    {
        public string id;
        public string name;
        public int cols;
        public int rows;
        public int[] values;
        public Vector2Int[] sealedTiles;
        public WinConditionType winType;
        public float winPercent;
        public bool consumption;
        public bool greedEconomy;
    }

    [MenuItem("TIDE/Populate All Puzzles")]
    public static void PopulateAllPuzzles()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = Path.GetDirectoryName(OutputFolder);
            string folderName = Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        var all = new List<PuzzleDef>();
        all.AddRange(GetLustPuzzles());
        all.AddRange(GetWrathPuzzles());
        all.AddRange(GetSlothPuzzles());
        all.AddRange(GetPridePuzzles());
        all.AddRange(GetEnvyPuzzles());
        all.AddRange(GetGluttonyPuzzles());

        int created = 0;
        int skipped = 0;

        foreach (var p in all)
        {
            string path = $"{OutputFolder}/{p.id}.asset";

            if (AssetDatabase.LoadAssetAtPath<PuzzleData>(path) != null)
            {
                skipped++;
                continue;
            }

            PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
            data.gridCols = p.cols;
            data.gridRows = p.rows;
            data.tileValues = p.values;
            data.sealedPositions = p.sealedTiles;
            data.winCondition = new PuzzleWinCondition
            {
                type = p.winType,
                targetValue = 5,
                requiredPercent = p.winPercent
            };
            data.instabilityThreshold = 3;
            data.enableConsumption = p.consumption;
            data.enableGreedEconomy = p.greedEconomy;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        // Also repopulate Greed puzzles with updated format
        created += PopulateGreedPuzzles();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PuzzleDataPopulator] Created {created} puzzle assets, skipped {skipped} existing");
        EditorUtility.DisplayDialog("All Puzzles Created",
            $"Created {created} PuzzleData assets.\n\n" +
            "Lust: 4 puzzles (Easiest)\n" +
            "Wrath: 4 puzzles (Easy-Medium)\n" +
            "Sloth: 4 puzzles (Medium)\n" +
            "Pride: 4 puzzles (Medium-Hard)\n" +
            "Envy: 4 puzzles (Hard)\n" +
            "Gluttony: 4 puzzles (Hardest)\n" +
            "Greed: 4 puzzles (reformatted)\n\n" +
            "Total: 28 puzzles",
            "OK");
    }

    [MenuItem("TIDE/Populate All Puzzles", true)]
    public static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }

    // Also keep the original Greed-only menu item
    [MenuItem("TIDE/Populate Greed Puzzles")]
    public static void PopulateGreedPuzzlesMenu()
    {
        int created = PopulateGreedPuzzles();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PuzzleDataPopulator] Greed: {created} puzzles created");
    }

    [MenuItem("TIDE/Populate Greed Puzzles", true)]
    public static bool PopulateGreedPuzzlesValidate()
    {
        return !EditorApplication.isPlaying;
    }

    private static int PopulateGreedPuzzles()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = Path.GetDirectoryName(OutputFolder);
            string folderName = Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        var greed = GetGreedPuzzles();

        foreach (var p in greed)
        {
            string path = $"{OutputFolder}/{p.id}.asset";

            if (AssetDatabase.LoadAssetAtPath<PuzzleData>(path) != null)
                continue;

            PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
            data.gridCols = p.cols;
            data.gridRows = p.rows;
            data.tileValues = p.values;
            data.sealedPositions = p.sealedTiles;
            data.winCondition = new PuzzleWinCondition
            {
                type = p.winType,
                targetValue = 5,
                requiredPercent = p.winPercent
            };
            data.instabilityThreshold = 3;
            data.enableConsumption = p.consumption;
            data.enableGreedEconomy = p.greedEconomy;

            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        return created;
    }

    // ============================================================
    // GREED (Earth) — Reformatted with sealedPositions array
    // ============================================================
    private static PuzzleDef[] GetGreedPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: Easy — 60% win, 12 active + 4 sealed
            new PuzzleDef {
                id = "puzzle_greed_p1", name = "Greed P1 - Initiation",
                cols = 4, rows = 4,
                values = new int[] {
                    9, 0, 3, 0,
                    0, 0, 3, 0,
                    0, 0, 9, 3,
                    3, 5, 5, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 0), new Vector2Int(3, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = true
            },
            // p2: Medium — 60% win, 13 active + 1 sealed (uses Greed economy)
            new PuzzleDef {
                id = "puzzle_greed_p2", name = "Greed P2 - The Collector's Reach",
                cols = 4, rows = 4,
                values = new int[] {
                    9, 3, 0, 0,
                    0, 5, 3, 0,
                    3, 0, 3, 3,
                    9, 3, 5, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(3, 0)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = true
            },
            // p3: Hard — 100% win, 16 active, 0 sealed
            new PuzzleDef {
                id = "puzzle_greed_p3", name = "Greed P3 - Perfect Equilibrium",
                cols = 4, rows = 4,
                values = new int[] {
                    10, 5, 5, 10,
                    5, 2, 2, 5,
                    5, 2, 2, 5,
                    10, 5, 2, 5
                },
                sealedTiles = new Vector2Int[0],
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: Expert — 100% win, 14 active + 2 sealed (consumption mechanic)
            new PuzzleDef {
                id = "puzzle_greed_p4", name = "Greed P4 - The Glutton's Decay",
                cols = 4, rows = 4,
                values = new int[] {
                    10, 5, 5, 5,
                    5, 0, 5, 5,
                    5, 5, 0, 5,
                    5, 5, 5, 10
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 1), new Vector2Int(2, 2)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // LUST (Water) — Easiest
    // ============================================================
    private static PuzzleDef[] GetLustPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: Shallow Pools — 60%, 4 sealed, 1 move
            new PuzzleDef {
                id = "puzzle_lust_p1", name = "Lust P1 - Shallow Pools",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 0, 5, 5,
                    5, 6, 5, 0,
                    5, 5, 4, 5,
                    0, 5, 0, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 0), new Vector2Int(3, 1),
                    new Vector2Int(0, 3), new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p2: Rising Foam — 60%, 3 sealed, 2 moves
            new PuzzleDef {
                id = "puzzle_lust_p2", name = "Lust P2 - Rising Foam",
                cols = 4, rows = 4,
                values = new int[] {
                    6, 6, 0, 5,
                    5, 4, 4, 5,
                    0, 5, 5, 6,
                    5, 5, 0, 6
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(0, 2),
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p3: Deep Current — 100%, 4 sealed, 2 moves
            new PuzzleDef {
                id = "puzzle_lust_p3", name = "Lust P3 - Deep Current",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 0, 3, 5,
                    5, 6, 5, 0,
                    5, 4, 5, 5,
                    0, 5, 5, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 0), new Vector2Int(3, 1),
                    new Vector2Int(0, 3), new Vector2Int(3, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: The Drowned Vault — 100%, 3 sealed, 3 moves
            new PuzzleDef {
                id = "puzzle_lust_p4", name = "Lust P4 - The Drowned Vault",
                cols = 4, rows = 4,
                values = new int[] {
                    6, 5, 4, 5,
                    0, 6, 5, 4,
                    5, 5, 0, 6,
                    5, 4, 5, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(0, 1), new Vector2Int(2, 2),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // WRATH (Fire) — Easy-Medium
    // ============================================================
    private static PuzzleDef[] GetWrathPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: Ember Grid — 60%, 3 sealed, 1 move
            new PuzzleDef {
                id = "puzzle_wrath_p1", name = "Wrath P1 - Ember Grid",
                cols = 4, rows = 4,
                values = new int[] {
                    8, 5, 0, 4,
                    5, 7, 5, 5,
                    4, 0, 5, 6,
                    5, 5, 4, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(1, 2),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p2: Coal Bed — 60%, 4 sealed, 1 move
            new PuzzleDef {
                id = "puzzle_wrath_p2", name = "Wrath P2 - Coal Bed",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 4, 0, 5,
                    5, 6, 5, 0,
                    0, 5, 4, 6,
                    5, 5, 0, 4
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(3, 1),
                    new Vector2Int(0, 2), new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p3: The Forge — 100%, 4 sealed, 3 moves
            new PuzzleDef {
                id = "puzzle_wrath_p3", name = "Wrath P3 - The Forge",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 5, 3, 0,
                    5, 6, 5, 4,
                    0, 5, 4, 6,
                    5, 5, 0, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 0), new Vector2Int(0, 2),
                    new Vector2Int(2, 3), new Vector2Int(3, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: Molten Core — 100%, 3 sealed, 3 moves
            new PuzzleDef {
                id = "puzzle_wrath_p4", name = "Wrath P4 - Molten Core",
                cols = 4, rows = 4,
                values = new int[] {
                    6, 4, 5, 0,
                    5, 6, 4, 5,
                    4, 5, 6, 5,
                    0, 5, 0, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 0), new Vector2Int(0, 3),
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // SLOTH (Air) — Medium
    // ============================================================
    private static PuzzleDef[] GetSlothPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: Drifting Sands — 60%, 3 sealed, 1 move
            new PuzzleDef {
                id = "puzzle_sloth_p1", name = "Sloth P1 - Drifting Sands",
                cols = 4, rows = 4,
                values = new int[] {
                    8, 5, 4, 6,
                    5, 0, 5, 5,
                    4, 6, 5, 0,
                    5, 5, 0, 7
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 1), new Vector2Int(3, 2),
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p2: Slumber Field — 60%, 4 sealed, 2 moves
            new PuzzleDef {
                id = "puzzle_sloth_p2", name = "Sloth P2 - Slumber Field",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 4, 0, 5,
                    5, 6, 5, 4,
                    0, 5, 6, 5,
                    4, 0, 5, 6
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(0, 2),
                    new Vector2Int(1, 3), new Vector2Int(3, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p3: The Long Rest — 100%, 3 sealed, 4 moves
            new PuzzleDef {
                id = "puzzle_sloth_p3", name = "Sloth P3 - The Long Rest",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 4, 5, 0,
                    5, 7, 3, 5,
                    4, 5, 6, 5,
                    0, 5, 4, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 0), new Vector2Int(0, 3),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: Eternal Dream — 100%, 2 sealed, 4 moves
            new PuzzleDef {
                id = "puzzle_sloth_p4", name = "Sloth P4 - Eternal Dream",
                cols = 4, rows = 4,
                values = new int[] {
                    6, 4, 5, 6,
                    5, 0, 5, 4,
                    4, 4, 6, 5,
                    6, 5, 0, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 1), new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // PRIDE (Space) — Medium-Hard
    // ============================================================
    private static PuzzleDef[] GetPridePuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: Hall of Mirrors — 60%, 3 sealed, 2 moves
            new PuzzleDef {
                id = "puzzle_pride_p1", name = "Pride P1 - Hall of Mirrors",
                cols = 4, rows = 4,
                values = new int[] {
                    9, 5, 4, 6,
                    5, 7, 5, 0,
                    4, 5, 8, 5,
                    0, 5, 4, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 1), new Vector2Int(0, 3),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p2: Gilded Throne — 60%, 4 sealed, 2 moves
            new PuzzleDef {
                id = "puzzle_pride_p2", name = "Pride P2 - Gilded Throne",
                cols = 4, rows = 4,
                values = new int[] {
                    8, 4, 0, 5,
                    5, 6, 4, 6,
                    0, 5, 7, 5,
                    5, 0, 4, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 0), new Vector2Int(0, 2),
                    new Vector2Int(1, 3), new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p3: The Reflection — 100%, 3 sealed, 4 moves
            new PuzzleDef {
                id = "puzzle_pride_p3", name = "Pride P3 - The Reflection",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 5, 3, 0,
                    5, 7, 5, 4,
                    4, 5, 6, 5,
                    0, 5, 4, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 0), new Vector2Int(0, 3),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: Apex — 100%, 2 sealed, 5 moves
            new PuzzleDef {
                id = "puzzle_pride_p4", name = "Pride P4 - Apex",
                cols = 4, rows = 4,
                values = new int[] {
                    6, 4, 7, 4,
                    5, 0, 5, 4,
                    4, 6, 5, 6,
                    5, 4, 0, 5
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 1), new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // ENVY (Air) — Hard (decay tricks in 60% puzzles)
    // ============================================================
    private static PuzzleDef[] GetEnvyPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: The Coveted Path — 60%, 3 sealed, 2 moves (decay as tool)
            new PuzzleDef {
                id = "puzzle_envy_p1", name = "Envy P1 - The Coveted Path",
                cols = 4, rows = 4,
                values = new int[] {
                    9, 5, 4, 7,
                    5, 8, 5, 4,
                    4, 5, 9, 5,
                    0, 5, 0, 0
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(0, 3), new Vector2Int(2, 3),
                    new Vector2Int(3, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p2: Green Mirror — 60%, 2 sealed, 3 moves (decay cascade)
            new PuzzleDef {
                id = "puzzle_envy_p2", name = "Envy P2 - Green Mirror",
                cols = 4, rows = 4,
                values = new int[] {
                    8, 4, 6, 5,
                    5, 7, 4, 6,
                    0, 5, 6, 4,
                    5, 5, 0, 8
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(0, 2), new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = false, greedEconomy = false
            },
            // p3: The Usurper's Gate — 100%, 2 sealed, 4 moves
            new PuzzleDef {
                id = "puzzle_envy_p3", name = "Envy P3 - The Usurper's Gate",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 5, 3, 0,
                    5, 7, 4, 5,
                    4, 5, 6, 5,
                    5, 5, 0, 4
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(3, 0), new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: Shade's Labyrinth — 100%, 1 sealed, 6 moves
            new PuzzleDef {
                id = "puzzle_envy_p4", name = "Envy P4 - Shade's Labyrinth",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 4, 5, 4,
                    5, 7, 4, 5,
                    4, 5, 7, 5,
                    5, 4, 0, 4
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }

    // ============================================================
    // GLUTTONY (Earth) — Hardest (decay-driven + consumption)
    // ============================================================
    private static PuzzleDef[] GetGluttonyPuzzles()
    {
        return new PuzzleDef[]
        {
            // p1: The Endless Table — 60%, 2 sealed, 2 moves (massive decay)
            new PuzzleDef {
                id = "puzzle_gluttony_p1", name = "Gluttony P1 - The Endless Table",
                cols = 4, rows = 4,
                values = new int[] {
                    9, 4, 7, 4,
                    5, 8, 4, 6,
                    6, 5, 9, 4,
                    5, 0, 0, 8
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(1, 3), new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = true, greedEconomy = false
            },
            // p2: Gorge — 60%, 1 sealed, 2 moves (decay-driven)
            new PuzzleDef {
                id = "puzzle_gluttony_p2", name = "Gluttony P2 - Gorge",
                cols = 4, rows = 4,
                values = new int[] {
                    8, 4, 8, 4,
                    4, 7, 4, 7,
                    8, 4, 8, 4,
                    5, 5, 0, 6
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.PercentageAtTarget, winPercent = 0.6f,
                consumption = true, greedEconomy = false
            },
            // p3: The Devourer's Feast — 100%, 1 sealed, 6 moves
            new PuzzleDef {
                id = "puzzle_gluttony_p3", name = "Gluttony P3 - The Devourer's Feast",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 4, 5, 4,
                    5, 7, 4, 5,
                    4, 5, 6, 5,
                    6, 4, 0, 4
                },
                sealedTiles = new Vector2Int[] {
                    new Vector2Int(2, 3)
                },
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
            // p4: The Last Supper — 100%, 0 sealed, 3 moves (clean capstone)
            new PuzzleDef {
                id = "puzzle_gluttony_p4", name = "Gluttony P4 - The Last Supper",
                cols = 4, rows = 4,
                values = new int[] {
                    7, 3, 5, 5,
                    5, 7, 5, 5,
                    5, 5, 7, 3,
                    3, 5, 5, 5
                },
                sealedTiles = new Vector2Int[0],
                winType = WinConditionType.AllEqualToTarget, winPercent = 1f,
                consumption = false, greedEconomy = false
            },
        };
    }
}
