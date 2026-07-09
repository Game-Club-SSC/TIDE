using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IslandVisualProfileTest : MonoBehaviour
{
    private static readonly string[] AllIslandIds = new string[]
    {
        "island_lust",
        "island_greed",
        "island_greed",
        "island_desire",
        "island_anger",
        "island_envy",
        "island_ego"
    };

    [ContextMenu("Run All Island Visual Profile Tests")]
    public void RunAllTests()
    {
        TestGetDefaultCoversAllSevenIslands();
        Debug.Log("=== All Island Visual Profile Tests Passed ===");
    }

    [ContextMenu("Test GetDefault Covers All 7 Islands")]
    public void TestGetDefaultCoversAllSevenIslands()
    {
        Debug.Log("[IslandVisualProfileTest] Testing GetDefault() covers all 7 islands...");

        for (int i = 0; i < AllIslandIds.Length; i++)
        {
            string islandId = AllIslandIds[i];
            IslandVisualProfile profile = IslandVisualProfile.GetDefault(islandId);
            Assert.IsNotNull(profile, $"GetDefault('{islandId}') should not return null.");
            Assert.AreEqual(islandId, profile.islandId, $"Profile islandId should be '{islandId}'.");
            Assert.IsNotNull(profile.groundGradient, $"Profile for '{islandId}' should have a ground gradient.");
            Assert.IsTrue(profile.tideColors.Length > 0, $"Profile for '{islandId}' should have tide colors.");
            DestroyImmediate(profile);
        }

        IslandVisualProfile nullProfile = IslandVisualProfile.GetDefault(null);
        Assert.IsNull(nullProfile, "GetDefault(null) should return null.");

        IslandVisualProfile emptyProfile = IslandVisualProfile.GetDefault("");
        Assert.IsNull(emptyProfile, "GetDefault('') should return null.");

        IslandVisualProfile unknownProfile = IslandVisualProfile.GetDefault("island_unknown");
        Assert.IsNull(unknownProfile, "GetDefault('island_unknown') should return null.");

        Debug.Log("[IslandVisualProfileTest] TestGetDefaultCoversAllSevenIslands passed.");
    }
}
