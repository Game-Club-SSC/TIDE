# Bug Hunt Report — TIDE UI/Audio/Camera/Input Files

**Scope:** Bug hunt across the UI/audio/camera/input C# files in `/Users/andrian/GitHub/TIDE/Assets/`, including `AudioManager.cs`, `AudioSettingsUI.cs`, `BattleHud.cs`, `BattleHudPolishService.cs`, `TopDownFollowCamera.cs`, `IsometricPlayer.cs`, `MobileTouchController.cs`, `MobileTouchInputManager.cs`, `PhoneInputBridge.cs`, `PhoneControllerAuthService.cs`, `ExplorationMapUI.cs`, `PlayerCustomizationUI.cs`, `SmithyUI.cs`, `SmithyInteractable.cs`, `FuturisticSpriteLibrary.cs`, `ExclamationMarkSprite.cs`, `TeleportAnchor.cs`, `IslandRestorationHud.cs`, `AncientTextLogUI.cs`, `DialogueUI.cs`, `DevMenuUI.cs`, `PartySetupUI.cs`, `PartySwapPanel.cs`, `TideBreakUnlockUI.cs`, `PuzzleHud.cs`, `GoodEndingCutsceneController.cs`, `BadEndingReactions.cs`, `BossIntroDirector.cs`, `CeremonyIntroDirector.cs`, `TribeLeaderNPC.cs`, `EnchantedMouraNPC.cs`, and `LocalizationService.cs`.

**Bugs found:** 8 (1 High, 5 Medium, 2 Low/Medium-Low).

---

## BUG #1
- **File:** `/Users/andrian/GitHub/TIDE/Assets/AudioManager.cs`
- **Line:** 422–448 (root cause); 927 and 778/787 reinforce it
- **Category:** Audio source null/volume state bug — mute toggle leaves volume at 0
- **Severity:** High
- **Description:** `SetMute()` only toggles each `AudioSource.mute` and never touches `.volume`. But elsewhere the volume is driven to 0 when muted: `ApplyVolumes()` (line 927) sets `bgmSource.volume = mute ? 0f : bgmVolume;`, and `CrossfadeBgm()` fades in to `float targetVolume = BgmVolume;` (line 778) — which is `0f` while muted (line 148). So after muting (or starting the game with mute persisted) and then a BGM crossfade/scene-load, the source volume is 0. Calling `SetMute(false)` sets `mute = false` but leaves `bgmSource.volume` at 0 → **BGM is silent even after un-muting**. Reproduction: launch with `Audio_Muted=1` in PlayerPrefs, then un-mute from the AudioSettingsUI slider/toggle → no BGM.
- **Suggested Fix:** In `SetMute`, also restore the source volumes (call `ApplyVolumes()` after setting `mute`, or set `bgmSource.volume = isMuted ? 0f : bgmVolume` etc.).
- **Code Snippet:**
```csharp
public void SetMute(bool isMuted)
{
    mute = isMuted;
    if (bgmSource != null)
    {
        bgmSource.mute = isMuted;   // <-- only toggles .mute; .volume is never restored
    }
    ...
}
// ApplyVolumes (line 927): bgmSource.volume = mute ? 0f : bgmVolume;   // drives volume to 0 when muted
// CrossfadeBgm (line 778): float targetVolume = BgmVolume;            // 0 while muted, fades to 0
```

---

## BUG #2
- **File:** `/Users/andrian/GitHub/TIDE/Assets/AudioManager.cs`
- **Line:** 707 vs 778/787
- **Category:** Logic error — act-based volume tone shift is silently lost
- **Severity:** Medium
- **Description:** `ApplyActToneShift()` intentionally lowers BGM volume per story act (`bgmSource.volume = BgmVolume * actVolMult;`, line 707 — e.g. Act III = 0.7× for a "somber" feel). But `PlayIslandAudio()`/`PlayIslandPuzzleAudio()` immediately call `PlayBgm()` → `CrossfadeBgm()`, whose fade-in target is `float targetVolume = BgmVolume;` (line 778, no act multiplier) and which ends with `bgmSource.volume = targetVolume;` (line 787). The crossfade therefore overwrites the act volume multiplier, so the per-act volume tone-shift feature never takes effect (pitch is preserved because CrossfadeBgm doesn't touch pitch, but volume is clobbered).
- **Suggested Fix:** Compute the fade-in target as `BgmVolume * (activeIslandProfile != null ? activeIslandProfile.GetActVolumeMultiplier(actNumber) : 1f)` instead of bare `BgmVolume`.
- **Code Snippet:**
```csharp
// ApplyActToneShift (line 707):
bgmSource.volume = BgmVolume * actVolMult;   // act multiplier applied...
// CrossfadeBgm (line 778/787):
float targetVolume = BgmVolume;              // ...but immediately overwritten (no actVolMult)
...
bgmSource.volume = targetVolume;
```

## BUG #3
- **File:** `/Users/andrian/GitHub/TIDE/Assets/TideBreakUnlockUI.cs`
- **Line:** 53–62 (OnDisable); 131–142 (DisplayPopup)
- **Category:** UI state leak — popup stuck visible when disabled mid-display
- **Severity:** Medium
- **Description:** `DisplayPopup()` activates the canvas and fades the `popupCanvasGroup` alpha to 1, holds, then fades to 0 and `SetActive(false)` at the very end. `OnDisable()` only does `StopAllCoroutines()` and `isDisplaying = false` — it **never hides `popupCanvasGroup`** (doesn't set alpha 0 or SetActive(false)) and **never clears `unlockQueue`**. If the component is disabled mid-popup (scene unload, GO disabled, or app backgrounding), the coroutine is killed before the fade-out/`SetActive(false)` runs, leaving the unlock popup permanently on-screen (and any queued unlocks re-display as stale on re-enable). The fallback canvas created in `CreateFallbackPopupCanvasGroup()` is a free-standing root object, so it isn't even destroyed with the component.
- **Suggested Fix:** In `OnDisable`, force-hide the popup: `if (popupCanvasGroup != null) { popupCanvasGroup.alpha = 0f; popupCanvasGroup.gameObject.SetActive(false); }` and `unlockQueue.Clear();`.
- **Code Snippet:**
```csharp
private void OnDisable()
{
    if (TideBreakProgressionManager.Instance != null)
        TideBreakProgressionManager.Instance.OnTideBreakUnlocked -= HandleTideBreakUnlocked;
    StopAllCoroutines();
    isDisplaying = false;
    // <-- popupCanvasGroup left at whatever alpha it had; unlockQueue not cleared
}
// DisplayPopup (131): popupCanvasGroup.gameObject.SetActive(true);
//                  (132): yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 0f, 1f, fadeInDuration));
//                  (141): popupCanvasGroup.gameObject.SetActive(false);  // only reached if coroutine completes
```

---

## BUG #4
- **File:** `/Users/andrian/GitHub/TIDE/Assets/CeremonyIntroDirector.cs`
- **Line:** 105–113 (SkipIntroForDebug), 415–418 (OnDestroy), 125 & 163 (lock/unlock)
- **Category:** Coroutine cancellation / movement-lock not restored
- **Severity:** Medium
- **Description:** `CeremonySequence()` calls `LockPlayerMovement(true)` at line 125 (sets `player.canMove = false`) and only calls `LockPlayerMovement(false)` at the very end (line 163). If the sequence is interrupted — `SkipIntroForDebug()` calls `StopAllCoroutines()` (line 110) without unlocking, and `OnDestroy()` also calls `StopAllCoroutines()` (line 417) without unlocking — the coroutine never reaches line 163, so `canMove` stays `false` and the player is permanently unable to move. `LockPlayerMovement` doesn't snapshot/restore `canMove` either, so nothing can recover it.
- **Suggested Fix:** Have `SkipIntroForDebug()` and `OnDestroy()` call `LockPlayerMovement(false)` before stopping coroutines (and make `LockPlayerMovement` snapshot the previous `canMove` so it can restore it).
- **Code Snippet:**
```csharp
public void SkipIntroForDebug()
{
    HasPlayedIntro = true;
    MarkIntroCompleted();
    isPlaying = false;
    StopAllCoroutines();   // stops CeremonySequence before it reaches LockPlayerMovement(false)
    HideUI();
    OnIntroFinished?.Invoke();
    // <-- LockPlayerMovement(false) never called; player stays frozen
}
```

## BUG #5
- **File:** `/Users/andrian/GitHub/TIDE/Assets/AncientTextLogUI.cs`
- **Line:** 376–384 (LockPlayerMovement unlock branch)
- **Category:** Missing/conditional restore — player can be left frozen
- **Severity:** Medium
- **Description:** On unlock, `LockPlayerMovement(false)` only restores `cachedPlayer.canMove = wasPlayerMoveEnabled` **if** `GameStateManager` is non-null, in `Exploration` state, and not transitioning (line 376–381). But it then unconditionally clears `hasMovementLockSnapshot = false` and `movementLocked = false` (lines 383–384). If the log is closed (Esc/Enter/click in `Update`, which doesn't gate on state) while a transition is in progress or the state isn't Exploration, `canMove` is **not** restored yet the snapshot is discarded — so the player is left with `canMove = false` and a future `LockPlayerMovement(false)` will early-out (`locked == movementLocked`, both false) without ever restoring movement.
- **Suggested Fix:** Restore `canMove` unconditionally on unlock (or keep the snapshot until the Exploration condition can be met instead of discarding it).
- **Code Snippet:**
```csharp
if (GameStateManager.Instance != null
    && GameStateManager.Instance.currentState == GameStateManager.GameState.Exploration
    && !GameStateManager.Instance.IsTransitioning)
{
    cachedPlayer.canMove = wasPlayerMoveEnabled;   // only restored in this narrow case
}
hasMovementLockSnapshot = false;   // but snapshot discarded regardless -> can never recover
movementLocked = false;
```

---

## BUG #6
- **File:** `/Users/andrian/GitHub/TIDE/Assets/BattleHudPolishService.cs`
- **Line:** 74–96 (PlayCritFlash/PlayHitFlash/PlayStatusPulse)
- **Category:** Coroutine cancellation issue — overlapping visual FX corrupt color
- **Severity:** Medium
- **Description:** Each helper starts a fresh coroutine (`FlashRoutine`/`SpriteFlashRoutine`/`PulseRoutine`) on the target with **no tracking or stopping of a previously-running flash on that same target** — a direct violation of AGENTS.md's "Stop previous coroutine before starting new visual effect" rule. `FlashRoutine` captures `Color original = target.color;` at its start and restores it at the end. If `PlayCritFlash` (or `PlayHitFlash`) is called again before the first finishes, the second captures a *mid-flash* color as its "original"; the two routines then fight over `target.color` every frame and, on completion, the second restores the target to that stale mid-flash color — leaving the sprite/Image tinted incorrectly.
- **Suggested Fix:** Track the active coroutine per-target (e.g. a `Dictionary<Image, Coroutine>` / `Dictionary<SpriteRenderer, Coroutine>`) and `StopCoroutine` the previous one before starting a new flash.
- **Code Snippet:**
```csharp
public static Coroutine PlayCritFlash(MonoBehaviour host, Image target)
{
    if (host == null || target == null) return null;
    return host.StartCoroutine(FlashRoutine(target, GetCritFlashColor(), GetCritFlashDuration())); // no prior cancellation
}
// FlashRoutine: Color original = target.color; ... target.color = Color.Lerp(flashColor, original, t); ... target.color = original;
```

## BUG #7
- **File:** `/Users/andrian/GitHub/TIDE/Assets/BossIntroDirector.cs`
- **Line:** 370–390 (PulseAtmosphere)
- **Category:** Coroutine yield/visual-FX — skip leaves overlay color unrestored
- **Severity:** Low-Medium
- **Description:** `PulseAtmosphere` captures `Color baseColor = atmosphereOverlay.color;` and only restores `atmosphereOverlay.color = baseColor;` **after** the while loop (line 389). Inside the loop, `if (skipRequested) yield break;` (line 381) exits the coroutine immediately without restoring the base color, leaving the atmosphere overlay stuck at the last pulse color (`pulseColor` with a pulsing alpha). Because each call re-captures `baseColor` from the current color, subsequent pulses compound the drift, so the intro's atmosphere tint diverges from the intended boss color after any skip.
- **Suggested Fix:** Restore `atmosphereOverlay.color = baseColor;` before `yield break` on skip (or use try/finally-style cleanup).
- **Code Snippet:**
```csharp
Color baseColor = atmosphereOverlay.color;
float elapsed = 0f;
while (elapsed < duration)
{
    if (skipRequested) yield break;          // <-- exits without restoring baseColor
    ...
    atmosphereOverlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha);
    yield return null;
}
atmosphereOverlay.color = baseColor;        // only reached on normal completion
```

---

## BUG #8
- **File:** `/Users/andrian/GitHub/TIDE/Assets/MobileTouchInputManager.cs`
- **Line:** 311–316 (UpdateJoystickKnob)
- **Category:** Division by zero (Mathf clamp on 0 missing)
- **Severity:** Low
- **Description:** `float clampedDistance = Mathf.Min(localPoint.magnitude, joystickRadius);` followed by `float normalized = clampedDistance / joystickRadius;` (line 316). `joystickRadius` is a raw `[SerializeField]` float (default 100) with no lower-bound clamp. If a designer sets it to 0 (or it's otherwise 0), `clampedDistance` becomes 0 and `0f / 0f` yields `NaN`; that NaN then flows into `adjusted = (normalized - joystickDeadZone) / (1f - joystickDeadZone);` and into `moveH`/`moveV`, producing `NaN` movement that propagates into `IsometricPlayer` velocity and breaks locomotion. AGENTS.md explicitly flags unguarded divisions as a bug category.
- **Suggested Fix:** Clamp the radius: `float radius = Mathf.Max(0.0001f, joystickRadius);` and use `radius` for the clamp/division.
- **Code Snippet:**
```csharp
float clampedDistance = Mathf.Min(localPoint.magnitude, joystickRadius);
Vector2 clamped = localPoint.normalized * clampedDistance;
joystickKnob.anchoredPosition = clamped;
float normalized = clampedDistance / joystickRadius;   // NaN when joystickRadius == 0
```

## Notes on items checked and cleared (not bugs)

- **Arial.ttf:** No `.cs` file references `Arial.ttf`/`Arial` — all UI code uses `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`. Confirmed clean. (Matches exist only in `AGENTS.md` and docs as documentation of the rule.)
- **FuturisticSpriteLibrary.cs `while(true)` (line 1043):** Standard Bresenham line algorithm (`DrawLine`); it advances toward `(x1,y1)` and `break`s on arrival — terminates correctly, not an infinite loop.
- **IsometricPlayer `Time.fixedDeltaTime` (line 834):** `UpdateCameraPolish()` is called from `FixedUpdate()` (line 499), so `Time.fixedDeltaTime` is the correct delta there (not a deltaTime-misuse bug).
- **StoryAct act-pitch indexing:** `GameStateManager.StoryAct` is 1-indexed (`ActI = 1, ActII = 2, ActIII = 3`), and `IslandAudioProfile.GetActPitch(actNumber)` converts to 0-based via `actNumber - 1` — indexing is correct, no off-by-one there.
- **All event invocations** in the audited files use `?.Invoke()` (no bare-`Invoke` NRE risks were found).

