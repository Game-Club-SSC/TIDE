# Puzzle / Tide / Dialogue System Bug Report

**Scope:** TideManager.cs, TideTile.cs, TideBreakAbility/Catalog/Data/ProgressionManager/UnlockUI.cs, AncientText*.cs, ExpandedAncientTexts.cs, PuzzleHud.cs, NarrativeBeatDirector.cs, NarratorDirector.cs, DialogueTree.cs, DialogueTreeRunner.cs, DialogueUI.cs, DialogueTrigger.cs, AcceptanceConversation.cs

---

## BUG #1 — Duplicate Ancient Text IDs Silently Lose 3 Authored Texts

- **File:** `/Users/andrian/GitHub/TIDE/Assets/AncientTextAuthoring.cs`
- **Lines:** 112, 117 (and corresponding act2/act3 pairs at 113/118, 114/119)
- **Category:** Data Corruption / Logic Error
- **Severity:** High
- **Description:** `BuildBaseline()` contains two greed sections with identical `textId` values. The second greed block ("Counted Sands", "The Coin Heart", "Empty Pockets") reuses the same IDs as the first greed block ("The Hunger at Dawn", "Salt and Memory", "Empty Plate"): `text_greed_act1_01`, `text_greed_act2_01`, `text_greed_act3_01`. `BuildMerged()` uses a `HashSet<string>` keyed on `textId`, so only the first entry with each ID is kept — the second three texts are silently discarded.
- **Fix:** Change the second greed block's IDs to `text_greed_act1_02`, `text_greed_act2_02`, `text_greed_act3_02`.
- **Code Snippet:**
```csharp
// Lines 111-114: First greed block
list.Add(MakeText("text_greed_act1_01", "The Hunger at Dawn", ...));
list.Add(MakeText("text_greed_act2_01", "Salt and Memory", ...));
list.Add(MakeText("text_greed_act3_01", "Empty Plate", ...));

// Lines 116-119: Second greed block — SAME IDs!
list.Add(MakeText("text_greed_act1_01", "Counted Sands", ...));   // DUPLICATE
list.Add(MakeText("text_greed_act2_01", "The Coin Heart", ...));   // DUPLICATE
list.Add(MakeText("text_greed_act3_01", "Empty Pockets", ...));    // DUPLICATE
```

---

## BUG #2 — Redundant Identical Double Method Call

- **File:** `/Users/andrian/GitHub/TIDE/Assets/AcceptanceConversation.cs`
- **Line:** 71
- **Category:** Logic Error / Dead Code
- **Severity:** Medium
- **Description:** `PlayAcceptanceConversation()` calls `!CanPlayAcceptanceConversation() && !CanPlayAcceptanceConversation()` — the exact same method ANDed with itself. This is semantically identical to a single call. The inner `HasMetPrerequisites()` check that follows is dead code because `CanPlayAcceptanceConversation()` already checks the same conditions.
- **Fix:** Replace with a single `if (!CanPlayAcceptanceConversation())` and remove the dead inner branch.
- **Code Snippet:**
```csharp
if (!CanPlayAcceptanceConversation() && !CanPlayAcceptanceConversation())
{
    if (!HasMetPrerequisites())
    {
        return false;
    }
}
```

---

## BUG #3 — `CollectNodesRecursive` Fallback Can Never Discover Missing Nodes

- **File:** `/Users/andrian/GitHub/TIDE/Assets/DialogueTreeRunner.cs`
- **Lines:** 118
- **Category:** Logic Error
- **Severity:** High
- **Description:** The comment at line 96 says "Fallback: traverse from root to catch any nodes not in allNodes", but `CollectNodesRecursive` only recurses into children that are already in `nodeLookup` (line 118 uses `TryGetValue`). If a choice references a node not in `tree.allNodes`, `TryGetValue` returns false and the missing node is never discovered. When the player later selects that choice, the lookup at line 438 fails and the tree silently completes early.
- **Fix:** When `TryGetValue` fails, search `tree.allNodes` directly for the missing node and add it to the lookup.
- **Code Snippet:**
```csharp
if (!string.IsNullOrEmpty(nextId) && nodeLookup.TryGetValue(nextId, out DialogueTreeNode next))
{
    CollectNodesRecursive(next);
}
```

---

## BUG #4 — Coroutine Color Fight Between Corruption Transition and Flash

- **File:** `/Users/andrian/GitHub/TIDE/Assets/TideTile.cs`
- **Lines:** 96-98, 265-272
- **Category:** Coroutine Cancellation / Visual Bug
- **Severity:** Medium
- **Description:** `ApplyDecay()` calls `StartCorruptionTransition()` and then `StartFlash(FlashDecay())`. Both start independent coroutines that write to `cachedMaterial.color`. The two coroutines fight over `cachedMaterial.color`, causing visual flicker/glitching. When the flash ends, `RefreshVisuals()` resets to the base color, but the transition coroutine may still be running and overwrites it again.
- **Fix:** Stop any active transition coroutine before starting the flash in `ApplyDecay`.
- **Code Snippet:**
```csharp
public void ApplyDecay(int decay)
{
    ...
    StartCorruptionTransition();   // Starts coroutine A writing to material.color
    StartFlash(FlashDecay());       // Starts coroutine B writing to material.color
}
```

---

## BUG #5 — `InitializePuzzle` Leaves Board in Partial State on Invalid Sealed Tile

- **File:** `/Users/andrian/GitHub/TIDE/Assets/TideManager.cs`
- **Lines:** 165-179
- **Category:** Logic Error / Missing Validation Order
- **Severity:** Medium
- **Description:** `InitializePuzzle(int[,] layout, Vector2Int sealedTile)` first resets all board state (clears `sealedTiles` to all false, sets `lockedPosition` to (-1,-1), nullifies `lockedEncounterId`), populates `puzzleValues`, then validates `sealedTile`. If the sealed tile position is out of bounds, the method returns early — but the board is already reconfigured without any sealed tile.
- **Fix:** Validate `sealedTile` bounds before mutating board state.
- **Code Snippet:**
```csharp
sealedTiles = new bool[gridRows, gridCols];     // all false
sealedPosition = new Vector2Int(-1, -1);
// ... state mutation ...

if (sealedTile.x < 0 || sealedTile.y < 0 || sealedTile.x >= gridCols || sealedTile.y >= gridRows)
{
    return;   // Board left WITHOUT sealed tile
}
```

---

## BUG #6 — Integer Division Causes Skewed Act Distribution

- **File:** `/Users/andrian/GitHub/TIDE/Assets/AncientTextDiscoverable.cs`
- **Line:** 185
- **Category:** Logic Error / Off-by-One
- **Severity:** Low
- **Description:** `DetermineActForIsland` computes `midpoint / 2` (integer division). With 7 islands: `midpoint = 3`, `midpoint / 2 = 1`. This assigns only indices 0 and 1 to ActI, indices 2-5 to ActII, and index 6 to ActIII — a 2/4/1 split. The `/ 2` halves the ActI boundary.
- **Fix:** Likely intended as just `midpoint` (giving 3/3/1), not `midpoint / 2`.
- **Code Snippet:**
```csharp
if (i <= midpoint / 2)      // integer division: 3/2 = 1
{
    return NarrativeAct.ActI;
}
```

---

## BUG #7 — Unreachable Code Path in Sealed Tile Marker

- **File:** `/Users/andrian/GitHub/TIDE/Assets/TideManager.cs`
- **Lines:** 662-672
- **Category:** Logic Error / Design Redundancy
- **Severity:** Low
- **Description:** `CreateSealedTileEnemyMarker` has `if (renderBoardAsUi) { return; }` as the first check, but this method is only called from `GenerateBoard()` and `TrySetSealedTile` — both of which are never reached in UI mode because `GenerateBoardUi()` takes a separate path.
- **Fix:** The guard is safe but the call sites are confusing. Add a comment or re-evaluate if needed.
- **Code Snippet:**
```csharp
private void CreateSealedTileEnemyMarker(Transform tileTransform)
{
    if (renderBoardAsUi) return;  // Always true in UI mode, but method's callers never reach UI mode
    ...
}
```

---

## BUG #8 — Inspector Public Fields Become Stale After Configure

- **File:** `/Users/andrian/GitHub/TIDE/Assets/TideTile.cs`
- **Lines:** 6-18, 50-58
- **Category:** Property Validation / State Desync
- **Severity:** Low
- **Description:** `TideTile` exposes `public int currentTideValue` and `public bool isSealed` with `[Range]` and `[Tooltip]` attributes for inspector editing, but `Configure()` sets them at runtime via code. After `Configure`, the inspector values become dead data at runtime since `Awake()` doesn't re-read them.
- **Fix:** Minor design issue; consider `[HideInInspector]` or clearing the field attributes.
