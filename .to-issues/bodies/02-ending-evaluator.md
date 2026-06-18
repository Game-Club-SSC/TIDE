## What to build

Codify the bad-ending rules from GDD section 6 into an `EndingEvaluator` service that `GameStateManager` consults at final-boss resolution.

Trigger conditions to encode:
- Final boss defeats the player more than three times across attempts.
- OR the player cleared only the minimum (currently 75%) of corruption before proceeding on each island.

Need to confirm with the design owner whether the per-island minimum stays at 75% or is tightened (this slice is HITL for that confirmation).

## Acceptance criteria

- [ ] `EndingEvaluator` service exists with a single `EvaluateOutcome(GameStateSnapshot)` returning `GoodEnding | BadEnding`.
- [ ] Loss-count rule increments on final-boss defeat and persists within the run.
- [ ] Minimum-restoration rule sums per-island cleared-restoration against a configurable threshold.
- [ ] GameStateManager calls EndingEvaluator on final-boss resolution and routes to the matching ending flow.
- [ ] Unit test suite for EndingEvaluator covers both rule paths.
- [ ] Threshold value confirmed by design owner before merge.

## Blocked by

None - can start immediately.