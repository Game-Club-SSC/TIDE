# TIDE — Visual Simulation Walkthrough

Grounded in the actual runtime UI-construction code (colors, layouts, timings read from
`TideManager`, `BattleHud`, `QuestJournalUI`, `PauseMenuUI`, `GameStateManager`,
`CombatSceneBootstrap`, ending controllers). Every screen below is what the game renders
today when played in the Unity Editor, start to finish.

---

## 1. Boot → Title Scene (`TitleScene`)

```
 ┌──────────────────────────────────────────────┐
 │                                              │
 │                  T I D E                     │   ← large title, Persona-style
 │           a story of balance                 │      off-white on deep navy
 │                                              │
 │         [ New Game ]                         │   ← BrightBlue button (PersonaUIStyle)
 │         [ Continue ]     ← dim if no save    │      Continue enabled only when
 │         [ Settings ]                         │      HasLoadableWorldState() = true
 │         [ Quit ]                             │
 │                                              │
 └──────────────────────────────────────────────┘
```

- **New Game** → `ResetWorldStateForNewGame()` wipes PlayerPrefs + runtime state.
- **Continue** → `LoadWorldStateAndRestoreScene()` restores islands/restoration/party/
  gear/dialogue flags, then fades into `level_1` or `HubScene` depending on saved island.
- All transitions use the shared **black fade canvas** (sorting order 1000,
  0.2 s out / 0.2 s in — `FadeDuration`).

---

## 2. Hub Island (`HubScene`) — Chapter One

Top-down camera snapped behind the player (`SnapFollowCameraToPlayer`). Visible props are
runtime-guaranteed by `EnsureHubSceneRuntimeComponents`:

| Object | Look |
|---|---|
| Player (`IsometricPlayer`) | stylized hero sprite/model; MC uses the element chosen at ceremony |
| `SmithyStation` | cube @ (15, 31.5, 2.7), scale (1.4, 1.1, 1.4) |
| `IslandBoat` | blue-cyan cylinder @ (8.5, 31.5, 2.7) — `(0.18, 0.36, 0.52)` |
| Tribe leaders | billboarded NPC prompts (`PuzzlePrompt.png` sprite, floating, camera-facing) |
| `Harbormaster Wren` (hub preset) | interact with **Enter** → dialogue box |

**Ceremony intro** plays once (`CeremonyIntroDirector.PlayCeremonyIntro`) — the
coming-of-age sequence from Chapter Zero, then never again (`ceremonyIntroCompleted`
persists in the save).

**Boat interaction** opens the travel panel listing unlocked islands:
`island_lust → greed → desire → anger → envy → ego`. Locked rows are hidden until the
previous island restores. Travel = 0.2 s fade → reposition → 0.2 s fade-in
(`TravelFadeAndRepositionRoutine`).

---

## 3. Exploration (`level_1`) — Corrupted Vice Island

- Ground tinted by the island's corruption theme; when a puzzle completes the ground
  visibly shifts toward restored color (`OnPuzzleCompleted` → "ground color changed").
- HUD pieces present: island restoration %, quest journal (**J**), pause (**Esc/P**),
  mobile pause button on touch devices.
- **PuzzleGuardSpawner** places guard enemies around locked tiles' world anchors;
  cleared encounters remove their guards (`RefreshGuards`).
- Walking into an overworld enemy triggers `EnterCombatSceneFromExploration(...)` with a
  captured return position + camera transform so you come back exactly where you stood.

---

## 4. Tide Puzzle (overlay or `PuzzleScene`)

The board builds itself every session (`GenerateBoardUi`):

```
 ╔═════════════ TIDE STABILIZATION ═════════════╗
 ║  Goal: stabilize all open tiles to 5         ║   ← header, 24pt bold
 ║        | Carry -    | Esc: Exit Overlay      ║
 ║  ┌────────┬────────┬────────┐                ║
 ║  │   9    │   1    │  10    │                ║   ← 46pt bold values
 ║  ├────────┼────────┼────────┤                ║
 ║  │   7    │   X ←sealed     │  2    │        ║   center tile dark gray "X"
 ║  ├────────┼────────┼────────┤                ║      (combat-guarded)
 ║  │   5    │   3    │   3    │                ║
 ║  └────────┴────────┴────────┘                ║
 ╚══════════════════════════════════════════════╝
   panel: deep navy (lerped toward vice color)  ·  grid: slate blue-grey
```

**Tile color language** (exactly what renders):
| Value | Color |
|---|---|
| 5 (balanced) | soft green `(0.58, 0.85, 0.66)` |
| 6–10 (light excess) | warm sand → blinding white as it climbs |
| 1–4 (dark excess) | cold blue → pure black at 1 |
| Sealed | charcoal gray, label "X" |
| Selected source | golden `(0.96, 0.9, 0.42)` @ 72% blend |
| Reachable destination | mint-white highlight |
| Invalid/unavailable | dimmed toward gray |

**Simulated turn** (default 3×3 `{9,1,10 / 7,5,2 / 5,3,3}`, carry range 2 incl. diagonals):

1. Click the **9** → it takes max 4 (9−5). Tile flashes, becomes 5, header shows
   `Carry 4`; BFS paints every tile within 2 steps reachable (corner-cutting through
   the sealed X is blocked).
2. Click the **1** → place 4 → becomes 5. Placement sound, carry resets.
3. `ApplyInstabilityDecay`: counts open tiles > 5 → e.g., three remain above 5 =
   threshold → no decay. If four were above 5, each loses 1 (never below 5) with a
   purple pulse-and-shrink flash.
4. Greed-island variant: after placing, one orthogonal neighbor gains
   `+coinTileYield` tide ("coin yield" log line); consumption variant drains the placed
   tile by 1.
5. Win check: default goal = *all* open tiles == 5 (late-game islands may use
   percentage goals — header reflects which). On success every open tile flashes
   green, the board scales down and fades out over 0.38 s, restoration is recorded,
   and a solved sting plays. Solving via overlay returns control to exploration and
   marks the mound/box visually "solved."

Sealed-tile click (non-overlay scenes) → loads the guarding encounter into combat;
winning unseals the tile permanently (persisted via `HasClearedEncounter`).

---

## 5. Combat (`CombatScene`)

`CombatSceneBootstrap` themes the battlefield to the island, spawns 3 allies left /
up to 3 enemies right (boss encounters use boss sprites & intro sting).

```
                      TIDE BREAK READY!          ← gold flash when bar maxes
   PLAYER ◄████████████│███░░░░░░░░░░░░░► ENEMY
                 ▲ tug-of-war pin slides with momentum
 ┌─────────────┐                              ┌──────────────┐
 │ Killian     │                              │ Imp   HP 42  │
 │ HP 88/110   │      (lunge/hit-shake,       │ Orc   HP 67  │
 │ MP 30/50    │       colored hit FX per     │ Troll HP 90  │
 │ [ATK][SKILL]│       element)               └──────────────┘
 │ [TB][FLEE]  │
 └─────────────┘   momentum fill: blue→green toward player, blue→red toward enemy
```

Turn flow exactly as coded:
1. **PlayerInput** — assign actions for all 3 allies (Attack / Defend / first usable
   Skill / Tide Break when ready / optional 1 swap per turn if enabled). Enemies plan
   simultaneously (`CacheEnemyActions`).
2. **ActionExecution** — speed-ordered queue (ties broken by registration order).
3. **Clash resolution** — if attacker and defender targeted each other: elemental
   winner deals `ClashWinnerMultiplier`, loser still deals reduced damage; neutral
   matchups trigger the **QTE ring**: a shrinking circle, press Space/click/tap inside
   the final 30% window ("WAIT..." flips to "PRESS!") — success shifts momentum to the
   player, fail hands it to the enemy.
4. Damage math: base ATK × (1+buffs) × element mult (1.5×/0.67×/1.0×) × variance
   (0.8–1.2) × crit (rate/damage) × difficulty; shields absorb before HP; defending
   cuts damage via `DefendMultiplier`.
5. **Momentum/Tide Breaks** — strong hits shift ±0.15; at ±1.0 the side unleashes a TB
   (AoE nuke, ally shield, party heal, or self-buff depending on data), then momentum
   resets. Team-up attacks can piggyback on ally attacks based on average bond level.
6. Victory → XP (full to active party, `reserveXpMultiplier` to reserve), gear drop
   roll with rarity weights, possible gear-drop toast on the HUD. Defeat/flee → return
   flow. Final-boss defeat counting increments only on loss (bad-ending trigger #1:
   4th loss = "more than three").

---

## 6. Quest Journal (**J**) — Persona-style tabs

```
 ┌─────────────────────────── Quest Journal ─────────────── X ─┐
 │      [ Story Progress ][ Ancient Texts ][ Hero Bonds ]      │
 │ Act II -- The Deepening -- ...                              │
 │ Islands Restored: 2 / 6                                     │
 │ Lust Restoration: 82%                                       │
 │ Next: The final challenge on Lust awaits...                 │
 │ Bad Ending Threshold: 1 / 4 defeats        ← shown only on  │
 │                                               Bad branch    │
 └─────────────────────────────────────────────────────────────┘
```

- Tabs slash-transition in/out (fixed this pass: the outgoing tab now animates
  correctly instead of vanishing).
- **Ancient Texts** lists discovered inscriptions with hero tags and truncated previews.
- **Hero Bonds** shows all 10 pairs with bars colored red → yellow → green across
  0–100 and relationship labels (Strangers → Inseparable). Bonds feed directly into
  the relationship multipliers used in battle.

---

## 7. Pause Menu (**Esc/P**, Start button, mobile ⏸)

560 px navy panel: **Resume · Party · Inventory · Save · Load · Settings · Quit to
Menu.** Time freezes while open (`timeScale 0`, always restored). Load shows an inline
confirm dialog ("Load saved game? Current progress will be replaced.") before restoring
and re-entering the correct scene.

---

## 8. Endings

**Good/Bittersweet** — beat the final island's boss with healthy restoration history:
gold ending music, good-ending narrative beat, cutscene of the five fading together on
the sunset hill, then exit flow back to menu.

**Bad** — triggered by 4th final-boss loss *or* clearing only the minimum 75% on every
island (`ShouldResolveBadEnding` checks both rules): red-toned reactions play, the MC
is left alone on the hill. Either way the book closes on the storyteller's tale.

---

## 9. What you'd notice playing today

- Everything above is functional end-to-end; saves persist across sessions and corrupt
  saves fall back to backups.
- Recent fixes in these passes: quest-journal tab transition targeting the right panel,
  restoration-event suppression now actually suppressible, battle heroes render with
  their own styled visuals again (a stray generic-visual overwrite was clobbering them
  in 3D battle mode), stale skill-targeting state cleared when switching actions, and
  the one-frame pause-menu overlap during Escape toggling.
