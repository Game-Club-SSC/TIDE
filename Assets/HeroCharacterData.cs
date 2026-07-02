using System;
using UnityEngine;

/// <summary>
/// Defines visual specifications for the 5 hero characters.
/// Provides static factory methods for each hero with their sprite
/// configuration, animation states, and corruption visual effects.
/// </summary>
[Serializable]
public class HeroCharacterData
{
    [Header("Identity")]
    public string heroId;
    public string displayName;
    public CombatUnit.Element element;

    [Header("Colors")]
    public Color primaryColor;
    public Color accentColor;
    public Color glowColor;

    [Header("Sprite Configuration")]
    public string spriteStyleId;
    public Vector2 spritePivot;
    public float spritePixelsPerUnit;

    [Header("Animation")]
    public AnimationState idleState;
    public AnimationState walkState;
    public AnimationState attackState;

    [Header("Corruption Visual")]
    public Color corruptionTint;
    public float corruptionIntensity;
    public CorruptionEffectType corruptionEffect;

    public enum CorruptionEffectType
    {
        None,
        Glitch,
        ShadowSpread,
        ElementalDistortion,
        VoidCrack
    }

    [Serializable]
    public class AnimationState
    {
        public string stateName;
        public float frameRate;
        public int frameCount;
        public bool loop;

        public AnimationState(string name, float fps, int frames, bool shouldLoop)
        {
            stateName = name;
            frameRate = fps;
            frameCount = frames;
            loop = shouldLoop;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Static Hero Definitions
    // ──────────────────────────────────────────────────────────────────

    private static HeroCharacterData[] allHeroes;

    public static HeroCharacterData[] GetAllHeroes()
    {
        if (allHeroes == null)
        {
            allHeroes = new HeroCharacterData[]
            {
                CreateFreida(),
                CreateBriar(),
                CreateKillian(),
                CreateMerrick(),
                CreateAether()
            };
        }

        return allHeroes;
    }

    public static HeroCharacterData GetHero(string heroId)
    {
        HeroCharacterData[] heroes = GetAllHeroes();
        for (int i = 0; i < heroes.Length; i++)
        {
            if (string.Equals(heroes[i].heroId, heroId, StringComparison.Ordinal))
            {
                return heroes[i];
            }
        }

        return null;
    }

    public static HeroCharacterData GetHeroByElement(CombatUnit.Element element)
    {
        HeroCharacterData[] heroes = GetAllHeroes();
        for (int i = 0; i < heroes.Length; i++)
        {
            if (heroes[i].element == element)
            {
                return heroes[i];
            }
        }

        return null;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Factory Methods — One Per Hero
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Freida — Earth element hero. Sturdy defender with stone-themed visuals.
    /// </summary>
    public static HeroCharacterData CreateFreida()
    {
        return new HeroCharacterData
        {
            heroId = "hero_earth",
            displayName = "Freida",
            element = CombatUnit.Element.Earth,

            primaryColor = new Color(0.39f, 0.67f, 0.34f, 1f),
            accentColor = new Color(0.77f, 0.89f, 0.52f, 1f),
            glowColor = new Color(0.68f, 0.84f, 0.42f, 1f),

            spriteStyleId = "style_earth_aegis",
            spritePivot = new Vector2(0.5f, 0.04f),
            spritePixelsPerUnit = 96f,

            idleState = new AnimationState("Idle", 6f, 4, true),
            walkState = new AnimationState("Walk", 8f, 6, true),
            attackState = new AnimationState("Attack", 12f, 5, false),

            corruptionTint = new Color(0.25f, 0.45f, 0.18f, 1f),
            corruptionIntensity = 0.6f,
            corruptionEffect = CorruptionEffectType.ShadowSpread
        };
    }

    /// <summary>
    /// Briar — Air element hero. Swift scout with wind-themed visuals.
    /// </summary>
    public static HeroCharacterData CreateBriar()
    {
        return new HeroCharacterData
        {
            heroId = "hero_air",
            displayName = "Briar",
            element = CombatUnit.Element.Air,

            primaryColor = new Color(0.73f, 0.86f, 0.96f, 1f),
            accentColor = new Color(0.94f, 0.98f, 1f, 1f),
            glowColor = new Color(0.7f, 0.94f, 1f, 1f),

            spriteStyleId = "style_air_lancer",
            spritePivot = new Vector2(0.5f, 0.04f),
            spritePixelsPerUnit = 96f,

            idleState = new AnimationState("Idle", 7f, 4, true),
            walkState = new AnimationState("Walk", 10f, 6, true),
            attackState = new AnimationState("Attack", 14f, 4, false),

            corruptionTint = new Color(0.55f, 0.65f, 0.75f, 1f),
            corruptionIntensity = 0.5f,
            corruptionEffect = CorruptionEffectType.ElementalDistortion
        };
    }

    /// <summary>
    /// Killian — Fire element hero. Aggressive damage dealer with flame-themed visuals.
    /// </summary>
    public static HeroCharacterData CreateKillian()
    {
        return new HeroCharacterData
        {
            heroId = "hero_fire",
            displayName = "Killian",
            element = CombatUnit.Element.Fire,

            primaryColor = new Color(0.92f, 0.36f, 0.26f, 1f),
            accentColor = new Color(1f, 0.72f, 0.36f, 1f),
            glowColor = new Color(1f, 0.5f, 0.22f, 1f),

            spriteStyleId = "style_fire_vanguard",
            spritePivot = new Vector2(0.5f, 0.04f),
            spritePixelsPerUnit = 96f,

            idleState = new AnimationState("Idle", 8f, 4, true),
            walkState = new AnimationState("Walk", 10f, 6, true),
            attackState = new AnimationState("Attack", 16f, 6, false),

            corruptionTint = new Color(0.7f, 0.15f, 0.08f, 1f),
            corruptionIntensity = 0.75f,
            corruptionEffect = CorruptionEffectType.Glitch
        };
    }

    /// <summary>
    /// Merrick — Water element hero. Balanced fighter with tide-themed visuals.
    /// </summary>
    public static HeroCharacterData CreateMerrick()
    {
        return new HeroCharacterData
        {
            heroId = "hero_water",
            displayName = "Merrick",
            element = CombatUnit.Element.Water,

            primaryColor = new Color(0.28f, 0.61f, 0.95f, 1f),
            accentColor = new Color(0.55f, 0.88f, 1f, 1f),
            glowColor = new Color(0.33f, 0.85f, 1f, 1f),

            spriteStyleId = "style_water_cipher",
            spritePivot = new Vector2(0.5f, 0.04f),
            spritePixelsPerUnit = 96f,

            idleState = new AnimationState("Idle", 5f, 4, true),
            walkState = new AnimationState("Walk", 7f, 6, true),
            attackState = new AnimationState("Attack", 10f, 5, false),

            corruptionTint = new Color(0.15f, 0.35f, 0.55f, 1f),
            corruptionIntensity = 0.55f,
            corruptionEffect = CorruptionEffectType.VoidCrack
        };
    }

    /// <summary>
    /// Aether — Space element hero. Mystic support with void-themed visuals.
    /// </summary>
    public static HeroCharacterData CreateAether()
    {
        return new HeroCharacterData
        {
            heroId = "hero_space",
            displayName = "Aether",
            element = CombatUnit.Element.Space,

            primaryColor = new Color(0.51f, 0.43f, 0.78f, 1f),
            accentColor = new Color(0.76f, 0.72f, 0.98f, 1f),
            glowColor = new Color(0.63f, 0.54f, 0.95f, 1f),

            spriteStyleId = "style_space_sentinel",
            spritePivot = new Vector2(0.5f, 0.04f),
            spritePixelsPerUnit = 96f,

            idleState = new AnimationState("Idle", 5f, 6, true),
            walkState = new AnimationState("Walk", 7f, 6, true),
            attackState = new AnimationState("Attack", 11f, 7, false),

            corruptionTint = new Color(0.3f, 0.2f, 0.5f, 1f),
            corruptionIntensity = 0.7f,
            corruptionEffect = CorruptionEffectType.VoidCrack
        };
    }

    // ──────────────────────────────────────────────────────────────────
    //  ElementalCharacterFactory Integration
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the exploration model for this hero using ElementalCharacterFactory.
    /// </summary>
    public Transform BuildExplorationModel(Transform parent, Vector3 offset, Vector3 scale)
    {
        return ElementalCharacterFactory.BuildExplorationPlayerModel(
            parent, element, primaryColor, accentColor, glowColor, offset, scale);
    }

    /// <summary>
    /// Builds the exploration sprite for this hero using ElementalCharacterFactory.
    /// </summary>
    public Transform BuildExplorationSprite(Transform parent, Vector3 offset, Vector3 scale)
    {
        return ElementalCharacterFactory.BuildExplorationPlayerSprite(
            parent, spriteStyleId, element, primaryColor, accentColor, glowColor, offset, scale);
    }

    /// <summary>
    /// Builds the battle model for this hero using ElementalCharacterFactory.
    /// </summary>
    public Transform BuildBattleModel(Transform parent, Vector3 offset, Vector3 scale)
    {
        return ElementalCharacterFactory.BuildBattleAllyModel(
            parent, element, primaryColor, accentColor, glowColor, offset, scale);
    }
}
