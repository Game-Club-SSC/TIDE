using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class SlothIslandVerificationTest : MonoBehaviour
{
    private const string IslandId = "island_sloth";

    [ContextMenu("Run Sloth Island Verification")]
    public void RunTests()
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(IslandId);
        Assert.IsNotNull(config, "Sloth island config should load from Resources/Islands.");
        Assert.AreEqual("Sloth", config.viceName, "Sloth island should expose the correct vice name.");
        Assert.AreEqual("island_wrath", IslandThemeRegistry.GetNextIslandId(IslandId),
            "Clearing Sloth should unlock Wrath next in progression order.");
    }
}
