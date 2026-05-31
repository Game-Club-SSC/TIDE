# Spike VS4 — Sloth Island Status Effect System (Issue #199)

Status: Design complete — ready for VS4 implementation.

## Goal
Design the Sloth-themed status effect set and the system changes needed to support
it, building on the existing `StatusEffect` / `SkillData` plumbing.

## Findings (current code)
- `StatusEffectType` enum: `None, BuffAttack, BuffDefense, DebuffAttack,
  DebuffDefense, Poison`. No speed/turn-denial effects exist yet.
- `StatusEffect` carries `Type, Duration, Magnitude, SourceName` and `Tick()`
  decrements duration each turn — sufficient for time-based effects.
- `SkillData` already supports applying one effect via `appliedEffectType`,
  `effectDuration`, `effectMagnitude`. No code change needed to *apply* a new type.

## Proposed design — Sloth = tempo denial
Add two `StatusEffectType` values and handle them where the turn queue / speed is
resolved (`BattleManager` turn ordering, `CombatUnit` speed):
1. `Slow` — reduces effective speed by `Magnitude` (0–1 fraction) for `Duration`
   turns. Hooks into BattleManager turn-order speed read.
2. `Drowsy` (skip) — `Magnitude` = probability [0,1] the afflicted unit forfeits
   its action that turn; rolled at turn start.

Sloth enemies/boss lean on `Slow`/`Drowsy`; the player counters with cleanse
skills (`SkillTarget.SingleAlly`, already supported) or by winning fast.

## Implementation plan
- Extend `StatusEffectType` enum (additive; existing values keep their order so
  serialized `SkillData` assets are unaffected).
- `CombatUnit`: apply `Slow` as a multiplier in the speed getter used for turn
  order; expose `ShouldSkipTurn()` reading active `Drowsy` effects.
- `BattleManager`: at turn start, if `ShouldSkipTurn()` true, log and advance.
- Author Sloth skills on existing `desire_*` enemies setting `appliedEffectType`.

## Balancing
- `Slow` magnitude 0.3–0.5, duration 2–3 turns. `Drowsy` probability 0.2–0.35.
- Boss (`desire_boss`) applies stacking-capped `Slow` (no permanent lock).

## Acceptance criteria
- New enum values are additive; existing status tests still pass.
- A `Slow`-afflicted unit moves later in the turn order; `Drowsy` can skip a turn.
- `SlothIslandVerificationTest` continues to pass.
