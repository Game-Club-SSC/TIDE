# Scene and source integration checks

Run from the repository root with .NET SDK 8 or later:

```sh
dotnet run --project Verification/SceneIntegration -- .
```

The harness parses all `Assets` C# sources with Unity's C# 9 language level
under editor, release player, development player, legacy-input fallback, and mobile player
symbols. Player passes omit `Editor` directories and flag active UnityEditor
imports, active NUnit imports in the flat runtime assembly, and player-active
references to verification types. Verification scripts remain available in
the Editor through `UNITY_EDITOR` guards. It also checks qualified nested
project type names against source declarations; other type/member binding and
Unity API compatibility still require engine compilation.

Every C# source must have a metadata file so Unity retains stable script GUIDs.

It checks unique project asset GUIDs, orphan metadata, enabled scene paths and
GUIDs, title entry ordering, enabled/active expected scene components, missing script entries,
script class names, and local/external references in every scene, Resources
asset, and top-level gameplay prefab. Known package GUIDs only establish that
the appropriate package is declared in `Packages/manifest.json`.

This is **not a Unity compilation or runtime test**. It cannot prove package
restoration/GUID resolution, asset import, engine API compatibility, lifecycle
ordering, scene transitions, UI input, rendered output, Play Mode, or builds.
Run the Editor verification menu and the complete player journey in Unity.
