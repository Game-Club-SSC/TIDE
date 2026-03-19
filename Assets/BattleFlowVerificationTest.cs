using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleFlowVerificationTest : MonoBehaviour
{
    [ContextMenu("Run Battle Flow Verification")]
    public void RunVerification()
    {
        GameObject managerObject = new GameObject("BattleManager_Verification");
        BattleManager manager = managerObject.AddComponent<BattleManager>();

        GameObject unitsRoot = new GameObject("VerificationUnits");

        try
        {
            CombatUnit allyFast = CreateUnit(unitsRoot.transform, "AllyFast", CombatUnit.UnitType.Ally, speed: 14, attack: 12, defense: 3, hp: 60);
            CombatUnit allySlow = CreateUnit(unitsRoot.transform, "AllySlow", CombatUnit.UnitType.Ally, speed: 9, attack: 22, defense: 2, hp: 55);
            CombatUnit enemyMid = CreateUnit(unitsRoot.transform, "EnemyMid", CombatUnit.UnitType.Enemy, speed: 12, attack: 10, defense: 0, hp: 50);
            CombatUnit enemySlow = CreateUnit(unitsRoot.transform, "EnemySlow", CombatUnit.UnitType.Enemy, speed: 5, attack: 8, defense: 5, hp: 30);

            manager.RegisterUnit(allyFast);
            manager.RegisterUnit(allySlow);
            manager.RegisterUnit(enemyMid);
            manager.RegisterUnit(enemySlow);

            InvokePrivate(manager, "BuildTurnQueueFromLivingUnits");

            Assert.AreEqual(4, manager.TurnQueue.Count, "Turn queue should include all living units.");
            Assert.AreEqual("AllyFast", manager.TurnQueue[0].UnitName, "Highest speed unit should act first.");
            Assert.AreEqual("EnemyMid", manager.TurnQueue[1].UnitName, "Second highest speed unit should act second.");
            Assert.AreEqual("AllySlow", manager.TurnQueue[2].UnitName, "Third speed unit should act third.");
            Assert.AreEqual("EnemySlow", manager.TurnQueue[3].UnitName, "Lowest speed unit should act last.");

            allyFast.DebugHP = 0;
            allyFast.DebugIsAlive = false;

            CombatUnit nextActor = (CombatUnit)InvokePrivate(manager, "TryGetNextActingUnit", new object[] { null });
            Assert.IsNotNull(nextActor, "Queue should produce a valid actor after skipping dead units.");
            Assert.AreEqual("EnemyMid", nextActor.UnitName, "Dead queued units should be skipped before their turn.");

            int enemySlowBefore = enemySlow.HP;
            InvokePrivate(manager, "ResolveAttack", allySlow, enemySlow);
            int expectedEnemySlowHp = enemySlowBefore - Mathf.Max(1, allySlow.Attack - enemySlow.Defense);
            Assert.AreEqual(expectedEnemySlowHp, enemySlow.HP, "Basic attack should apply expected damage after defense.");

            enemySlow.DebugHP = 0;
            enemySlow.DebugIsAlive = false;

            int enemyMidBefore = enemyMid.HP;
            InvokePrivate(manager, "ResolveAttack", allySlow, enemySlow);
            Assert.Less(enemyMid.HP, enemyMidBefore, "Dead targets should not be hit; attack should retarget a living opponent.");

            Debug.Log("=== Battle flow verification passed ===");
        }
        finally
        {
            DestroyImmediate(unitsRoot);
            DestroyImmediate(managerObject);
        }
    }

    private static CombatUnit CreateUnit(Transform parent, string name, CombatUnit.UnitType type, int speed, int attack, int defense, int hp)
    {
        GameObject unitObject = new GameObject(name);
        unitObject.transform.SetParent(parent, false);

        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        unit.UnitName = name;
        unit.Type = type;
        unit.Speed = speed;
        unit.Attack = attack;
        unit.Defense = defense;
        unit.MaxHP = Mathf.Max(1, hp);
        unit.HP = Mathf.Clamp(hp, 0, unit.MaxHP);
        unit.DebugIsAlive = unit.HP > 0;
        return unit;
    }

    private static object InvokePrivate(BattleManager manager, string methodName, params object[] args)
    {
        MethodInfo method = typeof(BattleManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");

        if (methodName == "TryGetNextActingUnit")
        {
            object[] invokeArgs = new object[] { null };
            bool result = (bool)method.Invoke(manager, invokeArgs);
            return result ? (CombatUnit)invokeArgs[0] : null;
        }

        return method.Invoke(manager, args);
    }
}
