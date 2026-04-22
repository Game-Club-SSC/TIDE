using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class WrathIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_wrath";

    [ContextMenu("Run Wrath Island Verification")]
    public void RunTests()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Wrath island config should load from Resources/Islands.");
        Assert.AreEqual("Wrath", config.viceName, "Wrath island should expose the correct vice name.");
        Assert.AreEqual("island_envy", IslandThemeRegistry.GetNextIslandId(IslandId),
            "Clearing Wrath should unlock Envy next in progression order.");
    }
}
