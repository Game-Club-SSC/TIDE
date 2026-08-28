using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick Time Event session for neutral element clashes.
/// Shows a shrinking circular indicator that the player must time correctly.
/// Press Space, click, or tap when the indicator enters the sweet spot (inner 30%).
/// </summary>
[DisallowMultipleComponent]
public class QTESession : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] private float defaultTimeWindow = 2.0f;
    [SerializeField] [Range(0.1f, 0.5f)] private float sweetSpotRatio = 0.3f;

    [Header("Indicator Colors")]
    [SerializeField] private Color indicatorColor = new Color(0.2f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color sweetSpotColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
    [SerializeField] private Color failColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

    private Image backgroundRing;
    private Image fillRing;
    private Text promptText;
    private Coroutine activeQteCoroutine;

    private bool isActive;
    private bool lastResult;
    private bool isComplete;

    public bool IsQTEActive => isActive;
    public bool LastResult => lastResult;

    /// <summary>
    /// Fired when the QTE completes. Argument is true on success, false on fail.
    /// </summary>
    public event Action<bool> OnQTEResolved;

    /// <summary>
    /// Starts the QTE countdown. The result is delivered asynchronously via
    /// the <see cref="OnQTEResolved"/> event and stored in <see cref="LastResult"/>
    /// once the QTE completes. Returns false immediately; returns false without
    /// starting if a QTE is already active.
    /// </summary>
    /// <param name="timeWindow">Total duration in seconds. Uses default if non-positive.</param>
    public bool ShowQTE(float timeWindow)
    {
        if (isActive)
        {
            Debug.LogWarning("[QTESession] QTE already active. Ignoring request.");
            return false;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[QTESession] Component is not active and enabled.");
            return false;
        }

        if (timeWindow <= 0f)
        {
            timeWindow = defaultTimeWindow;
        }

        lastResult = false;
        isComplete = false;
        isActive = true;
        EnsureUI();
        ShowUI();
        activeQteCoroutine = StartCoroutine(RunQTE(timeWindow));
        return true;
    }

    /// <summary>
    /// Stops an active QTE and reports a failed result to its listeners.
    /// </summary>
    public void CancelQTE()
    {
        if (!isActive)
        {
            return;
        }

        if (activeQteCoroutine != null)
        {
            StopCoroutine(activeQteCoroutine);
            activeQteCoroutine = null;
        }

        CompleteQTE(false);
    }

    /// <summary>
    /// Coroutine-friendly version. Starts the QTE and yields until it completes,
    /// then invokes the callback with the result.
    /// Usage: yield return StartCoroutine(qteSession.ShowQTECoroutine(2f, result => { ... }));
    /// </summary>
    public IEnumerator ShowQTECoroutine(float timeWindow, Action<bool> onResult)
    {
        if (!ShowQTE(timeWindow))
        {
            onResult?.Invoke(false);
            yield break;
        }

        yield return new WaitUntil(() => isComplete);
        onResult?.Invoke(lastResult);
    }

    private IEnumerator RunQTE(float timeWindow)
    {
        float elapsed = 0f;
        float sweetSpotStart = 1f - sweetSpotRatio;
        bool inputReceived = false;
        bool success = false;

        while (elapsed < timeWindow)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / timeWindow);
            float fillAmount = 1f - progress;

            // Update fill ring: shrinks from full to empty
            if (fillRing != null)
            {
                fillRing.fillAmount = fillAmount;

                // Shift to sweet spot color when in the inner zone
                if (progress >= sweetSpotStart)
                {
                    fillRing.color = sweetSpotColor;
                }
                else
                {
                    fillRing.color = indicatorColor;
                }
            }

            // Update prompt text
            if (promptText != null)
            {
                promptText.text = progress >= sweetSpotStart ? "PRESS!" : "WAIT...";
            }

            // Check for player input only within the sweet spot window
            if (progress >= sweetSpotStart && !inputReceived)
            {
                if (Input.GetKeyDown(KeyCode.Space)
                    || Input.GetMouseButtonDown(0)
                    || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    inputReceived = true;
                    success = true;
                    Debug.Log("[QTESession] Player hit the sweet spot!");
                }
            }

            yield return null;
        }

        // Time expired without input = fail
        if (!inputReceived)
        {
            success = false;
            Debug.Log("[QTESession] QTE timed out. Player missed.");
        }

        // Flash the result color briefly
        if (fillRing != null)
        {
            fillRing.color = success ? sweetSpotColor : failColor;
            fillRing.fillAmount = success ? 1f : 0f;
        }

        if (promptText != null)
        {
            promptText.text = success ? "SUCCESS!" : "MISS!";
        }

        yield return new WaitForSecondsRealtime(0.4f);

        CompleteQTE(success);
    }

    private void CompleteQTE(bool success)
    {
        if (!isActive && isComplete)
        {
            return;
        }

        activeQteCoroutine = null;
        lastResult = success;
        isComplete = true;
        isActive = false;
        HideUI();
        OnQTEResolved?.Invoke(success);
    }

    private void OnDisable()
    {
        CancelQTE();
    }

    #region UI Setup

    private void EnsureUI()
    {
        if (backgroundRing != null)
        {
            return;
        }

        Transform uiParent = GetOrCreateUiParent();
        if (uiParent == null)
        {
            Debug.LogError("[QTESession] QTE UI could not create a canvas parent.", this);
            return;
        }

        // Ring container (centered, fixed size)
        GameObject container = new GameObject("QTERing", typeof(RectTransform));
        container.transform.SetParent(uiParent, false);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(180f, 180f);

        // Background ring (always full, dark)
        backgroundRing = CreateRadialImage(containerRect, "BG", backgroundColor);

        // Fill ring (the shrinking indicator)
        fillRing = CreateRadialImage(containerRect, "Fill", indicatorColor);

        // Prompt text in the center
        GameObject textObj = new GameObject("Prompt");
        textObj.transform.SetParent(containerRect, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        promptText = textObj.AddComponent<Text>();
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 20;
        promptText.fontStyle = FontStyle.Bold;
        promptText.color = Color.white;
        promptText.text = "";

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private Transform GetOrCreateUiParent()
    {
        if (canvasGroup != null && canvasGroup.GetComponentInParent<Canvas>() != null)
        {
            return canvasGroup.transform;
        }

        GameObject canvasObject = new GameObject("NeutralClashQTECanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        return canvasObject.transform;
    }

    private static Image CreateRadialImage(RectTransform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillAmount = 1f;
        image.fillClockwise = true;

        return image;
    }

    private void ShowUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (fillRing != null)
        {
            fillRing.fillAmount = 1f;
            fillRing.color = indicatorColor;
        }

        if (promptText != null)
        {
            promptText.text = "WAIT...";
        }
    }

    private void HideUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    #endregion
}
