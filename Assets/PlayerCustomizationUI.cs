using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerCustomizationUI : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(560f, 460f);

    [Header("Theme")]
    [SerializeField] private Color panelColor = new Color(0.08f, 0.1f, 0.14f, 0.96f);
    [SerializeField] private Color headerColor = new Color(0.95f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color textColor = new Color(0.88f, 0.9f, 0.95f, 1f);
    [SerializeField] private Color subTextColor = new Color(0.65f, 0.72f, 0.82f, 1f);
    [SerializeField] private Color freeTagColor = new Color(0.3f, 0.86f, 0.52f, 1f);
    [SerializeField] private Color premiumTagColor = new Color(0.95f, 0.68f, 0.28f, 1f);

    private Canvas menuCanvas;
    private GameObject panelRoot;
    private bool isOpen;
    private IsometricPlayer player;
    private Text currencyLabel;

    private struct ColorPreset
    {
        public string Id;
        public string Name;
        public Color Color;
        public int Cost;
        public bool IsPremium;

        public ColorPreset(string id, string name, Color color, int cost, bool isPremium)
        {
            Id = id;
            Name = name;
            Color = color;
            Cost = Mathf.Max(0, cost);
            IsPremium = isPremium;
        }
    }

    private static readonly ColorPreset[] Presets =
    {
        new ColorPreset("forest", "Forest", new Color(0.2f, 0.8f, 0.2f), 0, false),
        new ColorPreset("ocean", "Ocean", new Color(0.2f, 0.64f, 1f), 0, false),
        new ColorPreset("ember", "Ember", new Color(1f, 0.4f, 0.24f), 0, false),
        new ColorPreset("sunlight", "Sunlight", new Color(1f, 0.82f, 0.3f), 0, false),
        new ColorPreset("void", "Void", new Color(0.35f, 0.3f, 0.52f), 80, true),
        new ColorPreset("frost", "Frost", new Color(0.62f, 0.92f, 1f), 120, true),
        new ColorPreset("crimson", "Crimson", new Color(0.82f, 0.18f, 0.26f), 150, true),
        new ColorPreset("gold", "Gold", new Color(1f, 0.9f, 0.25f), 220, true)
    };

    private void OnEnable()
    {
        if (HeroProgressionManager.Instance != null)
        {
            HeroProgressionManager.Instance.OnCosmeticXpChanged += HandleCosmeticXpChanged;
            HeroProgressionManager.Instance.OnPlayerColorPresetUnlocked += HandlePresetUnlocked;
        }
    }

    private void OnDisable()
    {
        if (HeroProgressionManager.Instance != null)
        {
            HeroProgressionManager.Instance.OnCosmeticXpChanged -= HandleCosmeticXpChanged;
            HeroProgressionManager.Instance.OnPlayerColorPresetUnlocked -= HandlePresetUnlocked;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        if (!CanToggle())
        {
            return;
        }

        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    private bool CanToggle()
    {
        if (GameStateManager.Instance == null)
        {
            return true;
        }

        return GameStateManager.Instance.currentState == GameStateManager.GameState.Exploration
            && !GameStateManager.Instance.IsTransitioning;
    }

    private void OpenMenu()
    {
        if (isOpen)
        {
            return;
        }

        player = player != null ? player : FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerCustomizationUI] IsometricPlayer not found.");
            return;
        }

        isOpen = true;
        EnsureCanvas();
        BuildPanel();
    }

    private void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        if (menuCanvas != null)
        {
            Destroy(menuCanvas.gameObject);
            menuCanvas = null;
        }
    }

    private void EnsureCanvas()
    {
        if (menuCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("PlayerCustomizationCanvas");
        canvasObject.transform.SetParent(transform, false);

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 940;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void BuildPanel()
    {
        if (menuCanvas == null)
        {
            return;
        }

        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        panelRoot = new GameObject("CustomizationPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = panelColor;

        CreateLabel(panelRoot.transform, "CHARACTER CUSTOMIZATION", new Vector2(24f, -24f), new Vector2(panelSize.x - 48f, 30f), headerColor, 24, FontStyle.Bold);
        currencyLabel = CreateLabel(panelRoot.transform, string.Empty, new Vector2(24f, -58f), new Vector2(panelSize.x - 48f, 22f), textColor, 16, FontStyle.Bold);
        CreateLabel(panelRoot.transform, "Free and premium color styles. Premium styles cost Cosmetic XP.", new Vector2(24f, -82f), new Vector2(panelSize.x - 48f, 20f), subTextColor, 12, FontStyle.Normal);

        RefreshCurrencyLabel();

        float startY = 116f;
        float cardHeight = 74f;
        float rowGap = 10f;
        float cardGap = 12f;
        float sidePadding = 20f;
        float cardWidth = (panelSize.x - sidePadding * 2f - cardGap) * 0.5f;

        for (int i = 0; i < Presets.Length; i++)
        {
            int row = i / 2;
            int column = i % 2;
            float x = sidePadding + (cardWidth + cardGap) * column;
            float y = startY + (cardHeight + rowGap) * row;
            CreatePresetRow(panelRoot.transform, Presets[i], x, y, cardWidth, cardHeight);
        }

        CreateLabel(panelRoot.transform, $"[{toggleKey}] Close", new Vector2(24f, -(panelSize.y - 28f)), new Vector2(panelSize.x - 48f, 20f), subTextColor, 12, FontStyle.Italic);
    }

    private void CreatePresetRow(Transform parent, ColorPreset preset, float x, float y, float width, float height)
    {
        GameObject rowObject = new GameObject($"Preset_{preset.Id}", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = new Vector2(x, -y);
        rowRect.sizeDelta = new Vector2(width, height);

        Image rowBg = rowObject.GetComponent<Image>();
        rowBg.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);

        GameObject swatchObject = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatchObject.transform.SetParent(rowObject.transform, false);
        RectTransform swatchRect = swatchObject.GetComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0f, 0.5f);
        swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.sizeDelta = new Vector2(34f, 34f);
        swatchRect.anchoredPosition = new Vector2(10f, 0f);
        Image swatchImage = swatchObject.GetComponent<Image>();
        swatchImage.color = preset.Color;

        float textWidth = Mathf.Max(80f, width - 152f);
        CreateLabel(rowObject.transform, preset.Name, new Vector2(52f, -8f), new Vector2(textWidth, 22f), textColor, 14, FontStyle.Bold);

        string tag = preset.IsPremium ? $"PREMIUM {preset.Cost} XP" : "FREE";
        Color tagColor = preset.IsPremium ? premiumTagColor : freeTagColor;
        CreateLabel(rowObject.transform, tag, new Vector2(52f, -30f), new Vector2(textWidth + 20f, 20f), tagColor, 11, FontStyle.Bold);

        string buttonText = preset.IsPremium && !IsPresetUnlocked(preset) ? "Unlock" : "Apply";
        Button applyButton = CreateButton(rowObject.transform, buttonText, new Vector2(0.68f, 0.15f), new Vector2(0.96f, 0.85f));
        applyButton.onClick.AddListener(() => OnPresetSelected(preset));

        Text buttonLabel = applyButton.GetComponentInChildren<Text>();
        if (buttonLabel != null)
        {
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.fontSize = 12;
        }
    }

    private void OnPresetSelected(ColorPreset preset)
    {
        if (player == null)
        {
            player = FindFirstObjectByType<IsometricPlayer>();
        }

        if (player == null)
        {
            return;
        }

        bool unlocked = IsPresetUnlocked(preset);
        if (!unlocked)
        {
            HeroProgressionManager manager = HeroProgressionManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[PlayerCustomizationUI] HeroProgressionManager not found.");
                return;
            }

            if (!manager.TryUnlockPlayerColorPreset(preset.Id, preset.Cost))
            {
                Debug.Log("[PlayerCustomizationUI] Not enough Cosmetic XP for this style.");
                return;
            }

            unlocked = true;
        }

        if (!unlocked)
        {
            return;
        }

        player.SetPlayerColor(preset.Color);
        BuildPanel();
    }

    private static bool IsPresetUnlocked(ColorPreset preset)
    {
        if (!preset.IsPremium)
        {
            return true;
        }

        if (HeroProgressionManager.Instance == null)
        {
            return false;
        }

        return HeroProgressionManager.Instance.IsPlayerColorPresetUnlocked(preset.Id);
    }

    private void HandlePresetUnlocked(string _)
    {
        if (isOpen)
        {
            BuildPanel();
        }
    }

    private void HandleCosmeticXpChanged(int _)
    {
        RefreshCurrencyLabel();
    }

    private void RefreshCurrencyLabel()
    {
        if (currencyLabel == null)
        {
            return;
        }

        int xp = HeroProgressionManager.Instance != null
            ? HeroProgressionManager.Instance.GetCosmeticXp()
            : 0;
        currencyLabel.text = $"Cosmetic XP: {xp}";
    }

    private static Text CreateLabel(Transform parent, string text, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize, FontStyle fontStyle)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        Text label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.24f, 0.3f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.22f, 0.24f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.36f, 0.45f, 1f);
        colors.pressedColor = new Color(0.16f, 0.18f, 0.24f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        label.raycastTarget = false;

        return button;
    }
}
