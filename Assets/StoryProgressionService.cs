using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StoryProgressionService : MonoBehaviour
{
    public static StoryProgressionService Instance { get; private set; }

    public enum StoryAct
    {
        None = 0,
        ActI = 1,
        ActII = 2,
        ActIII = 3
    }

    public enum EndingBranch
    {
        None,
        Good,
        Bad
    }

    [SerializeField] private StoryAct currentAct = StoryAct.None;
    [SerializeField] private StoryAct highestActReached = StoryAct.None;
    [SerializeField] private EndingBranch endingBranch = EndingBranch.None;
    [SerializeField] private bool endingTriggered;

    public event Action<StoryAct> OnStoryActChanged;
    public event Action<EndingBranch> OnEndingBranchChanged;
    public event Action OnEndingTriggered;

    public StoryAct CurrentAct => currentAct;
    public StoryAct HighestActReached => highestActReached;
    public EndingBranch ResolvedEndingBranch => endingBranch;
    public bool IsEndingTriggered => endingTriggered;

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

    public void SetCurrentAct(StoryAct act)
    {
        if (currentAct == act)
        {
            return;
        }

        currentAct = act;
        if ((int)act > (int)highestActReached)
        {
            highestActReached = act;
        }
        OnStoryActChanged?.Invoke(act);
        Debug.Log($"[StoryProgressionService] Story act set to {act}.");
    }

    public void SetEndingBranch(EndingBranch branch)
    {
        if (endingBranch == branch)
        {
            return;
        }

        endingBranch = branch;
        OnEndingBranchChanged?.Invoke(branch);
        Debug.Log($"[StoryProgressionService] Ending branch set to {branch}.");
    }

    public void TriggerEnding()
    {
        if (endingTriggered)
        {
            return;
        }

        endingTriggered = true;
        OnEndingTriggered?.Invoke();
        Debug.Log($"[StoryProgressionService] Ending triggered ({endingBranch}).");
    }

    public void ResetForDebug()
    {
        currentAct = StoryAct.None;
        highestActReached = StoryAct.None;
        endingBranch = EndingBranch.None;
        endingTriggered = false;
    }
}
