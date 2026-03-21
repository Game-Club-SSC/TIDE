using UnityEngine;
using NUnit.Framework;

public class EnemyDataVerificationTest : MonoBehaviour
{
    [ContextMenu("Run Enemy Data Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Enemy Data Verification Tests ===");

        TestEnemyDataAssetsLoad();
        TestEnemyDataValidation();
        TestEncounterConfigAssetsLoad();
        TestEncounterConfigValidation();
        TestEnemyCompositionFromEncounterConfig();
        TestEnemyCompositionHasEnemyDataSlots();

        Debug.Log("=== Enemy Data Verification Tests Complete ===");
    }

    private void TestEnemyDataAssetsLoad()
    {
        Debug.Log("Testing enemy data assets load from Resources...");

        string[] enemyIds = { "enemy_imp", "enemy_orc", "enemy_troll", "enemy_sprite", "enemy_wraith", "enemy_golem" };
        for (int i = 0; i < enemyIds.Length; i++)
        {
            EnemyData enemy = Resources.Load<EnemyData>($"EnemyData/{enemyIds[i]}");
            Assert.IsNotNull(enemy, $"EnemyData asset '{enemyIds[i]}' should load from Resources/EnemyData/");
            Assert.AreEqual(enemyIds[i], enemy.enemyId, $"EnemyData '{enemyIds[i]}' enemyId should match filename");
        }

        Debug.Log("  All 6 enemy data assets loaded successfully.");
    }

    private void TestEnemyDataValidation()
    {
        Debug.Log("Testing enemy data validation...");

        EnemyData imp = Resources.Load<EnemyData>("EnemyData/enemy_imp");
        Assert.IsNotNull(imp);
        Assert.IsTrue(imp.IsValid(), "Imp should be valid");
        Assert.AreEqual("Imp", imp.displayName);
        Assert.AreEqual(CombatUnit.Element.Fire, imp.element);
        Assert.AreEqual(50, imp.baseMaxHP);
        Assert.AreEqual(12, imp.baseAttack);
        Assert.AreEqual(3, imp.baseDefense);
        Assert.AreEqual(14, imp.baseSpeed);

        EnemyData golem = Resources.Load<EnemyData>("EnemyData/enemy_golem");
        Assert.IsNotNull(golem);
        Assert.IsTrue(golem.IsValid(), "Golem should be valid");
        Assert.AreEqual("Golem", golem.displayName);
        Assert.AreEqual(CombatUnit.Element.Earth, golem.element);
        Assert.AreEqual(150, golem.baseMaxHP);
        Assert.AreEqual(0, golem.baseMaxMP);
        Assert.AreEqual(15, golem.baseDefense);

        Debug.Log("  Enemy data validation passed.");
    }

    private void TestEncounterConfigAssetsLoad()
    {
        Debug.Log("Testing encounter config assets load from Resources...");

        string[] encounterIds = { "encounter_imp_trio", "encounter_orc_patrol", "encounter_troll_guard", "encounter_wraith_ambush", "encounter_golem_warden" };
        for (int i = 0; i < encounterIds.Length; i++)
        {
            EncounterConfig encounter = Resources.Load<EncounterConfig>($"Encounters/{encounterIds[i]}");
            Assert.IsNotNull(encounter, $"EncounterConfig asset '{encounterIds[i]}' should load from Resources/Encounters/");
        }

        Debug.Log("  All 5 encounter config assets loaded successfully.");
    }

    private void TestEncounterConfigValidation()
    {
        Debug.Log("Testing encounter config validation...");

        EncounterConfig impTrio = Resources.Load<EncounterConfig>("Encounters/encounter_imp_trio");
        Assert.IsNotNull(impTrio);
        Assert.IsTrue(impTrio.IsValid(), "Imp trio encounter should be valid");
        Assert.AreEqual(3, impTrio.EnemyCount, "Imp trio should have 3 enemies");
        Assert.AreEqual("enemy_imp", impTrio.enemies[0].enemyId);
        Assert.AreEqual("enemy_imp", impTrio.enemies[1].enemyId);
        Assert.AreEqual("enemy_imp", impTrio.enemies[2].enemyId);

        EncounterConfig orcPatrol = Resources.Load<EncounterConfig>("Encounters/encounter_orc_patrol");
        Assert.IsNotNull(orcPatrol);
        Assert.IsTrue(orcPatrol.IsValid(), "Orc patrol should be valid");
        Assert.AreEqual(2, orcPatrol.EnemyCount, "Orc patrol should have 2 enemies");

        Debug.Log("  Encounter config validation passed.");
    }

    private void TestEnemyCompositionFromEncounterConfig()
    {
        Debug.Log("Testing EnemyComposition.FromEncounterConfig...");

        EncounterConfig trollGuard = Resources.Load<EncounterConfig>("Encounters/encounter_troll_guard");
        Assert.IsNotNull(trollGuard);

        EnemyComposition comp = EnemyComposition.FromEncounterConfig(trollGuard);
        Assert.IsNotNull(comp);
        Assert.IsTrue(comp.HasEnemyDataSlots, "Composition from EncounterConfig should have enemy data slots");
        Assert.AreEqual(2, comp.Count, "Troll guard should have 2 enemies");
        Assert.IsTrue(comp.IsValidIndex(0));
        Assert.IsTrue(comp.IsValidIndex(1));
        Assert.IsFalse(comp.IsValidIndex(2));

        EnemyData first = comp.GetEnemyData(0);
        Assert.IsNotNull(first);
        Assert.AreEqual("enemy_troll", first.enemyId);

        EnemyData second = comp.GetEnemyData(1);
        Assert.IsNotNull(second);
        Assert.AreEqual("enemy_sprite", second.enemyId);

        Debug.Log("  EnemyComposition.FromEncounterConfig passed.");
    }

    private void TestEnemyCompositionHasEnemyDataSlots()
    {
        Debug.Log("Testing EnemyComposition.HasEnemyDataSlots...");

        EnemyComposition empty = new EnemyComposition();
        Assert.IsFalse(empty.HasEnemyDataSlots, "Empty composition should not have enemy data slots");

        EncounterConfig wraithAmbush = Resources.Load<EncounterConfig>("Encounters/encounter_wraith_ambush");
        EnemyComposition withData = EnemyComposition.FromEncounterConfig(wraithAmbush);
        Assert.IsTrue(withData.HasEnemyDataSlots, "Composition with data should report true");

        Debug.Log("  EnemyComposition.HasEnemyDataSlots passed.");
    }
}
