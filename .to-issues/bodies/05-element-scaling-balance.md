## What to build

Tune hero and enemy stat scaling so encounters are winnable but not trivial, and the elemental-advantage system feels meaningful.

Scope:
- Hero stat growth per level (LevelingConfig) tuned so 1-2 levels per island feels right.
- Enemy stats per EnemyData tuned so non-advantage fights are tense, advantage fights are comfortable.
- Boss stats tuned so they survive 3-4 Tide Break combos.

This is HITL because balance numbers need a human playtest pass before merge.

## Acceptance criteria

- [ ] LevelingConfig stat growth curves updated.
- [ ] EnemyData stat ranges updated for each vice island.
- [ ] Boss stat presets updated.
- [ ] HeroProgressionTest + Greed/Sloth/Envy/Pride/Wrath verification suites still pass.
- [ ] Playtest notes from design owner attached to the issue before merge.
- [ ] No encounter is unwinnable from level 1 with the starting element.

## Blocked by

- Slice 04 (elemental enemy AI) must land first so we are not retuning stale AI behavior.