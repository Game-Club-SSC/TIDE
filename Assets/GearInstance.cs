using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GearInstance
{
    public const int MaxBonusSlots = 3;
    public const float MinBonusPercent = 0.02f;
    public const float MaxBonusPercent = 0.10f;

    private static readonly GearBonusStatType[] AllowedBonusStats =
    {
        GearBonusStatType.ATK,
        GearBonusStatType.DEF,
        GearBonusStatType.HP
    };

    private static readonly string[] SlotStatLabels =
    {
        "ATK",
        "DEF",
        "HP"
    };

    public string instanceId;
    public string setId;
    [NonSerialized] public GearSetData template;
    public int level = 1;
    public int currentXp;
    public List<GearSlotBonus> unlockedSlots = new List<GearSlotBonus>();

    public int MaxLevel => MaxBonusSlots + 1;
    public bool CanLevelUp => level < MaxLevel && currentXp >= GetXpToNextLevel();
    public int UnlockedSlotCount => unlockedSlots != null ? unlockedSlots.Count : 0;

    public int GetXpToNextLevel()
    {
        if (level >= MaxLevel)
        {
            return 0;
        }

        int normalizedLevel = Mathf.Max(1, level);
        return 50 + (normalizedLevel - 1) * 25;
    }

    public bool GrantXp(int amount)
    {
        if (amount <= 0 || level >= MaxLevel)
        {
            return false;
        }

        currentXp = Mathf.Max(0, currentXp) + amount;
        bool leveledUp = false;

        while (level < MaxLevel)
        {
            int xpNeeded = GetXpToNextLevel();
            if (xpNeeded <= 0 || currentXp < xpNeeded)
            {
                break;
            }

            currentXp -= xpNeeded;
            level++;
            UnlockRandomSlot();
            leveledUp = true;
        }

        if (level >= MaxLevel)
        {
            currentXp = 0;
        }

        return leveledUp;
    }

    public float GetBonusForStat(GearBonusStatType statType)
    {
        if (unlockedSlots == null)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < unlockedSlots.Count; i++)
        {
            if (unlockedSlots[i].statType == statType)
            {
                total += unlockedSlots[i].percentValue;
            }
        }

        return total;
    }

    public float GetTotalAttackPercent()
    {
        float basePercent = template != null ? template.TotalAttackPercent : 0f;
        return basePercent + GetBonusForStat(GearBonusStatType.ATK);
    }

    public float GetTotalDefensePercent()
    {
        float basePercent = template != null ? template.TotalDefensePercent : 0f;
        return basePercent + GetBonusForStat(GearBonusStatType.DEF);
    }

    public float GetTotalHpPercent()
    {
        float basePercent = template != null ? template.TotalHpPercent : 0f;
        return basePercent + GetBonusForStat(GearBonusStatType.HP);
    }

    public GearInstance Duplicate()
    {
        GearInstance copy = new GearInstance
        {
            instanceId = Guid.NewGuid().ToString(),
            setId = setId,
            template = template,
            level = level,
            currentXp = currentXp,
            unlockedSlots = new List<GearSlotBonus>()
        };

        if (unlockedSlots != null)
        {
            for (int i = 0; i < unlockedSlots.Count; i++)
            {
                copy.unlockedSlots.Add(new GearSlotBonus
                {
                    statType = unlockedSlots[i].statType,
                    percentValue = unlockedSlots[i].percentValue
                });
            }
        }

        return copy;
    }

    public string GetSlotDisplayString()
    {
        if (UnlockedSlotCount == 0)
        {
            return "No bonus slots";
        }

        string result = string.Empty;
        for (int i = 0; i < unlockedSlots.Count; i++)
        {
            if (i > 0)
            {
                result += ", ";
            }

            GearSlotBonus slot = unlockedSlots[i];
            int index = (int)slot.statType;
            string label = index >= 0 && index < SlotStatLabels.Length
                ? SlotStatLabels[index]
                : slot.statType.ToString();

            int percent = Mathf.RoundToInt(slot.percentValue * 100f);
            result += $"+{percent}% {label}";
        }

        return result;
    }

    public bool RerollSlot(int slotIndex)
    {
        if (unlockedSlots == null || slotIndex < 0 || slotIndex >= unlockedSlots.Count)
        {
            return false;
        }

        GearSlotBonus slot = unlockedSlots[slotIndex];
        float rolled = UnityEngine.Random.Range(MinBonusPercent, MaxBonusPercent);
        slot.percentValue = ClampAndRoundPercent(rolled);
        unlockedSlots[slotIndex] = slot;
        return true;
    }

    public GearInstanceSaveData ToSaveData()
    {
        GearInstanceSaveData data = new GearInstanceSaveData
        {
            instanceId = instanceId,
            setId = setId,
            level = Mathf.Clamp(level, 1, MaxLevel),
            currentXp = Mathf.Max(0, currentXp),
            slotStatTypes = new List<int>(),
            slotPercentValues = new List<float>()
        };

        if (unlockedSlots == null)
        {
            return data;
        }

        int count = Mathf.Min(unlockedSlots.Count, MaxBonusSlots);
        for (int i = 0; i < count; i++)
        {
            GearSlotBonus slot = unlockedSlots[i];
            data.slotStatTypes.Add((int)slot.statType);
            data.slotPercentValues.Add(ClampAndRoundPercent(slot.percentValue));
        }

        return data;
    }

    public static GearInstance FromSaveData(GearInstanceSaveData saveData, GearSetData[] availableSets)
    {
        if (saveData == null)
        {
            return null;
        }

        GearInstance instance = new GearInstance
        {
            instanceId = saveData.instanceId,
            setId = saveData.setId,
            level = Mathf.Clamp(saveData.level, 1, MaxBonusSlots + 1),
            currentXp = Mathf.Max(0, saveData.currentXp),
            unlockedSlots = new List<GearSlotBonus>()
        };

        if (availableSets != null)
        {
            for (int i = 0; i < availableSets.Length; i++)
            {
                GearSetData candidate = availableSets[i];
                if (candidate != null && candidate.setId == saveData.setId)
                {
                    instance.template = candidate;
                    break;
                }
            }
        }

        HashSet<GearBonusStatType> usedStats = new HashSet<GearBonusStatType>();

        if (saveData.slotStatTypes != null && saveData.slotPercentValues != null)
        {
            int count = Mathf.Min(saveData.slotStatTypes.Count, saveData.slotPercentValues.Count);
            for (int i = 0; i < count && instance.unlockedSlots.Count < MaxBonusSlots; i++)
            {
                GearBonusStatType statType = (GearBonusStatType)saveData.slotStatTypes[i];
                if (!IsAllowedBonusStat(statType) || usedStats.Contains(statType))
                {
                    continue;
                }

                usedStats.Add(statType);
                instance.unlockedSlots.Add(new GearSlotBonus
                {
                    statType = statType,
                    percentValue = ClampAndRoundPercent(saveData.slotPercentValues[i])
                });
            }
        }

        int minimumLevelFromSlots = instance.unlockedSlots.Count + 1;
        instance.level = Mathf.Clamp(instance.level, minimumLevelFromSlots, instance.MaxLevel);

        if (instance.level >= instance.MaxLevel)
        {
            instance.currentXp = 0;
        }
        else
        {
            instance.currentXp = Mathf.Clamp(instance.currentXp, 0, instance.GetXpToNextLevel() - 1);
        }

        if (string.IsNullOrEmpty(instance.instanceId))
        {
            instance.instanceId = Guid.NewGuid().ToString();
        }

        return instance;
    }

    private void UnlockRandomSlot()
    {
        if (unlockedSlots == null)
        {
            unlockedSlots = new List<GearSlotBonus>();
        }

        if (unlockedSlots.Count >= MaxBonusSlots)
        {
            return;
        }

        List<GearBonusStatType> candidates = new List<GearBonusStatType>();
        for (int i = 0; i < AllowedBonusStats.Length; i++)
        {
            GearBonusStatType candidate = AllowedBonusStats[i];
            if (!HasSlotForStat(candidate))
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
        GearBonusStatType selectedStat = candidates[selectedIndex];

        float rolled = UnityEngine.Random.Range(MinBonusPercent, MaxBonusPercent);
        float rounded = ClampAndRoundPercent(rolled);

        unlockedSlots.Add(new GearSlotBonus
        {
            statType = selectedStat,
            percentValue = rounded
        });
    }

    private bool HasSlotForStat(GearBonusStatType statType)
    {
        if (unlockedSlots == null)
        {
            return false;
        }

        for (int i = 0; i < unlockedSlots.Count; i++)
        {
            if (unlockedSlots[i].statType == statType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedBonusStat(GearBonusStatType statType)
    {
        for (int i = 0; i < AllowedBonusStats.Length; i++)
        {
            if (AllowedBonusStats[i] == statType)
            {
                return true;
            }
        }

        return false;
    }

    private static float ClampAndRoundPercent(float value)
    {
        float clamped = Mathf.Clamp(value, MinBonusPercent, MaxBonusPercent);
        return Mathf.Round(clamped * 100f) / 100f;
    }
}

[Serializable]
public struct GearSlotBonus
{
    public GearBonusStatType statType;
    public float percentValue;
}

[Serializable]
public class GearInstanceSaveData
{
    public string instanceId;
    public string setId;
    public int level;
    public int currentXp;
    public List<int> slotStatTypes = new List<int>();
    public List<float> slotPercentValues = new List<float>();
}

[Serializable]
public class GearProgressionSaveData
{
    public List<GearInstanceSaveData> instances = new List<GearInstanceSaveData>();
    public List<string> heroIds = new List<string>();
    public List<string> equippedInstanceIds = new List<string>();
    public int currency;
}
