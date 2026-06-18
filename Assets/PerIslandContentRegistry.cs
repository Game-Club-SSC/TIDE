using System.Collections.Generic;

public static class PerIslandContentRegistry
{
    public sealed class IslandContentPack
    {
        public string IslandId { get; }
        public string DisplayName { get; }
        public string EncounterIdPrefix { get; }
        public int RecommendedLevel { get; }
        public string BossId { get; }

        public IslandContentPack(string islandId, string displayName, string encounterPrefix, int recommendedLevel, string bossId)
        {
            IslandId = islandId;
            DisplayName = displayName;
            EncounterIdPrefix = encounterPrefix;
            RecommendedLevel = recommendedLevel;
            BossId = bossId;
        }
    }

    private static readonly List<IslandContentPack> Packs = new List<IslandContentPack>
    {
        new IslandContentPack("island_gluttony", "Gluttony Island", "gluttony_enc", 1, "gluttony_boss"),
        new IslandContentPack("island_greed", "Greed Island", "greed_enc", 3, "greed_boss"),
        new IslandContentPack("island_sloth", "Sloth Island", "sloth_enc", 5, "sloth_boss"),
        new IslandContentPack("island_wrath", "Wrath Island", "wrath_enc", 7, "wrath_boss"),
        new IslandContentPack("island_envy", "Envy Island", "envy_enc", 9, "envy_boss"),
        new IslandContentPack("island_pride", "Pride Island", "pride_enc", 11, "pride_final_boss")
    };

    public static IReadOnlyList<IslandContentPack> GetAllPacks()
    {
        return Packs;
    }

    public static IslandContentPack GetPackForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        for (int i = 0; i < Packs.Count; i++)
        {
            if (string.Equals(Packs[i].IslandId, islandId, System.StringComparison.Ordinal))
            {
                return Packs[i];
            }
        }

        return null;
    }
}
