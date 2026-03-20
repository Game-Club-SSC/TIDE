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
}
