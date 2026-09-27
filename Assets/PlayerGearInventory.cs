using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent player gear inventory. Owns the "gearInventory" top-level save
/// section and is the award/equip API for battle drops (GameStateManager),
/// dialogue item rewards (issue 297) and the inventory UI (issue 294/295).
/// Gear instance state (level, XP, rolled bonus slots) is delegated to
/// HeroProgressionManager, which already applies stat bonuses to the party.
/// </summary>
[DisallowMultipleComponent]
public class PlayerGearInventory : MonoBehaviour
{
    public static PlayerGearInventory Instance { get; private set; }

    public event Action<GearInstance> OnGearAcquired;
    public event Action<GearInstance> OnGearEquippedChanged;

    private readonly List<GearInstance> lastBattleDrops = new List<GearInstance>();
    private bool hasRecordedBattleDrop;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ----- Awarding (drop / dialogue rewards) -----

    /// <summary>Adds a new owned gear instance for the given set id + rarity. Returns the instance, or null if the set is unknown.</summary>
    public GearInstance AddGear(string gearSetId, GearDropService.GearRarity rarity)
    {
        if (string.IsNullOrEmpty(gearSetId))
        {
            return null;
        }

        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null)
        {
            return null;
        }

        GearSetData template = FindGearSet(progression, gearSetId);
        if (template == null)
        {
            Debug.LogWarning($"[PlayerGearInventory] Cannot award gear '{gearSetId}': no matching gear set registered.");
            return null;
        }

        return AddGear(template, rarity);
    }

    /// <summary>Creates a new owned instance of the template with the given rarity. Each call yields a distinct instance.</summary>
    public GearInstance AddGear(GearSetData template, GearDropService.GearRarity rarity)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null || template == null || !template.IsValid())
        {
            return null;
        }

        GearInstance instance = progression.CreateGearInstance(template);
        if (instance == null)
        {
            return null;
        }

        instance.rarity = ClampRarity(rarity);
        progression.RegisterGearInstance(instance);

        Debug.Log($"[PlayerGearInventory] Awarded gear '{instance.setId}' ({instance.rarity}) as instance '{instance.instanceId}'.");
        OnGearAcquired?.Invoke(instance);
        return instance;
    }

    /// <summary>Registers an existing instance (e.g. smithy duplicate) as owned with the given rarity.</summary>
    public bool RegisterOwnedGear(GearInstance instance, GearDropService.GearRarity rarity)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null || instance == null || string.IsNullOrEmpty(instance.instanceId))
        {
            return false;
        }

        instance.rarity = ClampRarity(rarity);
        progression.RegisterGearInstance(instance);
        OnGearAcquired?.Invoke(instance);
        return true;
    }

    // ----- Owned listing -----

    /// <summary>All owned gear instances (sorted by set name, level, id).</summary>
    public IReadOnlyList<GearInstance> GetOwnedGear()
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        return progression != null ? progression.GetAllGearInstances() : Array.Empty<GearInstance>();
    }

    public int OwnedGearCount
    {
        get
        {
            HeroProgressionManager progression = HeroProgressionManager.Instance;
            return progression != null ? progression.GetAllGearInstances().Count : 0;
        }
    }

    // ----- Equip / unequip -----

    public bool TryEquip(string heroId, string instanceId)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null || string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        GearInstance instance = progression.GetGearInstance(instanceId);
        if (instance == null)
        {
            return false;
        }

        progression.EnsureHero(heroId);
        progression.EquipGearInstance(heroId, instance);
        OnGearEquippedChanged?.Invoke(instance);
        return true;
    }

    public bool TryUnequip(string heroId)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null || string.IsNullOrEmpty(heroId))
        {
            return false;
        }

        GearInstance equipped = progression.GetEquippedGearInstance(heroId);
        if (equipped == null)
        {
            return false;
        }

        progression.UnequipGearSet(heroId);
        OnGearEquippedChanged?.Invoke(equipped);
        return true;
    }

    public GearInstance GetEquipped(string heroId)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        return progression != null ? progression.GetEquippedGearInstance(heroId) : null;
    }

    public bool IsEquipped(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        return !string.IsNullOrEmpty(GetEquippedHeroId(instanceId));
    }

    public string GetEquippedHeroId(string instanceId)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null || string.IsNullOrEmpty(instanceId))
        {
            return null;
        }

        HeroProgressionManager.HeroProgressionSnapshot heroes = progression.CaptureHeroProgressionSnapshot();
        if (heroes == null || heroes.heroIds == null)
        {
            return null;
        }

        for (int i = 0; i < heroes.heroIds.Count; i++)
        {
            string heroId = heroes.heroIds[i];
            if (string.IsNullOrEmpty(heroId))
            {
                continue;
            }

            GearInstance equipped = progression.GetEquippedGearInstance(heroId);
            if (equipped != null && equipped.instanceId == instanceId)
            {
                return heroId;
            }
        }

        return null;
    }

    // ----- Battle results exposure -----

    /// <summary>Drops awarded during the most recent battle victory. Cleared when a new combat starts.</summary>
    public IReadOnlyList<GearInstance> LastBattleDrops => lastBattleDrops;

    /// <summary>Marks a freshly awarded instance as a battle drop and exposes it in results.</summary>
    public void RecordBattleDrop(GearInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!hasRecordedBattleDrop)
        {
            lastBattleDrops.Clear();
            hasRecordedBattleDrop = true;
        }

        lastBattleDrops.Add(instance);
    }

    /// <summary>Called when entering combat so the previous battle's drops no longer read as "new".</summary>
    public void BeginNewCombat()
    {
        hasRecordedBattleDrop = false;
        lastBattleDrops.Clear();
    }

    public void ClearBattleDrops()
    {
        hasRecordedBattleDrop = false;
        lastBattleDrops.Clear();
    }

    public static string GetDisplayName(GearInstance instance)
    {
        if (instance == null)
        {
            return "Unknown gear";
        }

        if (instance.template != null && !string.IsNullOrEmpty(instance.template.displayName))
        {
            return instance.template.displayName;
        }

        return instance.setId;
    }

    public static string GetDropSummary(GearInstance instance)
    {
        if (instance == null)
        {
            return string.Empty;
        }

        return $"{GetDisplayName(instance)} ({instance.rarity})";
    }

    // ----- Save / restore -----

    public GearInventorySaveData CaptureGearInventorySnapshot()
    {
        GearInventorySaveData snapshot = new GearInventorySaveData();
        IReadOnlyList<GearInstance> owned = GetOwnedGear();
        for (int i = 0; i < owned.Count; i++)
        {
            GearInstance instance = owned[i];
            if (instance == null || string.IsNullOrEmpty(instance.setId))
            {
                continue;
            }

            snapshot.entries.Add(new GearInventoryEntrySaveData
            {
                instanceId = instance.instanceId,
                setId = instance.setId,
                rarity = (int)ClampRarity(instance.rarity),
                level = Mathf.Clamp(instance.level, 1, instance.MaxLevel),
                currentXp = Mathf.Max(0, instance.currentXp),
                slotStatTypes = ToStatTypeList(instance),
                slotPercentValues = ToPercentValueList(instance)
            });
        }

        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression != null)
        {
            HeroProgressionManager.HeroProgressionSnapshot heroes = progression.CaptureHeroProgressionSnapshot();
            if (heroes != null && heroes.heroIds != null)
            {
                for (int i = 0; i < heroes.heroIds.Count; i++)
                {
                    string heroId = heroes.heroIds[i];
                    if (string.IsNullOrEmpty(heroId))
                    {
                        continue;
                    }

                    GearInstance equipped = progression.GetEquippedGearInstance(heroId);
                    if (equipped == null)
                    {
                        continue;
                    }

                    snapshot.equippedHeroIds.Add(heroId);
                    snapshot.equippedInstanceIds.Add(equipped.instanceId);
                }
            }
        }

        return snapshot;
    }

    public void ApplyGearInventorySnapshot(GearInventorySaveData snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null)
        {
            return;
        }

        GearSetData[] availableSets = progression.AvailableGearSets;
        HashSet<string> restoredInstanceIds = new HashSet<string>(StringComparer.Ordinal);

        if (snapshot.entries != null)
        {
            for (int i = 0; i < snapshot.entries.Count; i++)
            {
                GearInventoryEntrySaveData entry = snapshot.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.instanceId))
                {
                    continue;
                }

                GearInstance instance = progression.GetGearInstance(entry.instanceId);
                if (instance == null)
                {
                    instance = GearInstance.FromSaveData(ToGearInstanceSaveData(entry), availableSets);
                }

                if (instance == null)
                {
                    continue;
                }

                instance.rarity = ClampRarity((GearDropService.GearRarity)entry.rarity);
                progression.RegisterGearInstance(instance);
                restoredInstanceIds.Add(instance.instanceId);
            }
        }

        if (snapshot.equippedHeroIds != null && snapshot.equippedInstanceIds != null)
        {
            int count = Mathf.Min(snapshot.equippedHeroIds.Count, snapshot.equippedInstanceIds.Count);
            for (int i = 0; i < count; i++)
            {
                string heroId = snapshot.equippedHeroIds[i];
                string instanceId = snapshot.equippedInstanceIds[i];
                if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(instanceId))
                {
                    continue;
                }

                if (!restoredInstanceIds.Contains(instanceId) && progression.GetGearInstance(instanceId) == null)
                {
                    continue;
                }

                TryEquip(heroId, instanceId);
            }
        }
    }

    public void ResetInventoryForDebug()
    {
        lastBattleDrops.Clear();
        hasRecordedBattleDrop = false;
    }

    // ----- Internals -----

    private static GearSetData FindGearSet(HeroProgressionManager progression, string gearSetId)
    {
        GearSetData[] sets = progression.AvailableGearSets;
        if (sets == null)
        {
            return null;
        }

        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] != null && sets[i].setId == gearSetId)
            {
                return sets[i];
            }
        }

        return null;
    }

    private static GearDropService.GearRarity ClampRarity(GearDropService.GearRarity rarity)
    {
        int value = (int)rarity;
        int max = (int)GearDropService.GearRarity.Legendary;
        int min = (int)GearDropService.GearRarity.Common;
        return (GearDropService.GearRarity)Mathf.Clamp(value, min, max);
    }

    private static List<int> ToStatTypeList(GearInstance instance)
    {
        List<int> types = new List<int>();
        if (instance.unlockedSlots == null)
        {
            return types;
        }

        for (int i = 0; i < instance.unlockedSlots.Count; i++)
        {
            types.Add((int)instance.unlockedSlots[i].statType);
        }

        return types;
    }

    private static List<float> ToPercentValueList(GearInstance instance)
    {
        List<float> values = new List<float>();
        if (instance.unlockedSlots == null)
        {
            return values;
        }

        for (int i = 0; i < instance.unlockedSlots.Count; i++)
        {
            values.Add(instance.unlockedSlots[i].percentValue);
        }

        return values;
    }

    private static GearInstanceSaveData ToGearInstanceSaveData(GearInventoryEntrySaveData entry)
    {
        return new GearInstanceSaveData
        {
            instanceId = entry.instanceId,
            setId = entry.setId,
            level = entry.level,
            currentXp = entry.currentXp,
            slotStatTypes = entry.slotStatTypes != null ? new List<int>(entry.slotStatTypes) : new List<int>(),
            slotPercentValues = entry.slotPercentValues != null ? new List<float>(entry.slotPercentValues) : new List<float>()
        };
    }

    // ----- Save schema (top-level "gearInventory" section) -----

    [Serializable]
    public sealed class GearInventorySaveData
    {
        public List<GearInventoryEntrySaveData> entries = new List<GearInventoryEntrySaveData>();
        public List<string> equippedHeroIds = new List<string>();
        public List<string> equippedInstanceIds = new List<string>();
    }

    [Serializable]
    public sealed class GearInventoryEntrySaveData
    {
        public string instanceId;
        public string setId;
        public int rarity;
        public int level;
        public int currentXp;
        public List<int> slotStatTypes = new List<int>();
        public List<float> slotPercentValues = new List<float>();
    }
}
