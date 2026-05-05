using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class CombatBoxInteractable : MonoBehaviour, IPlayerInteractionAssistTarget
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
    [SerializeField] private Color boxColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private string islandId = "";
    [SerializeField] private string encounterId = "";
    [SerializeField] private float restorationValue = 0.001f;

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;
    private Sprite runtimePromptSprite;
    private bool startupClearCheckComplete;
    private string generatedEncounterId;
    private bool loggedGeneratedEncounterId;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplyBoxColor();
        SetPromptVisible(false);

        if (TryDisableAsClearedEncounter())
        {
            return;
        }
    }

    private void Update()
    {
        if (!startupClearCheckComplete && TryDisableAsClearedEncounter())
        {
            return;
        }

        UpdatePromptFacing();

        GameStateManager gsm = GameStateManager.Instance;

        bool canInteract = playerInRange &&
                           gsm != null &&
                           gsm.CanEnterCombatScene();

        SetPromptVisible(canInteract);

        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        string trackedEncounterId = ResolveTrackingEncounterId();
        if (!string.IsNullOrEmpty(trackedEncounterId))
        {
            gsm.EnterCombatSceneFromExploration(
                ResolveTrackingIslandId(),
                trackedEncounterId,
                Mathf.Max(0.001f, restorationValue),
                transform.position,
                IsBossEncounter(trackedEncounterId));
        }
        else
        {
            gsm.EnterCombatScene();
        }
    }

    private bool ShouldDisableAsClearedEncounter()
    {
        string trackedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(trackedEncounterId) || IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        return IslandRestorationTracker.Instance.HasClearedEncounter(ResolveTrackingIslandId(), trackedEncounterId);
    }

    private bool TryDisableAsClearedEncounter()
    {
        string trackedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(trackedEncounterId))
        {
            startupClearCheckComplete = true;
            return false;
        }

        if (IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        startupClearCheckComplete = true;
        if (!ShouldDisableAsClearedEncounter())
        {
            return false;
        }

        gameObject.SetActive(false);
        return true;
    }

    private string ResolveTrackingEncounterId()
    {
        if (!string.IsNullOrEmpty(encounterId))
        {
            return encounterId;
        }

        if (string.IsNullOrEmpty(generatedEncounterId))
        {
            generatedEncounterId = GetGeneratedEncounterId();
        }

        if (!loggedGeneratedEncounterId)
        {
            Debug.LogWarning($"[CombatBoxInteractable] Missing encounterId on '{name}'. Using generated id '{generatedEncounterId}'.", this);
            loggedGeneratedEncounterId = true;
        }

        return generatedEncounterId;
    }

    private string ResolveTrackingIslandId()
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return IslandThemeRegistry.GetActiveIslandId();
        }

        return IslandThemeRegistry.ResolveIslandId(islandId);
    }

    private string GetGeneratedEncounterId()
    {
        string scopedIslandId = ResolveTrackingIslandId();
        string hierarchyPath = BuildHierarchyPath(transform);
        return $"auto_combat::{scopedIslandId}::{hierarchyPath}";
    }

    private static string BuildHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return "unknown";
        }

        System.Collections.Generic.List<string> pathParts = new System.Collections.Generic.List<string>();
        Transform walker = current;
        while (walker != null)
        {
            pathParts.Add($"{walker.name}[{walker.GetSiblingIndex()}]");
            walker = walker.parent;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private static bool IsBossEncounter(string trackedEncounterId)
    {
        if (string.IsNullOrEmpty(trackedEncounterId))
        {
            return false;
        }

        return trackedEncounterId.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
        solidCollider.size = triggerSize;
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
            Debug.LogWarning($"{nameof(CombatBoxInteractable)} on {name} could not find a prompt sprite. Assign one in the inspector or add a PNG named {DefaultPromptResourceName} under Assets/Resources.", this);
        }
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
        return playerInRange && gsm != null && gsm.CanEnterCombatScene();
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
