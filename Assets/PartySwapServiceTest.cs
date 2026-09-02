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
        // TryQueueSwap requires an initialized HeroProgressionManager. Stand up
        // an isolated one when none exists; SendMessage runs OnEnable in edit
        // mode, where AddComponent does not invoke lifecycle callbacks.
        GameObject host = null;
        if (HeroProgressionManager.Instance == null)
        {
            host = new GameObject("TestPartySwap_Progression");
            host.AddComponent<HeroProgressionManager>();
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        }

        try
        {
            string reason;
            Assert.IsTrue(PartySwapService.TryQueueSwap("hero_fire", "hero_water", out reason), "Valid swap should be accepted.");
        }
        finally
        {
            if (host != null)
            {
                DestroyImmediate(host);
            }
        }
    }

    private void TestGetReservableHeroIdsReturnsEmptyWhenNoManager()
    {
        var reserves = PartySwapService.GetReservableHeroIds();
        Assert.IsNotNull(reserves, "Reserves list should not be null.");
    }
}
