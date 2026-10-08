# Offline source and island validation

Run all checks with .NET SDK 8 and PowerShell 7 installed:

```sh
bash scripts/verify.sh
```

Individual PowerShell entry points are available from any working directory:

```sh
pwsh -NoProfile -File scripts/validate.ps1
pwsh -NoProfile -File scripts/bug_audit.ps1
pwsh -NoProfile -File scripts/runtime_sim.ps1
```

These wrappers invoke the root implementations and propagate failure exit codes.

`validate.ps1` checks the canonical six corrupted islands (Lust, Greed, Desire,
Anger, Envy, Ego), separates the hub, resolves encounter/puzzle/scene references,
checks nine encounter slots per island and restoration budgets, and verifies
legacy aliases. Greed divides its preboss budget equally between combat and
puzzles; the other islands use 0.50 combat and 0.25 puzzles. All have 0.75 before
the boss and a 0.25 boss bucket. Lore IDs must use the authored vice prefix.

`bug_audit.ps1` checks selected known source invariants: currency persistence,
turn-order registration pruning and cached deterministic sort values, stale
encounter composition cleanup, singleton release, and explicitly allowed
runtime ScriptableObject factories. It is a narrow source-pattern audit, not
an exhaustive correctness proof.

`runtime_sim.ps1` checks source contracts and canonical asset structure. Despite
its historical filename, it does not simulate runtime scenes or invoke Unity.

The .NET harnesses link shipped gameplay sources against explicit engine/service
adapters. SceneIntegration parses all C# sources under five Editor/player/input
configurations and validates scene/YAML references. See the harness READMEs and
`PRODUCTION_READINESS_2026-10-08.md` for coverage, counts, and limitations.

The old Gluttony/Sloth/Wrath/Pride names are compatibility aliases. They must not
replace the GDD V2 roster or become a seventh corrupted island. There is no
`GluttonyIslandVerificationTest` in the current canonical test set; use
`LustIslandVerificationTest` and retain the legacy puzzle-asset checks.

Unity 6000.3.7f1 import, semantic compilation, package GUID resolution, Play Mode,
ContextMenu/Test Runner execution, and player builds remain separate required
checks. Offline success does not establish a release-ready game.
