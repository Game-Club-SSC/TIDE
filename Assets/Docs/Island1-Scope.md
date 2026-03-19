# Island 1 — Vertical Slice Scope

## Vice Theme: Greed

Island 1 is ruled by the embodiment of **Greed** — an insatiable hunger for treasure and wealth that has corrupted the island's Tide. The environment features gold and amber tones, and enemies are themed around hoarding and avarice.

## Structure: 5 Subsections

Each island contains **5 subsections**. Each subsection has one combat zone and one puzzle grid.

**Restoration Split: 50/50**
- Combat: 50% total (10% per subsection × 5)
- Puzzle: 50% total (10% per subsection × 5)
- Each subsection = 20% (10% combat + 10% puzzle)
- **Boss unlocks at 75% restoration** (after completing ~4 subsections)

## Encounter Sequence (10 total)

| # | Subsection | Type | Restoration | Cumulative |
|---|-----------|------|-------------|------------|
| 1 | 1 | Combat | +10% | 10% |
| 2 | 1 | Puzzle | +10% | 20% |
| 3 | 2 | Combat | +10% | 30% |
| 4 | 2 | Puzzle | +10% | 40% |
| 5 | 3 | Combat | +10% | 50% |
| 6 | 3 | Puzzle | +10% | 60% |
| 7 | 4 | Combat | +10% | 70% |
| 8 | 4 | Puzzle | +10% | 80% (boss unlocked) |
| 9 | 5 | Combat | +10% | 90% |
| 10 | 5 | Puzzle | +10% | 100% |

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
- [ ] 5 combat subsections playable with distinct enemy compositions
- [ ] 5 puzzle subsections playable with distinct grid layouts
- [ ] Each combat encounter contributes 10% to island restoration
- [ ] Each puzzle encounter contributes 10% to island restoration
- [ ] Island restoration reaches 100% after all 10 encounters cleared
- [ ] Boss unlocks at 75% restoration (after ~8 encounters)
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
