# Spike VS3 — Greed Island Economy Mechanics (Issue #198)

Status: Research complete — ready for VS3 implementation.

## Goal
Define the Greed-specific puzzle/economy mechanic, gold drop rates, and reward
systems so VS3 has a concrete, balanced target.

## Findings (current code)
- `PuzzleData` already supports a per-island signature modifier: Gluttony uses
  `enableConsumption` + `consumptionAmount` (tide removed after a valid placement).
  This is the precedent for a Greed economy tile — a flag + scalar on `PuzzleData`.
- `IslandConfig.encounters` for `island_greed` already total 100% restoration
  (4 combat @ 0.125, 4 puzzle @ 0.0625, boss @ 0.25). Economy mechanic must not
  change restoration weighting, only the moment-to-moment puzzle/combat feel.
- `EnemyData.xpReward` is the only existing drop field; there is no gold field yet.

## Proposed design — "Greed Coin" tiles
1. Add to `PuzzleData`:
   - `bool enableGreedEconomy`
   - `int coinTileYield = 2` (tide value granted to an adjacent tile when a coin
     tile reaches the target, simulating "hoarding" overflow).
   The mechanic mirrors Gluttony consumption but *adds* rather than removes, so
   Greed puzzles reward over-balancing and punish greed (overflow → instability
   via the existing `instabilityThreshold`).
2. Reuse `puzzle_greed_p1..p4` (already authored) — only set the new flags in-Editor.

## Gold / reward system (new, minimal)
- Add `int goldReward` to `EnemyData` (default 0; Greed enemies 40–80, boss 250).
- Track party gold in `GameStateManager` (new `int partyGold`) and persist it in
  the existing save payload alongside restoration state.
- Greed Smithy (existing `SmithyInteractable`) consumes gold for gear duplication.

## Balancing (level 15–20 party)
- Standard Greed enemy gold: 40–80 (avg 60) → ~1,800 across 4 combats + adds.
- Boss `enemy_greed_boss` gold: 250; "gold steal" boss ability (see #170) drains
  10% party gold per cast to reinforce the vice fantasy.

## Acceptance criteria
- `PuzzleData` exposes the new fields; existing puzzles compile unchanged.
- Gold persists across save/load; Smithy spends it.
- `GreedIslandVerificationTest` continues to pass (restoration budget unchanged).
