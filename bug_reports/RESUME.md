# TIDE Bug Hunt & Fix — Resume Guide

**Goal:** Find and fix at least 20 real bugs in the TIDE Unity project. Do not stop until all are fixed.
**Last updated:** 2026-07-19
**Status:** IN PROGRESS — 16 bugs found & documented across 2 domains; 2 domains still need hunting; 0 fixes applied yet.

---

## TL;DR — What's done vs. what's left

| Domain | Hunter | Report file | Bugs found | Status |
|--------|--------|-------------|-----------|--------|
| Combat / Battle | combat-hunter | `combat_hunter_report.md` | 8 | ✅ Report complete |
| UI / Audio / Camera | ui-hunter | `ui_hunter_report.md` | 8 | ✅ Report complete |
| Puzzle / Tide / Ancient Text | puzzle-hunter | `puzzle_hunter_report.md` | — | ❌ Failed twice (500 server error); needs re-run |
| State / Save / Progression | state-hunter | `state_hunter_report.md` | — | ❌ Failed twice (429 rate limit); needs re-run |

**Totals so far: 16 bugs found, 0 fixed.**
**Need:** Re-run puzzle-hunter + state-hunter to reach 20+, then dispatch all confirmed bugs to bug-fixer, then reviewer verifies.

---

## Team state (IMPORTANT)

The team runtime was reset during an interrupted `team_await_runs`. `team_status` now shows **no active teammates** — only the lead agent. To resume, **re-spawn the teammates**: `puzzle-hunter`, `state-hunter`, `bug-fixer`, `reviewer` (role prompts are at the bottom of this file). The `bug_reports/` directory persists on disk, so the two completed reports are safe.

---

## Bugs found so far (16) — full list with fix guidance

### From `combat_hunter_report.md` (8 bugs)

| # | File | Line(s) | Severity | Summary | Suggested fix |
|---|------|---------|----------|---------|---------------|
| C1 | `FateEncounterDirector.cs` | 779–797 | **Critical** | Runtime Fate boss never has `Type` set; defaults to `Ally` → no enemies on enemy team → `BattleManager` calls `SetVictory()` on frame 1 (Fate finale instantly won). | Add `fateUnit.Type = CombatUnit.UnitType.Enemy;` (and set `UnitName`). |
| C2 | `BattleManager.cs` | 1586–1591 (also 1784–1787, 1992–1995, 2066–2068) | **High** | Relationship defense multiplier is **multiplied** (`* relationshipDefenseMultiplier`). It returns 0.8 (low bonds) / 1.25 (high bonds), so high-bond allies take MORE damage — inverted vs. design. | Divide instead: `Mathf.RoundToInt(modifiedDamage / relationshipDefenseMultiplier)`. |
| C3 | `CombatUnit.cs` | 306–312 | Medium | Shield block uses `Mathf.RoundToInt(shieldAbsorb)`; `0.5` rounds to 0 (banker's rounding), so a fractional shield is fully consumed while absorbing nothing. | Use `Mathf.CeilToInt` for absorbed portion, or keep damage in float space. |
| C4 | `DesireStatusEffectSet.cs` | 11–15 | Medium | `CreateDrowsyEffect` hardcodes `magnitude: 0f`; Drowsy skip condition `highestDrowsy >= 1f || Random.value < highestDrowsy` is always false → Drowsy never skips a turn. | Set a non-zero magnitude (e.g. `0.5f`) or wire it to a config. |
| C5 | `BattleManager.cs` | 2321–2344 (and 1609/1640 trigger) | Medium | Hit-feedback shake/lunge coroutines started without `StopCoroutine` of a previous one; two concurrent shakes on the same transform both write `localPosition`, leaving the unit visually displaced. | Track per-visual `Coroutine` ref and `StopCoroutine` before starting new shake. |
| C6 | `BattleManager.cs` | 1611–1613 | Low | `ResolveAttack` log prints `modifiedDamage` instead of `finalDamage` (mismatches the HP delta on the same line). | Log `finalDamage` (or `hpBefore - hpAfter`). |
| C7 | `BattleManager.cs` | 2001 | Low | `ResolveTideBreak` AllEnemies log prints `modifiedDmg` instead of `finalDmg`. | Log `finalDmg` (or `hpBefore - target.HP`). |
| C8 | `SelfHarmBeat.cs` | 65 | Low | `if (!CanPlaySelfHarmSequence() && !CanPlaySelfHarmSequence())` — redundant duplicate predicate call. | Use a single `if (!CanPlaySelfHarmSequence())`. |


### From `ui_hunter_report.md` (8 bugs)

| # | File | Line(s) | Severity | Summary | Suggested fix |
|---|------|---------|----------|---------|---------------|
| U1 | `AudioManager.cs` | 422–448 (root); 927, 778/787 reinforce | **High** | `SetMute()` only toggles `AudioSource.mute`, never restores `.volume`. After muting (volume driven to 0 elsewhere) and a crossfade, un-muting leaves BGM silent. | In `SetMute`, also restore source volumes (call `ApplyVolumes()` or set `bgmSource.volume = isMuted ? 0f : bgmVolume`). |
| U2 | `AudioManager.cs` | 707 vs 778/787 | Medium | `ApplyActToneShift` sets `bgmSource.volume = BgmVolume * actVolMult`, but `CrossfadeBgm` overwrites it with bare `BgmVolume` → per-act volume tone-shift is lost. | Compute fade target as `BgmVolume * actVolMult`. |
| U3 | `TideBreakUnlockUI.cs` | 53–62 (OnDisable); 131–142 | Medium | `OnDisable` stops coroutines but never hides `popupCanvasGroup` or clears `unlockQueue`; disabling mid-display leaves popup permanently on-screen. | In `OnDisable`: `popupCanvasGroup.alpha = 0f; .gameObject.SetActive(false); unlockQueue.Clear();`. |
| U4 | `CeremonyIntroDirector.cs` | 105–113, 415–418 | Medium | `SkipIntroForDebug`/`OnDestroy` call `StopAllCoroutines()` without `LockPlayerMovement(false)` → player permanently frozen. | Call `LockPlayerMovement(false)` before stopping coroutines. |
| U5 | `AncientTextLogUI.cs` | 376–384 | Medium | On unlock, `canMove` restored only if in Exploration & not transitioning; snapshot is discarded regardless → player can be left frozen. | Restore `canMove` unconditionally, or keep the snapshot until the Exploration condition is met. |
| U6 | `BattleHudPolishService.cs` | 74–96 | Medium | `PlayCritFlash/PlayHitFlash/PlayStatusPulse` start a flash coroutine without stopping a prior one on the same target; overlapping flashes corrupt the restored color. | Track per-target `Coroutine` in a dict and `StopCoroutine` before new flash. |
| U7 | `BossIntroDirector.cs` | 370–390 | Low-Medium | `PulseAtmosphere` restores `atmosphereOverlay.color = baseColor` only after the loop; `if (skipRequested) yield break;` exits without restoring → overlay stuck at last pulse color. | Restore `baseColor` before `yield break` on skip. |
| U8 | `MobileTouchInputManager.cs` | 311–316 | Low | `normalized = clampedDistance / joystickRadius` divides by 0 if `joystickRadius == 0` → NaN movement. | `float radius = Mathf.Max(0.0001f, joystickRadius);`. |

---

## Next steps to resume

1. **Re-spawn teammates** (`puzzle-hunter`, `state-hunter`, `bug-fixer`, `reviewer`) — role prompts are at the bottom of this file.
2. **Re-run the two failed hunters** (async), instructing each to write its report to `puzzle_hunter_report.md` / `state_hunter_report.md`. Use `continueConversation: false` (fresh runs) and include the file-writing instruction from the start.
3. **Consolidate** all 4 reports into a single prioritized fix list (Critical → High → Medium → Low).
4. **Dispatch fixes** to `bug-fixer` (async, batched by file to avoid conflicts). bug-fixer reads each file fully, makes surgical edits with the `editor` tool, re-reads to confirm.
5. **Review** with `reviewer`: re-read each fixed region, run the codebase-wide sweep for `Arial.ttf`, bare `.Invoke(`, `try/catch` in production, and `foreach` over modified lists.
6. **Verify in Unity** (per AGENTS.md): open the relevant test components and run their `[ContextMenu]` methods.

### Severity-based fix priority
- **Critical (1):** C1 — Fate finale broken.
- **High (2):** C2 — combat balance inverted; U1 — BGM silent after un-mute.
- **Medium (9):** C3, C4, C5, U2, U3, U4, U5, U6, U7.
- **Low (4):** C6, C7, C8, U8.


---

## Teammate role prompts (for re-spawning)

### puzzle-hunter
> You are a C# bug hunter specializing in Unity puzzle and tile systems. Scan puzzle/tile/ancient-text C# files in /Users/andrian/GitHub/TIDE/Assets/ for real bugs. Focus: TideManager.cs, TideTile.cs, TideBreakAbility/Catalog/Data/ProgressionManager/UnlockUI.cs, AncientText*.cs, ExpandedAncientTexts.cs, PuzzleHud.cs, NarrativeBeatDirector.cs, NarratorDirector.cs, DialogueTree.cs, DialogueTreeRunner.cs, DialogueUI.cs, DialogueTrigger.cs, AcceptanceConversation.cs. Look for: null refs, missing ?.Invoke(), HP/MaxHP pattern violations, property validation missing, coroutine cancellation issues, singleton issues, logic errors (off-by-one, wrong comparison operator), unreachable code, missing return/early-out, variable shadowing, division by zero, missing break in switch, Arial.ttf references, foreach over list being modified, try/catch in production, inverted boolean conditions, wrong Mathf.Max/Min, missing Awake boundary defaults, coordinate/grid math errors, tile swap logic errors. For EACH bug output: BUG #N, File (absolute path), Line, Category, Severity, Description, Suggested Fix, Code Snippet. Find at least 7 real bugs. Be precise — cite exact line numbers and quote the buggy code. Verify each bug by reading the actual code. Do not invent bugs. When done, write the full report to /Users/andrian/GitHub/TIDE/bug_reports/puzzle_hunter_report.md using the editor tool.

### state-hunter
> You are a C# bug hunter specializing in Unity state management, save systems, progression, and singleton services. Scan state/save/progression C# files in /Users/andrian/GitHub/TIDE/Assets/ for real bugs. Focus: GameStateManager.cs, GameStateSerializer.cs, WorldSaveService.cs, IslandRestorationTracker/State/Hud.cs, IslandProgressionManager.cs, IslandBacktrackingManager.cs, IslandFlowController.cs, IslandConfig.cs, IslandBoatInteractable.cs, IslandArtResolver.cs, IslandVisualProfile.cs, IslandEnemyVisualProfile.cs, PerIslandContentRegistry.cs, StoryProgressionService.cs, HeroProgressionManager.cs, GearSetFactory/Data.cs, GearBonusStatType.cs, RelationshipTracker.cs, PartyManager/Data/SwapService.cs, EndingEvaluator.cs, NewGamePlusService.cs, DevCheatService.cs, DevModeController.cs, DifficultyModeService.cs, BossEncounterGate.cs, TravelValidationService.cs, PowerBudgetTracker.cs, PerformanceBudgetMonitor.cs, LevelingConfig.cs, GameConstants.cs, BalanceConfig.cs, HeroData.cs, HeroCharacterData.cs, BossCharacterData.cs, ElementalCharacterFactory.cs, HeroTideBreakFactory.cs. Look for: null refs, missing ?.Invoke(), HP/MaxHP pattern violations, property validation missing, coroutine cancellation issues, singleton issues (missing OnEnable guard), logic errors, unreachable code, missing return/early-out, variable shadowing, division by zero, missing break in switch, Arial.ttf references, foreach over list being modified, try/catch in production, inverted boolean conditions, wrong Mathf.Max/Min, missing Awake boundary defaults, save/serialization bugs, progression gate off-by-ones, dictionary lookup without ContainsKey, list index out of range risks. For EACH bug output: BUG #N, File (absolute path), Line, Category, Severity, Description, Suggested Fix, Code Snippet. Find at least 7 real bugs. Be precise — cite exact line numbers and quote the buggy code. Verify each bug by reading the actual code. Do not invent bugs. When done, write the full report to /Users/andrian/GitHub/TIDE/bug_reports/state_hunter_report.md using the editor tool.

### bug-fixer
> You are a meticulous C# bug fixer for a Unity 6 Turn-Based Fantasy RPG (TIDE). Apply fixes for confirmed bugs reported by bug hunters. RULES: never commit/push (only edit files); use absolute paths in /Users/andrian/GitHub/TIDE/Assets/; read each file fully before editing; make minimal surgical edits; verify each fix compiles conceptually. Style: PascalCase classes/methods, camelCase private fields, [SerializeField] for inspector fields, [Header]/[Tooltip]/[Range], Debug.Log($"[ClassName] ..."), LegacyRuntime.ttf (NEVER Arial.ttf), no try/catch in production, guard clauses, Mathf.Clamp, events use ?.Invoke(), singletons use OnEnable() with destroy guard + DontDestroyOnLoad, HP/MaxHP pattern (MaxHP setter Mathf.Max(1,value) + clamp HP; HP setter clamps to [0,maxHp] + triggers Die() at <=0). For each bug: read file, identify exact buggy text, use editor tool to replace old_text with new_text (match once), re-read to confirm. Output: FIX #N, File, Old, New, Status, Verification. Prioritize Critical > High > Medium > Low. Re-read before editing (line numbers may have shifted). Do not stop until all assigned bugs are fixed or skipped with a documented reason.

### reviewer
> You are a senior C# code reviewer for TIDE (Unity 6). Review bug fixes from bug-fixer for correctness and that no new bugs were introduced. Criteria: fix addresses the reported bug; follows AGENTS.md style; no new null risks; no broken HP/MaxHP invariants; no new event subscription leak; braces/semicolons/types balanced; no Arial.ttf; no try/catch in production; minimal; edge cases handled. For each fix: read the file at absolute path, locate the fixed region, verify, output REVIEW #N - File - APPROVED / NEEDS WORK (with reason). Final sweep: search codebase for remaining `Arial.ttf`, `.Invoke(` without `?`, `try {`/`catch (` in non-test production files, `foreach` over a list being modified inside the loop. Report any remaining issues. Be adversarial.

---

## Notes
- The two completed reports confirm **no `Arial.ttf` references in any `.cs` file** (only in docs), **all event invocations use `?.Invoke()`** in the audited files, and **no `try/catch` in combat production files**.
- The combat hunter also confirmed `CombatUnit` HP/MaxHP setters correctly clamp to `[0, maxHp]` and trigger `Die()` at `hp <= 0 && isAlive`, with `Awake()` boundary defaults. So the HP/MaxHP pattern is largely sound — bugs are elsewhere (logic, multipliers, rounding, coroutines).
- Both failed hunter runs were infrastructure errors (500 inference abort, 429 rate limit), not logic failures — a simple re-run should succeed.
- The `bug_reports/` directory is NOT tracked by git (it's a working folder). If you want to keep these reports, consider whether to commit them or leave as local notes.

