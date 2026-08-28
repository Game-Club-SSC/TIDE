using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Interactable service station placed in the hub scene. Reuses existing
/// systems: PartySetupUI (party management), SmithyInteractable/SmithyUI
/// (smithy + vendor economy), and DialogueSystem (narrative/NPC space).
/// Follows the same trigger + prompt + interact-key pattern as
/// <see cref="SmithyInteractable"/> and <see cref="IslandBoatInteractable"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class HubServiceInteractable : MonoBehaviour, IPlayerInteractionAssistTarget
{
    public enum ServiceType
    {
        Party,
        Smithy,
        Vendor,
        Narrative
    }

    private const string DefaultPromptResourceName = "PuzzlePrompt";
    private const float DefaultPromptPixelsPerUnit = 360f;

    [Header("Service")]
    [SerializeField] private ServiceType serviceType = ServiceType.Party;

    [Header("Narrative (Narrative service only)")]
    [Tooltip("Speaker name shown above the narrative dialogue lines.")]
    [SerializeField] private string narratorName = "Harbormaster Wren";
    [Tooltip("Dialogue lines shown when the narrative station is used.")]
    [SerializeField] private string[] narrativeLines = { "The harbor is quiet tonight. The boats are ready." };

    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.72f, 0.72f, 1f);
    [SerializeField] private Sprite promptSprite;
    [SerializeField] private Color promptTint = Color.white;

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3.25f, 2.25f, 3.25f);
    [SerializeField] private KeyCode interactKey = KeyCode.Return;

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;
    private Sprite runtimePromptSprite;
    private PartySetupUI partySetupUI;

    public ServiceType Type => serviceType;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePromptVisual();
        SetPromptVisible(false);
    }

    private void Update()
    {
        UpdatePromptFacing();

        GameStateManager gameStateManager = GameStateManager.Instance;
        bool canInteract = playerInRange
            && gameStateManager != null
            && gameStateManager.currentState == GameStateManager.GameState.Exploration
            && !gameStateManager.IsTransitioning
            && !IsServiceUiOpen();

        SetPromptVisible(canInteract);
        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(interactKey) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        OpenService();
    }

    private bool IsServiceUiOpen()
    {
        if (partySetupUI != null)
        {
            return partySetupUI.IsOpen;
        }

        SmithyInteractable smithy = GetComponentInChildren<SmithyInteractable>();
        if (smithy != null)
        {
            return smithy.IsServiceOpen;
        }

        return false;
    }

    private void OpenService()
    {
        switch (serviceType)
        {
            case ServiceType.Party:
                OpenPartyService();
                break;

            case ServiceType.Smithy:
            case ServiceType.Vendor:
                OpenSmithyService();
                break;

            case ServiceType.Narrative:
                OpenNarrativeService();
                break;
        }
    }

    private void OpenPartyService()
    {
        if (partySetupUI == null)
        {
            GameObject uiObject = new GameObject("HubPartySetupUI");
            uiObject.transform.SetParent(transform, false);
            partySetupUI = uiObject.AddComponent<PartySetupUI>();
        }

        if (partySetupUI != null && !partySetupUI.IsOpen)
        {
            partySetupUI.OpenMenu();
        }
    }

    private void OpenSmithyService()
    {
        SmithyInteractable smithy = GetComponentInChildren<SmithyInteractable>();
        if (smithy == null)
        {
            GameObject smithyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smithyObject.name = "HubSmithyStation";
            smithyObject.transform.SetParent(transform, false);
            smithyObject.transform.localPosition = Vector3.zero;
            smithyObject.transform.localScale = new Vector3(1.4f, 1.1f, 1.4f);
            TideRuntimeVisualUtility.EnsureMeshMaterial(smithyObject.GetComponent<Renderer>());
            smithy = smithyObject.AddComponent<SmithyInteractable>();
        }

        if (smithy != null)
        {
            smithy.TryOpenFromExternal();
        }
    }

    private void OpenNarrativeService()
    {
        DialogueSystem dialogueSystem = DialogueSystem.Instance;
        if (dialogueSystem == null || dialogueSystem.IsDialogueActive)
        {
            return;
        }

        List<DialogueSystem.DialogueEntry> entries = new List<DialogueSystem.DialogueEntry>();
        if (narrativeLines != null)
        {
            for (int i = 0; i < narrativeLines.Length; i++)
            {
                if (string.IsNullOrEmpty(narrativeLines[i]))
                {
                    continue;
                }

                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = narratorName,
                    dialogueText = narrativeLines[i],
                    emotion = DialogueSystem.Emotion.Neutral
                });
            }
        }

        if (entries.Count > 0)
        {
            dialogueSystem.ShowDialogue(entries);
        }
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
        promptRoot = new GameObject("ServicePrompt");
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptLocalOffset;

        GameObject spriteObject = new GameObject("PromptImage");
        spriteObject.transform.SetParent(promptRoot.transform, false);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localRotation = Quaternion.identity;
        spriteObject.transform.localScale = promptScale;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetPromptSprite();
        TideRuntimeVisualUtility.ApplySpriteColor(spriteRenderer, promptTint);
        spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        spriteRenderer.sortingOrder = 10;
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

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
        GameStateManager gameStateManager = GameStateManager.Instance;
        return playerInRange
            && gameStateManager != null
            && gameStateManager.currentState == GameStateManager.GameState.Exploration
            && !gameStateManager.IsTransitioning;
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
