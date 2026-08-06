using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which TideBreaks each hero has unlocked.
/// When a hero levels up, check if any new TideBreaks are now available
/// based on element match and level requirement.
/// </summary>
[DisallowMultipleComponent]
public class TideBreakProgressionManager : MonoBehaviour
{
    public static TideBreakProgressionManager Instance { get; private set; }

    /// <summary>Fired when a TideBreak is unlocked for a hero.</summary>
    public event Action<string, TideBreakData> OnTideBreakUnlocked;

    [Serializable]
    public class HeroTideBreakState
    {
        public string heroId;
        public List<string> unlockedAbilityNames = new List<string>();
    }

    [Serializable]
    public class TideBreakSaveData
    {
        public List<HeroTideBreakSaveEntry> heroEntries = new List<HeroTideBreakSaveEntry>();
    }

    [Serializable]
    public class HeroTideBreakSaveEntry
    {
        public string heroId;
        public List<string> unlockedAbilityNames = new List<string>();
    }

    private readonly Dictionary<string, HeroTideBreakState> heroStates =
        new Dictionary<string, HeroTideBreakState>();

    private TideBreakData[] allTideBreaks;

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

    /// <summary>
    /// Returns the list of TideBreaks currently unlocked for the given hero.
    /// </summary>
    public List<TideBreakData> GetUnlockedTideBreaks(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return new List<TideBreakData>();
        }

        EnsureState(heroId);
        HeroTideBreakState state = heroStates[heroId];
        List<TideBreakData> result = new List<TideBreakData>();

        TideBreakData[] all = GetAllTideBreaks();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            if (state.unlockedAbilityNames.Contains(all[i].abilityName))
            {
                result.Add(all[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns every TideBreak that the hero could potentially unlock
    /// (matches their element or has no element requirement), including
    /// already-unlocked and still-locked ones.
    /// </summary>
    public List<TideBreakData> GetAllAvailableTideBreaks(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return new List<TideBreakData>();
        }

        string heroElement = ResolveHeroElement(heroId);
        TideBreakData[] all = GetAllTideBreaks();
        List<TideBreakData> result = new List<TideBreakData>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(all[i].requiredHeroElement)
                || string.Equals(all[i].requiredHeroElement, heroElement, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(all[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true if the hero has the named TideBreak unlocked.
    /// </summary>
    public bool HasTideBreak(string heroId, string abilityName)
    {
        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(abilityName))
        {
            return false;
        }

        EnsureState(heroId);
        return heroStates[heroId].unlockedAbilityNames.Contains(abilityName);
    }

    /// <summary>
    /// Call when a hero levels up. Checks every TideBreak for that hero
    /// and unlocks any that are now eligible.
    /// </summary>
    public void OnHeroLevelUp(string heroId, int newLevel)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        EnsureState(heroId);
        HeroTideBreakState state = heroStates[heroId];
        string heroElement = ResolveHeroElement(heroId);
        TideBreakData[] all = GetAllTideBreaks();

        for (int i = 0; i < all.Length; i++)
        {
            TideBreakData tb = all[i];
            if (tb == null)
            {
                continue;
            }

            if (state.unlockedAbilityNames.Contains(tb.abilityName))
            {
                continue;
            }

            if (newLevel < tb.unlockLevel)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tb.requiredHeroElement)
                && !string.Equals(tb.requiredHeroElement, heroElement, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            state.unlockedAbilityNames.Add(tb.abilityName);
            Debug.Log($"[TideBreakProgressionManager] {heroId} unlocked TideBreak '{tb.abilityName}' at level {newLevel}.");
            OnTideBreakUnlocked?.Invoke(heroId, tb);
        }
    }

    /// <summary>
    /// Manually reveal a hidden TideBreak (e.g. when an ancient text is found).
    /// </summary>
    public bool RevealHiddenTideBreak(string heroId, string abilityName)
    {
        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(abilityName))
        {
            return false;
        }

        EnsureState(heroId);
        HeroTideBreakState state = heroStates[heroId];

        if (state.unlockedAbilityNames.Contains(abilityName))
        {
            return false;
        }

        TideBreakData[] all = GetAllTideBreaks();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null
                && all[i].isHidden
                && string.Equals(all[i].abilityName, abilityName, StringComparison.Ordinal))
            {
                state.unlockedAbilityNames.Add(abilityName);
                Debug.Log($"[TideBreakProgressionManager] {heroId} revealed hidden TideBreak '{abilityName}'.");
                OnTideBreakUnlocked?.Invoke(heroId, all[i]);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Force-unlocks a TideBreak for a hero by ability name, regardless of the
    /// hidden flag, level requirement, or element match. Used by authored
    /// dialogue UnlockTideBreak effects (issue #297) and by the dialogueState
    /// save section on load. Returns false when the ability is already unlocked
    /// or does not exist in the catalog.
    /// </summary>
    public bool UnlockTideBreak(string heroId, string abilityName)
    {
        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(abilityName))
        {
            return false;
        }

        EnsureState(heroId);
        HeroTideBreakState state = heroStates[heroId];

        if (state.unlockedAbilityNames.Contains(abilityName))
        {
            return false;
        }

        TideBreakData[] all = GetAllTideBreaks();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && string.Equals(all[i].abilityName, abilityName, StringComparison.Ordinal))
            {
                state.unlockedAbilityNames.Add(abilityName);
                Debug.Log($"[TideBreakProgressionManager] {heroId} unlocked TideBreak '{abilityName}' (dialogue effect).");
                OnTideBreakUnlocked?.Invoke(heroId, all[i]);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Unlocks all non-hidden TideBreaks up to the given level for a hero.
    /// Useful when loading a save or for debug/testing.
    /// </summary>
    public void UnlockAllUpToLevel(string heroId, int level)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        EnsureState(heroId);
        string heroElement = ResolveHeroElement(heroId);
        TideBreakData[] all = GetAllTideBreaks();

        for (int i = 0; i < all.Length; i++)
        {
            TideBreakData tb = all[i];
            if (tb == null || tb.isHidden)
            {
                continue;
            }

            if (tb.unlockLevel > level)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(tb.requiredHeroElement)
                && !string.Equals(tb.requiredHeroElement, heroElement, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!heroStates[heroId].unlockedAbilityNames.Contains(tb.abilityName))
            {
                heroStates[heroId].unlockedAbilityNames.Add(tb.abilityName);
                OnTideBreakUnlocked?.Invoke(heroId, tb);
            }
        }
    }

    public TideBreakSaveData GetSaveData()
    {
        TideBreakSaveData saveData = new TideBreakSaveData();

        foreach (KeyValuePair<string, HeroTideBreakState> pair in heroStates)
        {
            HeroTideBreakSaveEntry entry = new HeroTideBreakSaveEntry
            {
                heroId = pair.Key,
                unlockedAbilityNames = new List<string>(pair.Value.unlockedAbilityNames)
            };
            saveData.heroEntries.Add(entry);
        }

        return saveData;
    }

    public void ApplySaveData(TideBreakSaveData saveData)
    {
        heroStates.Clear();

        if (saveData == null || saveData.heroEntries == null)
        {
            return;
        }

        for (int i = 0; i < saveData.heroEntries.Count; i++)
        {
            HeroTideBreakSaveEntry entry = saveData.heroEntries[i];
            if (entry == null || string.IsNullOrEmpty(entry.heroId))
            {
                continue;
            }

            HeroTideBreakState state = new HeroTideBreakState
            {
                heroId = entry.heroId
            };

            if (entry.unlockedAbilityNames != null)
            {
                state.unlockedAbilityNames = new List<string>(entry.unlockedAbilityNames);
            }

            heroStates[entry.heroId] = state;
        }

        Debug.Log($"[TideBreakProgressionManager] Loaded save data for {heroStates.Count} hero(es).");
    }

    public void ResetProgressionForDebug()
    {
        heroStates.Clear();
    }

    private void EnsureState(string heroId)
    {
        if (!heroStates.ContainsKey(heroId))
        {
            heroStates[heroId] = new HeroTideBreakState { heroId = heroId };
        }
    }

    private string ResolveHeroElement(string heroId)
    {
        if (PartyManager.Instance == null)
        {
            return string.Empty;
        }

        HeroData hero = PartyManager.Instance.GetHero(heroId);
        if (hero == null)
        {
            return string.Empty;
        }

        CombatUnit.Element element = PartyManager.Instance.ResolveElement(hero);
        return element.ToString();
    }

    private TideBreakData[] GetAllTideBreaks()
    {
        if (allTideBreaks == null || allTideBreaks.Length == 0)
        {
            allTideBreaks = Resources.LoadAll<TideBreakData>("TideBreakData");
        }

        return allTideBreaks ?? Array.Empty<TideBreakData>();
    }
}
