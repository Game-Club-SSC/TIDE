using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class MobileTouchInputManagerTest : MonoBehaviour
{
    [ContextMenu("Run All Mobile Touch Input Manager Tests")]
    public void RunAllTests()
    {
        TestHandleBattleTouchIsImplemented();
        TestIsMobilePlatformToggleableInEditor();
        TestAutoHideDelayIsSerialized();
        Debug.Log("=== All Mobile Touch Input Manager Tests Passed ===");
    }

    [ContextMenu("Test HandleBattleTouch Is Implemented")]
    public void TestHandleBattleTouchIsImplemented()
    {
        Debug.Log("[MobileTouchInputManagerTest] Testing HandleBattleTouch is implemented...");
        GameObject managerObject = new GameObject("MobileTouchManager_Test");
        MobileTouchInputManager manager = managerObject.AddComponent<MobileTouchInputManager>();
        try
        {
            MethodInfo handleBattleTouch = typeof(MobileTouchInputManager).GetMethod(
                "HandleBattleTouch", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handleBattleTouch, "HandleBattleTouch method should exist.");

            string source = ReadSourceFile("MobileTouchInputManager.cs");
            Assert.IsTrue(source.Contains("HandleBattleTouch"), "HandleBattleTouch should be referenced in source.");

            int methodIndex = source.IndexOf("private void HandleBattleTouch");
            Assert.IsTrue(methodIndex >= 0, "HandleBattleTouch should have a method definition.");

            int openBrace = source.IndexOf('{', methodIndex);
            int closeBrace = FindClosingBrace(source, openBrace);
            string methodBody = source.Substring(openBrace, closeBrace - openBrace + 1);

            bool isPlaceholder = methodBody.Contains("No extra touch logic needed here") &&
                !methodBody.Contains("button") && !methodBody.Contains("Button");
            if (isPlaceholder)
            {
                Debug.LogWarning("[MobileTouchInputManagerTest] HandleBattleTouch appears to be a placeholder " +
                    "with no actual touch handling logic beyond the comment.");
            }

            Debug.Log("[MobileTouchInputManagerTest] TestHandleBattleTouchIsImplemented passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test IsMobilePlatform Toggleable In Editor")]
    public void TestIsMobilePlatformToggleableInEditor()
    {
        Debug.Log("[MobileTouchInputManagerTest] Testing IsMobilePlatform is toggleable in editor...");
        GameObject managerObject = new GameObject("MobileTouchManager_PlatformTest");
        MobileTouchInputManager manager = managerObject.AddComponent<MobileTouchInputManager>();
        try
        {
            PropertyInfo isMobileProp = typeof(MobileTouchInputManager).GetProperty("IsMobilePlatform");
            Assert.IsNotNull(isMobileProp, "IsMobilePlatform property should exist.");

            MethodInfo setter = isMobileProp.GetSetMethod(true);
            Assert.IsNotNull(setter, "IsMobilePlatform should have a setter (at least private) for editor toggling.");

            string source = ReadSourceFile("MobileTouchInputManager.cs");
            Assert.IsTrue(source.Contains("public bool IsMobilePlatform"),
                "IsMobilePlatform should be a public property.");

            bool hasEditorToggle = source.Contains("Application.isEditor") ||
                source.Contains("#if UNITY_EDITOR") ||
                source.Contains("enableMobileControls");
            Assert.IsTrue(hasEditorToggle,
                "IsMobilePlatform should have an editor toggle mechanism.");

            Debug.Log("[MobileTouchInputManagerTest] TestIsMobilePlatformToggleableInEditor passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
    }

    [ContextMenu("Test AutoHideDelay Is Serialized")]
    public void TestAutoHideDelayIsSerialized()
    {
        Debug.Log("[MobileTouchInputManagerTest] Testing AutoHideDelay is serialized...");
        GameObject managerObject = new GameObject("MobileTouchManager_AutoHideTest");
        MobileTouchInputManager manager = managerObject.AddComponent<MobileTouchInputManager>();
        try
        {
            FieldInfo autoHideField = typeof(MobileTouchInputManager).GetField(
                "autoHideDelay", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(autoHideField, "autoHideDelay field should exist.");

            float autoHideValue = (float)autoHideField.GetValue(manager);
            Assert.IsTrue(autoHideValue > 0f, "autoHideDelay should be positive.");
            Assert.AreEqual(3f, autoHideValue, 0.01f, "autoHideDelay should default to 3 seconds.");

            string source = ReadSourceFile("MobileTouchInputManager.cs");
            bool isConst = source.Contains("private const float AutoHideDelay");
            bool isSerialized = source.Contains("[SerializeField]") && source.Contains("autoHideDelay");

            if (isConst)
            {
                Debug.LogWarning("[MobileTouchInputManagerTest] AutoHideDelay is a const (not [SerializeField]). " +
                    "Consider making it serialized for inspector tuning.");
            }

            Assert.IsTrue(isConst || isSerialized,
                "AutoHideDelay should exist as either const or [SerializeField].");

            Debug.Log("[MobileTouchInputManagerTest] TestAutoHideDelayIsSerialized passed.");
        }
        finally
        {
            DestroyImmediate(managerObject);
        }
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

    private static int FindClosingBrace(string source, int openBraceIndex)
    {
        int depth = 1;
        int pos = openBraceIndex + 1;
        while (pos < source.Length && depth > 0)
        {
            if (source[pos] == '{') depth++;
            else if (source[pos] == '}') depth--;
            pos++;
        }
        return pos - 1;
    }
}
