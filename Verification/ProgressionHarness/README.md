Runs the actual restoration, progression, island flow, registry, travel validation, and dock source files against minimal Unity adapters. Island fixture restoration values are read from the repository's real YAML assets.

Run from the repository root:

    dotnet run --project Verification/ProgressionHarness/ProgressionHarness.csproj

This verifies C# gameplay decisions and regressions. Adapters do not simulate Unity lifecycle ordering, coroutines, scene imports, physics, or presentation; validate the full journey in Unity 6000.3.7f1 as well.
