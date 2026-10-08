#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PartySwapServiceTest : MonoBehaviour
{
    [ContextMenu("Run Party Swap Service Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Party Swap Service Tests ===");

        TestTryQueueSwapRejectsSelfSwap();
        TestTryQueueSwapRejectsEmptyIds();
        TestTryQueueSwapAcceptsValidRequest();
        TestGetReservableHeroIdsReturnsEmptyWhenNoManager();

        Debug.Log("=== All Party Swap Service Tests Passed ===");
    }

    private void TestTryQueueSwapRejectsSelfSwap()
    {
        string reason;
        Assert.IsFalse(PartySwapService.TryQueueSwap("hero_fire", "hero_fire", out reason), "Should reject self-swap.");
        Assert.IsFalse(string.IsNullOrEmpty(reason), "Should provide a reason.");
    }

    private void TestTryQueueSwapRejectsEmptyIds()
    {
        string reason;
        Assert.IsFalse(PartySwapService.TryQueueSwap("", "hero_fire", out reason), "Should reject empty active id.");
        Assert.IsFalse(PartySwapService.TryQueueSwap("hero_fire", "", out reason), "Should reject empty reserve id.");
    }

    private void TestTryQueueSwapAcceptsValidRequest()
    {
        // TryQueueSwap validates the request against the live party, so the
        // suite stands up a manager with a real active/reserve split. SendMessage
        // runs OnEnable in edit mode, where AddComponent does not.
        PartyManager partyManager = null;
        PartyData data = null;
        try
        {
            GameObject host = new GameObject("TestPartySwap_Party");
            partyManager = host.AddComponent<PartyManager>();
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            PartyData authored = Resources.Load<PartyData>("PartyData/DefaultParty");
            Assert.IsNotNull(authored, "DefaultParty asset must exist.");
            data = ScriptableObject.CreateInstance<PartyData>();
            data.activeSlots = (HeroData[])authored.activeSlots.Clone();
            data.reserveSlots = (HeroData[])authored.reserveSlots.Clone();
            partyManager.Initialize(data, null);

            string activeHeroId = data.activeSlots[0].heroId;
            string reserveHeroId = data.reserveSlots[0].heroId;

            string reason;
            Assert.IsTrue(PartySwapService.TryQueueSwap(activeHeroId, reserveHeroId, out reason), $"Valid swap should be accepted. {reason}");
            Assert.IsFalse(PartySwapService.TryQueueSwap(reserveHeroId, activeHeroId, out reason), "Swap must not be accepted with the slots reversed.");
            Assert.IsFalse(PartySwapService.TryQueueSwap("hero_missing", reserveHeroId, out reason), "Unknown hero must be rejected.");
        }
        finally
        {
            if (data != null)
            {
                DestroyImmediate(data);
            }

            if (partyManager != null)
            {
                DestroyImmediate(partyManager.gameObject);
            }
        }
    }

    private void TestGetReservableHeroIdsReturnsEmptyWhenNoManager()
    {
        var reserves = PartySwapService.GetReservableHeroIds();
        Assert.IsNotNull(reserves, "Reserves list should not be null.");
    }
}
#endif
