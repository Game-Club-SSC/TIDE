using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class CritStatsTest : MonoBehaviour
{
    [ContextMenu("Run Crit Stats Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Crit Stats Tests ===");

        TestHeroDataHasCritFields();
        TestEnemyDataHasCritFields();
        TestHeroDataCritFieldsClamped();
        TestEnemyDataCritFieldsClamped();

        Debug.Log("=== All Crit Stats Tests Passed ===");
    }

    private void TestHeroDataHasCritFields()
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        try
        {
            Assert.IsTrue(hero.baseCritRate >= 0f && hero.baseCritRate <= 1f, "HeroData.baseCritRate should default within [0,1].");
            Assert.GreaterOrEqual(hero.baseCritDamage, 1f, "HeroData.baseCritDamage should default to >= 1 (no penalty).");
        }
        finally
        {
            Object.DestroyImmediate(hero);
        }
    }

    private void TestEnemyDataHasCritFields()
    {
        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            Assert.IsTrue(enemy.baseCritRate >= 0f && enemy.baseCritRate <= 1f, "EnemyData.baseCritRate should default within [0,1].");
            Assert.GreaterOrEqual(enemy.baseCritDamage, 1f, "EnemyData.baseCritDamage should default to >= 1 (no penalty).");
        }
        finally
        {
            Object.DestroyImmediate(enemy);
        }
    }

    private void TestHeroDataCritFieldsClamped()
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        try
        {
            hero.baseCritRate = 5f;
            Assert.AreEqual(1f, hero.baseCritRate, "HeroData.baseCritRate should be capped at 1.");
            hero.baseCritRate = -1f;
            Assert.AreEqual(0f, hero.baseCritRate, "HeroData.baseCritRate should be floored at 0.");
            hero.baseCritDamage = -1f;
            Assert.AreEqual(0f, hero.baseCritDamage, "HeroData.baseCritDamage should be floored at 0.");
        }
        finally
        {
            Object.DestroyImmediate(hero);
        }
    }

    private void TestEnemyDataCritFieldsClamped()
    {
        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            enemy.baseCritRate = 5f;
            Assert.AreEqual(1f, enemy.baseCritRate, "EnemyData.baseCritRate should be capped at 1.");
            enemy.baseCritRate = -1f;
            Assert.AreEqual(0f, enemy.baseCritRate, "EnemyData.baseCritRate should be floored at 0.");
            enemy.baseCritDamage = -1f;
            Assert.AreEqual(1f, enemy.baseCritDamage, "EnemyData.baseCritDamage should be floored at 1 (no crit penalty).");
        }
        finally
        {
            Object.DestroyImmediate(enemy);
        }
    }
}
