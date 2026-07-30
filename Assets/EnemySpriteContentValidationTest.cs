using UnityEngine;

/// <summary>
/// Validates that FuturisticSpriteLibrary generates enemy sprites for all
/// 5 elemental types with overworld and battle variants.
/// </summary>
public class EnemySpriteContentValidationTest : MonoBehaviour
{
    [ContextMenu("Validate Enemy Sprite Content")]
    public void RunTests()
    {
        Debug.Log("=== Enemy Sprite Content Validation (Issue #280) ===");

        TestEnemySpritesPerElement();
        TestEnemySpritesAreDistinct();
        TestEnemyOverworldVariants();

        Debug.Log("=== All Enemy Sprite Tests Passed ===");
    }

    private void TestEnemySpritesPerElement()
    {
        Debug.Log("Validating enemy sprites for all 5 elements...");

        CombatUnit.Element[] elements = {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        string[] elementNames = { "Fire", "Water", "Earth", "Air", "Space" };

        for (int i = 0; i < elements.Length; i++)
        {
            Sprite battleSprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(elements[i]);
            Assert.IsNotNull(battleSprite, $"Enemy battle sprite for {elementNames[i]} should exist.");
            Assert.IsTrue(battleSprite.texture.width > 0, $"Enemy battle sprite for {elementNames[i]} should have texture data.");
        }

        Debug.Log("Enemy battle sprites validated for all 5 elements.");
    }

    private void TestEnemySpritesAreDistinct()
    {
        Debug.Log("Validating enemy sprites are visually distinct...");

        Sprite fire = FuturisticSpriteLibrary.GetEnemyBattleSprite(CombatUnit.Element.Fire);
        Sprite water = FuturisticSpriteLibrary.GetEnemyBattleSprite(CombatUnit.Element.Water);
        Sprite earth = FuturisticSpriteLibrary.GetEnemyBattleSprite(CombatUnit.Element.Earth);

        Assert.AreNotEqual(fire.name, water.name, "Fire and Water enemy sprites should be distinct.");
        Assert.AreNotEqual(water.name, earth.name, "Water and Earth enemy sprites should be distinct.");
        Assert.AreNotEqual(fire.name, earth.name, "Fire and Earth enemy sprites should be distinct.");

        Debug.Log("Enemy sprites are distinct across elements.");
    }

    private void TestEnemyOverworldVariants()
    {
        Debug.Log("Validating enemy overworld sprites...");

        CombatUnit.Element[] elements = {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        for (int i = 0; i < elements.Length; i++)
        {
            Sprite owSprite = FuturisticSpriteLibrary.GetEnemyOverworldSprite(elements[i]);
            Assert.IsNotNull(owSprite, $"Enemy overworld sprite for element {i} should exist.");

            Sprite battleSprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(elements[i]);
            Assert.AreNotEqual(owSprite.name, battleSprite.name, $"Overworld and battle sprites for element {i} should differ.");
        }

        Debug.Log("Enemy overworld sprites validated for all 5 elements.");
    }
}
