# Static Island Validation

Two PowerShell scripts in the repo root that mirror the assertions of the Unity
verification tests (`IslandContentVerificationTest`, the 5 per-island
`XxxIslandVerificationTest` classes, and the 3 signature-mechanic test suites
`SlothStatusEffectTestSuite`, `EnvyMirrorTestSuite`, `GreedEconomyTestSuite`).

These exist so AI agents (and humans) can audit the vertical slice data layer
without booting Unity:

```
powershell -ExecutionPolicy Bypass -File validate.ps1
powershell -ExecutionPolicy Bypass -File runtime_sim.ps1
```

The 294 individual checks cover:

- **VSR-001 (`IslandContentVerificationTest`)** — 7 island configs present, 9
  encounters per island in the right order, restoration sums to 1.0, boss at
  0.25, distinct vice colors, unique boss EncounterConfigs, puzzle variation
  (>= 2 distinct puzzles with both `AllEqualToTarget` and
  `PercentageAtTarget`), ancient text per vice, scene file per island.
- **VSR-010 (`GluttonyIslandVerificationTest`)** — boss enemy is "The Devourer"
  with a lifesteal skill (`restoreCasterPercentOfDamage > 0`); at least 3
  Gluttony puzzles enable consumption.
- **VSR-011 through VSR-015** (Greed/Sloth/Wrath/Envy/Pride) — 5 combat + 4
  puzzle encounters per island, boss is the 9th encounter, scene file exists,
  per-island progression (Pride is the final island).
- **VSR-016 (`SlothStatusEffectTestSuite`)** — `StatusEffectType.Slow` and
  `Drowsy` enum values, `CombatUnit.GetEffectiveSpeed` returns
  `speed * (1 - max(Slow))`, `CombatUnit.ShouldSkipTurn` returns true when
  Drowsy magnitude >= 1.0.
- **VSR-017 (`EnvyMirrorTestSuite`)** — `BattleManager.EnableEnvyMirror`,
  `lastAttacker`, `lastPlayerSkill`, `isBossEncounter`, `ComputeEnemyAction`
  all present; mirror branch copies `lastAttacker.ElementType`; covet branch
  null-guards `lastPlayerSkill`.
- **VSR-018 (`GreedEconomyTestSuite`)** — `PuzzleData.coinTileYield = 2`
  default, `enableGreedEconomy` field, `HeroProgressionManager.Currency` /
  `SetCurrency` / `TrySpendCurrency`, `SkillData.currencyStealAmount` field.
- All combat encounter `EncounterConfig` GUIDs resolve to real
  `Resources/Encounters/*.asset` files.
- All puzzle `PuzzleData` GUIDs resolve to real `Resources/Puzzles/*.asset`
  files.
- Every island's boss `EncounterConfig` resolves and references at least one
  enemy.

If the script reports a failure, fix the underlying data (or the code) rather
than editing the assertion — the assertions mirror the Unity test code.
