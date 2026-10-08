# TIDE production-readiness implementation pass — 2026-10-08

The repository is substantially stronger after this pass, with implemented
gameplay fixes and repeatable offline regression coverage. It is not yet proven
ready to ship. Unity 6000.3.7f1 was unavailable: no Unity import, full engine
semantic compilation, Play Mode journey, profiler capture, or player build ran.

Work was performed on branch `work`, after inspecting the initially clean git
state and pulling the existing branch (already up to date). No commits, pushes,
deployments, or messages to external collaborators were made. Six independent
GPT-6.1 Sol workstreams investigated combat, puzzles, progression, persistence,
narrative, and integration. Owners performed five review passes; the integration
reviewer independently examined the final cross-system changes. Findings from
those reviews were fixed and relevant checks rerun.

`Game Design Document V2 FINAL.md` was preserved. The canonical corrupted-island
set is Lust, Greed, Desire, Anger, Envy, Ego, with the hub separate. The shipped
registry's sequence is unchanged. Old Gluttony/Sloth/Wrath/Pride names remain
compatibility aliases and legacy puzzle assets remain supported.

## Findings and implemented changes

### Player journey, restoration, and travel

- New Game could enter an island instead of the intended hub. A cold Title boot
  lacked services needed to restore the saved party, story, affinity, and NG+
  state. Bootstrap now creates those services and uses a runtime copy of the
  default party resource, preserving authored ScriptableObjects.
- Boat destination encounters could start before the transition finished,
  creating competing scene coroutines. Flow now starts after the travel fade;
  loaded destination position is retained.
- Unsolved puzzle dismissal and tile-guard victories could advance a full puzzle
  encounter. Explicit solved/guard handling prevents either from granting its
  restoration reward.
- Flee and failed-combat retry could spend another encounter reservation or
  reopen combat on the next bootstrap frame. Flee pauses flow, retry preserves
  its reservation, and scene bootstrap is marked handled before deferred flow.
- Repeated terminal combat callbacks could duplicate rewards. GSM reserves the
  result once, suppresses intermediate autosaves during reward assembly, and
  saves the reward/restoration receipt before the visual victory delay.
- Restoration loads preserve explicit modern combat/puzzle/boss buckets and
  migrate legacy boss progress. Ordinary combat cannot unlock the next island;
  completing an island unlocks it without forcing immediate travel. Unknown
  boat destinations fail rather than falling back to the first island.
- Loaded puzzle grids are bounded to 256 rows/columns, retain zero/sealed markers,
  and clear old transient callbacks and encounter state. Null legacy enemy
  composition arrays fail closed.

### Save/load and New Game+

- The original loose JSON acceptance could admit corrupt or incompatible data.
  `SaveJsonValidator` now checks bounded strict JSON grammar, root structure,
  duplicate keys, schema compatibility, maximum depth 64, and maximum length
  4 MiB. Legacy absent-schema saves are recognized distinctly.
- V1 and V2 saves now retain validated backup generations. Failed writes do not
  replace a good backup with corrupt data; repeated identical autosaves retain
  the previous distinct generation. Missing/corrupt primaries can recover.
- Save-availability checks formerly repaired storage and emitted load effects.
  `TryPeekJson` and GSM's availability predicates are read-only. Explicit loads
  may attempt repair; a repair failure does not discard the usable backup.
- Future-schema roots are preserved against downgrade/overwrite. GSM validates
  its serialized payload before storing it. PlayerPrefs quota errors are handled
  at the storage boundary rather than escaping through pause-menu actions.
- `TrySaveWorldState` reports success/failure. Pause feedback no longer claims a
  save succeeded when storage rejected it. Saves during loading, transitions,
  and reward assembly are suppressed.
- Root snapshots now include relationship state and NG+ run metadata alongside
  hero, party, gear, inventory, Tide Break, story, dialogue, and restoration state.
- NG+ completion/start operations are idempotent and sanitize stale numeric
  metadata. The title exposes NG+ for a completed saved run. Starting a loop
  carries the earned build to a fresh hub, batches mutations into one save, and
  retains the completed root for rollback if that save fails. Normal New Game
  resets run state while retaining account completion history.
- Party restore preserves three active/two reserve slots and explicit holes,
  prevents duplicate heroes, and handles unknown IDs without stealing another
  known saved slot. The Unity persistence test now verifies real non-default
  party and XP state instead of only rechecking an in-memory object.

### Combat and rewards

- Clashes now obey speed-queue order rather than executing all mutually targeted
  pairs first. Dead, stunned, support-only, foreign-roster, and already-acted
  targets cannot produce illegal extra clash actions.
- Turn-start statuses prepare once; end-turn expiry preserves one-turn buffs
  and drowsy through the action. Per-recipient copies prevent shared duration
  mutation; newly applied stun and refreshed statuses are honored. Revive clears
  old stun/swap state.
- Skill/Tide Break clashes use authored power and effects, check affordability,
  spend resources once, and respect a previously consumed shared gauge. Enemy
  area Tide Breaks hit opponents. Simultaneous lethal clashes apply both hits
  and resolve a consistent terminal result exactly once.
- AI skips unaffordable skills and chooses supported affordable alternatives.
  Envy covets before ordinary skill selection and preserves its authored area
  damage modifier. The HUD rejects skills and Tide Breaks not owned/unlocked.
- Battle start prunes detached unit registration and stale action/QTE state while
  preserving deterministic cross-faction ties. Pending QTE cancellation/re-enable
  recovery is covered by an adapter regression; real teardown timing remains an
  Editor check.
- Generated encounters bind the difficulty singleton when configured and use the assigned valid
  vice profile or the existing default weights for the canonical vice. No new
  boss canon or encounter content was invented.
- `CombatMath` bounds damage/status/scaling arithmetic and rejects non-finite
  values. NG+ enemy stats scale once from base data, including the legacy spawn
  path; enemy XP is not scaled again.
- Battle XP combines existing difficulty and NG+ multipliers once before active/
  reserve distribution. Currency, cosmetic funds, reserve XP, gear XP, and
  leveling-curve arithmetic saturate safely. Capped hero XP is a successful
  no-op so durable dialogue grants cannot retry it in a later run.
- Legacy equipped gear is migrated before inventory capture, and restoring a
  snapshot cannot equip one physical item to two heroes. Party application and
  combat relationships use stable HeroId values rather than display names.

### Tide puzzles and authored boards

- Capturing/dismissing a board cancels carried Tide instead of losing mass.
  Resume preserves saved zero/sealed slots; reset restores the authored initial
  grid; undo restores move, decay, consumption, and economy state. Move/reset/
  undo refresh the runtime snapshot.
- Guard battles retain board progress, unlock only their authored sealed tile,
  and return to the puzzle. Sealed tile APIs cannot mutate permanent walls.
- Value, movement, traversal/corner, pickup/capacity, win-condition, dimension,
  and overflow checks were expanded. An entirely sealed board cannot win.
  Puzzle HUD exposes Z undo/cancel.
- Five confirmed impossible boards received minimal numeric corrections:

| Puzzle asset | Correction |
| --- | --- |
| `puzzle_anger_p4` | First tile 9 → 10 |
| `puzzle_ego_p4` | First tile 8 → 9 |
| `puzzle_greed_p4` | Seventh tile 7 → 8 |
| `puzzle_lust_p4` | First tile 8 → 10 |
| `puzzle_gluttony_p2` | Ninth tile 8 → 9; instability threshold 2 → 3 |

All 41 authored assets produce a legal solution witness through linked
`TideManager` input methods after their explicit guard unlocks. Search is bounded
to 40,000 states per board. A witness establishes a source-level route to a
solution; it does not prove every reachable player state recoverable or that
runtime presentation matches the source.

### Narrative and relationships

- Durable dialogue effect keys coalesce repeated deferred entries and reserve
  the ledger before reentrant reward/autosave callbacks. Latest saved flags
  replace stale deferred writes. Choice bonds persist and duplicate clicks
  cannot award them twice.
- Dialogue/tree cancellation releases pending state without completion rewards.
  NPC one-shot triggers and story beats become completed only after their own
  dialogue finishes, allowing interrupted conversations to retry. Completion
  cleanup preserves a new conversation started by a listener.
- Null authored nodes/effects/conditions/choices are guarded and silent routing
  cycles terminate within a fixed bound. Explicit hero-pair bond effects support
  gains and conflict losses.
- Story/affinity snapshots replace prior-run state, validate enums and bounded
  values, and reject corrupt legacy affinity sidecars. Completion saves occur
  after listener callbacks so NPC bonds and quest effects survive reload.
- Diagnostic ending evaluation now agrees with V2's per-island threshold-history
  rule and returns unresolved without game state. Live ending resolution remains
  in GSM; this pass did not rewrite authored endings.

### UI, input, performance, and verification infrastructure

- Controller code previously treated joystick key names as InputManager button
  names, which could throw. It now uses the Input System when enabled and actual
  joystick KeyCodes/configured axes for the legacy fallback. Input samples before
  consumers, sees devices beyond the first joystick slot, resets disconnect
  channels, and supports D-pad/stick edge navigation. Keyboard input no longer
  activates controller prompts.
- Legacy connection polling allocates at most once per half second instead of
  twice every frame. Pause submenu object searches occur on input rather than
  every frame. Pause executes before submenu handlers to avoid double Escape
  handling and refuses to open over active dialogue.
- Menu navigation skips disabled/hidden Continue/NG+ buttons. Pause and load
  confirmation select a usable control; the load modal disables underlying menu
  input and cancel restores focus. New Unity UI regressions cover these cases.
- NUnit verification scripts are Editor-guarded so release/development/mobile
  players do not import test code from the flat gameplay assembly.
- Added actual-source .NET harnesses for persistence, progression, combat,
  narrative, hero/gear, input, and scene/source integration. Puzzle verification
  now links shipped gameplay instead of depending on an unavailable Unity DLL.
  Existing hero/gear and combat/adversarial suites execute with visible adapters.
- PowerShell checks now use the GDD roster and actual authored lore/budgets.
  Script wrappers work from another directory and propagate nonzero failures.
  `runtime_sim.ps1` is explicitly a source-contract check, not gameplay execution.
- CI runs the full offline suite and a separate promotional-site job. New script
  metadata is present, harness projects are trackable, scratch/build outputs are
  ignored, and README/static verification docs describe the actual coverage.
- The promotional site's missing Cloudflare type declarations/database binding
  prevented TypeScript checks. Added pinned official Workers types, an optional
  DB binding declaration, `npm run typecheck`, and repeatable CI checks. Existing
  site content was preserved.

## Verification performed

The final detailed log accompanies the deliverable. SDK 8.0.408, PowerShell
7.4.13, Node 24.19, and clean npm dependency restores were used. Harness source
files compile under .NET against explicit adapters; the all-source parser is
syntax/configuration verification, not Unity semantic compilation.

| Check | Result |
| --- | --- |
| Phone security harness | 157 checks passed |
| Persistence harness | 2,118 assertions passed, including 2,000 deterministic parser mutations and storage fault cases |
| Progression harness | 20 production-source checks passed with actual six-island YAML budgets |
| Combat harness | 99 tests passed, zero failures |
| Puzzle harness | 1,822 checks passed; 41/41 authored legal solution witnesses |
| Narrative harness | 143 checks passed |
| Hero/gear harness | 176 assertions passed, including three existing verification suites |
| Input harness | 18 legacy and 17 Input System checks passed |
| Scene/source integration | 274 Editor/261 player sources, five configurations; 687 GUIDs; five enabled scenes; 293 serialized gameplay files; zero errors/warnings |
| `scripts/validate.ps1` | 224/224 passed |
| `scripts/bug_audit.ps1` | 24/24 passed |
| `scripts/runtime_sim.ps1` | 50/50 source contracts passed |
| PowerShell negative controls | All three wrappers exited 1 on deliberately broken scratch assets/source from a different working directory |
| Combat negative control | Restoring free clash MP and incorrect enemy area faction targeting failed four regression cases |
| Integration negative controls | Nine malformed source/scene/reference categories rejected during reviewer verification |
| Promotional site | `npm ci`, typecheck, build, 2/2 rendered-HTML tests, and lint passed |
| Diff hygiene | `git diff --check` passed; canonical GDD unchanged; no generated harness output or credentials included |

Warnings remain visible: offline compiler warnings for Inspector-injected fields
and unused existing test fields; an unused narrative threshold field; website
`@next/no-img-element` performance warning; vinext route classification warning;
deprecated transitive esbuild-kit installation notices. These were reviewed and
not suppressed. None establishes a measured runtime failure; the website image
warning and actual engine performance still need platform measurement.

## Remaining risks and production priorities

1. **Release gate: real Unity integration.** Full GSM/bootstrap scene orchestration
   is not executed by the adapters. General type/member/Unity API binding,
   lifecycle order, serialized imports, package GUID resolution, callbacks during
   real QTE/scene teardown, coroutine scheduling, physics, rendering, and player
   packaging remain unproven. Obtain the exact Editor and run the plan below.
2. **Save durability.** PlayerPrefs read-back is not proof of disk-atomic writes
   or power-loss durability. Backups reduce recoverable failures but multiple
   platform keys/sidecars are not a transactional journal. Test platform quotas,
   crash timing, cloud synchronization, and migration with real saved files;
   consider an atomic file/journal backend for release.
3. **Narrative/content completion.** Hero-pair scenes, field banter, and variation
   dialogue still lack sufficient authored/wired production content.
   `RelationshipVariationDialogue` has no production caller, and affinity APIs
   have no gameplay mutation callers. Combat bond identity is repaired, but
   most authored effects still grant hero-to-player bonds. Build a complete
   relationship/ending playthrough before claiming narrative readiness.
4. **Boss and balance validation.** Existing vice profiles/defaults are wired;
   this does not establish that each boss has distinct finished mechanics or
   satisfying difficulty. Play every element/vice/boss, inspect economy and
   NG+ scaling, and tune against recorded sessions. DifficultyModeService has no
   production creation/selection caller in the inspected path; Story/Hardcore
   integration works when the service is supplied, but a player-facing selector,
   bootstrap and saved difficulty remain production work.
5. **UX/accessibility/platform quality.** No controller hardware, touch device,
   keyboard/mouse Play Mode, resolution/safe-area/legibility inspection, audio
   listening, accessibility audit, or frame/GC profile ran. Measure those on
   supported targets. Finish onboarding, presentation and feedback for one
   island before expanding art/content across the rest.
6. **Automation after Editor access.** Capture the Unity matrix as automated
   Test Runner/batch-mode coverage, build a development and release player, and
   retain a smoke journey and save-migration fixture library in CI. Current CI
   changes were verified locally; GitHub Actions execution itself was not run.

## Required Unity/manual verification

Use Unity **6000.3.7f1**, a disposable verification project/user save, and the
project's package manifest. First import cleanly and inspect Console and build
compilation for Editor, development, release, and the intended mobile target.
The enabled scenes are TitleScene, StartingHubScene, level_1, PuzzleScene, and
CombatScene. Confirm their package-owned references resolve after import.

Run `PostDeferralVerticalSliceRegressionRunner` → **Run Post-Deferral Vertical
Slice Regression Matrix**, now including combat readiness, journey safety,
save-restore invariants, and narrative replay. Also run the existing individual
island/restoration/boss/travel/developer-state suites; `HeroProgressionTest`,
`GearSystemTest`, `GearProgressionTest`; `SavePersistenceTest`; the combat
adversarial runner; and Edit Mode UI wiring tests. A Gluttony island suite is not
present in the canonical roster; verify its compatibility alias/legacy assets
through the current data checks. Do not record these Editor suites as passed
from the offline results.

Then exercise this real journey with Console capture and a restarted player:

1. From clean Title, choose each main-hero element, start New Game, arrive at the
   hub, complete ceremony/onboarding, and depart to the first unlocked island.
2. Complete ordinary encounters and puzzles; verify restoration advances once,
   boss remains unavailable below 75%, and becomes available at 75%. Retry defeat,
   flee, cancel an unsolved puzzle, and repeat trigger/return inputs.
3. On a puzzle, take/cancel Tide, place/undo, reset, close/reopen, and save/reload.
   Fight a sealed-tile guard and confirm board progress returns without counting
   the guard as the full puzzle reward. Inspect animation, labels and controls.
4. Play all five elements, neutral QTE pass/fail/cancellation/re-enable, statuses,
   support/area skills, enemy area Tide Breaks, empty MP, dead/revived actors,
   simultaneous lethal outcomes, difficulty modes, and each island's boss.
5. Defeat a boss: verify 100%, one reward, next-island unlock, boat selection,
   arrival after fade, next encounter start, and return to previous unlocked areas.
6. Save and quit/restart before an encounter, after a puzzle move/guard, at 75%,
   immediately after rewards/boss, after travel, and after an ending. Confirm
   scene, party holes/order, XP, gear ownership, funds, restoration, story flags,
   relationship values and completed receipts. Try corrupt/missing primary,
   valid backup, future schema, quota failure and interrupted save on target.
7. Complete both ending routes and NG+. Verify carried build, fresh hub/story,
   base-derived enemy scaling, once-applied XP, second restart, and failed NG+
   save rollback. Start ordinary New Game afterward and confirm run reset.
8. Repeat title/pause/load/cancel/focus with mouse, keyboard, physical controller
   hot-plug and touch. Check resolutions, safe areas, readable text, audible cues,
   pause over submenus/dialogue, device performance and allocation profiles.

Keep evidence of failures and fixes. Passing the above in Editor and a built
player is the next concrete requirement before a production-readiness claim.

## Complete file inventory

145 existing files modified; 51 new files. No files staged, committed or pushed.

| Status | File |
| --- | --- |
| Modified | `.github/workflows/verify.yml` |
| Modified | `.gitignore` |
| Modified | `Assets/AcceptanceConversationTest.cs` |
| Modified | `Assets/AdversarialCombatChallengerRunner.cs` |
| Modified | `Assets/AmbientAudioValidationTest.cs` |
| Modified | `Assets/AncientTextLogUITest.cs` |
| Modified | `Assets/AncientTextRevealDirectorTest.cs` |
| Modified | `Assets/AngerIslandVerificationTest.cs` |
| Modified | `Assets/ArtAssetPassTest.cs` |
| Modified | `Assets/AudioIntegrationTest.cs` |
| Modified | `Assets/AudioManagerTest.cs` |
| Modified | `Assets/AudioMixingInvariantTest.cs` |
| Modified | `Assets/BadEndingReactionsTest.cs` |
| Modified | `Assets/BalancePassTest.cs` |
| Modified | `Assets/BattleFlowTestSuite.cs` |
| Modified | `Assets/BattleManager.cs` |
| Modified | `Assets/BgmContentValidationTest.cs` |
| Modified | `Assets/BoatTravelRegressionTest.cs` |
| Modified | `Assets/BoatTravelTest.cs` |
| Modified | `Assets/BossArtContentValidationTest.cs` |
| Modified | `Assets/BossEncounterGateTest.cs` |
| Modified | `Assets/BossIntroDirectorTest.cs` |
| Modified | `Assets/CharacterArtTest.cs` |
| Modified | `Assets/CombatBalanceInvariantTest.cs` |
| Modified | `Assets/CombatSceneBootstrap.cs` |
| Modified | `Assets/CombatUnit.cs` |
| Modified | `Assets/CombatUnitTestSuite.cs` |
| Modified | `Assets/CritStatsTest.cs` |
| Modified | `Assets/CutsceneStoryboardValidationTest.cs` |
| Modified | `Assets/DesireIslandVerificationTest.cs` |
| Modified | `Assets/DesireStatusEffectTestSuite.cs` |
| Modified | `Assets/DevGodModeStateTest.cs` |
| Modified | `Assets/DialogueContentTest.cs` |
| Modified | `Assets/DialogueEffectVerificationTest.cs` |
| Modified | `Assets/DialogueSystem.cs` |
| Modified | `Assets/DialogueTreeRunner.cs` |
| Modified | `Assets/DialogueTreeRunnerTest.cs` |
| Modified | `Assets/DialogueTrigger.cs` |
| Modified | `Assets/DialogueUI.cs` |
| Modified | `Assets/Docs/REPO_UNDERSTANDING.md` |
| Modified | `Assets/Docs/StaticIslandValidation.md` |
| Modified | `Assets/EgoIslandVerificationTest.cs` |
| Modified | `Assets/ElementMatchupTest.cs` |
| Modified | `Assets/ElementScalingBalanceTest.cs` |
| Modified | `Assets/EndingEvaluator.cs` |
| Modified | `Assets/EndingEvaluatorTest.cs` |
| Modified | `Assets/EnemyDataTestSuite.cs` |
| Modified | `Assets/EnemySpriteContentValidationTest.cs` |
| Modified | `Assets/EnvironmentArtTest.cs` |
| Modified | `Assets/EnvyIslandVerificationTest.cs` |
| Modified | `Assets/EnvyMirrorTestSuite.cs` |
| Modified | `Assets/GameStateManager.cs` |
| Modified | `Assets/GameStateManagerRefactorTest.cs` |
| Modified | `Assets/GameStateManagerSingletonVerificationTest.cs` |
| Modified | `Assets/GameSystemsTest.cs` |
| Modified | `Assets/GamepadInputManager.cs` |
| Modified | `Assets/GearProgressionTest.cs` |
| Modified | `Assets/GearSetFactoryTest.cs` |
| Modified | `Assets/GearSystemTest.cs` |
| Modified | `Assets/GreedEconomyTestSuite.cs` |
| Modified | `Assets/GreedIslandVerificationTest.cs` |
| Modified | `Assets/HeroDataTestSuite.cs` |
| Modified | `Assets/HeroProgressionManager.cs` |
| Modified | `Assets/HeroProgressionTest.cs` |
| Modified | `Assets/HeroTideBreakFactoryTest.cs` |
| Modified | `Assets/HubVerificationTest.cs` |
| Modified | `Assets/IslandBacktrackingManagerTest.cs` |
| Modified | `Assets/IslandConfig.cs` |
| Modified | `Assets/IslandContentVerificationTest.cs` |
| Modified | `Assets/IslandFlowController.cs` |
| Modified | `Assets/IslandFlowControllerTest.cs` |
| Modified | `Assets/IslandProgressionTravelTest.cs` |
| Modified | `Assets/IslandRestorationStateEdgeTest.cs` |
| Modified | `Assets/IslandRestorationTracker.cs` |
| Modified | `Assets/IslandVisualProfileTest.cs` |
| Modified | `Assets/IsometricPlayerMovementTestSuite.cs` |
| Modified | `Assets/LevelingConfig.cs` |
| Modified | `Assets/LocalizationServiceTest.cs` |
| Modified | `Assets/LustIslandVerificationTest.cs` |
| Modified | `Assets/MobileBattleTapTest.cs` |
| Modified | `Assets/MobileTouchControllerTest.cs` |
| Modified | `Assets/MobileTouchInputInvariantTest.cs` |
| Modified | `Assets/MomentumState.cs` |
| Modified | `Assets/NarrativeBeatDirector.cs` |
| Modified | `Assets/NarrativeBeatTest.cs` |
| Modified | `Assets/NarrativeSystemsTest.cs` |
| Modified | `Assets/NewGamePlusService.cs` |
| Modified | `Assets/OverworldEnemyTestSuite.cs` |
| Modified | `Assets/PartyManager.cs` |
| Modified | `Assets/PartySetupTestSuite.cs` |
| Modified | `Assets/PartySwapServiceTest.cs` |
| Modified | `Assets/PauseMenuUI.cs` |
| Modified | `Assets/PerformanceBudgetMonitorTest.cs` |
| Modified | `Assets/PlayerGearInventoryTestSuite.cs` |
| Modified | `Assets/PlayerSpriteVisualTestSuite.cs` |
| Modified | `Assets/PostDeferralVerticalSliceRegressionRunner.cs` |
| Modified | `Assets/PowerBudgetTrackerTest.cs` |
| Modified | `Assets/PreBossDialogueFlowTest.cs` |
| Modified | `Assets/ProceduralAudioBuilderTest.cs` |
| Modified | `Assets/PuzzleData.cs` |
| Modified | `Assets/PuzzleHud.cs` |
| Modified | `Assets/PuzzleWinConditionTest.cs` |
| Modified | `Assets/RelationshipCombatEffects.cs` |
| Modified | `Assets/RelationshipTracker.cs` |
| Modified | `Assets/Resources/Puzzles/puzzle_anger_p4.asset` |
| Modified | `Assets/Resources/Puzzles/puzzle_ego_p4.asset` |
| Modified | `Assets/Resources/Puzzles/puzzle_gluttony_p2.asset` |
| Modified | `Assets/Resources/Puzzles/puzzle_greed_p4.asset` |
| Modified | `Assets/Resources/Puzzles/puzzle_lust_p4.asset` |
| Modified | `Assets/RestorationThresholdGateTest.cs` |
| Modified | `Assets/RestorationTrackerTest.cs` |
| Modified | `Assets/SavePersistenceTest.cs` |
| Modified | `Assets/SaveRestoreInvariantTest.cs` |
| Modified | `Assets/SfxContentValidationTest.cs` |
| Modified | `Assets/SkillData.cs` |
| Modified | `Assets/StatusEffect.cs` |
| Modified | `Assets/StealthMechanicsTest.cs` |
| Modified | `Assets/StoryProgressionService.cs` |
| Modified | `Assets/StoryProgressionTest.cs` |
| Modified | `Assets/TideBreakDataTest.cs` |
| Modified | `Assets/TideBreakTargetingTest.cs` |
| Modified | `Assets/TideBreakUnlockTest.cs` |
| Modified | `Assets/TideManager.cs` |
| Modified | `Assets/TideMovementTest.cs` |
| Modified | `Assets/TideTile.cs` |
| Modified | `Assets/TitleScreenUI.cs` |
| Modified | `Assets/TravelValidationService.cs` |
| Modified | `Assets/TravelValidationTest.cs` |
| Modified | `Assets/UIUXPolishTest.cs` |
| Modified | `Assets/UIWiringVerificationTest.cs` |
| Modified | `Assets/UiArtContentValidationTest.cs` |
| Modified | `Assets/VerticalSliceRegressionRunnerTest.cs` |
| Modified | `Assets/WorldSaveService.cs` |
| Modified | `ProjectSettings/InputManager.asset` |
| Modified | `README.md` |
| Modified | `Verification/PuzzleIslandHarness/PuzzleIslandHarness.csproj` |
| Modified | `bug_audit.ps1` |
| Modified | `promo-site/.gitignore` |
| Modified | `promo-site/README.md` |
| Modified | `promo-site/package-lock.json` |
| Modified | `promo-site/package.json` |
| Modified | `promo-site/tsconfig.json` |
| Modified | `runtime_sim.ps1` |
| Modified | `scripts/verify.sh` |
| Modified | `validate.ps1` |
| Added | `Assets/CombatMath.cs` |
| Added | `Assets/CombatMath.cs.meta` |
| Added | `Assets/CombatReadinessRegressionTest.cs` |
| Added | `Assets/CombatReadinessRegressionTest.cs.meta` |
| Added | `Assets/Docs/PRODUCTION_READINESS_2026-10-08.md` |
| Added | `Assets/Docs/PRODUCTION_READINESS_2026-10-08.md.meta` |
| Added | `Assets/EnemyCombatStatScaler.cs` |
| Added | `Assets/EnemyCombatStatScaler.cs.meta` |
| Added | `Assets/JourneySafetyRegressionTest.cs` |
| Added | `Assets/JourneySafetyRegressionTest.cs.meta` |
| Added | `Assets/MenuNavigationUtility.cs` |
| Added | `Assets/MenuNavigationUtility.cs.meta` |
| Added | `Assets/NarrativeReplayVerificationTest.cs` |
| Added | `Assets/NarrativeReplayVerificationTest.cs.meta` |
| Added | `Assets/SaveJsonValidator.cs` |
| Added | `Assets/SaveJsonValidator.cs.meta` |
| Added | `Verification/CombatHarness/AssertAdapters.cs` |
| Added | `Verification/CombatHarness/CombatHarness.csproj` |
| Added | `Verification/CombatHarness/Program.cs` |
| Added | `Verification/CombatHarness/README.md` |
| Added | `Verification/CombatHarness/UnityAdapters.cs` |
| Added | `Verification/HeroGearHarness/Adapters.cs` |
| Added | `Verification/HeroGearHarness/HeroGearHarness.csproj` |
| Added | `Verification/HeroGearHarness/Program.cs` |
| Added | `Verification/InputHarness/InputHarness.csproj` |
| Added | `Verification/InputHarness/Program.cs` |
| Added | `Verification/InputHarness/UnityAdapters.cs` |
| Added | `Verification/NarrativeHarness/NarrativeHarness.csproj` |
| Added | `Verification/NarrativeHarness/Program.cs` |
| Added | `Verification/NarrativeHarness/README.md` |
| Added | `Verification/NarrativeHarness/UnityAdapters.cs` |
| Added | `Verification/PersistenceHarness/PersistenceHarness.csproj` |
| Added | `Verification/PersistenceHarness/Program.cs` |
| Added | `Verification/PersistenceHarness/README.md` |
| Added | `Verification/PersistenceHarness/UnityStubs.cs` |
| Added | `Verification/ProgressionHarness/Program.cs` |
| Added | `Verification/ProgressionHarness/ProgressionHarness.csproj` |
| Added | `Verification/ProgressionHarness/README.md` |
| Added | `Verification/ProgressionHarness/UnityShims.cs` |
| Added | `Verification/PuzzleIslandHarness/README.md` |
| Added | `Verification/PuzzleIslandHarness/SourceProgram.cs` |
| Added | `Verification/PuzzleIslandHarness/UnityShims.cs` |
| Added | `Verification/README.md` |
| Added | `Verification/SceneIntegration/.gitignore` |
| Added | `Verification/SceneIntegration/Program.cs` |
| Added | `Verification/SceneIntegration/README.md` |
| Added | `Verification/SceneIntegration/SceneIntegration.csproj` |
| Added | `promo-site/worker-env.d.ts` |
| Added | `scripts/bug_audit.ps1` |
| Added | `scripts/runtime_sim.ps1` |
| Added | `scripts/validate.ps1` |
