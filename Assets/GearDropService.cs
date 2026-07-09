using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GearDropService", menuName = "TIDE/Gear Drop Service")]
public class GearDropService : ScriptableObject
{
    [Serializable]
    public class LootEntry
    {
        public string gearSetId;
        public float weight;
    }

    [Serializable]
    public class LootTable
    {
        public string enemyType;
        public string islandId;
        public float dropRate = 0.3f;
        public LootEntry[] entries = Array.Empty<LootEntry>();
    }

    [Serializable]
    public class RarityWeight
    {
        public GearRarity rarity;
        public float weight;
    }

    public enum GearRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Header("Loot Tables")]
    [SerializeField] private LootTable[] lootTables = Array.Empty<LootTable>();

    [Header("Rarity Weights")]
    [SerializeField] private RarityWeight[] rarityWeights = new RarityWeight[]
    {
        new RarityWeight { rarity = GearRarity.Common, weight = 50f },
        new RarityWeight { rarity = GearRarity.Uncommon, weight = 25f },
        new RarityWeight { rarity = GearRarity.Rare, weight = 15f },
        new RarityWeight { rarity = GearRarity.Epic, weight = 8f },
        new RarityWeight { rarity = GearRarity.Legendary, weight = 2f }
    };

    [Header("Island Drop Modifiers")]
    [SerializeField] private float baseDropRate = 0.3f;
    [SerializeField] private float dropRatePerIsland = 0.05f;

    public event Action<string, GearRarity> OnGearDropped;

    private static GearDropService activeInstance;

    private static readonly Dictionary<string, int> IslandIndexByTheme = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        { "island_lust", 0 },
        { "island_greed", 1 },
        { "island_greed", 2 },
        { "island_desire", 3 },
        { "island_anger", 4 },
        { "island_envy", 5 },
        { "island_ego", 6 }
    };

    public static GearDropService ActiveInstance => activeInstance;

    public void SetAsActive()
    {
        activeInstance = this;
    }

    public LootTable GetLootTable(string enemyType, string islandId)
    {
        if (lootTables == null || lootTables.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < lootTables.Length; i++)
        {
            LootTable table = lootTables[i];
            if (table == null)
            {
                continue;
            }

            bool enemyMatch = string.IsNullOrEmpty(table.enemyType)
                || string.Equals(table.enemyType, enemyType, StringComparison.Ordinal);
            bool islandMatch = string.IsNullOrEmpty(table.islandId)
                || string.Equals(table.islandId, islandId, StringComparison.Ordinal);

            if (enemyMatch && islandMatch)
            {
                return table;
            }
        }

        return null;
    }

    public bool TryRollDrop(string enemyType, string islandId, out string gearSetId, out GearRarity rarity)
    {
        gearSetId = null;
        rarity = GearRarity.Common;

        float effectiveDropRate = GetEffectiveDropRate(islandId);
        if (UnityEngine.Random.value > effectiveDropRate)
        {
            return false;
        }

        LootTable table = GetLootTable(enemyType, islandId);
        if (table == null || table.entries == null || table.entries.Length == 0)
        {
            return false;
        }

        rarity = RollRarity();

        float totalWeight = 0f;
        for (int i = 0; i < table.entries.Length; i++)
        {
            if (table.entries[i] != null)
            {
                totalWeight += Mathf.Max(0f, table.entries[i].weight);
            }
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < table.entries.Length; i++)
        {
            if (table.entries[i] == null)
            {
                continue;
            }

            cumulative += Mathf.Max(0f, table.entries[i].weight);
            if (roll <= cumulative)
            {
                gearSetId = table.entries[i].gearSetId;
                OnGearDropped?.Invoke(gearSetId, rarity);
                return true;
            }
        }

        return false;
    }

    public float GetEffectiveDropRate(string islandId)
    {
        int islandIndex = 0;
        if (!string.IsNullOrEmpty(islandId) && IslandIndexByTheme.TryGetValue(islandId, out int idx))
        {
            islandIndex = idx;
        }

        return Mathf.Clamp01(baseDropRate + islandIndex * dropRatePerIsland);
    }

    private GearRarity RollRarity()
    {
        if (rarityWeights == null || rarityWeights.Length == 0)
        {
            return GearRarity.Common;
        }

        float totalWeight = 0f;
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            if (rarityWeights[i] != null)
            {
                totalWeight += Mathf.Max(0f, rarityWeights[i].weight);
            }
        }

        if (totalWeight <= 0f)
        {
            return GearRarity.Common;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            if (rarityWeights[i] == null)
            {
                continue;
            }

            cumulative += Mathf.Max(0f, rarityWeights[i].weight);
            if (roll <= cumulative)
            {
                return rarityWeights[i].rarity;
            }
        }

        return GearRarity.Common;
    }
}
