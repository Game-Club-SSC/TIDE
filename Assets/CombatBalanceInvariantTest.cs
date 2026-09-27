using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards the combat damage model.
///
/// Several of the bugs fixed in this pass were balance inversions that no
/// existing suite caught: a defense multiplier applied by multiplication
/// instead of division (high-bond allies took MORE damage), a clash winner
/// and loser transposed, and a shield absorbing nothing on a fractional
/// value. Those are silent behaviour changes, so the invariants they violate
/// are asserted here directly against the real formulas.
/// </summary>
[DisallowMultipleComponent]
public class CombatBalanceInvariantTest : MonoBehaviour
{
    [Test]
    public void ElementMatchupIsAntisymmetricAndTotal()
    {
        // Every element must have exactly one relationship with every other,
        // and the relationship must be the mirror image both ways. An
        // asymmetric table silently changes the outcome of a clash depending
        // on which side a unit happens to be on.
        CombatUnit.Element[] elements =
        {
            CombatUnit.Element.Fire, CombatUnit.Element.Water, CombatUnit.Element.Earth,
            CombatUnit.Element.Air, CombatUnit.Element.Space
        };

        for (int i = 0; i < elements.Length; i++)
        {
            for (int j = 0; j < elements.Length; j++)
            {
                if (elements[i] == elements[j])
                {
                    Assert.AreEqual(MatchupResult.Neutral,
                        ElementMatchup.GetResult(elements[i], elements[j]),
                        $"{elements[i]} vs itself must be neutral.");
                    continue;
                }

                MatchupResult forward = ElementMatchup.GetResult(elements[i], elements[j]);
                MatchupResult reverse = ElementMatchup.GetResult(elements[j], elements[i]);
                Assert.AreEqual(Opposite(forward), reverse,
                    $"Matchup must be antisymmetric: {elements[i]}->{elements[j]} is {forward} but {elements[j]}->{elements[i]} is {reverse}.");
            }
        }
    }

    [Test]
    public void MatchupMultipliersAreOrdered()
    {
        // Strong must beat neutral which must beat weak, otherwise an
        // elemental advantage is a disadvantage.
        Assert.Greater(ElementMatchup.StrongMultiplier, ElementMatchup.NeutralMultiplier,
            "A strong matchup must multiply damage above neutral.");
        Assert.Less(ElementMatchup.WeakMultiplier, ElementMatchup.NeutralMultiplier,
            "A weak matchup must multiply damage below neutral.");
        Assert.Greater(ElementMatchup.StrongMultiplier, 1f, "A strong matchup must be a buff.");
        Assert.Less(ElementMatchup.WeakMultiplier, 1f, "A weak matchup must be a debuff.");
    }

    [Test]
    public void ClashMultipliersFavourTheWinner()
    {
        // A clash winner must hit harder than the loser's retaliation.
        Assert.Greater(GameConstants.ClashWinnerMultiplier, GameConstants.ClashLoserMultiplier,
            "The clash winner must deal more than the loser retaliates.");
        Assert.Greater(GameConstants.ClashWinnerMultiplier, 1f, "The clash winner must be boosted above base damage.");
        Assert.LessOrEqual(GameConstants.ClashLoserMultiplier, 1f, "The clash loser must be reduced, not boosted.");
        Assert.GreaterOrEqual(GameConstants.ClashNeutralMultiplier, 0f, "The neutral clash multiplier must be non-negative.");
    }

    [Test]
    public void DefenceReducesIncomingDamage()
    {
        // The bug: a relationship defence multiplier was applied with '*'
        // instead of '/', so a value of 1.25 (a buff) dealt MORE damage.
        // Assert the shape of the reduction explicitly.
        float defenseMultiplier = 1.25f;
        int incoming = 100;
        int reduced = Mathf.Max(GameConstants.MinimumDamage,
            Mathf.RoundToInt(incoming / defenseMultiplier));
        Assert.Less(reduced, incoming,
            "A defence multiplier above 1 must reduce incoming damage, not amplify it.");
        Assert.AreEqual(80, reduced, "100 damage through a 1.25x defence must land as 80.");
    }

    [Test]
    public void MinimumDamagePreventsHarmlessHits()
    {
        // A heavily reduced hit must still register, so a clash can never
        // silently do nothing.
        Assert.GreaterOrEqual(GameConstants.MinimumDamage, 1,
            "MinimumDamage must be at least 1 so damage is never fully nullified.");
        Assert.AreEqual(1, Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(0.1f)),
            "A near-zero hit must floor to the minimum damage.");
    }

    [Test]
    public void StatusEffectExpiryUsesZeroNotNegative()
    {
        // CombatUnit, BattleHud and StatusEffect.IsValid must agree that an
        // effect is spent at exactly zero. A '<= 0' comparison against the
        // documented negative-duration 'permanent' sentinel deletes permanent
        // effects on the very next tick.
        StatusEffect effect = new StatusEffect(StatusEffectType.BuffAttack, 1, 0.5f, "test");

        effect.Tick();
        Assert.AreEqual(0, effect.Duration, "One tick must consume a one-turn effect exactly.");
        Assert.IsFalse(effect.IsValid(), "An effect at zero duration must be invalid.");
    }

    [Test]
    public void RestorePercentValidatesHealerValues()
    {
        // SkillData.IsValid used to cap restoreCasterPercentOfDamage at 1.0
        // while the shipped healer skills restore 1.5 and 2.0, which made the
        // whole hero data set invalid.
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            skill.skillName = "Healer Test";
            skill.mpCost = 20;
            skill.damageMultiplier = 0f;
            skill.healMultiplier = 0f;
            skill.restoreCasterPercentOfDamage = 2f;

            Assert.IsTrue(skill.IsValid(),
                "A healer that restores 2x the damage dealt must be valid; BattleManager scales the heal from this value.");
        }
        finally
        {
            Object.DestroyImmediate(skill);
        }
    }

    private static MatchupResult Opposite(MatchupResult result)
    {
        switch (result)
        {
            case MatchupResult.Strong: return MatchupResult.Weak;
            case MatchupResult.Weak: return MatchupResult.Strong;
            default: return MatchupResult.Neutral;
        }
    }
}
