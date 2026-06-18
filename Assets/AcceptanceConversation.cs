using System;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class AcceptanceConversation : MonoBehaviour
{
    public const string FinalBossIslandId = "island_pride";
    public const float RestorationThreshold = 0.75f;
    public const int LineCount = 3;

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

        if (!CanPlayAcceptanceConversation() && !CanPlayAcceptanceConversation())
        {
            if (!HasMetPrerequisites())
            {
                return false;
            }
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
        sb.Append("I've carried the tide through every island.");
        lines[0] = sb.ToString();
        sb.Clear();

        sb.Append("If I turn back now, the rift widens. If I press on, it costs me.");
        lines[1] = sb.ToString();
        sb.Clear();

        sb.Append("Then I'll pay it. We end this together.");
        lines[2] = sb.ToString();

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
