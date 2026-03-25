using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroProgressionManager : MonoBehaviour
{
    private const string CosmeticCurrencyHeroId = "__cosmetic_currency__";

    public static HeroProgressionManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelingConfig levelingConfig;

    [Header("Gear")]
    [SerializeField] private GearSetData[] availableGearSets = System.Array.Empty<GearSetData>();

    public event Action<string, int> OnHeroLeveledUp;
    public event Action<string, int> OnXpGained;
    public event Action<string, GearSetData> OnGearChanged;
    public event Action<int> OnCosmeticXpChanged;
    public event Action<string> OnPlayerColorPresetUnlocked;

    private Dictionary<string, HeroProgressionState> heroStates = new Dictionary<string, HeroProgressionState>();
    private HashSet<string> unlockedPlayerColorPresetIds = new HashSet<string>();

    public LevelingConfig LevelingConfig => levelingConfig;
    public GearSetData[] AvailableGearSets => availableGearSets;

    public class HeroProgressionState
    {
        public string heroId;
        public int level = 1;
        public int currentXp = 0;
        public string equippedGearSetId;

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

    public void EquipGearSet(string heroId, GearSetData gearSet)
    {
        if (string.IsNullOrEmpty(heroId) || gearSet == null)
        {
            return;
        }

        EnsureHero(heroId);
        HeroProgressionState state = heroStates[heroId];
        state.equippedGearSetId = gearSet.setId;

        OnGearChanged?.Invoke(heroId, gearSet);
        Debug.Log($"[HeroProgressionManager] {heroId} equipped gear set '{gearSet.displayName}'.");
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

        OnGearChanged?.Invoke(heroId, null);
        Debug.Log($"[HeroProgressionManager] {heroId} unequipped gear set '{oldSetId}'.");
    }

    public GearSetData GetEquippedGearSet(string heroId)
    {
        HeroProgressionState state = GetState(heroId);
        if (state == null || string.IsNullOrEmpty(state.equippedGearSetId))
        {
            return null;
        }

        return FindGearSet(state.equippedGearSetId);
    }

    public float GetAttackBonusPercent(string heroId)
    {
        GearSetData gear = GetEquippedGearSet(heroId);
        return gear != null ? gear.TotalAttackPercent : 0f;
    }

    public float GetDefenseBonusPercent(string heroId)
    {
        GearSetData gear = GetEquippedGearSet(heroId);
        return gear != null ? gear.TotalDefensePercent : 0f;
    }

    public float GetHpBonusPercent(string heroId)
    {
        GearSetData gear = GetEquippedGearSet(heroId);
        return gear != null ? gear.TotalHpPercent : 0f;
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
        HeroProgressionState state = GetState(CosmeticCurrencyHeroId);
        return state != null ? Mathf.Max(0, state.currentXp) : 0;
    }

    public void GrantCosmeticXp(int xpAmount)
    {
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

        return unlockedPlayerColorPresetIds.Contains(presetId);
    }

    public bool TryUnlockPlayerColorPreset(string presetId, int xpCost)
    {
        if (string.IsNullOrEmpty(presetId))
        {
            return false;
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

        GrantCosmeticXp(totalXp);

        int reserveXp = Mathf.RoundToInt(totalXp * levelingConfig.reserveXpMultiplier);

        if (activeHeroes != null)
        {
            for (int i = 0; i < activeHeroes.Length; i++)
            {
                if (activeHeroes[i] != null)
                {
                    GrantXp(activeHeroes[i].heroId, totalXp);
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
                }
            }
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
}
