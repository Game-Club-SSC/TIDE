using System;
using System.Collections.Generic;
using UnityEngine;

public static class IslandThemeRegistry
{
    public const string DefaultIslandId = "island_lust";

    private static readonly string[] progressionOrder =
    {
        "island_lust",
        "island_anger",
        "island_gluttony",
        "island_greed",
        "island_desire",
        "island_ego",
        "island_envy"
    };

    private static readonly Dictionary<string, string> legacyIslandIdAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "island_1", "island_lust" },
            { "island_2", "island_anger" },
            { "island_3", "island_greed" },
            { "island_4", "island_desire" },
            { "island_5", "island_ego" },
            { "island_6", "island_envy" },
            { "island_7", "island_envy" },
            { "island_8", "island_gluttony" }
        };

    private static readonly Dictionary<string, IslandConfig> configsById =
        new Dictionary<string, IslandConfig>(StringComparer.Ordinal);

    private static readonly List<IslandConfig> orderedConfigs = new List<IslandConfig>();

    private static bool isInitialized;
    private static string activeIslandId = DefaultIslandId;

    public static IReadOnlyList<string> ProgressionOrder => progressionOrder;

    public static IReadOnlyList<IslandConfig> GetAllOrdered()
    {
        EnsureInitialized();
        return orderedConfigs;
    }

    public static IslandConfig GetConfig(string islandId)
    {
        EnsureInitialized();
        string resolvedId = ResolveIslandId(islandId);
        if (configsById.TryGetValue(resolvedId, out IslandConfig config))
        {
            return config;
        }

        return null;
    }

    public static string GetActiveIslandId()
    {
        return ResolveIslandId(activeIslandId);
    }

    public static IslandConfig GetActiveConfig()
    {
        return GetConfig(GetActiveIslandId());
    }

    public static void SetActiveIslandId(string islandId)
    {
        activeIslandId = ResolveIslandId(islandId);
    }

    public static string ResolveIslandId(string islandId)
    {
        EnsureInitialized();

        if (!string.IsNullOrEmpty(islandId)
            && legacyIslandIdAliases.TryGetValue(islandId, out string aliasedIslandId))
        {
            islandId = aliasedIslandId;
        }

        if (!string.IsNullOrEmpty(islandId) && configsById.ContainsKey(islandId))
        {
            return islandId;
        }

        if (string.IsNullOrEmpty(islandId)
            && !string.IsNullOrEmpty(activeIslandId)
            && configsById.ContainsKey(activeIslandId))
        {
            return activeIslandId;
        }

        for (int i = 0; i < progressionOrder.Length; i++)
        {
            string candidate = progressionOrder[i];
            if (configsById.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        if (orderedConfigs.Count > 0 && orderedConfigs[0] != null)
        {
            return orderedConfigs[0].islandId;
        }

        return DefaultIslandId;
    }

    public static string GetNextIslandId(string islandId)
    {
        EnsureInitialized();
        string resolvedCurrent = ResolveIslandId(islandId);

        for (int i = 0; i < progressionOrder.Length; i++)
        {
            if (!string.Equals(progressionOrder[i], resolvedCurrent, StringComparison.Ordinal))
            {
                continue;
            }

            for (int nextIndex = i + 1; nextIndex < progressionOrder.Length; nextIndex++)
            {
                string nextCandidate = progressionOrder[nextIndex];
                if (configsById.ContainsKey(nextCandidate))
                {
                    return nextCandidate;
                }
            }

            return resolvedCurrent;
        }

        return resolvedCurrent;
    }

    public static bool IsKnownIslandId(string islandId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        if (configsById.ContainsKey(islandId))
        {
            return true;
        }

        return legacyIslandIdAliases.TryGetValue(islandId, out string aliasedIslandId)
            && configsById.ContainsKey(aliasedIslandId);
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        configsById.Clear();
        orderedConfigs.Clear();

        IslandConfig[] loadedConfigs = Resources.LoadAll<IslandConfig>("Islands");
        for (int i = 0; i < loadedConfigs.Length; i++)
        {
            IslandConfig config = loadedConfigs[i];
            if (config == null || string.IsNullOrEmpty(config.islandId))
            {
                continue;
            }

            if (!configsById.ContainsKey(config.islandId))
            {
                configsById.Add(config.islandId, config);
            }
        }

        for (int i = 0; i < progressionOrder.Length; i++)
        {
            string islandId = progressionOrder[i];
            if (configsById.TryGetValue(islandId, out IslandConfig config))
            {
                orderedConfigs.Add(config);
            }
        }

        foreach (KeyValuePair<string, IslandConfig> pair in configsById)
        {
            if (!orderedConfigs.Contains(pair.Value))
            {
                orderedConfigs.Add(pair.Value);
            }
        }

        isInitialized = true;
    }
}
