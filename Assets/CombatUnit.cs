using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all combat units (players and enemies) in the battle system.
/// Contains core combat statistics and basic functionality.
/// </summary>
[DisallowMultipleComponent]
public class CombatUnit : MonoBehaviour
{
    // Core combat stats
    [Header("Combat Stats")]
    [SerializeField] protected int maxHp = 100;
    [SerializeField] protected int hp = 100;
    [SerializeField] protected int maxMp = 50;
    [SerializeField] protected int mp = 50;
    [SerializeField] protected int attack = 10;
    [SerializeField] protected int defense = 5;
    [SerializeField] protected int speed = 10;
    [SerializeField] [Range(0f,1f)] protected float critRate = 0.05f;
    [SerializeField] protected float critDamage = 1.5f;
    [SerializeField] protected Element element = Element.None;

    [Header("Skills")]
    [SerializeField] protected SkillData[] skills = System.Array.Empty<SkillData>();
    private SkillData[] skillsViewSource = System.Array.Empty<SkillData>();
    private IReadOnlyList<SkillData> readOnlySkills = Array.AsReadOnly(System.Array.Empty<SkillData>());

    [Header("TideBreak Abilities")]
    [SerializeField] private List<TideBreakData> tideBreakAbilities = new List<TideBreakData>();
    public IReadOnlyList<TideBreakData> TideBreakAbilities => GetOrCreateTideBreakAbilities().AsReadOnly();
    public void AddTideBreak(TideBreakData tb)
    {
        if (tb == null)
        {
            Debug.LogWarning("[CombatUnit] Ignored null TideBreak ability.");
            return;
        }

        GetOrCreateTideBreakAbilities().Add(tb);
    }

    public void SetTideBreaks(List<TideBreakData> tbs)
    {
        if (tbs == null)
        {
            tideBreakAbilities = new List<TideBreakData>();
            return;
        }

        List<TideBreakData> defensiveCopy = new List<TideBreakData>(tbs.Count);
        for (int i = 0; i < tbs.Count; i++)
        {
            TideBreakData tideBreak = tbs[i];
            if (tideBreak != null)
            {
                defensiveCopy.Add(tideBreak);
            }
        }

        tideBreakAbilities = defensiveCopy;
    }

    // Unit state
    [Header("Unit State")]
    [SerializeField] protected string unitName = "Combat Unit";
    [SerializeField] protected UnitType unitType = UnitType.Ally;
    [SerializeField] protected bool isAlive = true;
    [SerializeField] protected bool isDefending = false;
    public bool SkipTurnThisRound { get; set; }

    // XP reward (set on enemies)
    [Header("XP")]
    [SerializeField] protected int xpReward;

    internal int DebugHP { set => hp = value; }
    internal int DebugMaxHP { set => maxHp = value; }
    internal int DebugMP { set => mp = value; }
    internal int DebugMaxMP { set => maxMp = value; }
    internal int DebugDefense { set => defense = value; }
    internal float DebugCritRate { set => critRate = value; }
    internal float DebugCritDamage { set => critDamage = value; }
    internal bool DebugIsAlive { set => isAlive = value; }
    internal int DebugXpReward { set => xpReward = value; }
    internal int DebugSpeed { set => speed = value; }

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects.AsReadOnly();

    /// <summary>
    /// Type of combat unit (ally or enemy).
    /// </summary>
    public enum UnitType
    {
        Ally,
        Enemy
    }

    /// <summary>
    /// Elements that units can possess, affecting interactions.
    /// </summary>
    public enum Element
    {
        None,
        Fire,
        Water,
        Earth,
        Air,
        Space
    }

    #region Properties
    public int HP
    {
        get => hp;
        set
        {
            hp = Mathf.Clamp(value, 0, maxHp);
            if (hp <= 0 && isAlive)
            {
                Die();
            }
        }
    }
    public int MaxHP
    {
        get => maxHp;
        set
        {
            maxHp = Mathf.Max(1, value);
            hp = Mathf.Clamp(hp, 0, maxHp);
        }
    }
    public int MP
    {
        get => mp;
        set => mp = Mathf.Clamp(value, 0, maxMp);
    }
    public int MaxMP
    {
        get => maxMp;
        set
        {
            maxMp = Mathf.Max(0, value);
            mp = Mathf.Clamp(mp, 0, maxMp);
        }
    }
    public int Attack { get => attack; set => attack = value; }
    public int Defense { get => defense; set => defense = value; }
    public int Speed { get => speed; set => speed = value; }
    public float CritRate { get => critRate; set => critRate = value; }
    public float CritDamage { get => critDamage; set => critDamage = value; }
    public Element ElementType
    {
        get => element;
        set => element = value;
    }
    public bool IsAlive => isAlive;
    public string UnitName { get => unitName; set => unitName = value; }
    public UnitType Type
    {
        get => unitType;
        set => unitType = value;
    }
    public IReadOnlyList<SkillData> Skills
    {
        get
        {
            EnsureSkillsInitialized();
            return readOnlySkills;
        }
    }
    public int XpReward { get => xpReward; set => xpReward = value; }
    public bool IsDefending => isDefending;

    public bool CanUseSkill(SkillData skill)
    {
        return isAlive && skill != null && mp >= skill.mpCost;
    }

    public void SetSkills(SkillData[] newSkills)
    {
        skills = newSkills == null || newSkills.Length == 0
            ? System.Array.Empty<SkillData>()
            : (SkillData[])newSkills.Clone();
        RefreshReadOnlySkills();
    }
    #endregion

    private void EnsureSkillsInitialized()
    {
        if (skills == null)
        {
            skills = System.Array.Empty<SkillData>();
        }

        if (!ReferenceEquals(skillsViewSource, skills))
        {
            RefreshReadOnlySkills();
        }
    }

    private void RefreshReadOnlySkills()
    {
        skillsViewSource = skills ?? System.Array.Empty<SkillData>();
        skills = skillsViewSource;
        readOnlySkills = Array.AsReadOnly(skillsViewSource);
    }

    #region Core Functions

    /// <summary>
    /// Applies damage to the unit, reducing HP and potentially triggering death.
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;

        // Apply defense reduction
        int modifiedDamage = damage;
        if (isDefending)
        {
            modifiedDamage = Mathf.RoundToInt(modifiedDamage * GameConstants.DefendMultiplier);
        }
        float defenseMod = GetDefenseModifier();
        int effectiveDefense = Mathf.Max(0, Mathf.RoundToInt(defense * (1f + defenseMod)));
        int actualDamage = Mathf.Max(1, modifiedDamage - effectiveDefense);
        HP = HP - actualDamage;

        Debug.Log($"[CombatUnit] {unitName} took {actualDamage} damage (from {damage}). HP: {HP}/{maxHp}");

        // HP property setter already handles death via Die() when hp <= 0 && isAlive
        // No need for duplicate Die() call here
    }

    /// <summary>
    /// Heals the unit, increasing HP up to maximum.
    /// </summary>
    /// <param name="amount">Amount of healing to apply</param>
    public virtual void Heal(int amount)
    {
        if (!isAlive) return;

        if (amount <= 0)
        {
            return;
        }

        int healedAmount = Mathf.Min(amount, maxHp - HP);
        HP = HP + healedAmount;

        Debug.Log($"[CombatUnit] {unitName} healed for {healedAmount}. HP: {HP}/{maxHp}");
    }

    /// <summary>
    /// Puts the unit into a defending state, reducing incoming damage.
    /// </summary>
    public void StartDefend()
    {
        if (!isAlive) return;
        isDefending = true;
        Debug.Log($"[CombatUnit] {unitName} is now defending.");
    }

    /// <summary>
    /// Clears the defending state.
    /// </summary>
    public void ClearDefend()
    {
        isDefending = false;
        Debug.Log($"[CombatUnit] {unitName} defend cleared.");
    }

    #region Status Effects

    public void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;
        // If effect of same type already exists, refresh duration to the NEWER one (more recent).
        StatusEffect existing = activeEffects.Find(e => e.Type == effect.Type);
        if (existing != null)
        {
            existing.Duration = effect.Duration;
            existing.Magnitude = effect.Magnitude;
            existing.SourceName = effect.SourceName;
            Debug.Log($"[CombatUnit] {unitName} refreshed {effect.Type} effect from {effect.SourceName}. Duration: {existing.Duration}");
        }
        else
        {
            activeEffects.Add(effect);
            Debug.Log($"[CombatUnit] {unitName} gained {effect.Type} effect from {effect.SourceName}. Duration: {effect.Duration}, Magnitude: {effect.Magnitude}");
        }
    }

    public void ProcessTurnStartEffects()
    {
        // Process effects at start of turn
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            if (effect.Type == StatusEffectType.Poison)
            {
                TakeDamage((int)effect.Magnitude);
                Debug.Log($"[CombatUnit] {unitName} took {effect.Magnitude} poison damage from {effect.SourceName}.");
            }
            effect.Tick();
            if (effect.Duration <= 0)
            {
                Debug.Log($"[CombatUnit] {unitName}'s {effect.Type} effect expired.");
                activeEffects.RemoveAt(i);
            }
        }
    }

    public float GetAttackModifier()
    {
        float total = 0f;
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.BuffAttack)
                total += effect.Magnitude;
            else if (effect.Type == StatusEffectType.DebuffAttack)
                total -= effect.Magnitude;
        }
        return Mathf.Clamp(total, -0.5f, 1.0f);
    }

    public float GetDefenseModifier()
    {
        float total = 0f;
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.BuffDefense)
                total += effect.Magnitude;
            else if (effect.Type == StatusEffectType.DebuffDefense)
                total -= effect.Magnitude;
        }
        return Mathf.Clamp(total, -0.5f, 1.0f);
    }

    public int GetEffectiveSpeed()
    {
        float largestSlow = 0f;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffect effect = activeEffects[i];
            if (effect != null && effect.Type == StatusEffectType.Slow && effect.Magnitude > largestSlow)
            {
                largestSlow = effect.Magnitude;
            }
        }
        return Mathf.Max(1, Mathf.RoundToInt(speed * (1f - Mathf.Clamp01(largestSlow))));
    }

    public bool ShouldSkipTurn()
    {
        float highestDrowsy = 0f;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffect effect = activeEffects[i];
            if (effect != null && effect.Type == StatusEffectType.Drowsy && effect.Magnitude > highestDrowsy)
            {
                highestDrowsy = effect.Magnitude;
            }
        }
        return highestDrowsy >= 1f || UnityEngine.Random.value < highestDrowsy;
    }

    public void ClearAllStatusEffects()
    {
        activeEffects.Clear();
        Debug.Log($"[CombatUnit] {unitName} cleared all status effects.");
    }

    #endregion

    /// <summary>
    /// Spends MP if the unit has sufficient MP available.
    /// </summary>
    /// <param name="amount">Amount of MP to spend</param>
    /// <returns>True if MP was successfully spent, false if insufficient MP</returns>
    public virtual bool SpendMp(int amount)
    {
        if (!isAlive) return false;

        if (amount <= 0)
        {
            return true;
        }

        if (mp >= amount)
        {
            mp -= amount;
            Debug.Log($"[CombatUnit] {unitName} spent {amount} MP. MP: {mp}/{maxMp}");
            return true;
        }
        else
        {
            Debug.Log($"[CombatUnit] {unitName} attempted to spend {amount} MP but only has {mp} MP available.");
            return false;
        }
    }

    /// <summary>
    /// Restores MP to the unit, up to maximum.
    /// </summary>
    /// <param name="amount">Amount of MP to restore</param>
    public virtual void RestoreMp(int amount)
    {
        if (!isAlive) return;

        if (amount <= 0)
        {
            return;
        }

        int restoredAmount = Mathf.Min(amount, maxMp - mp);
        mp += restoredAmount;

        Debug.Log($"[CombatUnit] {unitName} restored {restoredAmount} MP. MP: {mp}/{maxMp}");
    }

    /// <summary>
    /// Checks and updates the death state based on current HP.
    /// </summary>
    public virtual void CheckDeathState()
    {
        if (isAlive && hp <= 0)
        {
            Die();
        }
        else if (!isAlive && hp > 0)
        {
            // Unit was revived
            isAlive = true;
            Debug.Log($"[CombatUnit] {unitName} has been revived!");
        }
    }

    #endregion

    #region Protected/Virtual Methods

    /// <summary>
    /// Handles the unit dying. Called when HP reaches 0 or below.
    /// </summary>
    protected virtual void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        hp = 0; // Ensure HP doesn't go negative
        Debug.Log($"[CombatUnit] {unitName} has been defeated!");
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        GetOrCreateTideBreakAbilities();
        if (maxHp <= 0) maxHp = 100;
        if (maxMp <= 0) maxMp = 50;
        if (attack < 0) attack = 0;
        if (defense < 0) defense = 0;
        if (speed < 0) speed = 0;

        hp = Mathf.Clamp(hp, 0, maxHp);
        mp = Mathf.Clamp(mp, 0, maxMp);
        EnsureSkillsInitialized();

        isAlive = hp > 0;
    }

    protected virtual void Start()
    {
        // Additional initialization can go here
    }

    private List<TideBreakData> GetOrCreateTideBreakAbilities()
    {
        if (tideBreakAbilities == null)
        {
            tideBreakAbilities = new List<TideBreakData>();
        }

        return tideBreakAbilities;
    }

    #endregion
}
