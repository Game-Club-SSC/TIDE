Continue an in-progress bug-hunt-and-fix task on the TIDE Unity project (Unity 6 Turn-Based Fantasy RPG). Read AGENTS.md for project rules first, then resume from the state below.

## Goal
Find and fix at least 20 real bugs in the TIDE Unity project. Do NOT stop until all found bugs are fixed. Use the multi-agent team workflow.

## Current state
- Working dir: /Users/andrian/GitHub/TIDE
- Bug reports live in bug_reports/
- Full resume guide at bug_reports/RESUME.md — READ IT FIRST.

## Completed so far
- combat_hunter_report.md — 8 bugs (1 Critical, 1 High, 3 Medium, 3 Low)
- ui_hunter_report.md — 8 bugs (1 High, 6 Medium, 1 Low)
- Total: 16 bugs found, 0 fixed.
- Cleared: no Arial.ttf in .cs, all events use ?.Invoke() in audited files, HP/MaxHP sound in CombatUnit, no try/catch in combat production.

## Still TODO
1. Re-spawn 4 teammates: puzzle-hunter, state-hunter, bug-fixer, reviewer (team runtime was reset).
2. Re-run the 2 failed hunters async (continueConversation=false). Each writes report to file:
   - puzzle-hunter -> bug_reports/puzzle_hunter_report.md
   - state-hunter -> bug_reports/state_hunter_report.md
3. Consolidate all 4 reports. Need >=20 total; if any hunter finds <7, send back for more.
4. Dispatch fixes to bug-fixer async (batch by file to avoid conflicts).
5. Review with reviewer: re-read each fix, then codebase-wide sweep for remaining issues.
6. Verify in Unity per AGENTS.md.

## Role prompts

puzzle-hunter:
C# bug hunter for Unity puzzle/tile systems. Scan files in Assets/: TideManager, TideTile, TideBreak/Catalog/Data/Progression/UnlockUI, AncientText*, PuzzleHud, NarrativeBeatDirector, NarratorDirector, DialogueTree/Runner/UI/Trigger, AcceptanceConversation. Search for: null refs, missing ?.Invoke(), HP/MaxHP violations, property validation, coroutine cancellation, singleton issues, logic errors, unreachable code, missing return, variable shadowing, div by zero, missing break, Arial.ttf, foreach+modify, try/catch in production, inverted booleans, wrong Mathf.Max/Min, missing Awake defaults, grid math errors, tile swap errors. Find >=7 bugs. Write report to bug_reports/puzzle_hunter_report.md.

state-hunter:
C# bug hunter for Unity state/save/progression systems. Scan files in Assets/: GameStateManager, GameStateSerializer, WorldSaveService, IslandRestorationTracker/State/Hud, IslandProgressionManager, IslandBacktrackingManager, IslandFlowController/Config, IslandBoatInteractable, IslandArtResolver, Island(Visual/EnemyVisual)Profile, PerIslandContentRegistry, StoryProgressionService, HeroProgressionManager, GearSetFactory/Data, GearBonusStatType, RelationshipTracker, PartyManager/Data/SwapService, EndingEvaluator, NewGamePlusService, DevCheatService/DevModeController, DifficultyModeService, BossEncounterGate, TravelValidationService, PowerBudgetTracker, PerformanceBudgetMonitor, LevelingConfig, GameConstants, BalanceConfig, HeroData/HeroCharacterData/BossCharacterData, ElementalCharacterFactory, HeroTideBreakFactory. Search for: null refs, missing ?.Invoke(), HP/MaxHP violations, property validation, coroutine cancellation, singleton issues (missing OnEnable guard), logic errors, unreachable code, missing return, variable shadowing, div by zero, missing break, Arial.ttf, foreach+modify, try/catch, inverted booleans, wrong Mathf.Max/Min, missing Awake defaults, save/serialization bugs, progression off-by-ones, dictionary without ContainsKey, list index out of range. Find >=7 bugs. Write report to bug_reports/state_hunter_report.md.

bug-fixer:
Meticulous C# bug fixer for TIDE. Apply fixes for confirmed bugs. RULES: never commit/push, absolute paths in Assets/, read file fully before editing, minimal surgical edits, verify compiles conceptually. Style per AGENTS.md: PascalCase, camelCase, [SerializeField], Debug.Log($"[ClassName]..."), LegacyRuntime.ttf (NO Arial.ttf), no try/catch, guard clauses, Mathf.Clamp, ?.Invoke(), singletons OnEnable destroy guard, HP/MaxHP pattern. For each bug: read file -> identify buggy text -> editor replace old_text->new_text (match once) -> re-read. Output: FIX #N, File, Old, New, Status, Verification.

reviewer:
Senior C# code reviewer for TIDE. Review bug fixes. Criteria: fix addresses reported bug, follows AGENTS.md style, no new null risks, no broken HP/MaxHP, no event leak, balanced braces/types, no Arial.ttf, no try/catch, minimal, edge cases handled. For each fix: read file -> locate fixed region -> verify -> APPROVED/NEEDS WORK. Final sweep: search for Arial.ttf, bare .Invoke(, try/catch in production, foreach over modified list.

## The 16 already-found bugs (don't re-hunt these)
- C1 (CRIT) FateEncounterDirector.cs:779-797 — Fate boss Type never set; defaults Ally -> instant victory. Fix: fateUnit.Type = Enemy.
- C2 (High) BattleManager.cs:1586-1591(+1784,1992,2066) — defense multiplier multiplied not divided; high bonds = more damage.
- C3 (Med) CombatUnit.cs:306-312 — shield RoundToInt(0.5)=0; shield consumed for nothing. Fix: CeilToInt.
- C4 (Med) DesireStatusEffectSet.cs:11-15 — Drowsy magnitude 0f; never skips turn. Fix: non-zero magnitude.
- C5 (Med) BattleManager.cs:2321-2344 — hit-feedback coroutines not stopped; concurrent shakes corrupt positions.
- C6 (Low) BattleManager.cs:1611-1613 — log prints modifiedDamage not finalDamage.
- C7 (Low) BattleManager.cs:2001 — TideBreak AllEnemies log prints modifiedDmg not finalDmg.
- C8 (Low) SelfHarmBeat.cs:65 — !CanPlay() && !CanPlay() duplicate. Fix: single call.
- U1 (High) AudioManager.cs:422-448 — SetMute doesn't restore volume; BGM silent after un-mute.
- U2 (Med) AudioManager.cs:707 vs 778/787 — act tone-shift overwritten by crossfade.
- U3 (Med) TideBreakUnlockUI.cs:53-62 — OnDisable leaves popup visible.
- U4 (Med) CeremonyIntroDirector.cs:105-113 — SkipIntro/OnDestroy stops coroutines without unlocking player.
- U5 (Med) AncientTextLogUI.cs:376-384 — canMove restored only in Exploration state.
- U6 (Med) BattleHudPolishService.cs:74-96 — flash coroutines overlap, corrupt color.
- U7 (Low-Med) BossIntroDirector.cs:370-390 — PulseAtmosphere yield break skips baseColor restore.
- U8 (Low) MobileTouchInputManager.cs:311-316 — division by joystickRadius=0 -> NaN.

## Start now
1. Read AGENTS.md and bug_reports/RESUME.md.
2. Re-spawn the 4 teammates (use team_spawn_teammate with role prompts above).
3. Dispatch puzzle-hunter and state-hunter async (instruct each to write its report file).
4. team_await_runs, read both reports.
5. Spawn bug-fixer + reviewer, dispatch fixes batched by file, review.
6. Give final summary: total bugs found, fixed, skipped, Unity test steps.

Do NOT commit or push. Only edit .cs files.
