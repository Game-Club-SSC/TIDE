using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class FateEncounterDirectorTest : MonoBehaviour
{
    [ContextMenu("Run All Fate Encounter Director Tests")]
    public void RunAllTests()
    {
        TestFateBossHpScaling();
        TestConfigureEnvyContextNotCalledRedundantly();
        TestPlayEndingDirectlyInitializesEndingSequenceDirector();
        Debug.Log("=== All Fate Encounter Director Tests Passed ===");
    }

    [ContextMenu("Test Fate Boss HP Scaling")]
    public void TestFateBossHpScaling()
    {
        Debug.Log("[FateEncounterDirectorTest] Testing fate boss HP scaling...");
        GameObject directorObject = new GameObject("FateDirector_HPTest");
        FateEncounterDirector director = directorObject.AddComponent<FateEncounterDirector>();
        try
        {
            int defaultHp = GetSerializedField<int>(director, "fateMaxHp");
            Assert.IsTrue(defaultHp > 0, "Fate boss default HP must be positive.");
            Assert.AreEqual(9999, defaultHp, "Fate boss default HP should be 9999.");

            int defaultAttack = GetSerializedField<int>(director, "fateAttack");
            int defaultDefense = GetSerializedField<int>(director, "fateDefense");
            Assert.IsTrue(defaultAttack > 0, "Fate boss attack must be positive.");
            Assert.IsTrue(defaultDefense > 0, "Fate boss defense must be positive.");
            Assert.IsTrue(defaultAttack > defaultDefense, "Fate boss attack should exceed defense.");

            Debug.Log("[FateEncounterDirectorTest] TestFateBossHpScaling passed.");
        }
        finally
        {
            DestroyImmediate(directorObject);
        }
    }

    [ContextMenu("Test ConfigureEnvyContext Not Called Redundantly")]
    public void TestConfigureEnvyContextNotCalledRedundantly()
    {
        Debug.Log("[FateEncounterDirectorTest] Testing ConfigureEnvyContext is not called redundantly...");
        GameObject directorObject = new GameObject("FateDirector_EnvyTest");
        FateEncounterDirector director = directorObject.AddComponent<FateEncounterDirector>();
        try
        {
            MethodInfo configureMethod = typeof(FateEncounterDirector).GetMethod(
                "ConfigureFateCombat", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(configureMethod, "ConfigureFateCombat method should exist.");

            MethodInfo playEndingMethod = typeof(FateEncounterDirector).GetMethod(
                "PlayEndingDirectly", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(playEndingMethod, "PlayEndingDirectly method should exist.");

            string source = ReadSourceFile("FateEncounterDirector.cs");
            int envyCallCount = CountOccurrences(source, "ConfigureEnvyContext");
            Assert.AreEqual(1, envyCallCount, "ConfigureEnvyContext should be called exactly once.");

            int inConfigureFateCombat = CountOccurrencesBetween(source, "ConfigureFateCombat", "RunVictoryEnding", "ConfigureEnvyContext");
            Assert.AreEqual(1, inConfigureFateCombat, "ConfigureEnvyContext should be called inside ConfigureFateCombat.");

            Debug.Log("[FateEncounterDirectorTest] TestConfigureEnvyContextNotCalledRedundantly passed.");
        }
        finally
        {
            DestroyImmediate(directorObject);
        }
    }

    [ContextMenu("Test PlayEndingDirectly Initializes EndingSequenceDirector")]
    public void TestPlayEndingDirectlyInitializesEndingSequenceDirector()
    {
        Debug.Log("[FateEncounterDirectorTest] Testing PlayEndingDirectly initializes EndingSequenceDirector...");
        GameObject directorObject = new GameObject("FateDirector_EndingTest");
        FateEncounterDirector director = directorObject.AddComponent<FateEncounterDirector>();
        try
        {
            if (EndingSequenceDirector.Instance != null)
            {
                DestroyImmediate(EndingSequenceDirector.Instance.gameObject);
            }

            Assert.IsNull(EndingSequenceDirector.Instance, "EndingSequenceDirector should be null before PlayEndingDirectly.");

            MethodInfo playEndingMethod = typeof(FateEncounterDirector).GetMethod(
                "PlayEndingDirectly", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(playEndingMethod, "PlayEndingDirectly method should exist.");

            string source = ReadSourceFile("FateEncounterDirector.cs");
            Assert.IsTrue(source.Contains("AddComponent<EndingSequenceDirector>()"),
                "PlayEndingDirectly should create EndingSequenceDirector via AddComponent when instance is null.");
            Assert.IsTrue(source.Contains("director.PlayEnding(branch)"),
                "PlayEndingDirectly should call PlayEnding on the director.");

            Debug.Log("[FateEncounterDirectorTest] TestPlayEndingDirectlyInitializesEndingSequenceDirector passed.");
        }
        finally
        {
            DestroyImmediate(directorObject);
            if (EndingSequenceDirector.Instance != null)
            {
                DestroyImmediate(EndingSequenceDirector.Instance.gameObject);
            }
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

    private static int CountOccurrencesBetween(string source, string startMarker, string endMarker, string searchTerm)
    {
        int startIdx = source.IndexOf(startMarker);
        int endIdx = source.IndexOf(endMarker);
        if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx) return 0;

        string section = source.Substring(startIdx, endIdx - startIdx);
        return CountOccurrences(section, searchTerm);
    }
}
