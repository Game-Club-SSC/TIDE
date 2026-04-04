# AGENTS.md - TIDE Project Guide

## Project Overview

For an exhaustive technical deep-dive (architecture, data flow, patterns, pitfalls, system interactions), see: `Assets/Docs/REPO_UNDERSTANDING
Unity 6 (6000.3.7f1) Turn-Based Fantasy RPG. Five elements (Fire, Water, Earth, Air, Space) with combat and puzzle mechanics.

## Contributors
Shared server with three contributors. When a user identifies themselves at the start of a message, use their name as the git commit author:
- **Ryan N** - `git commit --author="Ryan N <ryan@example.com>"`
- **Andrian Z** - `git commit --author="Andrian Z <andrian.qiang.zhang@icloud.com>"`
- **Clinton W** - `git commit --author="Clinton W <clinton@example.com>"`

Replace placeholder emails with real ones if known. If no name is provided, ask the user to identify who is requesting the work before creating a commit.

## Authorship and Bot Account Rules
- GitHub bot account on this server: `OpenCode-SSC-T <daniela.duarte9203@goldfishgateway.com>`
- For all code commits, set `--author` to the identified teammate (Ryan N, Andrian Z, or Clinton W).
- If the user has not identified themselves for a code task, ask once for identity before committing.
- The bot account remains committer/pusher for those commits.
- If the task uses GitHub APIs only (issues, labels, milestones, comments, PR metadata) and does not create a git commit, attribution is the bot account; do not claim human commit authorship.
- For issue/PR comments created by the bot on behalf of a teammate, start comment text with `Requested by: <Name>` when the teammate identified themselves.

### GitHub Issue Creation (Critical)
The bot account `OpenCode-SSC-T` is **shadowbanned by GitHub for issue/PR creation**. Issues created via this account exist in the API but are invisible on the GitHub website.

**To create visible issues, switch to a human account token first:**
```bash
# Switch to a human account using a secure token source.
# Do not paste literal tokens into docs, commits, or chat.
printf '%s' "$HUMAN_ACCOUNT_PAT" | gh auth login --with-token
gh api user --jq '.login'  # verify it's NOT OpenCode-SSC-T

# Create issues using gh issue create
gh issue create --repo Game-Club-SSC/TIDE -l <label> -m "Vertical Slice" -t "Title" -b "Body"

# After all issues are created, switch back to bot for git commits
# Read the bot token from your secret manager or local secure env var.
printf '%s' "$OPENCODE_BOT_PAT" | gh auth login --with-token
gh api user --jq '.login'  # verify it IS OpenCode-SSC-T
```

**Always switch back to the bot account after creating issues** so git push continues to work.

Security notes:
- Never store PATs in tracked files or commit history.
- If a token is exposed, revoke/rotate it immediately and replace it via secure secret handling.
- Prefer short-lived credentials where possible.

## Pre-Work Protocol
```bash
git pull  # ALWAYS pull before making any changes
```
- **NEVER commit/push without explicit user consent**
- After code changes that affect gameplay, describe how to test in Unity Editor in your commit message
- Review generated code AT LEAST 5 times for bugs before finishing

## Two-Agent Workflow
1. **Coding Agent** - Implements features/fixes, writes tests, verifies changes
2. **Reviewing Agent** - Reviews code for bugs, style violations, edge cases
3. **Final Sweep** - Both agents verify: no compiler errors, null checks present, property validation correct

## Build & Test Commands
This is a Unity project - no CLI build system. All building/testing happens in Unity Editor.

### Running Tests
Tests are MonoBehaviour scripts with `[ContextMenu]` methods:
1. Open Unity Editor, create empty GameObject, attach test component
2. Right-click component header → Run test method
3. Check Console for results

Test files: `BattleFlowVerificationTest`, `CombatUnitTest`, `RestorationTrackerTest`, `BossEncounterGateTest`, `RestorationThresholdGateTest`, `CombatUnitVerificationTest`, `TideMovementTest`, `GearSystemTest`, `HeroProgressionTest`, `EnemyDataVerificationTest`, `HeroDataVerificationTest`, `PartySetupVerificationTest`

### Scene Flow
- `level_1.unity` - Main exploration scene
- `PuzzleScene.unity` - Tide puzzle gameplay
- `CombatScene.unity` - Turn-based battles

## Code Style Guidelines

### Naming Conventions
- **Classes**: PascalCase (`BattleManager`, `CombatUnit`)
- **Methods**: PascalCase (`TakeDamage`, `GetRestorationPercent`)
- **Private fields**: camelCase with no prefix (`currentPhase`, `allyUnits`)
- **Public properties**: PascalCase (`CurrentPhase`, `IsAlive`)
- **Constants**: PascalCase or UPPER_SNAKE for const strings (`MinimumDamage`, `DebugCanvasName`)
- **Enums**: PascalCase values (`BattlePhase.PlayerInput`, `Element.Fire`)

### Attributes & Serialization
```csharp
[DisallowMultipleComponent]  // On all MonoBehaviours
[Header("Category")]         // Group inspector fields
[SerializeField]             // Private fields visible in inspector
[Range(min, max)]            // Slider constraints
[Tooltip("Description")]     // Inspector tooltips
```

### Imports Order
```csharp
using System; using System.Collections.Generic; using System.Linq;
using UnityEngine; using UnityEngine.UI; using TMPro;
using NUnit.Framework;  // Only in test files
```

### Debug Logging Format
```csharp
Debug.Log($"[ClassName] Message: {variable}");
Debug.LogWarning($"[ClassName] Warning");
Debug.LogError($"[ClassName] Error");
```

### Property Pattern
```csharp
private int hp;
public int HP { get => hp; set => hp = value; }
public bool IsAlive => isAlive;  // Read-only expression body
```

### Property Validation (HP/MaxHP pattern)
```csharp
public int MaxHP { get => maxHp; set { maxHp = Mathf.Max(1, value); hp = Mathf.Clamp(hp, 0, maxHp); } }
public int HP { get => hp; set { hp = Mathf.Clamp(value, 0, maxHp); if (hp <= 0 && isAlive) Die(); } }
```

### Internal Debug Properties (for tests)
```csharp
internal int DebugHP { set => hp = value; }
internal bool DebugIsAlive { set => isAlive = value; }
```

## Error Handling Philosophy
- **No try/catch** in production code - use null checks and validation
- Guard clauses at method entry: `if (unit == null) return;`
- Validate ScriptableObjects: `if (!data.IsValid()) return;`
- Clamp values: `hp = Mathf.Clamp(value, 0, maxHp);`
- Boundary defaults in `Awake()`: `if (maxHp <= 0) maxHp = 100;`

## Coroutine Patterns
```csharp
// For visual effects and timed sequences only (no gameplay logic)
private IEnumerator FadeCanvas(float targetAlpha, float duration) {
    float elapsed = 0f;
    while (elapsed < duration) { canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration); elapsed += Time.deltaTime; yield return null; }
}
```

## Critical Font Fix
**ALWAYS use `LegacyRuntime.ttf` instead of `Arial.ttf`:**
```csharp
// WRONG - Arial.ttf not available in Unity 6
label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
// CORRECT
label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
```

## Common Patterns

### Singleton (e.g., GameStateManager, IslandRestorationTracker)
```csharp
public static ClassName Instance { get; private set; }
private void OnEnable()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Events
```csharp
public event Action<string, float> OnRestorationChanged;
OnRestorationChanged?.Invoke(islandId, progress);
```

### ScriptableObjects
```csharp
[CreateAssetMenu(fileName = "Name", menuName = "TIDE/Menu Name")]
public class DataClass : ScriptableObject { }
```

## Common Pitfalls
- **Arial.ttf** - Not available in Unity 6, use `LegacyRuntime.ttf`
- **Re-enable guards** - Always check `isTransitioning` before scene loads
- **Null events** - Always use `?.Invoke()` for events
- **HP death** - Setting HP ≤ 0 triggers `Die()` automatically
- **Coroutine cancellation** - Stop previous coroutine before starting new visual effect

## Folder Structure
```
Assets/
├── *.cs                    # Game scripts (flat structure)
├── Scenes/                 # Unity scenes
├── Resources/              # Runtime-loaded assets
├── TextMesh Pro/           # TMP assets
├── Settings/               # Project settings
└── Docs/                   # Documentation
```

## Key Systems
- **BattleManager** - Turn queue, phase management, clash resolution
- **CombatUnit** - Stats, damage, healing, MP management
- **TideManager** - Puzzle board, tile movement, win conditions
- **GameStateManager** - Scene transitions, state tracking
- **IslandRestorationTracker** - Progress tracking per island
- **IslandFlowController** - Encounter sequencing

## Verification Checklist (after code changes)
1. All public methods have null checks
2. Properties validate values (HP/MaxHP pattern)
3. Events use `?.Invoke()` (no null exceptions)
4. ScriptableObjects have `IsValid()` methods
5. Singletons use `OnEnable()` with destroy guard
6. Coroutines yield `return null` or `WaitForSeconds`
7. Debug logs use `[ClassName]` prefix format
8. No `Arial.ttf` references (use `LegacyRuntime.ttf`)
9. After creating GitHub issues, verify they appear in `gh issue list` (not just API GET)

## Design Doc Handling
- Treat user-provided game design docs as canonical references and consult them when implementing gameplay, systems, progression, and narrative tasks.
- If a PDF cannot be read by the current model, explicitly inform the user with this message: `ERROR: Cannot read "Copy%20of%20GAME%20IDEA.pdf.pdf" (this model does not support pdf input).`
- Ask the user to provide a text/markdown copy or paste key sections so the design content can be safely referenced in future implementation work.
