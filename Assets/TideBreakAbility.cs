public class TideBreakAbility
{
    public string AbilityName { get; }
    public bool IsPlayerAbility { get; }
    public float DamageMultiplier { get; }

    public TideBreakAbility(string name, bool isPlayer, float multiplier)
    {
        AbilityName = name;
        IsPlayerAbility = isPlayer;
        DamageMultiplier = multiplier;
    }

    public static TideBreakAbility PlayerDefault =>
        new TideBreakAbility("Tidal Surge", true, 2.0f);

    public static TideBreakAbility EnemyDefault =>
        new TideBreakAbility("Dark Surge", false, 2.0f);
}
