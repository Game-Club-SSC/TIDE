# Offline verification

Install .NET SDK 8 and PowerShell 7, then run `bash scripts/verify.sh` from any
working directory. The GitHub Actions source-verification job runs this command.
If PowerShell or Unity is absent, the script explicitly reports skipped checks.
Treat those skips as coverage limits, not passing results.

| Harness | Production sources and coverage | Engine boundary |
| --- | --- | --- |
| PhoneSecurityHarness | Existing phone authentication/privacy regressions | Deterministic adapters |
| PersistenceHarness | Save grammar, backup generations, quota faults, pure peek, NG+ metadata | PlayerPrefs/JSON/Unity adapters |
| ProgressionHarness | Real island YAML, restoration, registry, flow, unlock and boat decisions | No scene/coroutine execution; GSM is an adapter |
| CombatHarness | Battle manager, units, skill/status/Momentum/matrix, existing combat suites and adversarial regressions | Lifecycle, UI, QTE and neighboring service adapters |
| PuzzleIslandHarness | Tide manager/tile/data, legal moves, undo/reset, all authored solution witnesses | Rendering/coroutine/service adapters; bounded search |
| NarrativeHarness | Dialogue, tree/UI/trigger, story, affinity, ending and combat bond sources | Coroutine/UI/neighbor adapters |
| HeroGearHarness | Hero progression, gear and leveling plus three existing Unity verification suites | Unity/resources/service adapters |
| InputHarness | Input manager and menu navigation, legacy and Input System configurations | Fake device values; no physical hardware |
| SceneIntegration | C# 9 syntax under five configurations; player dependencies; scene/YAML/GUID consistency | No general semantic/API binding, package import or Play Mode |

The new harnesses link shipped C# files directly. Their adapters are deliberately
visible in each directory, and their assertions exercise production decisions.
They do not execute the entire game. Running them cannot prove that
GameStateManager's scene orchestration compiles against Unity or behaves correctly
under engine lifecycle ordering.

PowerShell checks validate canonical assets and selected source contracts.
`runtime_sim.ps1` does not simulate Unity. SceneIntegration also rejects missing
`.meta` files and player-active NUnit/UnityEditor references.

For website verification: `cd promo-site && npm ci && npm run typecheck && npm test
&& npm run lint`. Its separate CI job repeats these checks.

See `Assets/Docs/PRODUCTION_READINESS_2026-10-08.md` for findings, the Editor/manual
journey plan, and remaining release gates. Run destructive debug fixtures in a
disposable verification scene and isolate your personal save before running the
full ContextMenu matrix.
