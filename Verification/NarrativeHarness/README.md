# Narrative regression harness

Run `dotnet run --project Verification/NarrativeHarness -c Release` from the repository root.

The executable compiles the actual dialogue ledger, runner, UI, trigger, authored hero trees, narrative beat director, story service, relationship tracker, relationship combat effects, ending evaluator, island registry, and JSON validator from `Assets/`. It covers deferred reward deduplication, stale flag writes, save/callback reentry, choice bond replay, empty decision nodes, automatic navigation cycles, cancellation and movement restoration, reentrant sequence completion, bounded story/affinity snapshots, corrupt legacy affinity saves, hero pair bonds in combat, canonical ending history, interrupted story beats and triggers, and authored branch targets.

Adapters provide engine APIs and neighboring services. Rendering, Unity coroutine scheduling, scene import, prefab references, cinematics, and the complete title-to-ending journey still require Unity Play Mode/build verification. Unity state-ledger regression cases also live in `NarrativeReplayVerificationTest` and `EndingEvaluatorTest`.
