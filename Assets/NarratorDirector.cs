using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a narrator framing device for the entire game.
/// Shows opening narration before the ceremony, chapter transitions between acts,
/// and ending narration after the final cinematic (including the "book closing" effect).
/// All UI is built procedurally at runtime on a high sort-order canvas.
/// </summary>
[DisallowMultipleComponent]
public class NarratorDirector : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Singleton
    // ------------------------------------------------------------------ //

    public static NarratorDirector Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  Timing
    // ------------------------------------------------------------------ //

    [Header("Timing")]
    [SerializeField] private float typewriterSpeed = 0.035f;
    [SerializeField] private float linePauseDuration = 1.2f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float bookCloseDuration = 2.0f;

    // ------------------------------------------------------------------ //
    //  State
    // ------------------------------------------------------------------ //

    private Canvas narratorCanvas;
    private CanvasGroup fadePanel;
    private Text dialogueText;
    private Text titleText;
    private bool isPlaying;

    /// <summary>True while a narration sequence is actively playing.</summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// True if the opening narration has been played and persisted via GameStateManager.
    /// </summary>
    public bool HasPlayedOpening =>
        GameStateManager.Instance != null
        && GameStateManager.Instance.IsNarrativeBeatCompleted("narrator_opening");

    /// <summary>Raised when any narration sequence completes.</summary>
    public event Action OnNarrationComplete;

    // ------------------------------------------------------------------ //
    //  Beat IDs (persisted through GameStateManager)
    // ------------------------------------------------------------------ //

    private const string OpeningBeatId = "narrator_opening";
    private const string ChapterTransitionFormat = "narrator_chapter_{0}_to_{1}";

    private static string ChapterTransitionBeatId(int fromAct, int toAct)
    {
        return string.Format(ChapterTransitionFormat, fromAct, toAct);
    }

    private static string EndingBeatId(GameStateManager.EndingBranch branch)
    {
        return $"narrator_ending_{branch}";
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
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Plays the opening narration before the ceremony.
    /// "Once, in a world not so different from ours..."
    /// Only plays once per game (tracked via narrative beat).
    /// Safe to call multiple times -- subsequent calls are no-ops.
    /// </summary>
    public void PlayOpeningNarration()
    {
        if (isPlaying)
        {
            return;
        }

        if (HasPlayedOpening)
        {
            return;
        }

        isPlaying = true;
        EnsureUI();
        StartCoroutine(RunOpeningNarration());
    }

    /// <summary>
    /// Plays a chapter transition narration between acts.
    /// Only plays once per transition (tracked via narrative beat).
    /// Safe to call multiple times -- subsequent calls are no-ops.
    /// </summary>
    /// <param name="fromAct">The act being left (1-3).</param>
    /// <param name="toAct">The act being entered (1-3).</param>
    public void PlayChapterTransition(int fromAct, int toAct)
    {
        if (isPlaying)
        {
            return;
        }

        if (fromAct < 1 || fromAct > 3 || toAct < 1 || toAct > 3 || fromAct == toAct)
        {
            return;
        }

        string beatId = ChapterTransitionBeatId(fromAct, toAct);

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null && gsm.IsNarrativeBeatCompleted(beatId))
        {
            return;
        }

        isPlaying = true;
        EnsureUI();
        StartCoroutine(RunChapterTransition(fromAct, toAct, beatId));
    }

    /// <summary>
    /// Plays the ending narration after the cinematic sequence.
    /// Includes the narrator finishing the story and the "book closing" effect.
    /// Safe to call multiple times -- subsequent calls are no-ops.
    /// </summary>
    /// <param name="branch">Which ending branch was resolved (Good or Bad).</param>
    public void PlayEndingNarration(GameStateManager.EndingBranch branch)
    {
        if (isPlaying)
        {
            return;
        }

        if (branch == GameStateManager.EndingBranch.None)
        {
            return;
        }

        isPlaying = true;
        EnsureUI();
        StartCoroutine(RunEndingNarration(branch));
    }

    // ------------------------------------------------------------------ //
    //  Opening Narration
    // ------------------------------------------------------------------ //

    private IEnumerator RunOpeningNarration()
    {
        // Fade from black.
        yield return StartCoroutine(Fade(0f, 1f, 0.1f));
        yield return new WaitForSeconds(0.3f);

        // Narration lines.
        string[] lines = new string[]
        {
            "Once, in a world not so different from ours,",
            "there lived five children who did not know they were special...",
            "This is the story of how they became heroes...",
            "...and what it cost them."
        };

        yield return StartCoroutine(PlayDialogueLines(lines));

        yield return new WaitForSeconds(linePauseDuration);

        // Fade to gameplay.
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Persist the beat.
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.MarkNarrativeBeatCompleted(OpeningBeatId);
        }

        isPlaying = false;
        OnNarrationComplete?.Invoke();
        CleanupUI();
    }

    // ------------------------------------------------------------------ //
    //  Chapter Transitions
    // ------------------------------------------------------------------ //

    private string GetChapterTransitionText(int fromAct, int toAct)
    {
        if (fromAct == 1 && toAct == 2)
        {
            return "The texts told them what they were.\nBut knowing is not the same as understanding.";
        }

        if (fromAct == 2 && toAct == 3)
        {
            return "By the time they reached the final shore,\nthe truth was no longer a whisper. It was a scream.";
        }

        if (fromAct == 3 && toAct == 3)
        {
            return "And so they stood before the last horizon,\nknowing what waited beyond.";
        }

        return "The story continues...";
    }

    private IEnumerator RunChapterTransition(int fromAct, int toAct, string beatId)
    {
        // Fade to black.
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(0.5f);

        // Show chapter transition text.
        string text = GetChapterTransitionText(fromAct, toAct);
        yield return StartCoroutine(TypewriterLine(text));

        yield return new WaitForSeconds(linePauseDuration * 2f);

        // Fade to gameplay.
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Persist the beat.
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.MarkNarrativeBeatCompleted(beatId);
        }

        isPlaying = false;
        OnNarrationComplete?.Invoke();
        CleanupUI();
    }

    // ------------------------------------------------------------------ //
    //  Ending Narration
    // ------------------------------------------------------------------ //

    private IEnumerator RunEndingNarration(GameStateManager.EndingBranch branch)
    {
        // Fade from whatever state to black.
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(0.5f);

        if (branch == GameStateManager.EndingBranch.Good)
        {
            string[] goodLines = new string[]
            {
                "And so the tale ends, as all tides must... in balance.",
                "The five found peace not in victory, but in acceptance.",
                "Their story is told to children now,",
                "a reminder that some purposes are worth any price."
            };
            yield return StartCoroutine(PlayDialogueLines(goodLines));
        }
        else
        {
            string[] badLines = new string[]
            {
                "And so the tale ends... not in balance, but in silence.",
                "One survived, but survival without purpose is merely breathing.",
                "His story is told to children too,",
                "a warning that despair is the only true enemy."
            };
            yield return StartCoroutine(PlayDialogueLines(badLines));
        }

        yield return new WaitForSeconds(linePauseDuration);

        // Final line: "The book closes."
        yield return StartCoroutine(TypewriterLine("The book closes."));

        yield return new WaitForSeconds(1.5f);

        // "Book closing" effect: left and right panels slide inward to meet at center.
        yield return StartCoroutine(BookCloseEffect());

        // Persist the beat.
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.MarkNarrativeBeatCompleted(EndingBeatId(branch));
        }

        isPlaying = false;
        OnNarrationComplete?.Invoke();
        CleanupUI();
    }

    /// <summary>
    /// Animates two vertical panels sliding inward from the edges, simulating a book closing.
    /// Left panel moves from x=-1 to x=0, right panel from x=1 to x=0.
    /// Both are dark brown to evoke a book cover.
    /// </summary>
    private IEnumerator BookCloseEffect()
    {
        if (narratorCanvas == null)
        {
            yield break;
        }

        Transform canvasTransform = narratorCanvas.transform;

        // Left page (moves rightward to center).
        RectTransform leftPage = CreateBookPage("BookPageLeft", canvasTransform,
            new Vector2(-1f, 0f), new Vector2(0f, 1f));
        Image leftImg = leftPage.GetComponent<Image>();
        if (leftImg != null) leftImg.color = new Color(0.15f, 0.08f, 0.03f, 1f);

        // Right page (moves leftward to center).
        RectTransform rightPage = CreateBookPage("BookPageRight", canvasTransform,
            new Vector2(1f, 0f), new Vector2(2f, 1f));
        Image rightImg = rightPage.GetComponent<Image>();
        if (rightImg != null) rightImg.color = new Color(0.15f, 0.08f, 0.03f, 1f);

        leftPage.gameObject.SetActive(true);
        rightPage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < bookCloseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bookCloseDuration);
            // Ease in-out for smooth motion.
            float eased = t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            // Left page: anchor x goes from -1 to 0.
            Vector2 leftMin = leftPage.anchorMin;
            Vector2 leftMax = leftPage.anchorMax;
            leftMin.x = Mathf.Lerp(-1f, 0f, eased);
            leftMax.x = Mathf.Lerp(0f, 1f, eased);
            leftPage.anchorMin = leftMin;
            leftPage.anchorMax = leftMax;

            // Right page: anchor x goes from 1 to 0 (min), 2 to 1 (max).
            Vector2 rightMin = rightPage.anchorMin;
            Vector2 rightMax = rightPage.anchorMax;
            rightMin.x = Mathf.Lerp(1f, 0f, eased);
            rightMax.x = Mathf.Lerp(2f, 1f, eased);
            rightPage.anchorMin = rightMin;
            rightPage.anchorMax = rightMax;

            yield return null;
        }

        // Ensure fully closed.
        leftPage.anchorMin = new Vector2(0f, 0f);
        leftPage.anchorMax = new Vector2(1f, 1f);
        rightPage.anchorMin = new Vector2(0f, 0f);
        rightPage.anchorMax = new Vector2(1f, 1f);

        yield return new WaitForSeconds(1.0f);
    }

    private static RectTransform CreateBookPage(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(0.15f, 0.08f, 0.03f, 1f);

        go.SetActive(false);
        return rect;
    }

    // ------------------------------------------------------------------ //
    //  Dialogue Helpers
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
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    private IEnumerator TypewriterLine(string fullText)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        dialogueText.text = "";
        for (int i = 1; i <= fullText.Length; i++)
        {
            dialogueText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ------------------------------------------------------------------ //
    //  Fade Helper
    // ------------------------------------------------------------------ //

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadePanel == null)
        {
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

    // ------------------------------------------------------------------ //
    //  Runtime UI Construction
    // ------------------------------------------------------------------ //

    private void EnsureUI()
    {
        if (narratorCanvas != null && fadePanel != null && dialogueText != null)
        {
            return;
        }

        BuildUI();
    }

    private void BuildUI()
    {
        // Canvas (sort order 2500, above everything including EndingSequenceDirector at 999).
        GameObject canvasGo = new GameObject("NarratorCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        narratorCanvas = canvasGo.AddComponent<Canvas>();
        narratorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        narratorCanvas.sortingOrder = 2500;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        // Fade panel (full-screen black overlay).
        fadePanel = CreateFullStretchCanvasGroup("FadePanel", canvasGo.transform, Color.black);
        fadePanel.alpha = 1f; // Start black.
        fadePanel.gameObject.SetActive(true);

        // Dialogue panel (bottom area, dark semi-transparent background).
        GameObject panelGo = new GameObject("DialoguePanel", typeof(RectTransform));
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f);
        panelRect.anchorMax = new Vector2(0.95f, 0.25f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.7f);
        panelBg.raycastTarget = false;

        // Dialogue text.
        GameObject textGo = new GameObject("NarrationText", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        dialogueText = textGo.AddComponent<Text>();
        dialogueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dialogueText.fontSize = 26;
        dialogueText.alignment = TextAnchor.MiddleCenter;
        dialogueText.color = new Color(0.95f, 0.92f, 0.85f, 1f);
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
        dialogueText.raycastTarget = false;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        // Title text (centered, larger -- used for "The End" style cards if needed).
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
        titleGo.transform.SetParent(canvasGo.transform, false);
        titleText = titleGo.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 56;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyle.Bold;
        titleText.raycastTarget = false;

        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.35f);
        titleRect.anchorMax = new Vector2(0.9f, 0.65f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private void CleanupUI()
    {
        if (narratorCanvas != null)
        {
            Destroy(narratorCanvas.gameObject);
            narratorCanvas = null;
        }

        fadePanel = null;
        dialogueText = null;
        titleText = null;
    }

    private static CanvasGroup CreateFullStretchCanvasGroup(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        go.SetActive(false);
        return cg;
    }
}
