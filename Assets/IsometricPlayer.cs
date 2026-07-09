using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    private const float InputDeadZoneSqr = 0.0001f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 24f;
    [SerializeField] private float deceleration = 32f;
    [SerializeField] private bool autoRunEnabled;
    [SerializeField] private bool allowHop = true;
    [SerializeField] private float hopForce = 4.5f;
    [SerializeField] private float hopCooldown = 0.5f;
    [SerializeField] private KeyCode sprintHoldKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode sprintLockToggleKey = KeyCode.CapsLock;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode hopKey = KeyCode.Space;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.7f;
    [SerializeField] private float coyoteDashWindow = 0.15f;
    [SerializeField] private float groundNormalThreshold = 0.55f;
    [SerializeField] private float turnSmoothing = 12f;
    public bool canMove = true;

    [Header("Interaction Assist")]
    [SerializeField] private KeyCode interactKey = KeyCode.Return;
    [SerializeField] private float interactionAssistRadius = 4f;
    [SerializeField] private float interactionAssistSpeed = 2.5f;
    [SerializeField] private float interactionAssistTurnSmoothing = 18f;
    [SerializeField] private float interactionAssistStopDistance = 0.55f;
    [SerializeField] private bool autoFaceOnInteract = true;

    [Header("Camera Polish")]
    [SerializeField] private bool useCameraFollowPolish = true;
    [SerializeField] private float cameraLookAhead = 0.6f;
    [SerializeField] private float cameraLookAheadSmoothing = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip hopClip;
    [SerializeField] private float stepDistance = 1.8f;
    [SerializeField] private float footstepSilenceThreshold = 0.2f;

    [Header("UI")]
    [SerializeField] private bool addExplorationMapOnStart = true;
    [SerializeField] private bool addPlayerCustomizationUiOnStart = true;

    [Header("Visual")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Vector3 characterModelLocalOffset = new Vector3(0f, 0.55f, 0f);
    [SerializeField] private Vector3 characterModelLocalScale = new Vector3(1.1f, 1.1f, 1.1f);
    [SerializeField] private bool use2DSpriteVisual = true;

    private Rigidbody rb;
    private Camera cachedMainCamera;
    private Vector3 inputVector;
    private Vector3 currentPlanarVelocity;
    private bool isSprintLockEnabled;
    private Transform characterModelRoot;
    private string currentStyleId;
    private bool useManualColorOverride;
    private bool isDashing;
    private float dashEndsAt;
    private float nextDashAllowedAt;
    private float nextHopAllowedAt;
    private float lastGroundedTime;
    private float stepDistanceAccumulator;
    private Vector3 dashDirection;
    private IPlayerInteractionAssistTarget cachedAssistTarget;
    private float cachedAssistTargetRefreshAt;
    private AudioSource audioSource;
    private Vector3 cameraLookAheadCurrent;
    private CameraFollowPolishBridge cameraPolishBridge;

    public Color PlayerColor => playerColor;
    public string CurrentStyleId => currentStyleId;
    internal bool DebugIsDashing => isDashing;
    internal Vector3 DebugCurrentPlanarVelocity => currentPlanarVelocity;
    public bool AutoRunEnabled => autoRunEnabled;
    public bool AllowHop => allowHop;
    public bool Use2DSpriteVisual => use2DSpriteVisual;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        cachedMainCamera = Camera.main;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentPlanarVelocity = Vector3.zero;
        lastGroundedTime = Time.time;

        Ensure3DVisualSetup();
        ApplyCurrentStyleVisual();

        EnsureExplorationMapUi();
        EnsurePlayerCustomizationUi();
        EnsureCameraPolishBridge();
    }

    private void Ensure3DVisualSetup()
    {
        RemoveLegacySpriteVisuals();
        DisablePrimitiveRenderers();
        RebuildElementalPlayerModel();
    }

    private void DisablePrimitiveRenderers()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer is SpriteRenderer)
            {
                continue;
            }

            if (renderer.transform == transform)
            {
                if (use2DSpriteVisual)
                {
                    renderer.enabled = false;
                }
                continue;
            }

            string renderName = renderer.gameObject.name;
            if (renderName == ElementalCharacterFactory.PlayerModelRootName
                || renderName == ElementalCharacterFactory.PlayerSpriteRootName
                || renderName == ElementalCharacterFactory.ShadowQuadName
                || renderName == ElementalCharacterFactory.PlayerSpriteRendererName)
            {
                continue;
            }
        }
    }

    private void RemoveLegacySpriteVisuals()
    {
        Transform oldFallback = transform.Find("Player3DVisual");
        if (oldFallback != null)
        {
            Destroy(oldFallback.gameObject);
        }

        Transform overworldSprite = transform.Find("FuturisticPlayerVisual");
        if (overworldSprite != null)
        {
            Destroy(overworldSprite.gameObject);
        }

        Transform overworldShadow = transform.Find("FuturisticPlayerShadow");
        if (overworldShadow != null)
        {
            Destroy(overworldShadow.gameObject);
        }

        Transform battleSprite = transform.Find("BattleSpriteVisual");
        if (battleSprite != null)
        {
            Destroy(battleSprite.gameObject);
        }

        Transform battleShadow = transform.Find("BattleSpriteShadow");
        if (battleShadow != null)
        {
            Destroy(battleShadow.gameObject);
        }
    }

    private void ApplyCurrentStyleVisual()
    {
        if (string.IsNullOrEmpty(currentStyleId))
        {
            if (!string.IsNullOrEmpty(FuturisticSpriteLibrary.CurrentMainPlayerStyleId))
            {
                currentStyleId = FuturisticSpriteLibrary.CurrentMainPlayerStyleId;
            }

            CombatUnit.Element defaultElement = CombatUnit.Element.Earth;
            if (PartyManager.Instance != null)
            {
                CombatUnit.Element selectedElement = PartyManager.Instance.GetMainCharacterElement();
                if (selectedElement != CombatUnit.Element.None)
                {
                    defaultElement = selectedElement;
                }
            }

            if (string.IsNullOrEmpty(currentStyleId)
                || !FuturisticSpriteLibrary.TryGetPlayerStyle(currentStyleId, out FuturisticSpriteLibrary.PlayerStyleDefinition _))
            {
                currentStyleId = FuturisticSpriteLibrary.GetDefaultStyleIdForElement(defaultElement);
            }
        }

        if (FuturisticSpriteLibrary.TryGetPlayerStyle(currentStyleId, out FuturisticSpriteLibrary.PlayerStyleDefinition style))
        {
            playerColor = style.PrimaryColor;
        }

        FuturisticSpriteLibrary.SetCurrentMainPlayerStyle(currentStyleId);
        RebuildElementalPlayerModel();
    }

    private void EnsurePlayerCustomizationUi()
    {
        if (!addPlayerCustomizationUiOnStart)
        {
            return;
        }

        if (GetComponent<PlayerCustomizationUI>() != null)
        {
            return;
        }

        if (FindFirstObjectByType<PlayerCustomizationUI>() != null)
        {
            return;
        }

        gameObject.AddComponent<PlayerCustomizationUI>();
        Debug.Log("[IsometricPlayer] Added PlayerCustomizationUI.");
    }

    private void EnsureExplorationMapUi()
    {
        if (!addExplorationMapOnStart)
        {
            return;
        }

        if (GetComponent<ExplorationMapUI>() != null)
        {
            return;
        }

        if (FindFirstObjectByType<ExplorationMapUI>() != null)
        {
            return;
        }

        gameObject.AddComponent<ExplorationMapUI>();
        Debug.Log("[IsometricPlayer] Added ExplorationMapUI.");
    }

    private void ApplyPlayerColor()
    {
        RebuildElementalPlayerModel();
    }

    public void SetPlayerColor(Color newColor)
    {
        playerColor = newColor;
        useManualColorOverride = true;
        ApplyPlayerColor();
    }

    public void SetPlayerVisualStyle(string styleId)
    {
        if (string.IsNullOrEmpty(styleId))
        {
            return;
        }

        if (!FuturisticSpriteLibrary.TryGetPlayerStyle(styleId, out FuturisticSpriteLibrary.PlayerStyleDefinition style))
        {
            return;
        }

        currentStyleId = style.Id;
        playerColor = style.PrimaryColor;
        useManualColorOverride = false;
        FuturisticSpriteLibrary.SetCurrentMainPlayerStyle(currentStyleId);
        ApplyCurrentStyleVisual();

        Debug.Log($"[IsometricPlayer] Equipped {(use2DSpriteVisual ? "2D sprite" : "3D")} style: {currentStyleId}.");
    }

    private void RebuildElementalPlayerModel()
    {
        if (!FuturisticSpriteLibrary.TryGetPlayerStyle(currentStyleId, out FuturisticSpriteLibrary.PlayerStyleDefinition style))
        {
            string defaultStyle = FuturisticSpriteLibrary.GetDefaultStyleIdForElement(CombatUnit.Element.Earth);
            FuturisticSpriteLibrary.TryGetPlayerStyle(defaultStyle, out style);
            currentStyleId = style.Id;
        }

        Color primary = useManualColorOverride ? playerColor : style.PrimaryColor;
        Color accent = useManualColorOverride ? Color.Lerp(primary, Color.white, 0.32f) : style.AccentColor;
        Color glow = useManualColorOverride ? Color.Lerp(primary, Color.white, 0.48f) : style.GlowColor;

        playerColor = primary;

        if (use2DSpriteVisual)
        {
            characterModelRoot = ElementalCharacterFactory.BuildExplorationPlayerSprite(
                transform,
                style.Id,
                style.Element,
                primary,
                accent,
                glow,
                characterModelLocalOffset,
                characterModelLocalScale);
        }
        else
        {
            characterModelRoot = ElementalCharacterFactory.BuildExplorationPlayerModel(
                transform,
                style.Element,
                primary,
                accent,
                glow,
                characterModelLocalOffset,
                characterModelLocalScale);
        }

        if (characterModelRoot != null)
        {
            characterModelRoot.name = use2DSpriteVisual
                ? ElementalCharacterFactory.PlayerSpriteRootName
                : ElementalCharacterFactory.PlayerModelRootName;
        }

        ConfigureModelRendererVisibility();
    }

    private void ConfigureModelRendererVisibility()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool isModelRenderer = characterModelRoot != null && renderer.transform.IsChildOf(characterModelRoot);
            renderer.enabled = isModelRenderer;
        }
    }

    private void Update()
    {
        if (!canMove)
        {
            inputVector = Vector3.zero;
            currentPlanarVelocity = Vector3.zero;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            if (isDashing)
            {
                StopDash();
            }
            return;
        }

        if (Input.GetKeyDown(sprintLockToggleKey))
        {
            isSprintLockEnabled = !isSprintLockEnabled;
#if UNITY_EDITOR
            Debug.Log($"[IsometricPlayer] Sprint lock {(isSprintLockEnabled ? "enabled" : "disabled")}." );
#endif
        }

        // Read keyboard/gamepad input
        float keyboardH = Input.GetAxisRaw("Horizontal");
        float keyboardV = Input.GetAxisRaw("Vertical");

        // Read mobile touch input (highest priority on native mobile)
        float mobileH = 0f;
        float mobileV = 0f;
        MobileTouchInputManager mobileInput = MobileTouchInputManager.Instance;
        if (mobileInput != null && mobileInput.IsMobilePlatform)
        {
            mobileH = mobileInput.MoveH;
            mobileV = mobileInput.MoveV;
        }

        // Read phone input (if available and paired)
        float phoneH = 0f;
        float phoneV = 0f;
        PhoneInputBridge phoneBridge = PhoneInputBridge.Instance;
        if (phoneBridge != null && phoneBridge.IsPaired)
        {
            phoneH = phoneBridge.PhoneInputH;
            phoneV = phoneBridge.PhoneInputV;
        }

        // Read gamepad input (if connected)
        float gamepadH = 0f;
        float gamepadV = 0f;
        GamepadInputManager gamepad = GamepadInputManager.Instance;
        if (gamepad != null && gamepad.IsGamepadConnected)
        {
            gamepadH = gamepad.MoveH;
            gamepadV = gamepad.MoveV;
        }

        // Combine inputs: mobile > phone > gamepad > keyboard
        float h = mobileH != 0f ? mobileH : (phoneH != 0f ? phoneH : (gamepadH != 0f ? gamepadH : keyboardH));
        float v = mobileV != 0f ? mobileV : (phoneV != 0f ? phoneV : (gamepadV != 0f ? gamepadV : keyboardV));

        Vector3 rawInput = new Vector3(h, 0f, v);
        inputVector = rawInput.normalized;
        if (autoRunEnabled && inputVector.sqrMagnitude <= InputDeadZoneSqr)
        {
            inputVector = Vector3.forward;
        }

        // Handle hop from keyboard, phone, gamepad, or mobile touch
        bool hopRequested = Input.GetKeyDown(hopKey);
        if (phoneBridge != null && phoneBridge.IsPaired && phoneBridge.PhoneHopPressed)
        {
            hopRequested = true;
        }
        if (gamepad != null && gamepad.IsGamepadConnected && gamepad.HopPressed)
        {
            hopRequested = true;
        }
        if (mobileInput != null && mobileInput.IsMobilePlatform && mobileInput.HopPressed)
        {
            hopRequested = true;
        }
        if (allowHop && Time.time >= nextHopAllowedAt && hopRequested)
        {
            TryHop();
        }

        // Handle dash from keyboard, phone, gamepad, or mobile touch
        bool dashRequested = Input.GetKeyDown(dashKey);
        if (phoneBridge != null && phoneBridge.IsPaired && phoneBridge.PhoneDashPressed)
        {
            dashRequested = true;
        }
        if (gamepad != null && gamepad.IsGamepadConnected && gamepad.DashPressed)
        {
            dashRequested = true;
        }
        if (mobileInput != null && mobileInput.IsMobilePlatform && mobileInput.DashPressed)
        {
            dashRequested = true;
        }
        if (CanStartDash() && dashRequested)
        {
            StartDash();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!canMove)
        {
            currentPlanarVelocity = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        UpdateCameraPolish();

        if (Time.time >= dashEndsAt && isDashing)
        {
            StopDash();
        }

        Vector3 desiredPlanarVelocity = GetDesiredPlanarVelocity();
        currentPlanarVelocity = MovePlanarVelocityTowards(currentPlanarVelocity, desiredPlanarVelocity);
        rb.linearVelocity = new Vector3(currentPlanarVelocity.x, rb.linearVelocity.y, currentPlanarVelocity.z);
        HandleFootsteps(currentPlanarVelocity);

        Vector3 faceDirection = currentPlanarVelocity;
        bool usingAssistFacing = false;
        if (autoFaceOnInteract && faceDirection.sqrMagnitude <= InputDeadZoneSqr && cachedAssistTarget != null)
        {
            faceDirection = GetAssistFacingDirection(cachedAssistTarget);
            usingAssistFacing = true;
        }

        if (faceDirection.sqrMagnitude > InputDeadZoneSqr)
        {
            Quaternion targetRotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
            float smoothing = usingAssistFacing ? interactionAssistTurnSmoothing : turnSmoothing;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Max(1f, smoothing) * Time.fixedDeltaTime);
        }
    }

    private Vector3 GetDesiredPlanarVelocity()
    {
        if (isDashing)
        {
            return dashDirection * dashSpeed;
        }

        // Check phone, gamepad, and mobile touch interact buttons as well as keyboard
        bool interactKeyDown = Input.GetKey(interactKey);
        PhoneInputBridge phoneForInteract = PhoneInputBridge.Instance;
        if (phoneForInteract != null && phoneForInteract.IsPaired && phoneForInteract.PhoneInteractPressed)
        {
            interactKeyDown = true;
        }
        GamepadInputManager gamepadForInteract = GamepadInputManager.Instance;
        if (gamepadForInteract != null && gamepadForInteract.IsGamepadConnected && gamepadForInteract.InteractPressed)
        {
            interactKeyDown = true;
        }
        MobileTouchInputManager mobileForInteract = MobileTouchInputManager.Instance;
        if (mobileForInteract != null && mobileForInteract.IsMobilePlatform && mobileForInteract.InteractPressed)
        {
            interactKeyDown = true;
        }
        if (TryGetInteractionAssistTarget(interactKeyDown, out IPlayerInteractionAssistTarget assistTarget))
        {
            cachedAssistTarget = assistTarget;
            Vector3 assistDirection = GetAssistDirection(assistTarget);
            float assistDistance = assistDirection.magnitude;
            if (assistDistance > interactionAssistStopDistance)
            {
                return assistDirection.normalized * interactionAssistSpeed;
            }

            return Vector3.zero;
        }

        cachedAssistTarget = null;
        Camera activeCamera = cachedMainCamera;
        Vector3 forward = activeCamera != null ? activeCamera.transform.forward : Vector3.forward;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < InputDeadZoneSqr)
        {
            forward = Vector3.forward;
        }
        if (right.sqrMagnitude < InputDeadZoneSqr)
        {
            right = Vector3.right;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        if (moveDir.sqrMagnitude <= InputDeadZoneSqr)
        {
            return Vector3.zero;
        }

        bool sprintActive = Input.GetKey(sprintHoldKey) || isSprintLockEnabled;
        // Also check phone sprint toggle
        PhoneInputBridge phoneForSprint = PhoneInputBridge.Instance;
        if (phoneForSprint != null && phoneForSprint.IsPaired && phoneForSprint.PhoneSprintHeld)
        {
            sprintActive = true;
        }
        // Also check gamepad sprint
        GamepadInputManager gamepadForSprint = GamepadInputManager.Instance;
        if (gamepadForSprint != null && gamepadForSprint.IsGamepadConnected && gamepadForSprint.SprintHeld)
        {
            sprintActive = true;
        }
        // Also check mobile touch sprint toggle
        MobileTouchInputManager mobileForSprint = MobileTouchInputManager.Instance;
        if (mobileForSprint != null && mobileForSprint.IsMobilePlatform && mobileForSprint.SprintHeld)
        {
            sprintActive = true;
        }
        float speed = sprintActive ? sprintSpeed : walkSpeed;
        return moveDir.normalized * speed;
    }

    private Vector3 MovePlanarVelocityTowards(Vector3 currentVelocity, Vector3 targetVelocity)
    {
        float deltaTime = Time.fixedDeltaTime;
        float rate = targetVelocity.sqrMagnitude > InputDeadZoneSqr ? acceleration : deceleration;
        return Vector3.MoveTowards(currentVelocity, targetVelocity, Mathf.Max(0.01f, rate) * deltaTime);
    }

    private bool CanStartDash()
    {
        return Time.time >= nextDashAllowedAt && !isDashing && canMove && (Time.time - lastGroundedTime) <= coyoteDashWindow;
    }

    private void StartDash()
    {
        Vector3 dashSource = GetCurrentMoveDirection();
        if (dashSource.sqrMagnitude <= InputDeadZoneSqr)
        {
            dashSource = transform.forward;
            dashSource.y = 0f;
        }

        if (dashSource.sqrMagnitude <= InputDeadZoneSqr)
        {
            dashSource = Vector3.forward;
        }

        dashDirection = dashSource.normalized;
        isDashing = true;
        dashEndsAt = Time.time + Mathf.Max(0.01f, dashDuration);
        nextDashAllowedAt = Time.time + Mathf.Max(dashDuration, dashCooldown);
        cachedAssistTarget = null;
        PlayOneShot(dashClip);
#if UNITY_EDITOR
        Debug.Log($"[IsometricPlayer] Dash started toward {dashDirection}.");
#endif
    }

    private void StopDash()
    {
        isDashing = false;
    }

    private Vector3 GetCurrentMoveDirection()
    {
        Camera activeCamera = cachedMainCamera != null ? cachedMainCamera : Camera.main;
        if (activeCamera == null)
        {
            return new Vector3(inputVector.x, 0f, inputVector.z);
        }

        Vector3 forward = activeCamera.transform.forward;
        Vector3 right = activeCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude <= InputDeadZoneSqr)
        {
            forward = Vector3.forward;
        }
        if (right.sqrMagnitude <= InputDeadZoneSqr)
        {
            right = Vector3.right;
        }

        forward.Normalize();
        right.Normalize();
        return forward * inputVector.z + right * inputVector.x;
    }

    private bool TryGetInteractionAssistTarget(bool isInteractionRequested, out IPlayerInteractionAssistTarget assistTarget)
    {
        assistTarget = null;
        if (!isInteractionRequested)
        {
            return false;
        }

        if (Time.time < cachedAssistTargetRefreshAt
            && cachedAssistTarget != null
            && cachedAssistTarget.IsInteractionAssistActive())
        {
            assistTarget = cachedAssistTarget;
            return true;
        }

        cachedAssistTargetRefreshAt = Time.time + 0.1f;
        Collider[] overlaps = Physics.OverlapSphere(transform.position, Mathf.Max(0.1f, interactionAssistRadius));
        float bestDistance = float.MaxValue;
        IPlayerInteractionAssistTarget bestTarget = null;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            MonoBehaviour[] behaviours = overlap.GetComponentsInParent<MonoBehaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                IPlayerInteractionAssistTarget candidate = behaviours[j] as IPlayerInteractionAssistTarget;
                if (candidate == null || !candidate.IsInteractionAssistActive())
                {
                    continue;
                }

                Vector3 candidatePosition = candidate.GetInteractionAssistPosition();
                float distance = Vector3.Distance(transform.position, candidatePosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                }
            }
        }

        assistTarget = bestTarget;
        cachedAssistTarget = bestTarget;
        return bestTarget != null;
    }

    private Vector3 GetAssistDirection(IPlayerInteractionAssistTarget assistTarget)
    {
        if (assistTarget == null)
        {
            return Vector3.zero;
        }

        Vector3 targetPosition = assistTarget.GetInteractionAssistPosition();
        return targetPosition - transform.position;
    }

    private Vector3 GetAssistFacingDirection(IPlayerInteractionAssistTarget assistTarget)
    {
        Vector3 direction = GetAssistDirection(assistTarget);
        direction.y = 0f;
        return direction;
    }

    private void HandleFootsteps(Vector3 planarVelocity)
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        float planarSpeed = new Vector3(planarVelocity.x, 0f, planarVelocity.z).magnitude;
        if (planarSpeed <= footstepSilenceThreshold)
        {
            stepDistanceAccumulator = 0f;
            return;
        }

        stepDistanceAccumulator += planarSpeed * Time.fixedDeltaTime;
        if (stepDistanceAccumulator < Mathf.Max(0.1f, stepDistance))
        {
            return;
        }

        stepDistanceAccumulator = 0f;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        PlayOneShot(clip);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        if (collision.contactCount == 0)
        {
            return;
        }

        if (collision.contacts[0].normal.y > groundNormalThreshold)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void OnCollisionEnter(Collision collision) => OnCollisionStay(collision);

    private void TryHop()
    {
        if (rb == null)
        {
            return;
        }

        nextHopAllowedAt = Time.time + Mathf.Max(0.05f, hopCooldown);
        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, hopForce);
        rb.linearVelocity = velocity;
        lastGroundedTime = Time.time;
        PlayOneShot(hopClip);
    }

    private void UpdateCameraPolish()
    {
        if (!useCameraFollowPolish)
        {
            return;
        }

        if (cameraPolishBridge == null)
        {
            cameraPolishBridge = FindFirstObjectByType<CameraFollowPolishBridge>();
        }

        if (cameraPolishBridge == null)
        {
            return;
        }

        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        velocity.y = 0f;
        Vector3 desiredLookAhead = velocity.sqrMagnitude > InputDeadZoneSqr
            ? velocity.normalized * cameraLookAhead
            : Vector3.zero;
        cameraLookAheadCurrent = Vector3.Lerp(cameraLookAheadCurrent, desiredLookAhead, Mathf.Max(0.01f, cameraLookAheadSmoothing) * Time.fixedDeltaTime);
        cameraPolishBridge.SetLookAhead(cameraLookAheadCurrent);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void EnsureCameraPolishBridge()
    {
        if (FindFirstObjectByType<CameraFollowPolishBridge>() != null)
        {
            return;
        }

        GameObject bridge = new GameObject("CameraFollowPolishBridge");
        cameraPolishBridge = bridge.AddComponent<CameraFollowPolishBridge>();
    }

    public void SetAutoRunEnabled(bool enabled) => autoRunEnabled = enabled;
    public void ToggleAutoRunEnabled() => autoRunEnabled = !autoRunEnabled;
    public void SetAllowHop(bool enabled) => allowHop = enabled;
    public void ToggleAllowHop() => allowHop = !allowHop;
    public void ToggleSprintLock() => isSprintLockEnabled = !isSprintLockEnabled;
    public void SetUse2DSpriteVisual(bool enabled)
    {
        if (use2DSpriteVisual == enabled)
        {
            return;
        }
        use2DSpriteVisual = enabled;
        RebuildElementalPlayerModel();
    }
    public void ToggleUse2DSpriteVisual()
    {
        SetUse2DSpriteVisual(!use2DSpriteVisual);
    }
    public void SetAutoFaceOnInteract(bool enabled) => autoFaceOnInteract = enabled;
    public void ToggleAutoFaceOnInteract() => autoFaceOnInteract = !autoFaceOnInteract;
    public void SetUseCameraFollowPolish(bool enabled) => useCameraFollowPolish = enabled;
    public void ToggleUseCameraFollowPolish() => useCameraFollowPolish = !useCameraFollowPolish;
    public void SetInteractionAssistRadius(float value) => interactionAssistRadius = Mathf.Max(0.1f, value);
    public void SetDashCooldown(float value) => dashCooldown = Mathf.Max(0.01f, value);
    public void SetCoyoteDashWindow(float value) => coyoteDashWindow = Mathf.Max(0f, value);
    public void SetWalkSpeed(float value) => walkSpeed = Mathf.Max(0.1f, value);
    public void SetSprintSpeed(float value) => sprintSpeed = Mathf.Max(0.1f, value);
    public void SetDashSpeed(float value) => dashSpeed = Mathf.Max(0.1f, value);
    public void SetHopForce(float value) => hopForce = Mathf.Max(0.1f, value);
    public void SetInteractKey(KeyCode key) => interactKey = key;
    public void SetDashKey(KeyCode key) => dashKey = key;
    public void SetHopKey(KeyCode key) => hopKey = key;
    public KeyCode GetInteractKey() => interactKey;
    public KeyCode GetDashKey() => dashKey;
    public KeyCode GetHopKey() => hopKey;
    public bool GetAutoFaceOnInteract() => autoFaceOnInteract;
    public bool GetUseCameraFollowPolish() => useCameraFollowPolish;
}
