using System;
using System.Collections.Generic;
using UnityEngine;

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

        int clamped = Mathf.Clamp(value, 0, 100);
        RelationshipTier previousTier = GetRelationshipTier(clamped);
        affinityByHeroId[heroId] = clamped;
        RelationshipTier newTier = GetRelationshipTier(clamped);

        OnAffinityChanged?.Invoke(heroId, clamped, newTier);
        if (previousTier != newTier)
        {
            OnTierChanged?.Invoke(heroId, previousTier, newTier);
        }
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
    }
}
