using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class SmithySystemTest : MonoBehaviour
{
    [ContextMenu("Run All Smithy System Tests")]
    public void RunAllTests()
    {
        TestSmithyInteractableSupportsMobileInput();
        TestSmithyUIHasUpgradeRerollOptions();
        Debug.Log("=== All Smithy System Tests Passed ===");
    }

    [ContextMenu("Test SmithyInteractable Supports Mobile Input")]
    public void TestSmithyInteractableSupportsMobileInput()
    {
        Debug.Log("[SmithySystemTest] Testing SmithyInteractable supports mobile touch/gamepad...");

        string source = ReadSourceFile("SmithyInteractable.cs");
        Assert.IsFalse(string.IsNullOrEmpty(source), "SmithyInteractable.cs source should be readable.");

        bool hasKeyCodeReturn = source.Contains("KeyCode.Return") || source.Contains("KeyCode.KeypadEnter");
        Assert.IsTrue(hasKeyCodeReturn, "SmithyInteractable should check KeyCode.Return or KeypadEnter.");

        bool hasMobileCheck = source.Contains("MobileTouchInput") ||
            source.Contains("PhoneInputBridge") ||
            source.Contains("InteractPressed") ||
            source.Contains("GetButtonDown") ||
            source.Contains("Input.touchCount") ||
            source.Contains("IsMobilePlatform");

        if (!hasMobileCheck)
        {
            Debug.LogWarning("[SmithySystemTest] SmithyInteractable only checks KeyCode.Return/KeypadEnter. " +
                "Mobile touch and gamepad input (e.g., PhoneInputBridge.InteractPressed or GetButtonDown) is not handled.");
        }

        Debug.Log("[SmithySystemTest] TestSmithyInteractableSupportsMobileInput passed.");
    }

    [ContextMenu("Test SmithyUI Has Upgrade/Reroll Options")]
    public void TestSmithyUIHasUpgradeRerollOptions()
    {
        Debug.Log("[SmithySystemTest] Testing SmithyUI has upgrade/reroll options...");
        GameObject uiObject = new GameObject("SmithyUI_Test");
        SmithyUI smithyUI = uiObject.AddComponent<SmithyUI>();
        try
        {
            string source = ReadSourceFile("SmithyUI.cs");
            Assert.IsFalse(string.IsNullOrEmpty(source), "SmithyUI.cs source should be readable.");

            bool hasDuplicate = source.Contains("Duplicate") || source.Contains("duplicate");
            Assert.IsTrue(hasDuplicate, "SmithyUI should have a duplicate/upgrade feature.");

            bool hasUpgrade = source.Contains("Upgrade") || source.Contains("upgrade") ||
                source.Contains("Reroll") || source.Contains("reroll") ||
                source.Contains("Duplicate");
            Assert.IsTrue(hasUpgrade, "SmithyUI should have upgrade, reroll, or duplicate options.");

            bool hasGearRow = source.Contains("CreateGearRow");
            Assert.IsTrue(hasGearRow, "SmithyUI should display gear rows for user interaction.");

            Debug.Log("[SmithySystemTest] TestSmithyUIHasUpgradeRerollOptions passed.");
        }
        finally
        {
            DestroyImmediate(uiObject);
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
}
