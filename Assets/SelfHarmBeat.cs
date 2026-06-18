using System;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class SelfHarmBeat : MonoBehaviour
{
    public const int LineCount = 4;

    public static SelfHarmBeat Instance { get; private set; }

    public event Action<int, string> OnSelfHarmLinePresented;
    public event Action OnSelfHarmSequenceFinished;

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

    public bool CanPlaySelfHarmSequence()
    {
        if (hasPlayed || isPlaying)
        {
            return false;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return false;
        }

        if (!gsm.IsEndingTriggered)
        {
            return false;
        }

        return gsm.ResolvedEndingBranch == GameStateManager.EndingBranch.Bad;
    }

    public bool PlaySelfHarmSequence()
    {
        if (!CanPlaySelfHarmSequence() && !CanPlaySelfHarmSequence())
        {
            return false;
        }

        isPlaying = true;
        currentLineIndex = 0;
        FireSequence();
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
        FireSequence();
    }

    public void ResetForDebug()
    {
        isPlaying = false;
        hasPlayed = false;
        currentLineIndex = 0;
    }

    private void FireSequence()
    {
        string[] lines = BuildSequenceLines();
        for (int i = 0; i < lines.Length; i++)
        {
            currentLineIndex = i;
            OnSelfHarmLinePresented?.Invoke(i, lines[i]);
        }

        isPlaying = false;
        hasPlayed = true;
        OnSelfHarmSequenceFinished?.Invoke();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkNarrativeBeatCompleted(NarrativeBeatsData.SelfHarmBeatId);
        }
    }

    private string[] BuildSequenceLines()
    {
        string[] lines = new string[LineCount];
        StringBuilder sb = new StringBuilder();
        sb.Append("The tide recoiled when I reached for it.");
        lines[0] = sb.ToString();
        sb.Clear();

        sb.Append("So I reached for myself instead. The wound will be quiet.");
        lines[1] = sb.ToString();
        sb.Clear();

        sb.Append("If this is what it costs, the island will remember me by the scar.");
        lines[2] = sb.ToString();
        sb.Clear();

        sb.Append("Press on. The rift must close. The rest is the tide's problem.");
        lines[3] = sb.ToString();

        return lines;
    }
}
