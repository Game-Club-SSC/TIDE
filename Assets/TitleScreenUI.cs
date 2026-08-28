using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Player-facing title screen (issue #294). Builds a procedural Persona-styled
/// menu with New Game, Continue, Settings, and Quit. New Game resets the world
/// state and loads the first island scene; Continue is only enabled while a
/// persisted save exists and restores the saved scene after loading; Settings
/// opens the existing AudioSettingsUI; Quit exits the application.
/// </summary>
[DisallowMultipleComponent]
public class TitleScreenUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float buttonWidth = 420f;
    [SerializeField] private float buttonHeight = 64f;
    [SerializeField] private float buttonSpacing = 18f;

    [Header("Colors")]
    [SerializeField] private Color titleColor = PersonaUIStyle.OffWhite;
    [SerializeField] private Color subtitleColor = PersonaUIStyle.DimText;
    [SerializeField] private Color primaryButtonColor = PersonaUIStyle.BrightBlue;
    [SerializeField] private Color continueButtonColor = new Color(0.25f, 0.55f, 0.85f, 0.9f);
    [SerializeField] private Color disabledButtonColor = new Color(0.3f, 0.32f, 0.4f, 0.8f);
    [SerializeField] private Color elementPanelColor = new Color(0.035f, 0.07f, 0.15f, 0.97f);

    private Canvas canvas;
    private bool isReady;
    private bool quitRequested;
    private bool newGameRequested;
    private bool continueRequested;
    private bool isChoosingElement;
    private GameObject menuStack;
    private GameObject elementSelectionPanel;
    private Button elementCancelButton;

    // ----- Test accessors (internal Debug* props per repo conventions) -----

    internal bool IsReady => isReady;
    internal bool DebugQuitRequested => quitRequested;
    internal bool DebugNewGameRequested => newGameRequested;
    internal bool DebugContinueRequested => continueRequested;
    internal bool IsChoosingElement => isChoosingElement;

    internal Button NewGameButton { get; private set; }
    internal Button ContinueButton { get; private set; }
    internal Button SettingsButton { get; private set; }
    internal Button QuitButton { get; private set; }
    internal Button[] ElementButtons { get; private set; } = System.Array.Empty<Button>();

    private void Awake()
    {
        EnsureGamepadInputManager();
        EnsureUI();
        RefreshContinueButton();
    }

    private void Start()
    {
        // GameStateManager normally creates the persistent audio singleton. The
        // title scene also has one in its scene data, but keep this guard so a
        // test scene or a direct scene load still gets menu music.
        AudioManager audioManager = EnsureAudioManager();
        if (audioManager != null)
        {
            audioManager.HandleMenuBgm();
        }

        SelectButton(NewGameButton);
    }

    private void OnEnable()
    {
        if (!isReady)
        {
            EnsureUI();
        }

        RefreshContinueButton();
    }

    private static void EnsureGamepadInputManager()
    {
        if (GamepadInputManager.Instance != null)
        {
            return;
        }

        GameObject gamepadObject = new GameObject("GamepadInputManager");
        gamepadObject.AddComponent<GamepadInputManager>();
        Debug.Log("[TitleScreenUI] Created GamepadInputManager for controller support.");
    }

    /// <summary>
    /// Builds the title menu. Idempotent; safe to call from tests.
    /// </summary>
    public void EnsureUI()
    {
        if (isReady)
        {
            return;
        }

        GameObject canvasObject = PersonaUIStyle.CreateOverlayCanvas("TitleScreenCanvas", 500);
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();

        // Title block
        Text title = PersonaUIStyle.CreatePersonaLabel(canvasObject.transform, "T I D E", 96, titleColor, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(titleRect);
        titleRect.anchorMin = new Vector2(0f, 0.72f);
        titleRect.anchorMax = new Vector2(1f, 0.92f);

        Text subtitle = PersonaUIStyle.CreatePersonaLabel(canvasObject.transform, "The corrupted isles call...", 26, subtitleColor, TextAnchor.MiddleCenter);
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(subtitleRect);
        subtitleRect.anchorMin = new Vector2(0f, 0.64f);
        subtitleRect.anchorMax = new Vector2(1f, 0.72f);

        // Button stack
        menuStack = new GameObject("ButtonStack", typeof(RectTransform));
        menuStack.transform.SetParent(canvasObject.transform, false);
        RectTransform stackRect = menuStack.GetComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(0.5f, 0.28f);
        stackRect.anchorMax = new Vector2(0.5f, 0.60f);
        stackRect.pivot = new Vector2(0.5f, 0.5f);
        stackRect.sizeDelta = new Vector2(buttonWidth, buttonHeight * 4f + buttonSpacing * 3f);

        VerticalLayoutGroup vlg = menuStack.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = buttonSpacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        NewGameButton = CreateStackButton(menuStack.transform, "New Game", primaryButtonColor, OnNewGameClicked);
        ContinueButton = CreateStackButton(menuStack.transform, "Continue", continueButtonColor, OnContinueClicked);
        SettingsButton = CreateStackButton(menuStack.transform, "Settings", primaryButtonColor, OnSettingsClicked);
        QuitButton = CreateStackButton(menuStack.transform, "Quit", new Color(0.55f, 0.2f, 0.22f, 0.95f), OnQuitClicked);
        ConfigureMenuNavigation();

        isReady = true;
        Debug.Log("[TitleScreenUI] Title screen UI ready.");
    }

    private static AudioManager EnsureAudioManager()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
        }

        if (audioManager == null)
        {
            GameObject audioObject = new GameObject("AudioManager");
            audioManager = audioObject.AddComponent<AudioManager>();
        }

        return audioManager;
    }

    private void ConfigureMenuNavigation()
    {
        Button[] buttons = { NewGameButton, ContinueButton, SettingsButton, QuitButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnDown = buttons[(i + 1) % buttons.Length];
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            button.navigation = navigation;
        }
    }

    private static void SelectButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button.gameObject);
        button.Select();
    }

    private static Button CreateStackButton(Transform parent, string label, Color backgroundColor, UnityEngine.Events.UnityAction onClick)
    {
        Button button = PersonaUIStyle.CreateButton(parent, label, backgroundColor);
        button.onClick.AddListener(onClick);
        return button;
    }

    /// <summary>
    /// Enables/disables the Continue button based on whether a persisted save
    /// exists. Called on enable and after any action that could change save state.
    /// </summary>
    public void RefreshContinueButton()
    {
        if (!isReady)
        {
            EnsureUI();
        }

        bool hasSave = HasPersistedSave();
        ContinueButton.interactable = hasSave;

        ColorBlock colors = ContinueButton.colors;
        colors.disabledColor = disabledButtonColor;
        ContinueButton.colors = colors;
    }

    /// <summary>
    /// True when a save exists that Continue can actually restore through
    /// GameStateManager. WorldSaveService's V2 envelope is not yet the runtime
    /// load source, so it must not enable Continue on its own.
    /// </summary>
    internal bool HasPersistedSave()
    {
        GameStateManager gsm = GameStateManager.Instance;
        return gsm != null && gsm.HasLoadableWorldState();
    }

    private void OnNewGameClicked()
    {
        if (isChoosingElement)
        {
            return;
        }

        ShowElementSelection();
    }

    private void ShowElementSelection()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            Debug.LogWarning("[TitleScreenUI] Element choice ignored: no GameStateManager available.");
            return;
        }

        if (canvas == null || elementSelectionPanel != null)
        {
            return;
        }

        isChoosingElement = true;
        if (menuStack != null)
        {
            menuStack.SetActive(false);
        }

        elementSelectionPanel = new GameObject("ElementSelectionPanel", typeof(RectTransform), typeof(Image));
        elementSelectionPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = elementSelectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 650f);

        Image panelImage = elementSelectionPanel.GetComponent<Image>();
        panelImage.color = elementPanelColor;
        PersonaUIStyle.AddDiagonalEdge(panelRect, 16f);
        PersonaUIStyle.CreateAccentSlash(elementSelectionPanel.transform, PersonaUIStyle.BrightBlue, 6f);

        CreateSelectionLabel("CHOOSE YOUR AFFINITY", 32, titleColor, new Vector2(0f, -44f), new Vector2(500f, 42f));
        CreateSelectionLabel("This shapes the main character's combat element.", 17, subtitleColor, new Vector2(0f, -86f), new Vector2(480f, 28f));

        CombatUnit.Element[] elements =
        {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        ElementButtons = new Button[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            CombatUnit.Element element = elements[i];
            Button button = PersonaUIStyle.CreateButton(elementSelectionPanel.transform, element.ToString(), GetElementButtonColor(element));
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2(0f, -128f - i * 76f);
            buttonRect.sizeDelta = new Vector2(400f, 58f);
            button.onClick.AddListener(() => StartNewGameWithElement(element));
            ElementButtons[i] = button;
        }

        elementCancelButton = PersonaUIStyle.CreateButton(elementSelectionPanel.transform, "Back", disabledButtonColor);
        RectTransform cancelRect = elementCancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);
        cancelRect.anchoredPosition = new Vector2(0f, 28f);
        cancelRect.sizeDelta = new Vector2(220f, 48f);
        elementCancelButton.onClick.AddListener(HideElementSelection);

        ConfigureElementNavigation();
        SelectButton(ElementButtons[0]);
        Debug.Log("[TitleScreenUI] Showing main-character affinity choice.");
    }

    private void ConfigureElementNavigation()
    {
        if (ElementButtons == null || ElementButtons.Length == 0 || elementCancelButton == null)
        {
            return;
        }

        for (int i = 0; i < ElementButtons.Length; i++)
        {
            Button button = ElementButtons[i];
            if (button == null)
            {
                continue;
            }

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = i > 0 ? ElementButtons[i - 1] : elementCancelButton;
            navigation.selectOnDown = i < ElementButtons.Length - 1 ? ElementButtons[i + 1] : elementCancelButton;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            button.navigation = navigation;
        }

        Navigation cancelNavigation = elementCancelButton.navigation;
        cancelNavigation.mode = Navigation.Mode.Explicit;
        cancelNavigation.selectOnUp = ElementButtons[ElementButtons.Length - 1];
        cancelNavigation.selectOnDown = ElementButtons[0];
        cancelNavigation.selectOnLeft = null;
        cancelNavigation.selectOnRight = null;
        elementCancelButton.navigation = cancelNavigation;
    }

    private void StartNewGameWithElement(CombatUnit.Element element)
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null || !PartyManager.PersistMainCharacterElement(element))
        {
            Debug.LogWarning("[TitleScreenUI] New Game ignored: could not persist the selected affinity.");
            return;
        }

        newGameRequested = true;
        gsm.ResetWorldStateForNewGame();
        PartyManager.PersistMainCharacterElement(element);
        Debug.Log($"[TitleScreenUI] New Game: world state reset with {element} affinity.");
        HideElementSelection();

        if (Application.isPlaying)
        {
            gsm.ReturnToMainScene();
        }
    }

    private void HideElementSelection()
    {
        isChoosingElement = false;
        ElementButtons = System.Array.Empty<Button>();

        if (elementSelectionPanel != null)
        {
            Destroy(elementSelectionPanel);
            elementSelectionPanel = null;
        }

        elementCancelButton = null;

        if (menuStack != null)
        {
            menuStack.SetActive(true);
        }

        SelectButton(NewGameButton);
    }

    private void CreateSelectionLabel(string text, int fontSize, Color color, Vector2 position, Vector2 size)
    {
        Text label = PersonaUIStyle.CreatePersonaLabel(elementSelectionPanel.transform, text, fontSize, color, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = position;
        labelRect.sizeDelta = size;
    }

    private static Color GetElementButtonColor(CombatUnit.Element element)
    {
        return element switch
        {
            CombatUnit.Element.Fire => new Color(0.78f, 0.25f, 0.20f, 0.95f),
            CombatUnit.Element.Water => new Color(0.18f, 0.48f, 0.84f, 0.95f),
            CombatUnit.Element.Earth => new Color(0.28f, 0.58f, 0.32f, 0.95f),
            CombatUnit.Element.Air => new Color(0.46f, 0.70f, 0.86f, 0.95f),
            CombatUnit.Element.Space => new Color(0.48f, 0.28f, 0.72f, 0.95f),
            _ => PersonaUIStyle.BrightBlue
        };
    }

    private void OnContinueClicked()
    {
        if (!HasPersistedSave())
        {
            Debug.Log("[TitleScreenUI] Continue ignored: no save data present.");
            return;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            Debug.LogWarning("[TitleScreenUI] Continue ignored: no GameStateManager available.");
            return;
        }

        continueRequested = true;
        Debug.Log("[TitleScreenUI] Continue: loading world state and restoring scene.");
        gsm.LoadWorldStateAndRestoreScene();
    }

    private void OnSettingsClicked()
    {
        AudioSettingsUI settingsUI = FindFirstObjectByType<AudioSettingsUI>();
        if (settingsUI == null)
        {
            GameObject settingsObject = new GameObject("AudioSettingsUI");
            settingsObject.transform.SetParent(transform, false);
            settingsUI = settingsObject.AddComponent<AudioSettingsUI>();
        }

        settingsUI.SetVisible(true);
        Debug.Log("[TitleScreenUI] Opened audio settings.");
    }

    private void OnQuitClicked()
    {
        quitRequested = true;
        Debug.Log("[TitleScreenUI] Quit requested.");
        if (Application.isPlaying)
        {
            Application.Quit();
        }
    }
}
