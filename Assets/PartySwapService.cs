using System.Collections.Generic;
using UnityEngine;

public static class PartySwapService
{
    public sealed class SwapRequest
    {
        public string ActiveHeroId;
        public string ReserveHeroId;
        public string Reason;
    }

    public static bool TryQueueSwap(string activeHeroId, string reserveHeroId, out string failureReason)
    {
        failureReason = string.Empty;

        if (string.IsNullOrEmpty(activeHeroId) || string.IsNullOrEmpty(reserveHeroId))
        {
            failureReason = "Both hero ids are required.";
            return false;
        }

        if (string.Equals(activeHeroId, reserveHeroId, System.StringComparison.Ordinal))
        {
            failureReason = "Cannot swap a hero with itself.";
            return false;
        }

        if (HeroProgressionManager.Instance == null)
        {
            failureReason = "Hero progression manager not initialized.";
            return false;
        }

        return true;
    }

    public static IReadOnlyList<string> GetReservableHeroIds()
    {
        List<string> reserves = new List<string>();
        if (PartyManager.Instance == null || PartyManager.Instance.PartyData == null)
        {
            return reserves;
        }

        HeroData[] active = PartyManager.Instance.GetActiveParty();
        HashSet<string> activeSet = new HashSet<string>();
        for (int i = 0; i < active.Length; i++)
        {
            if (active[i] != null && !string.IsNullOrEmpty(active[i].heroId))
            {
                activeSet.Add(active[i].heroId);
            }
        }

        HeroData[] reserves2 = PartyManager.Instance.PartyData.reserveSlots;
        if (reserves2 == null)
        {
            return reserves;
        }

        for (int i = 0; i < reserves2.Length; i++)
        {
            HeroData hero = reserves2[i];
            if (hero == null || string.IsNullOrEmpty(hero.heroId))
            {
                continue;
            }

            if (!activeSet.Contains(hero.heroId))
            {
                reserves.Add(hero.heroId);
            }
        }

        return reserves;
    }
}
