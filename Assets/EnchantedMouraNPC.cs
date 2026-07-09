using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enchanted moura NPC that appears helpful but is actually malicious.
/// Offers misleading information about the ancient texts.
/// After enough interactions, reveals its true nature.
/// If <see cref="leadsToLustBoss"/> is true, the final interaction triggers the Lust boss fight.
/// Follows the same trigger + key-press interaction pattern as <see cref="AncientTextInteractable"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class EnchantedMouraNPC : MonoBehaviour, IPlayerInteractionAssistTarget
{
    private const string PromptResourceName = "PuzzlePrompt";
    private const float PromptPixelsPerUnit = 360f;
    [SerializeField] private int interactionsBeforeReveal = 3;

    // ------------------------------------------------------------------ //
    //  Inspector fields
    // ------------------------------------------------------------------ //

    [Header("Identity")]
    [SerializeField] private string npcName = "Enchanted Moura";

    [Header("Seeming Helpfulness")]
    [Tooltip("Lines shown on the first interaction -- the moura offers to help.")]
    [SerializeField] private string[] offerHelpLines;
    [Tooltip("Lines shown on subsequent interactions -- the moura provides misleading info.")]
    [SerializeField] private string[] provideInfoLines;
    [Tooltip("Lines shown when giving a 'gift' that is actually a trap.")]
    [SerializeField] private string[] giftLines;

    [Header("Hidden Evil")]
    [Tooltip("Lines shown after the player discovers the truth (after reveal threshold).")]
    [SerializeField] private string[] revealedEvilLines;
    [Tooltip("Whether this moura leads to the Lust boss encounter after reveal.")]
    [SerializeField] private bool leadsToLustBoss;

    [Header("Boss Encounter")]
    [Tooltip("Island ID for the Lust boss encounter trigger.")]
    [SerializeField] private string bossIslandId = "island_lust";
    [Tooltip("Encounter ID for the Lust boss fight.")]
    [SerializeField] private string bossEncounterId = "boss_lust";
    [Tooltip("Restoration value granted after defeating the Lust boss.")]
    [SerializeField] private float bossRestorationValue = 0.05f;

    [Header("Prompt")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.0f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.68f, 0.68f, 1f);
    [SerializeField] private Color promptColor = new Color(0.8f, 0.6f, 1f, 1f);

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3f, 2.2f, 3f);
    [SerializeField] private KeyCode interactKey = KeyCode.Return;
    [SerializeField] private float interactEntryDelay = 0.35f;
    [SerializeField] private bool allowMovementAssist;

    // ------------------------------------------------------------------ //
    //  Runtime state
    // ------------------------------------------------------------------ //

    private bool isRevealed;
    private int interactionCount;
    private bool playerInRange;
    private int playerOverlapCount;
    private float playerEnteredRangeAt;
    private BoxCollider interactionTrigger;
    private GameObject promptRoot;
    private Sprite runtimePromptSprite;

    // ------------------------------------------------------------------ //
    //  Public properties
    // ------------------------------------------------------------------ //

    /// <summary>True after the moura has revealed its true nature.</summary>
    public bool IsRevealed => isRevealed;

    /// <summary>How many times the player has spoken to this moura.</summary>
    public int InteractionCount => interactionCount;

    /// <summary>Whether this moura will trigger the Lust boss on final interaction.</summary>
    public bool LeadsToLustBoss => leadsToLustBoss;

    // ------------------------------------------------------------------ //
    //  Pre-configured defaults
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Populates this moura with the canonical enchanted moura dialogue.
    /// Call from a level-design script or custom inspector.
    /// </summary>
    public void ApplyDefaultDialogue()
    {
        npcName = "Enchanted Moura";

        offerHelpLines = new[]
        {
            "You seek knowledge of the old ways? I can help you. The texts you search for are in the garden to the east.",
            "Do not be alarmed, traveler. We moura are friends to those who seek truth.",
            "The tribe leaders will not speak to you? How unfortunate. But we remember the old words."
        };

        provideInfoLines = new[]
        {
            "The tribe leaders won't help you? They never do. But we moura remember everything.",
            "The texts are hidden in places of power. I can guide you, if you trust me.",
            "You are getting closer. I can feel it. The truth is just beyond the next ridge.",
            "The binding was a mistake, they say. But who are 'they' to judge?"
        };

        giftLines = new[]
        {
            "Take this. A gift from us to you. It will help you on your journey.",
            "A small token of our friendship. Do not worry about its... unusual appearance."
        };

        revealedEvilLines = new[]
        {
            "You believed us? How... delightful. The texts are real, yes. But the truth they hold? That is the real trap.",
            "Every step you took toward us was a step away from safety. And you never even noticed.",
            "The tribe leaders refused you because they were trying to protect you. How ironic that you ignored them for us.",
            "We are the voices in the silence. The ones who whisper when the old words should remain unspoken."
        };
    }

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        // Apply defaults if arrays are empty (set in inspector or via ApplyDefaultDialogue)
        if (offerHelpLines == null || offerHelpLines.Length == 0)
        {
            ApplyDefaultDialogue();
        }

        CreatePrompt();
        SetPromptVisible(false);
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

        InteractWithMoura(gsm);
    }

    // ------------------------------------------------------------------ //
    //  Interaction logic
    // ------------------------------------------------------------------ //

    private bool CanInteract(GameStateManager gsm)
    {
        return playerInRange
            && gsm != null
            && gsm.currentState == GameStateManager.GameState.Exploration
            && !gsm.IsTransitioning
            && Time.time - playerEnteredRangeAt >= Mathf.Max(0f, interactEntryDelay);
    }

    private void InteractWithMoura(GameStateManager gsm)
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        // On the reveal interaction: if leadsToLustBoss, trigger combat instead of dialogue
        if (isRevealed && leadsToLustBoss)
        {
            TriggerLustBossFight(gsm);
            return;
        }

        List<DialogueSystem.DialogueEntry> entries = BuildDialogueEntries();
        if (entries.Count > 0)
        {
            DialogueSystem.Instance.ShowDialogue(entries);
        }

        interactionCount++;

        // Check if this interaction triggers the reveal
        if (!isRevealed && interactionCount >= interactionsBeforeReveal)
        {
            isRevealed = true;
        }
    }

    private List<DialogueSystem.DialogueEntry> BuildDialogueEntries()
    {
        List<DialogueSystem.DialogueEntry> entries = new List<DialogueSystem.DialogueEntry>();

        if (isRevealed)
        {
            // Post-reveal: the moura reveals its true nature
            AddRandomLine(entries, revealedEvilLines, DialogueSystem.Emotion.Angry);

            // On the first revealed interaction, also show a gift line (the trap)
            if (interactionCount == interactionsBeforeReveal && giftLines != null && giftLines.Length > 0)
            {
                AddRandomLine(entries, giftLines, DialogueSystem.Emotion.Happy);
            }
        }
        else if (interactionCount == 0)
        {
            // First interaction: offer help (seemingly kind)
            AddRandomLine(entries, offerHelpLines, DialogueSystem.Emotion.Happy);
        }
        else
        {
            // Subsequent interactions before reveal: provide misleading info
            AddRandomLine(entries, provideInfoLines, DialogueSystem.Emotion.Neutral);
        }

        return entries;
    }

    private void AddRandomLine(
        List<DialogueSystem.DialogueEntry> entries,
        string[] lines,
        DialogueSystem.Emotion emotion)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        entries.Add(new DialogueSystem.DialogueEntry
        {
            speakerName = npcName,
            dialogueText = lines[Random.Range(0, lines.Length)],
            emotion = emotion
        });
    }

    // ------------------------------------------------------------------ //
    //  Lust boss trigger
    // ------------------------------------------------------------------ //

    private void TriggerLustBossFight(GameStateManager gsm)
    {
        if (gsm == null)
        {
            return;
        }

        if (!gsm.CanEnterCombatScene())
        {
            return;
        }

        Debug.Log($"[EnchantedMouraNPC] '{npcName}' revealing true form and triggering Lust boss combat.");

        GameStateManager.Instance.EnterCombatSceneFromExploration(
            bossIslandId,
            bossEncounterId,
            bossRestorationValue,
            transform.position,
            true);

        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  Prompt visuals (matches AncientTextInteractable pattern)
    // ------------------------------------------------------------------ //

    private void CreatePrompt()
    {
        promptRoot = new GameObject("MouraPrompt");
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
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

        promptRoot.transform.rotation = Quaternion.LookRotation(
            facingDirection.normalized, mainCamera.transform.up);
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

    // ------------------------------------------------------------------ //
    //  Trigger callbacks
    // ------------------------------------------------------------------ //

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

    // ------------------------------------------------------------------ //
    //  IPlayerInteractionAssistTarget
    // ------------------------------------------------------------------ //

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

    // ------------------------------------------------------------------ //
    //  Cleanup
    // ------------------------------------------------------------------ //

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

    // ------------------------------------------------------------------ //
    //  Utility
    // ------------------------------------------------------------------ //

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
