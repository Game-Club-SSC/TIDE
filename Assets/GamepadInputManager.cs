using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Singleton that reads gamepad input via the Input System (with a legacy input fallback) and exposes
/// the same action channels used by IsometricPlayer and UI systems.
/// Auto-detects gamepad connection and provides button name strings
/// for HUD prompts (Xbox and PlayStation labels).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class GamepadInputManager : MonoBehaviour
{
    private const string HorizontalAxis = "Gamepad Horizontal";
    private const string VerticalAxis = "Gamepad Vertical";

    public static GamepadInputManager Instance { get; private set; }

    [Header("Settings")]
    [Range(0f, 0.9f)]
    [SerializeField] private float stickDeadZone = 0.2f;
    [Min(1)]
    [SerializeField] private int maxTargetCount = 8;

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
    public bool NavUpPressed { get; private set; }
    public bool NavDownPressed { get; private set; }

    // ----- State -----
    public bool IsGamepadConnected { get; private set; }
    public bool IsGamepadActive { get; private set; }

    private float previousNavigationY;
#if ENABLE_INPUT_SYSTEM
    private Gamepad currentGamepad;
#else
    private float nextConnectionCheck;
#endif

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
#if ENABLE_INPUT_SYSTEM
        currentGamepad = Gamepad.current;
        IsGamepadConnected = currentGamepad != null;
#else
        // GetJoystickNames allocates. Poll hot-plug state at a bounded rate and
        // inspect every slot: disconnected devices can leave empty first slots.
        if (Time.unscaledTime >= nextConnectionCheck)
        {
            nextConnectionCheck = Time.unscaledTime + 0.5f;
            string[] names = Input.GetJoystickNames();
            IsGamepadConnected = false;
            for (int i = 0; i < names.Length; i++)
            {
                if (!string.IsNullOrEmpty(names[i]))
                {
                    IsGamepadConnected = true;
                    break;
                }
            }
        }
#endif
        if (!IsGamepadConnected)
        {
            IsGamepadActive = false;
        }
        else if (!IsGamepadActive)
        {
            bool buttonUsed = false;
            for (int i = 0; i <= 7; i++)
            {
                buttonUsed |= ReadButton(i, false);
            }
            IsGamepadActive = buttonUsed || Mathf.Abs(ReadStick(true)) > stickDeadZone
                || Mathf.Abs(ReadStick(false)) > stickDeadZone;
        }

#if UNITY_EDITOR
        if (wasConnected != IsGamepadConnected)
        {
            Debug.Log(IsGamepadConnected
                ? "[GamepadInputManager] Gamepad connected."
                : "[GamepadInputManager] Gamepad disconnected.");
        }
#endif
    }

    private float ReadStick(bool horizontal)
    {
#if ENABLE_INPUT_SYSTEM
        if (currentGamepad == null) return 0f;
        Vector2 stick = currentGamepad.leftStick.ReadValue();
        return horizontal ? stick.x : stick.y;
#else
        return Input.GetAxisRaw(horizontal ? HorizontalAxis : VerticalAxis);
#endif
    }

    private float ReadDpad(bool horizontal)
    {
#if ENABLE_INPUT_SYSTEM
        if (currentGamepad == null) return 0f;
        Vector2 dpad = currentGamepad.dpad.ReadValue();
        return horizontal ? dpad.x : dpad.y;
#else
        return Input.GetAxisRaw(horizontal ? "DPad Horizontal" : "DPad Vertical");
#endif
    }

    private bool ReadButton(int index, bool pressedThisFrame)
    {
#if ENABLE_INPUT_SYSTEM
        if (currentGamepad == null) return false;
        UnityEngine.InputSystem.Controls.ButtonControl button;
        switch (index)
        {
            case 0: button = currentGamepad.buttonSouth; break;
            case 1: button = currentGamepad.buttonEast; break;
            case 2: button = currentGamepad.buttonWest; break;
            case 3: button = currentGamepad.buttonNorth; break;
            case 4: button = currentGamepad.leftShoulder; break;
            case 5: button = currentGamepad.rightShoulder; break;
            case 6: button = currentGamepad.leftStickButton; break;
            case 7: button = currentGamepad.startButton; break;
            default: return false;
        }
        return pressedThisFrame ? button.wasPressedThisFrame : button.isPressed;
#else
        // GetButton takes an InputManager axis name, not a key name. Joystick
        // KeyCodes work without defining a separate virtual axis per button.
        KeyCode key = (KeyCode)((int)KeyCode.JoystickButton0 + index);
        return pressedThisFrame ? Input.GetKeyDown(key) : Input.GetKey(key);
#endif
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

        float rawH = ReadStick(true);
        float rawV = ReadStick(false);

        MoveH = ApplyDeadZone(rawH);
        MoveV = ApplyDeadZone(rawV);

        // Left stick click (L3). button 6 is L3; button 10 is R3. Fire3
        // resolves to "joystick button 2" (X/Square, already Dash) and
        // button 4 is LB (already Swap), so aliasing either would fire sprint
        // alongside those actions.
        SprintHeld = ReadButton(6, false);
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

        // One-shot button presses sampled before UI and player consumers update.
        InteractPressed = ReadButton(0, true);   // A / Cross
        DashPressed = ReadButton(2, true);       // X / Square
        HopPressed = ReadButton(1, true);        // B / Circle
        MenuPressed = ReadButton(7, true);       // Start
        CancelPressed = ReadButton(1, true);     // B / Circle (shared with Hop, context-dependent)
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

        AttackPressed = ReadButton(0, true);     // A / Cross
        SkillPressed = ReadButton(2, true);      // X / Square
        DefendPressed = ReadButton(3, true);     // Y / Triangle
        FleePressed = ReadButton(1, true);       // B / Circle
        SwapPressed = ReadButton(4, true)        // LB
                      || ReadButton(5, true);    // RB
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
            NavUpPressed = false;
            NavDownPressed = false;
            previousNavigationY = 0f;
            return;
        }

        TabLeftPressed = ReadButton(4, true);    // LB
        TabRightPressed = ReadButton(5, true);   // RB
        ConfirmPressed = ReadButton(0, true);    // A / Cross
        BackPressed = ReadButton(1, true);       // B / Circle

        float dpadY = ReadDpad(false);
        float navigationY = Mathf.Abs(dpadY) > stickDeadZone ? dpadY : ApplyDeadZone(ReadStick(false));
        NavUpPressed = navigationY > 0f && previousNavigationY <= 0f;
        NavDownPressed = navigationY < 0f && previousNavigationY >= 0f;
        previousNavigationY = navigationY;
    }

    // ----- Target Selection -----
    // Left/Right on D-Pad or Left Stick to cycle targets

    private bool lastDpadHorizontalActive;

    private void ReadTargetSelection()
    {
        if (!IsGamepadConnected)
        {
            lastDpadHorizontalActive = false;
            SelectedTargetIndex = 0;
            return;
        }

        // D-Pad horizontal (rising edge only; holding the d-pad would
        // otherwise cycle the index ~60 times per second)
        float dpadH = ReadDpad(true);
        if (Mathf.Abs(dpadH) <= stickDeadZone)
        {
            dpadH = ReadStick(true);
        }
        bool dpadActive = Mathf.Abs(dpadH) > stickDeadZone;
        bool dpadPressed = dpadActive && !lastDpadHorizontalActive;
        lastDpadHorizontalActive = dpadActive;

        if (dpadPressed)
        {
            int maxTargetCount = Mathf.Max(1, this.maxTargetCount);
            if (dpadH > 0f)
            {
                SelectedTargetIndex = (SelectedTargetIndex + 1) % maxTargetCount;
            }
            else
            {
                SelectedTargetIndex = (SelectedTargetIndex + maxTargetCount - 1) % maxTargetCount;
            }
        }
    }

    // ----- Utility -----

    private float ApplyDeadZone(float value)
    {
        float deadZone = Mathf.Clamp(stickDeadZone, 0f, 0.9f);
        if (Mathf.Abs(value) <= deadZone)
        {
            return 0f;
        }

        // Rescale so the dead zone edge maps to 0 and full tilt maps to 1
        float sign = Mathf.Sign(value);
        float range = 1f - deadZone;
        return sign * Mathf.Clamp01((Mathf.Abs(value) - deadZone) / range);
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
