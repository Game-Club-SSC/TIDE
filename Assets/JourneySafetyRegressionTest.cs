#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class JourneySafetyRegressionTest : MonoBehaviour
{
    [ContextMenu("Run Journey Safety Regression Tests")]
    public void RunTests()
    {
        TestSavedGridDimensionsAreBounded();
        TestSaveParserRejectsFutureAndMalformedPayloads();
        TestSnapshotMigrationDoesNotDuplicateExplicitBossContribution();
        Debug.Log("[JourneySafetyRegressionTest] All journey safety checks passed.");
    }

    private static void TestSavedGridDimensionsAreBounded()
    {
        MethodInfo validate = typeof(GameStateManager).GetMethod(
            "AreSavedPuzzleDimensionsValid", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(validate);
        Assert.IsTrue((bool)validate.Invoke(null, new object[] { 4, 4 }));
        Assert.IsTrue((bool)validate.Invoke(null, new object[] { 256, 256 }));
        Assert.IsFalse((bool)validate.Invoke(null, new object[] { int.MaxValue, int.MaxValue }),
            "A corrupt saved grid must never overflow its dimensions or allocate an unbounded array.");
        Assert.IsFalse((bool)validate.Invoke(null, new object[] { -1, 4 }));
        Assert.IsFalse((bool)validate.Invoke(null, new object[] { 0, 4 }));
        Assert.IsFalse((bool)validate.Invoke(null, new object[] { 4, 257 }));
    }

    private static void TestSaveParserRejectsFutureAndMalformedPayloads()
    {
        MethodInfo parse = typeof(GameStateManager).GetMethod(
            "TryParseWorldStatePayload", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(parse);
        object[] legacyArguments = { "{\"puzzleStates\":[]}", null };
        Assert.IsTrue((bool)parse.Invoke(null, legacyArguments));
        GameStateManager.WorldStateSaveData legacy = (GameStateManager.WorldStateSaveData)legacyArguments[1];
        Assert.AreEqual(0, legacy.saveSchemaVersion,
            "Absent legacy schema metadata must remain distinguishable from a modern empty flags snapshot.");
        Assert.IsFalse((bool)parse.Invoke(null, new object[]
            { "{\"saveSchemaVersion\":2,\"puzzleStates\":[]}", null }));
        Assert.IsFalse((bool)parse.Invoke(null, new object[]
            { "{\"puzzleStates\":[],}", null }));
        Assert.IsFalse((bool)parse.Invoke(null, new object[]
            { "{\"puzzleStates\":[],\"puzzleStates\":[]}", null }));
    }

    private static void TestSnapshotMigrationDoesNotDuplicateExplicitBossContribution()
    {
        IslandRestorationState state = new IslandRestorationState("island_lust");
        state.ApplySnapshot(new IslandRestorationStateSnapshot
        {
            combatContribution = 0.8f,
            bossContribution = 0.1f,
            puzzleContribution = 0.25f
        });
        Assert.AreEqual(0.5f, state.CombatContribution, 0.0001f);
        Assert.AreEqual(0.1f, state.BossContribution, 0.0001f,
            "A snapshot with an explicit boss bucket must not also migrate its ordinary combat overflow into that bucket.");
        Assert.AreEqual(0.85f, state.TotalContribution, 0.0001f);
    }
}
#endif
