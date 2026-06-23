public class TideBreakAbility
{
    private static readonly TideBreakAbility CachedPlayerDefault =
        new TideBreakAbility("Tidal Surge", true, 2.0f);
    private static readonly TideBreakAbility CachedEnemyDefault =
        new TideBreakAbility("Dark Surge", false, 2.0f);

    public string AbilityName { get; }
    public bool IsPlayerAbility { get; }
    public float DamageMultiplier { get; }

    /// <summary>Hero level required to use this ability. 1 = available from the start.</summary>
    public int RequiredLevel { get; }

    /// <summary>Stable hero id this ability belongs to (empty = generic / enemy).</summary>
    public string HeroId { get; }

    /// <summary>Whether the owning hero has unlocked this ability.</summary>
    public bool IsUnlocked { get; set; }

    /// <summary>Human-readable unlock condition shown in the UI (e.g. "Reach Level 5").</summary>
    public string UnlockCondition { get; }

    public TideBreakAbility(string name, bool isPlayer, float multiplier)
        : this(name, isPlayer, multiplier, 1, string.Empty, true, string.Empty)
    {
    }

    public TideBreakAbility(
        string name,
        bool isPlayer,
        float multiplier,
        int requiredLevel,
        string heroId,
        bool isUnlocked,
        string unlockCondition)
    {
        AbilityName = name;
        IsPlayerAbility = isPlayer;
        DamageMultiplier = multiplier;
        RequiredLevel = requiredLevel;
        HeroId = heroId;
        IsUnlocked = isUnlocked;
        UnlockCondition = unlockCondition;
    }

    public static TideBreakAbility PlayerDefault => CachedPlayerDefault;

    public static TideBreakAbility EnemyDefault => CachedEnemyDefault;
}
