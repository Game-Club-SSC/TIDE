# State / Save / Progression System Bug Report

**Scope:** GameStateManager.cs, GameStateSerializer.cs, WorldSaveService.cs, IslandRestorationTracker/State/Hud.cs, IslandProgressionManager.cs, IslandBacktrackingManager.cs, IslandFlowController.cs, IslandConfig.cs, IslandBoatInteractable.cs, IslandArtResolver.cs, IslandVisualProfile.cs, IslandEnemyVisualProfile.cs, PerIslandContentRegistry.cs, StoryProgressionService.cs, HeroProgressionManager.cs, GearSetFactory/Data.cs, GearBonusStatType.cs, RelationshipTracker.cs, PartyManager/Data/SwapService.cs, EndingEvaluator.cs, NewGamePlusService.cs, DevCheatService.cs, DevModeController.cs, DifficultyModeService.cs, BossEncounterGate.cs, TravelValidationService.cs, PowerBudgetTracker.cs, PerformanceBudgetMonitor.cs, LevelingConfig.cs, GameConstants.cs, BalanceConfig.cs, HeroData.cs, HeroCharacterData.cs, BossCharacterData.cs, ElementalCharacterFactory.cs, HeroTideBreakFactory.cs

---

## BUG #1 — Relationship Tier Changed Event Never Fires

- **File:** `/Users/andrian/GitHub/TIDE/Assets/RelationshipTracker.cs`
- **Line:** 67
- **Category:** Logic Error (Inverted Variable)
- **Severity:** High
- **Description:** `SetAffinity()` computes `previousTier` AFTER clamping to the new value instead of from the old affinity value. This means `previousTier` always equals `newTier`, so `OnTierChanged` never fires.
- **Fix:** Read the old affinity value before assigning the new one.
- **Code Snippet:**
```csharp
int clamped = Mathf.Clamp(value, 0, 100);
RelationshipTier previousTier = GetRelationshipTier(clamped);  // ← USES NEW VALUE
affinityByHeroId[heroId] = clamped;
RelationshipTier newTier = GetRelationshipTier(clamped);
// previousTier == newTier ALWAYS → OnTierChanged never fires
```

---

## BUG #2 — EndingEvaluator Singleton Pattern Broken

- **File:** `/Users/andrian/GitHub/TIDE/Assets/EndingEvaluator.cs`
- **Lines:** 48-52
- **Category:** Singleton Issue
- **Severity:** High
- **Description:** On duplicate detection, calls `DestroyImmediate(this)` instead of `DestroyImmediate(gameObject)`, leaving an orphaned empty GameObject. Also missing `DontDestroyOnLoad()` — every other singleton in the project calls it. The `EndingEvaluator` singleton may be lost on scene load.
- **Fix:** Use `DestroyImmediate(gameObject)` and add `DontDestroyOnLoad(gameObject)`.
- **Code Snippet:**
```csharp
if (Instance != null && Instance != this)
{
    DestroyImmediate(this);   // ← only destroys component, leaves GameObject
    return;
}
Instance = this;
// ← no DontDestroyOnLoad
```

---

## BUG #3 — GetBacktrackingNarrative Ignores Its Parameter

- **File:** `/Users/andrian/GitHub/TIDE/Assets/IslandBacktrackingManager.cs`
- **Lines:** 217-262
- **Category:** Logic Error (Dead Parameter)
- **Severity:** Medium
- **Description:** `GetBacktrackingNarrative(string currentIslandId)` resolves `currentIslandId` into `resolvedId` but never uses `resolvedId` afterwards. The method unconditionally returns the narrative from the highest-progression-index unlock that's been processed, regardless of which island the caller asked about. All islands always return the same narrative string.
- **Fix:** Filter the applicable unlocks by `resolvedId` before selecting the best narrative.
- **Code Snippet:**
```csharp
string resolvedId = IslandThemeRegistry.ResolveIslandId(currentIslandId);
// resolvedId is NEVER USED below
string bestNarrative = null;
int highestIndex = -1;
for (int i = 0; i < backtrackingUnlocks.Length; i++)
{
    // picks highest-index unlock regardless of island
}
```

---

## BUG #4 — Off-by-One in Cleared-Encounter Skip

- **File:** `/Users/andrian/GitHub/TIDE/Assets/IslandFlowController.cs`
- **Lines:** 207-214
- **Category:** Logic Error
- **Severity:** Medium
- **Description:** The `while` loop that skips already-cleared encounters uses `currentEncounterIndex < islandConfig.encounters.Length - 1` as its guard. If the last encounter was already cleared, the loop exits at `Length - 1` without advancing, and the code re-loads the already-cleared final encounter each time the flow restarts on a fully-cleared island.
- **Fix:** Change the guard to `currentEncounterIndex < islandConfig.encounters.Length` and add a post-loop check to advance past the last index when all encounters are cleared.
- **Code Snippet:**
```csharp
while (tracker != null && tracker.HasClearedEncounter(activeIslandId, encounterId)
    && currentEncounterIndex < islandConfig.encounters.Length - 1)  // ← blocks last-index skip
{
    currentEncounterIndex++;
    ...
}
```

---

## BUG #5 — WorldSaveService Unreachable Retry Loop

- **File:** `/Users/andrian/GitHub/TIDE/Assets/WorldSaveService.cs`
- **Lines:** 56-68
- **Category:** Logic Error
- **Severity:** Medium
- **Description:** `TryWriteJsonInternal()` can only return false when JSON is null/empty, but callers guard against that and `InjectSchemaVersion()` always returns non-null. The retry loop always exits on iteration 0, making the full retry + backup-restore dead code.
- **Fix:** Have `TryWriteJsonInternal` validate the write result (e.g., re-read and compare).
- **Code Snippet:**
```csharp
for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
{
    if (TryWriteJsonInternal(versionedJson))   // ← Always returns true
        return true;
    // This warning is never logged
}
// Backup restore code is unreachable
```

---

## BUG #6 — PartySwapPanel Double-Destroy of Children

- **File:** `/Users/andrian/GitHub/TIDE/Assets/PartySwapPanel.cs`
- **Lines:** 141-152
- **Category:** Logic Error (Double-Destroy)
- **Severity:** Medium
- **Description:** In `ClearButtons()`, the first `foreach` destroys all Button gameObjects in `buttonList`. The second `foreach` iterates `container`'s children and calls `Destroy()` on them again. Unity's `Destroy()` is deferred, so the second loop redundantly marks already-pending objects, generating warnings.
- **Fix:** Remove the second `foreach` loop — the first already handles cleanup.
- **Code Snippet:**
```csharp
foreach (Button btn in buttonList)
{
    if (btn != null)
        Destroy(btn.gameObject);
}
buttonList.Clear();
foreach (Transform child in container)   // ← Destroys children AGAIN
{
    Destroy(child.gameObject);
}
```

---

## BUG #7 — IslandBoatInteractable Destination Sort Desyncs SelectedIndex

- **File:** `/Users/andrian/GitHub/TIDE/Assets/IslandBoatInteractable.cs`
- **Lines:** 429-444
- **Category:** Logic Error
- **Severity:** Medium
- **Description:** In `EnsureDestinationList()`, `destinations.Sort(...)` reorders the list but `selectedIndex` is not re-bounded. Only `OpenTravelPanel()` calls `RefreshDestinationOrder()` afterwards, so any mid-panel call to `EnsureDestinationList()` leaves `selectedIndex` pointing to the wrong entry.
- **Fix:** Call `RefreshDestinationOrder()` at the end of `EnsureDestinationList()`.
- **Code Snippet:**
```csharp
destinations.Sort(...);   // ← sorts, shifting indices
// selectedIndex is NOT re-bounded here
```

---

## BUG #8 — GameStateManager Save/Load Ordering Bug

- **File:** `/Users/andrian/GitHub/TIDE/Assets/GameStateManager.cs`
- **Lines:** 267-274
- **Category:** Save/Load Ordering
- **Severity:** Medium
- **Description:** In `Awake()`, `LoadWorldState()` runs AFTER `IslandProgressionManager.Instance?.ReconcileStateFromRestoration()`. If `LoadWorldState` applies a progression snapshot that changes `activeIslandId`, the `ReconcileStateFromRestoration` call already bound event handlers and made state decisions based on stale data.
- **Fix:** Swap the order — call `LoadWorldState()` before `ReconcileStateFromRestoration()`.
- **Code Snippet:**
```csharp
IslandProgressionManager.Instance?.ReconcileStateFromRestoration();  // runs first with stale data
LoadWorldState();                                                      // then overwrites
```

---

## BUG #9 — LegacyRuntime.ttf Null Guards Missing

- **File:** Multiple files: `PartySetupUI.cs`, `PartySwapPanel.cs`, `IslandRestorationHud.cs`, `DevMenuUI.cs`
- **Category:** Portability / Null Reference
- **Severity:** Low
- **Description:** `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` returns null on some platforms (console, WebGL). Only `IslandBoatInteractable.cs` null-guards the result; the other files assign the potentially-null font to `Text`/`TMP_Text.font` without checking, causing silent missing text.
- **Fix:** Add null checks and fallback to Font.CreateDynamicFontFromOSFont or a bundled TTF.
- **Files:** `PartySetupUI.cs` ~245, `PartySwapPanel.cs` ~97, `IslandRestorationHud.cs` ~165, `DevMenuUI.cs` ~274

---

## BUG #10 — RelationshipTracker Data Not Persisted

- **File:** `/Users/andrian/GitHub/TIDE/Assets/RelationshipTracker.cs`
- **Lines:** 15-19, 133-151
- **Category:** Data Loss
- **Severity:** Medium
- **Description:** `affinityByHeroId` is a `Dictionary<string, int>` lived purely in memory. `OnDisable()` dumps to `PlayerPrefs` in JSON but `Awake()` only calls `InitializeFromDefaults()`, not `RestoreAffinityFromPrefs()`. The method exists but is never wired into the lifecycle — relationships reset to defaults every session.
- **Fix:** Call `RestoreAffinityFromPrefs()` in `Awake()` after `InitializeFromDefaults()`.
- **Code Snippet:**
```csharp
private void Awake()
{
    InitializeFromDefaults();
    // RestoreAffinityFromPrefs() never called
}
```
