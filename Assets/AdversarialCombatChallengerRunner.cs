#if UNITY_EDITOR || TIDE_COMBAT_HARNESS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class AdversarialCombatChallengerRunner : MonoBehaviour
{
    [ContextMenu("Run Adversarial Combat Tests")]
    public void RunContextMenuTests()
    {
        RunAllEmpiricalTests();
    }

    public static void RunAllEmpiricalTests()
    {
        Debug.Log("[CHALLENGER] ================= Starting Adversarial Empirical Tests =================");
        int passed = 0;
        int failed = 0;

        RunTest(TestLethalClashKillsAllEnemiesTriggersVictoryImmediately, nameof(TestLethalClashKillsAllEnemiesTriggersVictoryImmediately), ref passed, ref failed);
        RunTest(TestLethalClashKillsAllAlliesTriggersDefeatImmediately, nameof(TestLethalClashKillsAllAlliesTriggersDefeatImmediately), ref passed, ref failed);
        RunTest(TestMultipleClashesSequentialResolutionWithQte, nameof(TestMultipleClashesSequentialResolutionWithQte), ref passed, ref failed);
        RunTest(TestReviveWithZeroOrNegativeHpDoesNotRevive, nameof(TestReviveWithZeroOrNegativeHpDoesNotRevive), ref passed, ref failed);
        RunTest(TestReviveWithExceedingMaxHpClampsProperly, nameof(TestReviveWithExceedingMaxHpClampsProperly), ref passed, ref failed);
        RunTest(TestReviveNormalRestoresHpAndClearsStatus, nameof(TestReviveNormalRestoresHpAndClearsStatus), ref passed, ref failed);
        RunTest(TestPainAbsorptionOnDeadAllyAppliesRecoilAndRevives, nameof(TestPainAbsorptionOnDeadAllyAppliesRecoilAndRevives), ref passed, ref failed);
        RunTest(TestPainAbsorptionRecoilClampsToZeroAndKillsLowHpCaster, nameof(TestPainAbsorptionRecoilClampsToZeroAndKillsLowHpCaster), ref passed, ref failed);
        RunTest(TestPainAbsorptionOnLivingAllyHealsWithoutRecoil, nameof(TestPainAbsorptionOnLivingAllyHealsWithoutRecoil), ref passed, ref failed);
        RunTest(TestPainAbsorptionRedirectsToDeadAllyWhenLivingTargeted, nameof(TestPainAbsorptionRedirectsToDeadAllyWhenLivingTargeted), ref passed, ref failed);
        RunTest(TestElementMatchupSuite, nameof(TestElementMatchupSuite), ref passed, ref failed);
        RunNUnitSuite<CombatUnitTestSuite>(ref passed, ref failed);
        RunNUnitSuite<BattleFlowTestSuite>(ref passed, ref failed);

        Debug.Log($"[CHALLENGER] ================= Finished Adversarial Tests: {passed} PASSED, {failed} FAILED =================");
        if (failed > 0)
        {
            Debug.LogError($"[CHALLENGER] Empirical challenge failed with {failed} failures.");
        }
        else
        {
            Debug.Log("[CHALLENGER] ALL EMPIRICAL CHALLENGES PASSED.");
        }
    }

    private static void RunTest(Action testMethod, string testName, ref int passed, ref int failed)
    {
        try
        {
            testMethod.Invoke();
            passed++;
            Debug.Log($"[CHALLENGER] [PASS] {testName}");
        }
        catch (Exception ex)
        {
            failed++;
            Exception inner = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
            Debug.LogError($"[CHALLENGER] [FAIL] {testName}: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
        }
    }

    private static void TestLethalClashKillsAllEnemiesTriggersVictoryImmediately()
    {
        GameObject bmObj = new GameObject("BM_LethalClashVictory");
        GameObject unitsObj = new GameObject("Units");
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit ally = CreateUnit(unitsObj.transform, "AllyWinner", CombatUnit.UnitType.Ally, speed: 10, attack: 50, defense: 0, hp: 100, element: CombatUnit.Element.Fire);
            CombatUnit enemy = CreateUnit(unitsObj.transform, "EnemyLoser", CombatUnit.UnitType.Enemy, speed: 10, attack: 10, defense: 0, hp: 20, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(ally);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            SetPrivateField(bm, "currentPhase", BattlePhase.ActionExecution);

            var selectedPlayerActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "selectedPlayerActions");
            selectedPlayerActions[ally] = new PlannedAction(CombatActionType.Attack, ally, enemy);

            var enemyPlannedActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "enemyPlannedActions");
            enemyPlannedActions[enemy] = new PlannedAction(CombatActionType.Attack, enemy, ally);

            InvokePrivate(bm, "BeginActionExecutionPhase");
            InvokePrivate(bm, "HandleActionExecution");

            Assert.IsFalse(enemy.IsAlive, "Enemy should be dead after lethal clash.");
            Assert.AreEqual(0, enemy.HP, "Enemy HP should be 0.");
            Assert.AreEqual(BattlePhase.Victory, bm.CurrentPhase, "BattlePhase should immediately transition to Victory upon lethal clash.");
            bool actionExecutionActive = (bool)GetPrivateField(bm, "actionExecutionActive");
            Assert.IsFalse(actionExecutionActive, "Action execution should not be active after victory.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestLethalClashKillsAllAlliesTriggersDefeatImmediately()
    {
        GameObject bmObj = new GameObject("BM_LethalClashDefeat");
        GameObject unitsObj = new GameObject("Units");
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit ally = CreateUnit(unitsObj.transform, "AllyLoser", CombatUnit.UnitType.Ally, speed: 10, attack: 10, defense: 0, hp: 15, element: CombatUnit.Element.Earth);
            CombatUnit enemy = CreateUnit(unitsObj.transform, "EnemyWinner", CombatUnit.UnitType.Enemy, speed: 10, attack: 50, defense: 0, hp: 100, element: CombatUnit.Element.Fire);

            bm.RegisterUnit(ally);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            SetPrivateField(bm, "currentPhase", BattlePhase.ActionExecution);

            var selectedPlayerActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "selectedPlayerActions");
            selectedPlayerActions[ally] = new PlannedAction(CombatActionType.Attack, ally, enemy);

            var enemyPlannedActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "enemyPlannedActions");
            enemyPlannedActions[enemy] = new PlannedAction(CombatActionType.Attack, enemy, ally);

            InvokePrivate(bm, "BeginActionExecutionPhase");
            InvokePrivate(bm, "HandleActionExecution");

            Assert.IsFalse(ally.IsAlive, "Ally should be dead after lethal clash.");
            Assert.AreEqual(0, ally.HP, "Ally HP should be 0.");
            Assert.AreEqual(BattlePhase.Defeat, bm.CurrentPhase, "BattlePhase should immediately transition to Defeat upon lethal clash.");
            bool actionExecutionActive = (bool)GetPrivateField(bm, "actionExecutionActive");
            Assert.IsFalse(actionExecutionActive, "Action execution should not be active after defeat.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestMultipleClashesSequentialResolutionWithQte()
    {
        GameObject bmObj = new GameObject("BM_MultipleClashes");
        GameObject unitsObj = new GameObject("Units");
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit ally1 = CreateUnit(unitsObj.transform, "Ally1", CombatUnit.UnitType.Ally, speed: 10, attack: 20, defense: 0, hp: 100, element: CombatUnit.Element.Water);
            CombatUnit enemy1 = CreateUnit(unitsObj.transform, "Enemy1", CombatUnit.UnitType.Enemy, speed: 10, attack: 20, defense: 0, hp: 100, element: CombatUnit.Element.Water);

            CombatUnit ally2 = CreateUnit(unitsObj.transform, "Ally2", CombatUnit.UnitType.Ally, speed: 8, attack: 20, defense: 0, hp: 100, element: CombatUnit.Element.Fire);
            CombatUnit enemy2 = CreateUnit(unitsObj.transform, "Enemy2", CombatUnit.UnitType.Enemy, speed: 8, attack: 20, defense: 0, hp: 100, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(ally1);
            bm.RegisterUnit(enemy1);
            bm.RegisterUnit(ally2);
            bm.RegisterUnit(enemy2);
            bm.StartBattle();

            SetPrivateField(bm, "currentPhase", BattlePhase.ActionExecution);

            var selectedPlayerActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "selectedPlayerActions");
            selectedPlayerActions[ally1] = new PlannedAction(CombatActionType.Attack, ally1, enemy1);
            selectedPlayerActions[ally2] = new PlannedAction(CombatActionType.Attack, ally2, enemy2);

            var enemyPlannedActions = (Dictionary<CombatUnit, PlannedAction>)GetPrivateField(bm, "enemyPlannedActions");
            enemyPlannedActions[enemy1] = new PlannedAction(CombatActionType.Attack, enemy1, ally1);
            enemyPlannedActions[enemy2] = new PlannedAction(CombatActionType.Attack, enemy2, ally2);

            // Pair 1 (Ally1 vs Enemy1) is Neutral (Water vs Water).
            // Pair 2 (Ally2 vs Enemy2) is Fire vs Earth (Fire strong).
            // Simulate QTE session starting for Pair 1 by intercepting OnNeutralClashQteRequested returning null, but setting pending QTE.
            SetPrivateField(bm, "neutralClashQtePending", true);
            SetPrivateField(bm, "pendingNeutralClashAlly", ally1);
            SetPrivateField(bm, "pendingNeutralClashEnemy", enemy1);

            // Resolve QTE for Pair 1
            InvokePrivate(bm, "HandleNeutralClashQteResolved", true);

            // Verify Pair 1 resolved:
            HashSet<CombatUnit> clashedUnits = (HashSet<CombatUnit>)GetPrivateField(bm, "clashedUnits");
            Assert.IsTrue(clashedUnits.Contains(ally1), "Ally1 must be in clashedUnits.");
            Assert.IsTrue(clashedUnits.Contains(enemy1), "Enemy1 must be in clashedUnits.");

            // Slower Pair 2 waits for its speed slot after the QTE resumes.
            Assert.IsFalse(clashedUnits.Contains(ally2), "A QTE callback must not advance a slower clash ahead of the turn queue.");
            SetPrivateField(bm, "actionStepTimer", 0f);
            InvokePrivate(bm, "HandleActionExecution"); // already-clashed Ally1
            SetPrivateField(bm, "actionStepTimer", 0f);
            InvokePrivate(bm, "HandleActionExecution"); // already-clashed Enemy1
            SetPrivateField(bm, "actionStepTimer", 0f);
            InvokePrivate(bm, "HandleActionExecution"); // Ally2's speed slot
            Assert.IsTrue(clashedUnits.Contains(ally2), "Ally2 must be in clashedUnits after sequential resolution.");
            Assert.IsTrue(clashedUnits.Contains(enemy2), "Enemy2 must be in clashedUnits after sequential resolution.");

            // Verify Fire vs Earth clash damage was applied to Enemy2:
            // Enemy2 was at 100 HP, took 1.5 * 20 = 30 clash damage -> 70 HP.
            Assert.AreEqual(70, enemy2.HP, "Enemy2 should take clash loser damage (30).");
            // Ally2 was at 100 HP, took 0.5 * 20 = 10 clash damage -> 90 HP.
            Assert.AreEqual(90, ally2.HP, "Ally2 should take clash winner recoil damage (10).");

            // Verify action execution was resumed because no more clashes remain
            bool actionExecutionActive = (bool)GetPrivateField(bm, "actionExecutionActive");
            Assert.IsTrue(actionExecutionActive, "Action execution should resume once all clashes resolve.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestReviveWithZeroOrNegativeHpDoesNotRevive()
    {
        GameObject obj = new GameObject("ReviveTest_ZeroNegative");
        try
        {
            CombatUnit unit = obj.AddComponent<CombatUnit>();
            unit.MaxHP = 100;
            unit.HP = 0;
            unit.DebugIsAlive = false;

            // Revive with 0 HP
            unit.Revive(0);
            Assert.IsFalse(unit.IsAlive, "Unit must remain dead when Revive(0) is called.");
            Assert.AreEqual(0, unit.HP, "HP must remain 0 when Revive(0) is called.");

            // Revive with negative HP
            unit.Revive(-50);
            Assert.IsFalse(unit.IsAlive, "Unit must remain dead when Revive(-50) is called.");
            Assert.AreEqual(0, unit.HP, "HP must remain 0 when Revive(-50) is called.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void TestReviveWithExceedingMaxHpClampsProperly()
    {
        GameObject obj = new GameObject("ReviveTest_Exceed");
        try
        {
            CombatUnit unit = obj.AddComponent<CombatUnit>();
            unit.MaxHP = 100;
            unit.HP = 0;
            unit.DebugIsAlive = false;

            unit.Revive(999);
            Assert.IsTrue(unit.IsAlive, "Unit must be alive after Revive(999).");
            Assert.AreEqual(100, unit.HP, "HP must be clamped to MaxHP (100).");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void TestReviveNormalRestoresHpAndClearsStatus()
    {
        GameObject obj = new GameObject("ReviveTest_Normal");
        try
        {
            CombatUnit unit = obj.AddComponent<CombatUnit>();
            unit.MaxHP = 100;
            unit.HP = 0;
            unit.DebugIsAlive = false;
            unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 3, 5f, "Poison"));
            unit.StartDefend();

            unit.Revive(50);
            Assert.IsTrue(unit.IsAlive, "Unit must be alive after Revive(50).");
            Assert.AreEqual(50, unit.HP, "HP must be 50.");
            Assert.AreEqual(0, unit.ActiveEffects.Count, "All status effects must be cleared on Revive.");
            Assert.IsFalse(unit.IsDefending, "Defending state must be cleared on Revive.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void TestPainAbsorptionOnDeadAllyAppliesRecoilAndRevives()
    {
        GameObject bmObj = new GameObject("BM_PainAbsorptionDeadAlly");
        GameObject unitsObj = new GameObject("Units");
        SkillData painAbsorb = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit merrick = CreateUnit(unitsObj.transform, "Merrick", CombatUnit.UnitType.Ally, speed: 10, attack: 15, defense: 5, hp: 100, element: CombatUnit.Element.Water);
            merrick.MaxMP = 50;
            merrick.MP = 50;

            CombatUnit deadAlly = CreateUnit(unitsObj.transform, "Briar", CombatUnit.UnitType.Ally, speed: 12, attack: 20, defense: 2, hp: 80, element: CombatUnit.Element.Fire);
            deadAlly.HP = 0;
            deadAlly.DebugIsAlive = false;

            CombatUnit enemy = CreateUnit(unitsObj.transform, "Enemy", CombatUnit.UnitType.Enemy, speed: 8, attack: 10, defense: 0, hp: 100, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(merrick);
            bm.RegisterUnit(deadAlly);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            painAbsorb.skillName = "Pain Absorption";
            painAbsorb.target = SkillTarget.SingleAlly;
            painAbsorb.healMultiplier = 0.5f;
            painAbsorb.mpCost = 40;
            painAbsorb.element = CombatUnit.Element.Water;

            InvokePrivate(bm, "ResolveAllyTargetedSkill", merrick, deadAlly, painAbsorb);

            // Briar revived to 50% MaxHP (80 * 0.5 = 40)
            Assert.IsTrue(deadAlly.IsAlive, "Briar should be revived.");
            Assert.AreEqual(40, deadAlly.HP, "Briar HP should be 40 (50% MaxHP).");

            // Merrick takes 30% MaxHP recoil (100 * 0.30 = 30) -> 100 - 30 = 70 HP
            Assert.AreEqual(70, merrick.HP, "Merrick should take 30 recoil damage (30% MaxHP).");
            Assert.IsTrue(merrick.IsAlive, "Merrick should still be alive with 70 HP.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(painAbsorb);
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestPainAbsorptionRecoilClampsToZeroAndKillsLowHpCaster()
    {
        GameObject bmObj = new GameObject("BM_PainAbsorptionRecoilKill");
        GameObject unitsObj = new GameObject("Units");
        SkillData painAbsorb = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit merrick = CreateUnit(unitsObj.transform, "Merrick", CombatUnit.UnitType.Ally, speed: 10, attack: 15, defense: 5, hp: 100, element: CombatUnit.Element.Water);
            merrick.MaxMP = 50;
            merrick.MP = 50;
            // Set Merrick to 20 HP (less than 30% MaxHP recoil of 30)
            merrick.HP = 20;

            CombatUnit deadAlly = CreateUnit(unitsObj.transform, "Briar", CombatUnit.UnitType.Ally, speed: 12, attack: 20, defense: 2, hp: 80, element: CombatUnit.Element.Fire);
            deadAlly.HP = 0;
            deadAlly.DebugIsAlive = false;

            CombatUnit enemy = CreateUnit(unitsObj.transform, "Enemy", CombatUnit.UnitType.Enemy, speed: 8, attack: 10, defense: 0, hp: 100, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(merrick);
            bm.RegisterUnit(deadAlly);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            painAbsorb.skillName = "Pain Absorption";
            painAbsorb.target = SkillTarget.SingleAlly;
            painAbsorb.healMultiplier = 0.5f;
            painAbsorb.mpCost = 40;
            painAbsorb.element = CombatUnit.Element.Water;

            InvokePrivate(bm, "ResolveAllyTargetedSkill", merrick, deadAlly, painAbsorb);

            // Briar revived
            Assert.IsTrue(deadAlly.IsAlive, "Briar should be revived.");
            Assert.AreEqual(40, deadAlly.HP, "Briar HP should be 40.");

            // Merrick takes 30 recoil damage. 20 - 30 = -10, clamped to 0, triggers Die()
            Assert.AreEqual(0, merrick.HP, "Merrick HP should be clamped to 0.");
            Assert.IsFalse(merrick.IsAlive, "Merrick should die from recoil damage when HP <= 30%.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(painAbsorb);
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestPainAbsorptionOnLivingAllyHealsWithoutRecoil()
    {
        GameObject bmObj = new GameObject("BM_PainAbsorptionLivingAlly");
        GameObject unitsObj = new GameObject("Units");
        SkillData painAbsorb = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit merrick = CreateUnit(unitsObj.transform, "Merrick", CombatUnit.UnitType.Ally, speed: 10, attack: 15, defense: 5, hp: 100, element: CombatUnit.Element.Water);
            merrick.MaxMP = 50;
            merrick.MP = 50;

            CombatUnit livingAlly = CreateUnit(unitsObj.transform, "Briar", CombatUnit.UnitType.Ally, speed: 12, attack: 20, defense: 2, hp: 100, element: CombatUnit.Element.Fire);
            livingAlly.HP = 40; // damaged but alive

            CombatUnit enemy = CreateUnit(unitsObj.transform, "Enemy", CombatUnit.UnitType.Enemy, speed: 8, attack: 10, defense: 0, hp: 100, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(merrick);
            bm.RegisterUnit(livingAlly);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            painAbsorb.skillName = "Pain Absorption";
            painAbsorb.target = SkillTarget.SingleAlly;
            painAbsorb.healMultiplier = 0.5f;
            painAbsorb.mpCost = 40;
            painAbsorb.element = CombatUnit.Element.Water;

            int merrickHpBefore = merrick.HP;
            int briarHpBefore = livingAlly.HP;

            InvokePrivate(bm, "ResolveAllyTargetedSkill", merrick, livingAlly, painAbsorb);

            // Briar is healed:
            Assert.Greater(livingAlly.HP, briarHpBefore, "Briar should receive healing when targeted while alive (and no ally is dead).");
            // Merrick does NOT take recoil when cast on a living ally:
            Assert.AreEqual(merrickHpBefore, merrick.HP, "Merrick must NOT suffer recoil damage when casting on a living ally.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(painAbsorb);
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestPainAbsorptionRedirectsToDeadAllyWhenLivingTargeted()
    {
        GameObject bmObj = new GameObject("BM_PainAbsorptionRedirect");
        GameObject unitsObj = new GameObject("Units");
        SkillData painAbsorb = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            CombatUnit merrick = CreateUnit(unitsObj.transform, "Merrick", CombatUnit.UnitType.Ally, speed: 10, attack: 15, defense: 5, hp: 100, element: CombatUnit.Element.Water);
            merrick.MaxMP = 50;
            merrick.MP = 50;

            CombatUnit livingAlly = CreateUnit(unitsObj.transform, "LivingAlly", CombatUnit.UnitType.Ally, speed: 12, attack: 20, defense: 2, hp: 100, element: CombatUnit.Element.Fire);
            livingAlly.HP = 40;

            CombatUnit deadAlly = CreateUnit(unitsObj.transform, "DeadAlly", CombatUnit.UnitType.Ally, speed: 9, attack: 18, defense: 1, hp: 60, element: CombatUnit.Element.Air);
            deadAlly.HP = 0;
            deadAlly.DebugIsAlive = false;

            CombatUnit enemy = CreateUnit(unitsObj.transform, "Enemy", CombatUnit.UnitType.Enemy, speed: 8, attack: 10, defense: 0, hp: 100, element: CombatUnit.Element.Earth);

            bm.RegisterUnit(merrick);
            bm.RegisterUnit(livingAlly);
            bm.RegisterUnit(deadAlly);
            bm.RegisterUnit(enemy);
            bm.StartBattle();

            painAbsorb.skillName = "Pain Absorption";
            painAbsorb.target = SkillTarget.SingleAlly;
            painAbsorb.healMultiplier = 0.5f;
            painAbsorb.mpCost = 40;
            painAbsorb.element = CombatUnit.Element.Water;

            // Target livingAlly, but deadAlly exists
            InvokePrivate(bm, "ResolveAllyTargetedSkill", merrick, livingAlly, painAbsorb);

            // deadAlly should be revived because Pain Absorption prioritizes dead allies
            Assert.IsTrue(deadAlly.IsAlive, "Dead ally should be revived even when living ally was initially targeted.");
            Assert.AreEqual(30, deadAlly.HP, "Dead ally HP should be 30 (50% of 60).");
            Assert.AreEqual(70, merrick.HP, "Merrick should take 30% MaxHP recoil.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(painAbsorb);
            UnityEngine.Object.DestroyImmediate(unitsObj);
            UnityEngine.Object.DestroyImmediate(bmObj);
        }
    }

    private static void TestElementMatchupSuite()
    {
        GameObject obj = new GameObject("ElementMatchupTestObj");
        try
        {
            ElementMatchupTest test = obj.AddComponent<ElementMatchupTest>();
            test.RunTests();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void RunNUnitSuite<T>(ref int passed, ref int failed) where T : new()
    {
        MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo setUp = FindAttributedMethod<SetUpAttribute>(methods);
        MethodInfo tearDown = FindAttributedMethod<TearDownAttribute>(methods);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo test = methods[i];
            if (!Attribute.IsDefined(test, typeof(TestAttribute)))
            {
                continue;
            }

            T instance = new T();
            bool tearDownAttempted = false;
            try
            {
                setUp?.Invoke(instance, null);
                test.Invoke(instance, null);
                tearDownAttempted = true;
                tearDown?.Invoke(instance, null);
                passed++;
                Debug.Log($"[CHALLENGER] [PASS] {typeof(T).Name}.{test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Exception root = Unwrap(ex);
                Debug.LogError($"[CHALLENGER] [FAIL] {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
            }
            finally
            {
                if (!tearDownAttempted)
                {
                    try
                    {
                        tearDown?.Invoke(instance, null);
                    }
                    catch
                    {
                        // best-effort cleanup
                    }
                }
            }
        }
    }

    private static MethodInfo FindAttributedMethod<TAttribute>(MethodInfo[] methods) where TAttribute : Attribute
    {
        for (int i = 0; i < methods.Length; i++)
        {
            if (Attribute.IsDefined(methods[i], typeof(TAttribute)))
            {
                return methods[i];
            }
        }

        return null;
    }

    private static Exception Unwrap(Exception ex)
    {
        Exception current = ex;
        while (current is TargetInvocationException && current.InnerException != null)
        {
            current = current.InnerException;
        }

        return current;
    }

    private static CombatUnit CreateUnit(
        Transform parent,
        string name,
        CombatUnit.UnitType type,
        int speed,
        int attack,
        int defense,
        int hp,
        CombatUnit.Element element = CombatUnit.Element.None)
    {
        GameObject unitObject = new GameObject(name);
        unitObject.transform.SetParent(parent, false);

        CombatUnit unit = unitObject.AddComponent<CombatUnit>();
        unit.UnitName = name;
        unit.Type = type;
        unit.Speed = speed;
        unit.Attack = attack;
        unit.Defense = defense;
        unit.ElementType = element;
        unit.MaxHP = Mathf.Max(1, hp);
        unit.HP = Mathf.Clamp(hp, 0, unit.MaxHP);
        unit.DebugIsAlive = unit.HP > 0;
        return unit;
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        Assert.IsNotNull(target, "Reflection target should not be null.");
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");
        return method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        Assert.IsNotNull(target, "Reflection target should not be null.");
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        Assert.IsNotNull(target, "Reflection target should not be null.");
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        return field.GetValue(target);
    }
}
#endif
