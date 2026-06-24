<div align="center">

# 🌊 TIDE
**Turn-Based Fantasy RPG**

![Unity 6](https://img.shields.io/badge/Unity-6000.3-blue)
![Status](https://img.shields.io/badge/Status-Vertical%20Slice%20Complete-brightgreen)
![Issues](https://img.shields.io/github/issues/Game-Club-SSC/TIDE)

</div>

---

## 📖 Game Overview

The game is set in a world of five elemental forces: **Fire, Water, Earth, Air, and Space**. Every century, five chosen individuals are born with the ability to sense and manipulate **"Tide"** — the fundamental balance of good and evil. These chosen heroes are destined to cleanse six corrupted islands ruled by embodiments of human vice.

However, it is hidden from them that once their purpose is fulfilled, they will die. As the story unfolds, the tone shifts from a fantasy adventure to a philosophical exploration of fate, purpose, and the burden of knowing when you will die.

---

## 🎮 Core Gameplay

- **⛵ Island Restoration:** Players travel between islands by boat, restoring corrupted areas by balancing Tide.
- **🧩 Tide Puzzles:** Corrupted sections of the islands are represented as top-down grids where players must move and place Tide to reach a perfect balance of 5.
- **💥 Combat System:** Turn-based battles utilizing a Rock-Paper-Scissors elemental clash system with five elements.
- **⚖️ Tug-of-War Momentum:** Landing effective elemental attacks shifts a momentum bar. When fully shifted, that side can unleash a devastating **Tide Break**.
- **📖 Narrative:** Three-act story evolving from heroic fantasy into a meditation on fate, driven by ancient text discoveries and character relationships.

---

## 🏗️ Development Status

> **Vertical slice complete.** All core systems are implemented and data-rich. The game loop (explore → combat/puzzle → restoration → boss gate → next island) is fully functional at the code level.

### ✅ Implemented Systems

| System | Status | Details |
|--------|--------|---------|
| **Combat** | ✅ Complete | Full turn flow, 5-element clash, QTE neutral clashes, crit system |
| **Tide Puzzles** | ✅ Complete | 3×3 grid, take/place/traversal, sealed tiles, instability decay |
| **Momentum & Tide Breaks** | ✅ Complete | Tug-of-war bar, per-element Tide Breaks, unlock progression |
| **Island Restoration** | ✅ Complete | 75% boss gate, combat/puzzle/boss restoration split |
| **7 Island Configs** | ✅ Complete | Full encounters, puzzles, and enemy data for all islands |
| **Hero Progression** | ✅ Complete | XP, leveling, stat growth, gear sets with percentage bonuses |
| **Party Management** | ✅ Complete | 3 active + 2 reserve, swap service, setup UI |
| **Narrative System** | ✅ Complete | Ancient texts, 3-act structure, relationship tracker |
| **Dialogue System** | ✅ Complete | DialogueTree, runner, and UI |
| **Endings** | ✅ Complete | Good/bad ending evaluation, cutscene directors |
| **Save System** | ✅ Complete | WorldSaveService with PlayerPrefs persistence |
| **Island-Specific Mechanics** | ✅ Complete | Greed economy, Sloth status effects, Envy mirror |
| **Difficulty Modes** | ✅ Complete | Story / Standard / Hardcore |
| **New Game+** | ✅ Complete | Scaled multipliers across cycles |
| **Localization** | ✅ Complete | English + Spanish |
| **Mobile Input** | ✅ Complete | Touch d-pad + action buttons |
| **Dev Tools** | ✅ Complete | Dev menu, 32 cheat flags, performance monitor |
| **Test Suites** | ✅ Complete | 20+ MonoBehaviour test files, 318 offline checks |

### 🔲 Remaining Work

| Area | Status | Notes |
|------|--------|-------|
| **Character Art** | 🔲 Placeholder only | Procedural sprites — needs real pixel art / models |
| **Audio** | 🔲 Procedural only | Sine-envelope synth — needs real BGM & SFX |
| **Dialogue Content** | 🔲 Scaffolded | System exists, but story dialogue across all islands is sparse |
| **Environment Art** | 🔲 Not started | No tilemap art, island visuals, or corruption effects |
| **Boat Travel Flow** | 🔲 Partially wired | Components exist, end-to-end flow needs verification |
| **Enemy AI Variety** | 🔲 Scaffolded | ViceAIProfile exists, weighted element-aware selection needed |
| **UI Polish** | 🔲 Audit done | Layout fixes from playtesting not confirmed merged |
| **Balance Tuning** | 🔲 Needs playtest | Element scaling, stat curves, boss tuning |
| **Regression Tests** | 🔲 Blocked | All 9 VSR matrix rows require Unity Editor |

---

## 🗺️ Development Timeline & Roadmap

### 📅 Phase 1: Core Mechanics & Prototyping ✅
- [x] Foundational grid-movement system & puzzle board UI
- [x] Basic Tide Puzzle logic (balancing to 5)
- [x] Turn-Based combat skeleton
- [x] Automated test coverage

### 📅 Phase 2: Combat System & Elemental Balancing ✅
- [x] Rock-Paper-Scissors elemental clash system
- [x] Tug-of-War Momentum bar mechanics
- [x] Tide Break ultimate abilities
- [x] 3D character models and grounded combat visuals
- [x] SingleAlly skill targeting for support abilities
- [x] Neutral-clash QTE flow and in-battle feedback
- [ ] Varied elemental enemy types with distinct AI patterns
- [ ] Element scaling balance tuning

### 📅 Phase 3: World Building & Art Integration 🔄
- [x] Six corrupted islands designed and mapped
- [x] Vice-island content alignment with GDD scope
- [x] Boat travel mechanics between islands
- [x] World map travel framework and island progression
- [x] Gear instance leveling and smithy duplication
- [ ] Full character sprites, boss models, and environments
- [ ] End-to-end boat travel flow verification

### 📅 Phase 4: Polish, Audio & Narrative 🔲
- [ ] Background music and sound effects integration
- [x] Inventory and abilities menu framework
- [x] Three-act narrative beats and endings framework
- [ ] Full dialogue content for all story beats
- [ ] UI/UX polish and final game balancing

---

## 👥 The Team

| Role | Name |
|------|------|
| 👑 **Lead** | Ryan N |
| 💻 **Programming** | Andrian Z, Clinton W |
| 🎨 **Art** | Ryan P, Tilly, Tully |
| 📝 **Writing & World Design** | Hannah, Tilly, Tully |
| 🎵 **Music** | Cho, Enzo |

---

## 🧩 Five Elements

| Element | Beats | Loses To |
|---------|-------|----------|
| 🔥 **Fire** | Earth, Air | Water, Space |
| 💧 **Water** | Fire, Space | Earth, Air |
| 🌍 **Earth** | Water, Space | Fire, Air |
| 💨 **Air** | Earth, Water | Fire, Space |
| ✨ **Space** | Fire, Air | Water, Earth |

---

## 🏝️ The Six Islands

| Island | Vice | Boss Mechanic |
|--------|------|---------------|
| 🪙 **Greed** | Avarice | Coin tiles, gold economy |
| 🍖 **Gluttony** | Consumption | Puzzle consumption variant |
| 😴 **Sloth** | Apathy | Slow/Drowsy status effects |
| 🔥 **Wrath** | Rage | Aggressive enemy patterns |
| 🪞 **Envy** | Covetousness | Element mirror, skill covet |
| 👑 **Pride** | Ego | Relationship manipulation |

---

## 🛠️ Tech Stack

- **Engine:** Unity 6 (6000.3.7f1)
- **Language:** C#
- **UI:** TextMeshPro
- **Testing:** MonoBehaviour `[ContextMenu]` test suites + PowerShell offline validation (318 checks)
- **CI:** GitHub Actions (OpenCode workflow for issue-triggered automation)

---

## 📂 Project Structure

```
Assets/
├── *.cs                    # Game scripts (153 files, flat structure)
├── Scenes/                 # 10 Unity scenes (exploration, combat, puzzle, 7 islands)
├── Resources/              # Runtime-loaded ScriptableObjects
│   ├── HeroData/           # 5 hero definitions
│   ├── EnemyData/          # 12+ enemy types
│   ├── Islands/            # 7 island configs
│   ├── Encounters/         # 57 encounter configs
│   ├── Puzzles/            # 29 puzzle layouts
│   ├── SkillData/          # 7 skills
│   ├── TideBreakData/      # 5 element Tide Breaks
│   └── AncientTexts/       # 9 lore entries
├── Docs/                   # Technical documentation
└── TextMesh Pro/           # TMP assets
```

---

## 🧪 Testing

Tests are MonoBehaviour scripts with `[ContextMenu]` methods. To run:

1. Open Unity Editor
2. Create empty GameObject, attach test component
3. Right-click component header → Run test method
4. Check Console for results

Key test suites:
- `BattleFlowTestSuite` — Battle phase/clash/momentum
- `CombatUnitTestSuite` — Unit stats/damage/healing
- `PostDeferralVerticalSliceRegressionRunner` — 9-matrix regression
- `HeroProgressionTest` — Progression systems
- `GearSystemTest` — Gear mechanics
- `SavePersistenceTest` — Save/load verification

---

## 📜 License

Game Club 2026 — SSC
