using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
internal class RelationshipAffinityData
{
    public List<string> heroIds = new List<string>();
    public List<int> affinities = new List<int>();
}

[DisallowMultipleComponent]
public class RelationshipTracker : MonoBehaviour
{
    public enum RelationshipTier
    {
        Stranger,
        Acquaintance,
        Friend,
        Close,
        Bonded
    }

    public static RelationshipTracker Instance { get; private set; }

    [SerializeField, Range(0, 100)] private int strangerThreshold;
    [SerializeField, Range(0, 100)] private int acquaintanceThreshold = 20;
    [SerializeField, Range(0, 100)] private int friendThreshold = 40;
    [SerializeField, Range(0, 100)] private int closeThreshold = 60;
    [SerializeField, Range(0, 100)] private int bondedThreshold = 80;

    public event Action<string, int, RelationshipTier> OnAffinityChanged;
    public event Action<string, RelationshipTier, RelationshipTier> OnTierChanged;

    private const string AffinityPlayerPrefsKey = "RelationshipTracker_AffinityData";

    private Dictionary<string, int> affinityByHeroId = new Dictionary<string, int>();

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RestoreAffinityFromPrefs();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetAffinity(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return 0;
        }

        return affinityByHeroId.TryGetValue(heroId, out int value) ? value : 0;
    }

    public void SetAffinity(string heroId, int value)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        RelationshipTier previousTier = GetRelationshipTier(heroId);
        int clamped = Mathf.Clamp(value, 0, 100);
        affinityByHeroId[heroId] = clamped;
        RelationshipTier newTier = GetRelationshipTier(heroId);

        OnAffinityChanged?.Invoke(heroId, clamped, newTier);
        if (previousTier != newTier)
        {
            OnTierChanged?.Invoke(heroId, previousTier, newTier);
        }

        SaveAffinityToPrefs();
    }

    public void AdjustAffinity(string heroId, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        int current = GetAffinity(heroId);
        SetAffinity(heroId, current + delta);
    }

    public RelationshipTier GetRelationshipTier(string heroId)
    {
        return GetRelationshipTier(GetAffinity(heroId));
    }

    public RelationshipTier GetRelationshipTier(int affinityValue)
    {
        if (affinityValue >= bondedThreshold)
        {
            return RelationshipTier.Bonded;
        }

        if (affinityValue >= closeThreshold)
        {
            return RelationshipTier.Close;
        }

        if (affinityValue >= friendThreshold)
        {
            return RelationshipTier.Friend;
        }

        if (affinityValue >= acquaintanceThreshold)
        {
            return RelationshipTier.Acquaintance;
        }

        return RelationshipTier.Stranger;
    }

    public IReadOnlyDictionary<string, int> Snapshot()
    {
        return affinityByHeroId;
    }

    public void ResetForDebug()
    {
        affinityByHeroId.Clear();
        SaveAffinityToPrefs();
    }

    private void OnDisable()
    {
        SaveAffinityToPrefs();
    }

    private void SaveAffinityToPrefs()
    {
        RelationshipAffinityData data = new RelationshipAffinityData();
        foreach (var kvp in affinityByHeroId)
        {
            data.heroIds.Add(kvp.Key);
            data.affinities.Add(kvp.Value);
        }
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(AffinityPlayerPrefsKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[RelationshipTracker] Saved {affinityByHeroId.Count} affinity entries to PlayerPrefs.");
    }

    private void RestoreAffinityFromPrefs()
    {
        if (!PlayerPrefs.HasKey(AffinityPlayerPrefsKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(AffinityPlayerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        RelationshipAffinityData data = JsonUtility.FromJson<RelationshipAffinityData>(json);
        if (data == null || data.heroIds == null || data.affinities == null)
        {
            return;
        }

        affinityByHeroId.Clear();
        int count = Mathf.Min(data.heroIds.Count, data.affinities.Count);
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrEmpty(data.heroIds[i]))
            {
                affinityByHeroId[data.heroIds[i]] = data.affinities[i];
            }
        }

        Debug.Log($"[RelationshipTracker] Restored {affinityByHeroId.Count} affinity entries from PlayerPrefs.");
    }
}
