using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the two ending cinematics (Good/Bad) after the final boss is resolved.
/// Attach to a persistent GameObject or let <see cref="GameStateManager"/> create one on demand.
/// All visual elements are built at runtime if the serialized references are left empty.
/// </summary>
[DisallowMultipleComponent]
public class EndingSequenceDirector : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector fields
    // ------------------------------------------------------------------ //

    [Header("References")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text titleText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup flashOverlay;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Button returnButton;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float typewriterSpeed = 0.035f;
    [SerializeField] private float linePauseDuration = 1.2f;
    [SerializeField] private float heroFadeInterval = 0.8f;
    [SerializeField] private float postBeatPause = 1.5f;
    [SerializeField] private float theEndDisplayDuration = 3.0f;
    [SerializeField] private float flashDuration = 0.25f;

    [Header("Scene Assets")]
    [SerializeField] private Image[] heroSilhouettes;
    [SerializeField] private Image mainCharacterSilhouette;
    [SerializeField] private Color sunsetColor = new Color(0.95f, 0.55f, 0.2f, 1f);
    [SerializeField] private Color sunsetSkyColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    [SerializeField] private Color heroColor = new Color(0.9f, 0.85f, 0.75f, 1f);
    [SerializeField] private Color heroFadedColor = new Color(0.9f, 0.85f, 0.75f, 0f);
    [SerializeField] private Color mainCharColor = new Color(0.85f, 0.8f, 0.7f, 1f);

    [Header("Settings")]
    [SerializeField] private string titleSceneName = "TitleScene";

    // ------------------------------------------------------------------ //
    //  Singleton
    // ------------------------------------------------------------------ //

    public static EndingSequenceDirector Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    public bool IsPlaying { get; private set; }

    /// <summary>Raised once the entire sequence (including "The End") has finished.</summary>
    public event Action OnEndingComplete;

    /// <summary>
    /// Kick off the ending cinematic for the given branch.
    /// Safe to call multiple times -- subsequent calls are ignored while a sequence is running.
    /// </summary>
    public void PlayEnding(GameStateManager.EndingBranch branch)
    {
        if (IsPlaying)
        {
            return;
        }

        if (branch == GameStateManager.EndingBranch.None)
        {
            return;
        }

        IsPlaying = true;
        EnsureUI();
        StartCoroutine(RunEndingSequence(branch));
    }

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Polls <see cref="GameStateManager.IsEndingTriggered"/> so the sequence can start
    /// even if nothing explicitly calls <see cref="PlayEnding"/>.
    /// </summary>
    private void Update()
    {
        if (IsPlaying)
        {
            return;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null || !gsm.IsEndingTriggered)
        {
            return;
        }

        // Avoid re-triggering after the sequence has already completed once this session.
        if (lastEndingTriggeredState)
        {
            return;
        }

        lastEndingTriggeredState = true;
        PlayEnding(gsm.ResolvedEndingBranch);
    }

    private bool lastEndingTriggeredState;

    // ------------------------------------------------------------------ //
    //  Main sequence coroutine
    // ------------------------------------------------------------------ //

    private IEnumerator RunEndingSequence(GameStateManager.EndingBranch branch)
    {
        // 1. Fade to black.
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // 2. Prepare scene behind the black panel.
        SetBackgroundColor(Color.black);
        HideAllVisuals();
        SetDialogueText("");
        SetTitleText("");
        SetReturnButtonActive(false);

        yield return new WaitForSeconds(0.6f);

        // 3. Play the branch-specific cinematic (dialogue + visuals).
        if (branch == GameStateManager.EndingBranch.Good)
        {
            yield return StartCoroutine(PlayGoodEnding());
        }
        else
        {
            yield return StartCoroutine(PlayBadEnding());
        }

        // 4. "The End" title card.
        yield return StartCoroutine(ShowTheEndCard());

        // 5. Return-to-title prompt.
        ShowReturnToTitle();

        IsPlaying = false;
        OnEndingComplete?.Invoke();
    }

    // ------------------------------------------------------------------ //
    //  Good ending
    // ------------------------------------------------------------------ //

    private IEnumerator PlayGoodEnding()
    {
        // --- Dialogue on black ---
        string[] lines = new string[]
        {
            "The six enemies are gone...",
            "And with them, the need for the chosen five.",
            "The party understands at last —",
            "They were born only to restore balance.",
            "With balance restored, the Tide within them fades.",
            "Facing the sunset together,",
            "They accept that peace costs them their own fading light."
        };

        yield return StartCoroutine(PlayDialogueLines(lines));

        yield return new WaitForSeconds(postBeatPause);

        // --- Reveal the five heroes standing together ---
        if (heroSilhouettes != null)
        {
            for (int i = 0; i < heroSilhouettes.Length; i++)
            {
                if (heroSilhouettes[i] != null)
                {
                    heroSilhouettes[i].gameObject.SetActive(true);
                    SetImageColor(heroSilhouettes[i], heroColor);
                }
            }

            // Fade the panel away to reveal the scene.
            yield return StartCoroutine(Fade(1f, 0f, 1.0f));

            yield return new WaitForSeconds(0.8f);
        }

        // --- Fade heroes away one by one ---
        if (heroSilhouettes != null)
        {
            for (int i = 0; i < heroSilhouettes.Length; i++)
            {
                if (heroSilhouettes[i] != null)
                {
                    yield return StartCoroutine(
                        FadeImageColor(heroSilhouettes[i], heroColor, heroFadedColor, heroFadeInterval));
                }

                yield return new WaitForSeconds(0.15f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // --- Sunset background ---
        yield return StartCoroutine(LerpBackgroundColor(Color.black, sunsetSkyColor, 1.5f));

        if (backgroundImage != null)
        {
            backgroundImage.color = sunsetColor;
        }

        yield return new WaitForSeconds(2.0f);

        // --- Fade back to black before "The End" ---
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));
        SetBackgroundColor(Color.black);
    }

    // ------------------------------------------------------------------ //
    //  Bad ending
    // ------------------------------------------------------------------ //

    private IEnumerator PlayBadEnding()
    {
        // --- Dialogue on black ---
        string[] lines = new string[]
        {
            "The party falls before finishing its purpose.",
            "Only the main character remains.",
            "Despair twists fate into meaninglessness.",
            "On the hill at sunset,",
            "He dies believing the cycle ended in nothing."
        };

        yield return StartCoroutine(PlayDialogueLines(lines));

        yield return new WaitForSeconds(postBeatPause);

        // --- Reveal the lone main character ---
        if (mainCharacterSilhouette != null)
        {
            mainCharacterSilhouette.gameObject.SetActive(true);
            SetImageColor(mainCharacterSilhouette, mainCharColor);
        }

        // Fade panel away to reveal the scene.
        yield return StartCoroutine(Fade(1f, 0f, 1.0f));

        yield return new WaitForSeconds(1.2f);

        // --- Stabbing implication: screen flash ---
        yield return StartCoroutine(ScreenFlash());

        yield return new WaitForSeconds(0.4f);

        // --- Fade to black -- silence ---
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        yield return new WaitForSeconds(2.0f);
    }

    // ------------------------------------------------------------------ //
    //  "The End" card
    // ------------------------------------------------------------------ //

    private IEnumerator ShowTheEndCard()
    {
        SetTitleText("The End");
        SetTextAlpha(titleText, 0f);

        // Fade the title in.
        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(titleText, Mathf.Clamp01(elapsed / 1.5f));
            yield return null;
        }

        SetTextAlpha(titleText, 1f);
        yield return new WaitForSeconds(theEndDisplayDuration);

        // Fade the title out.
        elapsed = 0f;
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(titleText, 1f - Mathf.Clamp01(elapsed / 1.0f));
            yield return null;
        }

        SetTitleText("");
    }

    // ------------------------------------------------------------------ //
    //  Return to title
    // ------------------------------------------------------------------ //

    private void ShowReturnToTitle()
    {
        SetReturnButtonActive(true);
    }

    /// <summary>Called by the UI button.</summary>
    public void OnReturnToTitlePressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    // ------------------------------------------------------------------ //
    //  Dialogue helper
    // ------------------------------------------------------------------ //

    private IEnumerator PlayDialogueLines(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            yield return StartCoroutine(TypewriterLine(lines[i]));
            yield return new WaitForSeconds(linePauseDuration);
        }

        // Clear after last line.
        yield return new WaitForSeconds(0.5f);
        SetDialogueText("");
    }

    private IEnumerator TypewriterLine(string fullText)
    {
        SetDialogueText("");
        for (int i = 1; i <= fullText.Length; i++)
        {
            SetDialogueText(fullText.Substring(0, i));
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ------------------------------------------------------------------ //
    //  Visual effect helpers
    // ------------------------------------------------------------------ //

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadePanel == null || duration <= 0f)
        {
            if (fadePanel != null) fadePanel.alpha = to;
            yield break;
        }

        fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadePanel.alpha = to;

        if (to <= 0f)
        {
            fadePanel.gameObject.SetActive(false);
        }
    }

    private IEnumerator ScreenFlash()
    {
        if (flashOverlay == null || flashDuration <= 0f)
        {
            yield break;
        }

        flashOverlay.gameObject.SetActive(true);

        // Quick fade in.
        float elapsed = 0f;
        float halfDuration = flashDuration * 0.5f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            flashOverlay.alpha = Mathf.Clamp01(elapsed / halfDuration);
            yield return null;
        }

        flashOverlay.alpha = 1f;

        // Quick fade out.
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            flashOverlay.alpha = 1f - Mathf.Clamp01(elapsed / halfDuration);
            yield return null;
        }

        flashOverlay.alpha = 0f;
        flashOverlay.gameObject.SetActive(false);
    }

    private IEnumerator FadeImageColor(Image img, Color from, Color to, float duration)
    {
        if (img == null || duration <= 0f)
        {
            if (img != null) img.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            img.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        img.color = to;
    }

    private IEnumerator LerpBackgroundColor(Color from, Color to, float duration)
    {
        if (backgroundImage == null || duration <= 0f)
        {
            if (backgroundImage != null) backgroundImage.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            backgroundImage.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        backgroundImage.color = to;
    }

    // ------------------------------------------------------------------ //
    //  Runtime UI construction
    // ------------------------------------------------------------------ //

    private void EnsureUI()
    {
        if (fadePanel != null && dialogueText != null && titleText != null && backgroundImage != null)
        {
            // Build the flash overlay and button even when references are supplied.
            EnsureFlashOverlay();
            EnsureReturnButton();
            EnsureHeroSilhouettesIfNeeded();
            EnsureMainCharacterIfNeeded();
            return;
        }

        BuildFullUI();
    }

    private void BuildFullUI()
    {
        // -- Canvas --
        GameObject canvasGo = new GameObject("EndingSequenceCanvas");
        canvasGo.transform.SetParent(transform, false);
        uiCanvas = canvasGo.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // -- Background image (full screen) --
        if (backgroundImage == null)
        {
            backgroundImage = CreateFullStretchImage("Background", canvasGo.transform, Color.black);
        }

        // -- Dialogue text (bottom area) --
        if (dialogueText == null)
        {
            GameObject panelGo = new GameObject("DialoguePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.22f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(panelGo.transform, false);
            dialogueText = textGo.AddComponent<Text>();
            dialogueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dialogueText.fontSize = 22;
            dialogueText.alignment = TextAnchor.MiddleCenter;
            dialogueText.color = new Color(0.95f, 0.92f, 0.85f, 1f);
            dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogueText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);
        }

        // -- Title text ("The End") --
        if (titleText == null)
        {
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(canvasGo.transform, false);
            titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 56;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyle.Bold;

            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.35f);
            titleRect.anchorMax = new Vector2(0.9f, 0.65f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }

        // -- Fade panel (full screen black, on top of everything) --
        if (fadePanel == null)
        {
            fadePanel = CreateFullStretchCanvasGroup("FadePanel", canvasGo.transform, Color.black);
        }

        EnsureFlashOverlay();
        EnsureReturnButton();
        EnsureHeroSilhouettesIfNeeded();
        EnsureMainCharacterIfNeeded();
    }

    private void EnsureFlashOverlay()
    {
        if (flashOverlay != null)
        {
            return;
        }

        Canvas parentCanvas = uiCanvas;
        if (parentCanvas == null && fadePanel != null)
        {
            parentCanvas = fadePanel.GetComponentInParent<Canvas>();
        }

        if (parentCanvas == null)
        {
            return;
        }

        flashOverlay = CreateFullStretchCanvasGroup("FlashOverlay", parentCanvas.transform, Color.white);
        flashOverlay.alpha = 0f;
        flashOverlay.gameObject.SetActive(false);
    }

    private void EnsureReturnButton()
    {
        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(false);
            return;
        }

        Canvas parentCanvas = uiCanvas;
        if (parentCanvas == null && fadePanel != null)
        {
            parentCanvas = fadePanel.GetComponentInParent<Canvas>();
        }

        if (parentCanvas == null)
        {
            return;
        }

        GameObject btnGo = new GameObject("ReturnToTitleButton");
        btnGo.transform.SetParent(parentCanvas.transform, false);

        RectTransform btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.08f);
        btnRect.anchorMax = new Vector2(0.65f, 0.16f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnBg = btnGo.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        returnButton = btnGo.AddComponent<Button>();
        ColorBlock cb = returnButton.colors;
        cb.highlightedColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        cb.pressedColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        returnButton.colors = cb;
        returnButton.onClick.AddListener(OnReturnToTitlePressed);

        // Button label.
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);

        Text btnLabel = labelGo.AddComponent<Text>();
        btnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnLabel.fontSize = 20;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color = new Color(0.9f, 0.88f, 0.82f, 1f);
        btnLabel.text = "Return to Title";

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        btnGo.SetActive(false);
    }

    private void EnsureHeroSilhouettesIfNeeded()
    {
        if (heroSilhouettes != null && heroSilhouettes.Length > 0)
        {
            return;
        }

        Canvas parentCanvas = uiCanvas;
        if (parentCanvas == null && fadePanel != null)
        {
            parentCanvas = fadePanel.GetComponentInParent<Canvas>();
        }

        if (parentCanvas == null)
        {
            return;
        }

        heroSilhouettes = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            heroSilhouettes[i] = CreateHeroSilhouette(
                parentCanvas.transform, $"Hero_{i}", i, 5);
            heroSilhouettes[i].gameObject.SetActive(false);
        }
    }

    private void EnsureMainCharacterIfNeeded()
    {
        if (mainCharacterSilhouette != null)
        {
            return;
        }

        Canvas parentCanvas = uiCanvas;
        if (parentCanvas == null && fadePanel != null)
        {
            parentCanvas = fadePanel.GetComponentInParent<Canvas>();
        }

        if (parentCanvas == null)
        {
            return;
        }

        mainCharacterSilhouette = CreateHeroSilhouette(
            parentCanvas.transform, "MainCharacter", 0, 1);
        mainCharacterSilhouette.rectTransform.anchorMin = new Vector2(0.4f, 0.25f);
        mainCharacterSilhouette.rectTransform.anchorMax = new Vector2(0.6f, 0.75f);
        mainCharacterSilhouette.rectTransform.offsetMin = Vector2.zero;
        mainCharacterSilhouette.rectTransform.offsetMax = Vector2.zero;
        mainCharacterSilhouette.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  Primitive UI factory helpers
    // ------------------------------------------------------------------ //

    private static Image CreateFullStretchImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = color;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return img;
    }

    private static CanvasGroup CreateFullStretchCanvasGroup(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = color;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        return cg;
    }

    private static Image CreateHeroSilhouette(Transform parent, string name,
                                              int index, int total)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.9f, 0.85f, 0.75f, 1f);

        RectTransform rect = go.GetComponent<RectTransform>();

        // Arrange silhouettes in a horizontal row centred on screen.
        float totalWidth = Mathf.Min(0.7f, total * 0.14f);
        float spacing = total > 1 ? totalWidth / (total - 1) : 0f;
        float startX = 0.5f - totalWidth * 0.5f;
        float x = total > 1 ? startX + spacing * index : 0.5f;

        float silhouetteWidth = 0.1f;

        rect.anchorMin = new Vector2(x - silhouetteWidth * 0.5f, 0.25f);
        rect.anchorMax = new Vector2(x + silhouetteWidth * 0.5f, 0.75f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return img;
    }

    // ------------------------------------------------------------------ //
    //  Setters for optional references (keep null-checks clean)
    // ------------------------------------------------------------------ //

    private void SetBackgroundColor(Color c)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = c;
        }
    }

    private void SetDialogueText(string msg)
    {
        if (dialogueText != null)
        {
            dialogueText.text = msg;
        }
    }

    private void SetTitleText(string msg)
    {
        if (titleText != null)
        {
            titleText.text = msg;
        }
    }

    private static void SetTextAlpha(Text t, float a)
    {
        if (t == null)
        {
            return;
        }

        Color c = t.color;
        c.a = a;
        t.color = c;
    }

    private static void SetImageColor(Image img, Color c)
    {
        if (img != null)
        {
            img.color = c;
        }
    }

    private void HideAllVisuals()
    {
        if (heroSilhouettes != null)
        {
            for (int i = 0; i < heroSilhouettes.Length; i++)
            {
                if (heroSilhouettes[i] != null)
                {
                    heroSilhouettes[i].gameObject.SetActive(false);
                }
            }
        }

        if (mainCharacterSilhouette != null)
        {
            mainCharacterSilhouette.gameObject.SetActive(false);
        }
    }

    private void SetReturnButtonActive(bool active)
    {
        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(active);
        }
    }
}
