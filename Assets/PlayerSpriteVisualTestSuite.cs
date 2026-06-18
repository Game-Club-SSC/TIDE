using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteVisualTestSuite : MonoBehaviour
{
    [ContextMenu("Run Player Sprite Visual Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Player Sprite Visual Tests ===");

        TestSpriteModeBuildsSpriteAndShadow();
        TestSpriteModePreservesMovementCapabilities();
        TestLegacy3DModeStillWorks();
        TestCanSwitchBetweenSpriteAnd3D();
        TestStyleSwapRebuildsSprite();
        TestDefaultStyleUsesPartyElement();

        Debug.Log("=== All Player Sprite Visual Tests Passed ===");
    }

    private IsometricPlayer CreatePlayer(string name)
    {
        GameObject playerObject = new GameObject(name);
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        return playerObject.AddComponent<IsometricPlayer>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist.");
        field.SetValue(target, value);
    }

    private static object InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist.");
        return method.Invoke(target, args);
    }

    private void TestSpriteModeBuildsSpriteAndShadow()
    {
        IsometricPlayer player = CreatePlayer("SpriteTest_Build");
        SetPrivateField(player, "use2DSpriteVisual", true);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");

        Transform spriteRoot = player.transform.Find(ElementalCharacterFactory.PlayerSpriteRootName);
        Assert.IsNotNull(spriteRoot, "Sprite root should exist in sprite mode.");
        SpriteRenderer[] renderers = spriteRoot.GetComponentsInChildren<SpriteRenderer>(true);
        Assert.IsTrue(renderers.Length > 0, "At least one SpriteRenderer should be present.");
        Assert.IsNotNull(renderers[0].sprite, "SpriteRenderer should have a sprite assigned.");
        BillboardSprite billboard = renderers[0].GetComponent<BillboardSprite>();
        Assert.IsNotNull(billboard, "SpriteRenderer should have a BillboardSprite component.");
        Assert.IsTrue(billboard.FaceCamera, "BillboardSprite should face the camera.");

        Transform shadow = player.transform.Find(ElementalCharacterFactory.ShadowQuadName);
        Assert.IsNotNull(shadow, "Shadow quad should be present in sprite mode.");

        Object.DestroyImmediate(player.gameObject);
    }

    private void TestSpriteModePreservesMovementCapabilities()
    {
        IsometricPlayer player = CreatePlayer("SpriteTest_Movement");
        SetPrivateField(player, "use2DSpriteVisual", true);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");

        SetPrivateField(player, "canMove", true);
        SetPrivateField(player, "inputVector", new Vector3(0f, 0f, 1f));
        SetPrivateField(player, "dashSpeed", 14f);
        SetPrivateField(player, "dashDuration", 0.2f);
        SetPrivateField(player, "dashCooldown", 0.8f);
        SetPrivateField(player, "nextDashAllowedAt", 0f);

        InvokePrivateMethod(player, "StartDash");
        Assert.IsTrue(player.DebugIsDashing, "Sprite-mode player should still be able to dash.");

        Object.DestroyImmediate(player.gameObject);
    }

    private void TestLegacy3DModeStillWorks()
    {
        IsometricPlayer player = CreatePlayer("3DTest_Build");
        SetPrivateField(player, "use2DSpriteVisual", false);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");

        Transform modelRoot = player.transform.Find(ElementalCharacterFactory.PlayerModelRootName);
        Assert.IsNotNull(modelRoot, "3D model root should exist when sprite mode is disabled.");

        Object.DestroyImmediate(player.gameObject);
    }

    private void TestCanSwitchBetweenSpriteAnd3D()
    {
        IsometricPlayer player = CreatePlayer("SwitchTest");
        SetPrivateField(player, "use2DSpriteVisual", true);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");
        Assert.IsNotNull(player.transform.Find(ElementalCharacterFactory.PlayerSpriteRootName), "Sprite root should exist after switching to sprite mode.");

        SetPrivateField(player, "use2DSpriteVisual", false);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");
        Assert.IsNull(player.transform.Find(ElementalCharacterFactory.PlayerSpriteRootName), "Sprite root should be removed after switching to 3D mode.");
        Assert.IsNotNull(player.transform.Find(ElementalCharacterFactory.PlayerModelRootName), "3D model root should exist after switching to 3D mode.");

        SetPrivateField(player, "use2DSpriteVisual", true);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");
        Assert.IsNotNull(player.transform.Find(ElementalCharacterFactory.PlayerSpriteRootName), "Sprite root should be recreated after switching back to sprite mode.");
        Assert.IsNull(player.transform.Find(ElementalCharacterFactory.PlayerModelRootName), "3D model root should be removed after switching back to sprite mode.");

        Object.DestroyImmediate(player.gameObject);
    }

    private void TestStyleSwapRebuildsSprite()
    {
        IsometricPlayer player = CreatePlayer("StyleSwapTest");
        SetPrivateField(player, "use2DSpriteVisual", true);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");

        player.SetPlayerVisualStyle("style_fire_vanguard");
        Transform spriteRoot = player.transform.Find(ElementalCharacterFactory.PlayerSpriteRootName);
        Assert.IsNotNull(spriteRoot, "Sprite root should exist after style swap.");
        SpriteRenderer[] renderers = spriteRoot.GetComponentsInChildren<SpriteRenderer>(true);
        Assert.IsTrue(renderers.Length > 0, "SpriteRenderer should exist after style swap.");
        Assert.IsNotNull(renderers[0].sprite, "Sprite should be assigned after style swap.");

        Object.DestroyImmediate(player.gameObject);
    }

    private void TestDefaultStyleUsesPartyElement()
    {
        IsometricPlayer player = CreatePlayer("PartyElementTest");
        SetPrivateField(player, "use2DSpriteVisual", true);
        SetPrivateField(player, "currentStyleId", string.Empty);
        InvokePrivateMethod(player, "ApplyCurrentStyleVisual");

        string styleId = player.CurrentStyleId;
        Assert.IsFalse(string.IsNullOrEmpty(styleId), "Player should pick a default style id.");
        Assert.IsTrue(FuturisticSpriteLibrary.TryGetPlayerStyle(styleId, out _), "Style id should be a real definition.");

        Object.DestroyImmediate(player.gameObject);
    }
}
