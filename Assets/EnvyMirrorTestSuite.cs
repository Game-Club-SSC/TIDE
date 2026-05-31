using UnityEngine;
using NUnit.Framework;

[DisallowMultipleComponent]
public class EnvyMirrorTestSuite : MonoBehaviour
{
    [ContextMenu("Run Envy Mirror Tests")]
    public void RunTests()
    {
        Debug.Log("[EnvyMirrorTestSuite] Starting Envy Mirror tests...");

        TestMirrorCopiesElement();
        TestCovetWithNullSkillDoesNotThrow();

        Debug.Log("[EnvyMirrorTestSuite] All Envy Mirror tests passed.");
    }

    private void TestMirrorCopiesElement()
    {
        GameObject bmObj = new GameObject("TestBM_Envy");
        GameObject allyObj = new GameObject("TestAlly_Envy");
        GameObject enemyObj = new GameObject("TestEnemy_Envy");

        BattleManager bm = bmObj.AddComponent<BattleManager>();
        CombatUnit ally = allyObj.AddComponent<CombatUnit>();
        CombatUnit enemy = enemyObj.AddComponent<CombatUnit>();

        ally.UnitName = "Ally";
        ally.ElementType = CombatUnit.Element.Fire;
        enemy.UnitName = "Enemy";
        enemy.Type = CombatUnit.UnitType.Enemy;
        enemy.ElementType = CombatUnit.Element.Water;

        bm.RegisterUnit(ally);
        bm.RegisterUnit(enemy);
        bm.EnableEnvyMirror = true;

        var field = typeof(BattleManager).GetField("lastAttacker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(bm, ally);

        var method = typeof(BattleManager).GetMethod("ComputeEnemyAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(bm, new object[] { enemy });

        Assert.AreEqual(CombatUnit.Element.Fire, enemy.ElementType, "Enemy should mirror ally's Fire element.");

        DestroyImmediate(bmObj);
        DestroyImmediate(allyObj);
        DestroyImmediate(enemyObj);
        Debug.Log("[EnvyMirrorTestSuite] TestMirrorCopiesElement PASSED.");
    }

    private void TestCovetWithNullSkillDoesNotThrow()
    {
        GameObject bmObj = new GameObject("TestBM_Covet");
        GameObject allyObj = new GameObject("TestAlly_Covet");
        GameObject enemyObj = new GameObject("TestEnemy_Covet");

        BattleManager bm = bmObj.AddComponent<BattleManager>();
        CombatUnit ally = allyObj.AddComponent<CombatUnit>();
        CombatUnit enemy = enemyObj.AddComponent<CombatUnit>();

        ally.UnitName = "Ally";
        enemy.UnitName = "Enemy";
        enemy.Type = CombatUnit.UnitType.Enemy;

        bm.RegisterUnit(ally);
        bm.RegisterUnit(enemy);
        bm.EnableEnvyMirror = true;

        var attackerField = typeof(BattleManager).GetField("lastAttacker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        attackerField.SetValue(bm, ally);

        var skillField = typeof(BattleManager).GetField("lastPlayerSkill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        skillField.SetValue(bm, null);

        var bossField = typeof(BattleManager).GetField("isBossEncounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bossField.SetValue(bm, true);

        var method = typeof(BattleManager).GetMethod("ComputeEnemyAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(bm, new object[] { enemy });

        DestroyImmediate(bmObj);
        DestroyImmediate(allyObj);
        DestroyImmediate(enemyObj);
        Debug.Log("[EnvyMirrorTestSuite] TestCovetWithNullSkillDoesNotThrow PASSED.");
    }
}
