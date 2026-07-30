using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a notification popup when a TideBreak is unlocked.
/// Listens to TideBreakProgressionManager.OnTideBreakUnlocked and displays
/// the ability name, element icon, description, and unlock level.
/// Queues multiple unlocks if they happen simultaneously.
/// </summary>
[DisallowMultipleComponent]
public class TideBreakUnlockUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Text abilityNameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text unlockLevelText;
    [SerializeField] private Image elementIcon;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Element Colors")]
    [SerializeField] private Color fireColor = new Color(1f, 0.35f, 0.1f);
    [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color earthColor = new Color(0.65f, 0.45f, 0.2f);
    [SerializeField] private Color airColor = new Color(0.7f, 0.95f, 1f);
    [SerializeField] private Color spaceColor = new Color(0.6f, 0.2f, 0.9f);
    [SerializeField] private Color defaultColor = Color.white;

    private readonly Queue<TideBreakData> unlockQueue = new Queue<TideBreakData>();
    private Coroutine displayCoroutine;
    private bool isSubscribed;
    private bool isDisplaying;

    private void OnEnable()
    {
        TrySubscribe();

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isSubscribed)
        {
            TrySubscribe();
        }
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (TideBreakProgressionManager.Instance != null)
        {
            TideBreakProgressionManager.Instance.OnTideBreakUnlocked += HandleTideBreakUnlocked;
            isSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (isSubscribed && TideBreakProgressionManager.Instance != null)
        {
            TideBreakProgressionManager.Instance.OnTideBreakUnlocked -= HandleTideBreakUnlocked;
            isSubscribed = false;
        }

        StopAllCoroutines();
        isDisplaying = false;

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.gameObject.SetActive(false);
        }

        unlockQueue.Clear();
    }

    private void HandleTideBreakUnlocked(string heroId, TideBreakData tideBreak)
    {
        if (tideBreak == null)
        {
            return;
        }

        unlockQueue.Enqueue(tideBreak);

        if (!isDisplaying)
        {
            displayCoroutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isDisplaying = true;

        while (unlockQueue.Count > 0)
        {
            TideBreakData next = unlockQueue.Dequeue();
            yield return StartCoroutine(DisplayPopup(next));
        }

        isDisplaying = false;
        displayCoroutine = null;
    }

    private IEnumerator DisplayPopup(TideBreakData tideBreak)
    {
        if (popupCanvasGroup == null)
        {
            CreateFallbackPopupCanvasGroup();
        }

        if (popupCanvasGroup == null)
        {
            Debug.LogWarning("[TideBreakUnlockUI] Cannot display popup: no CanvasGroup available.");
            yield break;
        }

        // Populate the popup content
        if (abilityNameText != null)
        {
            abilityNameText.text = tideBreak.abilityName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = tideBreak.description;
        }

        if (unlockLevelText != null)
        {
            string levelText = tideBreak.isHidden
                ? "Hidden Ability Revealed!"
                : $"Unlocked at Level {tideBreak.unlockLevel}";
            unlockLevelText.text = levelText;
        }

        if (elementIcon != null)
        {
            elementIcon.color = GetElementColor(tideBreak.element);
        }

        // Show and fade in
        popupCanvasGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 1f, 0f, fadeOutDuration));

        // Hide
        popupCanvasGroup.gameObject.SetActive(false);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case 1: return fireColor;
            case 2: return waterColor;
            case 3: return earthColor;
            case 4: return airColor;
            case 5: return spaceColor;
            default: return defaultColor;
        }
    }

    private void CreateFallbackPopupCanvasGroup()
    {
        GameObject canvasObject = new GameObject("TideBreakUnlockFallbackCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        popupCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        popupCanvasGroup.alpha = 0f;

        GameObject panelObject = new GameObject("FallbackUnlockPanel", typeof(RectTransform));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.35f);
        panelRect.anchorMax = new Vector2(0.75f, 0.65f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObject.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.15f, 0.95f);

        abilityNameText = CreateFallbackLabel(panelRect, 0.7f, 1f, 28, FontStyle.Bold);
        descriptionText = CreateFallbackLabel(panelRect, 0.35f, 0.65f, 18, FontStyle.Normal);
        unlockLevelText = CreateFallbackLabel(panelRect, 0.1f, 0.3f, 16, FontStyle.Italic);

        Debug.Log("[TideBreakUnlockUI] Created fallback popup canvas.");
    }

    private static Text CreateFallbackLabel(RectTransform parent, float anchorMinY, float anchorMaxY, int fontSize, FontStyle style)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, anchorMinY);
        rect.anchorMax = new Vector2(0.95f, anchorMaxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.raycastTarget = false;
        return label;
    }
}
