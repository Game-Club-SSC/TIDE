using UnityEngine;
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

    private Canvas canvas;
    private bool isReady;
    private bool quitRequested;
    private bool newGameRequested;
    private bool continueRequested;

    // ----- Test accessors (internal Debug* props per repo conventions) -----

    internal bool IsReady => isReady;
    internal bool DebugQuitRequested => quitRequested;
    internal bool DebugNewGameRequested => newGameRequested;
    internal bool DebugContinueRequested => continueRequested;

    internal Button NewGameButton { get; private set; }
    internal Button ContinueButton { get; private set; }
    internal Button SettingsButton { get; private set; }
    internal Button QuitButton { get; private set; }

    private void Awake()
    {
        EnsureGamepadInputManager();
        EnsureUI();
        RefreshContinueButton();
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
        GameObject stackObject = new GameObject("ButtonStack", typeof(RectTransform));
        stackObject.transform.SetParent(canvasObject.transform, false);
        RectTransform stackRect = stackObject.GetComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(0.5f, 0.28f);
        stackRect.anchorMax = new Vector2(0.5f, 0.60f);
        stackRect.pivot = new Vector2(0.5f, 0.5f);
        stackRect.sizeDelta = new Vector2(buttonWidth, buttonHeight * 4f + buttonSpacing * 3f);

        VerticalLayoutGroup vlg = stackObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = buttonSpacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        NewGameButton = CreateStackButton(stackObject.transform, "New Game", primaryButtonColor, OnNewGameClicked);
        ContinueButton = CreateStackButton(stackObject.transform, "Continue", continueButtonColor, OnContinueClicked);
        SettingsButton = CreateStackButton(stackObject.transform, "Settings", primaryButtonColor, OnSettingsClicked);
        QuitButton = CreateStackButton(stackObject.transform, "Quit", new Color(0.55f, 0.2f, 0.22f, 0.95f), OnQuitClicked);

        isReady = true;
        Debug.Log("[TitleScreenUI] Title screen UI ready.");
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
    /// True when a save exists that Continue can restore. Checks WorldSaveService
    /// first (the dedicated save service), then falls back to GameStateManager's
    /// own persisted key so legacy V1 saves still enable the button.
    /// </summary>
    internal bool HasPersistedSave()
    {
        WorldSaveService saveService = WorldSaveService.Instance;
        if (saveService != null && saveService.HasPersistedData)
        {
            return saveService.TryLoadJson(out _);
        }

        GameStateManager gsm = GameStateManager.Instance;
        return gsm != null && gsm.HasLoadableWorldState();
    }

    private void OnNewGameClicked()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            Debug.LogWarning("[TitleScreenUI] New Game ignored: no GameStateManager available.");
            return;
        }

        newGameRequested = true;
        gsm.ResetWorldStateForNewGame();
        Debug.Log("[TitleScreenUI] New Game: world state reset.");

        if (Application.isPlaying)
        {
            gsm.ReturnToMainScene();
        }
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
