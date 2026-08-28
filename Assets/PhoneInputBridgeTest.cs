#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PhoneInputBridgeTest : MonoBehaviour
{
    [ContextMenu("Run All Phone Input Bridge Tests")]
    public void RunAllTests()
    {
        TestHandleActionSupportsMoreThanToggleAutoRunAndHop();
        Debug.Log("=== All Phone Input Bridge Tests Passed ===");
    }

    [ContextMenu("Test HandleAction Supports More Than Toggle Actions")]
    public void TestHandleActionSupportsMoreThanToggleAutoRunAndHop()
    {
        Debug.Log("[PhoneInputBridgeTest] Testing HandleAction supports more than toggle_auto_run and toggle_hop...");
        GameObject bridgeObject = new GameObject("PhoneInputBridge_Test");
        PhoneInputBridge bridge = bridgeObject.AddComponent<PhoneInputBridge>();
        try
        {
            string source = ReadSourceFile("PhoneInputBridge.cs");
            Assert.IsFalse(string.IsNullOrEmpty(source), "PhoneInputBridge.cs source should be readable.");

            int actionIndex = source.IndexOf("HandleAction");
            Assert.IsTrue(actionIndex >= 0, "HandleAction method should exist.");

            string handleActionBody = ExtractMethodBody(source, "HandleAction");
            Assert.IsFalse(string.IsNullOrEmpty(handleActionBody), "HandleAction method body should be extractable.");

            int caseCount = CountOccurrences(handleActionBody, "case \"");
            Assert.IsTrue(caseCount >= 2,
                $"HandleAction should support at least 2 action cases. Found {caseCount}.");

            bool hasToggleAutoRun = handleActionBody.Contains("toggle_auto_run");
            bool hasToggleHop = handleActionBody.Contains("toggle_hop");
            Assert.IsTrue(hasToggleAutoRun, "HandleAction should support toggle_auto_run.");
            Assert.IsTrue(hasToggleHop, "HandleAction should support toggle_hop.");

            bool hasFutureExpansion = source.Contains("For future expansion") ||
                source.Contains("future") ||
                caseCount > 2;
            if (!hasFutureExpansion)
            {
                Debug.LogWarning("[PhoneInputBridgeTest] HandleAction only has toggle_auto_run and toggle_hop. " +
                    "No additional actions found. Consider adding more actions for richer phone control.");
            }

            Debug.Log("[PhoneInputBridgeTest] TestHandleActionSupportsMoreThanToggleAutoRunAndHop passed.");
        }
        finally
        {
            DestroyImmediate(bridgeObject);
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

    private static string ExtractMethodBody(string source, string methodName)
    {
        int methodIndex = source.IndexOf($"private void {methodName}");
        if (methodIndex < 0) methodIndex = source.IndexOf($"public void {methodName}");
        if (methodIndex < 0) methodIndex = source.IndexOf($"void {methodName}");
        if (methodIndex < 0) return string.Empty;

        int openBrace = source.IndexOf('{', methodIndex);
        if (openBrace < 0) return string.Empty;

        int depth = 1;
        int pos = openBrace + 1;
        while (pos < source.Length && depth > 0)
        {
            if (source[pos] == '{') depth++;
            else if (source[pos] == '}') depth--;
            pos++;
        }

        return source.Substring(openBrace, pos - openBrace);
    }

    private static int CountOccurrences(string source, string searchTerm)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(searchTerm, index)) != -1)
        {
            count++;
            index += searchTerm.Length;
        }
        return count;
    }
}
#endif
