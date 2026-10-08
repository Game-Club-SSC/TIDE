using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RelationshipAffinitySnapshot
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
        SetAffinity(heroId, (int)Math.Max(0L, Math.Min(100L, (long)current + delta)));
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
        return new Dictionary<string, int>(affinityByHeroId, StringComparer.Ordinal);
    }

    public RelationshipAffinitySnapshot CaptureSnapshot()
    {
        RelationshipAffinitySnapshot snapshot = new RelationshipAffinitySnapshot();
        List<string> ids = new List<string>(affinityByHeroId.Keys);
        ids.Sort(StringComparer.Ordinal);
        for (int i = 0; i < ids.Count; i++)
        {
            snapshot.heroIds.Add(ids[i]);
            snapshot.affinities.Add(Mathf.Clamp(affinityByHeroId[ids[i]], 0, 100));
        }
        return snapshot;
    }

    /// <summary>Replaces affinity from the authoritative world save without writing a sidecar during load.</summary>
    public void ApplySnapshot(RelationshipAffinitySnapshot snapshot)
    {
        affinityByHeroId.Clear();
        if (snapshot == null || snapshot.heroIds == null || snapshot.affinities == null)
        {
            return;
        }

        int count = Mathf.Min(snapshot.heroIds.Count, snapshot.affinities.Count);
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.heroIds[i]))
            {
                affinityByHeroId[snapshot.heroIds[i]] = Mathf.Clamp(snapshot.affinities[i], 0, 100);
            }
        }
    }

    public void ResetForDebug()
    {
        affinityByHeroId.Clear();
        SaveAffinityToPrefs();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SaveAffinityToPrefs();
        }
    }

    private void SaveAffinityToPrefs()
    {
        RelationshipAffinitySnapshot data = CaptureSnapshot();
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

        if (!SaveJsonValidator.TryReadObject(json, out Dictionary<string, SaveJsonValidator.Field> fields)
            || !fields.TryGetValue("heroIds", out SaveJsonValidator.Field heroIds) || heroIds.Kind != '['
            || !fields.TryGetValue("affinities", out SaveJsonValidator.Field affinities) || affinities.Kind != '[')
        {
            Debug.LogWarning("[RelationshipTracker] Ignored invalid legacy affinity save data.");
            return;
        }

        RelationshipAffinitySnapshot data;
        // External persisted data can be valid JSON but have incompatible
        // member types. Keep this exception boundary at the deserializer.
        try
        {
            data = JsonUtility.FromJson<RelationshipAffinitySnapshot>(json);
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("[RelationshipTracker] Ignored incompatible legacy affinity save data.");
            return;
        }
        if (data == null || data.heroIds == null || data.affinities == null)
        {
            return;
        }

        ApplySnapshot(data);

        Debug.Log($"[RelationshipTracker] Restored {affinityByHeroId.Count} affinity entries from PlayerPrefs.");
    }
}
