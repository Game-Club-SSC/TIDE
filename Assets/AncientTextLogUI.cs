using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AncientTextLogUI : MonoBehaviour
{
    public struct DialoguePage
    {
        public string Speaker;
        public string Text;

        public DialoguePage(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    private sealed class PendingEntry
    {
        public string TextId;
        public string Title;
        public string Body;
        public bool NewlyDiscovered;
    }

    private const string CanvasName = "AncientTextLogCanvas";
    private const string DefaultSpeakerName = "Narrator";
    private const int MaxSpeakerNameLength = 28;

    private readonly Queue<PendingEntry> queuedEntries = new Queue<PendingEntry>();

    private Canvas canvas;
    private GameObject panel;
    private Image speakerPillImage;
    private Image accentLineImage;
    private Text titleText;
    private Text speakerText;
    private Text bodyText;
    private Text footerText;
    private Text continueText;
    private bool isVisible;
    private bool movementLocked;
    private bool hasMovementLockSnapshot;
    private bool wasPlayerMoveEnabled;
    private IsometricPlayer cachedPlayer;
    private PendingEntry activeEntry;
    private DialoguePage[] activePages = Array.Empty<DialoguePage>();
    private int activePageIndex;
    private int visibleStartedFrame;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureCanvas();
        HidePanel();
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        if (Time.frameCount == visibleStartedFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FinishCurrentEntry(true);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0))
        {
            AdvancePage();
        }
    }

    public void ShowEntry(string textId, string title, string body, bool newlyDiscovered)
    {
        EnsureCanvas();

        PendingEntry entry = new PendingEntry
        {
            TextId = textId,
            Title = title,
            Body = body,
            NewlyDiscovered = newlyDiscovered
        };

        if (isVisible)
        {
            queuedEntries.Enqueue(entry);
            RefreshFooter();
            return;
        }

        DisplayEntry(entry);
    }

    public void ShowDiscoveredLog()
    {
        EnsureCanvas();
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        string[] discoveredIds = gsm.GetDiscoveredAncientTextIds();
        if (discoveredIds.Length == 0)
        {
            ShowEntry("log_empty", "Ancient Texts", "No readable texts have been found yet.", false);
            return;
        }

        StringBuilder listBody = new StringBuilder();
        for (int i = 0; i < discoveredIds.Length; i++)
        {
            string id = discoveredIds[i];
            if (gsm.TryGetAncientTextEntry(id, out string entryTitle, out _, out bool discovered) && discovered)
            {
                listBody.Append(entryTitle);
            }
            else
            {
                listBody.Append(id);
            }

            if (i < discoveredIds.Length - 1)
            {
                listBody.Append('\n');
            }
        }

        ShowEntry("log_index", "Ancient Text Archive", listBody.ToString(), false);
    }

    public static DialoguePage[] BuildDialoguePages(string fallbackSpeaker, string body)
    {
        string resolvedFallback = string.IsNullOrWhiteSpace(fallbackSpeaker)
            ? DefaultSpeakerName
            : fallbackSpeaker.Trim();

        if (string.IsNullOrWhiteSpace(body))
        {
            return new[] { new DialoguePage(resolvedFallback, "No readable inscription remains on this fragment.") };
        }

        string[] rawLines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<DialoguePage> pages = new List<DialoguePage>();
        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            pages.Add(ParseLine(resolvedFallback, line));
        }

        if (pages.Count == 0)
        {
            pages.Add(new DialoguePage(resolvedFallback, "No readable inscription remains on this fragment."));
        }

        return pages.ToArray();
    }

    private static DialoguePage ParseLine(string fallbackSpeaker, string line)
    {
        int colonIndex = line.IndexOf(':');
        if (colonIndex > 0 && colonIndex <= MaxSpeakerNameLength)
        {
            string speaker = line.Substring(0, colonIndex).Trim();
            string text = line.Substring(colonIndex + 1).Trim();
            if (IsValidSpeakerName(speaker) && !string.IsNullOrEmpty(text))
            {
                return new DialoguePage(speaker, text);
            }
        }

        return new DialoguePage(fallbackSpeaker, line);
    }

    private static bool IsValidSpeakerName(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        for (int i = 0; i < speaker.Length; i++)
        {
            char c = speaker[i];
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '\'' && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    private void DisplayEntry(PendingEntry entry)
    {
        activeEntry = entry;
        activePages = BuildDialoguePages(ResolveEntryTitle(entry), entry.Body);
        activePageIndex = 0;
        visibleStartedFrame = Time.frameCount;
        isVisible = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        LockPlayerMovement(true);
        ApplyCurrentPage();
    }

    private void ApplyCurrentPage()
    {
        if (activePages == null || activePages.Length == 0)
        {
            activePages = BuildDialoguePages(ResolveEntryTitle(activeEntry), string.Empty);
            activePageIndex = 0;
        }

        activePageIndex = Mathf.Clamp(activePageIndex, 0, activePages.Length - 1);
        DialoguePage page = activePages[activePageIndex];

        if (titleText != null)
        {
            titleText.text = ResolveEntryTitle(activeEntry);
        }

        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrEmpty(page.Speaker) ? DefaultSpeakerName : page.Speaker;
        }

        if (bodyText != null)
        {
            bodyText.text = page.Text;
        }

        Color speakerColor = ResolveSpeakerColor(page.Speaker);
        if (speakerPillImage != null)
        {
            speakerPillImage.color = speakerColor;
        }

        if (accentLineImage != null)
        {
            accentLineImage.color = speakerColor;
        }

        if (continueText != null)
        {
            continueText.text = activePageIndex >= activePages.Length - 1 ? "Close" : "Next";
        }

        RefreshFooter();
    }

    private void RefreshFooter()
    {
        if (footerText == null)
        {
            return;
        }

        int pageCount = activePages == null ? 0 : activePages.Length;
        int displayIndex = pageCount == 0 ? 0 : activePageIndex + 1;
        string status = activeEntry != null && activeEntry.NewlyDiscovered ? "New entry" : "Archive";
        string action = activePages != null && activePageIndex >= activePages.Length - 1
            ? "Click / Enter to close"
            : "Click / Enter for next";
        string queued = queuedEntries.Count > 0 ? $"  Queued: {queuedEntries.Count}" : string.Empty;
        footerText.text = $"{status}  {displayIndex}/{Mathf.Max(1, pageCount)}  {action}  Esc closes{queued}";
    }

    private void AdvancePage()
    {
        if (activePages == null || activePages.Length == 0)
        {
            FinishCurrentEntry(false);
            return;
        }

        if (activePageIndex < activePages.Length - 1)
        {
            activePageIndex++;
            ApplyCurrentPage();
            return;
        }

        FinishCurrentEntry(false);
    }

    private void FinishCurrentEntry(bool clearQueue)
    {
        if (clearQueue)
        {
            queuedEntries.Clear();
        }

        if (!clearQueue && queuedEntries.Count > 0)
        {
            DisplayEntry(queuedEntries.Dequeue());
            return;
        }

        HidePanel();
        LockPlayerMovement(false);
    }

    private void HidePanel()
    {
        isVisible = false;
        activeEntry = null;
        activePages = Array.Empty<DialoguePage>();
        activePageIndex = 0;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void LockPlayerMovement(bool locked)
    {
        if (locked == movementLocked)
        {
            return;
        }

        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<IsometricPlayer>();
        }

        if (cachedPlayer == null)
        {
            movementLocked = locked;
            return;
        }

        if (locked)
        {
            wasPlayerMoveEnabled = cachedPlayer.canMove;
            hasMovementLockSnapshot = true;
            cachedPlayer.canMove = false;
            movementLocked = true;
            return;
        }

        if (!hasMovementLockSnapshot)
        {
            movementLocked = false;
            return;
        }

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.currentState == GameStateManager.GameState.Exploration
            && !GameStateManager.Instance.IsTransitioning)
        {
            cachedPlayer.canMove = wasPlayerMoveEnabled;
        }

        hasMovementLockSnapshot = false;
        movementLocked = false;
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName);
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        panel = CreatePanel(canvasObject.transform);
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("DialogueRail", typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.045f);
        panelRect.anchorMax = new Vector2(0.94f, 0.315f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.045f, 0.06f, 0.94f);
        panelImage.raycastTarget = true;

        accentLineImage = CreateBlock(panelRect, "SpeakerAccent", new Color(0.82f, 0.44f, 0.28f, 1f));
        RectTransform accentRect = accentLineImage.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0.96f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;

        speakerPillImage = CreateBlock(panelRect, "SpeakerPill", new Color(0.82f, 0.44f, 0.28f, 1f));
        RectTransform speakerPillRect = speakerPillImage.rectTransform;
        speakerPillRect.anchorMin = new Vector2(0.045f, 0.78f);
        speakerPillRect.anchorMax = new Vector2(0.25f, 0.94f);
        speakerPillRect.offsetMin = Vector2.zero;
        speakerPillRect.offsetMax = Vector2.zero;

        speakerText = CreateLabel(speakerPillRect, "Speaker", 25, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        speakerText.resizeTextForBestFit = true;
        speakerText.resizeTextMinSize = 16;
        speakerText.resizeTextMaxSize = 25;

        titleText = CreateLabel(panelRect, "Title", 20, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.8f, 0.86f, 0.92f, 0.92f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.3f, 0.78f);
        titleRect.anchorMax = new Vector2(0.94f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        bodyText = CreateLabel(panelRect, "Line", 32, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.94f, 0.96f, 0.98f, 1f));
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.06f, 0.28f);
        bodyRect.anchorMax = new Vector2(0.88f, 0.76f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        bodyText.resizeTextForBestFit = true;
        bodyText.resizeTextMinSize = 22;
        bodyText.resizeTextMaxSize = 32;

        footerText = CreateLabel(panelRect, "Footer", 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.68f, 0.76f, 0.84f, 1f));
        RectTransform footerRect = footerText.rectTransform;
        footerRect.anchorMin = new Vector2(0.06f, 0.07f);
        footerRect.anchorMax = new Vector2(0.74f, 0.22f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = Vector2.zero;

        continueText = CreateLabel(panelRect, "Continue", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.9f, 0.72f, 1f));
        RectTransform continueRect = continueText.rectTransform;
        continueRect.anchorMin = new Vector2(0.78f, 0.07f);
        continueRect.anchorMax = new Vector2(0.94f, 0.22f);
        continueRect.offsetMin = Vector2.zero;
        continueRect.offsetMax = Vector2.zero;
        continueText.resizeTextForBestFit = true;
        continueText.resizeTextMinSize = 16;
        continueText.resizeTextMaxSize = 22;

        return panelObject;
    }

    private static Image CreateBlock(Transform parent, string name, Color color)
    {
        GameObject blockObject = new GameObject(name, typeof(RectTransform));
        blockObject.transform.SetParent(parent, false);

        Image image = blockObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateLabel(Transform parent, string name, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static string ResolveEntryTitle(PendingEntry entry)
    {
        if (entry == null)
        {
            return "Story";
        }

        if (!string.IsNullOrEmpty(entry.Title))
        {
            return entry.Title;
        }

        return string.IsNullOrEmpty(entry.TextId) ? "Story" : entry.TextId;
    }

    private static Color ResolveSpeakerColor(string speaker)
    {
        switch (speaker)
        {
            case "Fire":
                return new Color(0.86f, 0.28f, 0.18f, 1f);
            case "Water":
                return new Color(0.14f, 0.5f, 0.82f, 1f);
            case "Earth":
                return new Color(0.45f, 0.56f, 0.26f, 1f);
            case "Air":
                return new Color(0.65f, 0.73f, 0.82f, 1f);
            case "Space":
                return new Color(0.45f, 0.36f, 0.74f, 1f);
            default:
                return new Color(0.78f, 0.55f, 0.28f, 1f);
        }
    }
}
