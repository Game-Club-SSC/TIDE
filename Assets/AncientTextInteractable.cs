using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class AncientTextInteractable : MonoBehaviour, IPlayerInteractionAssistTarget
{
    private const string PromptResourceName = "PuzzlePrompt";
    private const float PromptPixelsPerUnit = 360f;

    [Header("Text Data")]
    [SerializeField] private AncientTextData textData;

    [Header("Prompt")]
    [SerializeField] private Vector3 promptOffset = new Vector3(1.2f, 1.25f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.68f, 0.68f, 1f);
    [SerializeField] private Color promptColor = Color.white;

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3f, 2.2f, 3f);
    [SerializeField] private KeyCode interactKey = KeyCode.Return;
    [SerializeField] private float interactEntryDelay = 0.35f;
    [SerializeField] private bool allowMovementAssist;

    [Header("Visual")]
    [SerializeField] private Color unreadColor = new Color(0.86f, 0.75f, 0.47f, 1f);
    [SerializeField] private Color readColor = new Color(0.62f, 0.62f, 0.68f, 1f);

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private Sprite runtimePromptSprite;
    private bool playerInRange;
    private int playerOverlapCount;
    private float playerEnteredRangeAt;
    private bool isRead;

    public void ConfigureRuntimeData(AncientTextData data)
    {
        textData = data;
        RegisterTextData();
        SyncReadState();
    }

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePrompt();
        SetPromptVisible(false);
    }

    private void Start()
    {
        RegisterTextData();
        SyncReadState();
    }

    private void Update()
    {
        UpdatePromptFacing();

        GameStateManager gsm = GameStateManager.Instance;
        bool canInteract = CanInteract(gsm);

        SetPromptVisible(canInteract);
        if (!canInteract)
        {
            return;
        }

        bool pressedMain = Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (!pressedMain)
        {
            return;
        }

        OpenTextLog(gsm);
    }

    private bool CanInteract(GameStateManager gsm)
    {
        return playerInRange
            && gsm != null
            && gsm.currentState == GameStateManager.GameState.Exploration
            && !gsm.IsTransitioning
            && Time.time - playerEnteredRangeAt >= Mathf.Max(0f, interactEntryDelay);
    }

    private void RegisterTextData()
    {
        if (textData == null)
        {
            Debug.LogWarning($"[AncientTextInteractable] Missing AncientTextData on '{name}'.");
            return;
        }

        if (!textData.IsValid())
        {
            Debug.LogWarning($"[AncientTextInteractable] Text data on '{name}' is invalid.");
            return;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterAncientText(textData.textId, textData.title, textData.body);
        }
    }

    private void SyncReadState()
    {
        if (GameStateManager.Instance != null && textData != null)
        {
            isRead = GameStateManager.Instance.IsAncientTextDiscovered(textData.textId);
        }

        ApplyVisualState();
    }

    private void OpenTextLog(GameStateManager gsm)
    {
        if (gsm == null || textData == null || !textData.IsValid())
        {
            return;
        }

        bool wasNew = gsm.DiscoverAncientText(textData.textId);
        isRead = true;
        ApplyVisualState();

        AncientTextLogUI logUi = FindFirstObjectByType<AncientTextLogUI>();
        if (logUi == null)
        {
            GameObject logObject = new GameObject("AncientTextLogUI");
            logUi = logObject.AddComponent<AncientTextLogUI>();
        }

        if (logUi != null)
        {
            logUi.ShowEntry(textData.textId, textData.title, textData.body, wasNew);
        }
    }

    private void ApplyVisualState()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.material.color = isRead ? readColor : unreadColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerOverlapCount++;
        if (!playerInRange)
        {
            playerEnteredRangeAt = Time.time;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        playerInRange = playerOverlapCount > 0;
        if (!playerInRange)
        {
            SetPromptVisible(false);
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

    private void CreatePrompt()
    {
        promptRoot = new GameObject("AncientTextPrompt");
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptOffset;

        GameObject spriteObject = new GameObject("PromptImage");
        spriteObject.transform.SetParent(promptRoot.transform, false);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localRotation = Quaternion.identity;
        spriteObject.transform.localScale = promptScale;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetPromptSprite();
        spriteRenderer.color = promptColor;
        spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        spriteRenderer.sortingOrder = 10;
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null || Camera.main == null)
        {
            return;
        }

        Vector3 facingDirection = promptRoot.transform.position - Camera.main.transform.position;
        if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        promptRoot.transform.rotation = Quaternion.LookRotation(facingDirection.normalized, Camera.main.transform.up);
    }

    private Sprite GetPromptSprite()
    {
        if (runtimePromptSprite != null)
        {
            return runtimePromptSprite;
        }

        Texture2D promptTexture = Resources.Load<Texture2D>(PromptResourceName);
        if (promptTexture == null)
        {
            return null;
        }

        runtimePromptSprite = Sprite.Create(
            promptTexture,
            new Rect(0f, 0f, promptTexture.width, promptTexture.height),
            new Vector2(0.5f, 0.5f),
            PromptPixelsPerUnit);
        runtimePromptSprite.name = $"{PromptResourceName}_Runtime";
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
    }

    private void OnDisable()
    {
        playerOverlapCount = 0;
        playerInRange = false;
        SetPromptVisible(false);
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
        return allowMovementAssist && CanInteract(gsm);
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
