using System;
using System.Collections.Generic;
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

    [Serializable]
    public class StoryProgressionSnapshot
    {
        public int currentAct;
        public int highestActReached;
        public string endingBranch;
        public bool endingTriggered;
        public List<string> flagKeys = new List<string>();
        public List<bool> flagValues = new List<bool>();
    }

    [SerializeField] private StoryAct currentAct = StoryAct.None;
    [SerializeField] private StoryAct highestActReached = StoryAct.None;
    [SerializeField] private EndingBranch endingBranch = EndingBranch.None;
    [SerializeField] private bool endingTriggered;

    private readonly Dictionary<string, bool> storyFlags = new Dictionary<string, bool>(StringComparer.Ordinal);

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

    public void SetFlag(string flagId, bool value)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return;
        }

        storyFlags[flagId] = value;
        Debug.Log($"[StoryProgressionService] Flag '{flagId}' set to {value}.");
    }

    public bool GetFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return false;
        }

        return storyFlags.TryGetValue(flagId, out bool value) && value;
    }

    public bool IsQuestCompleted(string questId)
    {
        return GetFlag(questId);
    }

    /// <summary>
    /// Mirrors the quest/story flags into parallel save lists, because
    /// JsonUtility cannot serialize the dictionary directly.
    /// </summary>
    public void CopyFlagsTo(List<string> keys, List<bool> values)
    {
        if (keys == null || values == null)
        {
            return;
        }

        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<string, bool> pair in storyFlags)
        {
            if (string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    /// <summary>
    /// Restores flags from parallel save lists, replacing the current set.
    /// </summary>
    public void RestoreFlagsFrom(List<string> keys, List<bool> values)
    {
        if (keys == null || values == null)
        {
            return;
        }

        // Replace, not merge: the save is authoritative. Merging left flags
        // that the new payload no longer records, so a reload or a rollback
        // could not clear a quest.
        storyFlags.Clear();

        int count = Mathf.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrEmpty(keys[i]))
            {
                storyFlags[keys[i]] = values[i];
            }
        }
    }

    public StoryProgressionSnapshot CaptureSnapshot()
    {
        StoryProgressionSnapshot snapshot = new StoryProgressionSnapshot();
        snapshot.currentAct = (int)currentAct;
        snapshot.highestActReached = (int)highestActReached;
        snapshot.endingBranch = endingBranch.ToString();
        snapshot.endingTriggered = endingTriggered;

        foreach (KeyValuePair<string, bool> pair in storyFlags)
        {
            snapshot.flagKeys.Add(pair.Key);
            snapshot.flagValues.Add(pair.Value);
        }

        return snapshot;
    }

    public void ApplySnapshot(StoryProgressionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        currentAct = (StoryAct)snapshot.currentAct;
        highestActReached = (StoryAct)snapshot.highestActReached;
        endingTriggered = snapshot.endingTriggered;

        if (Enum.TryParse(snapshot.endingBranch, out EndingBranch parsed))
        {
            endingBranch = parsed;
        }
        else
        {
            endingBranch = EndingBranch.None;
        }

        storyFlags.Clear();
        if (snapshot.flagKeys != null && snapshot.flagValues != null)
        {
            int count = Mathf.Min(snapshot.flagKeys.Count, snapshot.flagValues.Count);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(snapshot.flagKeys[i]))
                {
                    storyFlags[snapshot.flagKeys[i]] = snapshot.flagValues[i];
                }
            }
        }

        Debug.Log($"[StoryProgressionService] Applied snapshot: act={currentAct}, ending={endingBranch}, flags={storyFlags.Count}.");
    }

    public void SyncFromGameStateManager()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        currentAct = (StoryAct)(int)gsm.CurrentStoryAct;
        if ((int)currentAct > (int)highestActReached)
        {
            highestActReached = currentAct;
        }

        endingBranch = (EndingBranch)(int)gsm.ResolvedEndingBranch;
        endingTriggered = gsm.IsEndingTriggered;

        Debug.Log("[StoryProgressionService] Synced state from GameStateManager.");
    }

    public void ApplyToGameStateManager()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        gsm.ForceStoryActForDebug((GameStateManager.StoryAct)(int)currentAct);
        gsm.ForceEndingBranchForDebug((GameStateManager.EndingBranch)(int)endingBranch);
        Debug.Log("[StoryProgressionService] Applied state to GameStateManager.");
    }

    /// <summary>
    /// Clears only the story flags, leaving act/ending state untouched.
    /// Used by the new-game/reset flow so dialogue-set flags cannot leak into a
    /// fresh playthrough (issue #297).
    /// </summary>
    public void ClearFlagsForDebug()
    {
        storyFlags.Clear();
    }

    public void ResetForDebug()
    {
        currentAct = StoryAct.None;
        highestActReached = StoryAct.None;
        endingBranch = EndingBranch.None;
        endingTriggered = false;
        storyFlags.Clear();
    }
}
