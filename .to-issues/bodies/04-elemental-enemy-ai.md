## What to build

Give each enemy an element-aware action selection pass on top of the existing combat flow.

Behavior:
- An enemy weighted-picks a skill whose element has advantage against the most-common player element on the field.
- Bosses (and mini-bosses) get a higher-priority tier that always tries an advantageous skill first.
- Decoy/utility skills are still picked when no advantageous skill is available.

Implementation lives inside `BattleManager`/`CombatUnit` action selection, not a separate system.

## Acceptance criteria

- [ ] Each enemy considers player elements when picking an action.
- [ ] Advantageous skills are picked at least 70% of the time when available.
- [ ] Bosses always try an advantageous skill first.
- [ ] Existing combat tests still pass.
- [ ] New BattleFlowTestSuite cases cover element-aware AI for at least one of each element.

## Blocked by

None - can start immediately.