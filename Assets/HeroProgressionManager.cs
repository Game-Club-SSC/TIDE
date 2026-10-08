using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroProgressionManager : MonoBehaviour
{
    private const bool DefaultCosmeticProgressionEconomyEnabled = false;
    private const string CosmeticCurrencyHeroId = "__cosmetic_currency__";
    [SerializeField] [Min(0)] private int gearXpPerBattleWin = 20;
    private static bool runtimeCosmeticProgressionEconomyEnabled = DefaultCosmeticProgressionEconomyEnabled;

    public static HeroProgressionManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelingConfig levelingConfig;
    [SerializeField] private BalanceConfig balanceConfig;

    [Header("Feature Gates")]
    [SerializeField] private bool enableCosmeticProgressionEconomy = DefaultCosmeticProgressionEconomyEnabled;

    [Header("Gear")]
    [SerializeField] private GearSetData[] availableGearSets = System.Array.Empty<GearSetData>();
    [SerializeField] private bool autoPopulateStarterGear = true;

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
        EnsureStarterGearRegistry();
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public void EnsureStarterGearRegistry()
    {
        if (!autoPopulateStarterGear)
        {
            return;
        }

        bool hasAny = availableGearSets != null && availableGearSets.Length > 0 && availableGearSets[0] != null;
        if (hasAny)
        {
            return;
        }

        List<GearSetData> starter = new List<GearSetData>(GearSetFactory.CreateStarterGearSets());
        availableGearSets = starter.ToArray();
        Debug.Log($"[HeroProgressionManager] Auto-populated {availableGearSets.Length} starter gear sets.");
    }

    public void RegisterGearSet(GearSetData gear)
    {
        if (gear == null || !gear.IsValid())
        {
            return;
        }

        if (availableGearSets == null)
        {
            availableGearSets = new GearSetData[] { gear };
            return;
        }

        for (int i = 0; i < availableGearSets.Length; i++)
        {
            if (availableGearSets[i] != null && availableGearSets[i].setId == gear.setId)
            {
                return;
            }
        }

        List<GearSetData> updated = new List<GearSetData>(availableGearSets) { gear };
        availableGearSets = updated.ToArray();
    }

    public GearSetData GetGearSetForHero(string heroId)
    {
        HeroData hero = HeroDatabase.FindHeroById(heroId);
        if (hero == null)
        {
            return GetGearSetForElement(CombatUnit.Element.Earth);
        }

        CombatUnit.Element element = PartyManager.Instance != null
            ? PartyManager.Instance.ResolveElement(hero)
            : hero.element;
        return GetGearSetForElement(element);
    }

    public GearSetData GetGearSetForElement(CombatUnit.Element element)
    {
        if (availableGearSets == null)
        {
            return null;
        }

        GearSetData bestMatch = null;
        for (int i = 0; i < availableGearSets.Length; i++)
        {
            GearSetData set = availableGearSets[i];
            if (set == null || !set.MatchesElement(element))
            {
                continue;
            }

            if (bestMatch == null || set.tier < bestMatch.tier)
            {
                bestMatch = set;
            }
        }

        return bestMatch;
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

    public SkillData[] GetUnlockedNormalSkills(HeroData hero)
    {
        if (hero == null)
        {
            return Array.Empty<SkillData>();
        }

        return hero.GetSkillsForLevel(GetLevel(hero.heroId));
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
        int reserveGearXp = CalculateReserveXp(xpAmount, reserveMultiplier);

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

    public float GetMpBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        return instance != null ? instance.GetTotalMpPercent() : 0f;
    }

    public float GetSpeedBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        return instance != null ? instance.GetTotalSpeedPercent() : 0f;
    }

    public float GetCritRateBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        return instance != null ? instance.GetTotalCritRatePercent() : 0f;
    }

    public float GetCritDamageBonusPercent(string heroId)
    {
        GearInstance instance = GetEquippedGearInstance(heroId);
        return instance != null ? instance.GetTotalCritDamagePercent() : 0f;
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
        if (unit == null || hero == null)
        {
            return;
        }

        int level = GetLevel(hero.heroId);

        int levelHp;
        int levelMp;
        int levelAttack;
        int levelDefense;
        int levelSpeed;

        if (balanceConfig != null)
        {
            BalanceConfig.StatGrowth growth = balanceConfig.GetCumulativeStatGrowth(level);
            levelHp = hero.baseMaxHP + growth.hp;
            levelMp = hero.baseMaxMP + growth.mp;
            levelAttack = hero.baseAttack + growth.attack;
            levelDefense = hero.baseDefense + growth.defense;
            levelSpeed = hero.baseSpeed + growth.speed;
        }
        else if (levelingConfig != null)
        {
            int bonus = level - 1;
            levelHp = hero.baseMaxHP + bonus * levelingConfig.hpPerLevel;
            levelMp = hero.baseMaxMP + bonus * levelingConfig.mpPerLevel;
            levelAttack = hero.baseAttack + bonus * levelingConfig.attackPerLevel;
            levelDefense = hero.baseDefense + bonus * levelingConfig.defensePerLevel;
            levelSpeed = hero.baseSpeed + bonus * levelingConfig.speedPerLevel;
        }
        else
        {
            return;
        }

        float atkPercent = GetAttackBonusPercent(hero.heroId);
        float defPercent = GetDefenseBonusPercent(hero.heroId);
        float hpPercent = GetHpBonusPercent(hero.heroId);
        float mpPercent = GetMpBonusPercent(hero.heroId);
        float speedPercent = GetSpeedBonusPercent(hero.heroId);
        float critRatePercent = GetCritRateBonusPercent(hero.heroId);
        float critDamagePercent = GetCritDamageBonusPercent(hero.heroId);

        unit.MaxHP = levelHp + Mathf.RoundToInt(levelHp * hpPercent);
        unit.HP = unit.MaxHP;
        unit.MaxMP = levelMp + Mathf.RoundToInt(levelMp * mpPercent);
        unit.MP = unit.MaxMP;
        unit.Attack = levelAttack + Mathf.RoundToInt(levelAttack * atkPercent);
        unit.Defense = levelDefense + Mathf.RoundToInt(levelDefense * defPercent);
        unit.Speed = levelSpeed + Mathf.RoundToInt(levelSpeed * speedPercent);
        unit.CritRate = hero.baseCritRate + critRatePercent;
        unit.CritDamage = hero.baseCritDamage + critDamagePercent;

        GearSetData gear = GetEquippedGearSet(hero.heroId);
        string gearTag = gear != null ? $", Gear={gear.setId}" : "";
        Debug.Log($"[HeroProgressionManager] Applied stats for {hero.displayName} (Lv.{level}{gearTag}): HP={unit.MaxHP}, ATK={unit.Attack}, DEF={unit.Defense}");
    }

    /// <summary>
    /// Adds XP to a hero. Returns true when the hero leveled up as a result.
    /// </summary>
    public bool GrantXp(string heroId, int xpAmount)
    {
        TryGrantXp(heroId, xpAmount, out bool leveledUp);
        return leveledUp;
    }

    /// <summary>
    /// Adds XP to a hero. <paramref name="applied"/> reports whether the XP was
    /// actually granted, which is what durable reward ledgers need: a level-up
    /// is not required for success, and treating a non-level-up as failure
    /// makes the ledger re-apply the same reward forever.
    /// </summary>
    public bool TryGrantXp(string heroId, int xpAmount, out bool leveledUpOut)
    {
        leveledUpOut = false;

        if (string.IsNullOrEmpty(heroId) || xpAmount <= 0 || levelingConfig == null)
        {
            return false;
        }

        EnsureHero(heroId);
        HeroProgressionState state = heroStates[heroId];

        if (state.level >= levelingConfig.maxLevel)
        {
            // An already-capped hero consumed this reward successfully. A
            // durable dialogue ledger must not retry it in a later run.
            return true;
        }

        long accumulatedXp = (long)Mathf.Max(0, state.currentXp) + xpAmount;
        state.currentXp = accumulatedXp > int.MaxValue ? int.MaxValue : (int)accumulatedXp;
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
            }
            else
            {
                break;
            }
        }

        if (state.level >= levelingConfig.maxLevel)
        {
            state.currentXp = 0;
        }

        if (leveledUp)
        {
            Debug.Log($"[HeroProgressionManager] {heroId} leveled up to {state.level}!");
        }

        leveledUpOut = leveledUp;
        return true;
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
        state.currentXp = (int)Math.Min(int.MaxValue, (long)Mathf.Max(0, state.currentXp) + amount);
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

        NewGamePlusService ngPlus = NewGamePlusService.Instance;
        DifficultyModeService difficulty = DifficultyModeService.Instance;
        double xpMultiplier = (ngPlus != null ? ngPlus.GetXpMultiplier() : 1f)
            * (double)(difficulty != null ? difficulty.GetXpMultiplier() : 1f);
        if (double.IsNaN(xpMultiplier) || double.IsInfinity(xpMultiplier) || xpMultiplier < 0d)
        {
            xpMultiplier = 1d;
        }
        totalXp = (int)Math.Min(int.MaxValue, Math.Round(totalXp * xpMultiplier));

        if (enableCosmeticProgressionEconomy)
        {
            GrantCosmeticXp(totalXp);
        }

        int currencyMult = difficulty != null ? difficulty.GetCurrencyMultiplier() : 1;
        AddCurrency((int)Math.Min(int.MaxValue, (long)Mathf.Max(0, totalXp / 2) * Mathf.Max(0, currencyMult)));

        int reserveXp = CalculateReserveXp(totalXp, levelingConfig.reserveXpMultiplier);
        int reserveGearXp = CalculateReserveXp(gearXpPerBattleWin, levelingConfig.reserveXpMultiplier);

        HashSet<string> rewardedHeroIds = new HashSet<string>(StringComparer.Ordinal);

        if (activeHeroes != null)
        {
            for (int i = 0; i < activeHeroes.Length; i++)
            {
                if (activeHeroes[i] != null
                    && !string.IsNullOrEmpty(activeHeroes[i].heroId)
                    && rewardedHeroIds.Add(activeHeroes[i].heroId))
                {
                    GrantXp(activeHeroes[i].heroId, totalXp);
                    GrantGearXp(activeHeroes[i].heroId, gearXpPerBattleWin);
                }
            }
        }

        if (reserveHeroes != null)
        {
            for (int i = 0; i < reserveHeroes.Length; i++)
            {
                if (reserveHeroes[i] != null
                    && !string.IsNullOrEmpty(reserveHeroes[i].heroId)
                    && rewardedHeroIds.Add(reserveHeroes[i].heroId))
                {
                    GrantXp(reserveHeroes[i].heroId, reserveXp);
                    GrantGearXp(reserveHeroes[i].heroId, reserveGearXp);
                }
            }
        }
    }

    private static int CalculateReserveXp(int xpAmount, float configuredMultiplier)
    {
        if (xpAmount <= 0 || float.IsNaN(configuredMultiplier) || float.IsInfinity(configuredMultiplier)) return 0;
        double multiplier = Math.Max(0d, Math.Min(1d, configuredMultiplier));
        return (int)Math.Min(int.MaxValue, Math.Round(xpAmount * multiplier));
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currency = (int)Math.Min(int.MaxValue, (long)Mathf.Max(0, currency) + amount);
        OnCurrencyChanged?.Invoke(currency);
        PersistCurrency();
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
        PersistCurrency();
        return true;
    }

    public void SetCurrency(int amount)
    {
        currency = Mathf.Max(0, amount);
        OnCurrencyChanged?.Invoke(currency);
        PersistCurrency();
    }

    private void PersistCurrency()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveWorldState();
        }
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

        // Legacy equipped-set migration above may create an instance. Capture
        // inventory after migration so the saved equipment reference resolves.
        foreach (GearInstance instance in allGearInstances.Values)
        {
            if (instance != null && !string.IsNullOrEmpty(instance.setId))
            {
                snapshot.instances.Add(instance.ToSaveData());
            }
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
            HashSet<string> assignedInstances = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> assignedHeroes = new HashSet<string>(StringComparer.Ordinal);
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
                if (equipped == null || assignedInstances.Contains(instanceId) || assignedHeroes.Contains(heroId))
                {
                    continue;
                }
                assignedInstances.Add(instanceId);
                assignedHeroes.Add(heroId);

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

        long total = 0;
        // BUGFIX: Guard against null EnemyUnits to prevent NRE when the
        // battle manager exists but hasn't initialized its enemy list yet.
        IReadOnlyList<CombatUnit> enemies = battleManager.EnemyUnits;
        if (enemies == null)
        {
            return 0;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                total += Mathf.Max(0, enemies[i].XpReward);
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }
        }

        return (int)total;
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

    [Serializable]
    public sealed class HeroProgressionSnapshot
    {
        public List<string> heroIds = new List<string>();
        public List<int> levels = new List<int>();
        public List<int> currentXpValues = new List<int>();
        public List<string> unlockedPlayerColorPresetIds = new List<string>();
    }

    [Serializable]
    public sealed class PartyCompositionSnapshot
    {
        public List<string> activeHeroIds = new List<string>();
        public List<string> reserveHeroIds = new List<string>();
    }

    public HeroProgressionSnapshot CaptureHeroProgressionSnapshot()
    {
        HeroProgressionSnapshot snapshot = new HeroProgressionSnapshot();
        foreach (KeyValuePair<string, HeroProgressionState> pair in heroStates)
        {
            if (pair.Value == null || string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            snapshot.heroIds.Add(pair.Key);
            snapshot.levels.Add(Mathf.Max(1, pair.Value.level));
            snapshot.currentXpValues.Add(Mathf.Max(0, pair.Value.currentXp));
        }

        foreach (string presetId in unlockedPlayerColorPresetIds)
        {
            if (!string.IsNullOrEmpty(presetId))
            {
                snapshot.unlockedPlayerColorPresetIds.Add(presetId);
            }
        }

        return snapshot;
    }

    public void ApplyHeroProgressionSnapshot(HeroProgressionSnapshot snapshot)
    {
        if (snapshot == null || snapshot.heroIds == null || snapshot.levels == null)
        {
            return;
        }

        int count = Mathf.Min(snapshot.heroIds.Count, snapshot.levels.Count);
        int xpCount = snapshot.currentXpValues != null ? snapshot.currentXpValues.Count : 0;

        for (int i = 0; i < count; i++)
        {
            string heroId = snapshot.heroIds[i];
            if (string.IsNullOrEmpty(heroId))
            {
                continue;
            }

            EnsureHero(heroId);
            HeroProgressionState state = heroStates[heroId];

            // The cosmetic sentinel stores a balance, not hero XP. Running it
            // through the hero levelling curve clamped it to a single-level
            // window, silently destroying funds the player had already earned.
            if (heroId == CosmeticCurrencyHeroId)
            {
                state.currentXp = Mathf.Max(0, i < xpCount ? snapshot.currentXpValues[i] : 0);
                continue;
            }

            int maxLevel = levelingConfig != null ? Mathf.Max(1, levelingConfig.maxLevel) : int.MaxValue;
            state.level = Mathf.Clamp(snapshot.levels[i], 1, maxLevel);
            if (i < xpCount)
            {
                state.currentXp = Mathf.Max(0, snapshot.currentXpValues[i]);
            }
            else
            {
                state.currentXp = 0;
            }

            if (levelingConfig != null)
            {
                if (state.level >= maxLevel)
                {
                    state.currentXp = 0;
                }
                else
                {
                    int xpToNextLevel = Mathf.Max(1, levelingConfig.GetXpToNextLevel(state.level));
                    state.currentXp = Mathf.Clamp(state.currentXp, 0, xpToNextLevel - 1);
                }
            }
        }

        if (snapshot.unlockedPlayerColorPresetIds != null)
        {
            unlockedPlayerColorPresetIds.Clear();
            for (int i = 0; i < snapshot.unlockedPlayerColorPresetIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(snapshot.unlockedPlayerColorPresetIds[i]))
                {
                    unlockedPlayerColorPresetIds.Add(snapshot.unlockedPlayerColorPresetIds[i]);
                }
            }
        }
    }

    public PartyCompositionSnapshot CapturePartyCompositionSnapshot()
    {
        PartyCompositionSnapshot snapshot = new PartyCompositionSnapshot();
        PartyManager partyManager = PartyManager.Instance;
        if (partyManager == null || partyManager.PartyData == null) return snapshot;
        AppendHeroIds(partyManager.GetActiveParty(), snapshot.activeHeroIds);
        AppendHeroIds(partyManager.GetReserveParty(), snapshot.reserveHeroIds);
        return snapshot;
    }

    public void ApplyPartyCompositionSnapshot(PartyCompositionSnapshot snapshot)
    {
        if (snapshot == null) return;
        PartyManager partyManager = PartyManager.Instance;
        if (partyManager == null || partyManager.PartyData == null) return;

        PartyData partyData = partyManager.PartyData;
        partyData.EnsureSlotLayout();
        int activeCount = PartyData.ActiveSlotCount;
        int reserveCount = PartyData.ReserveSlotCount;
        HeroData[] fallback = new HeroData[activeCount + reserveCount];
        Array.Copy(partyData.activeSlots, 0, fallback, 0, activeCount);
        Array.Copy(partyData.reserveSlots, 0, fallback, activeCount, reserveCount);

        // Resolve both rosters together so a stale-id fallback cannot duplicate
        // a hero which the save places in another active or reserve slot.
        List<string> savedIds = new List<string>(fallback.Length);
        AppendSavedSlots(snapshot.activeHeroIds, partyData.activeSlots, activeCount, savedIds);
        AppendSavedSlots(snapshot.reserveHeroIds, partyData.reserveSlots, reserveCount, savedIds);
        HeroData[] restored = ResolveHeroDataSlots(savedIds, fallback.Length, fallback);
        partyData.activeSlots = new HeroData[activeCount];
        partyData.reserveSlots = new HeroData[reserveCount];
        Array.Copy(restored, 0, partyData.activeSlots, 0, activeCount);
        Array.Copy(restored, activeCount, partyData.reserveSlots, 0, reserveCount);
    }

    private static void AppendHeroIds(HeroData[] heroes, List<string> target)
    {
        if (heroes == null || target == null) return;
        for (int i = 0; i < heroes.Length; i++)
        {
            // Empty markers retain interior holes instead of shifting every
            // later hero left. Older compact lists still restore at the start.
            target.Add(heroes[i] != null ? heroes[i].heroId ?? string.Empty : string.Empty);
        }
    }

    private static void AppendSavedSlots(List<string> ids, HeroData[] fallback, int count, List<string> target)
    {
        for (int i = 0; i < count; i++)
        {
            string id = ids == null
                ? (fallback[i] != null ? fallback[i].heroId : string.Empty)
                : i < ids.Count ? ids[i] : string.Empty;
            target.Add(id ?? string.Empty);
        }
    }

    private static HeroData[] ResolveHeroDataSlots(List<string> ids, int slotLimit, HeroData[] fallback)
    {
        int length = Mathf.Max(0, slotLimit);
        HeroData[] slots = new HeroData[length];
        bool[] needsFallback = new bool[length];
        HashSet<string> claimed = new HashSet<string>(StringComparer.Ordinal);

        // Claim all resolvable saved heroes first. A fallback for an earlier
        // unknown id may not steal a hero which resolves in a later slot.
        for (int i = 0; i < length; i++)
        {
            string id = ids != null
                ? (i < ids.Count ? ids[i] : string.Empty)
                : (fallback != null && i < fallback.Length && fallback[i] != null ? fallback[i].heroId : string.Empty);
            if (string.IsNullOrEmpty(id)) continue;
            HeroData hero = HeroDatabase.FindHeroById(id);
            if (hero != null && !string.IsNullOrEmpty(hero.heroId) && claimed.Add(hero.heroId))
            {
                slots[i] = hero;
            }
            else
            {
                needsFallback[i] = true;
            }
        }

        for (int i = 0; i < length; i++)
        {
            if (!needsFallback[i] || fallback == null) continue;
            HeroData preferred = i < fallback.Length ? fallback[i] : null;
            if (TryClaimFallback(preferred, claimed))
            {
                slots[i] = preferred;
                continue;
            }
            for (int candidate = 0; candidate < fallback.Length; candidate++)
            {
                if (TryClaimFallback(fallback[candidate], claimed))
                {
                    slots[i] = fallback[candidate];
                    break;
                }
            }
        }
        return slots;
    }

    private static bool TryClaimFallback(HeroData hero, HashSet<string> claimed)
    {
        return hero != null && !string.IsNullOrEmpty(hero.heroId) && claimed.Add(hero.heroId);
    }
}
