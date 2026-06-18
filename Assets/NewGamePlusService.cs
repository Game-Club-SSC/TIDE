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

    public bool CanStartNewGamePlus()
    {
        return CompletedRuns >= minCompletedRuns;
    }

    public void RegisterCompletion()
    {
        CompletedRuns++;
        Debug.Log($"[NewGamePlusService] Registered completion #{CompletedRuns}.");
    }

    public bool StartNewGamePlus()
    {
        if (!CanStartNewGamePlus())
        {
            return false;
        }

        IsInNewGamePlus = true;
        LoopIndex++;
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
        OnNewGamePlusEnded?.Invoke();
    }

    public float GetEnemyStatMultiplier()
    {
        return IsInNewGamePlus ? Mathf.Pow(ngPlusEnemyMultiplier, LoopIndex) : 1f;
    }

    public float GetXpMultiplier()
    {
        return IsInNewGamePlus ? Mathf.Pow(ngPlusXpMultiplier, LoopIndex) : 1f;
    }

    public int GetStartingCurrency()
    {
        return IsInNewGamePlus ? startingCurrency * LoopIndex : 0;
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

    public void ResetForDebug()
    {
        IsInNewGamePlus = false;
        LoopIndex = 0;
        CompletedRuns = 0;
    }
}
