using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to an NPC or story-point GameObject with a trigger collider.
/// When the player enters the trigger zone, the configured dialogue plays.
/// Supports one-shot or repeatable dialogue.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField]
    private List<DialogueSystem.DialogueEntry> dialogueEntries = new List<DialogueSystem.DialogueEntry>();

    [Tooltip("If set, this tree is used instead of the flat dialogueEntries list.")]
    [SerializeField]
    private DialogueTree dialogueTree;

    [Header("Trigger Settings")]
    [Tooltip("If true, dialogue plays only once per session. If false, it replays every time the player enters.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("Delay in seconds after the player enters the trigger before dialogue starts.")]
    [SerializeField] private float entryDelay = 0.3f;

    [Tooltip("Require the player to press the interact key instead of auto-triggering.")]
    [SerializeField] private bool requireInteractKey = false;

    [SerializeField] private KeyCode interactKey = KeyCode.Return;

    [Header("Bonding (optional)")]
    [Tooltip("If set, increase bond between the first two relatedHeroId speakers in this dialogue on completion.")]
    [SerializeField] private bool awardBondOnCompletion = false;

    [Tooltip("Bond amount awarded when dialogue completes.")]
    [SerializeField] private int bondAmount = 5;

    private bool playerInRange;
    private float playerEnteredRangeAt;
    private bool hasPlayed;
    private bool dialoguePending;

    /// <summary>Fired when this trigger's dialogue completes (useful for quest systems).</summary>
    public event Action OnDialogueFinished;

    private void Update()
    {
        if (!playerInRange || dialoguePending)
        {
            return;
        }

        if (hasPlayed && oneShot)
        {
            return;
        }

        if (!CanStartDialogue())
        {
            return;
        }

        if (requireInteractKey)
        {
            bool pressed = Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!pressed)
            {
                return;
            }
        }
        else
        {
            if (Time.time - playerEnteredRangeAt < entryDelay)
            {
                return;
            }
        }

        StartDialogue();
    }

    private bool CanStartDialogue()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return true; // allow dialogue even without GSM (e.g. cutscenes)
        }

        return gsm.currentState == GameStateManager.GameState.Exploration
            && !gsm.IsTransitioning
            && DialogueSystem.Instance != null
            && !DialogueSystem.Instance.IsDialogueActive;
    }

    private void StartDialogue()
    {
        dialoguePending = true;
        hasPlayed = true;

        DialogueSystem sys = DialogueSystem.Instance;
        if (sys == null)
        {
            Debug.LogWarning("[DialogueTrigger] DialogueSystem instance not found.");
            dialoguePending = false;
            return;
        }

        // Prefer branching tree if assigned
        if (dialogueTree != null && dialogueTree.rootNode != null)
        {
            sys.OnDialogueTreeCompleted += HandleTreeDialogueCompleted;
            sys.StartDialogueTree(dialogueTree);
            return;
        }

        if (dialogueEntries == null || dialogueEntries.Count == 0)
        {
            dialoguePending = false;
            return;
        }

        sys.OnDialogueCompleted += HandleDialogueCompleted;
        sys.ShowDialogue(dialogueEntries);
    }

    private void HandleDialogueCompleted(List<DialogueSystem.DialogueEntry> _)
    {
        DialogueSystem sys = DialogueSystem.Instance;
        if (sys != null)
        {
            sys.OnDialogueCompleted -= HandleDialogueCompleted;
        }

        if (awardBondOnCompletion)
        {
            AwardBondFromEntries();
        }

        dialoguePending = false;
        OnDialogueFinished?.Invoke();
    }

    private void HandleTreeDialogueCompleted(string treeId)
    {
        DialogueSystem sys = DialogueSystem.Instance;
        if (sys != null)
        {
            sys.OnDialogueTreeCompleted -= HandleTreeDialogueCompleted;
        }

        dialoguePending = false;
        OnDialogueFinished?.Invoke();
    }

    private void AwardBondFromEntries()
    {
        DialogueSystem sys = DialogueSystem.Instance;
        if (sys == null)
        {
            return;
        }

        // Collect unique related hero IDs from the dialogue entries
        HashSet<string> heroes = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < dialogueEntries.Count; i++)
        {
            DialogueSystem.DialogueEntry entry = dialogueEntries[i];
            if (!string.IsNullOrEmpty(entry.relatedHeroId))
            {
                heroes.Add(entry.relatedHeroId);
            }
        }

        // If we have at least 2 distinct heroes, bond the first pair
        List<string> heroList = new List<string>(heroes);
        if (heroList.Count >= 2)
        {
            sys.IncreaseBond(heroList[0], heroList[1], bondAmount);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = true;
        playerEnteredRangeAt = Time.time;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
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

    // ------------------------------------------------------------------ //
    //  Inspector helpers
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Add a dialogue entry from the inspector or code.
    /// </summary>
    public void AddDialogueEntry(string speakerName, string text, DialogueSystem.Emotion emotion = DialogueSystem.Emotion.Neutral, string relatedHeroId = null)
    {
        dialogueEntries.Add(new DialogueSystem.DialogueEntry
        {
            speakerName = speakerName,
            dialogueText = text,
            emotion = emotion,
            relatedHeroId = relatedHeroId
        });
    }

    /// <summary>
    /// Replace all dialogue entries at runtime.
    /// </summary>
    public void SetDialogueEntries(List<DialogueSystem.DialogueEntry> entries)
    {
        dialogueEntries = entries != null
            ? new List<DialogueSystem.DialogueEntry>(entries)
            : new List<DialogueSystem.DialogueEntry>();
    }

    /// <summary>
    /// Reset one-shot state so dialogue can play again.
    /// </summary>
    public void ResetOneShot()
    {
        hasPlayed = false;
    }
}
