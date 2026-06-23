using UnityEngine;

/// <summary>
/// Singleton that reads gamepad input via UnityEngine.Input and exposes
/// the same action channels used by IsometricPlayer and UI systems.
/// Auto-detects gamepad connection and provides button name strings
/// for HUD prompts (Xbox and PlayStation labels).
/// </summary>
[DisallowMultipleComponent]
public class GamepadInputManager : MonoBehaviour
{
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string MouseScrollAxis = "Mouse ScrollWheel";

    public static GamepadInputManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float stickDeadZone = 0.2f;
    [SerializeField] private float triggerThreshold = 0.5f;

    // ----- Movement -----
    public float MoveH { get; private set; }
    public float MoveV { get; private set; }
    public bool SprintHeld { get; private set; }

    // ----- Exploration actions -----
    public bool InteractPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool HopPressed { get; private set; }
    public bool MenuPressed { get; private set; }
    public bool CancelPressed { get; private set; }

    // ----- Battle input -----
    public bool AttackPressed { get; private set; }
    public bool SkillPressed { get; private set; }
    public bool DefendPressed { get; private set; }
    public bool FleePressed { get; private set; }
    public bool SwapPressed { get; private set; }
    public int SelectedTargetIndex { get; private set; }

    // ----- UI navigation -----
    public bool TabLeftPressed { get; private set; }
    public bool TabRightPressed { get; private set; }
    public bool ConfirmPressed { get; private set; }
    public bool BackPressed { get; private set; }

    // ----- State -----
    public bool IsGamepadConnected { get; private set; }
    public bool IsGamepadActive { get; private set; }

    private bool previousAnyButton;

    // ----- Lifecycle -----

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GamepadInputManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GamepadInputManager] Initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        DetectGamepadConnection();
        ReadMovement();
        ReadExplorationActions();
        ReadBattleActions();
        ReadUINavigation();
        ReadTargetSelection();
    }

    // ----- Gamepad Detection -----

    private void DetectGamepadConnection()
    {
        bool wasConnected = IsGamepadConnected;
        IsGamepadConnected = Input.GetJoystickNames().Length > 0 && !string.IsNullOrEmpty(Input.GetJoystickNames()[0]);

        if (IsGamepadConnected && !wasConnected)
        {
            Debug.Log("[GamepadInputManager] Gamepad connected.");
        }
        else if (!IsGamepadConnected && wasConnected)
        {
            Debug.Log("[GamepadInputManager] Gamepad disconnected.");
            IsGamepadActive = false;
        }

        // A gamepad is "active" if it is connected and any button/axis has been pressed
        if (IsGamepadConnected && !IsGamepadActive)
        {
            if (Input.anyButton || Mathf.Abs(Input.GetAxisRaw(HorizontalAxis)) > stickDeadZone || Mathf.Abs(Input.GetAxisRaw(VerticalAxis)) > stickDeadZone)
            {
                IsGamepadActive = true;
                Debug.Log("[GamepadInputManager] Gamepad input detected -- switching to gamepad prompts.");
            }
        }
    }

    // ----- Movement -----

    private void ReadMovement()
    {
        if (!IsGamepadConnected)
        {
            MoveH = 0f;
            MoveV = 0f;
            SprintHeld = false;
            return;
        }

        float rawH = Input.GetAxisRaw(HorizontalAxis);
        float rawV = Input.GetAxisRaw(VerticalAxis);

        MoveH = ApplyDeadZone(rawH);
        MoveV = ApplyDeadZone(rawV);

        // Left stick click or left trigger for sprint
        SprintHeld = Input.GetButton("joystick button 8") // Left stick click
                     || Input.GetAxisRaw("Fire3") > triggerThreshold; // Left trigger (alt mapping)
    }

    // ----- Exploration Actions -----
    // A/Cross  = Interact
    // X/Square = Dash
    // B/Circle = Hop
    // Start    = Menu
    // B/Circle (when menu open) = Cancel

    private void ReadExplorationActions()
    {
        if (!IsGamepadConnected)
        {
            InteractPressed = false;
            DashPressed = false;
            HopPressed = false;
            MenuPressed = false;
            CancelPressed = false;
            return;
        }

        // One-shot button presses (GetButtonDown for single-frame detection)
        InteractPressed = Input.GetButtonDown("joystick button 0");   // A / Cross
        DashPressed = Input.GetButtonDown("joystick button 2");       // X / Square
        HopPressed = Input.GetButtonDown("joystick button 1");        // B / Circle
        MenuPressed = Input.GetButtonDown("joystick button 7");       // Start
        CancelPressed = Input.GetButtonDown("joystick button 1");     // B / Circle (shared with Hop, context-dependent)
    }

    // ----- Battle Actions -----
    // A/Cross  = Attack
    // X/Square = Skill
    // Y/Triangle = Defend
    // B/Circle = Flee
    // LB/RB    = Swap

    private void ReadBattleActions()
    {
        if (!IsGamepadConnected)
        {
            AttackPressed = false;
            SkillPressed = false;
            DefendPressed = false;
            FleePressed = false;
            SwapPressed = false;
            return;
        }

        AttackPressed = Input.GetButtonDown("joystick button 0");     // A / Cross
        SkillPressed = Input.GetButtonDown("joystick button 2");      // X / Square
        DefendPressed = Input.GetButtonDown("joystick button 3");     // Y / Triangle
        FleePressed = Input.GetButtonDown("joystick button 1");       // B / Circle
        SwapPressed = Input.GetButtonDown("joystick button 4")        // LB
                      || Input.GetButtonDown("joystick button 5");    // RB
    }

    // ----- UI Navigation -----
    // LB = TabLeft
    // RB = TabRight
    // A  = Confirm
    // B  = Back

    private void ReadUINavigation()
    {
        if (!IsGamepadConnected)
        {
            TabLeftPressed = false;
            TabRightPressed = false;
            ConfirmPressed = false;
            BackPressed = false;
            return;
        }

        TabLeftPressed = Input.GetButtonDown("joystick button 4");    // LB
        TabRightPressed = Input.GetButtonDown("joystick button 5");   // RB
        ConfirmPressed = Input.GetButtonDown("joystick button 0");    // A / Cross
        BackPressed = Input.GetButtonDown("joystick button 1");       // B / Circle
    }

    // ----- Target Selection -----
    // Left/Right on D-Pad or Left Stick to cycle targets

    private void ReadTargetSelection()
    {
        if (!IsGamepadConnected)
        {
            return;
        }

        // D-Pad horizontal
        float dpadH = Input.GetAxisRaw("DPad Horizontal");
        if (Mathf.Abs(dpadH) > stickDeadZone)
        {
            if (dpadH > 0f)
            {
                SelectedTargetIndex++;
            }
            else if (dpadH < 0f)
            {
                SelectedTargetIndex--;
            }

            if (SelectedTargetIndex < 0)
            {
                SelectedTargetIndex = 0;
            }
        }
    }

    // ----- Utility -----

    private float ApplyDeadZone(float value)
    {
        if (Mathf.Abs(value) < stickDeadZone)
        {
            return 0f;
        }

        // Rescale so the dead zone edge maps to 0 and full tilt maps to 1
        float sign = Mathf.Sign(value);
        return sign * (Mathf.Abs(value) - stickDeadZone) / (1f - stickDeadZone);
    }

    public void ResetTargetIndex()
    {
        SelectedTargetIndex = 0;
    }

    // ----- Button Name Display -----

    /// <summary>
    /// Returns a human-readable button name for the given action.
    /// Shows Xbox labels by default; pass isPlayStation=true for Cross/Circle/Square/Triangle.
    /// </summary>
    public static string GetButtonName(GamepadAction action, bool isPlayStation = false)
    {
        switch (action)
        {
            // Face buttons
            case GamepadAction.Confirm:
            case GamepadAction.Interact:
            case GamepadAction.Attack:
                return isPlayStation ? "Cross" : "A";

            case GamepadAction.Cancel:
            case GamepadAction.Back:
            case GamepadAction.Hop:
            case GamepadAction.Flee:
                return isPlayStation ? "Circle" : "B";

            case GamepadAction.Dash:
            case GamepadAction.Skill:
                return isPlayStation ? "Square" : "X";

            case GamepadAction.Defend:
                return isPlayStation ? "Triangle" : "Y";

            // Triggers / bumpers
            case GamepadAction.Swap:
            case GamepadAction.TabLeft:
                return "LB";

            case GamepadAction.TabRight:
                return "RB";

            case GamepadAction.Sprint:
                return "LS";

            // System
            case GamepadAction.Menu:
                return "Start";

            // D-Pad
            case GamepadAction.TargetLeft:
                return "DPad Left";

            case GamepadAction.TargetRight:
                return "DPad Right";

            default:
                return "???";
        }
    }

    /// <summary>
    /// Returns an icon-friendly label such as "A_Btn", "Cross_Btn", etc.
    /// Use this when the UI has sprite assets per button name.
    /// </summary>
    public static string GetButtonIconName(GamepadAction action, bool isPlayStation = false)
    {
        return GetButtonName(action, isPlayStation) + "_Btn";
    }
}

// ----- Enums -----

public enum GamepadAction
{
    Confirm,
    Cancel,
    Back,
    Interact,
    Dash,
    Hop,
    Menu,
    Sprint,
    Attack,
    Skill,
    Defend,
    Flee,
    Swap,
    TabLeft,
    TabRight,
    TargetLeft,
    TargetRight
}
