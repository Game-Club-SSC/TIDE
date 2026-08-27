<div align="center">

# TIDE

**Balance the world. Face the fate.**

Turn-based fantasy RPG · Unity 6 · Five elements · Six corrupted islands

![TIDE key art](promo-site/public/og.png)

</div>

## The game

Every century, five teenagers are chosen to manipulate Tide: the balance between Light and Shadow. They must restore six islands ruled by human vice, knowing only gradually that completing their purpose will also end their lives.

TIDE is built around three promises:

- **Emotional inevitability:** the story moves from reluctant adventure to dread, then acceptance.
- **Balance over power:** the best move is not always the largest number.
- **Character attachment:** relationships must make the party's fate matter.

The canonical creative reference is [Game Design Document V2 FINAL.md](Game%20Design%20Document%20V2%20FINAL.md).

## Core loop

1. Explore a corrupted island and meet its people.
2. Win elemental encounters and redistribute Tide in environmental puzzles.
3. Reach the 75% restoration threshold.
4. Defeat the island boss to complete restoration.
5. Return to the boat and choose the next unlocked island.

Combat can contribute at most 50% of ordinary restoration. Puzzles and environmental actions provide the rest needed to reach the boss. The boss contribution is tracked separately so it cannot be lost to the ordinary-combat cap.

## Signature systems

| System | Player-facing idea | Repository state |
| --- | --- | --- |
| Tide puzzles | Carry and place values from 1–10 until the board balances at 5; sealed tiles, traversal, and instability complicate the solution. | Implemented with data-driven variants and verification components. |
| Elemental combat | Fire, Water, Earth, Air, and Space form a readable advantage network. Neutral clashes use timing and choice. | Turn flow, targeting, clashes, status effects, and enemy profiles are implemented. |
| Momentum | Effective play moves a shared tug-of-war meter and unlocks Tide Break abilities. | Implemented with progression and HUD integration. |
| Party building | Bring three active heroes from a five-person party; develop skills, Tide Breaks, bonds, and gear. | Core runtime and persistence exist; balance and authored content still need playtesting. |
| Island restoration | Combat, puzzles, boss completion, travel, and save state form one progression loop. | Source-level flow exists with regression coverage; the complete player journey still requires repeated Unity playtests. |
| Narrative | Ancient texts and relationships reframe victory across three acts and multiple endings. | Framework and persistence exist; full island dialogue, banter, and cinematic content remain production work. |

## Development status

TIDE is a **system-rich playable prototype in active development**, not a finished vertical slice. The repository contains most core gameplay foundations, but source implementation is not the same as a shippable experience.

### Strong foundations

- Turn-based combat, five-element matchups, neutral clashes, momentum, and Tide Breaks
- Data-driven Tide puzzles, sealed/encounter-locked tiles, decay, and win conditions
- Restoration thresholds, boss gates, explicit boat travel, and island progression
- Three-active/two-reserve party composition, XP, skills, gear, difficulty, and New Game+
- Save/load, dialogue effects, relationships, lore discoveries, endings, localization, mobile input, and developer controls
- Context-menu verification suites plus offline repository checks

### Highest-priority production work

- Prove the complete title → hub → island → encounter/puzzle → boss → travel → save/reload path in the Unity Editor and in builds.
- Finish one island to the target quality bar before scaling: environment, enemies, boss presentation, dialogue, VFX, music, onboarding, and rewards.
- Replace procedural or placeholder character, environment, UI, and audio assets with a coherent authored style.
- Write relationship scenes, field banter, conflict, reconciliation, and combat assists for every hero pair across all three acts.
- Tune encounters, restoration pacing, puzzle recovery, Tide Break frequency, XP curves, gear, and endings with playtest evidence.
- Finalize the Space hero, island/cultural canon, platforms, controls, accessibility, and release requirements.

## Five elements

| Element | Strong against | Vulnerable to |
| --- | --- | --- |
| Fire | Earth, Air | Water, Space |
| Water | Fire, Space | Earth, Air |
| Earth | Water, Space | Fire, Air |
| Air | Earth, Water | Fire, Space |
| Space | Fire, Air | Water, Earth |

## Six islands

| Order | Island | Production identity |
| ---: | --- | --- |
| 1 | Gluttony | Consumption and excess |
| 2 | Greed | Avarice, value, and temptation |
| 3 | Sloth | Apathy, delay, and diminished agency |
| 4 | Wrath | Rage, escalation, and loss of restraint |
| 5 | Envy | Reflection, comparison, and stolen strengths |
| 6 | Pride | Isolation, superiority, and relationship pressure |

## Open and test

Requirements:

- Unity `6000.3.7f1`
- TextMeshPro packages restored by Unity
- Git LFS if large assets are introduced later

Quick start:

1. Clone the repository and open its root in Unity Hub.
2. Open `Assets/Scenes/TitleScene.unity` for the intended entry flow, or `Assets/Scenes/level_1.unity` for exploration-focused testing.
3. Enter Play Mode and test new game, load, pause, dialogue, boat travel, combat, puzzles, and save/reload.
4. Treat Console errors, missing references, or a blocked critical path as release blockers.

Primary gameplay scenes:

- `level_1.unity` — exploration and island flow
- `PuzzleScene.unity` — Tide puzzle gameplay
- `CombatScene.unity` — turn-based battles

## Verification

Most project tests are MonoBehaviour components exposed through `[ContextMenu]`.

1. Create an empty GameObject in a test scene.
2. Attach the relevant verification component.
3. Open the component menu and run its named test command.
4. Confirm the Unity Console reports success with no exceptions or compile errors.

Critical regression commands:

- `PostDeferralVerticalSliceRegressionRunner` → **Run Post-Deferral Vertical Slice Regression Matrix**
- `IslandContentVerificationTest` → **Run Island Content Verification**
- `GluttonyIslandVerificationTest` → **Run Gluttony Island Verification**
- `RestorationTrackerTest` → **Run Restoration Tracker Tests**
- `BossEncounterGateTest` → **Run Boss Encounter Gate Tests**
- `RestorationThresholdGateTest` → **Run Restoration Threshold Gate Tests**
- `IslandProgressionTravelTest` → **Run Island Progression + Travel Tests**
- `DevGodModeStateTest` → **Run Dev God Mode State Tests**
- `HeroProgressionTest` → **Run All Progression Tests**
- `GearSystemTest` → **Run All Gear Tests**
- `GearProgressionTest` → **Run All Gear Progression Tests**

Repository checks live in `scripts/validate.ps1`, `scripts/bug_audit.ps1`, and `scripts/runtime_sim.ps1`. Unity compile/import and hands-on Play Mode testing remain authoritative.

## Repository map

```text
Assets/
├── *.cs                    Gameplay and verification components
├── Scenes/                 Title, hub, exploration, combat, and puzzle scenes
├── Resources/              Runtime-loaded heroes, enemies, islands, encounters, puzzles, skills, and lore
├── Docs/                   Architecture and production documentation
├── Settings/               Unity settings assets
└── TextMesh Pro/           Text rendering resources
promo-site/                 Promotional website source and generated key art
scripts/                    Offline validation and simulation checks
```

For a technical map of the runtime, start with [Assets/Docs/REPO_UNDERSTANDING.md](Assets/Docs/REPO_UNDERSTANDING.md).

## Team

| Role | Contributors |
| --- | --- |
| Lead | Ryan N |
| Programming | Andrian Z, Clinton W |
| Art | Ryan P, Tilly, Tully |
| Writing and world design | Hannah, Tilly, Tully |
| Music | Cho, Enzo |

## Project site

The promotional site lives in [`promo-site`](promo-site). Its final production URL will be added here after a validated Sites deployment.

## License

Game Club 2026 — SSC
