using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PrideIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_pride";

    [ContextMenu("Run Pride Island Verification")]
    public void RunTests()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Pride island config should load from Resources/Islands.");
        Assert.AreEqual("Pride", config.viceName, "Pride island should expose the correct vice name.");
        Assert.AreEqual(IslandId, IslandThemeRegistry.GetNextIslandId(IslandId),
            "Pride should be the terminal island in progression order.");
    }
}
