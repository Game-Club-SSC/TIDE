using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PowerBudgetTracker : MonoBehaviour
{
    public static PowerBudgetTracker Instance { get; private set; }

    [SerializeField, Min(0f)] private float defaultBudgetPerIsland = 3f;

    public event Action<string, float, float> OnBudgetChanged;

    private Dictionary<string, float> remainingBudgetByIslandId = new Dictionary<string, float>();

    public float DefaultBudgetPerIsland => defaultBudgetPerIsland;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SeedBudgets();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetBudget(string islandId, float amount)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return;
        }

        remainingBudgetByIslandId[islandId] = Mathf.Max(0f, amount);
        OnBudgetChanged?.Invoke(islandId, remainingBudgetByIslandId[islandId], 0f);
    }

    public float GetRemainingBudget(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return 0f;
        }

        return remainingBudgetByIslandId.TryGetValue(islandId, out float value) ? value : 0f;
    }

    public bool TryConsumeBudget(string islandId, float cost)
    {
        if (string.IsNullOrEmpty(islandId) || cost <= 0f)
        {
            return false;
        }

        if (!remainingBudgetByIslandId.TryGetValue(islandId, out float remaining))
        {
            remaining = defaultBudgetPerIsland;
            remainingBudgetByIslandId[islandId] = remaining;
        }

        if (remaining < cost)
        {
            return false;
        }

        remainingBudgetByIslandId[islandId] = remaining - cost;
        OnBudgetChanged?.Invoke(islandId, remaining - cost, -cost);
        return true;
    }

    public void RefundBudget(string islandId, float amount)
    {
        if (string.IsNullOrEmpty(islandId) || amount <= 0f)
        {
            return;
        }

        if (!remainingBudgetByIslandId.TryGetValue(islandId, out float current))
        {
            current = defaultBudgetPerIsland;
        }

        remainingBudgetByIslandId[islandId] = current + amount;
        OnBudgetChanged?.Invoke(islandId, current + amount, amount);
    }

    public void ResetBudget(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return;
        }

        remainingBudgetByIslandId[islandId] = defaultBudgetPerIsland;
        OnBudgetChanged?.Invoke(islandId, defaultBudgetPerIsland, 0f);
    }

    public void ResetAllBudgets()
    {
        SeedBudgets();
    }

    private void SeedBudgets()
    {
        IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < order.Count; i++)
        {
            string islandId = order[i];
            if (string.IsNullOrEmpty(islandId))
            {
                continue;
            }

            remainingBudgetByIslandId[islandId] = defaultBudgetPerIsland;
        }
    }

    public IReadOnlyDictionary<string, float> Snapshot()
    {
        return remainingBudgetByIslandId;
    }
}
