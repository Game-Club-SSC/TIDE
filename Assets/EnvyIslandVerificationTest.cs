using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class EnvyIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_envy";

    [ContextMenu("Run Envy Island Verification")]
    public void RunTests()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Envy island config should load from Resources/Islands.");
        Assert.AreEqual("Envy", config.viceName, "Envy island should expose the correct vice name.");
        Assert.AreEqual("island_pride", IslandThemeRegistry.GetNextIslandId(IslandId),
            "Clearing Envy should unlock Pride next in progression order.");
    }
}
