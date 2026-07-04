using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Static utility class providing Persona 5 / Persona 3 Reload inspired UI
/// construction helpers. All procedural UI (panels, labels, buttons, dividers,
/// transitions) should go through this class to maintain a consistent aesthetic:
/// sharp angular shapes, high-contrast dark backgrounds, diagonal slash motifs,
/// and the game's signature deep-blue color scheme.
/// </summary>
public static class PersonaUIStyle
{
    // ======================================================================
    //  Color palette  (Persona 5 / 3 Reload inspired -- blue somber theme)
    // ======================================================================

    public static readonly Color DeepNavy   = new Color(0.05f, 0.07f, 0.15f, 1f);
    public static readonly Color MediumBlue = new Color(0.10f, 0.15f, 0.30f, 1f);
    public static readonly Color BrightBlue = new Color(0.20f, 0.40f, 0.80f, 1f);
    public static readonly Color AccentRed  = new Color(0.85f, 0.20f, 0.25f, 1f); // Persona 5 red
    public static readonly Color White      = Color.white;
    public static readonly Color OffWhite   = new Color(0.92f, 0.94f, 0.96f, 1f);
    public static readonly Color DimText    = new Color(0.55f, 0.60f, 0.70f, 1f);
    public static readonly Color Gold       = new Color(0.90f, 0.75f, 0.40f, 1f);

    // Derived convenience colors
    public static readonly Color PanelBg       = new Color(0.06f, 0.07f, 0.12f, 0.96f);
    public static readonly Color TitleBarBg    = new Color(0.08f, 0.09f, 0.14f, 1f);
    public static readonly Color TabActive     = BrightBlue;
    public static readonly Color TabInactive   = new Color(0.12f, 0.14f, 0.22f, 0.9f);
    public static readonly Color Backdrop      = new Color(0f, 0f, 0f, 0.65f);
    public static readonly Color SlashColor    = new Color(0.20f, 0.40f, 0.80f, 0.45f);
    public static readonly Color DialoguePanel = new Color(0.06f, 0.08f, 0.14f, 0.94f);
    public static readonly Color PortraitTint  = new Color(0.15f, 0.25f, 0.50f, 1f);
    public static readonly Color CloseBtnBg     = new Color(0.65f, 0.15f, 0.20f, 1f);

    // ======================================================================
    //  Panel construction
    // ======================================================================

    /// <summary>
    /// Creates a dark angular panel. When <paramref name="angleOffset"/> is
    /// non-zero the top-right corner is clipped via a child mask to produce
    /// the sharp diagonal edge characteristic of the Persona UI.
    /// </summary>
    public static Image CreateAngularPanel(Transform parent, Color bgColor, float angleOffset = 0f)
    {
        GameObject panelObj = CreateUIElement("AngularPanel", parent);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        StretchFull(panelRect);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = bgColor;
        panelImg.raycastTarget = true;

        if (!Mathf.Approximately(angleOffset, 0f))
        {
            AddDiagonalEdge(panelRect, angleOffset);
        }

        return panelImg;
    }

    // ======================================================================
    //  Slash / diagonal dividers
    // ======================================================================

    /// <summary>
    /// Creates a thin diagonal slash divider (the signature Persona 5 motif).
    /// The slash is a rotated thin Image stretched across the parent width.
    /// </summary>
    public static Image CreateSlashDivider(Transform parent, Color color)
    {
        GameObject slashObj = CreateUIElement("SlashDivider", parent);
        RectTransform slashRect = slashObj.GetComponent<RectTransform>();

        // Anchor across parent width, centered vertically
        slashRect.anchorMin = new Vector2(0f, 0.5f);
        slashRect.anchorMax = new Vector2(1f, 0.5f);
        slashRect.pivot = new Vector2(0.5f, 0.5f);
        slashRect.sizeDelta = new Vector2(0f, 2f);
        slashRect.anchoredPosition = Vector2.zero;

        // Rotate for the diagonal effect
        slashRect.localRotation = Quaternion.Euler(0f, 0f, -12f);

        Image slashImg = slashObj.AddComponent<Image>();
        slashImg.color = color;
        slashImg.raycastTarget = false;

        LayoutElement le = slashObj.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;

        return slashImg;
    }

    /// <summary>
    /// Creates a small accent slash overlay at the left edge of a panel,
    /// acting as a visual accent stripe (Persona 5 style).
    /// </summary>
    public static Image CreateAccentSlash(Transform parent, Color color, float width = 4f)
    {
        GameObject slashObj = CreateUIElement("AccentSlash", parent);
        RectTransform slashRect = slashObj.GetComponent<RectTransform>();

        slashRect.anchorMin = new Vector2(0f, 0f);
        slashRect.anchorMax = new Vector2(0f, 1f);
        slashRect.pivot = new Vector2(0f, 0.5f);
        slashRect.sizeDelta = new Vector2(width, 0f);
        slashRect.anchoredPosition = Vector2.zero;

        // Slight diagonal skew via rotation
        slashRect.localRotation = Quaternion.Euler(0f, 0f, 3f);

        Image slashImg = slashObj.AddComponent<Image>();
        slashImg.color = color;
        slashImg.raycastTarget = false;

        return slashImg;
    }

    // ======================================================================
    //  Labels / text
    // ======================================================================

    /// <summary>
    /// Creates a bold-styled text label using the Persona typography approach:
    /// large, high-contrast, clean.
    /// </summary>
    public static Text CreatePersonaLabel(
        Transform parent,
        string text,
        int fontSize,
        Color color,
        TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        GameObject obj = CreateUIElement("PersonaLabel", parent);
        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = anchor;
        txt.color = color;
        txt.text = text;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    // ======================================================================
    //  Tab buttons  (sharp-edged, Persona style)
    // ======================================================================

    /// <summary>
    /// Creates a sharp-edged tab button. Returns the Button component.
    /// The caller can read / set the Image color to toggle active state.
    /// </summary>
    public static Button CreatePersonaTabButton(
        Transform parent,
        string label,
        Color activeColor,
        Color inactiveColor)
    {
        GameObject obj = CreateUIElement("PersonaTab", parent);

        Image img = obj.AddComponent<Image>();
        img.color = inactiveColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = inactiveColor;
        cb.highlightedColor = MediumBlue;
        cb.pressedColor = DeepNavy;
        cb.selectedColor = activeColor;
        btn.colors = cb;

        // Label text
        Text txt = CreatePersonaLabel(obj.transform, label, 17, White, TextAnchor.MiddleCenter);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        StretchFull(txtRect);

        return btn;
    }

    // ======================================================================
    //  Animated slash transition
    // ======================================================================

    /// <summary>
    /// Performs a slash-style transition on a CanvasGroup. On enter the panel
    /// slides in from the left with a quick ease; on exit it slides out left.
    /// The caller should have a Coroutine host (MonoBehaviour) to run this on.
    /// This is a fire-and-forget coroutine starter.
    /// </summary>
    public static void ApplySlashTransition(CanvasGroup panel, bool entering, float duration = 0.3f)
    {
        if (panel == null) return;

        MonoBehaviour host = panel.GetComponentInParent<MonoBehaviour>();
        if (host == null) return;

        host.StartCoroutine(SlashTransitionRoutine(panel, entering, duration));
    }

    private static IEnumerator SlashTransitionRoutine(CanvasGroup panel, bool entering, float duration)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 originalAnchorMin = rt.anchorMin;
        Vector2 originalAnchorMax = rt.anchorMax;
        Vector2 originalOffsetMin = rt.offsetMin;
        Vector2 originalOffsetMax = rt.offsetMax;

        float elapsed = 0f;

        if (entering)
        {
            // Slide in from left: start off-screen left, end at original position
            panel.alpha = 1f;
            panel.blocksRaycasts = false;

            float slideWidth = Screen.width * 0.3f;
            rt.offsetMin = new Vector2(originalOffsetMin.x - slideWidth, originalOffsetMin.y);
            rt.offsetMax = new Vector2(originalOffsetMax.x - slideWidth, originalOffsetMax.y);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rt.offsetMin = new Vector2(
                    Mathf.Lerp(originalOffsetMin.x - slideWidth, originalOffsetMin.x, ease),
                    originalOffsetMin.y);
                rt.offsetMax = new Vector2(
                    Mathf.Lerp(originalOffsetMax.x - slideWidth, originalOffsetMax.x, ease),
                    originalOffsetMax.y);

                yield return null;
            }

            rt.offsetMin = originalOffsetMin;
            rt.offsetMax = originalOffsetMax;
            panel.blocksRaycasts = true;
        }
        else
        {
            // Slide out to left
            panel.blocksRaycasts = false;

            float slideWidth = Screen.width * 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease in cubic
                float ease = t * t * t;

                rt.offsetMin = new Vector2(
                    Mathf.Lerp(originalOffsetMin.x, originalOffsetMin.x - slideWidth, ease),
                    originalOffsetMin.y);
                rt.offsetMax = new Vector2(
                    Mathf.Lerp(originalOffsetMax.x, originalOffsetMax.x - slideWidth, ease),
                    originalOffsetMax.y);
                panel.alpha = 1f - t;

                yield return null;
            }

            panel.alpha = 0f;
            rt.offsetMin = originalOffsetMin;
            rt.offsetMax = originalOffsetMax;
        }
    }

    // ======================================================================
    //  Diagonal edge (skewed corner)
    // ======================================================================

    /// <summary>
    /// Adds a diagonal clipped edge to a RectTransform by applying a skew
    /// to the top-right corner via an overlay mask child. This gives the
    /// signature Persona 5 angular panel look.
    /// </summary>
    public static void AddDiagonalEdge(RectTransform rect, float angle = 15f)
    {
        if (rect == null) return;

        // Add a corner-clip overlay at the top-right
        GameObject clipObj = CreateUIElement("DiagonalClip", rect);
        RectTransform clipRect = clipObj.GetComponent<RectTransform>();

        clipRect.anchorMin = new Vector2(1f, 1f);
        clipRect.anchorMax = new Vector2(1f, 1f);
        clipRect.pivot = new Vector2(1f, 1f);

        float clipSize = Mathf.Abs(angle) * 4f;
        clipRect.sizeDelta = new Vector2(clipSize, clipSize);

        // Rotate the clip to create the diagonal cut
        clipRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image clipImg = clipObj.AddComponent<Image>();
        clipImg.color = Color.clear;
        clipImg.raycastTarget = false;

        // Add a thin accent line along the diagonal edge
        GameObject lineObj = CreateUIElement("DiagonalLine", rect);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        lineRect.anchorMin = new Vector2(1f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(1f, 1f);
        lineRect.sizeDelta = new Vector2(clipSize * 1.2f, 2f);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        lineRect.anchoredPosition = new Vector2(-1f, 1f);

        Image lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(BrightBlue.r, BrightBlue.g, BrightBlue.b, 0.35f);
        lineImg.raycastTarget = false;
    }

    // ======================================================================
    //  Canvas creation
    // ======================================================================

    /// <summary>
    /// Creates a ScreenSpaceOverlay canvas with an EventSystem (if none exists)
    /// and standard CanvasScaler. Returns the root GameObject.
    /// </summary>
    public static GameObject CreateOverlayCanvas(string name, int sortOrder)
    {
        GameObject canvasObj = new GameObject(name);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Ensure an EventSystem exists
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        return canvasObj;
    }

    // ======================================================================
    //  Tooltip
    // ======================================================================

    /// <summary>
    /// Creates a Persona-style tooltip with an angular background and
    /// high-contrast text. Returns the root GameObject.
    /// </summary>
    public static GameObject CreateTooltip(Transform parent, string text, Vector2 position)
    {
        GameObject tooltipObj = CreateUIElement("PersonaTooltip", parent);
        RectTransform tooltipRect = tooltipObj.GetComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0f, 0f);
        tooltipRect.anchorMax = new Vector2(0f, 0f);
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.anchoredPosition = position;

        // Background
        Image bg = tooltipObj.AddComponent<Image>();
        bg.color = DeepNavy;

        // Layout
        VerticalLayoutGroup vlg = tooltipObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Content size fitter
        ContentSizeFitter csf = tooltipObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Accent slash at top
        Image accentLine = CreateAccentSlash(tooltipObj.transform, BrightBlue, 3f);
        LayoutElement accentLe = accentLine.gameObject.AddComponent<LayoutElement>();
        accentLe.preferredHeight = 3f;
        accentLe.flexibleWidth = 1f;

        // Text label
        GameObject textObj = CreateUIElement("TooltipText", tooltipObj.transform);
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 15;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.UpperLeft;
        txt.color = OffWhite;
        txt.text = text;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement textLe = textObj.AddComponent<LayoutElement>();
        textLe.preferredWidth = 280f;

        // Set the overall panel size based on text
        float tooltipWidth = 280f + 28f; // text width + padding
        float tooltipHeight = txt.preferredHeight + 30f; // text height + padding + accent
        tooltipRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);

        return tooltipObj;
    }

    // ======================================================================
    //  Internal helpers
    // ======================================================================

    internal static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    internal static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
