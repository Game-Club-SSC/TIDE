using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private KeyCode sprintHoldKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode sprintLockToggleKey = KeyCode.CapsLock;
    [SerializeField] private float turnSmoothing = 12f;
    public bool canMove = true;

    [Header("UI")]
    [SerializeField] private bool addExplorationMapOnStart = true;
    [SerializeField] private bool addPlayerCustomizationUiOnStart = true;

    [Header("Visual")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private bool ensureFallback3DModel = true;
    [SerializeField] private Vector3 fallbackModelLocalPosition = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 fallbackModelLocalScale = new Vector3(0.8f, 1f, 0.8f);

    private Rigidbody rb;
    private Camera cachedMainCamera;
    private Vector3 inputVector;
    private float currentSpeed;
    private bool isSprintLockEnabled;
    private Renderer[] cachedRenderers = new Renderer[0];
    private string currentStyleId;

    public Color PlayerColor => playerColor;
    public string CurrentStyleId => currentStyleId;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cachedMainCamera = Camera.main;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = walkSpeed;

        Ensure3DVisualSetup();
        ApplyCurrentStyleVisual();
        ApplyPlayerColor();

        EnsureExplorationMapUi();
        EnsurePlayerCustomizationUi();
    }

    private void Ensure3DVisualSetup()
    {
        RemoveLegacySpriteVisuals();

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedRenderers.Length > 0 || !ensureFallback3DModel)
        {
            return;
        }

        GameObject fallbackBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallbackBody.name = "Player3DVisual";
        fallbackBody.transform.SetParent(transform, false);
        fallbackBody.transform.localPosition = fallbackModelLocalPosition;
        fallbackBody.transform.localScale = fallbackModelLocalScale;

        Collider fallbackCollider = fallbackBody.GetComponent<Collider>();
        if (fallbackCollider != null)
        {
            Destroy(fallbackCollider);
        }

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void RemoveLegacySpriteVisuals()
    {
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

        if (FuturisticSpriteLibrary.TryGetPlayerStyle(currentStyleId, out FuturisticSpriteLibrary.PlayerStyleDefinition style))
        {
            playerColor = style.PrimaryColor;
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
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].material.color = playerColor;
            }
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
        ApplyPlayerColor();

        Debug.Log($"[IsometricPlayer] Equipped 3D style: {currentStyleId}.");
    }

    private void Update()
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputVector = new Vector3(h, 0f, v).normalized;

        bool sprintHeld = Input.GetKey(sprintHoldKey);
        bool shouldSprint = sprintHeld || isSprintLockEnabled;
        currentSpeed = shouldSprint ? sprintSpeed : walkSpeed;
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        Camera activeCamera = cachedMainCamera;
        Vector3 forward = activeCamera != null ? activeCamera.transform.forward : Vector3.forward;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;

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

        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        Vector3 targetVelocity = moveDir * currentSpeed;

        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        Vector3 planarVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        if (planarVelocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Max(1f, turnSmoothing) * Time.fixedDeltaTime);
        }
    }
}
