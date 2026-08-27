using System.Collections.Generic;
using UnityEngine;

public enum MatchupResult
{
    Strong,
    Weak,
    Neutral
}

public static class ElementMatchup
{
    private static readonly Dictionary<CombatUnit.Element, CombatUnit.Element[]> AdvantageMap =
        new Dictionary<CombatUnit.Element, CombatUnit.Element[]>
    {
        { CombatUnit.Element.Fire, new[] { CombatUnit.Element.Earth, CombatUnit.Element.Air } },
        { CombatUnit.Element.Water, new[] { CombatUnit.Element.Fire, CombatUnit.Element.Space } },
        { CombatUnit.Element.Earth, new[] { CombatUnit.Element.Water, CombatUnit.Element.Space } },
        { CombatUnit.Element.Air, new[] { CombatUnit.Element.Earth, CombatUnit.Element.Water } },
        { CombatUnit.Element.Space, new[] { CombatUnit.Element.Fire, CombatUnit.Element.Air } },
    };

    public const float StrongMultiplier = 1.5f;
    public const float WeakMultiplier = 0.67f;
    public const float NeutralMultiplier = 1.0f;

    public static MatchupResult GetResult(CombatUnit.Element attacker, CombatUnit.Element defender)
    {
        if (!IsDefinedElement(attacker) || !IsDefinedElement(defender))
        {
            return MatchupResult.Neutral;
        }

        if (attacker == CombatUnit.Element.None || defender == CombatUnit.Element.None)
        {
            return MatchupResult.Neutral;
        }

        if (attacker == defender)
        {
            return MatchupResult.Neutral;
        }

        if (AdvantageMap.TryGetValue(attacker, out CombatUnit.Element[] advantages))
        {
            for (int i = 0; i < advantages.Length; i++)
            {
                if (advantages[i] == defender)
                {
                    return MatchupResult.Strong;
                }
            }
        }

        return MatchupResult.Weak;
    }

    private static bool IsDefinedElement(CombatUnit.Element element)
    {
        int numericValue = (int)element;
        return numericValue >= (int)CombatUnit.Element.None
            && numericValue <= (int)CombatUnit.Element.Space;
    }

    public static float GetDamageMultiplier(CombatUnit.Element attacker, CombatUnit.Element defender)
    {
        switch (GetResult(attacker, defender))
        {
            case MatchupResult.Strong:
                return StrongMultiplier;
            case MatchupResult.Weak:
                return WeakMultiplier;
            default:
                return NeutralMultiplier;
        }
    }
}
