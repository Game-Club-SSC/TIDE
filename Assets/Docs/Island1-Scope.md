# Island 1 — Vertical Slice Scope

## Vice Theme: Greed

Island 1 is ruled by the embodiment of **Greed** — an insatiable hunger for treasure and wealth that has corrupted the island's Tide. The environment features gold and amber tones, and enemies are themed around hoarding and avarice.

## Encounter Sequence

| # | Type | Restoration | Description |
|---|------|-------------|-------------|
| 1 | Combat A | +20% | First encounter with Greed's minions |
| 2 | Puzzle A | +30% | Balance a corrupted Tide grid (sealed center) |
| 3 | Combat B | +20% | Stronger enemies guarding deeper corruption |
| 4 | Puzzle B | +30% | Balance a second corrupted grid (sealed corner) |

**Total restoration: 100%**

## Enemy Compositions

### Combat A (Standard)
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Gold Imp | Fire | Base |
| 2 | Greedy Goblin | Water | Defense +2 |
| 3 | Treasure Mimic | Earth | MaxHP +5 |

### Combat B (Mini-Boss)
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Hoarding Drake | Fire | Attack +10, MaxHP +20 |
| 2 | Merchant Ghost | Water | Base |
| 3 | Vault Golem | Earth | Defense +5 |

## Puzzle Layouts

### Puzzle A (sealed center)
```
9  1  10
7  X  2
5  3  3
```
Sealed tile: (row=1, col=1)

### Puzzle B (sealed top-left corner)
```
X  2  6
10 5  1
3  4  7
```
Sealed tile: (row=0, col=0)

## Color Palette

| Use | Color | Hex |
|-----|-------|-----|
| Primary (Greed) | Gold | #FFD700 |
| Secondary | Dark Amber | #8B6914 |
| Corruption | Green overlay | #8FBC8F @ 30% |

## Definition of Done

- [ ] Vice theme (Greed) reflected in enemy names/stats and environment palette
- [ ] 2 combat zones playable with distinct enemy compositions
- [ ] 2 puzzle subsections playable with distinct grid layouts
- [ ] Each combat zone contributes 20% to island restoration
- [ ] Each puzzle subsection contributes 30% to island restoration
- [ ] Island restoration reaches 100% after all 4 encounters cleared
- [ ] Player traverses island entrance → all zones → restored endpoint in one playthrough
- [ ] No critical bugs (softlocks, crashes, broken scene transitions)
- [ ] All existing NUnit tests pass

## Implementation Files

| File | Purpose |
|------|---------|
| `IslandConfig.cs` | ScriptableObject defining island encounters |
| `IslandRestorationTracker.cs` | Tracks restoration progress 0→100% |
| `IslandFlowController.cs` | Sequences encounters and manages transitions |
| `GameStateManager.cs` | Modified: stores pending puzzle layout and enemy composition |
| `TideManager.cs` | Modified: accepts parameterized grid layouts |
| `CombatSceneBootstrap.cs` | Modified: accepts enemy composition config |
| `PuzzleBoxInteractable.cs` | Modified: passes layout config to GameStateManager |
| `PuzzleSceneBootstrap.cs` | Modified: reads pending layout from GameStateManager |
