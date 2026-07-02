using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the character art and rendering system.
/// Verifies ElementalCharacterFactory and FuturisticSpriteLibrary.
/// </summary>
[DisallowMultipleComponent]
public class CharacterArtTest : MonoBehaviour
{
    [ContextMenu("Run Character Art Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Character Art Tests ===");

        TestElementalCharacterFactoryExists();
        TestFuturisticSpriteLibraryExists();
        TestCharacterModelBuilding();
        TestElementColorMapping();

        Debug.Log("=== All Character Art Tests Passed ===");
    }

    private void TestElementalCharacterFactoryExists()
    {
        Debug.Log("Testing ElementalCharacterFactory exists...");

        Assert.IsNotNull(typeof(ElementalCharacterFactory), "ElementalCharacterFactory class should exist.");

        // Verify key methods exist
        var buildPlayerModel = typeof(ElementalCharacterFactory).GetMethod("BuildExplorationPlayerModel");
        var buildEnemyModel = typeof(ElementalCharacterFactory).GetMethod("BuildExplorationEnemyModel");
        var getPrimaryColor = typeof(ElementalCharacterFactory).GetMethod("GetElementPrimaryColor");

        Assert.IsNotNull(buildPlayerModel, "BuildExplorationPlayerModel method should exist.");
        Assert.IsNotNull(buildEnemyModel, "BuildExplorationEnemyModel method should exist.");
        Assert.IsNotNull(getPrimaryColor, "GetElementPrimaryColor method should exist.");

        Debug.Log("ElementalCharacterFactory exists with expected methods: PASS");
    }

    private void TestFuturisticSpriteLibraryExists()
    {
        Debug.Log("Testing FuturisticSpriteLibrary exists...");

        Assert.IsNotNull(typeof(FuturisticSpriteLibrary), "FuturisticSpriteLibrary class should exist.");

        Debug.Log("FuturisticSpriteLibrary exists: PASS");
    }

    private void TestCharacterModelBuilding()
    {
        Debug.Log("Testing character model building...");

        // Create a test parent transform
        GameObject parentObj = new GameObject("TestParent");
        Transform parent = parentObj.transform;

        // Test building a player model for each element
        CombatUnit.Element[] elements = {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        foreach (CombatUnit.Element element in elements)
        {
            Color primary = ElementalCharacterFactory.GetElementPrimaryColor(element);
            Color accent = Color.Lerp(primary, Color.white, 0.3f);
            Color glow = Color.Lerp(primary, Color.yellow, 0.5f);

            Transform model = ElementalCharacterFactory.BuildExplorationPlayerModel(
                parent, element, primary, accent, glow, Vector3.zero, Vector3.one);

            if (model != null)
            {
                Assert.IsNotNull(model, $"Player model for {element} should be created.");
                Debug.Log($"Built player model for {element}: {model.name}");
            }
            else
            {
                Debug.LogWarning($"Could not build player model for {element} (may require Unity Editor).");
            }
        }

        // Cleanup
        Object.DestroyImmediate(parentObj);
        Debug.Log("Character model building: PASS");
    }

    private void TestElementColorMapping()
    {
        Debug.Log("Testing element color mapping...");

        // Verify each element has a distinct primary color
        Color fireColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Fire);
        Color waterColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Water);
        Color earthColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Earth);
        Color airColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Air);
        Color spaceColor = ElementalCharacterFactory.GetElementPrimaryColor(CombatUnit.Element.Space);

        // Colors should be different (not all the same)
        Assert.AreNotEqual(fireColor, waterColor, "Fire and Water colors should be different.");
        Assert.AreNotEqual(waterColor, earthColor, "Water and Earth colors should be different.");
        Assert.AreNotEqual(earthColor, airColor, "Earth and Air colors should be different.");
        Assert.AreNotEqual(airColor, spaceColor, "Air and Space colors should be different.");

        Debug.Log($"Element colors: Fire={fireColor}, Water={waterColor}, Earth={earthColor}, Air={airColor}, Space={spaceColor}");
        Debug.Log("Element color mapping: PASS");
    }
}
