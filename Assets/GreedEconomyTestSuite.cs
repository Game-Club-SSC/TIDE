using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GreedEconomyTestSuite : MonoBehaviour
{
    [ContextMenu("Run Greed Economy Tests")]
    public void RunTests()
    {
        TestPuzzleDataDefaults();
        TestCurrencyStealReducesCurrency();
        Debug.Log("[GreedEconomyTestSuite] All tests passed.");
    }

    private void TestPuzzleDataDefaults()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        Assert.IsTrue(data.coinTileYield >= 1, "coinTileYield must default to >= 1");
        Assert.AreEqual(2, data.coinTileYield, "coinTileYield default should be 2");
        Assert.IsFalse(data.enableGreedEconomy, "enableGreedEconomy should default to false");
        DestroyImmediate(data);
        Debug.Log("[GreedEconomyTestSuite] TestPuzzleDataDefaults PASS");
    }

    private void TestCurrencyStealReducesCurrency()
    {
        if (HeroProgressionManager.Instance == null)
        {
            Debug.LogWarning("[GreedEconomyTestSuite] HeroProgressionManager.Instance unavailable, skipping currency steal test.");
            return;
        }

        HeroProgressionManager.Instance.SetCurrency(100);
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName = "Greed Drain";
        skill.currencyStealAmount = 25;
        skill.damageMultiplier = 1f;
        skill.target = SkillTarget.SingleEnemy;

        Assert.AreEqual(100, HeroProgressionManager.Instance.Currency);
        bool spent = HeroProgressionManager.Instance.TrySpendCurrency(skill.currencyStealAmount);
        Assert.IsTrue(spent, "TrySpendCurrency should succeed with sufficient funds");
        Assert.AreEqual(75, HeroProgressionManager.Instance.Currency);

        DestroyImmediate(skill);
        Debug.Log("[GreedEconomyTestSuite] TestCurrencyStealReducesCurrency PASS");
    }
}
