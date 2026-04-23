using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

public class FuturisticSpriteLibraryTestSuite
{
    [SetUp]
    public void Setup()
    {
        // Clear the cache before each test to ensure fresh state
        FieldInfo cacheField = typeof(FuturisticSpriteLibrary).GetField("spriteCache", BindingFlags.Static | BindingFlags.NonPublic);
        var cache = cacheField.GetValue(null) as Dictionary<string, Sprite>;
        cache?.Clear();
    }

    [Test]
    public void GetEnemyBattleSprite_ReturnsSpriteWithCorrectName()
    {
        CombatUnit.Element element = CombatUnit.Element.Fire;
        string expectedName = $"enemy_battle_{(int)element}";

        Sprite sprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(element);

        Assert.IsNotNull(sprite, "GetEnemyBattleSprite should return a Sprite.");
        Assert.AreEqual(expectedName, sprite.name, "Returned sprite should have the correct name.");
    }

    [Test]
    public void GetEnemyBattleSprite_UsesCacheOnSubsequentCalls()
    {
        CombatUnit.Element element = CombatUnit.Element.Water;

        Sprite firstCall = FuturisticSpriteLibrary.GetEnemyBattleSprite(element);
        Sprite secondCall = FuturisticSpriteLibrary.GetEnemyBattleSprite(element);

        Assert.IsNotNull(firstCall);
        Assert.AreSame(firstCall, secondCall, "Second call should return the exact same cached Sprite instance.");
    }

    [Test]
    public void GetEnemyBattleSprite_ReturnsDifferentSpritesForDifferentElements()
    {
        Sprite fireSprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(CombatUnit.Element.Fire);
        Sprite earthSprite = FuturisticSpriteLibrary.GetEnemyBattleSprite(CombatUnit.Element.Earth);

        Assert.IsNotNull(fireSprite);
        Assert.IsNotNull(earthSprite);
        Assert.AreNotSame(fireSprite, earthSprite, "Different elements should generate different Sprites.");
        Assert.AreNotEqual(fireSprite.name, earthSprite.name, "Different elements should have different sprite names.");
    }
}
