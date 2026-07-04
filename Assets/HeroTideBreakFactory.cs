using System.Collections.Generic;
using UnityEngine;

public static class HeroTideBreakFactory
{
    private static List<TideBreakData> perHeroCache;

    public static IReadOnlyList<TideBreakData> GetTideBreaksForHero(string heroId, CombatUnit.Element element, int heroLevel)
    {
        IReadOnlyList<TideBreakData> all = GetAllHeroTideBreaks();
        List<TideBreakData> result = new List<TideBreakData>();
        for (int i = 0; i < all.Count; i++)
        {
            TideBreakData data = all[i];
            if (data == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(heroId) && !string.IsNullOrEmpty(data.heroId)
                && !string.Equals(data.heroId, heroId, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (data.element != (int)element && data.element != (int)CombatUnit.Element.None)
            {
                continue;
            }

            if (data.unlockLevel > heroLevel)
            {
                continue;
            }

            result.Add(data);
        }

        return result;
    }

    public static IReadOnlyList<TideBreakData> GetAllHeroTideBreaks()
    {
        if (perHeroCache == null)
        {
            perHeroCache = BuildAll();
        }
        return perHeroCache;
    }

    public static void ClearCache()
    {
        if (perHeroCache != null)
        {
            for (int i = 0; i < perHeroCache.Count; i++)
            {
                if (perHeroCache[i] != null)
                {
                    Object.DestroyImmediate(perHeroCache[i]);
                }
            }
        }
        perHeroCache = null;
    }

    private static List<TideBreakData> BuildAll()
    {
        List<TideBreakData> list = new List<TideBreakData>();

        // Fire hero
        list.Add(Build("hero_fire", "Inferno Cascade", CombatUnit.Element.Fire, 1, SkillTarget.AllEnemies, 2.2f, "A wave of flame that hits all enemies for heavy damage."));
        list.Add(Build("hero_fire", "Pyre Lance", CombatUnit.Element.Fire, 3, SkillTarget.SingleEnemy, 2.6f, "A focused lance of fire that pierces a single target."));

        // Water hero
        list.Add(Build("hero_water", "Tidal Crush", CombatUnit.Element.Water, 1, SkillTarget.AllEnemies, 2.0f, "The tide surges, drowning the opposing party."));
        list.Add(Build("hero_water", "Abyssal Lance", CombatUnit.Element.Water, 3, SkillTarget.SingleEnemy, 2.5f, "A deep-sea lance pierces a single enemy."));

        // Earth hero
        list.Add(Build("hero_earth", "Mountainfall", CombatUnit.Element.Earth, 1, SkillTarget.AllEnemies, 2.3f, "The earth rises to crush the opposition."));
        list.Add(Build("hero_earth", "Boulder Lance", CombatUnit.Element.Earth, 3, SkillTarget.SingleEnemy, 2.6f, "A single boulder rolled to crush one target."));

        // Air hero
        list.Add(Build("hero_air", "Gale Sweep", CombatUnit.Element.Air, 1, SkillTarget.AllEnemies, 1.9f, "A sweeping gale batters all enemies."));
        list.Add(Build("hero_air", "Storm Lance", CombatUnit.Element.Air, 3, SkillTarget.SingleEnemy, 2.4f, "A focused storm bolt for a single target."));

        // Space hero
        list.Add(Build("hero_space", "Void Cascade", CombatUnit.Element.Space, 1, SkillTarget.AllEnemies, 2.4f, "The void tears at all enemies."));
        list.Add(Build("hero_space", "Singularity Lance", CombatUnit.Element.Space, 3, SkillTarget.SingleEnemy, 2.7f, "A singularity collapses on a single enemy."));

        return list;
    }

    private static TideBreakData Build(string heroId, string name, CombatUnit.Element element, int level, SkillTarget target, float multiplier, string description)
    {
        TideBreakData data = ScriptableObject.CreateInstance<TideBreakData>();
        data.heroId = heroId;
        data.abilityName = name;
        data.description = description;
        data.element = (int)element;
        data.unlockLevel = level;
        data.targetType = target;
        data.damageMultiplier = multiplier;
        data.name = $"TB_{heroId}_{name.Replace(' ', '_')}";
        return data;
    }

    public static bool BaselineOk()
    {
        IReadOnlyList<TideBreakData> all = GetAllHeroTideBreaks();
        return all != null && all.Count > 0;
    }
}
