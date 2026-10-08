# Puzzle source verification

Run with .NET 8 or newer from the repository root:

```sh
dotnet run --project Verification/PuzzleIslandHarness/PuzzleIslandHarness.csproj
```

The project links `Assets/PuzzleData.cs`, `Assets/TideTile.cs`, `Assets/TideManager.cs` and `Assets/GameConstants.cs` directly. `UnityShims.cs` stubs engine rendering, coroutine scheduling, scene loading and neighboring services. It does **not** claim a Unity compile, Play Mode run, file persistence round trip, or visual verification. The older assembly-inspection `Program.cs` is retained for reference and excluded from this source project; it requires an existing Unity assembly and contains historical expectations.

The default run exercises tile value bounds and legal pickup/capacity combinations, sealed traversal and diagonal corners, placement-only decay, carry cancellation, completed-board capture, authored reset after resume, undo including decay and economy effects, guard-scoped unlocks, scene grid restoration, and idempotent completion. `--logic-only` omits the asset solver.

For each authored puzzle, the harness decodes the actual serialized values, applies its explicit guard unlock, and searches transitions produced by the linked `TideManager` input methods. The search is bounded to 40,000 distinct states. A returned solution is a source-produced witness; exhausting the bound is a failed verification, not a proof that a puzzle is impossible. All 41 assets currently produce witnesses, including the 4×4 boards. Permanent walls remain sealed during these searches.

Five confirmed content blockers were corrected:

| Asset | Previous blocker | Correction |
| --- | --- | --- |
| `puzzle_anger_p4` | Surplus 2, minimum decay event removes 3 | First tile 9 → 10 |
| `puzzle_ego_p4` | Surplus 2, minimum decay event removes 3 | First tile 8 → 9 |
| `puzzle_greed_p4` | Surplus 2, minimum decay event removes 3 | Seventh tile 7 → 8 |
| `puzzle_lust_p4` | Surplus 1, every first move removes at least 3 | First tile 8 → 10 |
| `puzzle_gluttony_p2` | Surplus 3, every first move removes at least 4 | Ninth tile 8 → 9; instability threshold 2 → GDD baseline 3 |

The production changes were reviewed in five passes: tile boundaries and malformed inputs; move/traversal and decay rules; carry, undo and reset lifecycle; guard/save/resume and completion flow; authored content solvability and final diff. An independent agent also reviewed the production changes.

Unity follow-up remains required: import/compile with Unity 6000.3.7f1; open one puzzle overlay, take and cancel Tide, place and undo using Z, reset using T, close/reopen, save/reload; fight a locked-tile guard and verify previous grid progress returns with the tile unlocked; solve a board and confirm one restoration reward and clean dismissal. Repeat with touch/controller input and inspect tile animation and label state.
