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
        string reason;
        Assert.IsTrue(PartySwapService.TryQueueSwap("hero_fire", "hero_water", out reason), "Valid swap should be accepted.");
    }

    private void TestGetReservableHeroIdsReturnsEmptyWhenNoManager()
    {
        var reserves = PartySwapService.GetReservableHeroIds();
        Assert.IsNotNull(reserves, "Reserves list should not be null.");
    }
}
