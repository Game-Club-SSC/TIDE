using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DevMenuUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panelRoot;
    private Text summaryText;
    private Text headerText;
    private bool isVisible;
    private float summaryRefreshTimer;
    private readonly KeyCode[] remapKeys = { KeyCode.Return, KeyCode.E, KeyCode.F, KeyCode.LeftAlt, KeyCode.Space };

    public bool IsVisible => isVisible;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        EnsureEventSystem();

        summaryRefreshTimer -= Time.unscaledDeltaTime;
        if (summaryRefreshTimer <= 0f)
        {
            summaryRefreshTimer = 0.2f;
            RefreshSummary();
        }
    }

    public void Toggle()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (canvas != null)
        {
            canvas.enabled = visible;
        }

        if (visible)
        {
            EnsureEventSystem();
            RefreshSummary();
        }
    }

    private void EnsureUI()
    {
        if (canvas != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("DevMenuCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreatePanel(canvasObject.transform, "DevMenuPanel", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));

        headerText = CreateText(panelRoot.transform, "Header", "DEV GOD MODE (Konami unlocked)", 28, FontStyle.Bold, TextAnchor.UpperLeft);
        headerText.color = new Color(1f, 0.3f, 0.3f, 1f);
        RectTransform headerRect = headerText.rectTransform;
        headerRect.anchorMin = new Vector2(0.02f, 0.91f);
        headerRect.anchorMax = new Vector2(0.98f, 0.98f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        GameObject buttonGridObject = new GameObject("ButtonGrid", typeof(RectTransform));
        buttonGridObject.transform.SetParent(panelRoot.transform, false);
        RectTransform gridRect = buttonGridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.02f, 0.16f);
        gridRect.anchorMax = new Vector2(0.70f, 0.89f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;
        GridLayoutGroup grid = buttonGridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(285f, 44f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        AddActionButton(buttonGridObject.transform, "Full Reset All", () => DevCheatService.Instance?.FullResetAllState());
        AddActionButton(buttonGridObject.transform, "Reset Encounters", () => DevCheatService.Instance?.ResetEncounterAndFightState());
        AddActionButton(buttonGridObject.transform, "Reset Puzzles", () => DevCheatService.Instance?.ResetPuzzleRuntimeState());
        AddActionButton(buttonGridObject.transform, "Unlock All Islands", () => DevCheatService.Instance?.UnlockAllIslands());
        AddActionButton(buttonGridObject.transform, "Teleport Active Spawn", () => DevCheatService.Instance?.TeleportToActiveIslandSpawn());
        AddActionButton(buttonGridObject.transform, "Restore Active 100%", () => DevCheatService.Instance?.SetActiveIslandRestoration(100f));
        AddActionButton(buttonGridObject.transform, "Restore Active 0%", () => DevCheatService.Instance?.SetActiveIslandRestoration(0f));
        AddActionButton(buttonGridObject.transform, "MAX EVERYTHING", () => DevCheatService.Instance?.MaxEverything());
        AddActionButton(buttonGridObject.transform, "Hide Menu", Toggle);
        AddActionButton(buttonGridObject.transform, "Return To Island", () => DevCheatService.Instance?.ReturnToMainScene());
        AddActionButton(buttonGridObject.transform, "Start Island Flow", () => DevCheatService.Instance?.StartActiveIslandFlow());

        AddToggleButton(buttonGridObject.transform, "Godmode Invincible", () =>
        {
            if (DevCheatService.Instance != null)
            {
                DevCheatService.Instance.GodModeInvincible = !DevCheatService.Instance.GodModeInvincible;
            }
        });

        AddToggleButton(buttonGridObject.transform, "One Hit Kill", () =>
        {
            if (DevCheatService.Instance != null)
            {
                DevCheatService.Instance.GodModeOneHitKill = !DevCheatService.Instance.GodModeOneHitKill;
            }
        });

        AddToggleButton(buttonGridObject.transform, "Infinite Resources", () =>
        {
            if (DevCheatService.Instance != null)
            {
                DevCheatService.Instance.GodModeInfiniteResources = !DevCheatService.Instance.GodModeInfiniteResources;
            }
        });

        AddToggleButton(buttonGridObject.transform, "Overlay Readout", () =>
        {
            if (DevCheatService.Instance != null)
            {
                DevCheatService.Instance.ShowDebugOverlay = !DevCheatService.Instance.ShowDebugOverlay;
            }
        });

        AddMovementButton(buttonGridObject.transform, "Auto-Run Toggle", player => player.ToggleAutoRunEnabled());
        AddMovementButton(buttonGridObject.transform, "Allow Hop", player => player.ToggleAllowHop());
        AddMovementButton(buttonGridObject.transform, "Auto-Face On Interact", player => player.ToggleAutoFaceOnInteract());
        AddMovementButton(buttonGridObject.transform, "Camera Follow Polish", player => player.ToggleUseCameraFollowPolish());
        AddMovementButton(buttonGridObject.transform, "Cycle Interact Key", player => player.SetInteractKey(CycleKey(player.GetInteractKey())));
        AddMovementButton(buttonGridObject.transform, "Cycle Dash Key", player => player.SetDashKey(CycleKey(player.GetDashKey())));
        AddMovementButton(buttonGridObject.transform, "Cycle Hop Key", player => player.SetHopKey(CycleKey(player.GetHopKey())));
        AddMovementButton(buttonGridObject.transform, "Coyote Dash +", player => player.SetCoyoteDashWindow(0.2f));
        AddMovementButton(buttonGridObject.transform, "Coyote Dash -", player => player.SetCoyoteDashWindow(0.05f));

        AddActionButton(buttonGridObject.transform, "Toggle Phone Ctrl", () => DevCheatService.Instance?.TogglePhoneWebController());
        AddActionButton(buttonGridObject.transform, "Phone QR / URL", () => DevCheatService.Instance?.ShowPhoneControllerUrl());

        AddActionButton(buttonGridObject.transform, "Cycle 75% Rule", () => DevCheatService.Instance?.CycleMinimumRestorationBadEndingRuleMode());
        AddActionButton(buttonGridObject.transform, "Force Act I", () => DevCheatService.Instance?.ForceStoryAct(GameStateManager.StoryAct.ActI));
        AddActionButton(buttonGridObject.transform, "Force Act II", () => DevCheatService.Instance?.ForceStoryAct(GameStateManager.StoryAct.ActII));
        AddActionButton(buttonGridObject.transform, "Force Act III", () => DevCheatService.Instance?.ForceStoryAct(GameStateManager.StoryAct.ActIII));
        AddActionButton(buttonGridObject.transform, "Force Good Ending", () => DevCheatService.Instance?.ForceEndingBranch(GameStateManager.EndingBranch.Good));
        AddActionButton(buttonGridObject.transform, "Force Bad Ending", () => DevCheatService.Instance?.ForceEndingBranch(GameStateManager.EndingBranch.Bad));
        AddActionButton(buttonGridObject.transform, "Reset Story State", () => DevCheatService.Instance?.ResetStoryProgression());
        AddActionButton(buttonGridObject.transform, "Final Boss Defeat +1", () => DevCheatService.Instance?.RecordFinalBossDefeatAttempt());
        AddActionButton(buttonGridObject.transform, "Reset Final Boss Defeats", () => DevCheatService.Instance?.ResetFinalBossDefeatAttempts());

        AddIslandButtons(buttonGridObject.transform);

        summaryText = CreateText(panelRoot.transform, "Summary", string.Empty, 20, FontStyle.Normal, TextAnchor.UpperLeft);
        summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        summaryText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform summaryRect = summaryText.rectTransform;
        summaryRect.anchorMin = new Vector2(0.72f, 0.16f);
        summaryRect.anchorMax = new Vector2(0.98f, 0.89f);
        summaryRect.offsetMin = Vector2.zero;
        summaryRect.offsetMax = Vector2.zero;
    }

    private void AddIslandButtons(Transform parent)
    {
        for (int i = 0; i < IslandThemeRegistry.ProgressionOrder.Count; i++)
        {
            string islandId = IslandThemeRegistry.ProgressionOrder[i];
            string captured = islandId;
            AddActionButton(parent, $"Go {captured}", () => DevCheatService.Instance?.SetActiveIsland(captured));
        }
    }

    private void RefreshSummary()
    {
        if (summaryText == null)
        {
            return;
        }

        DevCheatService service = DevCheatService.Instance;
        if (service == null)
        {
            summaryText.text = "DevCheatService unavailable.";
            return;
        }

        string toggles = $"Toggles -> Invincible:{service.GodModeInvincible} OneHit:{service.GodModeOneHitKill} Infinite:{service.GodModeInfiniteResources} Overlay:{service.ShowDebugOverlay}";
        summaryText.text = toggles + "\n\n" + service.BuildDebugSummary();
        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            summaryText.text += $"\n\nMovement -> AutoRun:{player.AutoRunEnabled} Hop:{player.AllowHop} AutoFace:{player.GetAutoFaceOnInteract()} CamPolish:{player.GetUseCameraFollowPolish()} Interact:{player.GetInteractKey()} Dash:{player.GetDashKey()} HopKey:{player.GetHopKey()}";
        }
    }

    private void AddMovementButton(Transform parent, string label, System.Action<IsometricPlayer> action)
    {
        AddActionButton(parent, label, () =>
        {
            IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
            if (player != null)
            {
                action?.Invoke(player);
            }
            RefreshSummary();
        });
    }

    private KeyCode CycleKey(KeyCode current)
    {
        if (remapKeys == null || remapKeys.Length == 0)
        {
            return current;
        }

        for (int i = 0; i < remapKeys.Length; i++)
        {
            if (remapKeys[i] == current)
            {
                return remapKeys[(i + 1) % remapKeys.Length];
            }
        }

        return remapKeys[0];
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.035f, 0.98f);
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle fontStyle, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = anchor;
        uiText.color = new Color(0.95f, 0.97f, 1f, 1f);
        return uiText;
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

    private void AddActionButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.22f, 0.34f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text buttonText = CreateText(buttonObject.transform, "Text", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void AddToggleButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        AddActionButton(parent, label, () =>
        {
            action?.Invoke();
            RefreshSummary();
        });
    }
}
