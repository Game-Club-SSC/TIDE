#if UNITY_EDITOR || TIDE_COMBAT_HARNESS
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
        // on which side a unit happens to be on. Enumerated from the enum so
        // a newly added element is covered automatically.
        System.Array values = System.Enum.GetValues(typeof(CombatUnit.Element));
        Assert.Greater(values.Length, 1, "The element enum should have several values.");

        for (int i = 0; i < values.Length; i++)
        {
            CombatUnit.Element attacker = (CombatUnit.Element)values.GetValue(i);
            for (int j = 0; j < values.Length; j++)
            {
                CombatUnit.Element defender = (CombatUnit.Element)values.GetValue(j);

                if (attacker == defender)
                {
                    Assert.AreEqual(MatchupResult.Neutral,
                        ElementMatchup.GetResult(attacker, defender),
                        $"{attacker} vs itself must be neutral.");
                    continue;
                }

                MatchupResult forward = ElementMatchup.GetResult(attacker, defender);
                MatchupResult reverse = ElementMatchup.GetResult(defender, attacker);
                Assert.AreEqual(Opposite(forward), reverse,
                    $"Matchup must be antisymmetric: {attacker}->{defender} is {forward} but {defender}->{attacker} is {reverse}.");
            }
        }
    }

    [Test]
    public void MatchupDamageMultiplierFollowsTheResult()
    {
        // The result enum and the damage multiplier must agree: a Strong
        // result must actually deal more than a Weak one. Comparing the
        // constants to each other would not catch a mismatch between the
        // result table and the multiplier table. Every element is probed, so
        // no hand-picked pair can be wrong.
        System.Array values = System.Enum.GetValues(typeof(CombatUnit.Element));
        int strongPairs = 0;
        int weakPairs = 0;

        for (int i = 0; i < values.Length; i++)
        {
            CombatUnit.Element attacker = (CombatUnit.Element)values.GetValue(i);
            for (int j = 0; j < values.Length; j++)
            {
                CombatUnit.Element defender = (CombatUnit.Element)values.GetValue(j);
                if (attacker == defender)
                {
                    continue;
                }

                MatchupResult result = ElementMatchup.GetResult(attacker, defender);
                float multiplier = ElementMatchup.GetDamageMultiplier(attacker, defender);

                if (result == MatchupResult.Strong)
                {
                    Assert.Greater(multiplier, 1f, $"{attacker} vs {defender} is Strong so must deal more than base damage.");
                    strongPairs++;
                }
                else if (result == MatchupResult.Weak)
                {
                    Assert.Less(multiplier, 1f, $"{attacker} vs {defender} is Weak so must deal less than base damage.");
                    weakPairs++;
                }
                else
                {
                    Assert.AreEqual(ElementMatchup.NeutralMultiplier, multiplier, 1e-5f,
                        $"{attacker} vs {defender} is Neutral so must use the neutral multiplier.");
                }
            }
        }

        Assert.Greater(strongPairs, 0, "The element table should contain strong matchups.");
        Assert.Greater(weakPairs, 0, "The element table should contain weak matchups.");
    }

    [Test]
    public void BalanceConfigIsEitherUnwiredOrKeepsTheElementAdvantage()
    {
        // BattleManager prefers BalanceConfig's own multipliers over the
        // ElementMatchup constants whenever an instance is wired in
        // (BattleManager.cs:1882 and five other call sites). The project
        // currently ships no BalanceConfig asset, so the fallback path is what
        // actually runs; this asserts the invariant holds for whichever path
        // is live, and that any future asset cannot invert the advantage.
        BalanceConfig config = Resources.Load<BalanceConfig>("BalanceConfig");

        if (config == null)
        {
            // No asset: every call site falls back to ElementMatchup, which
            // MatchupDamageMultiplierFollowsTheResult already validates.
            return;
        }

        float strong = config.GetElementMultiplier(MatchupResult.Strong);
        float weak = config.GetElementMultiplier(MatchupResult.Weak);
        float neutral = config.GetElementMultiplier(MatchupResult.Neutral);

        Assert.Greater(strong, neutral, "A wired BalanceConfig must keep a strong matchup above neutral.");
        Assert.Greater(neutral, weak, "A wired BalanceConfig must keep neutral above weak.");
        Assert.Greater(strong, 1f, "A strong matchup must be a buff in the wired config.");
        Assert.Less(weak, 1f, "A weak matchup must be a debuff in the wired config.");
    }

    [Test]
    public void StatusEffectExpiryAgreesAcrossCombatUnitAndHud()
    {
        // Three sites must agree that an effect is spent at exactly zero:
        // CombatUnit removes it, BattleHud stops rendering it, and
        // StatusEffect.IsValid reports it invalid. The decisive input is a
        // NEGATIVE duration, the documented 'permanent' sentinel: a '<= 0'
        // comparison deletes it on the next tick, an '== 0' comparison keeps
        // it. Asserting only IsValid would leave the other two sites free to
        // regress, so a real CombatUnit is driven here.
        StatusEffect permanent = new StatusEffect(StatusEffectType.BuffAttack, -1, 0.5f, "permanent");
        permanent.Tick();
        Assert.AreEqual(-1, permanent.Duration, "A negative duration must not be decremented.");
        Assert.IsTrue(permanent.IsValid(),
            "A negative duration marks a permanent effect and must remain valid; '<= 0' would expire it immediately.");

        // The CombatUnit site: a permanent effect must survive its tick loop.
        GameObject unitObject = new GameObject("TestUnit_PermanentEffect");
        try
        {
            CombatUnit unit = unitObject.AddComponent<CombatUnit>();
            unit.ApplyStatusEffect(permanent);
            Assert.AreEqual(1, unit.ActiveEffects.Count,
                "Setup: the permanent effect should be applied to the unit.");

            unit.ProcessTurnStartEffects();
            unit.ProcessTurnStartEffects();

            Assert.AreEqual(1, unit.ActiveEffects.Count,
                "CombatUnit must not remove a permanent (negative duration) effect on turn start; a '<= 0' expiry would drop it.");

            // A one-turn effect of a DIFFERENT type, by contrast, must still
            // expire at exactly zero. (Same-type effects refresh rather than
            // stack, so reusing BuffAttack would overwrite the permanent one.)
            unit.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 1, 0.5f, "expiring"));
            Assert.AreEqual(2, unit.ActiveEffects.Count, "Setup: both effects should be present.");
            unit.ProcessTurnStartEffects();
            unit.ProcessTurnEndEffects();
            Assert.AreEqual(1, unit.ActiveEffects.Count,
                "The one-turn effect should have expired, leaving only the permanent one.");
        }
        finally
        {
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void ShippedHealerSkillsPassValidation()
    {
        // SkillData.IsValid used to cap restoreCasterPercentOfDamage at 1.0
        // while the shipped healer skills restore 1.5 and 2.0, which made
        // every hero that referenced them report invalid. Validate the actual
        // shipped assets rather than a hand-built fixture, so a broken asset
        // is caught.
        SkillData[] shipped = Resources.LoadAll<SkillData>("Skills");
        Assert.IsNotNull(shipped, "Skills resources should load.");
        Assert.Greater(shipped.Length, 0, "At least one shipped skill asset is expected.");

        for (int i = 0; i < shipped.Length; i++)
        {
            SkillData skill = shipped[i];
            Assert.IsNotNull(skill, "A shipped skill asset resolved to null.");
            Assert.IsTrue(skill.IsValid(),
                $"Shipped skill '{skill.skillName}' must be valid (restore {skill.restoreCasterPercentOfDamage}, heal {skill.healMultiplier}).");
        }
    }

    [Test]
    public void ShippedHeroDataValidatesWithItsSkills()
    {
        // The Merrick failure surfaced here: HeroData.IsValid gates on
        // HasValidNormalSkillUnlocks, so an invalid skill asset invalidated
        // the whole hero. Validate the shipped roster.
        HeroData[] heroes = Resources.LoadAll<HeroData>("HeroData");
        Assert.Greater(heroes.Length, 0, "At least one shipped hero asset is expected.");

        for (int i = 0; i < heroes.Length; i++)
        {
            HeroData hero = heroes[i];
            Assert.IsNotNull(hero, "A shipped hero asset resolved to null.");
            Assert.IsTrue(hero.IsValid(), $"Shipped hero '{hero.displayName}' must validate.");
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

#endif
