## What to build

Wire the existing IslandBoatInteractable to IslandProgressionManager and GameStateManager so the player can ride a boat from one island to the next.

End-to-end behavior:
1. Player interacts with IslandBoatInteractable on a fully-restored island.
2. GameStateManager validates the destination island via IslandProgressionManager.GetUnlockedIslandIds().
3. Player picks a destination from an unlocked-island list UI.
4. Fade out -> load target island's level_1 snapshot via IslandProgressionManager -> IslandRestorationTracker loads per-island state.
5. Player spawns at the boat dock on the destination island.

## Acceptance criteria

- [ ] Player can board boat on any restored island and see a destination picker of unlocked islands
- [ ] Destination picker only shows islands unlocked per IslandProgressionManager
- [ ] Fade transition runs through GameStateManager (no scene-skip bypasses)
- [ ] Player spawns at the destination island's dock
- [ ] Per-island restoration state is preserved across the transition
- [ ] IslandProgressionTravelTest extended to cover the boat travel path

## Blocked by

None - can start immediately.