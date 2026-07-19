using System;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class AcceptanceConversation : MonoBehaviour
{
    public const string FinalBossIslandId = "island_ego";
    public const float RestorationThreshold = 0.75f;
    public const int LineCount = 10;

    public static AcceptanceConversation Instance { get; private set; }

    [SerializeField] private string finalBossIslandId = FinalBossIslandId;
    [SerializeField, Range(0f, 1f)] private float restorationThreshold = RestorationThreshold;

    public event Action<int, string> OnAcceptanceLinePresented;
    public event Action OnAcceptanceConversationFinished;

    private bool isPlaying;
    private bool hasPlayed;
    private int currentLineIndex;

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
        FireLines();
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
        FireLines();
    }

    public void ResetForDebug()
    {
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

    private void FireLines()
    {
        string[] lines = BuildDialogueLines();
        for (int i = 0; i < lines.Length; i++)
        {
            currentLineIndex = i;
            OnAcceptanceLinePresented?.Invoke(i, lines[i]);
        }

        isPlaying = false;
        hasPlayed = true;
        OnAcceptanceConversationFinished?.Invoke();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkNarrativeBeatCompleted(NarrativeBeatsData.AcceptanceConversationId);
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
