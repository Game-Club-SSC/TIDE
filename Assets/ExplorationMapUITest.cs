using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class ExplorationMapUITest : MonoBehaviour
{
    [ContextMenu("Run All Exploration Map UI Tests")]
    public void RunAllTests()
    {
        TestWorldBoundsNotHardcodedMagicNumbers();
        Debug.Log("=== All Exploration Map UI Tests Passed ===");
    }

    [ContextMenu("Test World Bounds Not Hardcoded")]
    public void TestWorldBoundsNotHardcodedMagicNumbers()
    {
        Debug.Log("[ExplorationMapUITest] Testing world bounds are not hardcoded magic numbers...");
        GameObject mapObject = new GameObject("ExplorationMapUI_Test");
        ExplorationMapUI mapUI = mapObject.AddComponent<ExplorationMapUI>();
        try
        {
            Vector2 fallbackCenter = GetSerializedField<Vector2>(mapUI, "fallbackWorldCenter");
            Vector2 fallbackSize = GetSerializedField<Vector2>(mapUI, "fallbackWorldSize");

            Assert.IsTrue(fallbackSize.x > 0f, "Fallback world size X must be positive.");
            Assert.IsTrue(fallbackSize.y > 0f, "Fallback world size Y must be positive.");

            string boundsObjectName = GetSerializedField<string>(mapUI, "worldBoundsObjectName");
            Assert.IsFalse(string.IsNullOrEmpty(boundsObjectName),
                "worldBoundsObjectName should be set so bounds are resolved from scene, not hardcoded.");
            Assert.AreEqual("Ground", boundsObjectName, "worldBoundsObjectName should default to 'Ground'.");

            string source = ReadSourceFile("ExplorationMapUI.cs");
            Assert.IsTrue(source.Contains("ResolveWorldBounds"),
                "ExplorationMapUI should have a ResolveWorldBounds method for dynamic bounds.");
            Assert.IsTrue(source.Contains("worldRenderer.bounds") || source.Contains("Renderer"),
                "ResolveWorldBounds should read bounds from a Renderer component.");

            bool usesFallbackOnly = !source.Contains("worldRenderer.bounds") && !source.Contains("GetComponent<Renderer>()");
            Assert.IsFalse(usesFallbackOnly,
                "ExplorationMapUI should not rely solely on fallback values. It should read from scene objects.");

            Debug.Log("[ExplorationMapUITest] TestWorldBoundsNotHardcodedMagicNumbers passed.");
        }
        finally
        {
            DestroyImmediate(mapObject);
        }
    }

    private static T GetSerializedField<T>(object obj, string fieldName)
    {
        FieldInfo field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found.");
        return (T)field.GetValue(obj);
    }

    private static string ReadSourceFile(string fileName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(fileName.Replace(".cs", " t:MonoScript"));
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(fileName))
            {
                return System.IO.File.ReadAllText(path);
            }
        }
        return string.Empty;
    }
}
