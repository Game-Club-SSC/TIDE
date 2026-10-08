using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NewGamePlusService : MonoBehaviour
{
    public static NewGamePlusService Instance { get; private set; }

    [SerializeField] private int minCompletedRuns = 1;
    [SerializeField] private float ngPlusEnemyMultiplier = 1.5f;
    [SerializeField] private float ngPlusXpMultiplier = 1.25f;
    [SerializeField] private int startingCurrency = 500;

    public event Action<int> OnNewGamePlusStarted;
    public event Action OnNewGamePlusEnded;

    public bool IsInNewGamePlus { get; private set; }
    public int LoopIndex { get; private set; }
    public int CompletedRuns { get; private set; }

    private const string SaveKey = "NewGamePlusService";
    private const float MaximumStatMultiplier = 1000f;
    private bool completedCurrentRun;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanStartNewGamePlus()
    {
        return CompletedRuns >= Mathf.Max(1, minCompletedRuns) && completedCurrentRun;
    }

    public void RegisterCompletion()
    {
        if (completedCurrentRun) return;
        completedCurrentRun = true;
        if (CompletedRuns < int.MaxValue) CompletedRuns++;
        Save();
        Debug.Log($"[NewGamePlusService] Registered completion #{CompletedRuns}.");
    }

    public bool StartNewGamePlus()
    {
        if (!CanStartNewGamePlus())
        {
            return false;
        }

        IsInNewGamePlus = true;
        completedCurrentRun = false;
        if (LoopIndex < int.MaxValue) LoopIndex++;
        Save();
        OnNewGamePlusStarted?.Invoke(LoopIndex);
        Debug.Log($"[NewGamePlusService] Starting NG+ loop {LoopIndex}.");
        return true;
    }

    public void EndNewGamePlus()
    {
        if (!IsInNewGamePlus)
        {
            return;
        }

        IsInNewGamePlus = false;
        Save();
        OnNewGamePlusEnded?.Invoke();
    }

    public float GetEnemyStatMultiplier()
    {
        return GetBoundedMultiplier(ngPlusEnemyMultiplier);
    }

    public float GetXpMultiplier()
    {
        return GetBoundedMultiplier(ngPlusXpMultiplier);
    }

    public int GetStartingCurrency()
    {
        return IsInNewGamePlus ? (int)Math.Min(int.MaxValue, (long)Mathf.Max(0, startingCurrency) * LoopIndex) : 0;
    }

    public IReadOnlyList<string> GetCarryOverHeroIds()
    {
        if (!IsInNewGamePlus)
        {
            return System.Array.Empty<string>();
        }

        if (PartyManager.Instance == null)
        {
            return System.Array.Empty<string>();
        }

        HeroData[] active = PartyManager.Instance.GetActiveParty();
        if (active == null) return Array.Empty<string>();
        List<string> ids = new List<string>(active.Length);
        for (int i = 0; i < active.Length; i++)
        {
            if (active[i] != null && !string.IsNullOrEmpty(active[i].heroId))
            {
                ids.Add(active[i].heroId);
            }
        }
        return ids;
    }

    [Serializable]
    public sealed class RunSnapshot
    {
        public int completedRuns;
        public int loopIndex;
        public bool isInNewGamePlus;
        public bool completedCurrentRun;
    }

    public RunSnapshot CaptureSnapshot()
    {
        return new RunSnapshot
        {
            completedRuns = CompletedRuns,
            loopIndex = LoopIndex,
            isInNewGamePlus = IsInNewGamePlus,
            completedCurrentRun = completedCurrentRun
        };
    }

    /// <summary>Restores the saved run without rewriting account completion history.</summary>
    public void ApplySnapshot(RunSnapshot snapshot)
    {
        LoopIndex = 0;
        IsInNewGamePlus = false;
        completedCurrentRun = false;
        if (snapshot == null || snapshot.completedRuns < 0) return;
        int runs = Mathf.Max(CompletedRuns, snapshot.completedRuns);
        if (snapshot.loopIndex < 0 || snapshot.loopIndex > runs
            || (snapshot.isInNewGamePlus && snapshot.loopIndex == 0)
            || (snapshot.completedCurrentRun && runs == 0)) return;
        CompletedRuns = runs;
        LoopIndex = snapshot.loopIndex;
        IsInNewGamePlus = snapshot.isInNewGamePlus;
        completedCurrentRun = snapshot.completedCurrentRun;
    }

    /// <summary>Starts a normal run while retaining the account's completed-run history.</summary>
    public void ResetForNewRun()
    {
        IsInNewGamePlus = false;
        LoopIndex = 0;
        completedCurrentRun = false;
        Save();
    }

    public void ResetForDebug()
    {
        CompletedRuns = 0;
        ResetForNewRun();
    }

    private float GetBoundedMultiplier(float configuredMultiplier)
    {
        if (!IsInNewGamePlus) return 1f;
        float multiplier = float.IsNaN(configuredMultiplier) || float.IsInfinity(configuredMultiplier)
            ? 1f : Mathf.Clamp(configuredMultiplier, 1f, MaximumStatMultiplier);
        return (float)Math.Min(MaximumStatMultiplier, Math.Pow(multiplier, LoopIndex));
    }

    private void Save()
    {
        NgPlusSaveData data = new NgPlusSaveData
        {
            completedRuns = CompletedRuns,
            loopIndex = LoopIndex,
            isInNewGamePlus = IsInNewGamePlus,
            completedCurrentRun = completedCurrentRun
        };
        string json = JsonUtility.ToJson(data);
        try
        {
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
        catch (PlayerPrefsException exception)
        {
            Debug.LogWarning($"[NewGamePlusService] Save storage rejected the write: {exception.Message}");
        }
    }

    private void Load()
    {
        // Reset first: re-enabling a persistent component must not retain the
        // previous session's values when its save has been removed or damaged.
        CompletedRuns = 0;
        LoopIndex = 0;
        IsInNewGamePlus = false;
        completedCurrentRun = false;
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!SaveJsonValidator.TryReadObject(json, out Dictionary<string, SaveJsonValidator.Field> fields)
            || !fields.TryGetValue("completedRuns", out SaveJsonValidator.Field runsField)
            || !runsField.TryGetInt(out int runs) || runs < 0
            || !fields.TryGetValue("loopIndex", out SaveJsonValidator.Field loopField)
            || !loopField.TryGetInt(out int loop) || loop < 0 || loop > runs
            || !fields.TryGetValue("isInNewGamePlus", out SaveJsonValidator.Field activeField)
            || !activeField.TryGetBool(out bool active) || (active && loop == 0))
        {
            Debug.LogWarning("[NewGamePlusService] Invalid save ignored; starting with normal-run defaults.");
            return;
        }

        bool completed = !active && runs > 0; // Legacy normal-run completions can unlock NG+.
        if (fields.TryGetValue("completedCurrentRun", out SaveJsonValidator.Field completionField)
            && (!completionField.TryGetBool(out completed) || (completed && runs == 0)))
        {
            Debug.LogWarning("[NewGamePlusService] Invalid completion state ignored.");
            return;
        }

        CompletedRuns = runs;
        LoopIndex = loop;
        IsInNewGamePlus = active;
        completedCurrentRun = completed;
    }

    [System.Serializable]
    private struct NgPlusSaveData
    {
        public int completedRuns;
        public int loopIndex;
        public bool isInNewGamePlus;
        public bool completedCurrentRun;
    }
}
