using UnityEngine;
using UnityEngine.UI;

public class PuzzleHud : MonoBehaviour
{
    private const string CanvasName = "PuzzleHudCanvas";
    private const int CanvasSortingOrder = 760;

    [Header("Display")]
    [SerializeField] private int fontSize = 20;
    [SerializeField] private Color carriedColor = new Color(1f, 0.9f, 0.4f);
    [SerializeField] private Color hintColor = new Color(0.7f, 0.7f, 0.7f);

    private Text carriedLabel;
    private Text hintLabel;
    private Image statusPanel;
    private Button resetButton;
    private TideManager tideManager;

    private void Awake()
    {
        EnsureCanvas();
    }

    private void OnEnable()
    {
        TryFindTideManager();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromTideManager();
    }

    private void Update()
    {
        if (tideManager == null)
        {
            TryFindTideManager();
        }

        if (tideManager != null && tideManager.IsOverlayMode && Input.GetKeyDown(KeyCode.Escape))
        {
            tideManager.RequestOverlayClose();
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            OnResetPressed();
        }
    }

    private void TryFindTideManager()
    {
        if (tideManager != null)
        {
            return;
        }

        tideManager = FindFirstObjectByType<TideManager>();
        if (tideManager != null)
        {
            tideManager.OnCarriedAmountChanged += RefreshDisplay;
            tideManager.OnPuzzleReset += RefreshDisplay;
        }
    }

    private void UnsubscribeFromTideManager()
    {
        if (tideManager != null)
        {
            tideManager.OnCarriedAmountChanged -= RefreshDisplay;
            tideManager.OnPuzzleReset -= RefreshDisplay;
        }
    }

    private void OnResetPressed()
    {
        if (tideManager != null)
        {
            tideManager.ResetPuzzle();
        }
    }

    private void RefreshDisplay()
    {
        if (carriedLabel == null || tideManager == null)
        {
            return;
        }

        if (tideManager.IsCarrying)
        {
            carriedLabel.text = $"Carrying: {tideManager.CarriedAmount}";
            carriedLabel.color = carriedColor;
            hintLabel.text = tideManager.IsOverlayMode
                ? "Click a tile to place Tide | Esc to exit"
                : "Click a tile to place Tide";
        }
        else
        {
            carriedLabel.text = "Carrying: -";
            carriedLabel.color = Color.white;
            hintLabel.text = tideManager.IsOverlayMode
                ? "Click a tile to pick up Tide | Esc to exit"
                : "Click a tile to pick up Tide";
        }

        if (tideManager.UsesUiBoard)
        {
            hintLabel.text += " | T to reset";
        }
    }

    private void EnsureCanvas()
    {
        Transform existingCanvas = transform.Find(CanvasName);
        if (existingCanvas != null)
        {
            CacheComponents(existingCanvas);
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        Transform statusPanelTransform = CreateStatusPanel(canvasObject.transform);
        CreateCarriedLabel(statusPanelTransform);
        CreateHintLabel(statusPanelTransform);
        CreateResetButton(canvasObject.transform);
    }

    private Transform CreateStatusPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(520f, 104f);

        statusPanel = panelObject.GetComponent<Image>();
        statusPanel.color = new Color(0.05f, 0.07f, 0.11f, 0.88f);
        statusPanel.raycastTarget = false;

        return panelObject.transform;
    }

    private void CreateCarriedLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("CarriedLabel", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        carriedLabel = labelObject.AddComponent<Text>();
        RectTransform rect = carriedLabel.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -12f);
        rect.sizeDelta = new Vector2(484f, 38f);

        carriedLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        carriedLabel.fontSize = Mathf.Max(fontSize, 24);
        carriedLabel.fontStyle = FontStyle.Bold;
        carriedLabel.alignment = TextAnchor.UpperLeft;
        carriedLabel.color = Color.white;
        carriedLabel.raycastTarget = false;
    }

    private void CreateHintLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("HintLabel", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        hintLabel = labelObject.AddComponent<Text>();
        RectTransform rect = hintLabel.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -54f);
        rect.sizeDelta = new Vector2(484f, 42f);

        hintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintLabel.fontSize = 18;
        hintLabel.alignment = TextAnchor.UpperLeft;
        hintLabel.color = hintColor;
        hintLabel.raycastTarget = false;
    }

    private void CreateResetButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ResetButton", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-24f, 24f);
        buttonRect.sizeDelta = new Vector2(120f, 36f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        resetButton = buttonObject.AddComponent<Button>();
        resetButton.targetGraphic = buttonImage;
        resetButton.onClick.AddListener(OnResetPressed);

        GameObject textObject = new GameObject("ResetText", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text resetText = textObject.AddComponent<Text>();
        resetText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resetText.fontSize = 16;
        resetText.alignment = TextAnchor.MiddleCenter;
        resetText.color = Color.white;
        resetText.text = "Reset [T]";
        resetText.raycastTarget = false;
    }

    private void CacheComponents(Transform canvasTransform)
    {
        statusPanel = canvasTransform.Find("StatusPanel")?.GetComponent<Image>();
        carriedLabel = canvasTransform.Find("StatusPanel/CarriedLabel")?.GetComponent<Text>()
            ?? canvasTransform.Find("CarriedLabel")?.GetComponent<Text>();
        hintLabel = canvasTransform.Find("StatusPanel/HintLabel")?.GetComponent<Text>()
            ?? canvasTransform.Find("HintLabel")?.GetComponent<Text>();
        resetButton = canvasTransform.Find("ResetButton")?.GetComponent<Button>();

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetPressed);
            resetButton.onClick.AddListener(OnResetPressed);
        }
    }
}
