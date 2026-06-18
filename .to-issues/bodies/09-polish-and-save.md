## What to build

Two related end-to-end improvements:

A. **Save persistence.** Flip `GameStateManager.EnablePersistentSaveData` to true and wire PlayerPrefs save/load through `GameStateSerializer` for: party composition, hero XP/level, equipped gear, restoration state per island, unlocked islands.

B. **UI/UX polish sweep.** Audit existing HUDs (IslandRestorationHud, BattleHud, PuzzleHud, PartySetupUI) for layout nits surfaced by playtest. Fix anything that breaks immersion or blocks the slice 1-8 flow.

## Acceptance criteria

- [ ] Save persists across play sessions for the fields above.
- [ ] Load restores party, gear, restoration, and unlocks on next session start.
- [ ] HUD audit done; documented fixes merged.
- [ ] No NRE or layout regressions in playtest.

## Blocked by

- Slice 01 (boat travel) - save data must cover cross-island state.
- Slice 02 (EndingEvaluator) - save data must cover loss-count for bad-ending rule.
- Slice 03 (Good ending cutscene) - HUD polish covers the cutscene HUD.
- Slice 06 (Narrative acts) - save data must cover discovered ancient texts.