## What to build

Implement the good/bittersweet ending flow from GDD section 6:

1. Final-boss defeated.
2. Fade out.
3. Party appears on a hill facing a sunset (placeholder scene is acceptable for AFK build).
4. Narrative beat fires via NarrativeBeatDirector: party accepts fate, fades away.
5. Credits roll.

Routing is driven by EndingEvaluator (slice 02).

This is HITL because the cutscene scene composition and copy need design review before merge.

## Acceptance criteria

- [ ] `GameStateManager.OnFinalBossDefeated()` triggers the cutscene flow when EndingEvaluator returns GoodEnding.
- [ ] Cutscene scene is reachable from CombatScene via the existing fade pipeline.
- [ ] NarrativeBeatDirector plays the final acceptance beat and the fade-away beat in order.
- [ ] Credits scroll on screen after fade-away.
- [ ] Cutscene scene and copy approved by design owner before merge.
- [ ] StoryProgressionTest extended to cover the good-ending path.

## Blocked by

- Slice 02 (EndingEvaluator) must exist so GameStateManager can route correctly.