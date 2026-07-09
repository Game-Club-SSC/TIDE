using System.Collections.Generic;
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
    private RectTransform panelRect;
    private bool isOpen;
    private IsometricPlayer player;
    private Text currencyLabel;
    private bool wasPlayerMoveEnabled = true;
    private bool hasPausedTimeScale;
    private float previousTimeScale = 1f;
    private HeroProgressionManager subscribedProgressionManager;
    private FuturisticSpriteLibrary.PlayerStyleDefinition[] stylePresets =
        System.Array.Empty<FuturisticSpriteLibrary.PlayerStyleDefinition>();

    private void OnEnable()
    {
        TrySubscribeToProgressionManager();
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            CloseMenu();
        }

        UnsubscribeFromProgressionManager();
    }

    private void Update()
    {
        TrySubscribeToProgressionManager();

        if (isOpen && Input.GetMouseButtonDown(0) && IsPointerOutsidePanel())
        {
            CloseMenu();
            return;
        }

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

    public void ToggleFromExternal()
    {
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

        if (string.IsNullOrEmpty(FuturisticSpriteLibrary.CurrentMainPlayerStyleId))
        {
            string defaultStyle = FuturisticSpriteLibrary.GetDefaultStyleIdForElement(CombatUnit.Element.Earth);
            if (PartyManager.Instance != null)
            {
                CombatUnit.Element selectedElement = PartyManager.Instance.GetMainCharacterElement();
                if (selectedElement != CombatUnit.Element.None)
                {
                    defaultStyle = FuturisticSpriteLibrary.GetDefaultStyleIdForElement(selectedElement);
                }
            }

            FuturisticSpriteLibrary.SetCurrentMainPlayerStyle(defaultStyle);
            player.SetPlayerVisualStyle(defaultStyle);
        }

        isOpen = true;
        EnsureCanvas();
        BuildPanel();

        if (string.IsNullOrEmpty(player.CurrentStyleId) && stylePresets.Length > 0)
        {
            string styleToApply = FuturisticSpriteLibrary.CurrentMainPlayerStyleId;
            if (string.IsNullOrEmpty(styleToApply))
            {
                styleToApply = stylePresets[0].Id;
            }

            player.SetPlayerVisualStyle(styleToApply);
            BuildPanel();
        }

        PauseGameplay();
    }

    private void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        panelRect = null;

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

        ResumeGameplay();
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

        bool cosmeticEconomyEnabled = IsCosmeticEconomyEnabled();

        IReadOnlyList<FuturisticSpriteLibrary.PlayerStyleDefinition> styles = FuturisticSpriteLibrary.GetPlayerStyles();
        stylePresets = new FuturisticSpriteLibrary.PlayerStyleDefinition[styles.Count];
        for (int i = 0; i < styles.Count; i++)
        {
            stylePresets[i] = styles[i];
        }

        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        panelRoot = new GameObject("CustomizationPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(menuCanvas.transform, false);

        panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = panelColor;

        CreateLabel(panelRoot.transform, "CHARACTER CUSTOMIZATION", new Vector2(24f, -24f), new Vector2(panelSize.x - 48f, 30f), headerColor, 24, FontStyle.Bold);
        currencyLabel = CreateLabel(panelRoot.transform, string.Empty, new Vector2(24f, -58f), new Vector2(panelSize.x - 48f, 22f), textColor, 16, FontStyle.Bold);
        string description = cosmeticEconomyEnabled
            ? "Futuristic element suits. Premium styles cost Cosmetic XP."
            : "Futuristic element suits. Cosmetic economy is disabled for this slice.";
        CreateLabel(panelRoot.transform, description, new Vector2(24f, -82f), new Vector2(panelSize.x - 48f, 20f), subTextColor, 12, FontStyle.Normal);

        RefreshCurrencyLabel();

        float startY = 116f;
        float cardHeight = 84f;
        float rowGap = 10f;
        float cardGap = 12f;
        float sidePadding = 20f;
        float cardWidth = (panelSize.x - sidePadding * 2f - cardGap) * 0.5f;

        for (int i = 0; i < stylePresets.Length; i++)
        {
            int row = i / 2;
            int column = i % 2;
            float x = sidePadding + (cardWidth + cardGap) * column;
            float y = startY + (cardHeight + rowGap) * row;
            CreatePresetRow(panelRoot.transform, stylePresets[i], x, y, cardWidth, cardHeight, cosmeticEconomyEnabled);
        }

        CreateLabel(panelRoot.transform, $"[{toggleKey}] Close", new Vector2(24f, -(panelSize.y - 28f)), new Vector2(panelSize.x - 48f, 20f), subTextColor, 12, FontStyle.Italic);
    }

    private void CreatePresetRow(Transform parent, FuturisticSpriteLibrary.PlayerStyleDefinition preset, float x, float y, float width, float height, bool cosmeticEconomyEnabled)
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
        swatchRect.sizeDelta = new Vector2(50f, 50f);
        swatchRect.anchoredPosition = new Vector2(8f, 0f);
        Image swatchImage = swatchObject.GetComponent<Image>();
        swatchImage.sprite = FuturisticSpriteLibrary.GetPlayerStyleIcon(preset.Id);
        swatchImage.color = Color.white;
        swatchImage.preserveAspect = true;

        float textWidth = Mathf.Max(80f, width - 164f);
        CreateLabel(rowObject.transform, preset.DisplayName, new Vector2(62f, -10f), new Vector2(textWidth, 22f), textColor, 14, FontStyle.Bold);
        CreateLabel(rowObject.transform, preset.Element.ToString(), new Vector2(62f, -30f), new Vector2(textWidth, 18f), subTextColor, 11, FontStyle.Italic);

        bool requiresUnlock = cosmeticEconomyEnabled && preset.IsPremium;
        string tag = requiresUnlock ? $"PREMIUM {preset.Cost} XP" : "AVAILABLE";
        Color tagColor = requiresUnlock ? premiumTagColor : freeTagColor;
        CreateLabel(rowObject.transform, tag, new Vector2(62f, -48f), new Vector2(textWidth + 20f, 20f), tagColor, 11, FontStyle.Bold);

        string buttonText = requiresUnlock && !IsPresetUnlocked(preset) ? "Unlock" : "Apply";
        if (player != null && player.CurrentStyleId == preset.Id)
        {
            buttonText = "Equipped";
        }

        Button applyButton = CreateButton(rowObject.transform, buttonText, new Vector2(0.68f, 0.15f), new Vector2(0.96f, 0.85f));
        applyButton.onClick.AddListener(() => OnPresetSelected(preset));

        Text buttonLabel = applyButton.GetComponentInChildren<Text>();
        if (buttonLabel != null)
        {
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.fontSize = 12;
        }
    }

    private void OnPresetSelected(FuturisticSpriteLibrary.PlayerStyleDefinition preset)
    {
        if (preset == null)
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<IsometricPlayer>();
        }

        if (player == null)
        {
            return;
        }

        bool cosmeticEconomyEnabled = IsCosmeticEconomyEnabled();
        bool unlocked = IsPresetUnlocked(preset);
        if (!unlocked)
        {
            HeroProgressionManager manager = HeroProgressionManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[PlayerCustomizationUI] HeroProgressionManager not found.");
                return;
            }

            if (cosmeticEconomyEnabled && !manager.TryUnlockPlayerColorPreset(preset.Id, preset.Cost))
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

        player.SetPlayerVisualStyle(preset.Id);
        BuildPanel();
    }

    private static bool IsPresetUnlocked(FuturisticSpriteLibrary.PlayerStyleDefinition preset)
    {
        if (preset == null)
        {
            return false;
        }

        if (!preset.IsPremium || !IsCosmeticEconomyEnabled())
        {
            return true;
        }

        HeroProgressionManager manager = HeroProgressionManager.Instance;
        if (manager == null)
        {
            return false;
        }

        return manager.IsPlayerColorPresetUnlocked(preset.Id);
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

    private bool IsPointerOutsidePanel()
    {
        if (panelRect == null)
        {
            return false;
        }

        return !RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, null);
    }

    private void PauseGameplay()
    {
        if (player != null)
        {
            wasPlayerMoveEnabled = player.canMove;
            player.canMove = false;
        }

        if (!hasPausedTimeScale)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedTimeScale = true;
        }
    }

    private void ResumeGameplay()
    {
        if (player != null)
        {
            player.canMove = wasPlayerMoveEnabled;
        }

        if (hasPausedTimeScale)
        {
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            hasPausedTimeScale = false;
        }
    }

    private void RefreshCurrencyLabel()
    {
        if (currencyLabel == null)
        {
            return;
        }

        if (!IsCosmeticEconomyEnabled())
        {
            currencyLabel.text = "Cosmetic XP: Disabled";
            return;
        }

        int xp = HeroProgressionManager.Instance != null
            ? HeroProgressionManager.Instance.GetCosmeticXp()
            : 0;
        currencyLabel.text = $"Cosmetic XP: {xp}";
    }

    private static bool IsCosmeticEconomyEnabled()
    {
        HeroProgressionManager manager = HeroProgressionManager.Instance;
        if (manager != null)
        {
            return manager.IsCosmeticProgressionEnabled;
        }

        return HeroProgressionManager.IsRuntimeCosmeticProgressionEconomyEnabled;
    }

    private void TrySubscribeToProgressionManager()
    {
        HeroProgressionManager manager = HeroProgressionManager.Instance;
        if (manager == null || manager == subscribedProgressionManager)
        {
            return;
        }

        UnsubscribeFromProgressionManager();
        subscribedProgressionManager = manager;
        subscribedProgressionManager.OnCosmeticXpChanged += HandleCosmeticXpChanged;
        subscribedProgressionManager.OnPlayerColorPresetUnlocked += HandlePresetUnlocked;
        RefreshCurrencyLabel();
    }

    private void UnsubscribeFromProgressionManager()
    {
        if (subscribedProgressionManager == null)
        {
            return;
        }

        subscribedProgressionManager.OnCosmeticXpChanged -= HandleCosmeticXpChanged;
        subscribedProgressionManager.OnPlayerColorPresetUnlocked -= HandlePresetUnlocked;
        subscribedProgressionManager = null;
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
