# Spike VS6 — Envy Island Copy/Mirror Mechanics (Issue #200)

Status: Research complete — ready for VS6 implementation.

## Goal
Define the Envy "copy/mirror" combat fantasy (enemies that covet and steal the
party's strengths) and the minimal system support required.

## Findings (current code)
- `CombatUnit.Element` (Fire, Water, Earth, Air, Space, None) drives the
  Rock-Paper-Scissors clash; mirroring an element is the natural Envy hook.
- `SkillData` is data-only (name, multiplier, lifesteal, target, one status).
  Enemies hold a `SkillData[]`; there is no "copy last skill" behaviour yet.
- Elemental clash logic already exists (`ElementMatchup`), so a mirrored element
  immediately neutralises the player's advantage — strong, readable design.

## Proposed design — "Mirror" enemy behaviour
1. Element Mirror (no new data type): an Envy enemy AI option `mirrorTargetElement`
   that, on spawn or on first hit taken, copies the attacker's `Element`, negating
   the player's matchup edge for `N` turns.
2. Skill Covet: a boss ability that copies the *last skill the player cast this
   battle* (store `lastPlayerSkill` on `BattleManager`) and replays it at reduced
   `damageMultiplier` (e.g. 0.75x). Reuses existing `SkillData` resolution.

Both reuse existing resolution paths; only AI/selection logic is new.

## Implementation plan
- `BattleManager`: track `SkillData lastPlayerSkill` and `CombatUnit lastAttacker`.
- New `EnvyMirrorBehaviour` (MonoBehaviour or AI hook on the enemy) that:
  - copies `lastAttacker.Element` into its own element for a duration, and
  - for the boss, selects `lastPlayerSkill` as its action when available.
- Author on existing `envy_*` enemies / `enemy_envy_*`; no asset schema change.

## Balancing
- Mirror duration 2–3 turns; covet replay at 0.6–0.8x to avoid burst spikes.
- Boss covets at most once every 3 turns to keep counterplay readable.

## Acceptance criteria
- Mirrored element flips the clash result as expected in a manual battle.
- Covet never crashes when no player skill has been cast (null-guarded).
- `EnvyIslandVerificationTest` continues to pass.
