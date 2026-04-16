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
- **6 corrupted islands**, each ruled by a vice (Lust, Anger, Greed, Desire, Ego, Envy)
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
Format: `island_<vice>` (e.g., `island_lust`, `island_anger`, `island_greed`, `island_desire`, `island_ego`, `island_envy`)

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
| `Islands/` | `island_lust.asset`, `island_anger.asset`, `island_greed.asset`, `island_desire.asset`, `island_ego.asset`, `island_envy.asset` |
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
