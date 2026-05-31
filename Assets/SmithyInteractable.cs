using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class SmithyInteractable : MonoBehaviour, IPlayerInteractionAssistTarget
{
    private const string DefaultPromptResourceName = "PuzzlePrompt";
    private const float DefaultPromptPixelsPerUnit = 360f;

    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.72f, 0.72f, 1f);
    [SerializeField] private Sprite promptSprite;
    [SerializeField] private Color promptTint = new Color(1f, 0.85f, 0.4f);

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3.25f, 2.25f, 3.25f);
    [SerializeField] private Color smithyColor = new Color(0.6f, 0.45f, 0.2f);

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;
    private Sprite runtimePromptSprite;
    private SmithyUI smithyUI;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplySmithyColor();
        SetPromptVisible(false);
    }

    private void Update()
    {
        UpdatePromptFacing();

        GameStateManager gsm = GameStateManager.Instance;
        bool canInteract = playerInRange
            && gsm != null
            && gsm.currentState == GameStateManager.GameState.Exploration
            && !gsm.IsTransitioning
            && smithyUI == null;

        SetPromptVisible(canInteract);
        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        PartySetupUI partySetupUi = FindFirstObjectByType<PartySetupUI>();
        if (partySetupUi != null && partySetupUi.IsOpen)
        {
            return;
        }

        OpenSmithy();
    }

    private void OpenSmithy()
    {
        if (smithyUI != null)
        {
            return;
        }

        GameObject uiObject = new GameObject("SmithyUI_Root");
        smithyUI = uiObject.AddComponent<SmithyUI>();
        smithyUI.Initialize(this);
    }

    public void OnSmithyClosed()
    {
        smithyUI = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);

        if (smithyUI != null)
        {
            smithyUI.CloseSmithy();
            smithyUI = null;
        }
    }

    private void ApplySmithyColor()
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = smithyColor;
        }
    }

    private void EnsureSolidCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
            {
                return;
            }
        }

        BoxCollider solidCollider = gameObject.AddComponent<BoxCollider>();
        solidCollider.isTrigger = false;
        solidCollider.size = triggerSize;
        solidCollider.center = Vector3.zero;
    }

    private void CreatePromptVisual()
    {
        promptRoot = new GameObject("SmithyPrompt");
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptLocalOffset;

        GameObject spriteObject = new GameObject("PromptImage");
        spriteObject.transform.SetParent(promptRoot.transform, false);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localRotation = Quaternion.identity;
        spriteObject.transform.localScale = promptScale;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetPromptSprite();
        spriteRenderer.color = promptTint;
        spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        spriteRenderer.sortingOrder = 10;
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 facingDirection = promptRoot.transform.position - mainCamera.transform.position;
        if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        promptRoot.transform.rotation = Quaternion.LookRotation(facingDirection.normalized, mainCamera.transform.up);
    }

    private Sprite GetPromptSprite()
    {
        if (promptSprite != null)
        {
            return promptSprite;
        }

        if (runtimePromptSprite != null)
        {
            return runtimePromptSprite;
        }

        Texture2D promptTexture = Resources.Load<Texture2D>(DefaultPromptResourceName);
        if (promptTexture == null)
        {
            return null;
        }

        runtimePromptSprite = Sprite.Create(
            promptTexture,
            new Rect(0f, 0f, promptTexture.width, promptTexture.height),
            new Vector2(0.5f, 0.5f),
            DefaultPromptPixelsPerUnit);
        runtimePromptSprite.name = $"{DefaultPromptResourceName}_Runtime";
        return runtimePromptSprite;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (promptRoot != null && promptRoot.activeSelf != isVisible)
        {
            promptRoot.SetActive(isVisible);
        }
    }

    private void OnDestroy()
    {
        if (runtimePromptSprite != null)
        {
            Destroy(runtimePromptSprite);
            runtimePromptSprite = null;
        }

        if (smithyUI != null)
        {
            smithyUI.CloseSmithy();
            smithyUI = null;
        }
    }

    public Vector3 GetInteractionAssistPosition()
    {
        return transform.position;
    }

    public float GetInteractionAssistRadius()
    {
        return Mathf.Max(triggerSize.x, triggerSize.z);
    }

    public bool IsInteractionAssistActive()
    {
        GameStateManager gsm = GameStateManager.Instance;
        return playerInRange
            && gsm != null
            && gsm.currentState == GameStateManager.GameState.Exploration
            && !gsm.IsTransitioning
            && smithyUI == null;
    }

    private static bool IsPlayerCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return collider.GetComponentInParent<IsometricPlayer>() != null;
    }
}
