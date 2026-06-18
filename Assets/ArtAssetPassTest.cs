using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class ArtAssetPassTest : MonoBehaviour
{
    [ContextMenu("Run Art Asset Pass Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Art Asset Pass Tests ===");

        TestHeroStylesCoverAllFiveElements();
        TestBossSpritesAreDistinctPerIsland();
        TestBossSpritesAreDistinctAcrossElements();
        TestEnemySpritesAreGeneratedPerElement();

        Debug.Log("=== All Art Asset Pass Tests Passed ===");
    }

    private void TestHeroStylesCoverAllFiveElements()
    {
        Debug.Log("Testing hero style coverage across all five elements...");

        CombatUnit.Element[] requiredElements =
        {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        HashSet<CombatUnit.Element> seenElements = new HashSet<CombatUnit.Element>();
        IReadOnlyList<FuturisticSpriteLibrary.PlayerStyleDefinition> styles = FuturisticSpriteLibrary.GetPlayerStyles();
        Assert.IsNotNull(styles, "GetPlayerStyles should not return null.");
        Assert.Greater(styles.Count, 0, "Player style library should have at least one entry.");

        for (int i = 0; i < styles.Count; i++)
        {
            if (!styles[i].IsPremium)
            {
                seenElements.Add(styles[i].Element);
            }
        }

        foreach (CombatUnit.Element element in requiredElements)
        {
            Assert.IsTrue(seenElements.Contains(element),
                $"Default hero style should exist for element '{element}'.");

            Sprite sprite = FuturisticSpriteLibrary.GetPlayerBattleSprite(FuturisticSpriteLibrary.GetDefaultStyleIdForElement(element));
            Assert.IsNotNull(sprite,
                $"Default hero battle sprite should be generated for element '{element}'.");
            Assert.Greater(sprite.rect.width, 0f,
                $"Hero battle sprite for element '{element}' should have non-zero width.");
        }
    }

    private void TestBossSpritesAreDistinctPerIsland()
    {
        Debug.Log("Testing boss sprites are distinct per island...");

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        HashSet<string> generatedKeys = new HashSet<string>();
        HashSet<Sprite> generatedSprites = new HashSet<Sprite>();

        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            int slotIndex = FuturisticSpriteLibrary.GetBossSlotIndexForIsland(islandId);
            CombatUnit.Element element = ResolveElementForIsland(islandId);

            Sprite sprite = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(element, slotIndex);
            Assert.IsNotNull(sprite, $"Boss battle sprite should be generated for island '{islandId}'.");

            string spriteKey = sprite.name;
            Assert.IsFalse(generatedKeys.Contains(spriteKey),
                $"Boss battle sprite for island '{islandId}' should be visually distinct from earlier bosses.");
            generatedKeys.Add(spriteKey);
            generatedSprites.Add(sprite);
        }

        Assert.GreaterOrEqual(generatedSprites.Count, 3,
            "At least three distinct boss sprites should be generated across the progression order.");
    }

    private void TestBossSpritesAreDistinctAcrossElements()
    {
        Debug.Log("Testing boss sprites are distinct across elements...");

        HashSet<string> seenSpriteNames = new HashSet<string>();
        CombatUnit.Element[] elements =
        {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        foreach (CombatUnit.Element element in elements)
        {
            Sprite sprite = FuturisticSpriteLibrary.GetEnemyBossBattleSprite(element, 0);
            Assert.IsNotNull(sprite, $"Boss battle sprite for element '{element}' at slot 0 should be generated.");
            seenSpriteNames.Add(sprite.name);
        }

        Assert.GreaterOrEqual(seenSpriteNames.Count, 4,
            "Boss sprites should be visually distinct across at least 4 of the 5 element slots.");
    }

    private void TestEnemySpritesAreGeneratedPerElement()
    {
        Debug.Log("Testing standard enemy sprites are generated per element...");

        CombatUnit.Element[] elements =
        {
            CombatUnit.Element.Fire,
            CombatUnit.Element.Water,
            CombatUnit.Element.Earth,
            CombatUnit.Element.Air,
            CombatUnit.Element.Space
        };

        HashSet<Sprite> generated = new HashSet<Sprite>();
        foreach (CombatUnit.Element element in elements)
        {
            Sprite sprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(element);
            Assert.IsNotNull(sprite, $"Standard enemy battle sprite for element '{element}' should be generated.");
            generated.Add(sprite);
        }

        Assert.GreaterOrEqual(generated.Count, elements.Length - 1,
            "Standard enemy battle sprites should be visually distinct across elements.");
    }

    private static CombatUnit.Element ResolveElementForIsland(string islandId)
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
        if (config == null)
        {
            return CombatUnit.Element.Earth;
        }

        Color primary = config.vicePrimaryColor;
        if (primary.r > 0.7f && primary.b < 0.4f && primary.g < 0.6f)
        {
            return CombatUnit.Element.Fire;
        }

        if (primary.b > 0.6f && primary.r < 0.6f)
        {
            return CombatUnit.Element.Water;
        }

        if (primary.g > 0.5f && primary.r > 0.5f)
        {
            return CombatUnit.Element.Earth;
        }

        if (primary.r > 0.7f && primary.g > 0.7f && primary.b > 0.7f)
        {
            return CombatUnit.Element.Air;
        }

        if (primary.b > 0.5f && primary.r > 0.4f && primary.g < 0.7f)
        {
            return CombatUnit.Element.Space;
        }

        return CombatUnit.Element.Earth;
    }
}
