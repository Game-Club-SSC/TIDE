# Combat / Battle System Bug Report

**Scope:** Combat, battle, and encounter C# files under `/Users/andrian/GitHub/TIDE/Assets/`.
**Files reviewed (read in full):** `BattleManager.cs`, `BattleHud.cs`, `BattleEscapeMenu.cs`, `BattleFlowTestSuite.cs`, `EncounterConfig.cs`, `FateEncounterDirector.cs`, `IslandFlowController.cs`, `ViceAIProfile.cs`, `BossEncounterGate.cs`, `BossIntroDirector.cs`, `BossNarrativeMechanic.cs`, `OverworldEnemy.cs`, `EnemyTrigger.cs`, `MomentumState.cs`, `DesireStatusEffectSet.cs`, `SelfHarmBeat.cs`, `SkillData.cs`, `CombatUnit.cs`, plus supporting `StatusEffect.cs`, `RelationshipCombatEffects.cs`, `ElementMatchup.cs`.

Every bug below was verified by reading the actual source. Each cites an absolute path, exact line(s), and the buggy code quoted verbatim.

---

## BUG #1 — Runtime-built Fate boss is registered as an Ally (instant-victory break)

- **File:** `/Users/andrian/GitHub/TIDE/Assets/FateEncounterDirector.cs`
- **Line:** 779–797 (`SpawnFateBoss` runtime branch); root cause confirmed at `/Users/andrian/GitHub/TIDE/Assets/CombatUnit.cs:68`
- **Category:** 3 (HP / unit-state pattern violation) / 8 (logic error) — unit `Type` never set
- **Severity:** Critical
- **Description:** When no `fateBossPrefab` is assigned (the default, since the field has no initializer), `SpawnFateBoss` builds a `CombatUnit` at runtime and configures `MaxHP`/`HP`/`Attack`/`Defense`/`Speed`/`ElementType` — but **never sets `fateUnit.Type`**. `CombatUnit.unitType` defaults to `UnitType.Ally` (`CombatUnit.cs:68`). `ConfigureFateCombat` then calls `bm.RegisterUnit(fateUnit)`, and `BattleManager.RegisterUnit` routes by `Type` into `allyUnits`. So the "hardest boss in the game" joins the **player's team**. Worse, since there are no enemies on the enemy team, `BattleManager.CheckBattleOutcome()` sees `enemiesAlive == 0` and calls `SetVictory()` on the first action-execution frame — the Fate finale is broken / instantly won.
- **Suggested Fix:** In the runtime branch add `fateUnit.Type = CombatUnit.UnitType.Enemy;` (and set `fateUnit.UnitName` to a proper boss name).
- **Code Snippet:**
```csharp
// CombatUnit.cs:68
[SerializeField] protected UnitType unitType = UnitType.Ally;   // default is Ally

// FateEncounterDirector.cs:781-797
spawnedFateBoss = new GameObject("Fate_The_Inevitable", typeof(CombatUnit));
...
CombatUnit fateUnit = spawnedFateBoss.GetComponent<CombatUnit>();
if (fateUnit != null)
{
    fateUnit.MaxHP = fateMaxHp;
    fateUnit.HP = fateMaxHp;
    fateUnit.Attack = fateAttack;
    fateUnit.Defense = fateDefense;
    fateUnit.Speed = fateSpeed;
    fateUnit.ElementType = fateBaseElement;
    // <-- Type never set; stays UnitType.Ally
}
```

---

## BUG #2 — Relationship defense multiplier is applied inverted (high-bond allies take MORE damage)

- **File:** `/Users/andrian/GitHub/TIDE/Assets/BattleManager.cs`
- **Line:** 1586–1591 (same inverted pattern repeats at 1784–1787, 1992–1995, 2066–2068)
- **Category:** 8 (logic error / inverted condition) + 19 (wrong Mathf usage)
- **Severity:** High
- **Description:** `relationshipDefenseMultiplier = RelationshipCombatEffects.GetTeamDefenseMultiplier(allies)` (`BattleManager.cs:390`), which returns **0.8 for low bonds** and **1.25 for high bonds** (see `RelationshipCombatEffects.cs:51–58`, and its docstring: low bonds = "defense reduced", high bonds = defense bonuses). For enemy→ally damage the code does `finalDamage = modifiedDamage * relationshipDefenseMultiplier`. So with **high bonds (1.25) allies take 25% MORE damage**, and with **low bonds (0.8) allies take LESS damage** — the exact opposite of the design intent ("defense reduced" for low bonds ⇒ more damage taken; "defense amplified" for high ⇒ less). The inline comment even says *"(reduces damage when enemies hit allies)"*, but for high bonds it increases damage. The damage-dealt path (`multiplier *= relationshipDamageMultiplier`, line 1561) is correct; only the defense path is inverted.
- **Suggested Fix:** Divide instead of multiply: `Mathf.RoundToInt(modifiedDamage / relationshipDefenseMultiplier)` (or invert the values returned by `GetTeamDefenseMultiplier`).
- **Code Snippet:**
```csharp
// Apply relationship defense multiplier (reduces damage when enemies hit allies)
int finalDamage = modifiedDamage;
if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
{
    finalDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamage * relationshipDefenseMultiplier));
}
target.TakeDamage(finalDamage);
```

---

## BUG #3 — Shield absorption loses fractional HP to `Mathf.RoundToInt` (0.5 shield absorbs 0)

- **File:** `/Users/andrian/GitHub/TIDE/Assets/CombatUnit.cs`
- **Line:** 306–312 (TakeDamage shield block)
- **Category:** 19 (wrong Mathf usage) / 4 (validation)
- **Severity:** Medium
- **Description:** `shieldHp` is a `float` (set as `effect.Magnitude * maxHp` in `ApplyStatusEffect`, frequently fractional). The shield block does `actualDamage -= Mathf.RoundToInt(shieldAbsorb)`. `Mathf.RoundToInt` uses banker's rounding, so `Mathf.RoundToInt(0.5f) == 0` and any `shieldAbsorb < 0.5` rounds to 0. When a shield has a small residual (e.g. 0.5 HP) and `actualDamage >= 1`, `shieldAbsorb = Mathf.Min(0.5, 1) = 0.5`, `shieldHp` is consumed to 0, but `actualDamage -= 0` ⇒ the shield absorbs **nothing** while being fully spent, and the full damage goes to HP. Fractional shield residuals are very reachable (e.g. `0.005 * 100 = 0.5`, or after partial absorption) and silently vanish.
- **Suggested Fix:** Track damage in float space (only round the final HP reduction), or use `Mathf.CeilToInt` for the absorbed portion so a non-zero shield always absorbs at least 1.
- **Code Snippet:**
```csharp
if (shieldHp > 0f)
{
    float shieldAbsorb = Mathf.Min(shieldHp, actualDamage);
    shieldHp -= shieldAbsorb;
    actualDamage -= Mathf.RoundToInt(shieldAbsorb);   // 0.5 -> 0: shield consumed, 0 absorbed
    Debug.Log($"[CombatUnit] {unitName}'s shield absorbed {Mathf.RoundToInt(shieldAbsorb)} damage. Shield HP: {shieldHp:F0}");
}
```

---

## BUG #4 — `CreateDrowsyEffect` hardcodes magnitude `0f`, making Drowsy mechanically inert

- **File:** `/Users/andrian/GitHub/TIDE/Assets/DesireStatusEffectSet.cs`
- **Line:** 11–15 (mechanical effect at `/Users/andrian/GitHub/TIDE/Assets/CombatUnit.cs:586–595`)
- **Category:** 4 (property/validation) / 8 (logic error)
- **Severity:** Medium
- **Description:** `CreateDrowsyEffect` constructs the Drowsy effect with `magnitude: 0f`. The **only** mechanical effect of `Drowsy` is in `CombatUnit.ShouldSkipTurn()`: `return highestDrowsy >= 1f || UnityEngine.Random.value < highestDrowsy;`. With magnitude 0, `highestDrowsy == 0`, so the condition is `0 >= 1 || Random.value < 0` ⇒ **always false** — the Drowsy effect never causes a turn skip. This factory therefore produces a Drowsy that does nothing (compare `CreateSlowEffect`, which takes a caller-provided magnitude). The Desire vice is described as "debuff-focused, weakens players," yet its Drowsy debuff is inert.
- **Suggested Fix:** Accept a `magnitude` parameter (like `CreateSlowEffect`) or default to a sensible non-zero probability (e.g. `0.5f`).
- **Code Snippet:**
```csharp
public static StatusEffect CreateDrowsyEffect(string sourceName, int duration)
{
    StatusEffect effect = new StatusEffect(StatusEffectType.Drowsy, Mathf.Max(1, duration), 0f, sourceName);
    return effect;
}
// CombatUnit.cs:595
return highestDrowsy >= 1f || UnityEngine.Random.value < highestDrowsy;   // 0 => never skips
```

---

## BUG #5 — Visual-feedback coroutines started without stopping previous ones (position drift)

- **File:** `/Users/andrian/GitHub/TIDE/Assets/BattleManager.cs`
- **Line:** 2321–2344 (`TriggerBattleHitFeedback`); coroutines `AnimateLunge` 2359–2392 & `AnimateHitShake` 2394–2417
- **Category:** 5 (coroutine cancellation)
- **Severity:** Medium
- **Description:** `TriggerBattleHitFeedback` starts `AnimateLunge`, `AnimateHitShake`, `AnimateShadowPulse` (and via `SpawnHitEffect`, `AnimateHitEffect`) using `StartCoroutine(...)` without storing references or stopping any prior coroutine on the same transform. Both `AnimateLunge` and `AnimateHitShake` capture `start = visualTransform.localPosition` at launch and **reset `localPosition = start`** on completion. A target is genuinely hit twice in quick succession in `ResolveAttack`: the main attack calls `TriggerBattleHitFeedback(actor, target, …)` (line 1609) and then the team-up partner calls `TriggerBattleHitFeedback(partner, target, …)` (line 1640) on the **same target**. Two shake/lunge coroutines then run concurrently on the same transform, both writing `localPosition`; the second captures a mid-animation position as its `start` and restores to that wrong offset when it finishes, leaving the unit visually displaced.
- **Suggested Fix:** Keep a per-visual `Coroutine` reference (e.g. in a dictionary keyed by transform) and `StopCoroutine` it before starting a new lunge/shake, or guard against overlapping animations.
- **Code Snippet:**
```csharp
Transform targetVisual = ResolveActionVisualTransform(target);
if (targetVisual != null)
{
    StartCoroutine(AnimateHitShake(targetVisual, isCrit));   // no StopCoroutine of a previous shake
}
...
// AnimateHitShake end (line 2416):
visualTransform.localPosition = start;   // 'start' may be a mid-shake offset if another shake is still running
```

---

## BUG #6 — `ResolveAttack` damage log reports `modifiedDamage` instead of the actual `finalDamage`

- **File:** `/Users/andrian/GitHub/TIDE/Assets/BattleManager.cs`
- **Line:** 1611–1613
- **Category:** 11 (wrong variable used)
- **Severity:** Low
- **Description:** `ResolveAttack` computes `finalDamage` (which diverges from `modifiedDamage` whenever the enemy→ally relationship-defense multiplier applies, lines 1588–1591) and calls `target.TakeDamage(finalDamage)`. But the summary log prints `for {modifiedDamage}` while also printing `HP {hpBefore} -> {hpAfter}`. When the defense multiplier is active, the printed damage does not match the HP delta shown on the same line. The sibling skill-resolution log (`ResolveSkill`, line 1914) correctly uses `actualDamageSingle` (= `hpBefore - hpAfter`), confirming the intended pattern.
- **Suggested Fix:** Log `finalDamage` (or `hpBefore - hpAfter`) instead of `modifiedDamage`.
- **Code Snippet:**
```csharp
target.TakeDamage(finalDamage);

int hpAfter = target.HP;
...
Debug.Log(
    $"[BattleManager] {actor.UnitName} attacks {target.UnitName} for {modifiedDamage} (base {baseDamage} x{multiplier:F2}). HP {hpBefore} -> {hpAfter}.{matchupFeedback}",
    this);
```

---

## BUG #7 — `ResolveTideBreak` (AllEnemies) damage log reports `modifiedDmg` instead of `finalDmg`

- **File:** `/Users/andrian/GitHub/TIDE/Assets/BattleManager.cs`
- **Line:** 2001 (same root-cause pattern as BUG #6, but a separate method/location)
- **Category:** 11 (wrong variable used)
- **Severity:** Low
- **Description:** In the AllEnemies TideBreak loop, `finalDmg` is computed (lines 1991–1995, again diverging from `modifiedDmg` when the enemy→ally defense multiplier applies) and `target.TakeDamage(finalDmg)` is called (line 1998), but the per-target log prints `takes {modifiedDmg} damage` alongside `HP {hpBefore} -> {target.HP}`. The *single-target* TideBreak log at line 2074 correctly uses `finalDmg` — so this AllEnemies instance is the inconsistent/buggy one.
- **Suggested Fix:** Log `finalDmg` (or `hpBefore - target.HP`) instead of `modifiedDmg`.
- **Code Snippet:**
```csharp
int finalDmg = modifiedDmg;
if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
{
    finalDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDmg * relationshipDefenseMultiplier));
}

int hpBefore = target.HP;
target.TakeDamage(finalDmg);
TriggerBattleHitFeedback(actor, target, false, true);
totalDamage += (hpBefore - target.HP);
Debug.Log($"  -> {target.UnitName} takes {modifiedDmg} damage. HP {hpBefore} -> {target.HP}", this);
```

---

## BUG #8 — Redundant duplicate call in `PlaySelfHarmSequence` guard

- **File:** `/Users/andrian/GitHub/TIDE/Assets/SelfHarmBeat.cs`
- **Line:** 65
- **Category:** 9 (dead/redundant code) / 8 (logic error)
- **Severity:** Low
- **Description:** The guard `if (!CanPlaySelfHarmSequence() && !CanPlaySelfHarmSequence())` calls the **same** side-effect-free predicate twice with `&&`. `A && A` is logically identical to `A`, so the second operand is pure dead code (a copy-paste artifact). It is functionally harmless because `CanPlaySelfHarmSequence()` only reads state, but it is a real defect and signals the intended condition was something else.
- **Suggested Fix:** Replace with a single `if (!CanPlaySelfHarmSequence())`.
- **Code Snippet:**
```csharp
public bool PlaySelfHarmSequence()
{
    if (!CanPlaySelfHarmSequence() && !CanPlaySelfHarmSequence())
    {
        return false;
    }

    isPlaying = true;
    currentLineIndex = 0;
    FireSequence();
    return true;
}
```

---

## Summary table

| # | File | Line | Category | Severity | Short description |
|---|------|------|----------|----------|-------------------|
| 1 | FateEncounterDirector.cs | 779–797 | 3 / 8 | Critical | Runtime Fate boss never set to `Enemy`; defaults to `Ally` → no enemies → instant victory. |
| 2 | BattleManager.cs | 1586–1591 | 8 / 19 | High | Relationship defense multiplier multiplied (not divided); high bonds make allies take MORE damage. |
| 3 | CombatUnit.cs | 306–312 | 19 / 4 | Medium | `Mathf.RoundToInt` on shield absorb loses fractional HP; 0.5 shield absorbs 0 damage. |
| 4 | DesireStatusEffectSet.cs | 11–15 | 4 / 8 | Medium | `CreateDrowsyEffect` hardcodes magnitude `0f`; Drowsy never triggers a skip. |
| 5 | BattleManager.cs | 2321–2344 | 5 | Medium | Hit-feedback coroutines not stopped before starting new ones; concurrent writes cause position drift. |
| 6 | BattleManager.cs | 1611–1613 | 11 | Low | `ResolveAttack` log prints `modifiedDamage` instead of `finalDamage`. |
| 7 | BattleManager.cs | 2001 | 11 | Low | `ResolveTideBreak` AllEnemies log prints `modifiedDmg` instead of `finalDmg`. |
| 8 | SelfHarmBeat.cs | 65 | 9 / 8 | Low | `!CanPlaySelfHarmSequence() && !CanPlaySelfHarmSequence()` — redundant duplicate call. |

## Priority order for fixing
1. **BUG #1** (Critical) — breaks the entire Fate finale.
2. **BUG #2** (High) — inverts combat-balance scaling for bonded parties.
3. **BUG #3, #4, #5** (Medium) — shield rounding, inert Drowsy, coroutine drift.
4. **BUG #6, #7, #8** (Low) — logging/semantic defects.

## Items checked and cleared (no bug)
- All C# `event` invocations in these files use `?.Invoke()` (`OnClashResolved`, `OnDamageDealt`, `OnMomentumChanged`, `OnFateDialogueComplete`, `OnFateCombatComplete`, `OnBossUnlocked`, `OnBossLocked`, `OnBadEndingThresholdReached`, `OnSelfHarmLinePresented`, `OnSelfHarmSequenceFinished`).
- No `Arial.ttf` references in any `.cs` file (all UI uses `LegacyRuntime.ttf`); `Arial.ttf` only appears in docs.
- No `try/catch` in any combat production file (earlier grep hits were comments/strings).
- `StatusEffect` is a `class` (so the "refresh existing effect" mutation in `ApplyStatusEffect` correctly mutates the list element — not a struct-copy bug).
- `MomentumState.ShiftForAction` switch covers all `MatchupResult` values (`Strong`/`Weak`/`Neutral` only); no missing case.
- `CombatUnit` HP/MaxHP setters correctly clamp to `[0, maxHp]` and trigger `Die()` when `hp <= 0 && isAlive`; `Awake()` has boundary defaults for `maxHp`/`maxMp`.


