using UnityEngine;
using NUnit.Framework;

[DisallowMultipleComponent]
public class DesireStatusEffectTestSuite : MonoBehaviour
{
    [ContextMenu("Run Desire Status Effect Tests")]
    public void RunTests()
    {
        Debug.Log("[DesireStatusEffectTestSuite] Starting Desire status effect tests...");

        TestSlowReducesEffectiveSpeed();
        TestDrowsyWithFullMagnitudeSkipsTurn();
        TestNoEffectMeansBaseSpeed();

        Debug.Log("[DesireStatusEffectTestSuite] All Desire status effect tests passed.");
    }

    private void TestSlowReducesEffectiveSpeed()
    {
        GameObject go = new GameObject("TestUnit_Slow");
        CombatUnit unit = go.AddComponent<CombatUnit>();
        unit.DebugSpeed = 20;
        unit.DebugHP = 100;
        unit.DebugMaxHP = 100;

        int baseSpeed = unit.GetEffectiveSpeed();
        Assert.AreEqual(20, baseSpeed, "Base effective speed should equal Speed when no Slow applied.");

        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 3, 0.5f, "TestSource"));
        int slowedSpeed = unit.GetEffectiveSpeed();
        Assert.AreEqual(10, slowedSpeed, "Effective speed should be halved with 0.5 Slow magnitude.");
        Assert.GreaterOrEqual(slowedSpeed, 1, "Effective speed must be at least 1.");

        DestroyImmediate(go);
        Debug.Log("[DesireStatusEffectTestSuite] TestSlowReducesEffectiveSpeed PASSED.");
    }

    private void TestDrowsyWithFullMagnitudeSkipsTurn()
    {
        GameObject go = new GameObject("TestUnit_Drowsy");
        CombatUnit unit = go.AddComponent<CombatUnit>();
        unit.DebugHP = 100;
        unit.DebugMaxHP = 100;

        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Drowsy, 3, 1f, "TestSource"));
        bool skipped = unit.ShouldSkipTurn();
        Assert.IsTrue(skipped, "Drowsy with magnitude 1.0 should always skip turn.");

        DestroyImmediate(go);
        Debug.Log("[DesireStatusEffectTestSuite] TestDrowsyWithFullMagnitudeSkipsTurn PASSED.");
    }

    private void TestNoEffectMeansBaseSpeed()
    {
        GameObject go = new GameObject("TestUnit_NoEffect");
        CombatUnit unit = go.AddComponent<CombatUnit>();
        unit.DebugSpeed = 15;
        unit.DebugHP = 100;
        unit.DebugMaxHP = 100;

        Assert.AreEqual(15, unit.GetEffectiveSpeed(), "With no effects, GetEffectiveSpeed should return base Speed.");
        Assert.IsFalse(unit.ShouldSkipTurn(), "With no Drowsy effect, ShouldSkipTurn should return false.");

        DestroyImmediate(go);
        Debug.Log("[DesireStatusEffectTestSuite] TestNoEffectMeansBaseSpeed PASSED.");
    }
}
