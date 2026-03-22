using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroProgressionManager : MonoBehaviour
{
    public static HeroProgressionManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelingConfig levelingConfig;

    public event Action<string, int> OnHeroLeveledUp;
    public event Action<string, int> OnXpGained;

    private Dictionary<string, HeroProgressionState> heroStates = new Dictionary<string, HeroProgressionState>();

    public LevelingConfig LevelingConfig => levelingConfig;

    public class HeroProgressionState
    {
        public string heroId;
        public int level = 1;
        public int currentXp = 0;

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

    public void ApplyStatGrowth(CombatUnit unit, HeroData hero)
    {
        if (unit == null || hero == null || levelingConfig == null)
        {
            return;
        }

        int level = GetLevel(hero.heroId);
        if (level <= 1)
        {
            return;
        }

        int bonus = level - 1;
        unit.MaxHP = hero.baseMaxHP + bonus * levelingConfig.hpPerLevel;
        unit.HP = unit.MaxHP;
        unit.MaxMP = hero.baseMaxMP + bonus * levelingConfig.mpPerLevel;
        unit.MP = unit.MaxMP;
        unit.Attack = hero.baseAttack + bonus * levelingConfig.attackPerLevel;
        unit.Defense = hero.baseDefense + bonus * levelingConfig.defensePerLevel;
        unit.Speed = hero.baseSpeed + bonus * levelingConfig.speedPerLevel;

        Debug.Log($"[HeroProgressionManager] Applied stat growth for {hero.displayName} (Lv.{level}): HP={unit.MaxHP}, ATK={unit.Attack}, DEF={unit.Defense}");
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

    public void GrantBattleXp(int totalXp, HeroData[] activeHeroes, HeroData[] reserveHeroes)
    {
        if (levelingConfig == null || totalXp <= 0)
        {
            return;
        }

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
