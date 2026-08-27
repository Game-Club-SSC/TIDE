using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the coming-of-age ceremony cinematic intro at game start.
/// Fades in from black, shows narrative text cards, displays the five
/// hero names with their elements, and fades into gameplay.
///
/// Wire up via <see cref="GameStateManager"/>: if CeremonyIntroCompleted is false
/// when the main scene loads, this director is auto-found and PlayCeremonyIntro() is called.
/// </summary>
[DisallowMultipleComponent]
public class CeremonyIntroDirector : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Configurable timing
    // ------------------------------------------------------------------ //

    [Header("Timing")]
    [SerializeField] private float initialBlackHold = 1.0f;
    [SerializeField] private float textFadeInDuration = 0.6f;
    [SerializeField] private float textHoldDuration = 3.0f;
    [SerializeField] private float textFadeOutDuration = 0.4f;
    [SerializeField] private float heroNameStaggerDelay = 0.8f;
    [SerializeField] private float heroNameHoldDuration = 3.5f;
    [SerializeField] private float tideFlashDuration = 1.2f;
    [SerializeField] private float finalFadeToBlackDuration = 0.5f;
    [SerializeField] private float finalBlackHold = 0.6f;

    [Header("Hero Display Names")]
    [Tooltip("Display names of the five chosen heroes. Must have exactly 5 entries.")]
    [SerializeField] private string[] heroDisplayNames = new string[]
    {
        "Ember", "Tidecaller", "Stoneheart", "Zephyr", "Voidwalker"
    };

    [Header("Hero Elements")]
    [Tooltip("Element label shown under each hero name. Same order as heroDisplayNames.")]
    [SerializeField] private string[] heroElementLabels = new string[]
    {
        "Fire", "Water", "Earth", "Air", "Space"
    };

    // ------------------------------------------------------------------ //
    //  Narrative text cards
    // ------------------------------------------------------------------ //

    private static readonly string[] NarrativeCards = new string[]
    {
        "At seventeen, every island gathers at the water's edge...",
        "Five ordinary youths step forward, carrying ordinary hopes...",
        "Then the Ceremony asks the Tide to answer...",
    };

    private const string PostFlashText = "You are the Chosen.\nThe Tide is your burden and your gift.";

    // ------------------------------------------------------------------ //
    //  State
    // ------------------------------------------------------------------ //

    private Canvas canvas;
    private CanvasGroup fadeOverlay;
    private Image fadeImage;
    private Text centreText;
    private bool isPlaying;
    private IsometricPlayer movementLockedPlayer;
    private bool movementLockSnapshot;
    private bool hasMovementLockSnapshot;

    /// <summary>Fired when the ceremony intro finishes.</summary>
    public event Action OnIntroFinished;

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>True if the intro has already been played and completed.</summary>
    public bool HasPlayedIntro { get; private set; }

    /// <summary>True while the ceremony intro sequence is running.</summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// Start the ceremony intro sequence. Call from GameStateManager when
    /// CeremonyIntroCompleted is false.
    /// </summary>
    public void PlayCeremonyIntro()
    {
        if (isPlaying)
        {
            return;
        }

        if (HasPlayedIntro)
        {
            return;
        }

        isPlaying = true;
        StartCoroutine(CeremonySequence());
    }

    /// <summary>
    /// Force the intro to be considered "played" without running it (debug/skip).
    /// </summary>
    public void SkipIntroForDebug()
    {
        HasPlayedIntro = true;
        MarkIntroCompleted();
        isPlaying = false;
        LockPlayerMovement(false);
        StopAllCoroutines();
        HideUI();
        OnIntroFinished?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Coroutine sequence
    // ------------------------------------------------------------------ //

    private IEnumerator CeremonySequence()
    {
        EnsureCanvas();
        ShowUI();

        // Lock player
        LockPlayerMovement(true);

        // Start fully black
        SetOverlayAlpha(1f);
        fadeImage.color = Color.black;
        SetCentreText(string.Empty);

        yield return new WaitForSecondsRealtime(initialBlackHold);

        // --- Narrative cards ---
        for (int i = 0; i < NarrativeCards.Length; i++)
        {
            yield return FadeTextIn(NarrativeCards[i]);
            yield return new WaitForSecondsRealtime(textHoldDuration);
            yield return FadeTextOut();
        }

        // --- Tide flash (fade to white) ---
        yield return TideFlash();

        // --- Hero names reveal ---
        yield return HeroRevealSequence();

        // --- Final message ---
        yield return FadeTextIn(PostFlashText);
        yield return new WaitForSecondsRealtime(textHoldDuration);
        yield return FadeTextOut();

        // --- Fade to black, then reveal gameplay ---
        fadeImage.color = Color.black;
        yield return FadeOverlay(1f, finalFadeToBlackDuration);
        yield return new WaitForSecondsRealtime(finalBlackHold);
        yield return FadeOverlay(0f, finalFadeToBlackDuration);

        // Done
        HasPlayedIntro = true;
        MarkIntroCompleted();
        isPlaying = false;
        LockPlayerMovement(false);
        HideUI();

        OnIntroFinished?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Text animations
    // ------------------------------------------------------------------ //

    private IEnumerator FadeTextIn(string text)
    {
        SetCentreText(text);
        yield return FadeTextAlpha(0f, 1f, textFadeInDuration);
    }

    private IEnumerator FadeTextOut()
    {
        yield return FadeTextAlpha(1f, 0f, textFadeOutDuration);
        SetCentreText(string.Empty);
    }

    private IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (centreText == null)
        {
            yield break;
        }

        Color c = centreText.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(from, to, t);
            centreText.color = c;
            yield return null;
        }

        c.a = to;
        centreText.color = c;
    }

    // ------------------------------------------------------------------ //
    //  Tide flash
    // ------------------------------------------------------------------ //

    private IEnumerator TideFlash()
    {
        // Fade text out first
        yield return FadeTextOut();

        // Fade overlay to white
        float elapsed = 0f;
        float halfDuration = tideFlashDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            fadeImage.color = Color.Lerp(Color.black, Color.white, t);
            yield return null;
        }

        fadeImage.color = Color.white;

        // Hold white briefly
        yield return new WaitForSecondsRealtime(0.3f);

        // Fade back to black
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            fadeImage.color = Color.Lerp(Color.white, Color.black, t);
            yield return null;
        }

        fadeImage.color = Color.black;
    }

    // ------------------------------------------------------------------ //
    //  Hero reveal
    // ------------------------------------------------------------------ //

    private IEnumerator HeroRevealSequence()
    {
        int count = Mathf.Min(heroDisplayNames.Length, heroElementLabels.Length, 5);

        // Reveal each chosen hero one by one with a staggered cadence.
        for (int i = 0; i < count; i++)
        {
            string heroText = $"<size=36>{heroDisplayNames[i]}</size>\n<size=22>{heroElementLabels[i]}</size>";
            yield return FadeTextIn(heroText);
            yield return new WaitForSecondsRealtime(heroNameStaggerDelay);
            yield return FadeTextOut();
        }

        // Hold the full roster so the player can read all five chosen names
        // before the ceremony continues (uses heroNameHoldDuration).
        yield return FadeTextIn(BuildHeroRosterText(count));
        yield return new WaitForSecondsRealtime(heroNameHoldDuration);
        yield return FadeTextOut();
    }

    private string BuildHeroRosterText(int count)
    {
        string roster = string.Empty;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                roster += "\n";
            }
            roster += $"<size=30>{heroDisplayNames[i]}</size> <size=18>{heroElementLabels[i]}</size>";
        }
        return roster;
    }

    // ------------------------------------------------------------------ //
    //  Overlay fade
    // ------------------------------------------------------------------ //

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (fadeOverlay == null)
        {
            yield break;
        }

        float startAlpha = fadeOverlay.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeOverlay.alpha = targetAlpha;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = alpha;
        }
    }

    // ------------------------------------------------------------------ //
    //  Canvas construction
    // ------------------------------------------------------------------ //

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObj = new GameObject("CeremonyIntroCanvas");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000; // above everything else

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Fade overlay (full-screen black/white image) ---
        GameObject overlayObj = CreateUIElement("FadeOverlay", canvasObj.transform);
        fadeOverlay = overlayObj.AddComponent<CanvasGroup>();
        fadeImage = overlayObj.AddComponent<Image>();
        fadeImage.color = Color.black;

        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // --- Centre text ---
        GameObject textObj = CreateUIElement("CeremonyText", canvasObj.transform);
        centreText = textObj.AddComponent<Text>();
        centreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        centreText.fontSize = 28;
        centreText.alignment = TextAnchor.MiddleCenter;
        centreText.color = Color.white;
        centreText.horizontalOverflow = HorizontalWrapMode.Wrap;
        centreText.verticalOverflow = VerticalWrapMode.Overflow;
        centreText.lineSpacing = 1.2f;
        centreText.supportRichText = true;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.25f);
        textRect.anchorMax = new Vector2(0.9f, 0.75f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Start hidden
        SetOverlayAlpha(0f);
        SetCentreText(string.Empty);
    }

    private void ShowUI()
    {
        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private void HideUI()
    {
        if (canvas != null && canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private static GameObject CreateUIElement(string elementName, Transform parent)
    {
        GameObject obj = new GameObject(elementName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return obj;
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private void SetCentreText(string text)
    {
        if (centreText != null)
        {
            centreText.text = text ?? string.Empty;
        }
    }

    private void LockPlayerMovement(bool locked)
    {
        if (locked)
        {
            if (hasMovementLockSnapshot)
            {
                return;
            }

            movementLockedPlayer = FindFirstObjectByType<IsometricPlayer>();
            if (movementLockedPlayer == null)
            {
                return;
            }

            movementLockSnapshot = movementLockedPlayer.canMove;
            hasMovementLockSnapshot = true;
            movementLockedPlayer.canMove = false;
            return;
        }

        RestorePlayerMovement();
    }

    private void RestorePlayerMovement()
    {
        if (!hasMovementLockSnapshot)
        {
            return;
        }

        if (movementLockedPlayer != null)
        {
            movementLockedPlayer.canMove = movementLockSnapshot;
        }

        movementLockedPlayer = null;
        hasMovementLockSnapshot = false;
    }

    private void MarkIntroCompleted()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.CeremonyIntroCompleted = true;
        }
    }

    private void OnDestroy()
    {
        RestorePlayerMovement();
        StopAllCoroutines();
    }
}
