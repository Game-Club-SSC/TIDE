public static class GameConstants
{
    public const float ClashWinnerMultiplier = 1.5f;
    public const float ClashLoserMultiplier = 0.5f;
    public const float ClashNeutralMultiplier = 0.6f;
    public const int MinimumDamage = 1;
    public const int MaxCarrySteps = 2;
    public const int InstabilityThreshold = 3;
    public const int PuzzleTargetValue = 5;
    public const int PuzzleMinValue = 1;
    public const int PuzzleMaxValue = 10;
    public const float AutoAdvanceDelay = 1.25f;
    public const float ActionStepDelay = 0.55f;
    public const float MomentumBarLength = 20f;
    public const int DefaultBaseXpToLevel = 100;
    public const int DefaultXpPerLevelIncrement = 50;
    public const float DefaultReserveXpMultiplier = 0.5f;
    public const float DefendMultiplier = 0.5f;

    public const string IslandLust = "island_lust";
    public const string IslandGreed = "island_greed";
    public const string IslandDesire = "island_desire";
    public const string IslandAnger = "island_anger";
    public const string IslandEnvy = "island_envy";
    public const string IslandEgo = "island_ego";

    // BUGFIX: Hero IDs corrected from old thematic names to match the actual
    // runtime heroId values used throughout the codebase (e.g. hero_fire, not
    // hero_ember). These constants were dead code (not referenced at runtime)
    // but had incorrect values that could confuse new developers.
    public const string HeroEmber = "hero_fire";
    public const string HeroTidecaller = "hero_water";
    public const string HeroStoneheart = "hero_earth";
    public const string HeroZephyr = "hero_air";
    public const string HeroVoidwalker = "hero_space";
}
