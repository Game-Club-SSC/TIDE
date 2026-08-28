using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class AcceptanceConversation : MonoBehaviour
{
    public const string FinalBossIslandId = "island_ego";
    public const float RestorationThreshold = 75f;
    public const int LineCount = 10;

    public static AcceptanceConversation Instance { get; private set; }

    [SerializeField] private string finalBossIslandId = FinalBossIslandId;
    [SerializeField, Range(0f, 100f)] private float restorationThreshold = RestorationThreshold;

    public event Action<int, string> OnAcceptanceLinePresented;
    public event Action OnAcceptanceConversationFinished;

    private bool isPlaying;
    private bool hasPlayed;
    private int currentLineIndex;
    private DialogueSystem activeDialogueSystem;
    private List<DialogueSystem.DialogueEntry> activeDialogueEntries;

    public bool IsPlaying => isPlaying;
    public bool HasPlayed => hasPlayed;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        UnsubscribeFromDialogueSystem();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanPlayAcceptanceConversation()
    {
        if (hasPlayed || isPlaying)
        {
            return false;
        }

        string activeId = ResolveActiveIslandId();
        if (!string.Equals(activeId, finalBossIslandId, StringComparison.Ordinal))
        {
            return false;
        }

        float restoration = GetActiveIslandRestoration();
        return restoration >= restorationThreshold;
    }

    public bool PlayAcceptanceConversation()
    {
        if (isPlaying || hasPlayed)
        {
            return false;
        }

        if (!CanPlayAcceptanceConversation())
        {
            return false;
        }

        isPlaying = true;
        currentLineIndex = 0;
        StartCoroutine(PlayVisibleDialogueRoutine());
        return true;
    }

    public void ForcePlayForDebug()
    {
        if (isPlaying || hasPlayed)
        {
            return;
        }

        isPlaying = true;
        currentLineIndex = 0;

        // Fire all lines synchronously so that tests (and context-menu
        // regression checks) observe every event in a single frame.
        string[] lines = BuildDialogueLines();
        for (int i = 0; i < lines.Length; i++)
        {
            currentLineIndex = i;
            OnAcceptanceLinePresented?.Invoke(i, lines[i]);
        }

        isPlaying = false;
        hasPlayed = true;
        OnAcceptanceConversationFinished?.Invoke();
    }

    public void ResetForDebug()
    {
        ResetForNewGame();
    }

    public void ResetForNewGame()
    {
        StopAllCoroutines();
        UnsubscribeFromDialogueSystem();
        isPlaying = false;
        hasPlayed = false;
        currentLineIndex = 0;
    }

    private bool HasMetPrerequisites()
    {
        string activeId = ResolveActiveIslandId();
        if (!string.Equals(activeId, finalBossIslandId, StringComparison.Ordinal))
        {
            return false;
        }

        float restoration = GetActiveIslandRestoration();
        return restoration >= restorationThreshold;
    }

    private IEnumerator PlayVisibleDialogueRoutine()
    {
        DialogueSystem dialogueSystem = EnsureDialogueSystem();
        if (dialogueSystem == null)
        {
            CancelVisibleDialogue("DialogueSystem could not be created.");
            yield break;
        }

        while (isPlaying && dialogueSystem.IsDialogueActive)
        {
            yield return null;
        }

        if (!isPlaying || hasPlayed)
        {
            yield break;
        }

        activeDialogueSystem = dialogueSystem;
        activeDialogueEntries = BuildDialogueEntries();
        if (activeDialogueEntries == null || activeDialogueEntries.Count != LineCount)
        {
            CancelVisibleDialogue("Acceptance dialogue entries are incomplete.");
            yield break;
        }

        for (int i = 0; i < activeDialogueEntries.Count; i++)
        {
            currentLineIndex = i;
            OnAcceptanceLinePresented?.Invoke(i, activeDialogueEntries[i].dialogueText);
        }

        activeDialogueSystem.OnDialogueCompleted += HandleVisibleDialogueCompleted;
        activeDialogueSystem.ShowDialogue(activeDialogueEntries);

        if (!isPlaying)
        {
            yield break;
        }

        if (!activeDialogueSystem.IsDialogueActive)
        {
            CancelVisibleDialogue("DialogueSystem rejected the acceptance sequence.");
            yield break;
        }

        while (isPlaying && activeDialogueSystem != null && activeDialogueSystem.IsDialogueActive)
        {
            yield return null;
        }

        if (isPlaying)
        {
            CancelVisibleDialogue("Acceptance dialogue ended before its completion callback.");
        }
    }

    private void HandleVisibleDialogueCompleted(List<DialogueSystem.DialogueEntry> completedEntries)
    {
        if (activeDialogueEntries == null || !ReferenceEquals(completedEntries, activeDialogueEntries))
        {
            return;
        }

        UnsubscribeFromDialogueSystem();
        isPlaying = false;
        hasPlayed = true;
        currentLineIndex = LineCount - 1;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkNarrativeBeatCompleted(NarrativeBeatsData.AcceptanceConversationId);
        }

        OnAcceptanceConversationFinished?.Invoke();
    }

    private DialogueSystem EnsureDialogueSystem()
    {
        if (DialogueSystem.Instance != null)
        {
            return DialogueSystem.Instance;
        }

        GameObject dialogueSystemObject = new GameObject("DialogueSystem");
        return dialogueSystemObject.AddComponent<DialogueSystem>();
    }

    private void UnsubscribeFromDialogueSystem()
    {
        if (activeDialogueSystem != null)
        {
            activeDialogueSystem.OnDialogueCompleted -= HandleVisibleDialogueCompleted;
        }

        activeDialogueSystem = null;
        activeDialogueEntries = null;
    }

    private void CancelVisibleDialogue(string reason)
    {
        UnsubscribeFromDialogueSystem();
        isPlaying = false;
        currentLineIndex = 0;
        Debug.LogWarning($"[AcceptanceConversation] {reason}");
    }

    private List<DialogueSystem.DialogueEntry> BuildDialogueEntries()
    {
        string[] lines = BuildDialogueLines();
        List<DialogueSystem.DialogueEntry> entries = new List<DialogueSystem.DialogueEntry>(lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            entries.Add(new DialogueSystem.DialogueEntry
            {
                speakerName = GetSpeakerName(i),
                dialogueText = lines[i],
                emotion = GetEmotion(i)
            });
        }

        return entries;
    }

    private static string GetSpeakerName(int lineIndex)
    {
        switch (lineIndex)
        {
            case 0:
            case 2:
                return "MC";
            case 1:
            case 7:
                return "Freida";
            case 3:
            case 6:
                return "Merrick";
            case 4:
                return "Briar";
            case 5:
                return "Killian";
            case 8:
                return "The Five";
            default:
                return "Narrator";
        }
    }

    private static DialogueSystem.Emotion GetEmotion(int lineIndex)
    {
        switch (lineIndex)
        {
            case 1:
            case 3:
            case 4:
                return DialogueSystem.Emotion.Worried;
            case 5:
            case 6:
            case 8:
                return DialogueSystem.Emotion.Determined;
            case 7:
                return DialogueSystem.Emotion.Happy;
            default:
                return DialogueSystem.Emotion.Neutral;
        }
    }

    private string[] BuildDialogueLines()
    {
        string[] lines = new string[LineCount];
        StringBuilder sb = new StringBuilder();

        sb.Append("This is it. The last island.");
        lines[0] = sb.ToString();
        sb.Clear();

        sb.Append("The texts call it the Shore of Self. Where the tide meets its source.");
        lines[1] = sb.ToString();
        sb.Clear();

        sb.Append("I've carried the tide through every island. If I turn back now, the rift widens.");
        lines[2] = sb.ToString();
        sb.Clear();

        sb.Append("If you press on, it costs you. If you turn back, it costs everyone.");
        lines[3] = sb.ToString();
        sb.Clear();

        sb.Append("The cost was always part of the equation. We just didn't want to read the fine print.");
        lines[4] = sb.ToString();
        sb.Clear();

        sb.Append("Then we end this together. One way or another.");
        lines[5] = sb.ToString();
        sb.Clear();

        sb.Append("That's all I needed to hear. The tide will carry us — even through the end.");
        lines[6] = sb.ToString();
        sb.Clear();

        sb.Append("Whatever happens on that shore... we faced it together. That's what matters.");
        lines[7] = sb.ToString();
        sb.Clear();

        sb.Append("We've come too far to let doubt be the thing that breaks us. Together. As five. As we started.");
        lines[8] = sb.ToString();
        sb.Clear();

        sb.Append("And so the five walk toward the final shore, carrying the weight of every choice that brought them here.");
        lines[9] = sb.ToString();

        return lines;
    }

    private static string ResolveActiveIslandId()
    {
        IslandProgressionManager progression = IslandProgressionManager.Instance;
        if (progression != null && !string.IsNullOrEmpty(progression.ActiveIslandId))
        {
            return progression.ActiveIslandId;
        }

        return IslandThemeRegistry.DefaultIslandId;
    }

    private static float GetActiveIslandRestoration()
    {
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker == null)
        {
            return 0f;
        }

        string activeId = ResolveActiveIslandId();
        return tracker.GetRestorationPercent(activeId);
    }
}
