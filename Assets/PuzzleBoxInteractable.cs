using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class PuzzleBoxInteractable : MonoBehaviour
{
    private const string DefaultPromptResourceName = "PuzzlePrompt";
    private const float DefaultPromptPixelsPerUnit = 360f;

    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.72f, 0.72f, 1f);
    [SerializeField] private Sprite promptSprite;
    [SerializeField] private Color promptTint = Color.white;

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3.25f, 2.25f, 3.25f);
    [SerializeField] private Color boxColor = new Color(1f, 0.45f, 0.12f);

    private Collider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;
    private Sprite runtimePromptSprite;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<Collider>();

        if (interactionTrigger is BoxCollider triggerBox)
        {
            triggerBox.isTrigger = true;
            triggerBox.size = triggerSize;
        }

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplyBoxColor();
        SetPromptVisible(false);
    }

    private void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.PuzzleSolved)
        {
            Destroy(gameObject);
        }
    }

    private bool isBeingDestroyed;

    private void Update()
    {
        if (isBeingDestroyed)
        {
            return;
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.PuzzleSolved)
        {
            isBeingDestroyed = true;
            Destroy(gameObject);
            return;
        }

        UpdatePromptFacing();

        bool canInteract = playerInRange &&
                           GameStateManager.Instance != null &&
                           GameStateManager.Instance.CanEnterPuzzle();

        SetPromptVisible(canInteract);

        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        Vector3 returnPosition = player != null ? player.transform.position : transform.position + Vector3.back * 2f;
        GameStateManager.Instance.EnterPuzzleScene(returnPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void ApplyBoxColor()
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = boxColor;
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
        solidCollider.size = Vector3.one;
        solidCollider.center = Vector3.zero;
    }

    private void CreatePromptVisual()
    {
        promptRoot = new GameObject("ExaminePrompt");
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

        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"{nameof(PuzzleBoxInteractable)} on {name} could not find a prompt sprite. Assign one in the inspector or add a PNG named {DefaultPromptResourceName} under Assets/Resources.", this);
        }
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
    }
}
