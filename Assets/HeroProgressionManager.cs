using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroProgressionManager : MonoBehaviour
{
    private const bool DefaultCosmeticProgressionEconomyEnabled = false;
    private const string CosmeticCurrencyHeroId = "__cosmetic_currency__";
    private const int GearXpPerBattleWin = 20;
    private static bool runtimeCosmeticProgressionEconomyEnabled = DefaultCosmeticProgressionEconomyEnabled;

    public static HeroProgressionManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelingConfig levelingConfig;

    [Header("Feature Gates")]
    [SerializeField] private bool enableCosmeticProgressionEconomy = DefaultCosmeticProgressionEconomyEnabled;

    [Header("Gear")]
    [SerializeField] private GearSetData[] availableGearSets = System.Array.Empty<GearSetData>();

    [Header("Smithy Economy")]
    [SerializeField] [Min(0)] private int baseSmithyDuplicateCost = 50;
    [SerializeField] [Min(0)] private int smithyDuplicateCostPerLevel = 30;

    public event Action<string, int> OnHeroLeveledUp;
    public event Action<string, int> OnXpGained;
    public event Action<string, GearSetData> OnGearChanged;
    public event Action<int> OnCosmeticXpChanged;
    public event Action<string> OnPlayerColorPresetUnlocked;
    public event Action<int> OnCurrencyChanged;
    public event Action<string, GearInstance> OnGearInstanceLevelUp;

    private Dictionary<string, HeroProgressionState> heroStates = new Dictionary<string, HeroProgressionState>();
    private HashSet<string> unlockedPlayerColorPresetIds = new HashSet<string>();
    private Dictionary<string, GearInstance> allGearInstances = new Dictionary<string, GearInstance>();
    private int currency;

    public LevelingConfig LevelingConfig => levelingConfig;
    public GearSetData[] AvailableGearSets => availableGearSets;
    public bool IsCosmeticProgressionEnabled => enableCosmeticProgressionEconomy;
    public static bool IsRuntimeCosmeticProgressionEconomyEnabled => runtimeCosmeticProgressionEconomyEnabled;
    public int Currency => currency;
    public int BaseSmithyDuplicateCost => baseSmithyDuplicateCost;
    public int SmithyDuplicateCostPerLevel => smithyDuplicateCostPerLevel;

    public static void ConfigureRuntimeCosmeticProgressionEconomy(bool isEnabled)
    {
        runtimeCosmeticProgressionEconomyEnabled = isEnabled;

        if (Instance != null)
        {
            Instance.enableCosmeticProgressionEconomy = runtimeCosmeticProgressionEconomyEnabled;
        }
    }

    public class HeroProgressionState
    {
        public string heroId;
        public int level = 1;
        public int currentXp = 0;
        public string equippedGearSetId;
        public string equippedGearInstanceId;

        public HeroProgressionState(string id)
        {
            heroId = id;
        }
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        enableCosmeticProgressionEconomy = runtimeCosmeticProgressionEconomyEnabled;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void EnsureHero(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        if (!heroStates.ContainsKey(heroId))
        {
            heroStates[heroId] = new HeroProgressionState(heroId);
        }
    }

    public HeroProgressionState GetState(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return null;
        }

        heroStates.TryGetValue(heroId, out HeroProgressionState state);
        return state;
    }

    public int GetLevel(string heroId)
    {
        HeroProgressionState state = GetState(heroId);
        return state != null ? state.level : 1;
    }

    public int GetXp(string heroId)
    {
        HeroProgressionState state = GetState(heroId);
        return state != null ? state.currentXp : 0;
    }

    public int GetXpToNextLevel(string heroId)
    {
        if (levelingConfig == null) return 0;
        int level = GetLevel(heroId);
        return levelingConfig.GetXpToNextLevel(level);
    }

    public GearInstance CreateGearInstance(GearSetData template)
    {
        if (template == null || !template.IsValid())
        {
            return null;
        }

        GearInstance instance = new GearInstance
        {
            instanceId = Guid.NewGuid().ToString(),
            setId = template.setId,
            template = template,
            level = 1,
            currentXp = 0,
            unlockedSlots = new List<GearSlotBonus>()
        };

        allGearInstances[instance.instanceId] = instance;
        return instance;
    }

    public void RegisterGearInstance(GearInstance instance)
    {
        if (instance == null || string.IsNullOrEmpty(instance.instanceId))
        {
            return;
        }

        if (string.IsNullOrEmpty(instance.setId) && instance.template != null)
        {
            instance.setId = instance.template.setId;
        }

        if (instance.template == null && !string.IsNullOrEmpty(instance.setId))
        {
            instance.template = FindGearSet(instance.setId);
        }

        instance.level = Mathf.Clamp(instance.level, 1, instance.MaxLevel);
        if (instance.unlockedSlots == null)
        {
            instance.unlockedSlots = new List<GearSlotBonus>();
        }

        allGearInstances[instance.instanceId] = instance;
    }

    public GearInstance GetGearInstance(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return null;
        }

        if (!allGearInstances.TryGetValue(instanceId, out GearInstance instance))
        {
            return null;
        }

        if (instance.template == null && !string.IsNullOrEmpty(instance.setId))
        {
            instance.template = FindGearSet(instance.setId);
        }

        return instance;
    }

    public List<GearInstance> GetAllGearInstances()
    {
        List<GearInstance> instances = new List<GearInstance>(allGearInstances.Values);
        instances.Sort((left, right) =>
        {
            string leftName = left != null ? left.setId : string.Empty;
            string rightName = right != null ? right.setId : string.Empty;
            int nameCompare = string.Compare(leftName, rightName, StringComparison.Ordinal);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            int leftLevel = left != null ? left.level : 0;
            int rightLevel = right != null ? right.level : 0;
            int levelCompare = rightLevel.CompareTo(leftLevel);
            if (levelCompare != 0)
            {
                return levelCompare;
            }

            string leftId = left != null ? left.instanceId : string.Empty;
            string rightId = right != null ? right.instanceId : string.Empty;
            return string.Compare(leftId, rightId, StringComparison.Ordinal);
        });
        return instances;
    }

    public void EquipGearInstance(string heroId, GearInstance instance)
    {
        if (string.IsNullOrEmpty(heroId) || instance == null)
        {
            return;
        }

        RegisterGearInstance(instance);
        if (instance.template == null)
        {
            return;
        }

        EnsureHero(heroId);
        ClearInstanceFromOtherHeroes(instance.instanceId, heroId);
        HeroProgressionState state = heroStates[heroId];
        state.equippedGearInstanceId = instance.instanceId;
        state.equippedGearSetId = instance.setId;

        OnGearChanged?.Invoke(heroId, instance.template);
        Debug.Log($"[HeroProgressionManager] {heroId} equipped gear instance '{instance.instanceId}' ({instance.setId}, Lv.{instance.level}).");
    }

    public GearInstance GetEquippedGearInstance(string heroId)
    {
        HeroProgressionState state = GetState(heroId);
        if (state == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(state.equippedGearInstanceId))
        {
            GearInstance equipped = GetGearInstance(state.equippedGearInstanceId);
            if (equipped != null)
            {
                return equipped;
            }

            state.equippedGearInstanceId = null;
        }

        if (string.IsNullOrEmpty(state.equippedGearSetId))
        {
            return null;
        }

        GearInstance fallback = FindFirstOwnedInstanceForSet(state.equippedGearSetId, heroId);
        if (fallback == null)
        {
            GearSetData template = FindGearSet(state.equippedGearSetId);
            fallback = CreateGearInstance(template);
        }

        if (fallback != null)
        {
            state.equippedGearInstanceId = fallback.instanceId;
            state.equippedGearSetId = fallback.setId;
        }

        return fallback;
    }

    public bool GrantGearXp(string heroId, int xpAmount)
    {
        if (string.IsNullOrEmpty(heroId) || xpAmount <= 0)
        {
            return false;
        }

        GearInstance instance = GetEquippedGearInstance(heroId);
        if (instance == null)
        {
            return false;
        }

        bool leveledUp = instance.GrantXp(xpAmount);
        if (leveledUp)
        {
            OnGearInstanceLevelUp?.Invoke(heroId, instance);
            Debug.Log($"[HeroProgressionManager] Gear for {heroId} leveled to Lv.{instance.level}. Slots {instance.UnlockedSlotCount}/{GearInstance.MaxBonusSlots}.");
        }

        return leveledUp;
    }

    public void GrantGearXpToAllEquipped(int xpAmount, HeroData[] activeHeroes, HeroData[] reserveHeroes)
    {
        if (xpAmount <= 0)
        {
            return;
        }

        float reserveMultiplier = levelingConfig != null
            ? levelingConfig.reserveXpMultiplier
            : 0.5f;
        int reserveGearXp = Mathf.RoundToInt(xpAmount * reserveMultiplier);

        if (activeHeroes != null)
        {
            for (int i = 0; i < activeHeroes.Length; i++)
            {
                if (activeHeroes[i] != null)
                {
                    GrantGearXp(activeHeroes[i].heroId, xpAmount);
                }
            }
        }

        if (reserveHeroes != null)
        {
            for (int i = 0; i < reserveHeroes.Length; i++)
            {
                if (reserveHeroes[i] != null)
                {
                    GrantGearXp(reserveHeroes[i].heroId, reserveGearXp);
                }
            }
        }
    }

    public void EquipGearSet(string heroId, GearSetData gearSet)
    {
        if (string.IsNullOrEmpty(heroId) || gearSet == null)
        {
            return;
        }

        GearInstance instance = FindFirstOwnedInstanceForSet(gearSet.setId, heroId);
        if (instance == null)
        {
            instance = CreateGearInstance(gearSet);
        }

        if (instance == null)
        {
            return;
        }

        EquipGearInstance(heroId, instance);
    }

    public void UnequipGearSet(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        HeroProgressionState state = GetState(heroId);
        if (state == null || state.equippedGearSetId == null)
        {
            return;
        }

        string oldSetId = state.equippedGearSetId;
        state.equippedGearSetId = null;
        state.equippedGearInstanceId = null;

        OnGearChanged?.Invoke(heroId, null);
        Debug.Log($"[HeroProgressionManager] {heroId} unequipped gear set '{oldSetId}'.");
    }

    public GearSetData GetEquippedGearSet(string heroId)
    {
        GearInstance equipped = GetEquippedGearInstance(heroId);
        if (equipped != null && equipped.template != null)
        {
            return equipped.template;
        }

        HeroProgressionState state = GetState(heroId);
        return state != null ? FindGearSet(state.equippedGearSetId) : null;
    }

    public float GetAttackBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        if (instance != null)
        {
            return instance.GetTotalAttackPercent();
        }

        GearSetData setData = GetEquippedGearSet(heroId);
        return setData != null ? setData.TotalAttackPercent : 0f;
    }

    public float GetDefenseBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        if (instance != null)
        {
            return instance.GetTotalDefensePercent();
        }

        GearSetData setData = GetEquippedGearSet(heroId);
        return setData != null ? setData.TotalDefensePercent : 0f;
    }

    public float GetHpBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        if (instance != null)
        {
            return instance.GetTotalHpPercent();
        }

        GearSetData setData = GetEquippedGearSet(heroId);
        return setData != null ? setData.TotalHpPercent : 0f;
    }

    private GearSetData FindGearSet(string setId)
    {
        if (string.IsNullOrEmpty(setId) || availableGearSets == null)
        {
            return null;
        }

        for (int i = 0; i < availableGearSets.Length; i++)
        {
            if (availableGearSets[i] != null && availableGearSets[i].setId == setId)
            {
                return availableGearSets[i];
            }
        }

        return null;
    }

    public void ApplyStatGrowth(CombatUnit unit, HeroData hero)
    {
        if (unit == null || hero == null || levelingConfig == null)
        {
            return;
        }

        int level = GetLevel(hero.heroId);
        int bonus = level - 1;

        int levelHp = hero.baseMaxHP + bonus * levelingConfig.hpPerLevel;
        int levelMp = hero.baseMaxMP + bonus * levelingConfig.mpPerLevel;
        int levelAttack = hero.baseAttack + bonus * levelingConfig.attackPerLevel;
        int levelDefense = hero.baseDefense + bonus * levelingConfig.defensePerLevel;
        int levelSpeed = hero.baseSpeed + bonus * levelingConfig.speedPerLevel;

        float atkPercent = GetAttackBonusPercent(hero.heroId);
        float defPercent = GetDefenseBonusPercent(hero.heroId);
        float hpPercent = GetHpBonusPercent(hero.heroId);

        unit.MaxHP = levelHp + Mathf.RoundToInt(levelHp * hpPercent);
        unit.HP = unit.MaxHP;
        unit.MaxMP = levelMp;
        unit.MP = unit.MaxMP;
        unit.Attack = levelAttack + Mathf.RoundToInt(levelAttack * atkPercent);
        unit.Defense = levelDefense + Mathf.RoundToInt(levelDefense * defPercent);
        unit.Speed = levelSpeed;

        GearSetData gear = GetEquippedGearSet(hero.heroId);
        string gearTag = gear != null ? $", Gear={gear.setId}" : "";
        Debug.Log($"[HeroProgressionManager] Applied stats for {hero.displayName} (Lv.{level}{gearTag}): HP={unit.MaxHP}, ATK={unit.Attack}, DEF={unit.Defense}");
    }

    public bool GrantXp(string heroId, int xpAmount)
    {
        if (string.IsNullOrEmpty(heroId) || xpAmount <= 0 || levelingConfig == null)
        {
            return false;
        }

        EnsureHero(heroId);
        HeroProgressionState state = heroStates[heroId];

        if (state.level >= levelingConfig.maxLevel)
        {
            return false;
        }

        state.currentXp += xpAmount;
        OnXpGained?.Invoke(heroId, xpAmount);

        bool leveledUp = false;
        while (state.level < levelingConfig.maxLevel)
        {
            int xpNeeded = levelingConfig.GetXpToNextLevel(state.level);
            if (state.currentXp >= xpNeeded)
            {
                state.currentXp -= xpNeeded;
                state.level++;
                leveledUp = true;
                OnHeroLeveledUp?.Invoke(heroId, state.level);
                Debug.Log($"[HeroProgressionManager] {heroId} leveled up to {state.level}!");
            }
            else
            {
                break;
            }
        }

        return leveledUp;
    }

    public int GetCosmeticXp()
    {
        if (!enableCosmeticProgressionEconomy)
        {
            return 0;
        }

        HeroProgressionState state = GetState(CosmeticCurrencyHeroId);
        return state != null ? Mathf.Max(0, state.currentXp) : 0;
    }

    public void GrantCosmeticXp(int xpAmount)
    {
        if (!enableCosmeticProgressionEconomy)
        {
            return;
        }

        int amount = Mathf.Max(0, xpAmount);
        if (amount <= 0)
        {
            return;
        }

        EnsureHero(CosmeticCurrencyHeroId);
        HeroProgressionState state = heroStates[CosmeticCurrencyHeroId];
        state.currentXp += amount;
        state.level = 1;
        OnCosmeticXpChanged?.Invoke(state.currentXp);
    }

    public bool TrySpendCosmeticXp(int xpCost)
    {
        if (!enableCosmeticProgressionEconomy)
        {
            return true;
        }

        int cost = Mathf.Max(0, xpCost);
        if (cost <= 0)
        {
            return true;
        }

        EnsureHero(CosmeticCurrencyHeroId);
        HeroProgressionState state = heroStates[CosmeticCurrencyHeroId];
        if (state.currentXp < cost)
        {
            return false;
        }

        state.currentXp -= cost;
        state.level = 1;
        OnCosmeticXpChanged?.Invoke(state.currentXp);
        return true;
    }

    public bool IsPlayerColorPresetUnlocked(string presetId)
    {
        if (string.IsNullOrEmpty(presetId))
        {
            return false;
        }

        if (!enableCosmeticProgressionEconomy)
        {
            return true;
        }

        return unlockedPlayerColorPresetIds.Contains(presetId);
    }

    public bool TryUnlockPlayerColorPreset(string presetId, int xpCost)
    {
        if (string.IsNullOrEmpty(presetId))
        {
            return false;
        }

        if (!enableCosmeticProgressionEconomy)
        {
            return true;
        }

        if (IsPlayerColorPresetUnlocked(presetId))
        {
            return true;
        }

        if (!TrySpendCosmeticXp(xpCost))
        {
            return false;
        }

        unlockedPlayerColorPresetIds.Add(presetId);
        OnPlayerColorPresetUnlocked?.Invoke(presetId);
        return true;
    }

    public void GrantBattleXp(int totalXp, HeroData[] activeHeroes, HeroData[] reserveHeroes)
    {
        if (levelingConfig == null || totalXp <= 0)
        {
            return;
        }

        if (enableCosmeticProgressionEconomy)
        {
            GrantCosmeticXp(totalXp);
        }

        AddCurrency(Mathf.Max(1, totalXp / 2));

        int reserveXp = Mathf.RoundToInt(totalXp * levelingConfig.reserveXpMultiplier);
        int reserveGearXp = Mathf.RoundToInt(GearXpPerBattleWin * levelingConfig.reserveXpMultiplier);

        if (activeHeroes != null)
        {
            for (int i = 0; i < activeHeroes.Length; i++)
            {
                if (activeHeroes[i] != null)
                {
                    GrantXp(activeHeroes[i].heroId, totalXp);
                    GrantGearXp(activeHeroes[i].heroId, GearXpPerBattleWin);
                }
            }
        }

        if (reserveHeroes != null)
        {
            for (int i = 0; i < reserveHeroes.Length; i++)
            {
                if (reserveHeroes[i] != null)
                {
                    GrantXp(reserveHeroes[i].heroId, reserveXp);
                    GrantGearXp(reserveHeroes[i].heroId, reserveGearXp);
                }
            }
        }
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currency += amount;
        OnCurrencyChanged?.Invoke(currency);
    }

    public bool TrySpendCurrency(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (currency < cost)
        {
            return false;
        }

        currency -= cost;
        OnCurrencyChanged?.Invoke(currency);
        return true;
    }

    public void SetCurrency(int amount)
    {
        currency = Mathf.Max(0, amount);
        OnCurrencyChanged?.Invoke(currency);
    }

    public int GetGearDuplicateCost(GearInstance instance)
    {
        int level = instance != null
            ? Mathf.Clamp(instance.level, 1, instance.MaxLevel)
            : 1;
        return Mathf.Max(0, baseSmithyDuplicateCost + (level - 1) * smithyDuplicateCostPerLevel);
    }

    public GearProgressionSaveData CaptureGearSnapshot()
    {
        GearProgressionSaveData snapshot = new GearProgressionSaveData
        {
            currency = currency
        };

        foreach (GearInstance instance in allGearInstances.Values)
        {
            if (instance == null || string.IsNullOrEmpty(instance.setId))
            {
                continue;
            }

            snapshot.instances.Add(instance.ToSaveData());
        }

        foreach (KeyValuePair<string, HeroProgressionState> pair in heroStates)
        {
            HeroProgressionState state = pair.Value;
            if (state == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(state.equippedGearInstanceId) && !string.IsNullOrEmpty(state.equippedGearSetId))
            {
                GearInstance fallback = FindFirstOwnedInstanceForSet(state.equippedGearSetId, pair.Key);
                if (fallback == null)
                {
                    fallback = CreateGearInstance(FindGearSet(state.equippedGearSetId));
                }

                if (fallback != null)
                {
                    state.equippedGearInstanceId = fallback.instanceId;
                }
            }

            if (string.IsNullOrEmpty(state.equippedGearInstanceId))
            {
                continue;
            }

            snapshot.heroIds.Add(pair.Key);
            snapshot.equippedInstanceIds.Add(state.equippedGearInstanceId);
        }

        return snapshot;
    }

    public void ApplyGearSnapshot(GearProgressionSaveData snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        allGearInstances.Clear();
        currency = Mathf.Max(0, snapshot.currency);

        if (snapshot.instances != null)
        {
            for (int i = 0; i < snapshot.instances.Count; i++)
            {
                GearInstance loaded = GearInstance.FromSaveData(snapshot.instances[i], availableGearSets);
                if (loaded != null)
                {
                    RegisterGearInstance(loaded);
                }
            }
        }

        foreach (HeroProgressionState state in heroStates.Values)
        {
            if (state == null)
            {
                continue;
            }

            state.equippedGearInstanceId = null;
            state.equippedGearSetId = null;
        }

        if (snapshot.heroIds != null && snapshot.equippedInstanceIds != null)
        {
            int count = Mathf.Min(snapshot.heroIds.Count, snapshot.equippedInstanceIds.Count);
            for (int i = 0; i < count; i++)
            {
                string heroId = snapshot.heroIds[i];
                string instanceId = snapshot.equippedInstanceIds[i];
                if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(instanceId))
                {
                    continue;
                }

                GearInstance equipped = GetGearInstance(instanceId);
                if (equipped == null)
                {
                    continue;
                }

                EnsureHero(heroId);
                HeroProgressionState state = heroStates[heroId];
                state.equippedGearInstanceId = equipped.instanceId;
                state.equippedGearSetId = equipped.setId;
            }
        }

        OnCurrencyChanged?.Invoke(currency);
    }

    private GearInstance FindFirstOwnedInstanceForSet(string setId, string requestingHeroId = null)
    {
        if (string.IsNullOrEmpty(setId))
        {
            return null;
        }

        foreach (GearInstance instance in allGearInstances.Values)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.setId == setId)
            {
                if (!string.IsNullOrEmpty(requestingHeroId)
                    && IsInstanceEquippedByAnotherHero(instance.instanceId, requestingHeroId))
                {
                    continue;
                }

                if (instance.template == null)
                {
                    instance.template = FindGearSet(instance.setId);
                }

                return instance;
            }
        }

        return null;
    }

    private bool IsInstanceEquippedByAnotherHero(string instanceId, string requestingHeroId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        foreach (KeyValuePair<string, HeroProgressionState> pair in heroStates)
        {
            if (pair.Key == requestingHeroId || pair.Value == null)
            {
                continue;
            }

            if (pair.Value.equippedGearInstanceId == instanceId)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearInstanceFromOtherHeroes(string instanceId, string exceptHeroId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return;
        }

        foreach (KeyValuePair<string, HeroProgressionState> pair in heroStates)
        {
            if (pair.Key == exceptHeroId || pair.Value == null)
            {
                continue;
            }

            HeroProgressionState state = pair.Value;
            if (state.equippedGearInstanceId != instanceId)
            {
                continue;
            }

            string oldSetId = state.equippedGearSetId;
            state.equippedGearInstanceId = null;
            state.equippedGearSetId = null;
            OnGearChanged?.Invoke(pair.Key, null);
            Debug.Log($"[HeroProgressionManager] Moved gear instance '{instanceId}' from {pair.Key} to {exceptHeroId} (previous set {oldSetId}).");
        }
    }

    public int GetTotalXpFromEnemies(BattleManager battleManager)
    {
        if (battleManager == null)
        {
            return 0;
        }

        int total = 0;
        IReadOnlyList<CombatUnit> enemies = battleManager.EnemyUnits;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                total += enemies[i].XpReward;
            }
        }

        return total;
    }

    public void ResetProgressionForDebug()
    {
        heroStates.Clear();
        unlockedPlayerColorPresetIds.Clear();
        allGearInstances.Clear();
        currency = 0;
        OnCurrencyChanged?.Invoke(currency);
        OnCosmeticXpChanged?.Invoke(GetCosmeticXp());
    }

    public void MaxOutHeroForDebug(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        EnsureHero(heroId);
        HeroProgressionState state = heroStates[heroId];
        if (levelingConfig != null)
        {
            state.level = Mathf.Max(1, levelingConfig.maxLevel);
        }
        else
        {
            state.level = Mathf.Max(state.level, 99);
        }

        state.currentXp = 0;
    }

    public void MaxOutGearForDebug(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return;
        }

        GearInstance instance = GetEquippedGearInstance(heroId);
        if (instance == null)
        {
            return;
        }

        while (instance.level < instance.MaxLevel)
        {
            int needed = Mathf.Max(1, instance.GetXpToNextLevel());
            instance.GrantXp(needed);
        }
    }
}
