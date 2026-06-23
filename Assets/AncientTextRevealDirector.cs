using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the progressive revelation of ancient texts that tell the story
/// of the 100-year cycle -- how enemies return every century, how the
/// chosen heroes are not truly human, and the price of maintaining balance.
/// </summary>
[DisallowMultipleComponent]
public class AncientTextRevealDirector : MonoBehaviour
{
    public static AncientTextRevealDirector Instance { get; private set; }

    [Serializable]
    public class AncientTextFragment
    {
        [Tooltip("Unique key for persistence (e.g. 'cycle_fragment_1').")]
        public string fragmentId;

        [Tooltip("Display title shown to the player.")]
        public string title;

        [TextArea(4, 12)]
        [Tooltip("The text content of this fragment.")]
        public string body;

        [Tooltip("Minimum island progress index required to find this fragment (0-based).")]
        public int requiredIslandIndex;

        [Tooltip("Minimum island restoration percent to find this fragment (0-100).")]
        public float requiredRestorationPercent;

        [Tooltip("Hero whose ancestor wrote this fragment. Used for bonding updates.")]
        public string relatedHeroId;
    }

    /// <summary>
    /// Simple record of hero bonding levels, raised when fragments are discovered.
    /// No dedicated bonding system exists yet; this is a placeholder that other
    /// systems can query or subscribe to.
    /// </summary>
    [Serializable]
    public class HeroBondingState
    {
        public string heroId;
        public int bondLevel;
    }

    [Serializable]
    private class RevealDirectorSaveData
    {
        public List<string> discoveredFragmentIds = new List<string>();
        public List<HeroBondingSaveEntry> bondingEntries = new List<HeroBondingSaveEntry>();
    }

    [Serializable]
    private class HeroBondingSaveEntry
    {
        public string heroId;
        public int bondLevel;
    }

    public event Action<AncientTextFragment> OnFragmentDiscovered;
    public event Action<string, int> OnHeroBondLevelChanged;

    [Header("100-Year Cycle Fragments")]
    [SerializeField] private AncientTextFragment[] fragments = Array.Empty<AncientTextFragment>();

    [Header("Configuration")]
    [Tooltip("Bond level increase per fragment discovered for the related hero.")]
    [SerializeField] private int bondLevelPerFragment = 1;

    private readonly HashSet<string> discoveredFragmentIds = new HashSet<string>();
    private readonly Dictionary<string, int> heroBondLevels = new Dictionary<string, int>();

    public IReadOnlyCollection<string> DiscoveredFragmentIds => discoveredFragmentIds;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Call during gameplay to check if any new fragments are now eligible
    /// for discovery based on the player's current island and restoration.
    /// </summary>
    public void CheckForNewReveals(string currentIslandId, float restorationPercent)
    {
        if (fragments == null || fragments.Length == 0)
        {
            return;
        }

        int currentIndex = ResolveIslandIndex(currentIslandId);

        for (int i = 0; i < fragments.Length; i++)
        {
            AncientTextFragment fragment = fragments[i];
            if (fragment == null)
            {
                continue;
            }

            if (discoveredFragmentIds.Contains(fragment.fragmentId))
            {
                continue;
            }

            if (currentIndex < fragment.requiredIslandIndex)
            {
                continue;
            }

            if (restorationPercent < fragment.requiredRestorationPercent)
            {
                continue;
            }

            DiscoverFragment(fragment);
        }
    }

    /// <summary>
    /// Forces discovery of a specific fragment by id. Returns true if newly discovered.
    /// </summary>
    public bool ForceDiscoverFragment(string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId))
        {
            return false;
        }

        if (discoveredFragmentIds.Contains(fragmentId))
        {
            return false;
        }

        AncientTextFragment fragment = FindFragment(fragmentId);
        if (fragment == null)
        {
            Debug.LogWarning($"[AncientTextRevealDirector] Fragment '{fragmentId}' not found in configuration.");
            return false;
        }

        DiscoverFragment(fragment);
        return true;
    }

    /// <summary>
    /// Returns all fragments the player has discovered so far, in order.
    /// </summary>
    public List<AncientTextFragment> GetDiscoveredFragments()
    {
        List<AncientTextFragment> result = new List<AncientTextFragment>();
        if (fragments == null)
        {
            return result;
        }

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] != null && discoveredFragmentIds.Contains(fragments[i].fragmentId))
            {
                result.Add(fragments[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns 0-5 representing how much the player knows about the 100-year cycle.
    /// 0 = nothing known, 5 = full revelation.
    /// </summary>
    public int GetRevealStage()
    {
        return discoveredFragmentIds.Count;
    }

    /// <summary>
    /// Returns a human-readable description of the player's current knowledge
    /// about the 100-year cycle.
    /// </summary>
    public string GetOverallNarrativeState()
    {
        int stage = GetRevealStage();

        switch (stage)
        {
            case 0:
                return "The nature of the cycle remains completely unknown.";
            case 1:
                return "You have heard vague whispers of heroes who came before, but little is understood.";
            case 2:
                return "The texts hint at a recurring cycle, but its true meaning remains unclear.";
            case 3:
                return "A troubling truth emerges -- past heroes perished after fulfilling their purpose.";
            case 4:
                return "The revelation is staggering: the chosen heroes are not truly human.";
            case 5:
                return "The full truth is known. The cycle is eternal, and its price is absolute.";
            default:
                return "The nature of the cycle remains completely unknown.";
        }
    }

    /// <summary>
    /// Returns the bonding state for a specific hero, or null if none exists.
    /// </summary>
    public HeroBondingState GetHeroBonding(string heroId)
    {
        if (string.IsNullOrEmpty(heroId) || !heroBondLevels.ContainsKey(heroId))
        {
            return null;
        }

        return new HeroBondingState
        {
            heroId = heroId,
            bondLevel = heroBondLevels[heroId]
        };
    }

    /// <summary>
    /// Returns bonding states for all heroes that have any bond level.
    /// </summary>
    public List<HeroBondingState> GetAllHeroBonding()
    {
        List<HeroBondingState> result = new List<HeroBondingState>();
        foreach (KeyValuePair<string, int> pair in heroBondLevels)
        {
            result.Add(new HeroBondingState
            {
                heroId = pair.Key,
                bondLevel = pair.Value
            });
        }

        return result;
    }

    public RevealDirectorSaveData CaptureSaveData()
    {
        RevealDirectorSaveData saveData = new RevealDirectorSaveData();
        saveData.discoveredFragmentIds = new List<string>(discoveredFragmentIds);

        foreach (KeyValuePair<string, int> pair in heroBondLevels)
        {
            saveData.bondingEntries.Add(new HeroBondingSaveEntry
            {
                heroId = pair.Key,
                bondLevel = pair.Value
            });
        }

        return saveData;
    }

    public void ApplySaveData(RevealDirectorSaveData saveData)
    {
        discoveredFragmentIds.Clear();
        heroBondLevels.Clear();

        if (saveData == null)
        {
            return;
        }

        if (saveData.discoveredFragmentIds != null)
        {
            for (int i = 0; i < saveData.discoveredFragmentIds.Count; i++)
            {
                string id = saveData.discoveredFragmentIds[i];
                if (!string.IsNullOrEmpty(id))
                {
                    discoveredFragmentIds.Add(id);
                }
            }
        }

        if (saveData.bondingEntries != null)
        {
            for (int i = 0; i < saveData.bondingEntries.Count; i++)
            {
                HeroBondingSaveEntry entry = saveData.bondingEntries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.heroId))
                {
                    heroBondLevels[entry.heroId] = entry.bondLevel;
                }
            }
        }

        Debug.Log($"[AncientTextRevealDirector] Loaded save data: {discoveredFragmentIds.Count} fragments, {heroBondLevels.Count} hero bonds.");
    }

    public void ResetForDebug()
    {
        discoveredFragmentIds.Clear();
        heroBondLevels.Clear();
    }

    private void DiscoverFragment(AncientTextFragment fragment)
    {
        if (fragment == null)
        {
            return;
        }

        discoveredFragmentIds.Add(fragment.fragmentId);

        Debug.Log($"[AncientTextRevealDirector] Fragment discovered: '{fragment.fragmentId}' - {fragment.title}");

        // Register with GameStateManager
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.RegisterAncientText(fragment.fragmentId, fragment.title, fragment.body);
            gsm.DiscoverAncientText(fragment.fragmentId);
        }

        // Show via AncientTextLogUI
        AncientTextLogUI logUi = FindFirstObjectByType<AncientTextLogUI>();
        if (logUi == null)
        {
            GameObject logObject = new GameObject("AncientTextLogUI");
            logUi = logObject.AddComponent<AncientTextLogUI>();
        }

        if (logUi != null)
        {
            logUi.ShowEntry(fragment.fragmentId, fragment.title, fragment.body, true);
        }

        // Update hero bonding
        if (!string.IsNullOrEmpty(fragment.relatedHeroId))
        {
            UpdateHeroBonding(fragment.relatedHeroId);
        }

        // Trigger narrative beat
        TriggerNarrativeBeatForFragment(fragment);

        OnFragmentDiscovered?.Invoke(fragment);
    }

    private void UpdateHeroBonding(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        if (!heroBondLevels.TryGetValue(heroId, out int currentLevel))
        {
            currentLevel = 0;
        }

        int newLevel = currentLevel + Mathf.Max(1, bondLevelPerFragment);
        heroBondLevels[heroId] = newLevel;

        Debug.Log($"[AncientTextRevealDirector] {heroId} bond level: {currentLevel} -> {newLevel}");
        OnHeroBondLevelChanged?.Invoke(heroId, newLevel);
    }

    private void TriggerNarrativeBeatForFragment(AncientTextFragment fragment)
    {
        NarrativeBeatDirector director = FindFirstObjectByType<NarrativeBeatDirector>();
        if (director == null)
        {
            return;
        }

        // The NarrativeBeatDirector shows beats via ShowBeat, but it is private.
        // Instead we rely on the fragment already being registered and discovered
        // in GameStateManager, and the AncientTextLogUI display above handles
        // the player-facing presentation. The beat director will pick up the
        // state change on its next Update tick.
        Debug.Log($"[AncientTextRevealDirector] Fragment '{fragment.fragmentId}' registered for narrative progression.");
    }

    private int ResolveIslandIndex(string islandId)
    {
        if (IslandProgressionManager.Instance != null)
        {
            return IslandProgressionManager.Instance.GetIslandProgressIndex(islandId);
        }

        return -1;
    }

    private AncientTextFragment FindFragment(string fragmentId)
    {
        if (fragments == null || string.IsNullOrEmpty(fragmentId))
        {
            return null;
        }

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] != null
                && string.Equals(fragments[i].fragmentId, fragmentId, StringComparison.Ordinal))
            {
                return fragments[i];
            }
        }

        return null;
    }
}
