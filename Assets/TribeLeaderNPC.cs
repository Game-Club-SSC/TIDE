using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tribe leader NPC that refuses to share the truth about the ancient texts.
/// Each island has its own leader with unique evasive dialogue.
/// After the player discovers the truth, the leader's dialogue changes.
/// Follows the same trigger + key-press interaction pattern as <see cref="AncientTextInteractable"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class TribeLeaderNPC : MonoBehaviour, IPlayerInteractionAssistTarget
{
    private const string PromptResourceName = "PuzzlePrompt";
    private const float PromptPixelsPerUnit = 360f;

    // ------------------------------------------------------------------ //
    //  Inspector fields
    // ------------------------------------------------------------------ //

    [Header("Identity")]
    [SerializeField] private string npcName = "Elder";
    [SerializeField] private string _tribeName;
    [SerializeField] private string islandId;

    [Header("Dialogue")]
    [Tooltip("Lines shown when the player asks about the ancient texts (pre-revelation).")]
    [SerializeField] private string[] refuseToShareTruthLines;
    [Tooltip("Vague hints the leader does give, mixed in during refusal.")]
    [SerializeField] private string[] hintLines;
    [Tooltip("Lines shown after the player has discovered the truth on this island.")]
    [SerializeField] private string[] postRevelationLines;

    [Header("Requirements")]
    [Tooltip("Minimum story act required for this NPC to appear.")]
    [SerializeField] private GameStateManager.StoryAct requiredAct = GameStateManager.StoryAct.ActI;
    [Tooltip("Minimum restoration % on this island to find this NPC.")]
    [SerializeField] private float requiredRestoration;

    [Header("Prompt")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.68f, 0.68f, 1f);
    [SerializeField] private Color promptColor = Color.white;

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3f, 2.2f, 3f);
    [SerializeField] private KeyCode interactKey = KeyCode.Return;
    [SerializeField] private float interactEntryDelay = 0.35f;
    [SerializeField] private bool allowMovementAssist;

    // ------------------------------------------------------------------ //
    //  Runtime state
    // ------------------------------------------------------------------ //

    private bool hasBeenSpokenTo;
    private bool truthRevealed;
    private bool playerInRange;
    private int playerOverlapCount;
    private float playerEnteredRangeAt;
    private BoxCollider interactionTrigger;
    private GameObject promptRoot;
    private Sprite runtimePromptSprite;

    // ------------------------------------------------------------------ //
    //  Pre-configured tribe leaders
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Static factory that populates a <see cref="TribeLeaderNPC"/> with the
    /// canonical dialogue for a given island. Call from a level-design script
    /// or custom inspector to stamp out the correct leader.
    /// </summary>
    public static void ApplyPreset(TribeLeaderNPC npc, string islandKey)
    {
        if (npc == null)
        {
            return;
        }

        switch (islandKey)
        {
            case "island_lust":
                npc.npcName = "Elder Mirael";
                npc.islandId = "island_lust";
                npc._tribeName = "Tidal Kin";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "The tide flows as it always has. What was will be again.",
                    "You ask about things that are not meant to be spoken aloud.",
                    "The waves carry many secrets. It is not my place to disturb them.",
                    "Some truths are like the deep ocean -- beautiful from above, deadly below."
                };
                npc.hintLines = new[]
                {
                    "If you listen closely, you can hear the old words in the surf...",
                    "The garden remembers what the people have forgotten."
                };
                npc.postRevelationLines = new[]
                {
                    "You have seen it now. The tide does not forgive, but it does not forget either.",
                    "I could not tell you. Not because I did not want to, but because you had to see it for yourself.",
                    "The truth was always there, written in the water."
                };
                break;

            case "island_greed":
                npc.npcName = "Merchant Kael";
                npc.islandId = "island_greed";
                npc._tribeName = "Coin Holders";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "Knowledge is the only currency that cannot be stolen. But some knowledge is better left unspent.",
                    "You want answers? Everyone wants answers. What are you willing to pay?",
                    "I have traded in many things. Wisdom is the most expensive. And the most dangerous.",
                    "The texts are valuable, yes. But value is determined by the buyer, not the seller."
                };
                npc.hintLines = new[]
                {
                    "There are... accounts in the old ledger that speak of the binding...",
                    "The vault beneath the market holds more than gold."
                };
                npc.postRevelationLines = new[]
                {
                    "Now you know the true cost of knowledge. It was never about gold.",
                    "I told you some knowledge is better left unspent. You should have listened.",
                    "The market has crashed, in a manner of speaking."
                };
                break;

            case "island_anger":
                npc.npcName = "War Chief Torven";
                npc.islandId = "island_anger";
                npc._tribeName = "Blood Oath";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "You ask about the old words? Words are wind. Only actions leave marks.",
                    "I did not become chief by talking. I became chief by not listening.",
                    "The texts? irrelevant. The war is what matters. The war is all that has ever mattered.",
                    "Go poke around in your dusty scrolls if you want. I deal in steel, not stories."
                };
                npc.hintLines = new[]
                {
                    "The war camp archives are... not well guarded. Not anymore.",
                    "Even I have read the marks on the standing stones, when the fire burns low."
                };
                npc.postRevelationLines = new[]
                {
                    "Hmph. So the old words were true. I still say actions speak louder.",
                    "You found what you were looking for. Good. Now leave me to my war.",
                    "I should have listened. But that is not the way of the Blood Oath."
                };
                break;

            case "island_desire":
                npc.npcName = "Sage Lirael";
                npc.islandId = "island_desire";
                npc._tribeName = "Dream Walkers";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "The answers you seek are in the silence between heartbeats. But silence is... uncomfortable, isn't it?",
                    "Mmm? Oh, you are still here? I thought you had drifted off like the rest.",
                    "The truth requires patience. More patience than you have. More patience than anyone has.",
                    "I could tell you, but then you would have to sit with it. And sitting... is the hardest thing."
                };
                npc.hintLines = new[]
                {
                    "The dream journals in the archives speak of the binding, if you care to read them...",
                    "The old words are written in the lullabies the Dream Walkers sing."
                };
                npc.postRevelationLines = new[]
                {
                    "Now you know. Can you sit with it? Or will you run, like everyone else?",
                    "The silence between heartbeats... now you have heard what it says.",
                    "I told you the truth requires patience. You found it. Was it worth the stillness?"
                };
                break;

            case "island_envy":
                npc.npcName = "Seer Nyx";
                npc.islandId = "island_envy";
                npc._tribeName = "Eyeless Watch";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "I see what you will become. But telling you would rob you of the journey.",
                    "You envy what I know? That is the first step. The second is understanding why.",
                    "The texts show futures. Not all of them are the ones you want.",
                    "I see you standing at the threshold. But I cannot tell you what is on the other side."
                };
                npc.hintLines = new[]
                {
                    "The seeing pools in the tower reflect more than the sky...",
                    "The Eyeless Watch recorded everything. They just hid it well."
                };
                npc.postRevelationLines = new[]
                {
                    "Now you see what I see. The question is -- can you bear it?",
                    "I did not tell you because knowing changes the seeing. And now everything has changed.",
                    "The jealousy in your eyes... it was always about what you did not know."
                };
                break;

            case "island_ego":
                npc.npcName = "High Priestess Aurelia";
                npc.islandId = "island_ego";
                npc._tribeName = "Crown of Thorns";
                npc.requiredAct = GameStateManager.StoryAct.ActII;
                npc.refuseToShareTruthLines = new[]
                {
                    "We are the keepers of truth. And the truth is... you are not ready.",
                    "The sacred texts are not for outsiders. They are for those who have earned them.",
                    "You think you deserve to know? Ego comes before understanding, little one.",
                    "The Temple guards its knowledge as it guards its people. With absolute certainty."
                };
                npc.hintLines = new[]
                {
                    "The inner sanctum has not been opened in generations. Perhaps it is time...",
                    "The high texts are carved into the temple walls, if one knows where to look."
                };
                npc.postRevelationLines = new[]
                {
                    "You have humbled us. The keepers of truth... were blind to it themselves.",
                    "The ego of our order nearly cost us everything. You have given us a second chance.",
                    "We were not ready. But neither were you. And yet here we are."
                };
                break;

            default:
                Debug.LogWarning($"[TribeLeaderNPC] No preset defined for island '{islandKey}'.");
                break;
        }
    }

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

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

        InteractWithLeader(gsm);
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
            && Time.time - playerEnteredRangeAt >= Mathf.Max(0f, interactEntryDelay)
            && MeetsRequirements(gsm);
    }

    private bool MeetsRequirements(GameStateManager gsm)
    {
        if ((int)gsm.CurrentStoryAct < (int)requiredAct)
        {
            return false;
        }

        if (requiredRestoration > 0f)
        {
            float currentRestoration = gsm.GetIslandRestorationPercent(islandId);
            if (currentRestoration < requiredRestoration)
            {
                return false;
            }
        }

        return true;
    }

    private void InteractWithLeader(GameStateManager gsm)
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        List<DialogueSystem.DialogueEntry> entries = BuildDialogueEntries(gsm);
        if (entries.Count > 0)
        {
            hasBeenSpokenTo = true;
            DialogueSystem.Instance.ShowDialogue(entries);
        }
    }

    private List<DialogueSystem.DialogueEntry> BuildDialogueEntries(GameStateManager gsm)
    {
        List<DialogueSystem.DialogueEntry> entries = new List<DialogueSystem.DialogueEntry>();

        // Check if the truth has been revealed on this island
        // (truth is considered revealed when restoration reaches a threshold or a flag is set)
        truthRevealed = HasTruthBeenRevealed(gsm);

        if (truthRevealed && postRevelationLines != null && postRevelationLines.Length > 0)
        {
            // After truth is revealed, the leader speaks differently
            AddLines(entries, postRevelationLines, DialogueSystem.Emotion.Sad);
        }
        else
        {
            // Pre-revelation: refuse to share, possibly drop a hint
            if (refuseToShareTruthLines != null && refuseToShareTruthLines.Length > 0)
            {
                string refusal = refuseToShareTruthLines[Random.Range(0, refuseToShareTruthLines.Length)];
                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = npcName,
                    dialogueText = refusal,
                    emotion = hasBeenSpokenTo ? DialogueSystem.Emotion.Angry : DialogueSystem.Emotion.Neutral
                });
            }

            // On repeat visits, occasionally drop a vague hint
            if (hasBeenSpokenTo && hintLines != null && hintLines.Length > 0 && Random.value < 0.4f)
            {
                string hint = hintLines[Random.Range(0, hintLines.Length)];
                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = npcName,
                    dialogueText = hint,
                    emotion = DialogueSystem.Emotion.Worried
                });
            }
        }

        return entries;
    }

    private void AddLines(List<DialogueSystem.DialogueEntry> entries, string[] lines, DialogueSystem.Emotion emotion)
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            entries.Add(new DialogueSystem.DialogueEntry
            {
                speakerName = npcName,
                dialogueText = lines[i],
                emotion = emotion
            });
        }
    }

    /// <summary>
    /// Determines whether the truth about the ancient texts has been revealed
    /// for this island. Override this or extend the GameStateManager to add
    /// custom flags. Currently uses restoration threshold as a proxy.
    /// </summary>
    private bool HasTruthBeenRevealed(GameStateManager gsm)
    {
        if (gsm == null)
        {
            return false;
        }

        // Consider the truth revealed once restoration passes 75%
        float restoration = gsm.GetIslandRestorationPercent(islandId);
        return restoration >= 0.75f;
    }

    // ------------------------------------------------------------------ //
    //  Prompt visuals (matches AncientTextInteractable pattern)
    // ------------------------------------------------------------------ //

    private void CreatePrompt()
    {
        promptRoot = new GameObject("TribeLeaderPrompt");
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
