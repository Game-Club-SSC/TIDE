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

        PartyManager partyManager = PartyManager.Instance;
        if (partyManager == null || partyManager.PartyData == null)
        {
            failureReason = "Party manager not initialized.";
            return false;
        }

        // Validate against the live party so a swap can never be requested
        // for a hero that is not actually in the given slot.
        if (!partyManager.PartyData.IsHeroActive(activeHeroId))
        {
            failureReason = $"'{activeHeroId}' is not in the active party.";
            return false;
        }

        if (!partyManager.PartyData.IsHeroInReserve(reserveHeroId))
        {
            failureReason = $"'{reserveHeroId}' is not in the reserve party.";
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
