using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyData", menuName = "TIDE/Party Data")]
public class PartyData : ScriptableObject
{
    [Header("Active Party (3 in battle)")]
    public HeroData[] activeSlots = new HeroData[3];

    [Header("Reserve Party (2 on bench)")]
    public HeroData[] reserveSlots = new HeroData[2];

    public HeroData[] GetAllHeroes()
    {
        List<HeroData> all = new List<HeroData>(5);
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null)
            {
                all.Add(activeSlots[i]);
            }
        }

        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] != null)
            {
                all.Add(reserveSlots[i]);
            }
        }

        return all.ToArray();
    }

    public HeroData GetMainCharacter()
    {
        HeroData[] all = GetAllHeroes();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].isMainCharacter)
            {
                return all[i];
            }
        }

        return null;
    }

    public bool Contains(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return false;
        }

        HeroData[] all = GetAllHeroes();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].heroId == heroId)
            {
                return true;
            }
        }

        return false;
    }

    public int GetActiveCount()
    {
        int count = 0;
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    public int GetReserveCount()
    {
        int count = 0;
        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    public bool SwapActiveReserve(int activeIndex, int reserveIndex)
    {
        if (activeIndex < 0 || activeIndex >= activeSlots.Length)
        {
            Debug.LogWarning($"[PartyData] Invalid active index: {activeIndex}");
            return false;
        }

        if (reserveIndex < 0 || reserveIndex >= reserveSlots.Length)
        {
            Debug.LogWarning($"[PartyData] Invalid reserve index: {reserveIndex}");
            return false;
        }

        HeroData temp = activeSlots[activeIndex];
        activeSlots[activeIndex] = reserveSlots[reserveIndex];
        reserveSlots[reserveIndex] = temp;
        return true;
    }

    public bool IsValid()
    {
        if (GetActiveCount() == 0)
        {
            return false;
        }

        HeroData[] all = GetAllHeroes();
        HashSet<string> seenIds = new HashSet<string>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }

            if (!all[i].IsValid())
            {
                return false;
            }

            if (!seenIds.Add(all[i].heroId))
            {
                Debug.LogWarning($"[PartyData] Duplicate heroId: {all[i].heroId}");
                return false;
            }
        }

        return true;
    }

    public bool ToggleHeroActive(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            return false;
        }

        int activeIndex = FindActiveIndex(heroId);
        if (activeIndex >= 0)
        {
            int reserveSlot = FindFirstEmptyReserveSlot();
            if (reserveSlot >= 0)
            {
                reserveSlots[reserveSlot] = activeSlots[activeIndex];
                activeSlots[activeIndex] = null;
                return true;
            }

            int swapIdx = FindFirstReserveSlotForSwap();
            if (swapIdx >= 0)
            {
                HeroData temp = activeSlots[activeIndex];
                activeSlots[activeIndex] = reserveSlots[swapIdx];
                reserveSlots[swapIdx] = temp;
                return true;
            }

            Debug.LogWarning("[PartyData] No empty reserve slot available.");
            return false;
        }

        int reserveIdx = FindReserveIndex(heroId);
        if (reserveIdx >= 0)
        {
            int activeSlot = FindFirstEmptyActiveSlot();
            if (activeSlot >= 0)
            {
                activeSlots[activeSlot] = reserveSlots[reserveIdx];
                reserveSlots[reserveIdx] = null;
                return true;
            }

            int swapIdx = FindFirstActiveSlotForSwap();
            if (swapIdx >= 0)
            {
                HeroData temp = reserveSlots[reserveIdx];
                reserveSlots[reserveIdx] = activeSlots[swapIdx];
                activeSlots[swapIdx] = temp;
                return true;
            }

            Debug.LogWarning("[PartyData] Active party is full (3/3). Remove a hero first.");
            return false;
        }

        Debug.LogWarning($"[PartyData] Hero '{heroId}' not found in party.");
        return false;
    }

    public bool SetActiveParty(string[] heroIds)
    {
        if (heroIds == null || heroIds.Length != 3)
        {
            Debug.LogWarning("[PartyData] SetActiveParty requires exactly 3 hero IDs.");
            return false;
        }

        HeroData[] all = GetAllHeroes();
        Dictionary<string, HeroData> heroLookup = new Dictionary<string, HeroData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !string.IsNullOrEmpty(all[i].heroId))
            {
                heroLookup[all[i].heroId] = all[i];
            }
        }

        HeroData[] newActive = new HeroData[3];
        HeroData[] newReserve = new HeroData[2];
        HashSet<string> usedIds = new HashSet<string>();

        for (int i = 0; i < 3; i++)
        {
            if (string.IsNullOrEmpty(heroIds[i]) || !heroLookup.ContainsKey(heroIds[i]))
            {
                Debug.LogWarning($"[PartyData] Hero '{heroIds[i]}' not found in party.");
                return false;
            }

            if (!usedIds.Add(heroIds[i]))
            {
                Debug.LogWarning($"[PartyData] Duplicate hero ID in active party: '{heroIds[i]}'.");
                return false;
            }

            newActive[i] = heroLookup[heroIds[i]];
        }

        int reserveIdx = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !usedIds.Contains(all[i].heroId) && reserveIdx < 2)
            {
                newReserve[reserveIdx] = all[i];
                reserveIdx++;
            }
        }

        activeSlots = newActive;
        reserveSlots = newReserve;
        return true;
    }

    public bool IsHeroActive(string heroId)
    {
        return FindActiveIndex(heroId) >= 0;
    }

    public bool IsHeroInReserve(string heroId)
    {
        return FindReserveIndex(heroId) >= 0;
    }

    private int FindActiveIndex(string heroId)
    {
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null && activeSlots[i].heroId == heroId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindReserveIndex(string heroId)
    {
        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] != null && reserveSlots[i].heroId == heroId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstEmptyActiveSlot()
    {
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstEmptyReserveSlot()
    {
        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstReserveSlotForSwap()
    {
        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstActiveSlotForSwap()
    {
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] != null)
            {
                return i;
            }
        }

        return -1;
    }
}
