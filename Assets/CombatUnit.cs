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
    [SerializeField] protected Element element = Element.None;

    // Unit state
    [Header("Unit State")]
    [SerializeField] protected string unitName = "Combat Unit";
    [SerializeField] protected UnitType unitType = UnitType.Ally;
    [SerializeField] protected bool isAlive = true;

    internal int DebugHP { set => hp = value; }
    internal int DebugMaxHP { set => maxHp = value; }
    internal int DebugMP { set => mp = value; }
    internal int DebugMaxMP { set => maxMp = value; }
    internal int DebugDefense { set => defense = value; }
    internal bool DebugIsAlive { set => isAlive = value; }

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
    public int HP { get => hp; set => hp = value; }
    public int MaxHP { get => maxHp; set => maxHp = value; }
    public int MP => mp;
    public int MaxMP => maxMp;
    public int Attack { get => attack; set => attack = value; }
    public int Defense { get => defense; set => defense = value; }
    public int Speed { get => speed; set => speed = value; }
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
    #endregion

    #region Core Functions

    /// <summary>
    /// Applies damage to the unit, reducing HP and potentially triggering death.
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;

        // Apply defense reduction
        int actualDamage = Mathf.Max(1, damage - defense);
        hp = Mathf.Max(0, hp - actualDamage);

        Debug.Log($"{unitName} took {actualDamage} damage (from {damage}). HP: {hp}/{maxHp}");

        // Check if unit has died
        if (hp <= 0)
        {
            Die();
        }
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

        int healedAmount = Mathf.Min(amount, maxHp - hp);
        hp += healedAmount;

        Debug.Log($"{unitName} healed for {healedAmount}. HP: {hp}/{maxHp}");
    }

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
            Debug.Log($"{unitName} spent {amount} MP. MP: {mp}/{maxMp}");
            return true;
        }
        else
        {
            Debug.Log($"{unitName} attempted to spend {amount} MP but only has {mp} MP available.");
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

        Debug.Log($"{unitName} restored {restoredAmount} MP. MP: {mp}/{maxMp}");
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
            Debug.Log($"{unitName} has been revived!");
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
        Debug.Log($"{unitName} has been defeated!");
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        if (maxHp <= 0) maxHp = 100;
        if (maxMp <= 0) maxMp = 50;
        if (attack < 0) attack = 0;
        if (defense < 0) defense = 0;
        if (speed < 0) speed = 0;

        hp = Mathf.Clamp(hp, 0, maxHp);
        mp = Mathf.Clamp(mp, 0, maxMp);

        isAlive = hp > 0;
    }

    protected virtual void Start()
    {
        // Additional initialization can go here
    }

    #endregion
}
