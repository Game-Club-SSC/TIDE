#if UNITY_EDITOR
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
        TestFateCombatUsesCombatScenePayload();
        TestFinaleDirectorsPersistUntilTheEndingCompletes();
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

            Assert.IsTrue(source.Contains("bm.ConfigureEnvyContext(false, true)"),
                "Fate must not enable the Envy-only battle rule.");

            Debug.Log("[FateEncounterDirectorTest] TestConfigureEnvyContextNotCalledRedundantly passed.");
        }
        finally
        {
            DestroyImmediate(directorObject);
        }
    }

    [ContextMenu("Test Fate Combat Uses CombatScene Payload")]
    public void TestFateCombatUsesCombatScenePayload()
    {
        GameObject directorObject = new GameObject("FateDirector_CombatSceneTest");
        FateEncounterDirector director = directorObject.AddComponent<FateEncounterDirector>();
        try
        {
            EnemyComposition composition = director.BuildFateCombatEnemyComposition();
            Assert.IsNotNull(composition, "Fate combat needs a pending enemy composition.");
            Assert.AreEqual(1, composition.Count, "Fate combat should create exactly one enemy.");
            Assert.AreEqual("Fate, The Inevitable", composition.names[0], "Fate must keep its combat name.");
            Assert.AreEqual(9999 - 100, composition.maxHpModifiers[0],
                "Fate payload must raise the standard CombatScene unit to its configured HP.");

            string source = ReadSourceFile("FateEncounterDirector.cs");
            Assert.IsTrue(source.Contains("EnterFateCombatScene(fateComposition)"),
                "Defiance must enter Fate through GameStateManager's CombatScene route.");
            Assert.IsFalse(source.Contains("bm.StartBattle();"),
                "Fate must not start a battle in the exploration scene.");
        }
        finally
        {
            DestroyImmediate(directorObject);
        }
    }

    [ContextMenu("Test Finale Directors Persist During Scene Change")]
    public void TestFinaleDirectorsPersistUntilTheEndingCompletes()
    {
        string fateSource = ReadSourceFile("FateEncounterDirector.cs");
        string endingSource = ReadSourceFile("EndingSequenceDirector.cs");

        Assert.IsTrue(fateSource.Contains("DontDestroyOnLoad(gameObject)"),
            "Fate dialogue must survive the CombatScene transition.");
        Assert.IsTrue(endingSource.Contains("DontDestroyOnLoad(gameObject)"),
            "Ending playback must survive the return from CombatScene.");
        Assert.IsTrue(endingSource.Contains("Destroy(gameObject);"),
            "The ending director must clean itself up before a later new game.");
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
#endif
