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

    // Shield HP (temporary HP buffer from Shield effect)
    [SerializeField] protected float shieldHp = 0f;

    // Taunt tracking
    private CombatUnit tauntedBy = null;

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
    public int Attack { get => attack; set => attack = Mathf.Max(0, value); }
    public int Defense { get => defense; set => defense = Mathf.Max(0, value); }
    public int Speed { get => speed; set => speed = Mathf.Max(0, value); }
    public float CritRate { get => critRate; set => critRate = Mathf.Clamp01(value); }
    public float CritDamage { get => critDamage; set => critDamage = Mathf.Max(1f, value); }
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

    /// <summary>
    /// Current shield HP acting as a temporary damage buffer.
    /// Shield absorbs incoming damage before HP is reduced.
    /// </summary>
    public float ShieldHp
    {
        get => shieldHp;
        set => shieldHp = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Whether this unit is currently stunned (skips its next turn).
    /// Set during ProcessTurnStartEffects when a Stun effect is active.
    /// </summary>
    public bool IsStunned { get; private set; }

    /// <summary>
    /// Whether this unit is currently taunted, forcing it to target a specific enemy.
    /// </summary>
    public bool IsTaunted => tauntedBy != null && tauntedBy.IsAlive;

    /// <summary>
    /// The unit that is taunting this unit, if any. Null when not taunted.
    /// </summary>
    public CombatUnit TauntedBy => IsTaunted ? tauntedBy : null;

    /// <summary>
    /// Whether this unit currently has an AccuracyDown debuff active.
    /// </summary>
    public bool IsAccuracyDown
    {
        get
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null && activeEffects[i].Type == StatusEffectType.AccuracyDown)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Whether this unit currently has an AccuracyBuff active.
    /// </summary>
    public bool IsAccuracyBuff
    {
        get
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null && activeEffects[i].Type == StatusEffectType.AccuracyBuff)
                    return true;
            }
            return false;
        }
    }

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
    /// Shield HP absorbs damage before HP is reduced.
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;

        if (damage <= 0)
        {
            if (damage < 0)
            {
                Debug.LogWarning($"[CombatUnit] TakeDamage called with negative amount {damage}. Treating as no-op.");
            }
            return;
        }

        // Apply defense reduction
        int modifiedDamage = damage;
        if (isDefending)
        {
            modifiedDamage = Mathf.RoundToInt(modifiedDamage * GameConstants.DefendMultiplier);
        }
        float defenseMod = GetDefenseModifier();
        int effectiveDefense = Mathf.Max(0, Mathf.RoundToInt(defense * (1f + defenseMod)));
        int actualDamage = Mathf.Max(1, modifiedDamage - effectiveDefense);

        // Shield absorbs damage before HP is reduced
        if (shieldHp > 0f)
        {
            float shieldAbsorb = Mathf.Min(shieldHp, actualDamage);
            shieldHp -= shieldAbsorb;
            actualDamage -= Mathf.RoundToInt(shieldAbsorb);
            Debug.Log($"[CombatUnit] {unitName}'s shield absorbed {Mathf.RoundToInt(shieldAbsorb)} damage. Shield HP: {shieldHp:F0}");
        }

        if (actualDamage > 0)
        {
            HP = HP - actualDamage;
        }

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

        if (effect.Type == StatusEffectType.Shield)
        {
            // Shield refreshes replace the existing shield value rather than stacking
            shieldHp = effect.Magnitude * maxHp;
            Debug.Log($"[CombatUnit] {unitName}'s shield HP set to {shieldHp:F0} ({effect.Magnitude:P0} of MaxHP).");
        }
    }

    public void ProcessTurnStartEffects()
    {
        // Reset stun flag at the start of processing; it will be re-set if Stun is active
        IsStunned = false;

        // Process effects at start of turn
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            switch (effect.Type)
            {
                case StatusEffectType.Poison:
                {
                    int poisonDamage = Mathf.Max(1, (int)effect.Magnitude);
                    TakeDamage(poisonDamage);
                    Debug.Log($"[CombatUnit] {unitName} took {poisonDamage} poison damage from {effect.SourceName}.");
                    break;
                }
                case StatusEffectType.Burn:
                {
                    // Burn deals 1.5x the magnitude as damage (higher than poison)
                    int burnDamage = Mathf.Max(1, Mathf.RoundToInt(effect.Magnitude * 1.5f));
                    TakeDamage(burnDamage);
                    Debug.Log($"[CombatUnit] {unitName} took {burnDamage} burn damage from {effect.SourceName}.");
                    break;
                }
                case StatusEffectType.Regeneration:
                {
                    int healAmount = Mathf.Max(1, (int)effect.Magnitude);
                    Heal(healAmount);
                    Debug.Log($"[CombatUnit] {unitName} regenerated {healAmount} HP from {effect.SourceName}.");
                    break;
                }
                case StatusEffectType.Stun:
                {
                    IsStunned = true;
                    SkipTurnThisRound = true;
                    Debug.Log($"[CombatUnit] {unitName} is stunned by {effect.SourceName} and will skip their turn.");
                    break;
                }
                case StatusEffectType.Shield:
                {
                    // Shield does nothing on tick; it absorbs damage in TakeDamage
                    break;
                }
                default:
                    break;
            }

            effect.Tick();
            if (effect.Duration <= 0)
            {
                Debug.Log($"[CombatUnit] {unitName}'s {effect.Type} effect expired.");
                activeEffects.RemoveAt(i);

                if (effect.Type == StatusEffectType.Shield && !HasActiveShieldEffect())
                {
                    shieldHp = 0f;
                }
            }
        }

        // Update taunt state: only clear tauntedBy if no active Taunt effect remains.
        // tauntedBy is set externally via SetTaunter; we preserve it as long as a
        // Taunt effect is active so the taunt persists across turns.
        bool hasActiveTaunt = false;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffect effect = activeEffects[i];
            if (effect != null && effect.Type == StatusEffectType.Taunt)
            {
                hasActiveTaunt = true;
                break;
            }
        }

        if (!hasActiveTaunt)
        {
            tauntedBy = null;
        }
    }

    private bool HasActiveShieldEffect()
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffect effect = activeEffects[i];
            if (effect != null && effect.Type == StatusEffectType.Shield)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets the unit that is taunting this unit. Called by the battle system
    /// when a Taunt effect is applied.
    /// </summary>
    /// <param name="taunter">The unit exerting the taunt</param>
    public void SetTaunter(CombatUnit taunter)
    {
        tauntedBy = taunter;
        Debug.Log($"[CombatUnit] {unitName} is now taunted by {taunter?.UnitName ?? "none"}.");
    }

    /// <summary>
    /// Clears the taunt state, allowing free target selection.
    /// </summary>
    public void ClearTaunt()
    {
        tauntedBy = null;
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
            else if (effect.Type == StatusEffectType.Berserk)
                total += effect.Magnitude;
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
            else if (effect.Type == StatusEffectType.Berserk)
                total -= effect.Magnitude;
        }

        // Shield provides a flat defense bonus while active
        if (shieldHp > 0f)
        {
            total += 0.1f;
        }

        return Mathf.Clamp(total, -0.5f, 1.0f);
    }

    /// <summary>
    /// Returns the combined accuracy modifier from all active effects.
    /// Positive values increase hit chance; negative values decrease it.
    /// </summary>
    public float GetAccuracyModifier()
    {
        float total = 0f;
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.AccuracyBuff)
                total += effect.Magnitude;
            else if (effect.Type == StatusEffectType.AccuracyDown)
                total -= effect.Magnitude;
        }
        return Mathf.Clamp(total, -0.5f, 0.5f);
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
        // Stun always skips the turn
        if (IsStunned) return true;

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
        IsStunned = false;
        tauntedBy = null;
        shieldHp = 0f;
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
            if (amount < 0)
            {
                Debug.LogWarning($"[CombatUnit] SpendMp called with negative amount {amount}. Treating as no-op.");
            }
            return amount == 0;
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
