#if UNITY_EDITOR || TIDE_COMBAT_HARNESS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Production combat regressions driven through real round execution.</summary>
[DisallowMultipleComponent]
public class CombatReadinessRegressionTest : MonoBehaviour
{
    private GameObject managerObject;
    private BattleManager battle;
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

    [SetUp]
    public void SetUp()
    {
        managerObject = new GameObject("CombatReadinessBattle");
        battle = managerObject.AddComponent<BattleManager>();
        battle.autoStartBattle = false;
        SetField("autoAdvancePhases", false);
        SetField("enableActionAnimations", false);
        SetField("enableNeutralClashQte", false);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object item in created) UnityEngine.Object.DestroyImmediate(item);
        created.Clear();
        UnityEngine.Object.DestroyImmediate(managerObject);
    }

    [ContextMenu("Run Combat Readiness Regressions")]
    public void RunTests()
    {
        MethodInfo[] methods = GetType().GetMethods();
        int count = 0;
        foreach (MethodInfo test in methods)
        {
            if (!Attribute.IsDefined(test, typeof(TestAttribute))) continue;
            SetUp();
            try { test.Invoke(this, null); count++; }
            finally { TearDown(); }
        }
        Debug.Log($"[CombatReadinessRegressionTest] {count} combat regressions passed.");
    }

    [Test]
    public void CanonicalFiveElementMatrixIsExact()
    {
        int[][] strong = { new[] { 3, 4 }, new[] { 1, 5 }, new[] { 2, 5 }, new[] { 3, 2 }, new[] { 1, 4 } };
        for (int attacker = 1; attacker <= 5; attacker++)
        for (int defender = 1; defender <= 5; defender++)
        {
            MatchupResult expected = attacker == defender ? MatchupResult.Neutral
                : Array.IndexOf(strong[attacker - 1], defender) >= 0 ? MatchupResult.Strong : MatchupResult.Weak;
            Assert.AreEqual(expected, ElementMatchup.GetResult((CombatUnit.Element)attacker, (CombatUnit.Element)defender),
                $"GDD elemental matrix {attacker}->{defender}");
        }
        Assert.AreEqual(MatchupResult.Neutral, ElementMatchup.GetResult((CombatUnit.Element)99, CombatUnit.Element.Fire));
    }

    [Test]
    public void OneTurnBuffAndDrowsySurviveUntilActionCompletes()
    {
        CombatUnit unit = Unit("Hero", true);
        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffAttack, 1, 0.5f, "Test"));
        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Drowsy, 1, 1f, "Test"));
        unit.ProcessTurnStartEffects();
        Assert.AreEqual(0.5f, unit.GetAttackModifier(), 0.0001f);
        Assert.IsTrue(unit.ShouldSkipTurn(), "One-turn drowsy must still skip this turn.");
        unit.ProcessTurnEndEffects();
        Assert.AreEqual(0, unit.ActiveEffects.Count);
        Assert.IsFalse(unit.ShouldSkipTurn(), "The control effect must not leak into the next round.");
    }

    [Test]
    public void StatusApplicationCopiesEachRecipientsDuration()
    {
        CombatUnit first = Unit("First", true), second = Unit("Second", true);
        StatusEffect shared = new StatusEffect(StatusEffectType.BuffAttack, 2, 0.2f, "AoE");
        first.ApplyStatusEffect(shared); second.ApplyStatusEffect(shared);
        first.ProcessTurnStartEffects(); first.ProcessTurnEndEffects();
        Assert.AreEqual(2, second.ActiveEffects[0].Duration);
        Assert.AreEqual(2, shared.Duration, "Applying a status must not transfer caller-owned mutable state.");
    }

    [Test]
    public void MalformedStatusAndSkillDataCannotPoisonCombatMath()
    {
        CombatUnit hero = Unit("Hero", true);
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffAttack, 0, 1f, "Spent"));
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffAttack, 2, float.NaN, "Bad"));
        hero.ApplyStatusEffect(new StatusEffect((StatusEffectType)99, 2, 1f, "Bad"));
        Assert.AreEqual(0, hero.ActiveEffects.Count);
        SkillData bad = Skill("Bad", SkillTarget.SingleEnemy, 2, float.PositiveInfinity);
        Assert.IsFalse(bad.IsValid()); Assert.IsFalse(hero.CanUseSkill(bad));
        MomentumState momentum = new MomentumState();
        momentum.ShiftTowardPlayer(float.NaN); momentum.ShiftTowardEnemy(float.PositiveInfinity);
        Assert.AreEqual(0f, momentum.Value);
    }

    [Test]
    public void StunnedMutualTargetCannotClashOrSpendSkillMp()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.ElementType = CombatUnit.Element.Fire; enemy.ElementType = CombatUnit.Element.Earth;
        SkillData skill = Skill("Flame", SkillTarget.SingleEnemy, 7, 2f);
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.Stun, 1, 1f, "Test"));
        Plan(new PlannedAction(CombatActionType.Skill, hero, enemy, skill), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Step();
        Assert.AreEqual(100, hero.HP); Assert.AreEqual(100, enemy.HP);
        Assert.AreEqual(50, hero.MP, "A skipped action must not spend MP.");
        Step(); Assert.Less(hero.HP, 100, "The enemy still receives its normal turn.");
        Step(); battle.AdvancePhase();
        Assert.IsFalse(hero.ShouldSkipTurn(), "A one-turn stun expires after the skipped turn.");
    }

    [Test]
    public void LethalTurnStartPoisonStopsBattleBeforeClash()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.HP = 1; hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 1, 2, "Test"));
        Plan(new PlannedAction(CombatActionType.Attack, hero, enemy), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Step();
        Assert.AreEqual(BattlePhase.Defeat, battle.CurrentPhase);
        Assert.AreEqual(100, enemy.HP, "Poison-killed units cannot resolve a queued clash.");
    }

    [Test]
    public void SkillClashUsesAuthoredDamageCostsMpAndAppliesStatusOnce()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.ElementType = CombatUnit.Element.Fire; enemy.ElementType = CombatUnit.Element.Earth;
        SkillData skill = Skill("Flame", SkillTarget.SingleEnemy, 7, 3f);
        skill.appliedEffectType = StatusEffectType.DebuffDefense; skill.effectDuration = 2; skill.effectMagnitude = 0.2f;
        Plan(new PlannedAction(CombatActionType.Skill, hero, enemy, skill), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Step();
        Assert.AreEqual(43, hero.MP);
        Assert.AreEqual(55, enemy.HP, "10 attack x 3 skill x 1.5 clash must deal 45 damage.");
        Assert.AreEqual(95, hero.HP, "The loser still lands its simultaneous reduced attack.");
        Assert.AreEqual(1, enemy.ActiveEffects.Count);
        Step(); Assert.AreEqual(43, hero.MP); Assert.AreEqual(55, enemy.HP);
    }

    [Test]
    public void SupportSkillsNeverBecomeHostileClashes()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        SkillData heal = Skill("Mend", SkillTarget.SingleAlly, 4, 0f); heal.healMultiplier = 1f;
        hero.HP = 50;
        Plan(new PlannedAction(CombatActionType.Skill, hero, hero, heal), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Step();
        Assert.Greater(hero.HP, 50); Assert.AreEqual(100, enemy.HP); Assert.AreEqual(46, hero.MP);
    }

    [Test]
    public void SimultaneousLethalClashResolvesBothHitsAndDefeat()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.ElementType = CombatUnit.Element.Fire; enemy.ElementType = CombatUnit.Element.Earth;
        hero.HP = 5; enemy.HP = 5;
        Plan(new PlannedAction(CombatActionType.Attack, hero, enemy), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Step();
        Assert.AreEqual(0, hero.HP); Assert.AreEqual(0, enemy.HP);
        Assert.AreEqual(BattlePhase.Defeat, battle.CurrentPhase, "A wiped player party cannot collect a victory reward.");
    }

    [Test]
    public void FasterOrdinaryActionRunsBeforeSlowerClash()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        CombatUnit fast = Unit("Fast", true), target = Unit("OtherEnemy", false);
        fast.Speed = 100; hero.Speed = 20; enemy.Speed = 10; target.Speed = 5;
        Plan(new PlannedAction(CombatActionType.Attack, hero, enemy), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Actions("selectedPlayerActions")[fast] = new PlannedAction(CombatActionType.Attack, fast, target);
        Actions("enemyPlannedActions")[target] = new PlannedAction(CombatActionType.Defend, target, null);
        Step();
        Assert.Less(target.HP, 100); Assert.AreEqual(100, enemy.HP); Assert.AreEqual(100, hero.HP);
    }

    [Test]
    public void NoBattleActionCanTargetAUnitOutsideItsRegisteredRoster()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        CombatUnit outsider = Unit("Outsider", false, false);
        Call("ResolveAttack", hero, outsider);
        Assert.AreEqual(100, outsider.HP); Assert.Less(enemy.HP, 100);
    }

    [Test]
    public void EnemyAreaTideBreakHitsEveryLivingOpponentAndConsumesGauge()
    {
        CombatUnit hero = Unit("Hero", true), second = Unit("Second", true), enemy = Unit("Enemy", false);
        TideBreakData tideBreak = TideBreak(SkillTarget.AllEnemies);
        battle.Momentum.ShiftTowardEnemy(1f);
        Call("ResolveTideBreak", enemy, hero, tideBreak);
        Assert.Less(hero.HP, 100); Assert.Less(second.HP, 100); Assert.AreEqual(100, enemy.HP);
        Assert.AreEqual(0f, battle.Momentum.Value);
    }

    [Test]
    public void PlannedTideBreakCannotReuseAnAlreadyConsumedSharedGauge()
    {
        CombatUnit hero = Unit("Hero", true), second = Unit("Second", true), enemy = Unit("Enemy", false);
        enemy.MaxHP = 1000; enemy.HP = 1000;
        TideBreakData tideBreak = TideBreak(SkillTarget.AllEnemies); tideBreak.damageMultiplier = 10f;
        battle.Momentum.ShiftTowardPlayer(1f);
        Call("ResolveTideBreak", hero, enemy, tideBreak); int afterFirst = enemy.HP;
        Call("ResolveTideBreak", second, enemy, tideBreak);
        Assert.Less(afterFirst - enemy.HP, 20, "The second queued Tide Break falls back to a basic attack.");
    }

    [Test]
    public void TideBreakClashConsumesChargeAndKeepsItsAuthoredPower()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.ElementType = CombatUnit.Element.Fire; enemy.ElementType = CombatUnit.Element.Earth;
        TideBreakData tideBreak = TideBreak(SkillTarget.SingleEnemy); tideBreak.damageMultiplier = 2f;
        Plan(new PlannedAction(CombatActionType.TideBreak, hero, enemy, tideBreak), new PlannedAction(CombatActionType.Attack, enemy, hero));
        battle.Momentum.ShiftTowardPlayer(1f);
        Step();
        Assert.AreEqual(70, enemy.HP); Assert.IsFalse(battle.Momentum.IsPlayerTideBreakReady);
    }

    [Test]
    public void RepeatedTerminalCallbacksKeepTheFirstResult()
    {
        Unit("Hero", true); Unit("Enemy", false); battle.StartBattle();
        battle.SetVictory(); battle.SetVictory(); battle.SetDefeat();
        Assert.AreEqual(BattlePhase.Victory, battle.CurrentPhase);
    }

    [Test]
    public void ZeroDamageDebuffSkillDoesNotInflictMinimumDamage()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        SkillData debuff = Skill("Slow", SkillTarget.SingleEnemy, 3, 0f);
        debuff.appliedEffectType = StatusEffectType.Slow; debuff.effectDuration = 1; debuff.effectMagnitude = 0.5f;
        Call("ResolveSkill", hero, enemy, debuff);
        Assert.AreEqual(100, enemy.HP); Assert.AreEqual(1, enemy.ActiveEffects.Count); Assert.AreEqual(47, hero.MP);
    }

    [Test]
    public void ReviveClearsOldStunAndSwapSkipState()
    {
        CombatUnit hero = Unit("Hero", true);
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.Stun, 2, 1f, "Test")); hero.SkipTurnThisRound = true;
        hero.HP = 0; hero.Revive(30);
        Assert.IsTrue(hero.IsAlive); Assert.IsFalse(hero.ShouldSkipTurn()); Assert.IsFalse(hero.SkipTurnThisRound);
        Assert.AreEqual(0, hero.ActiveEffects.Count);
    }

    [Test]
    public void RefreshDuringTurnKeepsTheNewFullDuration()
    {
        CombatUnit hero = Unit("Hero", true);
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffAttack, 1, 0.2f, "Old"));
        hero.ProcessTurnStartEffects();
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffAttack, 3, 0.4f, "Refresh"));
        hero.ProcessTurnEndEffects();
        Assert.AreEqual(3, hero.ActiveEffects[0].Duration);
        Assert.AreEqual(0.4f, hero.GetAttackModifier(), 0.0001f);
    }

    [Test]
    public void StunAppliedAfterPreparationStillStopsTheQueuedAction()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        Plan(new PlannedAction(CombatActionType.Attack, hero, enemy), new PlannedAction(CombatActionType.Attack, enemy, hero));
        Call("PrepareUnitTurn", hero);
        hero.ApplyStatusEffect(new StatusEffect(StatusEffectType.Stun, 1, 1f, "Late stun"));
        Step();
        Assert.AreEqual(100, hero.HP); Assert.AreEqual(100, enemy.HP);
    }

    [Test]
    public void EnemyAiSkipsUnaffordableSkillsAndUsesAnAffordableAlternative()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        enemy.MP = 3;
        SkillData expensive = Skill("Expensive", SkillTarget.SingleEnemy, 10, 2f);
        SkillData affordable = Skill("Affordable", SkillTarget.SingleEnemy, 2, 1f);
        enemy.SetSkills(new[] { expensive, affordable });
        PlannedAction action = (PlannedAction)Call("ComputeEnemyAction", enemy);
        Assert.AreSame(affordable, action.SelectedSkill);
    }

    [Test]
    public void EnvyBossCovetsBeforeItsOrdinarySkillsAndScalesAreaDamage()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        hero.ElementType = CombatUnit.Element.Fire; enemy.ElementType = CombatUnit.Element.Water;
        SkillData own = Skill("Own attack", SkillTarget.SingleEnemy, 1, 1f);
        SkillData copied = Skill("Copied area attack", SkillTarget.AllEnemies, 3, 1f);
        enemy.SetSkills(new[] { own });
        battle.ConfigureEnvyContext(true, true); SetField("lastAttacker", hero); SetField("lastPlayerSkill", copied);
        PlannedAction action = (PlannedAction)Call("ComputeEnemyAction", enemy);
        Assert.AreSame(copied, action.SelectedSkill);
        Assert.AreEqual(CombatUnit.Element.Fire, enemy.ElementType);
        Call("ResolveSkill", enemy, null, copied);
        Assert.GreaterOrEqual(hero.HP, 92); Assert.LessOrEqual(hero.HP, 94);
        Assert.AreEqual(47, enemy.MP);
    }

    [Test]
    public void HudRejectsSkillsAndTideBreaksTheCurrentHeroHasNotUnlocked()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        battle.StartBattle(); battle.AdvancePhase();
        battle.SetPendingSkill(Skill("Unowned", SkillTarget.SingleEnemy, 1, 1f));
        Assert.IsFalse(battle.TryAssignActionFromHud(CombatActionType.Skill, enemy));
        battle.Momentum.ShiftTowardPlayer(1f); battle.SetPendingTideBreak(TideBreak(SkillTarget.AllEnemies));
        Assert.IsFalse(battle.TryAssignActionFromHud(CombatActionType.TideBreak, null));
    }

    [Test]
    public void NewGamePlusEnemyScalingUsesBaseDataAndDoesNotDuplicateXpBonus()
    {
        CombatUnit enemy = Unit("Enemy", false);
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>(); created.Add(data);
        data.baseMaxHP = 100; data.baseMaxMP = 20; data.baseAttack = 10; data.baseDefense = 4; data.baseSpeed = 8; data.xpReward = 25;
        EnemyCombatStatScaler.ApplyEnemyData(enemy, data, 1.2f, 1.5f);
        Assert.AreEqual(180, enemy.MaxHP); Assert.AreEqual(36, enemy.MaxMP); Assert.AreEqual(18, enemy.Attack);
        Assert.AreEqual(30, enemy.XpReward, "NG+ XP is granted by progression, not applied to each enemy reward.");
        enemy.HP = 0;
        EnemyCombatStatScaler.ApplyEnemyData(enemy, data, 1.2f, 1.5f);
        Assert.AreEqual(180, enemy.MaxHP, "Reapplying base data must not compound scaled live stats.");
        Assert.IsTrue(enemy.IsAlive); Assert.AreEqual(enemy.MaxHP, enemy.HP);
    }

    [Test]
    public void LargeCombatStatsSaturateInsteadOfOverflowingToMinimumDamage()
    {
        CombatUnit hero = Unit("Hero", true), enemy = Unit("Enemy", false);
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>(); created.Add(data);
        data.baseMaxHP = int.MaxValue; data.baseAttack = int.MaxValue;
        EnemyCombatStatScaler.ApplyEnemyData(enemy, data, 1000f, 1000f);
        Assert.AreEqual(int.MaxValue, enemy.MaxHP); Assert.AreEqual(int.MaxValue, enemy.Attack);
        hero.Defense = 0;
        Call("ResolveAttack", enemy, hero);
        Assert.AreEqual(0, hero.HP, "A huge attack must not overflow to one damage.");
        enemy.Defense = int.MaxValue; enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.BuffDefense, 2, 1f, "Test"));
        int before = enemy.HP; enemy.TakeDamage(100);
        Assert.AreEqual(before - 1, enemy.HP, "A huge defense must not overflow to zero defense.");
    }

    [Test]
    public void ShippedScalarSkillDataPassesCombatValidation()
    {
        SkillData[] skills = Resources.LoadAll<SkillData>("Skills");
        Assert.Greater(skills.Length, 0);
        foreach (SkillData skill in skills) Assert.IsTrue(skill.IsValid(), "Invalid shipped skill: " + skill.skillName);
    }

    [Test]
    public void BattleStartAndRestartPreserveCrossFactionRegistrationTies()
    {
        CombatUnit enemy = Unit("RegisteredFirstEnemy", false), hero = Unit("RegisteredSecondHero", true);
        enemy.Speed = hero.Speed = 10;
        battle.StartBattle();
        Assert.AreSame(enemy, battle.TurnQueue[0]);
        battle.StartBattle();
        Assert.AreSame(enemy, battle.TurnQueue[0]);
        Assert.AreEqual(2, battle.TurnQueue.Count);
    }

    [Test]
    public void RegistrationPrunesDetachedUnitsAndKeepsFactionRostersDisjoint()
    {
        CombatUnit old = Unit("Detached", false), hero = Unit("Hero", true);
        typeof(BattleManager).GetField("enemyUnits", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, new List<CombatUnit>());
        hero.Type = CombatUnit.UnitType.Enemy; battle.RegisterUnit(hero);
        Assert.AreEqual(0, battle.AllyUnits.Count); Assert.AreEqual(1, battle.EnemyUnits.Count);
        battle.StartBattle();
        var registration = (Dictionary<CombatUnit, int>)typeof(BattleManager).GetField("unitRegistrationOrder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
        Assert.IsFalse(registration.ContainsKey(old)); Assert.AreEqual(1, registration.Count);
    }

    [Test]
    public void RetargetingCannotGiveAnOpponentThatAlreadyActedASecondClashAttack()
    {
        CombatUnit hero = Unit("Hero", true), fastEnemy = Unit("FastEnemy", false);
        CombatUnit killer = Unit("Killer", true), doomedEnemy = Unit("DoomedEnemy", false);
        fastEnemy.Speed = 40; killer.Speed = 20; hero.Speed = 10; doomedEnemy.Speed = 5; doomedEnemy.HP = 1;
        Plan(new PlannedAction(CombatActionType.Attack, hero, doomedEnemy), new PlannedAction(CombatActionType.Attack, fastEnemy, hero));
        Actions("selectedPlayerActions")[killer] = new PlannedAction(CombatActionType.Attack, killer, doomedEnemy);
        Actions("enemyPlannedActions")[doomedEnemy] = new PlannedAction(CombatActionType.Defend, doomedEnemy, null);
        Step(); int heroAfterFastEnemy = hero.HP;
        Step(); Assert.IsFalse(doomedEnemy.IsAlive);
        Step();
        Assert.Less(fastEnemy.HP, 100, "The hero retargets its normal attack to the surviving opponent.");
        Assert.AreEqual(heroAfterFastEnemy, hero.HP, "An opponent that already acted must not get another attack through a late clash.");
    }

    [Test]
    public void DynamicallyCreatedBattleUsesExistingAuthoredViceDefaults()
    {
        battle.ConfigureViceAI(ViceType.Desire);
        var profile = (ViceAIProfile)typeof(BattleManager).GetField("activeViceProfile", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
        Assert.IsNotNull(profile); Assert.AreEqual(ViceType.Desire, profile.vice);
        Assert.AreEqual(0.8f, profile.skillUsageWeight, 0.0001f); Assert.IsTrue(profile.IsValid());
        battle.ConfigureViceAI((ViceType)99);
        Assert.IsNull(typeof(BattleManager).GetField("activeViceProfile", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle));
    }

    [Test]
    public void BattleResolvesSingletonDifficultyWithoutAnInspectorReference()
    {
        GameObject settingsObject = new GameObject("CombatDifficultySettings"); created.Add(settingsObject);
        DifficultyModeService settings = settingsObject.AddComponent<DifficultyModeService>();
        settings.SetDifficulty(DifficultyModeService.Difficulty.Hardcore);
        Unit("Hero", true); Unit("Enemy", false); battle.StartBattle(); battle.AdvancePhase();
        Assert.AreSame(settings, typeof(BattleManager).GetField("difficultyService", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle));
        bool accepted = battle.TryAttemptFleeFromMenu(out bool fled, out float chance, out float roll);
        Assert.IsFalse(accepted, "Hardcore flee restrictions apply to generated battles too.");
    }

    private CombatUnit Unit(string name, bool ally, bool register = true)
    {
        GameObject obj = new GameObject(name); created.Add(obj);
        CombatUnit unit = obj.AddComponent<CombatUnit>();
        unit.UnitName = name; unit.Type = ally ? CombatUnit.UnitType.Ally : CombatUnit.UnitType.Enemy;
        unit.Attack = 10; unit.Defense = 0; unit.Speed = ally ? 30 : 20; unit.CritRate = 0f;
        if (register) battle.RegisterUnit(unit);
        return unit;
    }

    private SkillData Skill(string name, SkillTarget target, int cost, float damage)
    {
        SkillData data = ScriptableObject.CreateInstance<SkillData>(); created.Add(data);
        data.skillName = name; data.target = target; data.mpCost = cost; data.damageMultiplier = damage;
        return data;
    }

    private TideBreakData TideBreak(SkillTarget target)
    {
        TideBreakData data = ScriptableObject.CreateInstance<TideBreakData>(); created.Add(data);
        data.abilityName = "Regression Tide Break"; data.targetType = target;
        return data;
    }

    private void Plan(PlannedAction hero, PlannedAction enemy)
    {
        battle.StartBattle(); battle.AdvancePhase();
        // Public input can auto-confirm after the final ally. Set the same
        // action dictionaries directly to test precise simultaneous pairings.
        Actions("selectedPlayerActions")[hero.Actor] = hero;
        foreach (CombatUnit ally in battle.AllyUnits)
            if (!Actions("selectedPlayerActions").ContainsKey(ally))
                Actions("selectedPlayerActions")[ally] = new PlannedAction(CombatActionType.Defend, ally, null);
        Actions("enemyPlannedActions")[enemy.Actor] = enemy;
        battle.AdvancePhase();
    }

    private void Step() { SetField("actionStepTimer", 0f); Call("HandleActionExecution"); }
    private Dictionary<CombatUnit, PlannedAction> Actions(string name)
        => (Dictionary<CombatUnit, PlannedAction>)typeof(BattleManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
    private void SetField(string name, object value)
        => typeof(BattleManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, value);
    private object Call(string name, params object[] args)
        => typeof(BattleManager).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, args);
}
#endif
