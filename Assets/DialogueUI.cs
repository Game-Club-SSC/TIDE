using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the visual presentation of dialogue sequences: speaker name,
/// portrait placeholder (colored circle), typewriter text, and advance-on-click.
/// Created automatically by <see cref="DialogueSystem"/> when needed.
/// </summary>
[DisallowMultipleComponent]
public class DialogueUI : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Constants
    // ------------------------------------------------------------------ //

    private const string CanvasName = "DialogueCanvas";
    private const float TypewriterBaseSpeed = 0.025f; // seconds per character
    private const float FadeInDuration = 0.15f;
    private const float PanelHeightRatio = 0.28f; // fraction of screen height

    // ------------------------------------------------------------------ //
    //  Runtime state
    // ------------------------------------------------------------------ //

    private Canvas canvas;
    private CanvasGroup panelGroup;
    private Image portraitImage;
    private Text speakerText;
    private Text bodyText;
    private Text continuePrompt;

    private List<DialogueSystem.DialogueEntry> currentEntries;
    private int currentIndex;
    private bool isTyping;
    private bool skipRequested;
    private Action onCompleteCallback;
    private Coroutine typewriterRoutine;

    // ------------------------------------------------------------------ //
    //  Public
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Play a full dialogue sequence. When done, invoke <paramref name="onComplete"/>.
    /// </summary>
    public void PlaySequence(List<DialogueSystem.DialogueEntry> entries, Action onComplete)
    {
        if (entries == null || entries.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentEntries = entries;
        currentIndex = 0;
        onCompleteCallback = onComplete;

        EnsureCanvas();
        ShowPanel();
        ShowEntry(currentEntries[0]);
    }

    // ------------------------------------------------------------------ //
    //  Update (advance / skip)
    // ------------------------------------------------------------------ //

    private void Update()
    {
        if (panelGroup == null || panelGroup.alpha < 0.5f)
        {
            return;
        }

        if (currentEntries == null || currentEntries.Count == 0)
        {
            return;
        }

        bool pressedAdvance = Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space);

        if (!pressedAdvance)
        {
            return;
        }

        if (isTyping)
        {
            // Skip to full text
            skipRequested = true;
            return;
        }

        AdvanceEntry();
    }

    // ------------------------------------------------------------------ //
    //  Entry display
    // ------------------------------------------------------------------ //

    private void ShowEntry(DialogueSystem.DialogueEntry entry)
    {
        if (speakerText != null)
        {
            speakerText.text = entry.speakerName ?? "???";
        }

        Color emotionColor = DialogueSystem.GetEmotionColor(entry.emotion);
        if (speakerText != null)
        {
            speakerText.color = emotionColor;
        }

        if (portraitImage != null)
        {
            portraitImage.color = DialogueSystem.GetEmotionPortraitColor(entry.emotion);
        }

        if (continuePrompt != null)
        {
            continuePrompt.gameObject.SetActive(false);
        }

        // Start typewriter
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        skipRequested = false;
        typewriterRoutine = StartCoroutine(TypewriterRoutine(entry.dialogueText));
    }

    private void AdvanceEntry()
    {
        currentIndex++;

        if (currentIndex >= currentEntries.Count)
        {
            HidePanel();
            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
            currentEntries = null;
            return;
        }

        ShowEntry(currentEntries[currentIndex]);
    }

    // ------------------------------------------------------------------ //
    //  Typewriter effect
    // ------------------------------------------------------------------ //

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            OnTypewriterComplete();
            yield break;
        }

        for (int i = 0; i <= fullText.Length; i++)
        {
            if (skipRequested)
            {
                if (bodyText != null)
                {
                    bodyText.text = fullText;
                }

                break;
            }

            if (bodyText != null)
            {
                bodyText.text = fullText.Substring(0, i);
            }

            yield return new WaitForSecondsRealtime(TypewriterBaseSpeed);
        }

        isTyping = false;
        OnTypewriterComplete();
    }

    private void OnTypewriterComplete()
    {
        if (continuePrompt != null)
        {
            continuePrompt.gameObject.SetActive(true);
        }

        typewriterRoutine = null;
    }

    // ------------------------------------------------------------------ //
    //  Canvas construction (procedural, no prefab required)
    // ------------------------------------------------------------------ //

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        // --- Canvas ---
        GameObject canvasObj = new GameObject(CanvasName);
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Panel background (Persona-style dark angular panel) ---
        GameObject panelObj = CreateUIElement("DialoguePanel", canvasObj.transform);
        panelGroup = panelObj.AddComponent<CanvasGroup>();
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, PanelHeightRatio);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = PersonaUIStyle.DialoguePanel;

        // Persona-style diagonal edge on the dialogue panel
        PersonaUIStyle.AddDiagonalEdge(panelRect, 10f);

        // Accent slash along the left edge
        PersonaUIStyle.CreateAccentSlash(panelObj.transform, PersonaUIStyle.BrightBlue, 3f);

        // --- Portrait circle (with subtle blue tint) ---
        GameObject portraitObj = CreateUIElement("Portrait", panelObj.transform);
        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.type = Image.Type.Filled;
        portraitImage.fillMethod = Image.FillMethod.Radial360;
        portraitImage.fillClockwise = true;
        portraitImage.color = PersonaUIStyle.PortraitTint;

        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(70f, 0f);
        portraitRect.sizeDelta = new Vector2(80f, 80f);

        // Blue ring around the portrait
        GameObject ringObj = CreateUIElement("PortraitRing", panelObj.transform);
        Image ringImg = ringObj.AddComponent<Image>();
        ringImg.type = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillClockwise = true;
        ringImg.fillAmount = 1f;
        ringImg.color = PersonaUIStyle.BrightBlue;
        RectTransform ringRect = ringObj.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0f, 0.5f);
        ringRect.anchorMax = new Vector2(0f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = new Vector2(70f, 0f);
        ringRect.sizeDelta = new Vector2(86f, 86f);
        ringImg.raycastTarget = false;
        // Move ring behind the portrait
        ringObj.transform.SetAsFirstSibling();

        // --- Speaker name ---
        GameObject speakerObj = CreateUIElement("SpeakerName", panelObj.transform);
        speakerText = speakerObj.AddComponent<Text>();
        speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (speakerText.font == null)
        {
            speakerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        speakerText.fontSize = 26;
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.color = PersonaUIStyle.BrightBlue;

        RectTransform speakerRect = speakerObj.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.anchoredPosition = new Vector2(120f, -10f);
        speakerRect.sizeDelta = new Vector2(-140f, 36f);

        // --- Body text ---
        GameObject bodyObj = CreateUIElement("BodyText", panelObj.transform);
        bodyText = bodyObj.AddComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (bodyText.font == null)
        {
            bodyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        bodyText.fontSize = 22;
        bodyText.color = Color.white;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = 1.15f;

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(120f, 12f);
        bodyRect.offsetMax = new Vector2(-30f, -42f);

        // --- Continue prompt ---
        GameObject promptObj = CreateUIElement("ContinuePrompt", panelObj.transform);
        continuePrompt = promptObj.AddComponent<Text>();
        continuePrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (continuePrompt.font == null)
        {
            continuePrompt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        continuePrompt.fontSize = 16;
        continuePrompt.fontStyle = FontStyle.Italic;
        continuePrompt.color = new Color(PersonaUIStyle.DimText.r, PersonaUIStyle.DimText.g, PersonaUIStyle.DimText.b, 0.7f);
        continuePrompt.text = "[Press Enter to continue]";
        continuePrompt.alignment = TextAnchor.LowerRight;

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.offsetMin = new Vector2(120f, 4f);
        promptRect.offsetMax = new Vector2(-20f, -4f);

        // Start hidden
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = false;
    }

    private void ShowPanel()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = true;
        }
    }

    private void HidePanel()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
        }
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

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
}
