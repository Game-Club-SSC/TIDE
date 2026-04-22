using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GreedIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_greed";

    [ContextMenu("Run Greed Island Verification")]
    public void RunTests()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Greed island config should load from Resources/Islands.");
        Assert.AreEqual("Greed", config.viceName, "Greed island should expose the correct vice name.");
        Assert.AreEqual("island_sloth", IslandThemeRegistry.GetNextIslandId(IslandId),
            "Clearing Greed should unlock Sloth next in progression order.");
    }
}
