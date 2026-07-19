using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Native mobile touch input manager. Provides a virtual joystick, action buttons,
/// and battle controls for direct phone/tablet play (not the companion web controller).
/// Auto-detects iOS/Android and builds all UI procedurally.
/// </summary>
[DisallowMultipleComponent]
public class MobileTouchInputManager : MonoBehaviour
{
    private const string CanvasName = "MobileTouchCanvas";
    private const float CanvasSortOrder = 950;
    [SerializeField] private float autoHideDelay = 3f;
    private const float FadeDuration = 0.4f;
    private const float AutoHideMinAlpha = 0.15f;
    private const float FullAlpha = 0.4f;

    public static MobileTouchInputManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableMobileControls = true;
    [SerializeField] private bool forceMobileInEditor;
    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private float joystickDeadZone = 0.15f;
    [SerializeField] private float sprintHoldTime = 0.3f;
    [SerializeField] private float joystickSmoothSpeed = 12f;

    // Movement input state
    private float moveH;
    private float moveV;
    private bool sprintHeld;
    private bool interactPressed;
    private bool dashPressed;
    private bool hopPressed;
    private bool menuPressed;

    // One-shot flags (consumed each frame)
    private bool interactQueued;
    private bool dashQueued;
    private bool hopQueued;
    private bool menuQueued;

    // Public read-only accessors for IsometricPlayer
    public float MoveH => moveH;
    public float MoveV => moveV;
    public bool SprintHeld => sprintHeld;
    public bool InteractPressed => interactPressed;
    public bool DashPressed => dashPressed;
    public bool HopPressed => hopPressed;
    public bool MenuPressed => menuPressed;
    public bool NavUpPressed { get; private set; }
    public bool NavDownPressed { get; private set; }

    // Platform detection
    public bool IsMobilePlatform { get; private set; }
    public bool AreControlsVisible { get; private set; } = true;

    private bool isActive;
    private float previousNavigationY;

    // --- Joystick ---
    private Canvas touchCanvas;
    private RectTransform joystickBase;
    private RectTransform joystickKnob;
    private bool isDraggingJoystick;
    private int joystickTouchId = -1;
    private Vector2 joystickBaseCenter;
    private float autoHideTimer;
    private CanvasGroup canvasGroup;
    private float currentAlpha;
    private float alphaTarget;

    // --- Action buttons (exploration) ---
    private GameObject actionButtonPanel;
    private RectTransform interactBtnRect;
    private RectTransform dashBtnRect;
    private RectTransform menuBtnRect;
    private RectTransform sprintBtnRect;

    // --- Battle touch controls ---
    private GameObject battleTouchPanel;
    private GameObject skillPopupPanel;

    // --- Cached references ---
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DetectPlatform();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (!IsMobilePlatform || !enableMobileControls)
        {
            return;
        }

        BuildUI();
        isActive = true;
        currentAlpha = FullAlpha;
        alphaTarget = FullAlpha;
        autoHideTimer = autoHideDelay;
    }

    private void Update()
    {
        if (!isActive)
        {
            ResetNavigationInput();
            return;
        }

        ConsumeOneShots();
        HandleTouches();
        HandleJoystickInput();
        UpdateNavigationInput();
        UpdateBattlePanelVisibility();
        UpdateAutoHide();
    }

    private void UpdateNavigationInput()
    {
        NavUpPressed = moveV > joystickDeadZone && previousNavigationY <= joystickDeadZone;
        NavDownPressed = moveV < -joystickDeadZone && previousNavigationY >= -joystickDeadZone;
        previousNavigationY = moveV;
    }

    private void ResetNavigationInput()
    {
        NavUpPressed = false;
        NavDownPressed = false;
        previousNavigationY = 0f;
    }

    // ====================================================================
    //  Platform detection
    // ====================================================================

    private void DetectPlatform()
    {
#if UNITY_IOS || UNITY_ANDROID
        IsMobilePlatform = true;
#else
        IsMobilePlatform = forceMobileInEditor;
#endif
    }

    // ====================================================================
    //  One-shot consumption
    // ====================================================================

    private void ConsumeOneShots()
    {
        interactPressed = false;
        dashPressed = false;
        hopPressed = false;
        menuPressed = false;

        if (interactQueued) { interactPressed = true; interactQueued = false; }
        if (dashQueued) { dashPressed = true; dashQueued = false; }
        if (hopQueued) { hopPressed = true; hopQueued = false; }
        if (menuQueued) { menuPressed = true; menuQueued = false; }
    }

    // ====================================================================
    //  Touch processing
    // ====================================================================

    private void HandleTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 pos = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchHeld(touch);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnded(touch);
                    break;
            }
        }
    }

    private void OnTouchBegan(Touch touch)
    {
        ResetAutoHide();

        // Check if touch landed on the joystick base area (left half of screen)
        if (touch.position.x < Screen.width * 0.5f && joystickBase != null)
        {
            if (IsInsideRectTransform(touch.position, joystickBase))
            {
                isDraggingJoystick = true;
                joystickTouchId = touch.fingerId;
                joystickBaseCenter = joystickBase.position;
                UpdateJoystickKnob(touch.position);
                return;
            }
        }

        // Check action buttons (right side)
        if (touch.position.x > Screen.width * 0.5f)
        {
            if (IsInsideRectTransform(touch.position, interactBtnRect))
            {
                interactQueued = true;
                return;
            }
            if (IsInsideRectTransform(touch.position, dashBtnRect))
            {
                dashQueued = true;
                return;
            }
            if (IsInsideRectTransform(touch.position, menuBtnRect))
            {
                menuQueued = true;
                return;
            }
            if (IsInsideRectTransform(touch.position, sprintBtnRect))
            {
                sprintHeld = !sprintHeld;
                return;
            }
        }

        // Battle buttons (bottom of screen)
        if (battleTouchPanel != null && battleTouchPanel.activeSelf)
        {
            HandleBattleTouch(touch);
        }
    }

    private void OnTouchHeld(Touch touch)
    {
        if (isDraggingJoystick && touch.fingerId == joystickTouchId)
        {
            UpdateJoystickKnob(touch.position);
        }
    }

    private void OnTouchEnded(Touch touch)
    {
        if (isDraggingJoystick && touch.fingerId == joystickTouchId)
        {
            isDraggingJoystick = false;
            joystickTouchId = -1;
            moveH = 0f;
            moveV = 0f;
            if (joystickKnob != null)
            {
                joystickKnob.anchoredPosition = Vector2.zero;
            }
        }
    }

    // ====================================================================
    //  Joystick
    // ====================================================================

    private void HandleJoystickInput()
    {
        if (!isDraggingJoystick || joystickTouchId < 0)
        {
            float decay = joystickSmoothSpeed * Time.deltaTime;
            moveH = Mathf.MoveTowards(moveH, 0f, decay);
            moveV = Mathf.MoveTowards(moveV, 0f, decay);
            return;
        }
    }

    private void UpdateJoystickKnob(Vector2 screenPos)
    {
        if (joystickKnob == null || joystickBase == null)
        {
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase, screenPos, touchCanvas.worldCamera, out localPoint);

        float clampedDistance = Mathf.Min(localPoint.magnitude, joystickRadius);
        Vector2 clamped = localPoint.normalized * clampedDistance;
        joystickKnob.anchoredPosition = clamped;

        // Normalize to -1..1
        float normalized = clampedDistance / joystickRadius;
        if (normalized < joystickDeadZone)
        {
            moveH = 0f;
            moveV = 0f;
        }
        else
        {
            float adjusted = (normalized - joystickDeadZone) / (1f - joystickDeadZone);
            moveH = localPoint.normalized.x * adjusted;
            moveV = localPoint.normalized.y * adjusted;
        }
    }

    // ====================================================================
    //  Battle touch
    // ====================================================================

    private void HandleBattleTouch(Touch touch)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return;
        }

        CombatUnit targetUnit = hit.collider.GetComponentInParent<CombatUnit>();
        if (targetUnit == null || !targetUnit.IsAlive || targetUnit.Type == CombatUnit.UnitType.Ally)
        {
            return;
        }

        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            return;
        }

        bm.TryAssignActionFromHud(CombatActionType.Attack, targetUnit);
    }

    private void UpdateBattlePanelVisibility()
    {
        if (battleTouchPanel == null)
        {
            return;
        }

        // Show battle controls when BattleManager is active in PlayerInput phase
        BattleManager bm = FindFirstObjectByType<BattleManager>();
        bool inBattle = bm != null && bm.CurrentPhase == BattlePhase.PlayerInput;
        battleTouchPanel.SetActive(inBattle);

        // Hide exploration buttons during battle
        if (actionButtonPanel != null)
        {
            actionButtonPanel.SetActive(!inBattle);
        }
    }

    // ====================================================================
    //  Auto-hide / fade
    // ====================================================================

    private void ResetAutoHide()
    {
        autoHideTimer = autoHideDelay;
        alphaTarget = FullAlpha;
    }

    private void UpdateAutoHide()
    {
        autoHideTimer -= Time.deltaTime;
        if (autoHideTimer <= 0f)
        {
            alphaTarget = AutoHideMinAlpha;
        }

        currentAlpha = Mathf.MoveTowards(currentAlpha, alphaTarget, (1f / FadeDuration) * Time.deltaTime);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = currentAlpha;
        }
    }

    // ====================================================================
    //  UI Construction (procedural, matches project pattern)
    // ====================================================================

    private void BuildUI()
    {
        EnsureEventSystem();
        BuildCanvas();
        BuildJoystick();
        BuildActionButtons();
        BuildBattleTouchPanel();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject(CanvasName, typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);

        touchCanvas = canvasObj.AddComponent<Canvas>();
        touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        touchCanvas.sortingOrder = (int)CanvasSortOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = FullAlpha;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    // --- Joystick ---

    private void BuildJoystick()
    {
        // Joystick base (translucent circle, left side)
        joystickBase = CreateRadialImage(
            touchCanvas.transform,
            "JoystickBase",
            new Vector2(0.12f, 0.35f),
            joystickRadius * 2f,
            new Color(1f, 1f, 1f, 0.25f));

        // Joystick knob (smaller inner circle)
        float knobSize = joystickRadius * 0.55f;
        joystickKnob = CreateRadialImage(
            joystickBase,
            "JoystickKnob",
            Vector2.zero,
            knobSize,
            new Color(1f, 1f, 1f, 0.5f));
    }

    private RectTransform CreateRadialImage(Transform parent, string name, Vector2 anchorPos, float size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;

        return rect;
    }

    // --- Exploration action buttons (right side) ---

    private void BuildActionButtons()
    {
        actionButtonPanel = new GameObject("ActionButtons", typeof(RectTransform));
        actionButtonPanel.transform.SetParent(touchCanvas.transform, false);

        RectTransform panelRect = actionButtonPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.82f, 0.25f);
        panelRect.anchorMax = new Vector2(0.98f, 0.65f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        interactBtnRect = CreateActionButton(panelRect.transform, "InteractBtn", "ACT", 0);
        dashBtnRect = CreateActionButton(panelRect.transform, "DashBtn", "DASH", 1);
        menuBtnRect = CreateActionButton(panelRect.transform, "MenuBtn", "MAP", 2);
        sprintBtnRect = CreateToggleButton(panelRect.transform, "SprintBtn", "RUN", 3);

        // Wire up click listeners
        AddButtonListener(interactBtnRect, () => interactQueued = true);
        AddButtonListener(dashBtnRect, () => dashQueued = true);
        AddButtonListener(menuBtnRect, () => menuQueued = true);
        AddButtonListener(sprintBtnRect, () => sprintHeld = !sprintHeld);
    }

    private RectTransform CreateActionButton(Transform parent, string name, string label, int index)
    {
        return CreateButtonStyled(parent, name, label, index,
            new Color(0.2f, 0.6f, 1f, FullAlpha));
    }

    private RectTransform CreateToggleButton(Transform parent, string name, string label, int index)
    {
        return CreateButtonStyled(parent, name, label, index,
            new Color(0.8f, 0.5f, 0.1f, FullAlpha));
    }

    private RectTransform CreateButtonStyled(Transform parent, string name, string label, int index, Color color)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        float yPos = -(index * 70f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, yPos - 60f);
        rect.offsetMax = new Vector2(0f, yPos);

        Image bg = btnObj.GetComponent<Image>();
        bg.color = color;

        // Button label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(btnObj.transform, false);

        Text text = labelObj.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return rect;
    }

    private void AddButtonListener(RectTransform btnRect, Action callback)
    {
        if (btnRect == null)
        {
            return;
        }

        Button btn = btnRect.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => callback?.Invoke());
        }
    }

    // --- Battle touch panel (bottom of screen) ---

    private void BuildBattleTouchPanel()
    {
        battleTouchPanel = new GameObject("BattleTouchPanel", typeof(RectTransform));
        battleTouchPanel.transform.SetParent(touchCanvas.transform, false);
        battleTouchPanel.SetActive(false);

        RectTransform panelRect = battleTouchPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.02f);
        panelRect.anchorMax = new Vector2(0.85f, 0.18f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Attack button
        CreateBattleButton(panelRect.transform, "AttackBtn", "ATK", 0,
            new Color(0.9f, 0.25f, 0.25f, 0.7f), OnBattleAttack);

        // Skill button
        CreateBattleButton(panelRect.transform, "SkillBtn", "SKL", 1,
            new Color(0.3f, 0.5f, 0.9f, 0.7f), OnBattleSkill);

        // Defend button
        CreateBattleButton(panelRect.transform, "DefendBtn", "DEF", 2,
            new Color(0.2f, 0.75f, 0.35f, 0.7f), OnBattleDefend);

        // Flee button
        CreateBattleButton(panelRect.transform, "FleeBtn", "FLY", 3,
            new Color(0.6f, 0.6f, 0.6f, 0.7f), OnBattleFlee);

        // Skill popup (hidden by default)
        BuildSkillPopup();
    }

    private void CreateBattleButton(Transform parent, string name, string label, int index, Color color, Action onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        float totalButtons = 4f;
        float buttonWidth = 1f / totalButtons;
        float gap = 0.005f;

        rect.anchorMin = new Vector2(index * buttonWidth + gap, 0f);
        rect.anchorMax = new Vector2((index + 1) * buttonWidth - gap, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = btnObj.GetComponent<Image>();
        bg.color = color;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(btnObj.transform, false);

        Text text = labelObj.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void BuildSkillPopup()
    {
        skillPopupPanel = new GameObject("SkillPopup", typeof(RectTransform), typeof(Image));
        skillPopupPanel.transform.SetParent(touchCanvas.transform, false);
        skillPopupPanel.SetActive(false);

        RectTransform rect = skillPopupPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.2f);
        rect.anchorMax = new Vector2(0.8f, 0.6f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = skillPopupPanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(skillPopupPanel.transform, false);
        Text titleText = titleObj.GetComponent<Text>();
        titleText.text = "SELECT SKILL";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 22;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyle.Bold;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.85f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Close button
        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(skillPopupPanel.transform, false);
        Image closeBg = closeObj.GetComponent<Image>();
        closeBg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.88f, 0.85f);
        closeRect.anchorMax = new Vector2(0.98f, 0.98f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;

        GameObject closeLabel = new GameObject("X", typeof(RectTransform), typeof(Text));
        closeLabel.transform.SetParent(closeObj.transform, false);
        Text closeText = closeLabel.GetComponent<Text>();
        closeText.text = "X";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 20;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;
        RectTransform closeLabelRect = closeLabel.GetComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;

        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => skillPopupPanel.SetActive(false));
    }

    /// <summary>
    /// Populates the skill popup with the current unit's skills.
    /// Called by the skill button handler when a skill selection is needed.
    /// </summary>
    private void PopulateSkillPopup(BattleManager bm)
    {
        if (bm == null || skillPopupPanel == null)
        {
            return;
        }

        // Clear existing skill buttons (keep title and close)
        for (int i = skillPopupPanel.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = skillPopupPanel.transform.GetChild(i);
            if (child != null && child.name != "Title" && child.name != "CloseBtn")
            {
                Destroy(child.gameObject);
            }
        }

        CombatUnit currentUnit = bm.GetCurrentInputUnit();
        if (currentUnit == null)
        {
            return;
        }

        IReadOnlyList<SkillData> skills = currentUnit.Skills;
        float yStart = 0.78f;
        float rowHeight = 0.14f;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (!bm.IsSkillSupportedForCurrentSlice(skill))
            {
                continue;
            }

            int capturedIndex = i;
            GameObject skillBtnObj = new GameObject("Skill_" + skill.skillName, typeof(RectTransform), typeof(Image), typeof(Button));
            skillBtnObj.transform.SetParent(skillPopupPanel.transform, false);

            RectTransform rect = skillBtnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, yStart - (capturedIndex * rowHeight));
            rect.anchorMax = new Vector2(0.95f, yStart - (capturedIndex * rowHeight) + rowHeight - 0.01f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = skillBtnObj.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(skillBtnObj.transform, false);
            Text text = labelObj.GetComponent<Text>();
            text.text = skill.skillName;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0f);
            labelRect.anchorMax = new Vector2(0.95f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Button btn = skillBtnObj.GetComponent<Button>();
            SkillData capturedSkill = skill;
            btn.onClick.AddListener(() =>
            {
                bm.SetPendingSkill(capturedSkill);
                bm.TryAssignActionFromHud(CombatActionType.Skill, null);
                skillPopupPanel.SetActive(false);
            });
        }

        skillPopupPanel.SetActive(true);
    }

    // ====================================================================
    //  Battle button callbacks
    // ====================================================================

    private void OnBattleAttack()
    {
        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            return;
        }

        // Delegate to BattleHud's attack flow (shows target selection)
        BattleHud hud = FindFirstObjectByType<BattleHud>();
        if (hud != null)
        {
            // Use reflection-free approach: set the pending action type and let the
            // existing HUD target selection handle it. Since BattleHud buttons are
            // already visible in the scene, we can invoke the same button callback.
            bm.TryAssignActionFromHud(CombatActionType.Attack, null);
        }
    }

    private void OnBattleSkill()
    {
        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            return;
        }

        CombatUnit currentUnit = bm.GetCurrentInputUnit();
        if (currentUnit == null)
        {
            return;
        }

        IReadOnlyList<SkillData> skills = currentUnit.Skills;
        if (skills.Count == 0)
        {
            return;
        }

        // If only one supported skill, assign directly
        int supportedCount = 0;
        SkillData onlySkill = null;
        for (int i = 0; i < skills.Count; i++)
        {
            if (bm.IsSkillSupportedForCurrentSlice(skills[i]))
            {
                supportedCount++;
                onlySkill = skills[i];
            }
        }

        if (supportedCount == 1 && onlySkill != null)
        {
            bm.SetPendingSkill(onlySkill);
            bm.TryAssignActionFromHud(CombatActionType.Skill, null);
            return;
        }

        // Multiple skills: show popup
        PopulateSkillPopup(bm);
    }

    private void OnBattleDefend()
    {
        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm != null)
        {
            bm.TryAssignActionFromHud(CombatActionType.Defend, null);
        }
    }

    private void OnBattleFlee()
    {
        BattleManager bm = FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            return;
        }

        bool accepted = bm.TryAttemptFleeFromMenu(out bool fledSuccessfully, out float fleeChance, out float fleeRoll);
        if (accepted && fledSuccessfully)
        {
            Debug.Log("[MobileTouchInput] Flee succeeded.");
        }
    }

    // ====================================================================
    //  Utility
    // ====================================================================

    private bool IsInsideRectTransform(Vector2 screenPos, RectTransform rect)
    {
        if (rect == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, touchCanvas != null ? touchCanvas.worldCamera : null);
    }

    // ====================================================================
    //  Public API
    // ====================================================================

    public void SetEnabled(bool enabled)
    {
        enableMobileControls = enabled;
        isActive = enabled && IsMobilePlatform;

        if (!isActive)
        {
            ResetNavigationInput();
        }

        if (!isActive && touchCanvas != null)
        {
            touchCanvas.gameObject.SetActive(false);
        }
        else if (isActive && touchCanvas != null)
        {
            touchCanvas.gameObject.SetActive(true);
        }
    }

    public void ShowControls()
    {
        ResetAutoHide();
        if (touchCanvas != null)
        {
            AreControlsVisible = true;
        }
    }

    public void HideControls()
    {
        alphaTarget = AutoHideMinAlpha;
    }
}
