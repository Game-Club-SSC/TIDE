using UnityEngine;

/// <summary>
/// Bridges phone web controller input to the game's existing input system.
/// Receives commands from PhoneWebController and applies them to the IsometricPlayer
/// by setting input state that the player reads in its Update/FixedUpdate methods.
/// </summary>
[DisallowMultipleComponent]
public class PhoneInputBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Joystick Settings")]
    [SerializeField] private float joystickDeadZone = 0.15f;
    [SerializeField] private bool invertJoystickY = false;

    // Current input state (set by phone, read by IsometricPlayer)
    private float phoneInputH;
    private float phoneInputV;
    private bool phoneSprintHeld;
    private bool phoneInteractPressed;
    private bool phoneDashPressed;
    private bool phoneHopPressed;
    private bool phoneInputActive; // true when phone is providing input

    // One-shot flags (cleared after being consumed)
    private bool phoneInteractQueued;
    private bool phoneDashQueued;
    private bool phoneHopQueued;

    private IsometricPlayer cachedPlayer;
    private bool isPaired;

    public static PhoneInputBridge Instance { get; private set; }

    public bool IsPaired => isPaired;
    public bool IsPhoneInputActive => phoneInputActive;

    // Public accessors for IsometricPlayer to read
    public float PhoneInputH => phoneInputH;
    public float PhoneInputV => phoneInputV;
    public bool PhoneSprintHeld => phoneSprintHeld;
    public bool PhoneInteractPressed => phoneInteractPressed;
    public bool PhoneDashPressed => phoneDashPressed;
    public bool PhoneHopPressed => phoneHopPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SubscribeToServer();
    }

    private void OnDisable()
    {
        UnsubscribeFromServer();
    }

    private void SubscribeToServer()
    {
        PhoneWebController server = PhoneWebController.Instance;
        if (server != null)
        {
            server.OnCommandReceived += HandleCommand;
        }
    }

    private void UnsubscribeFromServer()
    {
        PhoneWebController server = PhoneWebController.Instance;
        if (server != null)
        {
            server.OnCommandReceived -= HandleCommand;
        }
    }

    /// <summary>
    /// Call this to re-subscribe to the server (e.g., if server was created after bridge).
    /// </summary>
    public void ReconnectToServer()
    {
        UnsubscribeFromServer();
        SubscribeToServer();
    }

    private void Update()
    {
        if (!isPaired)
        {
            return;
        }

        CachePlayer();

        // Clear one-shot flags each frame
        phoneInteractPressed = false;
        phoneDashPressed = false;
        phoneHopPressed = false;

        // Consume queued one-shot actions
        if (phoneInteractQueued)
        {
            phoneInteractPressed = true;
            phoneInteractQueued = false;
        }
        if (phoneDashQueued)
        {
            phoneDashPressed = true;
            phoneDashQueued = false;
        }
        if (phoneHopQueued)
        {
            phoneHopPressed = true;
            phoneHopQueued = false;
        }

        // Determine if phone input is active
        float mag = new Vector2(phoneInputH, phoneInputV).magnitude;
        phoneInputActive = mag > joystickDeadZone || phoneSprintHeld
            || phoneInteractPressed || phoneDashPressed || phoneHopPressed;
    }

    private void HandleCommand(PhoneInputCommand command)
    {
        if (command == null || !isPaired)
        {
            return;
        }

        switch (command.type)
        {
            case "joystick":
                HandleJoystick(command);
                break;

            case "button":
                HandleButton(command);
                break;

            case "action":
                HandleAction(command);
                break;
        }
    }

    private void HandleJoystick(PhoneInputCommand command)
    {
        float x = command.x;
        float y = invertJoystickY ? -command.y : command.y;

        // Apply dead zone
        float mag = Mathf.Sqrt(x * x + y * y);
        if (mag < joystickDeadZone)
        {
            phoneInputH = 0f;
            phoneInputV = 0f;
            return;
        }

        // Normalize after dead zone
        float normalizedMag = (mag - joystickDeadZone) / (1f - joystickDeadZone);
        normalizedMag = Mathf.Clamp01(normalizedMag);

        float angle = Mathf.Atan2(y, x);
        phoneInputH = Mathf.Cos(angle) * normalizedMag;
        phoneInputV = Mathf.Sin(angle) * normalizedMag;
    }

    private void HandleButton(PhoneInputCommand command)
    {
        switch (command.action)
        {
            case "interact":
                if (command.pressed)
                {
                    phoneInteractQueued = true;
                }
                break;

            case "dash":
                if (command.pressed)
                {
                    phoneDashQueued = true;
                }
                break;

            case "hop":
                if (command.pressed)
                {
                    phoneHopQueued = true;
                }
                break;

            case "sprint":
                phoneSprintHeld = command.pressed;
                break;
        }
    }

    private void HandleAction(PhoneInputCommand command)
    {
        // For future expansion - custom actions
        switch (command.action)
        {
            case "toggle_auto_run":
                if (cachedPlayer != null)
                {
                    cachedPlayer.ToggleAutoRunEnabled();
                }
                break;

            case "toggle_hop":
                if (cachedPlayer != null)
                {
                    cachedPlayer.ToggleAllowHop();
                }
                break;
        }
    }

    private void CachePlayer()
    {
        if (cachedPlayer == null && autoFindPlayer)
        {
            cachedPlayer = FindFirstObjectByType<IsometricPlayer>();
        }
    }

    /// <summary>
    /// Called by PhoneWebController when pairing is successful.
    /// </summary>
    public void SetPaired(bool paired)
    {
        isPaired = paired;
        if (!paired)
        {
            ResetInputState();
        }
    }

    /// <summary>
    /// Resets all phone input state to zero.
    /// </summary>
    public void ResetInputState()
    {
        phoneInputH = 0f;
        phoneInputV = 0f;
        phoneSprintHeld = false;
        phoneInteractPressed = false;
        phoneDashPressed = false;
        phoneHopPressed = false;
        phoneInteractQueued = false;
        phoneDashQueued = false;
        phoneHopQueued = false;
        phoneInputActive = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
