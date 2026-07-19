using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleFlowTestSuite
{
    private GameObject managerObject;
    private BattleManager manager;
    private GameObject unitsRoot;

    [SetUp]
    public void SetUp()
    {
        managerObject = new GameObject("BattleManager_Verification");
        manager = managerObject.AddComponent<BattleManager>();
        unitsRoot = new GameObject("VerificationUnits");
    }

    [TearDown]
    public void TearDown()
    {
        if (unitsRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(unitsRoot);
        }
        if (managerObject != null)
        {
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void BattleFlowVerification()
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

        CombatUnit nextActor = (CombatUnit)InvokePrivate(manager, "TryGetNextActingUnit");
        Assert.IsNotNull(nextActor, "Queue should produce a valid actor after skipping dead units.");
        Assert.AreEqual("EnemyMid", nextActor.UnitName, "Dead queued units should be skipped before their turn.");

        int enemySlowBefore = enemySlow.HP;
        InvokePrivate(manager, "ResolveAttack", allySlow, enemySlow);
        Assert.Less(enemySlow.HP, enemySlowBefore, "Basic attack should damage the target.");

        enemySlow.DebugHP = 0;
        enemySlow.DebugIsAlive = false;

        int enemySlowHpAfterKill = enemySlow.HP;
        int enemyMidBefore = enemyMid.HP;
        InvokePrivate(manager, "ResolveAttack", allySlow, enemySlow);
        Assert.AreEqual(enemySlowHpAfterKill, enemySlow.HP, "Dead target should not take additional damage.");
        Assert.Less(enemyMid.HP, enemyMidBefore, "Attack should retarget to living enemyMid.");
    }

    [Test]
    public void CombatUnitIgnoresNonPositiveDamage()
    {
        CombatUnit unit = CreateUnit(
            unitsRoot.transform,
            "DamageGuardUnit",
            CombatUnit.UnitType.Ally,
            speed: 10,
            attack: 10,
            defense: 0,
            hp: 50);

        unit.TakeDamage(0);
        unit.TakeDamage(-10);

        Assert.AreEqual(50, unit.HP, "Zero or negative damage must not reduce HP.");
        Assert.IsTrue(unit.IsAlive, "Zero or negative damage must not defeat a unit.");
    }

    [Test]
    public void MomentumIgnoresInvalidInputs()
    {
        MomentumState momentum = new MomentumState();

        momentum.ShiftTowardPlayer(-0.5f);
        momentum.ShiftTowardEnemy(-0.5f);
        momentum.ShiftForAction(null, MatchupResult.Strong);

        Assert.AreEqual(0f, momentum.Value, 0.0001f,
            "Negative shifts and null attackers must not reverse or change momentum.");
    }

    [Test]
    public void TurnStartDamageSkipsDefeatedActor()
    {
        CombatUnit poisoned = CreateUnit(
            unitsRoot.transform,
            "PoisonedActor",
            CombatUnit.UnitType.Ally,
            speed: 20,
            attack: 10,
            defense: 0,
            hp: 1);
        CombatUnit next = CreateUnit(
            unitsRoot.transform,
            "NextActor",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 10,
            defense: 0,
            hp: 50);
        poisoned.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 1, 2f, "Test"));
        manager.RegisterUnit(poisoned);
        manager.RegisterUnit(next);
        InvokePrivate(manager, "BuildTurnQueueFromLivingUnits");

        CombatUnit actor = (CombatUnit)InvokePrivate(manager, "TryGetNextActingUnit");

        Assert.IsFalse(poisoned.IsAlive, "Turn-start poison should defeat the first unit.");
        Assert.AreSame(next, actor, "A unit defeated by turn-start damage must not receive a turn.");
    }

    [Test]
    public void EnemyAllyTargetHealCannotHealPlayerUnit()
    {
        CombatUnit player = CreateUnit(
            unitsRoot.transform,
            "PlayerTarget",
            CombatUnit.UnitType.Ally,
            speed: 10,
            attack: 10,
            defense: 0,
            hp: 100);
        CombatUnit enemyHealer = CreateUnit(
            unitsRoot.transform,
            "EnemyHealer",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 20,
            defense: 0,
            hp: 100);
        enemyHealer.HP = 20;
        player.HP = 20;
        manager.RegisterUnit(player);
        manager.RegisterUnit(enemyHealer);
        SkillData heal = ScriptableObject.CreateInstance<SkillData>();
        heal.skillName = "Enemy Heal";
        heal.target = SkillTarget.SingleAlly;
        heal.healMultiplier = 1f;
        heal.mpCost = 0;
        try
        {
            InvokePrivate(manager, "ResolveSkill", enemyHealer, player, heal);

            Assert.AreEqual(20, player.HP, "Enemy ally-target heals must reject player targets.");
            Assert.Greater(enemyHealer.HP, 20, "An invalid ally target should fall back to the caster.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(heal);
        }
    }

    [Test]
    public void EnemyAllAlliesHealUsesEnemySide()
    {
        CombatUnit player = CreateUnit(
            unitsRoot.transform,
            "PlayerNotHealed",
            CombatUnit.UnitType.Ally,
            speed: 10,
            attack: 10,
            defense: 0,
            hp: 100);
        CombatUnit enemyHealer = CreateUnit(
            unitsRoot.transform,
            "EnemyGroupHealer",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 20,
            defense: 0,
            hp: 100);
        CombatUnit enemyAlly = CreateUnit(
            unitsRoot.transform,
            "EnemyHealAlly",
            CombatUnit.UnitType.Enemy,
            speed: 8,
            attack: 10,
            defense: 0,
            hp: 100);
        player.HP = 20;
        enemyHealer.HP = 20;
        enemyAlly.HP = 20;
        manager.RegisterUnit(player);
        manager.RegisterUnit(enemyHealer);
        manager.RegisterUnit(enemyAlly);
        SkillData heal = ScriptableObject.CreateInstance<SkillData>();
        heal.skillName = "Enemy Group Heal";
        heal.target = SkillTarget.AllAllies;
        heal.healMultiplier = 1f;
        heal.mpCost = 0;
        try
        {
            InvokePrivate(manager, "ResolveSkill", enemyHealer, null, heal);

            Assert.AreEqual(20, player.HP, "Enemy group heals must not heal player units.");
            Assert.Greater(enemyHealer.HP, 20, "Enemy group heals should heal the caster.");
            Assert.Greater(enemyAlly.HP, 20, "Enemy group heals should heal enemy allies.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(heal);
        }
    }

    [Test]
    public void NeutralClashQteRuntimeSuccessShiftsMomentumToPlayer()
    {
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyQteSuccess",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyQteSuccess",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);

        manager.RegisterUnit(ally);
        manager.RegisterUnit(enemy);

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;
        manager.OnNeutralClashQteRequested += (requestedAlly, requestedEnemy) =>
        {
            Assert.AreSame(ally, requestedAlly, "Neutral clash QTE should pass ally participant first.");
            Assert.AreSame(enemy, requestedEnemy, "Neutral clash QTE should pass enemy participant second.");
            return true;
        };

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsTrue(resultData.HasWinner, "QTE-triggered neutral clash should resolve with a winner.");
        Assert.IsTrue(resultData.NeutralQteTriggered, "Neutral clash should report QTE trigger state.");
        Assert.IsTrue(resultData.NeutralQteSuccess, "Runtime success should be recorded.");
        Assert.AreEqual("Runtime", resultData.NeutralQteResolution, "Runtime callback should be marked as the resolution source.");
        Assert.AreSame(ally, resultData.Winner, "QTE success should grant ally advantage.");
        Assert.AreSame(enemy, resultData.Loser, "QTE success should set enemy as loser.");
        Assert.AreEqual(91, ally.HP, "Ally should take loser clash damage (0.5x enemy ATK).");
        Assert.AreEqual(70, enemy.HP, "Enemy should take winner clash damage (1.5x ally ATK).");
        Assert.That(manager.Momentum.Value, Is.EqualTo(0.15f).Within(0.0001f), "QTE success should shift momentum toward player.");
    }

    [Test]
    public void NeutralClashQteRuntimeFailShiftsMomentumToEnemy()
    {
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyQteFail",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Water);
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyQteFail",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Water);

        manager.RegisterUnit(ally);
        manager.RegisterUnit(enemy);

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;
        manager.OnNeutralClashQteRequested += (_, _) => false;

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsTrue(resultData.HasWinner, "QTE-triggered neutral clash should resolve with a winner.");
        Assert.IsTrue(resultData.NeutralQteTriggered, "Neutral clash should report QTE trigger state.");
        Assert.IsFalse(resultData.NeutralQteSuccess, "Runtime fail should be recorded.");
        Assert.AreEqual("Runtime", resultData.NeutralQteResolution, "Runtime callback should be marked as the resolution source.");
        Assert.AreSame(enemy, resultData.Winner, "QTE fail should grant enemy advantage.");
        Assert.AreSame(ally, resultData.Loser, "QTE fail should set ally as loser.");
        Assert.AreEqual(73, ally.HP, "Ally should take winner clash damage (1.5x enemy ATK).");
        Assert.AreEqual(90, enemy.HP, "Enemy should take loser clash damage (0.5x ally ATK).");
        Assert.That(manager.Momentum.Value, Is.EqualTo(-0.15f).Within(0.0001f), "QTE fail should shift momentum toward enemy.");
    }

    [Test]
    public void NeutralClashQteFallbackWithoutRuntimeIsDeterministic()
    {
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyFallback",
            CombatUnit.UnitType.Ally,
            speed: 17,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Earth);
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyFallback",
            CombatUnit.UnitType.Enemy,
            speed: 12,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Earth);

        manager.RegisterUnit(ally);
        manager.RegisterUnit(enemy);

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsTrue(resultData.HasWinner, "Fallback QTE should still resolve to a winner.");
        Assert.IsTrue(resultData.NeutralQteTriggered, "Fallback QTE should be marked as triggered.");
        Assert.IsTrue(resultData.NeutralQteSuccess, "Higher-speed ally should win deterministic fallback.");
        Assert.AreEqual("Fallback", resultData.NeutralQteResolution, "Missing runtime should use deterministic fallback.");
        Assert.AreSame(ally, resultData.Winner, "Fallback winner should be ally due to higher speed.");
        Assert.That(manager.Momentum.Value, Is.EqualTo(0.15f).Within(0.0001f), "Fallback success should still shift momentum toward player.");
    }

    [Test]
    public void NeutralClashQteFallbackTieUsesRegistrationOrder()
    {
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyTieFallback",
            CombatUnit.UnitType.Enemy,
            speed: 12,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Space);
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyTieFallback",
            CombatUnit.UnitType.Ally,
            speed: 12,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Space);

        manager.RegisterUnit(enemy);
        manager.RegisterUnit(ally);

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsTrue(resultData.HasWinner, "Fallback tie should still resolve to a winner.");
        Assert.IsFalse(resultData.NeutralQteSuccess, "Earlier-registered enemy should win fallback tie-break.");
        Assert.AreEqual("Fallback", resultData.NeutralQteResolution, "Missing runtime should use deterministic fallback tie-break.");
        Assert.AreSame(enemy, resultData.Winner, "Fallback tie should resolve to earlier registration order.");
        Assert.That(manager.Momentum.Value, Is.EqualTo(-0.15f).Within(0.0001f), "Fallback tie loss should shift momentum toward enemy.");
    }

    [Test]
    public void NeutralClashQteMissingRuntimeWithFallbackDisabledUsesNeutralResolution()
    {
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyNoFallback",
            CombatUnit.UnitType.Ally,
            speed: 17,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Air);
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyNoFallback",
            CombatUnit.UnitType.Enemy,
            speed: 12,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Air);

        manager.RegisterUnit(ally);
        manager.RegisterUnit(enemy);
        SetPrivateField(manager, "allowNeutralClashQteFallbackWhenRuntimeMissing", false);

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsFalse(resultData.HasWinner, "With fallback disabled and no runtime, clash should remain neutral.");
        Assert.IsFalse(resultData.NeutralQteTriggered, "QTE should report as not triggered when runtime is unavailable and fallback disabled.");
        Assert.AreEqual("RuntimeUnavailable", resultData.NeutralQteResolution, "Resolution should document missing runtime path.");
        Assert.AreEqual(89, ally.HP, "Neutral clash should apply 0.6x enemy damage to ally.");
        Assert.AreEqual(88, enemy.HP, "Neutral clash should apply 0.6x ally damage to enemy.");
        Assert.That(manager.Momentum.Value, Is.EqualTo(0f).Within(0.0001f), "Neutral fallback path should not shift momentum.");
    }

    [Test]
    public void NeutralClashQteNotTriggeredWhenElementIsNone()
    {
        CombatUnit ally = CreateUnit(
            unitsRoot.transform,
            "AllyNoneElement",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.None);
        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyNoneElement",
            CombatUnit.UnitType.Enemy,
            speed: 12,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);

        manager.RegisterUnit(ally);
        manager.RegisterUnit(enemy);

        bool qteRequested = false;
        manager.OnNeutralClashQteRequested += (_, _) =>
        {
            qteRequested = true;
            return true;
        };

        BattleManager.ClashResult? resolved = null;
        manager.OnClashResolved += result => resolved = result;

        InvokePrivate(manager, "ExecuteNeutralClash", ally, enemy);

        Assert.IsFalse(qteRequested, "QTE should not trigger when either unit has Element.None.");
        Assert.IsTrue(resolved.HasValue, "Clash should emit OnClashResolved.");
        BattleManager.ClashResult resultData = resolved.Value;
        Assert.IsFalse(resultData.HasWinner, "Element.None should force neutral resolution.");
        Assert.AreEqual("Ineligible", resultData.NeutralQteResolution, "Resolution should document ineligible trigger conditions.");
        Assert.That(manager.Momentum.Value, Is.EqualTo(0f).Within(0.0001f), "Ineligible neutral clash should not shift momentum.");
    }

    [Test]
    public void EnemyAiPicksAdvantageousSkillAgainstDominantPlayerElement()
    {
        CombatUnit allyFire = CreateUnit(
            unitsRoot.transform,
            "AllyFireDominant",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        CombatUnit allyEarth = CreateUnit(
            unitsRoot.transform,
            "AllyEarthDominant",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        manager.RegisterUnit(allyFire);
        manager.RegisterUnit(allyEarth);

        SkillData waterSkill = ScriptableObject.CreateInstance<SkillData>();
        waterSkill.skillName = "TidalSlam";
        waterSkill.mpCost = 0;
        waterSkill.target = SkillTarget.SingleEnemy;
        waterSkill.damageMultiplier = 1f;
        waterSkill.element = CombatUnit.Element.Water;
        SkillData fireSkill = ScriptableObject.CreateInstance<SkillData>();
        fireSkill.skillName = "EmberBurst";
        fireSkill.mpCost = 0;
        fireSkill.target = SkillTarget.SingleEnemy;
        fireSkill.damageMultiplier = 1f;
        fireSkill.element = CombatUnit.Element.Fire;

        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyHybrid",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Water);
        enemy.SetSkills(new SkillData[] { fireSkill, waterSkill });
        manager.RegisterUnit(enemy);

        PlannedAction action = (PlannedAction)InvokePrivate(manager, "ComputeEnemyAction", enemy);

        Assert.AreEqual(CombatActionType.Skill, action.ActionType,
            "Enemy AI should pick a skill when one is advantageous.");
        Assert.AreSame(waterSkill, action.SelectedSkill,
            "Enemy AI should pick the Water skill because it is Strong against the dominant Fire player element.");
    }

    [Test]
    public void EnemyAiFallsBackToAttackWithoutAdvantageousSkill()
    {
        CombatUnit allyWater = CreateUnit(
            unitsRoot.transform,
            "AllyWaterSolo",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Water);
        manager.RegisterUnit(allyWater);

        SkillData fireSkill = ScriptableObject.CreateInstance<SkillData>();
        fireSkill.skillName = "EmberTap";
        fireSkill.mpCost = 0;
        fireSkill.target = SkillTarget.SingleEnemy;
        fireSkill.damageMultiplier = 1f;
        fireSkill.element = CombatUnit.Element.Fire;

        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyFire",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        enemy.SetSkills(new SkillData[] { fireSkill });
        manager.RegisterUnit(enemy);

        PlannedAction action = (PlannedAction)InvokePrivate(manager, "ComputeEnemyAction", enemy);

        Assert.AreEqual(CombatActionType.Attack, action.ActionType,
            "Enemy AI should fall back to attack when no skill has elemental advantage.");
    }

    [Test]
    public void BossEnemyAiPicksAdvantageousSkillFirst()
    {
        CombatUnit allyFire = CreateUnit(
            unitsRoot.transform,
            "AllyFireBoss",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        manager.RegisterUnit(allyFire);

        SkillData waterSkill = ScriptableObject.CreateInstance<SkillData>();
        waterSkill.skillName = "TidalStrike";
        waterSkill.mpCost = 0;
        waterSkill.target = SkillTarget.SingleEnemy;
        waterSkill.damageMultiplier = 1f;
        waterSkill.element = CombatUnit.Element.Water;

        CombatUnit boss = CreateUnit(
            unitsRoot.transform,
            "EnemyBossHybrid",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 200,
            element: CombatUnit.Element.Water);
        boss.SetSkills(new SkillData[] { waterSkill });
        manager.RegisterUnit(boss);
        SetPrivateField(manager, "isBossEncounter", true);

        PlannedAction action = (PlannedAction)InvokePrivate(manager, "ComputeEnemyAction", boss);

        Assert.AreEqual(CombatActionType.Skill, action.ActionType,
            "Boss AI should pick an advantageous skill when one is available.");
        Assert.AreSame(waterSkill, action.SelectedSkill,
            "Boss AI should prioritize the Water skill against the dominant Fire player element.");

        SetPrivateField(manager, "isBossEncounter", false);
    }

    [Test]
    public void EnemyAiHandlesNoPlayerElementsGracefully()
    {
        CombatUnit allyNone = CreateUnit(
            unitsRoot.transform,
            "AllyNoneAi",
            CombatUnit.UnitType.Ally,
            speed: 14,
            attack: 20,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.None);
        manager.RegisterUnit(allyNone);

        CombatUnit enemy = CreateUnit(
            unitsRoot.transform,
            "EnemyNoPlayerElement",
            CombatUnit.UnitType.Enemy,
            speed: 10,
            attack: 18,
            defense: 0,
            hp: 100,
            element: CombatUnit.Element.Fire);
        SkillData fireSkill = ScriptableObject.CreateInstance<SkillData>();
        fireSkill.skillName = "EmberFallback";
        fireSkill.mpCost = 0;
        fireSkill.target = SkillTarget.SingleEnemy;
        fireSkill.damageMultiplier = 1f;
        fireSkill.element = CombatUnit.Element.Fire;
        enemy.SetSkills(new SkillData[] { fireSkill });
        manager.RegisterUnit(enemy);

        PlannedAction action = (PlannedAction)InvokePrivate(manager, "ComputeEnemyAction", enemy);

        Assert.AreNotEqual(CombatActionType.Skill, action.ActionType,
            "Enemy AI should not pick a skill via advantage path when no player has a known element.");
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

        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist for verification.");

        if (target is BattleManager manager && methodName == "TryGetNextActingUnit")
        {
            object[] invokeArgs = new object[] { null };
            bool result = (bool)method.Invoke(manager, invokeArgs);
            return result ? (CombatUnit)invokeArgs[0] : null;
        }

        return method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        Assert.IsNotNull(target, "Reflection target should not be null.");

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist for verification.");
        field.SetValue(target, value);
    }
}
