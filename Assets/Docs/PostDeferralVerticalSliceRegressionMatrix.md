# Post-Deferral Vertical Slice Regression Matrix

Date: 2026-04-10
Issue: #130
Scope baseline: explore -> puzzle/combat -> restoration -> boss gate (75%)

## Matrix Definition

| Matrix ID | Scene Anchor | Core Loop Coverage | Verification Script | Method | Run Order |
|---|---|---|---|---|---|
| VSR-001 | `level_1.unity` | Island content set, encounter ordering, restoration budgets, boss-threshold budgeting | `IslandContentVerificationTest` | `RunTests` | 1 |
| VSR-002 | `level_1.unity` | Restoration accounting, duplicate protection, threshold checks, reset/multi-island isolation | `RestorationTrackerTest` | `RunTests` | 2 |
| VSR-003 | `level_1.unity` | Boss unlock at threshold/above-threshold, event behavior, island filtering | `BossEncounterGateTest` | `RunTests` | 3 |
| VSR-004 | `level_1.unity` | Restoration threshold gate startup sync and threshold transitions | `RestorationThresholdGateTest` | `RunTests` | 4 |
| VSR-005 | `level_1.unity` | Exploration progression and travel unlocks after restoration completion | `IslandProgressionTravelTest` | `RunTests` | 5 |
| VSR-006 | `CombatScene.unity` | Combat-driven hero XP and leveling behaviors | `HeroProgressionTest` | `RunAllTests` | 6 |
| VSR-007 | `CombatScene.unity` | Gear equip/unequip/full-set effects with level growth application | `GearSystemTest` | `RunAllTests` | 7 |
| VSR-008 | `CombatScene.unity` | Gear progression milestones, random slot rules, duplication/finalization guardrails | `GearProgressionTest` | `RunAllTests` | 8 |
| VSR-009 | `level_1.unity` | Post-deferral debug state controls for tracker/progression consistency | `DevGodModeStateTest` | `RunTests` | 9 |

## Execution Status (Current Run)

Environment note: The current agent session is CLI-only and cannot open Unity Editor. Context-menu test execution is blocked in this environment.

| Matrix ID | Status | Result | Repro / Blocking Notes | Defect Link |
|---|---|---|---|---|
| VSR-001 | Blocked | Not run | Blocked: Unity Editor required to execute context-menu MonoBehaviour tests. Repro: open `level_1.unity`, attach `PostDeferralVerticalSliceRegressionRunner`, invoke matrix run from component menu. | N/A |
| VSR-002 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-003 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-004 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-005 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-006 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-007 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-008 | Blocked | Not run | Same environment block as VSR-001. | N/A |
| VSR-009 | Blocked | Not run | Same environment block as VSR-001. | N/A |

## Blocking Defect Template (Severity-Tagged)

Use this template if any matrix row fails when run in Unity Editor:

- `Severity`: `S0` (crash/data loss), `S1` (core loop blocked), `S2` (major functional regression), `S3` (minor)
- `Matrix ID`: e.g., `VSR-003`
- `Scene`: e.g., `level_1.unity`
- `Script + Method`: e.g., `BossEncounterGateTest.RunTests`
- `Observed`: concise failure statement
- `Expected`: concise expectation
- `Repro`: exact steps and setup
- `Follow-up Issue`: GitHub issue URL once created

## How To Run In Unity Editor

1. Open `level_1.unity`.
2. Create an empty GameObject named `PostDeferralRegressionRunner`.
3. Add component `PostDeferralVerticalSliceRegressionRunner`.
4. From the component context menu, run `Run Post-Deferral Vertical Slice Regression Matrix`.
5. Capture console output into this document by replacing each blocked row with pass/fail outcome and notes.
