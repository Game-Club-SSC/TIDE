# HANDOFF — Bug Hunt & Fix for TIDE Project

## Mission
Find and fix at least 20 real bugs in the TIDE Unity project (Unity 6 Turn-Based Fantasy RPG) at /Users/andrian/GitHub/TIDE. Do not stop until all found bugs are fixed.

## Previous sessions
Two previous attempts were made. Both were interrupted mid-execution. The state is:

### What was accomplished
- **combat-hunter** ✅ Completed scan. Wrote report to bug_reports/combat_hunter_report.md
- **ui-hunter** ✅ Completed scan. Wrote report to bug_reports/ui_hunter_report.md
- **Total: 16 bugs found, 0 fixed**

### What failed / was interrupted
- **puzzle-hunter** — Re-dispatched but never completed (interrupted). No report file exists.
- **state-hunter** — Re-dispatched but never completed (interrupted). No report file exists.
- **bug-fixer** — Never dispatched (waiting on hunters).
- **reviewer** — Never dispatched (waiting on fixes).

### Files on disk (read these first)
- /Users/andrian/GitHub/TIDE/AGENTS.md — project rules, code style, naming conventions
- /Users/andrian/GitHub/TIDE/bug_reports/RESUME.md — full resume guide with bug details, role prompts, next steps
- /Users/andrian/GitHub/TIDE/bug_reports/combat_hunter_report.md — 8 detailed combat bugs (1 Critical, 1 High, 3 Medium, 3 Low)
- /Users/andrian/GitHub/TIDE/bug_reports/ui_hunter_report.md — 8 detailed UI/audio bugs (1 High, 6 Medium, 1 Low)

## Bugs already found (16 total) — DO NOT re-hunt these

### Combat (C1-C8)
| # | File | Lines | Severity | Bug | Fix |
|---|------|-------|----------|-----|-----|
| C1 | FateEncounterDirector.cs | 779-797 | CRITICAL | Runtime Fate boss Type never set (defaults to Ally) -> no enemies -> instant victory | Add `fateUnit.Type = CombatUnit.UnitType.Enemy;` |
| C2 | BattleManager.cs | 1586-1591 (+1784,1992,2066) | HIGH | Relationship defense multiplier multiplied instead of divided; high-bond allies take 25% MORE damage | Change `*` to `/` |
| C3 | CombatUnit.cs | 306-312 | MED | Shield block uses RoundToInt(0.5)=0 (banker's rounding) — shield consumed, 0 absorbed | Use CeilToInt or float math |
| C4 | DesireStatusEffectSet.cs | 11-15 | MED | Drowsy effect magnitude hardcoded 0f; never triggers skip | Set non-zero magnitude |
| C5 | BattleManager.cs | 2321-2344 | MED | Hit-feedback coroutines not stopped before new ones; concurrent shakes corrupt localPosition | Track per-visual Coroutine ref, StopCoroutine before new |
| C6 | BattleManager.cs | 1611-1613 | LOW | ResolveAttack log prints modifiedDamage not finalDamage | Log finalDamage |
| C7 | BattleManager.cs | 2001 | LOW | ResolveTideBreak AllEnemies log prints modifiedDmg not finalDmg | Log finalDmg |
| C8 | SelfHarmBeat.cs | 65 | LOW | !CanPlay() && !CanPlay() duplicate predicate | Single call |

### UI/Audio (U1-U8)
| # | File | Lines | Severity | Bug | Fix |
|---|------|-------|----------|-----|-----|
| U1 | AudioManager.cs | 422-448 | HIGH | SetMute doesn't restore .volume; BGM silent after un-mute | Restore volumes in SetMute |
| U2 | AudioManager.cs | 707 vs 778/787 | MED | Act volume tone-shift overwritten by CrossfadeBgm | Include actVolMult in fade target |
| U3 | TideBreakUnlockUI.cs | 53-62 | MED | OnDisable leaves popup visible & queue uncleared | Hide popup + clear queue in OnDisable |
| U4 | CeremonyIntroDirector.cs | 105-113 | MED | SkipIntroForDebug/OnDestroy stops coroutines without LockPlayerMovement(false); player frozen | Unlock before stopping |
| U5 | AncientTextLogUI.cs | 376-384 | MED | canMove restored only in Exploration state; snapshot discarded | Restore unconditionally |
| U6 | BattleHudPolishService.cs | 74-96 | MED | Flash coroutines overlap, corrupt restored color | Per-target Coroutine dict |
| U7 | BossIntroDirector.cs | 370-390 | LOW-MED | PulseAtmosphere yield break skips baseColor restore | Restore before yield break |
| U8 | MobileTouchInputManager.cs | 311-316 | LOW | Division by joystickRadius (0 -> NaN) | Mathf.Max(0.0001f, radius) |

## What still needs to be done

### Step 1: Hunt puzzle/tile/ancient-text bugs
- Spawn a teammate with role: `puzzle-hunter`
- Tell it to scan files in /Users/andrian/GitHub/TIDE/Assets/: TideManager.cs, TideTile.cs, TideBreakAbility.cs, TideBreakCatalog.cs, TideBreakData.cs, TideBreakProgressionManager.cs, TideBreakUnlockUI.cs, AncientTextInteractable.cs, AncientTextRevealDirector.cs, AncientTextLogUI.cs, AncientTextDiscoverable.cs, AncientTextSceneBootstrap.cs, AncientTextAuthoring.cs, AncientTextData.cs, ExpandedAncientTexts.cs, PuzzleHud.cs, NarrativeBeatDirector.cs, NarratorDirector.cs, DialogueTree.cs, DialogueTreeRunner.cs, DialogueUI.cs, DialogueTrigger.cs, AcceptanceConversation.cs
- Find AT LEAST 7 real bugs (null refs, ?.Invoke() missing, logic errors, grid math, tile swap, coroutine cancellation, singleton issues, etc.)
- Write report to bug_reports/puzzle_hunter_report.md

### Step 2: Hunt state/save/progression bugs
- Spawn a teammate with role: `state-hunter`
- Tell it to scan files in /Users/andrian/GitHub/TIDE/Assets/: GameStateManager.cs, GameStateSerializer.cs, WorldSaveService.cs, IslandRestorationTracker.cs, IslandRestorationState.cs, IslandRestorationHud.cs, IslandProgressionManager.cs, IslandBacktrackingManager.cs, IslandFlowController.cs, IslandConfig.cs, IslandBoatInteractable.cs, IslandArtResolver.cs, IslandVisualProfile.cs, IslandEnemyVisualProfile.cs, PerIslandContentRegistry.cs, StoryProgressionService.cs, HeroProgressionManager.cs, GearSetFactory.cs, GearSetData.cs, GearBonusStatType.cs, RelationshipTracker.cs, PartyManager.cs, PartyData.cs, PartySwapService.cs, PartySwapPanel.cs, PartySetupUI.cs, EndingEvaluator.cs, NewGamePlusService.cs, DevCheatService.cs, DevModeController.cs, DevMenuUI.cs, DifficultyModeService.cs, BossEncounterGate.cs, TravelValidationService.cs, PowerBudgetTracker.cs, PerformanceBudgetMonitor.cs, LevelingConfig.cs, GameConstants.cs, BalanceConfig.cs, HeroData.cs, HeroCharacterData.cs, BossCharacterData.cs, ElementalCharacterFactory.cs, HeroTideBreakFactory.cs
- Find AT LEAST 7 real bugs (singleton issues, save/serialization bugs, progression off-by-ones, dictionary without ContainsKey, list bounds, null checks, etc.)
- Write report to bug_reports/state_hunter_report.md

### Step 3: Dispatch puzzle-hunter and state-hunter async (parallel), await both

### Step 4: Consolidate all bugs
- Read all 4 reports: combat_hunter_report.md, ui_hunter_report.md, puzzle_hunter_report.md, state_hunter_report.md
- Goal: >= 20 total bugs. If any hunter found < 7, send it back for more.
- Prioritize: Critical -> High -> Medium -> Low

### Step 5: Fix bugs with bug-fixer
- Spawn a `bug-fixer` teammate
- Batch fixes by file (e.g. all BattleManager.cs bugs in one task)
- bug-fixer workflow for each bug:
  1. Read the full file at /Users/andrian/GitHub/TIDE/Assets/<file>
  2. Identify exact buggy text
  3. Use editor tool to replace old_text -> new_text (match exactly once)
  4. Re-read to confirm
  5. Output: FIX #N, File, Old, New, Status, Verification
- Critical (C1) first, then High (C2, U1), then Medium, then Low

### Step 6: Review fixes with reviewer
- Spawn a `reviewer` teammate
- For each fix: read file, locate fixed region, verify, output APPROVED / NEEDS WORK
- Final sweep across codebase:
  - Search for Arial.ttf in .cs files (should be LegacyRuntime.ttf)
  - Search for .Invoke( without ? before it
  - Search for try { / catch ( in production (non-test) files
  - Search for foreach over a list being .Add/.Remove'd inside the loop

### Step 7: Summarize
- Total bugs found, total fixed, any skipped (with reasons)
- Note which Unity [ContextMenu] test methods to run for verification

## Important rules
- NEVER commit or push anything. Only edit .cs files.
- Base path for all files: /Users/andrian/GitHub/TIDE/Assets/
- Read AGENTS.md for full code style rules
- No try/catch in production code
- Use LegacyRuntime.ttf, never Arial.ttf
- Events always use ?.Invoke()
- Singletons: OnEnable with destroy guard + DontDestroyOnLoad
- HP/MaxHP pattern: Health setter clamps [0, maxHp], triggers Die() at <=0
