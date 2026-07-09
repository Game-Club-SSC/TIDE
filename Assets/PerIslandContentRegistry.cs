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
        new IslandContentPack("island_lust", "Lust Island", "lust_enc", 1, "lust_boss"),
        new IslandContentPack("island_greed", "Greed Island", "greed_enc", 4, "greed_boss"),
        new IslandContentPack("island_desire", "Desire Island", "desire_enc", 6, "desire_boss"),
        new IslandContentPack("island_anger", "Anger Island", "anger_enc", 8, "anger_boss"),
        new IslandContentPack("island_envy", "Envy Island", "envy_enc", 10, "envy_boss"),
        new IslandContentPack("island_ego", "Ego Island", "ego_enc", 12, "ego_boss")
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
