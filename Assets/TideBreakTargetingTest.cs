#if UNITY_EDITOR || TIDE_COMBAT_HARNESS
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for Tide Break target resolution.
///
/// AllEnemies must resolve to the opposing faction. Enemy support Tide Break
/// data uses the established hostile fallback. Three of the five
/// shipped Tide Break assets are authored AllEnemies, and enemies can reach
/// the Tide Break action whenever the enemy momentum gauge is full, so this
/// was reachable in ordinary play.
/// </summary>
[DisallowMultipleComponent]
public class TideBreakTargetingTest : MonoBehaviour
{
    private GameObject managerObject;
    private readonly System.Collections.Generic.List<GameObject> units = new System.Collections.Generic.List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        Cleanup();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [Test]
    public void ShippedTideBreaksThatAreAllEnemiesAreTheCommonCase()
    {
        // Guards the premise of the tests below. NOTE: these tests are written
        // against ResolveTideBreak directly and hand-build a TideBreakData,
        // because no production path currently assigns TideBreaks to enemies
        // (ApplyEnemyDataToUnit never calls SetTideBreaks). They are therefore
        // forward-looking hardening rather than a fix for a bug players can
        // currently hit; this test records that AllEnemies abilities do exist
        // and would become wrong the moment enemy abilities are authored.
        TideBreakData[] shipped = Resources.LoadAll<TideBreakData>("TideBreakData");
        Assert.Greater(shipped.Length, 0, "Tide Break assets should load.");

        int allEnemies = 0;
        for (int i = 0; i < shipped.Length; i++)
        {
            if (shipped[i] != null && shipped[i].targetType == SkillTarget.AllEnemies)
            {
                allEnemies++;
            }
        }

        Assert.Greater(allEnemies, 0,
            "At least one shipped Tide Break must be AllEnemies; otherwise the actor-side derivation bug is unreachable.");
    }

    [Test]
    public void EnemyAllEnemiesTideBreakHitsOnlyThePlayerParty()
    {
        // AllEnemies is relative to the caster, for both factions.
        BattleManager battle = CreateBattleManager();
        CombatUnit enemy = CreateUnit("EnemyImp", CombatUnit.UnitType.Enemy, 120, 30, 10, CombatUnit.Element.Fire);
        CombatUnit player = CreateUnit("HeroFire", CombatUnit.UnitType.Ally, 200, 25, 15, CombatUnit.Element.Water);
        TideBreakData allEnemies = CreateTideBreak("TB_AllEnemies_Probe", SkillTarget.AllEnemies);
        RegisterUnits(battle, enemy, player);

        int playerHpBefore = player.HP;
        int enemyHpBefore = enemy.HP;

        battle.Momentum.ShiftTowardEnemy(1f);
        InvokeResolveTideBreak(battle, enemy, player, allEnemies);

        Assert.Less(player.HP, playerHpBefore,
            "An enemy AllEnemies Tide Break must damage the opposing player party.");
        Assert.AreEqual(enemyHpBefore, enemy.HP,
            "The enemy Tide Break must never damage its own team.");
    }

    [Test]
    public void EnemyAllySupportTideBreakNeverHealsItsOwnSquad()
    {
        // The defect: the AllAllies branch healed GetAliveUnits(actor.Type),
        // so an enemy spent its charge healing the enemy team.
        BattleManager battle = CreateBattleManager();
        CombatUnit enemy = CreateUnit("EnemyImp", CombatUnit.UnitType.Enemy, 60, 30, 10, CombatUnit.Element.Fire);
        CombatUnit enemyAlly = CreateUnit("EnemyGoblin", CombatUnit.UnitType.Enemy, 60, 20, 10, CombatUnit.Element.Fire);
        CombatUnit player = CreateUnit("HeroFire", CombatUnit.UnitType.Ally, 200, 25, 15, CombatUnit.Element.Water);
        TideBreakData allAllies = CreateTideBreak("TB_AllAllies_Probe", SkillTarget.AllAllies);
        RegisterUnits(battle, enemy, enemyAlly, player);

        int woundedAllyHpBefore = enemyAlly.HP;
        int playerHpBefore = player.HP;

        battle.Momentum.ShiftTowardEnemy(1f);
        InvokeResolveTideBreak(battle, enemy, player, allAllies);

        Assert.AreEqual(woundedAllyHpBefore, enemyAlly.HP,
            "An enemy must not be able to heal its own squad with a Tide Break.");
        Assert.Less(player.HP, playerHpBefore,
            "The unsupported ally-support ability should have been redirected at the opponent.");
    }

    [Test]
    public void EnemySingleAllyTideBreakNeverShieldsItsOwnSquad()
    {
        BattleManager battle = CreateBattleManager();
        CombatUnit enemy = CreateUnit("EnemyImp", CombatUnit.UnitType.Enemy, 100, 30, 10, CombatUnit.Element.Fire);
        CombatUnit enemyAlly = CreateUnit("EnemyGoblin", CombatUnit.UnitType.Enemy, 100, 20, 10, CombatUnit.Element.Fire);
        CombatUnit player = CreateUnit("HeroFire", CombatUnit.UnitType.Ally, 200, 25, 15, CombatUnit.Element.Water);
        TideBreakData singleAlly = CreateTideBreak("TB_SingleAlly_Probe", SkillTarget.SingleAlly);
        RegisterUnits(battle, enemy, enemyAlly, player);

        int playerHpBefore = player.HP;

        battle.Momentum.ShiftTowardEnemy(1f);
        InvokeResolveTideBreak(battle, enemy, enemyAlly, singleAlly);

        Assert.AreEqual(0f, enemyAlly.ShieldHp, 0.001f,
            "An enemy must not be able to shield its own squad with a Tide Break.");
        Assert.AreEqual(0f, enemy.ShieldHp, 0.001f,
            "The enemy itself must not be shielded either.");
        Assert.Less(player.HP, playerHpBefore,
            "The unsupported ally-support ability must be redirected at the opponent, not silently dropped.");
    }

    [Test]
    public void ShieldRefreshNeverShrinksAnActiveShield()
    {
        // ApplyStatusEffect assigned ShieldHp directly, so a weaker re-apply
        // mid-fight silently deleted the remaining buffer.
        CombatUnit unit = CreateUnit("ShieldHero", CombatUnit.UnitType.Ally, 200, 25, 15, CombatUnit.Element.Water);

        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shield, 3, 0.3f, "strong"));
        float strongShield = unit.ShieldHp;
        Assert.Greater(strongShield, 0f, "Setup: the first shield should apply.");

        // Take a hit so the buffer is partly consumed, then re-apply weakly.
        unit.TakeDamage(10);
        float afterHit = unit.ShieldHp;
        Assert.Less(afterHit, strongShield, "Setup: the shield should have absorbed part of the hit.");

        unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shield, 3, 0.05f, "weak"));
        Assert.GreaterOrEqual(unit.ShieldHp, afterHit,
            "A weaker shield refresh must not delete the shield already earned.");
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private BattleManager CreateBattleManager()
    {
        managerObject = new GameObject("TestBattleManager_TideBreak");
        return managerObject.AddComponent<BattleManager>();
    }

    /// <summary>
    /// Registers the units with the battle's rosters, mirroring what
    /// CombatSceneBootstrap does. ResolveTideBreak reads the roster rather
    /// than scanning the scene, so without this it finds nothing to target.
    /// </summary>
    private void RegisterUnits(BattleManager battle, params CombatUnit[] created)
    {
        System.Collections.IList allies = GetRoster(battle, "allyUnits");
        System.Collections.IList enemies = GetRoster(battle, "enemyUnits");
        for (int i = 0; i < created.Length; i++)
        {
            if (created[i] == null)
            {
                continue;
            }

            if (created[i].Type == CombatUnit.UnitType.Ally)
            {
                allies.Add(created[i]);
            }
            else
            {
                enemies.Add(created[i]);
            }
        }
    }

    private static System.Collections.IList GetRoster(BattleManager battle, string fieldName)
    {
        FieldInfo field = typeof(BattleManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"BattleManager.{fieldName} should exist.");
        return (System.Collections.IList)field.GetValue(battle);
    }

    private CombatUnit CreateUnit(string name, CombatUnit.UnitType type, int maxHp, int attack, int defense, CombatUnit.Element element)
    {
        GameObject host = new GameObject("TestUnit_" + name);
        units.Add(host);
        CombatUnit unit = host.AddComponent<CombatUnit>();
        unit.UnitName = name;
        unit.Type = type;
        unit.ElementType = element;
        unit.MaxHP = maxHp;
        unit.HP = maxHp;
        unit.Attack = attack;
        unit.Defense = defense;
        return unit;
    }

    private TideBreakData CreateTideBreak(string name, SkillTarget targetType)
    {
        TideBreakData data = ScriptableObject.CreateInstance<TideBreakData>();
        data.abilityName = name;
        data.targetType = targetType;
        data.damageMultiplier = 1.5f;

        // element is an int (0=None, 1=Fire); leave it at the default so the
        // ability resolves to the actor's element.
        data.element = 0;
        return data;
    }

    private static void InvokeResolveTideBreak(BattleManager battle, CombatUnit actor, CombatUnit target, TideBreakData tideBreak)
    {
        MethodInfo method = typeof(BattleManager).GetMethod(
            "ResolveTideBreak", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "BattleManager.ResolveTideBreak should exist.");
        method.Invoke(battle, new object[] { actor, target, tideBreak });
    }

    private void Cleanup()
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null)
            {
                Object.DestroyImmediate(units[i]);
            }
        }

        units.Clear();

        if (managerObject != null)
        {
            Object.DestroyImmediate(managerObject);
            managerObject = null;
        }
    }
}

#endif
