using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the environment art and island theming system.
/// Verifies IslandThemeRegistry and IslandConfig.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentArtTest : MonoBehaviour
{
    [ContextMenu("Run Environment Art Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Environment Art Tests ===");

        TestIslandThemeRegistryExists();
        TestIslandConfigExists();
        TestIslandProgressionOrder();
        TestIslandConfigurations();
        TestCorruptionVisualSystem();
        TestTilesetTextures();

        Debug.Log("=== All Environment Art Tests Passed ===");
    }

    private void TestIslandThemeRegistryExists()
    {
        Debug.Log("Testing IslandThemeRegistry exists...");

        Assert.IsNotNull(typeof(IslandThemeRegistry), "IslandThemeRegistry class should exist.");

        // Verify progression order exists
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        Assert.IsNotNull(progressionOrder, "Progression order should exist.");
        // ProgressionOrder currently contains 6 canonical V2 island IDs;
        // legacy aliases are handled by IslandThemeRegistry.
        Assert.GreaterOrEqual(progressionOrder.Count, 6, "Should have at least 6 islands.");

        Debug.Log($"IslandThemeRegistry exists with {progressionOrder.Count} islands: PASS");
    }

    private void TestIslandConfigExists()
    {
        Debug.Log("Testing IslandConfig exists...");

        Assert.IsNotNull(typeof(IslandConfig), "IslandConfig class should exist.");

        // Verify IslandConfig has expected fields
        var islandIdField = typeof(IslandConfig).GetField("islandId");
        var viceNameField = typeof(IslandConfig).GetField("viceName");
        var vicePrimaryColorField = typeof(IslandConfig).GetField("vicePrimaryColor");

        Assert.IsNotNull(islandIdField, "IslandConfig should have islandId field.");
        Assert.IsNotNull(viceNameField, "IslandConfig should have viceName field.");
        Assert.IsNotNull(vicePrimaryColorField, "IslandConfig should have vicePrimaryColor field.");

        Debug.Log("IslandConfig exists with expected fields: PASS");
    }

    private void TestIslandProgressionOrder()
    {
        Debug.Log("Testing island progression order...");

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;

        // Verify expected islands exist in order. This matches the current
        // IslandThemeRegistry.ProgressionOrder; the legacy-&gt;canonical rename is
        // tracked as a separate refactor.
        string[] expectedIslands = {
            "island_lust",
            "island_greed",
            "island_desire",
            "island_anger",
            "island_envy",
            "island_ego"
        };

        for (int i = 0; i < expectedIslands.Length && i < progressionOrder.Count; i++)
        {
            Assert.AreEqual(expectedIslands[i], progressionOrder[i],
                $"Island at position {i} should be {expectedIslands[i]}.");
        }

        Debug.Log("Island progression order: PASS");
    }

    private void TestIslandConfigurations()
    {
        Debug.Log("Testing island configurations...");

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;

        foreach (string islandId in progressionOrder)
        {
            IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
            if (config != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(config.islandId), $"Island {islandId} should have an ID.");
                Assert.IsFalse(string.IsNullOrEmpty(config.viceName), $"Island {islandId} should have a vice name.");

                // Verify colors are set (not default black)
                Assert.IsTrue(config.vicePrimaryColor != Color.black || config.viceSecondaryColor != Color.black,
                    $"Island {islandId} should have non-default colors.");

                Debug.Log($"Island {islandId}: vice={config.viceName}, primary={config.vicePrimaryColor}");
            }
            else
            {
                Debug.LogWarning($"Island {islandId} config not found (may need to be assigned in Inspector).");
            }
        }

        Debug.Log("Island configurations: PASS");
    }

    private void TestTilesetTextures()
    {
        Debug.Log("Testing tileset texture loading...");

        string[] canonicalIds = {
            "island_lust",
            "island_greed",
            "island_anger",
            "island_desire",
            "island_envy",
            "island_ego"
        };

        foreach (string islandId in canonicalIds)
        {
            IslandVisualProfile profile = IslandVisualProfile.GetDefault(islandId);
            Assert.IsNotNull(profile, $"GetDefault('{islandId}') should return a profile.");
            Assert.IsNotNull(profile.groundTexture, $"Island {islandId} should load a ground texture.");
            Assert.IsNotNull(profile.wallTexture, $"Island {islandId} should load a wall texture.");
            Assert.IsNotNull(profile.waterTexture, $"Island {islandId} should load a water texture.");
            DestroyImmediate(profile);
        }

        Debug.Log("Tileset texture loading: PASS");
    }

    private void TestCorruptionVisualSystem()
    {
        Debug.Log("Testing corruption visual system...");

        // Verify TideTile exists and has corruption visuals
        Assert.IsNotNull(typeof(TideTile), "TideTile class should exist.");

        // Verify GroundRestorationVisualizer exists
        Assert.IsNotNull(typeof(GroundRestorationVisualizer), "GroundRestorationVisualizer class should exist.");

        // Test TideTile color calculation
        TideTile testTile = new GameObject("TestTile").AddComponent<TideTile>();

        // Configure tile at different tide values
        testTile.Configure(Vector2Int.zero, 1, false); // Excess evil
        Color evilColor = GetTileColorViaReflection(testTile);

        testTile.Configure(Vector2Int.zero, 5, false); // Balanced
        Color balancedColor = GetTileColorViaReflection(testTile);

        testTile.Configure(Vector2Int.zero, 10, false); // Excess good
        Color goodColor = GetTileColorViaReflection(testTile);

        // Evil should be darker than balanced
        Assert.Less(evilColor.grayscale, balancedColor.grayscale,
            "Evil tide (1) should be darker than balanced tide (5).");

        // Good should be lighter than balanced
        Assert.Greater(goodColor.grayscale, balancedColor.grayscale,
            "Good tide (10) should be lighter than balanced tide (5).");

        // Cleanup
        Object.DestroyImmediate(testTile.gameObject);

        Debug.Log($"Corruption visuals: evil={evilColor}, balanced={balancedColor}, good={goodColor}");
        Debug.Log("Corruption visual system: PASS");
    }

    private Color GetTileColorViaReflection(TideTile tile)
    {
        // Use the RefreshVisuals method to apply the color
        tile.RefreshVisuals();

        // Get the renderer's material color
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            return renderer.material.color;
        }

        return Color.magenta; // Fallback for testing
    }
}
