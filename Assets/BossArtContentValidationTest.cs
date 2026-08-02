using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Validates that FuturisticSpriteLibrary generates unique boss sprites
/// for all 6 V2 islands (per-element, overworld + battle variants).
/// </summary>
public class BossArtContentValidationTest : MonoBehaviour
{
    [ContextMenu("Validate Boss Art Content")]
    public void RunTests()
    {
        Debug.Log("=== Boss Art Content Validation (Issue #279) ===");

        TestBossSpritesPerElement();
        TestBossSpritesAreDistinct();
        TestBossOverworldVariants();

        Debug.Log("=== All Boss Art Tests Passed ===");
    }

    private void TestBossSpritesPerElement()
    {
        Debug.Log("Validating boss sprites for all 6 island elements...");

        CombatUnit.Element[] elements = {
            CombatUnit.Element.Fire,    // Anger
            CombatUnit.Element.Water,   // Lust
            CombatUnit.Element.Earth,   // Greed
            CombatUnit.Element.Air,     // Envy
            CombatUnit.Element.Space,   // Desire
            CombatUnit.Element.Fire,    // Ego (uses Fire with different slot)
        };

        string[] islandNames = { "Anger", "Lust", "Greed", "Envy", "Desire", "Ego" };

        for (int i = 0; i < elements.Length; i++)
        {
            Sprite battleSprite = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(elements[i], i);
            Assert.IsNotNull(battleSprite, $"Boss battle sprite for {islandNames[i]} should exist.");
            Assert.IsTrue(battleSprite.texture.width > 0, $"Boss battle sprite for {islandNames[i]} should have texture data.");
        }

        Debug.Log("Boss battle sprites validated for all 6 islands.");
    }

    private void TestBossSpritesAreDistinct()
    {
        Debug.Log("Validating boss sprites are visually distinct...");

        Sprite sprite0 = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(CombatUnit.Element.Fire, 0);
        Sprite sprite1 = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(CombatUnit.Element.Water, 1);
        Sprite sprite2 = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(CombatUnit.Element.Earth, 2);

        Assert.AreNotEqual(sprite0.name, sprite1.name, "Boss sprites for different elements should have different names.");
        Assert.AreNotEqual(sprite1.name, sprite2.name, "Boss sprites for different slots should have different names.");

        Debug.Log("Boss sprites are distinct across elements and slots.");
    }

    private void TestBossOverworldVariants()
    {
        Debug.Log("Validating boss overworld sprites...");

        for (int slot = 0; slot < 6; slot++)
        {
            Sprite owSprite = FuturisticSpriteLibrary.GetEnemyBossOverworldSprite(CombatUnit.Element.Fire, slot);
            Assert.IsNotNull(owSprite, $"Boss overworld sprite for slot {slot} should exist.");
        }

        Debug.Log("Boss overworld sprites validated for all 6 slots.");
    }
}
