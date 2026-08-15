using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exploration pause overlay (issue #294). Opens with ESC/P, the gamepad Start
/// button, or the mobile pause button, and exposes Resume, Party, Inventory,
/// Save, Load, Settings, and Quit to Menu. Save persists via
/// GameStateManager.SaveWorldState; Load confirms, then restores the saved
/// world state and transitions to the correct scene.
/// </summary>
[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float panelWidth = 560f;
    [SerializeField] private float buttonHeight = 56f;
    [SerializeField] private float buttonSpacing = 12f;
    [SerializeField] private float padding = 20f;

    [Header("Colors")]
    [SerializeField] private Color panelBackground = new Color(0.06f, 0.07f, 0.12f, 0.97f);
    [SerializeField] private Color titleColor = PersonaUIStyle.OffWhite;
    [SerializeField] private Color buttonColor = PersonaUIStyle.BrightBlue;
    [SerializeField] private Color dangerButtonColor = new Color(0.65f, 0.2f, 0.22f, 0.95f);
    [SerializeField] private Color feedbackColor = PersonaUIStyle.Gold;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode altPauseKey = KeyCode.P;

    private Canvas canvas;
    private GameObject panelRoot;
    private GameObject confirmRoot;
    private Text feedbackText;
    private GameObject mobilePauseButton;
    private bool isOpen;
    private bool confirmLoadVisible;
    private float previousTimeScale = 1f;

    private const float FeedbackDuration = 2.5f;
    private float feedbackTimer;
    private string queuedFeedback;

    // ----- Test accessors (internal Debug* props per repo conventions) -----

    internal bool IsOpen => isOpen;
    internal bool ConfirmLoadVisible => confirmLoadVisible;
    internal bool DebugLoadConfirmed { get; private set; }

    internal Button ResumeButton { get; private set; }
    internal Button PartyButton { get; private set; }
    internal Button InventoryButton { get; private set; }
    internal Button SaveButton { get; private set; }
    internal Button LoadButton { get; private set; }
    internal Button SettingsButton { get; private set; }
    internal Button QuitToMenuButton { get; private set; }
    internal Button ConfirmYesButton { get; private set; }
    internal Button ConfirmNoButton { get; private set; }

    private void Awake()
    {
        EnsureGamepadInputManager();
        EnsureUI();
        SetOpen(false);
    }

    private void Start()
    {
        // Late hook so scenes that add PauseMenuUI at runtime (via GameStateManager)
        // still build their canvas on the first frame.
        EnsureUI();
    }

    private void OnDestroy()
    {
        RestoreTimeIfOpen();
    }

    private void OnEnable()
    {
        if (!isOpen)
        {
            ShowMobilePauseButton(true);
        }
    }

    private void OnDisable()
    {
        // Disabling the component does not destroy its canvas. Close the panel
        // and restore time so an external UI toggle cannot leave play frozen.
        if (isOpen)
        {
            RestoreTimeIfOpen();
            SetOpen(false);
        }

        ShowMobilePauseButton(false);
    }

    private void Update()
    {
        if (queuedFeedback != null)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f)
            {
                queuedFeedback = null;
                if (feedbackText != null)
                {
                    feedbackText.text = string.Empty;
                }
            }
        }

        if (HasSubmenuOpen())
        {
            // A submenu (gear/party/settings) is layered on top. Defer to it:
            // ESC closes the topmost submenu instead of toggling the pause menu
            // (avoids the double-close race where both handlers fire the same
            // frame, and keeps ESC from opening pause while a submenu is up).
            if (Input.GetKeyDown(pauseKey))
            {
                CloseTopmostSubmenu();
            }

            return;
        }

        bool pausePressed = Input.GetKeyDown(pauseKey);

        // The P key doubles as PartySetupUI's toggle key. Only treat it as a
        // pause key when no party UI exists in the scene so the two never fight.
        if (altPauseKey == KeyCode.P && FindFirstObjectByType<PartySetupUI>() == null)
        {
            pausePressed = pausePressed || Input.GetKeyDown(altPauseKey);
        }

        GamepadInputManager gamepad = GamepadInputManager.Instance;
        if (gamepad != null && gamepad.IsGamepadConnected && gamepad.MenuPressed)
        {
            pausePressed = true;
        }

        MobileTouchInputManager mobile = MobileTouchInputManager.Instance;
        if (mobile != null && mobile.IsMobilePlatform && mobile.MenuPressed)
        {
            pausePressed = true;
        }

        if (!pausePressed)
        {
            return;
        }

        if (isOpen)
        {
            CloseMenu();
        }
        else if (CanTogglePause())
        {
            OpenMenu();
        }
    }

    private bool HasSubmenuOpen()
    {
        GearInventoryUI gearUI = FindFirstObjectByType<GearInventoryUI>();
        if (gearUI != null && gearUI.IsOpen)
        {
            return true;
        }

        PartySetupUI partyUI = FindFirstObjectByType<PartySetupUI>();
        if (partyUI != null && partyUI.IsOpen)
        {
            return true;
        }

        AudioSettingsUI settingsUI = FindFirstObjectByType<AudioSettingsUI>();
        return settingsUI != null && settingsUI.IsVisible;
    }

    private void CloseTopmostSubmenu()
    {
        AudioSettingsUI settingsUI = FindFirstObjectByType<AudioSettingsUI>();
        if (settingsUI != null && settingsUI.IsVisible)
        {
            settingsUI.SetVisible(false);
            return;
        }

        PartySetupUI partyUI = FindFirstObjectByType<PartySetupUI>();
        if (partyUI != null && partyUI.IsOpen)
        {
            partyUI.CloseMenu();
            return;
        }

        GearInventoryUI gearUI = FindFirstObjectByType<GearInventoryUI>();
        if (gearUI != null && gearUI.IsOpen)
        {
            gearUI.CloseMenu();
        }
    }

    // ----- Public API -----

    public void ToggleMenu()
    {
        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isOpen)
        {
            return;
        }

        if (!CanTogglePause())
        {
            Debug.Log("[PauseMenuUI] Cannot open pause menu right now (not in exploration or transitioning).");
            return;
        }

        EnsureUI();
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        isOpen = true;
        confirmLoadVisible = false;
        SetOpen(true);
        ShowMobilePauseButton(false);

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleMenuOpen();
        }

        Debug.Log("[PauseMenuUI] Pause menu opened.");
    }

    public void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        confirmLoadVisible = false;
        SetOpen(false);
        ShowMobilePauseButton(true);

        // Always restore to 1f. The pause menu can only open during Exploration
        // (verified by CanTogglePause), where timeScale is always 1. Using a
        // cached previousTimeScale risked restoring a stale value if external
        // code modified timeScale while the menu was open.
        Time.timeScale = 1f;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleMenuClose();
        }

        Debug.Log("[PauseMenuUI] Pause menu closed.");
    }

    public void OnResumeClicked()
    {
        CloseMenu();
    }

    public void OnPartyClicked()
    {
        PartySetupUI partyUI = FindFirstObjectByType<PartySetupUI>();
        if (partyUI == null)
        {
            GameObject partyObject = new GameObject("PartySetupUI");
            partyObject.transform.SetParent(transform, false);
            partyUI = partyObject.AddComponent<PartySetupUI>();
        }

        if (!partyUI.IsOpen)
        {
            partyUI.OpenMenu();
        }

        Debug.Log("[PauseMenuUI] Opened party setup.");
    }

    public void OnInventoryClicked()
    {
        GearInventoryUI inventoryUI = FindFirstObjectByType<GearInventoryUI>();
        if (inventoryUI == null)
        {
            GameObject inventoryObject = new GameObject("GearInventoryUI");
            inventoryObject.transform.SetParent(transform, false);
            inventoryUI = inventoryObject.AddComponent<GearInventoryUI>();
        }

        if (!inventoryUI.IsOpen)
        {
            inventoryUI.OpenMenu();
        }

        Debug.Log("[PauseMenuUI] Opened gear inventory.");
    }

    public void OnSaveClicked()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            SetFeedback("No GameStateManager available.");
            return;
        }

        gsm.SaveWorldState();
        SetFeedback("Game saved.");
        Debug.Log("[PauseMenuUI] World state saved.");
    }

    public void OnLoadClicked()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null || !gsm.HasLoadableWorldState())
        {
            SetFeedback("No save data to load.");
            return;
        }

        confirmLoadVisible = true;
        if (confirmRoot != null)
        {
            confirmRoot.SetActive(true);
        }

        Debug.Log("[PauseMenuUI] Load confirmation shown.");
    }

    public void OnLoadConfirmed()
    {
        confirmLoadVisible = false;
        if (confirmRoot != null)
        {
            confirmRoot.SetActive(false);
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            SetFeedback("No GameStateManager available.");
            return;
        }

        // Restore time before the scene transition so the freshly loaded scene
        // does not start frozen (the pause canvas dies with the unloaded scene).
        Time.timeScale = 1f;
        isOpen = false;
        SetOpen(false);

        DebugLoadConfirmed = true;
        Debug.Log("[PauseMenuUI] Load confirmed; restoring world state.");
        gsm.LoadWorldStateAndRestoreScene();
    }

    public void OnLoadCancelled()
    {
        confirmLoadVisible = false;
        if (confirmRoot != null)
        {
            confirmRoot.SetActive(false);
        }
    }

    public void OnSettingsClicked()
    {
        AudioSettingsUI settingsUI = FindFirstObjectByType<AudioSettingsUI>();
        if (settingsUI == null)
        {
            GameObject settingsObject = new GameObject("AudioSettingsUI");
            settingsObject.transform.SetParent(transform, false);
            settingsUI = settingsObject.AddComponent<AudioSettingsUI>();
        }

        settingsUI.SetVisible(true);
        Debug.Log("[PauseMenuUI] Opened audio settings.");
    }

    public void OnQuitToMenuClicked()
    {
        if (confirmLoadVisible)
        {
            confirmLoadVisible = false;
            if (confirmRoot != null)
            {
                confirmRoot.SetActive(false);
            }
        }

        CloseMenu();

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            Debug.LogWarning("[PauseMenuUI] Quit to menu ignored: no GameStateManager available.");
            return;
        }

        if (Application.isPlaying)
        {
            gsm.ReturnToTitleScene();
        }
    }

    private void SetFeedback(string message)
    {
        queuedFeedback = message;
        feedbackTimer = FeedbackDuration;
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    // ----- UI construction -----

    private static void EnsureGamepadInputManager()
    {
        if (GamepadInputManager.Instance != null)
        {
            return;
        }

        GameObject gamepadObject = new GameObject("GamepadInputManager");
        gamepadObject.AddComponent<GamepadInputManager>();
        Debug.Log("[PauseMenuUI] Created GamepadInputManager for controller support.");
    }

    public void EnsureUI()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = PersonaUIStyle.CreateOverlayCanvas("PauseMenuCanvas", 850);
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();

        panelRoot = new GameObject("PauseMenuPanel", typeof(RectTransform));
        panelRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, 7f * buttonHeight + 6f * buttonSpacing + padding * 2f + 60f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = panelBackground;
        panelBg.raycastTarget = true;

        Text title = PersonaUIStyle.CreatePersonaLabel(panelRoot.transform, "PAUSED", 34, titleColor, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(titleRect);
        titleRect.anchorMin = new Vector2(0f, 0.92f);
        titleRect.anchorMax = new Vector2(1f, 1f);

        float y = padding;
        float rowHeight = buttonHeight;

        ResumeButton = CreatePanelButton(panelRoot.transform, "Resume", buttonColor, OnResumeClicked, ref y, rowHeight);
        PartyButton = CreatePanelButton(panelRoot.transform, "Party", buttonColor, OnPartyClicked, ref y, rowHeight);
        InventoryButton = CreatePanelButton(panelRoot.transform, "Inventory", buttonColor, OnInventoryClicked, ref y, rowHeight);
        SaveButton = CreatePanelButton(panelRoot.transform, "Save", buttonColor, OnSaveClicked, ref y, rowHeight);
        LoadButton = CreatePanelButton(panelRoot.transform, "Load", buttonColor, OnLoadClicked, ref y, rowHeight);
        SettingsButton = CreatePanelButton(panelRoot.transform, "Settings", buttonColor, OnSettingsClicked, ref y, rowHeight);
        QuitToMenuButton = CreatePanelButton(panelRoot.transform, "Quit to Menu", dangerButtonColor, OnQuitToMenuClicked, ref y, rowHeight);

        feedbackText = PersonaUIStyle.CreatePersonaLabel(panelRoot.transform, string.Empty, 16, feedbackColor, TextAnchor.MiddleCenter);
        RectTransform feedbackRect = feedbackText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(feedbackRect);
        feedbackRect.anchorMin = new Vector2(0f, 0f);
        feedbackRect.anchorMax = new Vector2(1f, 0.05f);

        BuildLoadConfirmPanel(canvasObject.transform);
        BuildMobilePauseButton(canvasObject.transform);
    }

    private Button CreatePanelButton(Transform parent, string label, Color backgroundColor, UnityEngine.Events.UnityAction onClick, ref float y, float rowHeight)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.offsetMin = new Vector2(padding, -(y + rowHeight));
        buttonRect.offsetMax = new Vector2(-padding, -y);

        Image buttonBg = buttonObject.AddComponent<Image>();
        buttonBg.color = backgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.25f, 0.45f, 0.95f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text labelText = PersonaUIStyle.CreatePersonaLabel(buttonObject.transform, label, 22, PersonaUIStyle.White, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(labelRect);
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-4f, -2f);

        y += rowHeight + buttonSpacing;
        return button;
    }

    private void BuildLoadConfirmPanel(Transform canvasTransform)
    {
        confirmRoot = new GameObject("LoadConfirmPanel", typeof(RectTransform));
        confirmRoot.transform.SetParent(canvasTransform, false);
        RectTransform confirmRect = confirmRoot.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.sizeDelta = new Vector2(panelWidth * 0.8f, 180f);
        confirmRect.anchoredPosition = Vector2.zero;

        Image confirmBg = confirmRoot.AddComponent<Image>();
        confirmBg.color = new Color(0.1f, 0.12f, 0.18f, 0.98f);
        confirmBg.raycastTarget = true;

        Text prompt = PersonaUIStyle.CreatePersonaLabel(confirmRoot.transform, "Load saved game?\nCurrent progress will be replaced.", 20, PersonaUIStyle.OffWhite, TextAnchor.MiddleCenter);
        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(promptRect);
        promptRect.anchorMin = new Vector2(0f, 0.5f);
        promptRect.anchorMax = new Vector2(1f, 0.95f);

        ConfirmYesButton = CreateConfirmButton(confirmRoot.transform, "Yes", new Color(0.25f, 0.55f, 0.3f, 0.95f), OnLoadConfirmed, new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.4f));
        ConfirmNoButton = CreateConfirmButton(confirmRoot.transform, "No", new Color(0.55f, 0.2f, 0.22f, 0.95f), OnLoadCancelled, new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.4f));

        confirmRoot.SetActive(false);
    }

    private static Button CreateConfirmButton(Transform parent, string label, Color backgroundColor, UnityEngine.Events.UnityAction onClick, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonBg = buttonObject.AddComponent<Image>();
        buttonBg.color = backgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        button.onClick.AddListener(onClick);

        Text labelText = PersonaUIStyle.CreatePersonaLabel(buttonObject.transform, label, 20, PersonaUIStyle.White, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(labelRect);
        return button;
    }

    private void BuildMobilePauseButton(Transform canvasTransform)
    {
        mobilePauseButton = new GameObject("MobilePauseButton", typeof(RectTransform));
        mobilePauseButton.transform.SetParent(canvasTransform, false);
        RectTransform buttonRect = mobilePauseButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-28f, -28f);
        buttonRect.sizeDelta = new Vector2(72f, 72f);

        Image buttonBg = mobilePauseButton.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.4f, 0.8f, 0.85f);

        Button button = mobilePauseButton.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        button.onClick.AddListener(ToggleMenu);

        Text labelText = PersonaUIStyle.CreatePersonaLabel(mobilePauseButton.transform, "II", 24, PersonaUIStyle.White, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(labelRect);

        bool isMobile = Application.isMobilePlatform
            || (MobileTouchInputManager.Instance != null && MobileTouchInputManager.Instance.IsMobilePlatform);
        mobilePauseButton.SetActive(isMobile);
    }

    private void ShowMobilePauseButton(bool visible)
    {
        if (mobilePauseButton == null)
        {
            return;
        }

        bool isMobile = Application.isMobilePlatform
            || (MobileTouchInputManager.Instance != null && MobileTouchInputManager.Instance.IsMobilePlatform);
        mobilePauseButton.SetActive(visible && isMobile);
    }

    private void SetOpen(bool open)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(open);
        }

        if (confirmRoot != null)
        {
            confirmRoot.SetActive(false);
        }
    }

    private bool CanTogglePause()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return false;
        }

        return gsm.currentState == GameStateManager.GameState.Exploration && !gsm.IsTransitioning;
    }

    private void RestoreTimeIfOpen()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        confirmLoadVisible = false;
        Time.timeScale = 1f;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleMenuClose();
        }
    }
}
