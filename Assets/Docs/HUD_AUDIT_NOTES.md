# HUD Audit Notes (Issue #225)

This document captures the audit findings for the four core HUDs called out in
issue #225 (UI/UX polish + save persistence pass).

## IslandRestorationHud
- The HUD already renders restoration percentage via `IslandRestorationHud`.
  Audit confirms it does not crash when the tracker reports > 100% (clamps to 100)
  or when no island is active.
- Recommendation: keep restoration text formatted to one decimal place to
  prevent jitter during incremental updates.

## BattleHud
- BattleHud already subscribes to `OnDamageDealt` and updates HP bars.
- New audit item: when an actor is defeated mid-turn, the HUD now updates via
  the existing `OnUnitDefeated` callback chain. No NRE observed.

## PuzzleHud
- PuzzleHud uses `LegacyRuntime.ttf` for label rendering; verified no references
  to `Arial.ttf` remain.
- Recommendation: keep prompt placement above the puzzle box to avoid overlap
  with the IslandRestorationHud when both are visible on the same scene.

## PartySetupUI
- PartySetupUI swaps active/reserve slots using `PartyData.SwapActiveReserve`.
- Audit confirms hero IDs round-trip through save/load (issue #225 acceptance).
- Recommendation: highlight the main character slot with a distinct color.

## Save Persistence (issue #225 acceptance)
- `enablePersistentSaveData` defaults to **true** in `GameStateManager`.
- `WorldStateSaveData` now also captures:
  - `HeroProgressionSnapshot` (per-hero level + current XP)
  - `PartyCompositionSnapshot` (active + reserve hero IDs)
- `LoadWorldState` applies both new snapshots after the existing story +
  progression + restoration + gear snapshots.
- Legacy save JSON (without the new fields) still deserializes cleanly because
  `JsonUtility` initializes new fields to null.

## Verification
- `Assets/SavePersistenceTest.cs` adds four context-menu tests covering party
  composition round-trip, hero progression round-trip, persistent-save default,
  and legacy schema compatibility.
