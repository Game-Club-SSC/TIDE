# Persistence source verification

Run from the repository root:

```sh
dotnet run --project Verification/PersistenceHarness/PersistenceHarness.csproj --configuration Release
```

The executable compiles the production `SaveJsonValidator`, `WorldSaveService`, and `NewGamePlusService` files directly. Its Unity test doubles keep working PlayerPrefs values separate from flushed values and inject rejected and corrupted writes. The checks cover:

- Strict JSON structure, nesting/size bounds, duplicate keys, escaped keys, schema migration and future-schema preservation. Two thousand deterministic mutations are compared against `System.Text.Json` as an independent parser.
- Save/load/restart, generation backup, missing/empty/corrupt primary recovery, quota exceptions, failed retries, backup-flush failure and event counts.
- NG+ malformed metadata, completion/start idempotence, legacy migration, finite scaling, saturated counters/currency, normal-run reset and world-snapshot restoration against newer sidecar state.

This is a source-level executable check with Unity and storage doubles. It cannot establish actual platform PlayerPrefs durability, Unity JsonUtility compatibility, Unity lifecycle behaviour, or GameStateManager's complete scene/persistence flow.

For those checks, open Unity `6000.3.7f1`, run the `SavePersistenceTest` and `SaveRestoreInvariantTest` component context menus, then exercise title → island → combat/puzzle → save → reload. Confirm the party, partial XP, quest flags, relationship values, travel/restoration and NG+ mode restore together. The editor verification components are excluded from player compilation.
