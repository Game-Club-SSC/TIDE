# TIDE Repository Understanding — AI Agent Reference

> **Purpose:** A comprehensive reference for AI agents working on this codebase. Summarizes architecture, systems, data flow, and key patterns so future sessions can onboard quickly.

---

## 1. Project Identity

| Field | Value |
|---|---|
| **Engine** | Unity 6 (6000.3.7f1) |
| **Genre** | Turn-Based Fantasy RPG |
| **Repo** | `Game-Club-SSC/TIDE` on GitHub |
| **Language** | C# (Unity MonoBehaviour-based) |
| **Build System** | Unity Editor only — no CLI build/test tooling |
| **Contributors** | Ryan N (Lead), Andrian Z, Clinton W |
| **Bot Account** | `OpenCode-SSC-T` (shadowbanned for GitHub issue creation) |

---

## 2. High-Level Architecture

The game has **three main scenes** that the player transitions between:

```
level_1.unity (Exploration)  ←→  CombatScene.unity (Battles)
         ↕
   PuzzleScene.unity (Tide Puzzles)
```

**GameStateManager** (singleton, `DontDestroyOnLoad`) orchestrates all scene transitions, state persistence, and cross-scene data passing via `Pending*` properties.

### Singleton Pattern (used everywhere)
```csharp
public static ClassName Instance { get; private set; }
private void OnEnable() {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

**Key singletons:** `GameStateManager`, `IslandRestorationTracker`, `PartyManager`, `HeroProgressionManager`, `IslandProgressionManager`

---

## 3. Core Game Loop

```
Explore island → Encounter enemies OR puzzle boxes → Fight / Solve →
Restoration % increases → At 75%, boss unlocks → Defeat boss → Next island
```

### Island Restoration System
- Each island tracks restoration via `IslandRestorationTracker` (singleton).
- Combat can contribute up to ~50% restoration; puzzles contribute the rest.
- At **75% restoration**, the island's boss encounter unlocks.
- `IslandFlowController` sequences encounters (combat/puzzle pairs) for an island.
- `IslandRestorationState` tracks per-island completion with encounter-level granularity.

---

## 4. Combat System

### Files
| File | Role |
|---|---|
| `BattleManager.cs` | Turn flow, phase management, clash resolution, action execution, flee logic, visual feedback |
| `CombatUnit.cs` | Base class for all combat units — stats, damage, healing, status effects, death |
| `ElementMatchup.cs` | 5-element advantage table + damage multipliers |
| `MomentumState.cs` | Tug-of-war bar that enables Tide Break ultimates |
| `SkillData.cs` | ScriptableObject defining skills (single-target, AoE, self, single-ally) |
| `TideBreakAbility.cs` | Static default Tide Break definitions |
| `TideBreakData.cs` | ScriptableObject for element-specific Tide Break abilities |
| `StatusEffect.cs` | Buff/debuff/poison effects with duration and magnitude |
| `BattleHud.cs` | Runtime battle UI (action buttons, HP bars, momentum bar) |
| `BattleEscapeMenu.cs` | In-combat escape/flee menu |
| `CombatSceneBootstrap.cs` | Sets up combat scene from `GameStateManager.PendingEnemyComposition` |

### Battle Flow (BattlePhase enum)
```
StartBattle → PlayerInput → ActionExecution → EndTurn → (loop back to PlayerInput)
                                                      → Victory / Defeat / Fled (terminal)
```

### Element System (5 elements)
```
Fire   beats Earth, Air    — loses to Water, Space
Water  beats Fire, Space   — loses to Earth, Air
Earth  beats Water, Space  — loses to Fire, Air
Air    beats Earth, Water  — loses to Fire, Space
Space  beats Fire, Air     — loses to Water, Earth
```
- **Strong:** 1.5x damage (attacker has advantage)
- **Weak:** 0.67x damage (attacker has disadvantage)
- **Neutral:** 1.0x damage (same element or Element.None only — all cross-element non-advantaged attacks are Weak)

### Clash System
When a player and enemy target each other simultaneously:
- Element advantage determines winner
- Winner deals 1.5x attack, loser deals 0.5x attack
- Neutral clash: both deal 0.6x attack
- Shifts momentum bar
- Neutral elemental clashes can trigger a QTE when both units are alive, opposite factions, and both have non-`None` elements
- QTE success gives ally clash advantage (+0.15 momentum); QTE fail gives enemy clash advantage (-0.15 momentum)
- If no runtime QTE responder exists, fallback is deterministic: higher Speed wins, then earlier registration order; fallback can also be disabled to keep legacy neutral resolution

### Momentum / Tide Break
- `MomentumState` ranges from -1.0 (enemy) to +1.0 (player)
- Effective attacks shift toward attacker's side by 0.15
- At ±1.0, that side can unleash a **Tide Break** (ultimate ability)
- Momentum resets after Tide Break fires

### Damage Formula
```
baseDamage = max(1, attack * (1 + attackModifier))      // attackModifier from status effects
modifiedDamage = baseDamage * elementMultiplier * skillMultiplier * variance(0.8–1.2)
if crit: modifiedDamage *= critDamage
if defending: modifiedDamage *= 0.5                       // applied BEFORE defense subtraction
effectiveDefense = max(0, defense * (1 + defenseModifier)) // defenseModifier from status effects
actualDamage = max(1, modifiedDamage - effectiveDefense)
```

### CombatUnit Stats
`HP`, `MaxHP`, `MP`, `MaxMP`, `Attack`, `Defense`, `Speed`, `CritRate`, `CritDamage`, `Element`

### Skill Target Types
`SingleEnemy`, `AllEnemies`, `Self`, `SingleAlly`

### Status Effects
`Poison`, `BuffAttack`, `DebuffAttack`, `BuffDefense`, `DebuffDefense`, `None`

---

## 5. Tide Puzzle System

### Files
| File | Role |
|---|---|
| `TideManager.cs` | Puzzle board logic, UI generation, tile interaction, instability decay, win evaluation |
| `TideTile.cs` | Individual tile — value (1–10), sealed state, visual states |
| `PuzzleData.cs` | ScriptableObject defining puzzle layout, sealed/locked tiles, win conditions |
| `PuzzleHud.cs` | Puzzle UI overlay (carry indicator, reset button) |
| `PuzzleOverlayController.cs` | Opens puzzles as overlays within the exploration scene |
| `PuzzleBoxInteractable.cs` | World object the player interacts with to start a puzzle |
| `PuzzleGuardSpawner.cs` | Spawns enemy guards near sealed tiles |
| `PuzzleSceneBootstrap.cs` | Initializes the puzzle scene |

### Puzzle Mechanics
- **3×3 grid** of tiles, each with Tide value 1–10
- **Goal:** Balance tiles to value 5 (perfect balance)
- **Taking Tide:** If tile > 5, take up to (value - 5). If tile < 5, take down to 1. Cannot cross past 5.
- **Carrying:** Player carries one bundle at a time, can traverse up to 2 tiles (including diagonal)
- **Placing:** Place carried Tide onto a normal tile (cannot exceed 10)
- **Sealed tiles:** Cannot interact with; cleared by defeating guards in combat
- **Instability decay:** If tiles > 5 exceed threshold (default 3), excess tiles decay toward 5 after each move

### Win Conditions
- Early game: percentage of tiles at 5
- Late game: all tiles at exactly 5

### Overlay vs Scene Mode
Puzzles can run either as a full scene (`PuzzleScene.unity`) or as an overlay within the exploration scene via `PuzzleOverlayController`.

---

## 6. Exploration System

### Files
| File | Role |
|---|---|
| `IsometricPlayer.cs` | Player movement controller (isometric top-down) |
| `TopDownFollowCamera.cs` | Camera that follows the player |
| `EnemyTrigger.cs` | Triggers combat encounters on contact |
| `OverworldEnemy.cs` | Overworld enemy representation |
| `ExclamationMarkSprite.cs` | Alert indicator above enemies |
| `CombatBoxInteractable.cs` | Interactable that starts combat |
| `AncientTextInteractable.cs` | Interactable for lore discovery |
| `ExplorationMapUI.cs` | Map UI during exploration |
| `GroundRestorationVisualizer.cs` | Visual feedback for island restoration progress |
| `BossEncounterGate.cs` | Gate that unlocks boss fight at 75% restoration |
| `RestorationThresholdGate.cs` | Gate that unlocks at specific restoration % |

---

## 7. Progression System

### Files
| File | Role |
|---|---|
| `HeroData.cs` | ScriptableObject — hero identity, element, base stats, starter skills |
| `HeroDatabase.cs` | Collection of all HeroData assets |
| `HeroProgressionManager.cs` | Singleton — XP, leveling, gear equip/unequip, stat growth |
| `LevelingConfig.cs` | ScriptableObject — XP curve, stat growth per level |
| `GearSetData.cs` | ScriptableObject — gear sets with percentage-based stat bonuses |
| `PartyManager.cs` | Singleton — active party (3) + reserve (2) management |
| `PartyData.cs` | ScriptableObject — party composition (active/reserve slots) |
| `PartySetupUI.cs` | UI for configuring party before exploration |
| `PartySwapPanel.cs` | In-battle party swap UI |
| `PlayerCustomizationUI.cs` | Cosmetic customization UI |
| `IslandProgressionManager.cs` | Singleton — tracks which island is currently active |

### Party Structure
- **5 heroes total** (one per element: Fire, Water, Earth, Air, Space)
- **3 active** in battle, **2 reserve**
- Main character's element chosen at game start
- Reserve members gain reduced XP (default 50%)
- Swapping allowed during first round of combat only

### Leveling
- XP granted after combat victory (from enemy `XpReward` values)
- Level up grants stat bonuses per `LevelingConfig` (HP/MP/ATK/DEF/SPD per level)

### Gear
- Full sets only (no mix-and-match)
- Sets provide percentage-based stat bonuses (ATK%, DEF%, HP%)
- Sets gain XP and unlock up to 3 bonus slots with random buffs

---

## 8. Narrative System

### Files
| File | Role |
|---|---|
| `NarrativeBeatDirector.cs` | Triggers narrative events based on game state |
| `AncientTextData.cs` | ScriptableObject — lore text content |
| `AncientTextInteractable.cs` | World object for discovering ancient texts |
| `AncientTextLogUI.cs` | UI for reviewing discovered texts |
| `AncientTextSceneBootstrap.cs` | Initializes ancient text system in exploration scene |

### Story Structure
- **6 corrupted islands**, each ruled by a vice (Lust, Wrath, Greed, Sloth, Pride, Envy)
- **Ancient texts** reveal the heroes' true nature across Acts I–III
- **Good ending:** Heroes fulfill purpose and fade away peacefully
- **Bad ending:** Triggered by more than 3 final-boss defeats or by the minimum-restoration bad-ending rule

---

## 9. World & Island Configuration

### Files
| File | Role |
|---|---|
| `IslandConfig.cs` | ScriptableObject — island identity, vice colors, encounter sequence |
| `IslandThemeRegistry.cs` | Resolves island IDs and provides active island context |
| `IslandFlowController.cs` | Sequences combat/puzzle encounters for an island |
| `IslandRestorationTracker.cs` | Singleton — tracks restoration % per island |
| `IslandRestorationState.cs` | Per-island state (combat/puzzle contributions, cleared encounters) |
| `IslandRestorationHud.cs` | HUD displaying restoration progress |
| `EncounterConfig.cs` | ScriptableObject — enemy composition for an encounter |
| `EnemyData.cs` | ScriptableObject — enemy stats, element, skills, XP reward |

### Island IDs
Format: `island_<vice>` (e.g., `island_lust`, `island_wrath`, `island_greed`, `island_sloth`, `island_pride`, `island_envy`)

---

## 10. Data Flow: Scene Transitions

### Exploration → Combat
```
1. EnemyTrigger/BossEncounterGate sets GameStateManager.PendingEnemyComposition
2. GameStateManager.EnterCombatSceneFromExploration(islandId, encounterId, restorationValue, returnPos)
3. Fade out → Load CombatScene → CombatSceneBootstrap reads PendingEnemyComposition → spawns enemies
4. Battle plays out → BattleManager calls GameStateManager.OnCombatEnded(won, fled)
5. If won: restoration recorded, XP granted, world state saved
6. Fade → Return to level_1 at saved position
```

### Exploration → Puzzle
```
1. PuzzleBoxInteractable sets GameStateManager.PendingPuzzleData/Layout
2. GameStateManager.EnterPuzzleScene(returnPos, puzzleBoxId)
3. Fade out → Load PuzzleScene → TideManager reads pending data → generates board
4. Puzzle solved → GameStateManager.MarkPuzzleSolved()
5. Fade → Return to level_1 → restoration recorded
```

### Puzzle → Combat (sealed tile)
```
1. Player clicks sealed tile → TideManager.TryTriggerSealedTileEncounter()
2. GameStateManager.EnterCombatSceneFromPuzzle(islandId, encounterId)
3. After combat → return to PuzzleScene (not level_1)
4. If won: sealed tile unseals
```

---

## 11. Resource Assets (Assets/Resources/)

| Path | Contents |
|---|---|
| `HeroData/` | `hero_fire.asset`, `hero_water.asset`, `hero_earth.asset`, `hero_air.asset`, `hero_space.asset` |
| `EnemyData/` | `enemy_golem.asset`, `enemy_orc.asset`, `enemy_troll.asset`, `enemy_sprite.asset`, `enemy_wraith.asset`, `enemy_imp.asset` |
| `Islands/` | `island_lust.asset`, `island_wrath.asset`, `island_greed.asset`, `island_sloth.asset`, `island_pride.asset`, `island_envy.asset` |
| `TideBreakData/` | Element-specific Tide Break abilities (`tb_fire.asset`, etc.) |
| `AncientTexts/` | Lore texts per island theme (`text_anger.asset`, `text_ego.asset`, etc.) |
| `Encounters/` | Combat encounter configurations |
| Root | `HeroDatabase.asset`, `PartyData.asset`, `PuzzlePrompt.png` |

---

## 12. Test Files

All tests are **MonoBehaviour scripts** with `[ContextMenu]` methods (no NUnit CLI runner):

| Test File | What It Tests |
|---|---|
| `BattleFlowTestSuite.cs` | Battle phase transitions, clash resolution, momentum, neutral clash QTE runtime/fallback/guard paths |
| `CombatUnitTest.cs` / `CombatUnitTestSuite.cs` | Unit stats, damage, healing, death, status effects |
| `RestorationTrackerTest.cs` | Island restoration tracking |
| `BossEncounterGateTest.cs` | Boss unlock at 75% restoration |
| `RestorationThresholdGateTest.cs` | Threshold gate logic |
| `TideMovementTest.cs` | Puzzle tile take/place/traversal |
| `GearSystemTest.cs` | Gear equip/unequip, stat bonuses |
| `HeroProgressionTest.cs` | XP, leveling, stat growth |
| `EnemyDataTestSuite.cs` | Enemy data validation |
| `HeroDataTestSuite.cs` | Hero data validation |
| `PartySetupTestSuite.cs` | Party composition validation |
| `IslandContentVerificationTest.cs` | Island config integrity |
| `TideBreakDataTest.cs` | Tide Break data validation |

---

## 13. Key Code Patterns & Conventions

### Naming
- Classes/Methods/Properties: `PascalCase`
- Private fields: `camelCase` (no prefix)
- Constants: `PascalCase` or `UPPER_SNAKE`
- Enums: `PascalCase` values

### Property Validation (HP/MaxHP pattern)
```csharp
public int MaxHP { get => maxHp; set { maxHp = Mathf.Max(1, value); hp = Mathf.Clamp(hp, 0, maxHp); } }
public int HP { get => hp; set { hp = Mathf.Clamp(value, 0, maxHp); if (hp <= 0 && isAlive) Die(); } }
```

### Error Handling
- No try/catch — use guard clauses and null checks
- Events always use `?.Invoke()`
- Values clamped via `Mathf.Clamp`

### Debug Logging
```csharp
Debug.Log($"[ClassName] Message: {variable}");
```

### Font
**Always use `LegacyRuntime.ttf`** (Arial.ttf not available in Unity 6)

### Attributes
All MonoBehaviours use `[DisallowMultipleComponent]`. Inspector fields use `[Header]`, `[SerializeField]`, `[Range]`, `[Tooltip]`.

---

## 14. File Organization

```
Assets/
├── *.cs                          # ALL game scripts (flat — no subdirectories)
├── *.prefab                      # Prefabs (PlayerUnit, EnemyUnit)
├── Scenes/                       # level_1, PuzzleScene, CombatScene
├── Resources/                    # Runtime-loaded ScriptableObject assets
│   ├── HeroData/                 # Hero definitions
│   ├── EnemyData/                # Enemy definitions
│   ├── Islands/                  # Island configurations
│   ├── TideBreakData/            # Tide Break abilities
│   ├── AncientTexts/             # Lore content
│   └── Encounters/               # Combat encounter configs
├── Settings/                     # URP render pipeline settings
├── TextMesh Pro/                 # TMP assets
├── Docs/                         # Documentation
└── TutorialInfo/                 # Unity tutorial layout
```

**Important:** All `.cs` files live flat in `Assets/` — there are no subdirectories for scripts.

---

## 15. Important Gotchas

- **Persistent save is OFF:** `GameStateManager.EnablePersistentSaveData` is hardcoded `false`. World state save/load via `PlayerPrefs` is effectively a no-op. All state resets between play sessions.
- **`CLAUDE.md`** exists as an untracked file in the repo root — may contain additional agent instructions.
- **Utility files not listed above:** `ElementalCharacterFactory.cs` (procedural character creation), `FuturisticSpriteLibrary.cs` (procedural sprite generation for combat VFX), `CombatDebugEntry.cs` (debug overlay helpers).

---

## 16. Development Status (as of latest commits)

### Completed
- ✅ Grid movement & puzzle board UI
- ✅ Tide puzzle logic (balance to 5, instability decay)
- ✅ Turn-based combat skeleton with full phase flow
- ✅ Elemental clash system (Rock-Paper-Scissors)
- ✅ Tug-of-War momentum bar + Tide Break ultimates
- ✅ Island restoration tracking per island
- ✅ Hero progression (XP, leveling, gear)
- ✅ Party management (3 active + 2 reserve)
- ✅ Ancient text / narrative system
- ✅ Six island configs with encounter sequences
- ✅ Boss encounter gating at 75% restoration
- ✅ Automated NUnit test coverage
- ✅ SingleAlly skill targeting
- ✅ Battle flee mechanics
- ✅ Puzzle overlay mode (in-exploration puzzles)

### In Progress / Not Started
- ❌ Varied elemental enemy AI patterns
- ❌ Element scaling balance
- ❌ Full character sprites, boss models, environments
- ❌ Boat travel between islands
- ❌ Background music and sound effects
- ❌ Dialogue and story events
- ❌ UI/UX polish and final balancing

---

## 17. Vertical Slice 1–40 Systems (issues #218–#258)

The post-vertical-slice cohort of issues added a fleet of new singletons, services, and tests. All live in flat `Assets/`. Playtest instructions per system are below.

### 17.1 Sprite-based player visual (VS-14, 2D sprite in 3D world)
- `BillboardSprite.cs` — keeps a `SpriteRenderer` facing the camera in the 3D world. Toggled via `FaceCamera`, `LockYAxis`, `RotationOffset`. Used by the overworld player root.
- `ElementalCharacterFactory.BuildExplorationPlayerSprite` — assembles `ElementalPlayerSprite` (sprite + ground shadow quad) under the player's transform. Replaces the legacy procedural 3D primitives when `IsometricPlayer.use2DSpriteVisual = true`.
- `IsometricPlayer.DisablePrimitiveRenderers` — disables the default `MeshRenderer` on the player GameObject so the 2D sprite is the only thing drawn.
- **Test:** `PlayerSpriteVisualTestSuite.cs` — "Run Player Sprite Visual Tests". Verifies sprite + shadow build, billboard, dash works in sprite mode, 3D legacy still builds, mode toggling removes the old root, style swap rebuilds sprite.
- **Playtest in Editor:** open `level_1`, enter Play mode, walk around. The player should appear as a sprite that always faces the camera. The cube primitive is hidden. Press `LeftAlt` to dash and `Space` to hop — both should still work. Toggle the legacy 3D mode in the inspector (`use2DSpriteVisual` on the player object) to confirm the procedural robot still builds and the sprite disappears.

### 17.2 Procedural audio (VS-15)
- `ProceduralAudioBuilder.cs` — synthesizes 4 BGM loops (Exploration, Combat, Puzzle, Ending) and 5 stings (Victory, Defeat, PuzzleSolved, BossIntro, TravelFanfare) from sine + exponential-decay envelopes. Used when no imported `AudioClip` is wired in.
- `AudioManager.GetOrGenerateClip` — falls back to the procedural builder per-cue when the serialized field is null.
- **Test:** `ProceduralAudioBuilderTest.cs` — "Run Procedural Audio Builder Tests". Verifies every cue has audible signal and stings decay head-to-tail.
- **Playtest:** enter Play mode. The `AudioManager` instance should crossfade BGM across scene loads (Exploration ↔ Combat ↔ Puzzle). Boss intro sting fires when `BossEncounterGate` unlocks a boss.

### 17.3 Crit stats (VS-10)
- `HeroData.baseCritRate` (0–1) and `baseCritDamage` (≥0) with Range/Min guards.
- `EnemyData.baseCritRate` and `baseCritDamage` mirror the above.
- `HeroProgressionManager.ApplyStatGrowth` and `CombatSceneBootstrap.ApplyEnemyDataToUnit` propagate crit stats onto spawned `CombatUnit`s.
- `BattleManager.ResolveAttack` rolls `Random.value < actor.CritRate` and multiplies damage by `actor.CritDamage` when triggered.
- **Test:** `CritStatsTest.cs` — "Run Crit Stats Tests". Verifies defaults, ranges, and clamps.
- **Playtest:** fight any encounter. Crits should now land with the new per-asset crit rate. Inspect the BattleManager console for the "CRITICAL HIT!" log line.

### 17.4 Per-element gear (VS-16) + Tide Break unlocks (VS-17)
- `GearSetData.element`, `GearSetData.tier`, `GearSetData.MatchesElement` (helper).
- `GearSetFactory` produces 5 element-themed starter sets (iron_guard, ember_weave, tide_charm, zephyr_mail, cosmic_lattice) plus a universal fallback.
- `HeroProgressionManager.EnsureStarterGearRegistry` auto-populates the registry on first boot; `GetGearSetForElement` picks the lowest-tier set matching the hero's element.
- **Test:** `GearSetFactoryTest.cs` — "Run Gear Set Factory Tests". Verifies element coverage, matching, validity, clamping, and registry lookup.
- **Playtest:** open Smithy (smithy interactable), equip a fire hero with ember_weave. The Fire-themed gear should apply ATK/DEF/HP bonuses via `TotalAttackPercent`/`TotalDefensePercent`/`TotalHpPercent`.
- **Tide Break unlocks:** already handled by `TideBreakData.GetForElement(elementId, heroLevel)` filtering on `unlockLevel`. Each hero in `HeroTideBreakFactory` has a level-1 AOE and a level-3 single-target.

### 17.5 Pre-final-boss conversation (VS-12) + Self-harm bad-ending beat (VS-13)
- `AcceptanceConversation.cs` — singleton with 3 dialogue lines. Gated on `island_pride` being active and `IslandRestorationTracker.GetRestorationPercent` ≥ 75%. `ForcePlayForDebug` bypasses the gate.
- `SelfHarmBeat.cs` — singleton with 4 lines. Gated on `GameStateManager.EndingBranch == Bad` and `IsEndingTriggered`.
- `NarrativeBeatsData.cs` — string constants for the 5 beat IDs and a `GetAllBeats()` catalog.
- **Test:** `NarrativeSystemsTest.cs` — "Run Narrative Systems Tests". Verifies gating, event firing, and catalog completeness.
- **Playtest:** set `IslandProgressionManager.ActiveIslandId = "island_pride"` and force restoration to 80% via dev menu, then trigger `AcceptanceConversation.ForcePlayForDebug` (or call `PlayAcceptanceConversation` once the gate passes). The 3 lines should log + fire `OnAcceptanceConversationFinished`. For self-harm: complete a bad-ending run, then call `SelfHarmBeat.ForcePlayForDebug`.

### 17.6 Ancient text authoring (VS-18)
- `AncientTextAuthoring.cs` — static class with 18 baseline `AncientTextData` entries covering Gluttony/Greed/Sloth/Wrath/Envy/Pride across Acts I/II/III.
- `GetAllAuthoredTexts()` merges the baseline with `Resources.LoadAll<AncientTextData>("AncientTexts")`.
- **Test:** `NarrativeSystemsTest.cs` (TestAncientTextAuthoring* cases) verifies count, validity, sin coverage, and act coverage.

### 17.7 Relationship tracker (VS-19)
- `RelationshipTracker.cs` — singleton. Per-heroId affinity 0–100. `GetRelationshipTier` returns `Stranger/Acquaintance/Friend/Close/Bonded`. Configurable thresholds. `OnAffinityChanged` and `OnTierChanged` events.
- **Test:** `NarrativeSystemsTest.cs` covers clamping, tier thresholds, and tier-transition events.

### 17.8 Power budget per island (VS-20)
- `PowerBudgetTracker.cs` — singleton. `TryConsumeBudget(islandId, cost)` returns false if the budget is insufficient. Default 3 per island. Refunds supported.
- **Test:** `NarrativeSystemsTest.cs` covers default seeding, in-budget consumption, and rejection of over-budget consumption.

### 17.9 Teleport anchors + travel validation (VS-21)
- `TeleportAnchor.cs` — MonoBehaviour with `anchorId`, `islandId`, `spawnPosition`, `isSceneEntrance`, `isBoatDock`. Static registry by id and by island.
- `TravelValidationService.ValidateTravel` — checks the destination is unlocked, fully restored, and has a boat-dock anchor. Returns a `ValidationResult` with failure reason.
- **Test:** `TravelValidationTest.cs` covers register/unregister, dock lookup, unrestored rejection, and empty-destination rejection.

### 17.10 Per-hero Tide Breaks (VS-22)
- `TideBreakData.heroId` — new field for per-hero ownership.
- `HeroTideBreakFactory` generates 2 unique Tide Breaks per hero (AOE at level 1, single-target at level 3) for all 5 elements. `GetTideBreaksForHero` filters by heroId, element, and hero level.
- **Test:** `HeroTideBreakFactoryTest.cs` covers all five heroes.

### 17.11 Party swap polish (VS-23)
- `PartySwapService.cs` — `TryQueueSwap(activeHeroId, reserveHeroId, out reason)` validates the request (rejects self-swap, empty ids, missing manager). `GetReservableHeroIds` returns the reserve party's heroIds.
- **Test:** `PartySwapServiceTest.cs` covers gating rules.

### 17.12 Mobile touch controller (VS-24)
- `MobileTouchController.cs` — singleton. D-pad input clamped to unit magnitude, 4 action buttons (Interact / Dash / Hop / Sprint), visibility toggle, ScreenSpaceOverlay canvas.
- **Test:** `MobileTouchControllerTest.cs` covers clamp, press/release, and visibility.

### 17.13 Per-island content packs (VS-25)
- `PerIslandContentRegistry.cs` — 6 packs (gluttony through pride), each with `islandId`, `displayName`, `encounterIdPrefix`, `recommendedLevel`, `bossId`. `GetPackForIsland` lookup.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestPerIslandContentRegistryCoverage) verifies the 6 packs.

### 17.14 Greed/Gluttony puzzle variants (VS-26)
- `PuzzleVariantService.cs` — wraps `PuzzleData.enableConsumption`, `consumptionAmount`, `enableGreedEconomy`, `coinTileYield`. `GetVariantLabel` returns gluttony/greed/default.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestPuzzleVariantService*).

### 17.15 Sloth status effects (VS-27)
- `SlothStatusEffectSet.cs` — `CreateSlowEffect(sourceName, duration, magnitude)` and `CreateDrowsyEffect(sourceName, duration)`. `IsSlothEffectType` helper.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestSlothStatusEffect*).

### 17.16 Envy mirror mechanic (VS-28)
- `EnvyMirrorService.cs` — `SetMirrorEnabled`, `SetMirroredElement`, `GetMirrorElementFor`, `GetMirrorSkillFor` (builds a half-cost / multiplier-scaled SkillData clone).
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestEnvyMirror*).

### 17.17 Difficulty pass (VS-29) + Difficulty modes (VS-37)
- `DifficultyModeService.cs` — singleton with Story / Standard / Hardcore. `GetDamageMultiplierForPlayer/Enemy`, `GetXpMultiplier`, `GetCurrencyMultiplier`, `AllowsFleeInCombat`. Hardcore removes flee and currency.
- **Test:** `GameSystemsTest.cs` covers the difficulty gating rules.

### 17.18 BattleHud polish (VS-30)
- `BattleHudPolishService.cs` — `GetCritFlashColor`, `GetCritFlashDuration`, `GetStatusEffectIconColor` per `StatusEffectType`, `GetStatusEffectLabel`, `GetMomentumBarColor` gradient.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestBattleHud*).

### 17.19 PlayerCustomizationUI palette + premium unlock (VS-31)
- `PlayerCustomizationCatalog.cs` — 7 palettes (3 default, 4 premium), `UnlockPalette`, `IsPaletteUnlocked`, `GetRequiredCurrencyFor`, `ResetForDebug`.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestPlayerCustomization*).

### 17.20 Vertical slice regression runner (VS-32)
- `VerticalSliceRegressionRunner.cs` — singleton with 32+ issue checks (one per VS issue). `RunRegression` logs pass/fail per issue. `RegressionCheckHelpers` provides `LogicOk` adapters per subsystem.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` covers registration, execution, and per-subsystem logic.

### 17.21 GameStateManager refactor (VS-33)
- The refactor was scoped as incremental extraction. New singletons split out of `GameStateManager`:
  - `WorldSaveService.cs` — owns the `TIDE_WORLD_STATE_V1` `PlayerPrefs` key, `enablePersistentSaveData` flag, `TryWriteJson`/`TryLoadJson`/`Clear`/`SetPersistentSaveEnabled`. `OnSavePersisted`/`OnSaveLoaded`/`OnSaveCleared` events.
  - `StoryProgressionService.cs` — owns `CurrentAct`/`HighestActReached`/`ResolvedEndingBranch`/`IsEndingTriggered` plus `OnStoryActChanged`/`OnEndingBranchChanged`/`OnEndingTriggered` events. `SetCurrentAct`/`SetEndingBranch`/`TriggerEnding`/`ResetForDebug`.
  - Plus the prior batch (`AcceptanceConversation`, `SelfHarmBeat`, `RelationshipTracker`, `PowerBudgetTracker`, `DifficultyModeService`, `NewGamePlusService`, `VerticalSliceRegressionRunner`, `MobileTouchController`).
- `GameStateManager` now focuses on scene transitions, combat outcome orchestration, and the public surface (Pending* fields, FlowController hookup).
- **Test:** `GameStateManagerRefactorTest.cs` — "Run GameStateManager Refactor Tests". Verifies WorldSaveService write/read/clear/disable and StoryProgressionService act/ending/trigger/reset.

### 17.22 Playtest documentation (VS-34)
- This section (`## 17`) is the canonical per-system playtest reference.
- `AGENTS.md` references this file as the deep-dive doc.
- **Test:** `VerticalSliceRegressionRunner` check #34 returns true.

### 17.23 Phone web controller auth (VS-35)
- `PhoneControllerAuthService.cs` — thread-safe bearer tokens plus a cryptographic six-digit pairing code. Pairing codes expire after five minutes, rotate after use or abuse, and enforce per-peer and global failed-attempt limits.
- `PhoneWebController.cs` — requires bearer auth for commands and state, accepts only its exact page origin, caps request bodies and worker/command queues, and applies network work off the Unity thread. LAN access over plain HTTP is an explicit, off-by-default `allowInsecureLanAccess` setting; loopback remains the safe default.
- HTTPS is not enabled in this component. `HttpListener` cannot load and serve a project certificate by itself: each host needs an OS-level certificate binding and trust setup. A portable in-app TLS path needs a different server stack plus certificate creation, storage, renewal, and phone trust handling. Do not turn LAN access on outside a trusted network until that product work is complete.
- **Test:** `PhoneControllerAuthServiceTest.cs` covers bearer auth, code expiry and rotation, per-peer/global limits, origin checks, and request-body caps. `VerticalSliceRegressionRunnerTest.cs` retains the service check.

### 17.24 New Game+ (VS-36)
- `NewGamePlusService.cs` — singleton. `RegisterCompletion`, `CanStartNewGamePlus`, `StartNewGamePlus`, `EndNewGamePlus`. Scaled enemy/xp multipliers via `Mathf.Pow(base, loopIndex)`. `GetCarryOverHeroIds` returns the active party.
- **Test:** `GameSystemsTest.cs` (TestNewGamePlusService).

### 17.25 Localization (VS-38)
- `LocalizationService.cs` — static. English + Spanish dictionary with 20+ ui.* keys. `SetLanguage`, `Get`, `HasKey`, `GetAllKeys`.
- **Test:** `GameSystemsTest.cs` (TestLocalizationService) verifies key lookup and language switching.

### 17.26 Dev cheat feature flags (VS-39)
- `DevCheatFeatureFlags.cs` — exactly 32 boolean/float flags. `GetAllFlagIds`, `LogicOk`, `ResetAllForDebug`.
- **Test:** `VerticalSliceRegressionRunnerTest.cs` (TestDevCheatFeatureFlagsCoverage) asserts 32 flags.

### 17.27 Performance budget (VS-40)
- `PerformanceBudgetMonitor.cs` — singleton. Tracks `TargetFrameRate=60`, `MaxHeroes=6`, `MaxEnemies=6`, `MaxFrameMs=16.67`. Records per-frame timing into a 600-frame rolling queue, exposes `CurrentAverageFrameMs`, `Min/MaxObservedFrameMs`, `IsMeetingBudget`. `IsWithinUnitCap(heroCount, enemyCount)` enforces the 6+6 cap.
- **Test:** `PerformanceBudgetMonitorTest.cs` — "Run Performance Budget Monitor Tests". Verifies singleton, unit cap, frame recording, reset, and defaults.
- **Playtest:** open Unity Profiler in a development build, run a Combat scene with the maximum party (3 active + 3 reserve enemy), watch the `PerformanceBudgetMonitor.CurrentAverageFrameMs` log line. Look for per-frame allocations in `ResolveAttack` and `ApplyStatusEffect`.
