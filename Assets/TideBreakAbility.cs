public class TideBreakAbility
{
    private static readonly TideBreakAbility CachedPlayerDefault =
        new TideBreakAbility("Tidal Surge", true, 2.0f);
    private static readonly TideBreakAbility CachedEnemyDefault =
        new TideBreakAbility("Dark Surge", false, 2.0f);

    public string AbilityName { get; }
    public bool IsPlayerAbility { get; }
    public float DamageMultiplier { get; }

    public TideBreakAbility(string name, bool isPlayer, float multiplier)
    {
        AbilityName = name;
        IsPlayerAbility = isPlayer;
        DamageMultiplier = multiplier;
    }

    public static TideBreakAbility PlayerDefault => CachedPlayerDefault;

    public static TideBreakAbility EnemyDefault => CachedEnemyDefault;
}
