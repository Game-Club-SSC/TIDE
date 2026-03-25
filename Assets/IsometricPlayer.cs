using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private KeyCode sprintHoldKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode sprintLockToggleKey = KeyCode.CapsLock;
    public bool canMove = true;

    [Header("UI")]
    [SerializeField] private bool addExplorationMapOnStart = true;
    [SerializeField] private bool addPlayerCustomizationUiOnStart = true;

    private Rigidbody rb;
    private Camera cachedMainCamera;
    private Vector3 inputVector;
    private float currentSpeed;
    private bool isSprintLockEnabled;
    private SpriteRenderer futuristicSpriteRenderer;
    private SpriteRenderer shadowSpriteRenderer;
    private string currentStyleId;
    private Vector3 spriteBaseLocalPosition;
    private float walkCycleTimer;

    [Header("Visual")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private float spriteScale = 1.9f;
    [SerializeField] private Vector3 spriteLocalOffset = new Vector3(0f, 0.95f, 0f);
    [SerializeField] private bool hideLegacyMeshRenderer = true;
    [SerializeField] private bool enableFuturisticSpriteVisual = true;

    public Color PlayerColor => playerColor;
    public string CurrentStyleId => currentStyleId;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cachedMainCamera = Camera.main;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = walkSpeed;
        ApplyPlayerColor();
        EnsureFuturisticVisualSetup();
        ApplyCurrentStyleVisual();
        EnsureExplorationMapUi();
        EnsurePlayerCustomizationUi();
    }

    private void EnsureFuturisticVisualSetup()
    {
        if (!enableFuturisticSpriteVisual)
        {
            return;
        }

        if (hideLegacyMeshRenderer)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        if (futuristicSpriteRenderer == null)
        {
            Transform existing = transform.Find("FuturisticPlayerVisual");
            GameObject visualObject = existing != null ? existing.gameObject : new GameObject("FuturisticPlayerVisual");
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = spriteLocalOffset;
            visualObject.transform.localScale = new Vector3(spriteScale, spriteScale, 1f);

            futuristicSpriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            if (futuristicSpriteRenderer == null)
            {
                futuristicSpriteRenderer = visualObject.AddComponent<SpriteRenderer>();
            }

            spriteBaseLocalPosition = visualObject.transform.localPosition;

            futuristicSpriteRenderer.sortingOrder = 20;
            futuristicSpriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            futuristicSpriteRenderer.receiveShadows = false;
        }
        else
        {
            spriteBaseLocalPosition = futuristicSpriteRenderer.transform.localPosition;
        }

        if (shadowSpriteRenderer == null)
        {
            Transform existingShadow = transform.Find("FuturisticPlayerShadow");
            GameObject shadowObject = existingShadow != null ? existingShadow.gameObject : new GameObject("FuturisticPlayerShadow");
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            shadowObject.transform.localScale = new Vector3(1.05f, 0.45f, 1f);

            shadowSpriteRenderer = shadowObject.GetComponent<SpriteRenderer>();
            if (shadowSpriteRenderer == null)
            {
                shadowSpriteRenderer = shadowObject.AddComponent<SpriteRenderer>();
            }

            shadowSpriteRenderer.sprite = FuturisticSpriteLibrary.GetShadowSprite();
            shadowSpriteRenderer.color = new Color(0f, 0f, 0f, 0.28f);
            shadowSpriteRenderer.sortingOrder = 5;
            shadowSpriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowSpriteRenderer.receiveShadows = false;
        }
    }

    private void ApplyCurrentStyleVisual()
    {
        if (!enableFuturisticSpriteVisual)
        {
            return;
        }

        EnsureFuturisticVisualSetup();

        if (string.IsNullOrEmpty(currentStyleId))
        {
            CombatUnit.Element defaultElement = CombatUnit.Element.Earth;
            if (PartyManager.Instance != null)
            {
                CombatUnit.Element selectedElement = PartyManager.Instance.GetMainCharacterElement();
                if (selectedElement != CombatUnit.Element.None)
                {
                    defaultElement = selectedElement;
                }
            }

            currentStyleId = FuturisticSpriteLibrary.GetDefaultStyleIdForElement(defaultElement);
        }

        if (futuristicSpriteRenderer != null)
        {
            futuristicSpriteRenderer.sprite = FuturisticSpriteLibrary.GetPlayerOverworldSprite(currentStyleId);
            futuristicSpriteRenderer.color = Color.white;
        }

        FuturisticSpriteLibrary.SetCurrentMainPlayerStyle(currentStyleId);
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
        Renderer playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
        {
            playerRenderer.material.color = playerColor;
            Debug.Log($"[IsometricPlayer] Player color set to {playerColor}.");
        }

        if (futuristicSpriteRenderer != null)
        {
            futuristicSpriteRenderer.color = Color.white;
        }
    }

    public void SetPlayerColor(Color newColor)
    {
        playerColor = newColor;
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
        FuturisticSpriteLibrary.SetCurrentMainPlayerStyle(currentStyleId);
        ApplyCurrentStyleVisual();
        ApplyPlayerColor();

        Debug.Log($"[IsometricPlayer] Equipped futuristic style: {currentStyleId}.");
    }

    void Update()
    {
        if (!canMove)
        {
            inputVector = Vector3.zero;
            currentSpeed = 0f;
            return;
        }

        if (Input.GetKeyDown(sprintLockToggleKey))
        {
            isSprintLockEnabled = !isSprintLockEnabled;
            Debug.Log($"[IsometricPlayer] Sprint lock {(isSprintLockEnabled ? "enabled" : "disabled")}." );
        }

        // 1. Gather WASD Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputVector = new Vector3(h, 0f, v).normalized;

        // 2. Sprinting Check
        bool sprintHeld = Input.GetKey(sprintHoldKey);
        bool shouldSprint = sprintHeld || isSprintLockEnabled;
        if (shouldSprint)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        // 3. Camera-relative movement math
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        Camera activeCamera = cachedMainCamera;
        Vector3 forward = activeCamera != null ? activeCamera.transform.forward : Vector3.forward;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;

        // Flatten the camera vectors
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }

        forward.Normalize();
        right.Normalize();

        // 4. Apply Velocity (keeping Y velocity intact for normal gravity)
        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        Vector3 targetVelocity = moveDir * currentSpeed; 
        
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (futuristicSpriteRenderer != null)
        {
            Camera cam = cachedMainCamera != null ? cachedMainCamera : Camera.main;
            if (cam != null)
            {
                Vector3 toCamera = cam.transform.position - futuristicSpriteRenderer.transform.position;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    futuristicSpriteRenderer.transform.rotation = Quaternion.LookRotation(toCamera.normalized, cam.transform.up);
                }
            }

            float planarSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            bool isMoving = planarSpeed > 0.05f;
            float bobMagnitude = isMoving ? 0.06f : 0.02f;
            float bobFrequency = isMoving ? 10f : 3f;

            walkCycleTimer += Time.fixedDeltaTime * bobFrequency;
            float bob = Mathf.Sin(walkCycleTimer) * bobMagnitude;

            Vector3 adjustedPosition = spriteBaseLocalPosition + new Vector3(0f, bob, 0f);
            futuristicSpriteRenderer.transform.localPosition = adjustedPosition;

            if (shadowSpriteRenderer != null)
            {
                float flatten = isMoving ? Mathf.Abs(Mathf.Sin(walkCycleTimer * 0.5f)) : 0f;
                shadowSpriteRenderer.transform.localScale = new Vector3(
                    1.05f + flatten * 0.08f,
                    0.45f - flatten * 0.06f,
                    1f);
                shadowSpriteRenderer.color = new Color(0f, 0f, 0f, isMoving ? 0.34f : 0.26f);
            }
        }
    }
}
