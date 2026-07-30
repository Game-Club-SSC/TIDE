using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays a unique intro cutscene before a boss encounter begins.
/// Detects which boss is being faced from GameStateManager.PendingCombatIslandId,
/// shows a per-boss sequence with typewriter text and atmosphere effects,
/// and provides a skip prompt. When the intro finishes, it requests
/// BattleManager to start the fight.
/// </summary>
[DisallowMultipleComponent]
public class BossIntroDirector : MonoBehaviour
{
    public static BossIntroDirector Instance { get; private set; }

    // ──────────────────────────────────────────────────────────────────
    //  Serialized Inspector Fields
    // ──────────────────────────────────────────────────────────────────

    [Header("UI References (auto-built if null)")]
    [SerializeField] private Canvas introCanvas;
    [SerializeField] private Image atmosphereOverlay;
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text locationText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text skipPromptText;

    [Header("Typewriter")]
    [SerializeField] private float typewriterCharInterval = 0.035f;
    [SerializeField] private float lineDelay = 1.2f;
    [SerializeField] private float atmosphereRevealDuration = 0.8f;
    [SerializeField] private float introHoldDuration = 1.5f;
    [SerializeField] private float skipPromptDelay = 0.5f;

    [Header("Pulse")]
    [SerializeField] private float pulseAmplitude = 0.08f;
    [SerializeField] private float pulseFrequency = 1.2f;

    [Header("Skip")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private string skipButtonLabel = "Press SPACE to skip";
    [SerializeField] private bool allowMouseSkipOnMobile = true;
    [SerializeField] private float introTimeout = 8f;

    // ──────────────────────────────────────────────────────────────────
    //  Internal State
    // ──────────────────────────────────────────────────────────────────

    private enum IntroPhase { Idle, AtmosphereReveal, Dialogue, Completed }

    private IntroPhase currentPhase = IntroPhase.Idle;
    private bool skipRequested;
    private bool isPlaying;
    private Coroutine activeRoutine;
    private BossNarrativeMechanic bossMechanic;
    private string pendingIslandId;

    // ──────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────────

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
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isPlaying || currentPhase == IntroPhase.Idle || currentPhase == IntroPhase.Completed)
        {
            return;
        }

        if (Input.GetKeyDown(skipKey))
        {
            skipRequested = true;
        }

        if (!allowMouseSkipOnMobile && (Application.isMobilePlatform))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public Entry Point
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the boss intro sequence. Call this after the combat scene is
    /// built but before BattleManager.StartBattle().
    /// </summary>
    public void PlayIntro()
    {
        if (isPlaying)
        {
            return;
        }

        BossNarrativeMechanic mechanic = ResolveBossMechanic();
        if (mechanic == null)
        {
            Debug.LogWarning("[BossIntroDirector] No boss mechanic found for current encounter. Skipping intro.");
            CompleteAndStartBattle();
            return;
        }

        bossMechanic = mechanic;
        isPlaying = true;
        skipRequested = false;

        activeRoutine = StartCoroutine(RunIntroSequence());
    }

    // ──────────────────────────────────────────────────────────────────
    //  Intro Sequence
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RunIntroSequence()
    {
        EnsureIntroUI();
        ShowCanvas(true);

        // Phase 1: Atmosphere reveal — fade in the boss color overlay
        currentPhase = IntroPhase.AtmosphereReveal;
        yield return StartCoroutine(RevealAtmosphere());

        // Show boss name
        if (bossNameText != null)
        {
            bossNameText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(bossNameText, bossMechanic.bossName));
            yield return new WaitForSeconds(0.3f);
        }

        // Show location
        if (locationText != null && !string.IsNullOrEmpty(bossMechanic.locationDescription))
        {
            locationText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(locationText, bossMechanic.locationDescription));
        }

        yield return new WaitForSeconds(introHoldDuration);

        // Phase 2: Boss-specific dialogue
        currentPhase = IntroPhase.Dialogue;

        // Per-boss unique sequence
        yield return StartCoroutine(PlayBossSpecificSequence());

        // Show the intro description text from BossNarrativeMechanic
        if (!string.IsNullOrEmpty(bossMechanic.introDescription))
        {
            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
                yield return StartCoroutine(TypewriteText(dialogueText, bossMechanic.introDescription));
                yield return new WaitForSeconds(lineDelay);
            }
        }

        // Show skip prompt after a brief delay
        if (skipPromptText != null)
        {
            yield return new WaitForSeconds(skipPromptDelay);
            skipPromptText.gameObject.SetActive(true);
        }

        // Wait for skip or auto-complete
        yield return StartCoroutine(WaitForSkipOrTimeout());

        currentPhase = IntroPhase.Completed;
        CompleteAndStartBattle();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Per-Boss Unique Sequences
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator PlayBossSpecificSequence()
    {
        if (bossMechanic == null || string.IsNullOrEmpty(bossMechanic.islandId))
        {
            yield break;
        }

        string islandId = bossMechanic.islandId;
        Color atmosphereColor = bossMechanic.atmosphereColor;

        switch (islandId)
        {
            case "island_greed":
                yield return StartCoroutine(GreedSequence(atmosphereColor));
                break;
            case "island_desire":
                yield return StartCoroutine(AttachmentSequence(atmosphereColor));
                break;
            case "island_envy":
                yield return StartCoroutine(JealousySequence(atmosphereColor));
                break;
            case "island_lust":
                yield return StartCoroutine(LustSequence(atmosphereColor));
                break;
            case "island_anger":
                yield return StartCoroutine(AngerSequence(atmosphereColor));
                break;
            case "island_ego":
                yield return StartCoroutine(EgoSequence(atmosphereColor));
                break;
        }
    }

    private IEnumerator GreedSequence(Color color)
    {
        // Gold temple — golden shimmer, wealth temptation
        yield return StartCoroutine(PulseAtmosphere(new Color(0.85f, 0.75f, 0.2f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "Gold. Everywhere gold."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "The temple groans under the weight of treasure. Coins spill like waterfalls."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "One of you reaches out instinctively. The floor trembles."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "\"Take nothing,\" someone whispers. But the gold does not want to be left behind."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator AttachmentSequence(Color color)
    {
        // Garden of memories — emotional reveals, past attachments
        yield return StartCoroutine(PulseAtmosphere(new Color(0.4f, 0.7f, 0.4f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "The garden blooms with things you thought you had buried."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "A faded ribbon. A broken locket. A name you no longer say."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "Every flower holds a memory. Every petal, a goodbye you never finished."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "The boss does not attack your bodies. It attacks your hearts."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator JealousySequence(Color color)
    {
        // Beach mirrors — idealized reflections, mind invasion
        yield return StartCoroutine(PulseAtmosphere(new Color(0.6f, 0.3f, 0.7f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "The beach is lined with mirrors. But they do not show your face."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "They show what you wish you were. Stronger. Better. More worthy."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "Your allies look different through the glass — and the glass whispers lies."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "The boss enters your mind. It shows you everything you are not."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator LustSequence(Color color)
    {
        // Enchanted moura — allure, enchantment shimmer
        yield return StartCoroutine(PulseAtmosphere(new Color(0.9f, 0.4f, 0.5f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "The air shimmers with a sweet perfume. Figures emerge from the haze."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "They smile. Their beauty is unsettling — too perfect, too knowing."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "You want what they offer. Everyone does. That is the trap."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "This is a test of gear, goods, and the wisdom to see through enchantment."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator AngerSequence(Color color)
    {
        // Rage escalation — burning air, party tension
        yield return StartCoroutine(PulseAtmosphere(new Color(0.9f, 0.2f, 0.1f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "The air burns. Every grievance you have ever swallowed rises to the surface."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "Old arguments echo. Unspoken words claw at your throat."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "The boss grins. It has been waiting for this."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "Say it. Say what you have been holding back. The boss feeds on rage."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator EgoSequence(Color color)
    {
        // Mountain peak — clarity becomes cruelty, ego manipulation
        yield return StartCoroutine(PulseAtmosphere(new Color(0.95f, 0.9f, 0.8f), 0.6f));
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(dialogueText, "The mountain peak offers clarity. And clarity, sometimes, is cruelty."));
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TypewriteText(dialogueText, "The boss speaks softly. It tells each of you why you are better than the others."));
            yield return new WaitForSeconds(0.6f);
            yield return StartCoroutine(TypewriteText(dialogueText, "Ego is not loud. It is reasonable. It sounds like the truth."));
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(TypewriteText(dialogueText, "You have come so far. Surely you deserve more than the rest."));
            yield return new WaitForSeconds(lineDelay);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Atmosphere Effects
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RevealAtmosphere()
    {
        if (atmosphereOverlay == null)
        {
            yield break;
        }

        Color targetColor = bossMechanic != null ? bossMechanic.atmosphereColor : Color.black;
        targetColor.a = 0.55f;

        atmosphereOverlay.gameObject.SetActive(true);
        atmosphereOverlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < atmosphereRevealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / atmosphereRevealDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            atmosphereOverlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * eased);
            yield return null;
        }

        atmosphereOverlay.color = targetColor;
    }

    private IEnumerator PulseAtmosphere(Color pulseColor, float duration)
    {
        Color baseColor = atmosphereOverlay != null ? atmosphereOverlay.color : default;

        if (atmosphereOverlay == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipRequested)
            {
                atmosphereOverlay.color = baseColor;
                yield break;
            }
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
            float alpha = Mathf.Clamp01(baseColor.a + pulse);
            atmosphereOverlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha);
            yield return null;
        }

        atmosphereOverlay.color = baseColor;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Wait / Skip
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator WaitForSkipOrTimeout()
    {
        float elapsed = 0f;

        while (elapsed < introTimeout)
        {
            if (skipRequested)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Typewriter
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator TypewriteText(Text target, string fullText)
    {
        if (target == null || string.IsNullOrEmpty(fullText))
        {
            yield break;
        }

        target.text = string.Empty;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipRequested)
            {
                target.text = fullText;
                yield break;
            }

            target.text += fullText[i];
            yield return new WaitForSeconds(typewriterCharInterval);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Completion
    // ──────────────────────────────────────────────────────────────────

    private void CompleteAndStartBattle()
    {
        ShowCanvas(false);

        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm != null)
        {
            bm.StartBattle();
        }
        else
        {
            Debug.LogWarning("[BossIntroDirector] BattleManager not found after intro. Cannot start battle.");
        }

        // Self-destruct after a short delay to allow cleanup
        Destroy(gameObject, 0.5f);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Boss Mechanic Resolution
    // ──────────────────────────────────────────────────────────────────

    private BossNarrativeMechanic ResolveBossMechanic()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return null;
        }

        string islandId = IslandThemeRegistry.ResolveIslandId(gsm.PendingCombatIslandId);
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        pendingIslandId = islandId;
        return BossNarrativeMechanic.GetDefaultForIsland(islandId);
    }

    // ──────────────────────────────────────────────────────────────────
    //  UI Construction
    // ──────────────────────────────────────────────────────────────────

    private void EnsureIntroUI()
    {
        if (introCanvas != null && dialogueText != null && atmosphereOverlay != null)
        {
            return;
        }

        // Create canvas
        if (introCanvas == null)
        {
            GameObject canvasGo = new GameObject("BossIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            introCanvas = canvasGo.GetComponent<Canvas>();
            introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            introCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Atmosphere overlay (full-screen tint)
        if (atmosphereOverlay == null)
        {
            GameObject overlayGo = CreatePanel(introCanvas.transform, "AtmosphereOverlay",
                Vector2.zero, Vector2.one, Color.black);
            atmosphereOverlay = overlayGo.GetComponent<Image>();
            atmosphereOverlay.raycastTarget = true; // Blocks clicks -> skip
            atmosphereOverlay.gameObject.SetActive(false);
        }

        // Boss name (top center)
        if (bossNameText == null)
        {
            GameObject nameGo = CreateTextChild(introCanvas.transform, "BossName",
                new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.88f), 42,
                TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));
            bossNameText = nameGo.GetComponent<Text>();
        }

        // Location description (below name)
        if (locationText == null)
        {
            GameObject locGo = CreateTextChild(introCanvas.transform, "LocationText",
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.73f), 24,
                TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
            locationText = locGo.GetComponent<Text>();
        }

        // Dialogue text (center area)
        if (dialogueText == null)
        {
            GameObject textGo = CreateTextChild(introCanvas.transform, "IntroDialogueText",
                new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.58f), 28,
                TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.75f));
            dialogueText = textGo.GetComponent<Text>();
        }

        // Skip prompt (bottom center)
        if (skipPromptText == null)
        {
            GameObject skipGo = CreateTextChild(introCanvas.transform, "SkipPrompt",
                new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.08f), 20,
                TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f));
            skipPromptText = skipGo.GetComponent<Text>();
            skipPromptText.text = skipButtonLabel;
            skipPromptText.gameObject.SetActive(false);
        }
    }

    private void ShowCanvas(bool visible)
    {
        if (introCanvas != null)
        {
            introCanvas.gameObject.SetActive(visible);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ──────────────────────────────────────────────────────────────────

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(parent, false);

        RectTransform rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panelGo.GetComponent<Image>();
        img.color = bgColor;

        return panelGo;
    }

    private static GameObject CreateTextChild(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize,
        TextAnchor alignment, Color color)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(parent, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;

        return textGo;
    }
}
