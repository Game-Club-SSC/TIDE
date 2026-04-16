# Island 1 — Vertical Slice Scope

## Vice Theme: Greed

Island 1 is ruled by the embodiment of **Greed** — an insatiable hunger for treasure and wealth that has corrupted the island's Tide. The environment features gold and amber tones, and enemies are themed around hoarding and avarice.

## Structure: Current Vertical Slice Runtime Model

The current vertical slice runtime uses **4 pre-boss combat encounters, 4 pre-boss puzzle encounters, and 1 boss encounter** per island.

**Restoration Split: 50 / 25 / 25**
- Combat: 50% total (12.5% × 4)
- Puzzle: 25% total (6.25% × 4)
- Boss: 25% total
- **Boss unlocks at 75% restoration** after all pre-boss encounters are completed

## Encounter Sequence (9 total)

| # | Subsection | Type | Restoration | Cumulative |
|---|-----------|------|-------------|------------|
| 1 | 1 | Combat | +12.5% | 12.5% |
| 2 | 1 | Puzzle | +6.25% | 18.75% |
| 3 | 2 | Combat | +12.5% | 31.25% |
| 4 | 2 | Puzzle | +6.25% | 37.5% |
| 5 | 3 | Combat | +12.5% | 50% |
| 6 | 3 | Puzzle | +6.25% | 56.25% |
| 7 | 4 | Combat | +12.5% | 68.75% |
| 8 | 4 | Puzzle | +6.25% | 75% (boss unlocked) |
| 9 | Boss | Combat | +25% | 100% |

## Element System

Five elemental affinities (rock-paper-scissors):
- **Fire** beats Earth, Air | loses to Water, Space
- **Water** beats Fire, Space | loses to Earth, Air
- **Earth** beats Water, Space | loses to Fire, Air
- **Air** beats Earth, Water | loses to Fire, Space
- **Space** beats Fire, Air | loses to Water, Earth

## Enemy Compositions (per subsection)

### Subsection 1 — Greed's Outer Reaches
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Gold Imp | Fire | Base |
| 2 | Greedy Goblin | Water | Defense +2 |
| 3 | Treasure Mimic | Earth | MaxHP +5 |

### Subsection 2 — Merchant's Quarter
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Avarice Shade | Space | Attack +3 |
| 2 | Debt Wraith | Air | Speed +3 |
| 3 | Hoarding Drake | Fire | MaxHP +15 |

### Subsection 3 — The Vault Depths
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Merchant Ghost | Water | Attack +5 |
| 2 | Vault Golem | Earth | Defense +5 |
| 3 | Gold Elemental | Fire | MaxHP +15 |

### Subsection 4 — Corridors of Want
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Greed Serpent | Space | Attack +8, Speed +2 |
| 2 | Tax Collector | Air | Defense +4 |
| 3 | Gilded Knight | Earth | MaxHP +20, Defense +3 |

### Subsection 5 — The Dragon's Hoard
| Slot | Name | Element | Stats vs Base |
|------|------|---------|---------------|
| 1 | Hoard Dragon | Fire | Attack +12, MaxHP +25 |
| 2 | Fortune Wraith | Space | Attack +8 |
| 3 | Corruption Golem | Earth | Defense +8, MaxHP +10 |

## Puzzle Layouts

All puzzles are 3×3 grids. Goal: balance all non-sealed tiles to value 5.
- **Sealed tiles** (X): impassable, cannot take/place Tide
- **Locked tiles** (L): sealed until combat is cleared, then become normal

### Puzzle 1 — sealed center
```
9  1  10
7  X  2
5  3  3
```
Sealed: (1,1) | Locked: none

### Puzzle 2 — sealed bottom-left
```
6  3  8
4  7  2
X  5  9
```
Sealed: (2,0) | Locked: none

### Puzzle 3 — sealed top-right
```
3  8  X
10 5  4
7  2  6
```
Sealed: (0,2) | Locked: none

### Puzzle 4 — sealed center + locked top-left
```
L  2  8
5  X  6
9  3  7
```
Sealed: (1,1) | Locked: (0,0)

### Puzzle 5 — sealed bottom-right + locked center-left
```
8  4  10
L  X  3
6  7  2
```
Sealed: (1,1) | Locked: (1,0)

## Color Palette

| Use | Color | Hex |
|-----|-------|-----|
| Primary (Greed) | Gold | #FFD700 |
| Secondary | Dark Amber | #8B6914 |
| Corruption | Green overlay | #8FBC8F @ 30% |

## Definition of Done

- [ ] Vice theme (Greed) reflected in enemy names/stats and environment palette
- [ ] 4 pre-boss combat encounters playable with distinct enemy compositions
- [ ] 4 pre-boss puzzle encounters playable with distinct grid layouts
- [ ] Each pre-boss combat encounter contributes 12.5% to island restoration
- [ ] Each pre-boss puzzle encounter contributes 6.25% to island restoration
- [ ] Island restoration reaches 100% after all 9 encounters cleared
- [ ] Boss unlocks at 75% restoration after the 8 pre-boss encounters
- [ ] Player traverses subsections sequentially in one playthrough
- [ ] No critical bugs (softlocks, crashes, broken scene transitions)
- [ ] All existing NUnit tests pass

## Implementation Files

| File | Purpose |
|------|---------|
| `IslandConfig.cs` | ScriptableObject defining island encounters |
| `IslandRestorationTracker.cs` | Tracks restoration progress 0→100% |
| `IslandFlowController.cs` | Sequences encounters and manages transitions |
| `GameStateManager.cs` | Stores pending puzzle layout and enemy composition |
| `TideManager.cs` | Accepts parameterized grid layouts |
| `CombatSceneBootstrap.cs` | Accepts enemy composition config |
| `PuzzleBoxInteractable.cs` | Passes layout config to GameStateManager |
