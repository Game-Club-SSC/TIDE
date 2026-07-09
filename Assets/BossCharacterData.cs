using System;
using UnityEngine;

/// <summary>
/// Defines visual specifications for the 6 vice bosses.
/// Each boss has unique visual identity tied to their island theme,
/// element affinity, and narrative mechanic.
/// </summary>
[Serializable]
public class BossCharacterData
{
    [Header("Identity")]
    public string bossId;
    public string displayName;
    public string islandId;
    public CombatUnit.Element element;

    [Header("Colors")]
    public Color primaryColor;
    public Color accentColor;
    public Color glowColor;
    public Color auraColor;

    [Header("Sprite Configuration")]
    public int bossSlotIndex;
    public Vector2 spritePivot;
    public float spritePixelsPerUnit;

    [Header("Scale")]
    public Vector3 explorationScale;
    public Vector3 battleScale;

    [Header("Animation")]
    public BossAnimationState idleState;
    public BossAnimationState attackState;
    public BossAnimationState specialState;
    public BossAnimationState defeatState;

    [Header("Corruption Visual")]
    public Color corruptionTint;
    public float corruptionIntensity;
    public BossCorruptionType corruptionType;
    public float corruptionSpreadRadius;

    public enum BossCorruptionType
    {
        None,
        GoldShimmer,
        MemoryVines,
        MirrorShatter,
        EnchantHaze,
        RageFlames,
        EgoFracture
    }

    [Serializable]
    public class BossAnimationState
    {
        public string stateName;
        public float frameRate;
        public int frameCount;
        public bool loop;

        public BossAnimationState(string name, float fps, int frames, bool shouldLoop)
        {
            stateName = name;
            frameRate = fps;
            frameCount = frames;
            loop = shouldLoop;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Static Boss Definitions
    // ──────────────────────────────────────────────────────────────────

    private static BossCharacterData[] allBosses;

    public static BossCharacterData[] GetAllBosses()
    {
        if (allBosses == null)
        {
            allBosses = new BossCharacterData[]
            {
                CreateGreedBoss(),
                CreateAttachmentBoss(),
                CreateJealousyBoss(),
                CreateLustBoss(),
                CreateAngerBoss(),
                CreateEgoBoss()
            };
        }

        return allBosses;
    }

    public static BossCharacterData GetBoss(string bossId)
    {
        BossCharacterData[] bosses = GetAllBosses();
        for (int i = 0; i < bosses.Length; i++)
        {
            if (string.Equals(bosses[i].bossId, bossId, StringComparison.Ordinal))
            {
                return bosses[i];
            }
        }

        return null;
    }

    public static BossCharacterData GetBossByIsland(string islandId)
    {
        BossCharacterData[] bosses = GetAllBosses();
        for (int i = 0; i < bosses.Length; i++)
        {
            if (string.Equals(bosses[i].islandId, islandId, StringComparison.Ordinal))
            {
                return bosses[i];
            }
        }

        return null;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Factory Methods — One Per Boss
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Greed — Fire boss on island_greed. Gold temple with shimmering corruption.
    /// </summary>
    public static BossCharacterData CreateGreedBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_greed",
            displayName = "Greed",
            islandId = "island_greed",
            element = CombatUnit.Element.Fire,

            primaryColor = new Color(0.85f, 0.75f, 0.2f, 1f),
            accentColor = new Color(1f, 0.92f, 0.5f, 1f),
            glowColor = new Color(1f, 0.85f, 0.3f, 1f),
            auraColor = new Color(0.85f, 0.75f, 0.2f, 0.6f),

            bossSlotIndex = 0,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.2f, 1.2f, 1.2f),
            battleScale = new Vector3(1.4f, 1.4f, 1.4f),

            idleState = new BossAnimationState("Idle", 4f, 6, true),
            attackState = new BossAnimationState("Attack", 10f, 8, false),
            specialState = new BossAnimationState("GoldRain", 8f, 10, false),
            defeatState = new BossAnimationState("Defeat", 6f, 12, false),

            corruptionTint = new Color(0.9f, 0.8f, 0.15f, 1f),
            corruptionIntensity = 0.8f,
            corruptionType = BossCorruptionType.GoldShimmer,
            corruptionSpreadRadius = 2.5f
        };
    }

    /// <summary>
    /// Attachment — Earth boss on island_desire. Garden with vine corruption.
    /// </summary>
    public static BossCharacterData CreateAttachmentBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_attachment",
            displayName = "Attachment",
            islandId = "island_desire",
            element = CombatUnit.Element.Earth,

            primaryColor = new Color(0.4f, 0.7f, 0.4f, 1f),
            accentColor = new Color(0.65f, 0.88f, 0.55f, 1f),
            glowColor = new Color(0.5f, 0.8f, 0.45f, 1f),
            auraColor = new Color(0.4f, 0.7f, 0.4f, 0.5f),

            bossSlotIndex = 1,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.15f, 1.15f, 1.15f),
            battleScale = new Vector3(1.35f, 1.35f, 1.35f),

            idleState = new BossAnimationState("Idle", 3f, 6, true),
            attackState = new BossAnimationState("Attack", 8f, 8, false),
            specialState = new BossAnimationState("VineGrab", 7f, 10, false),
            defeatState = new BossAnimationState("Defeat", 5f, 12, false),

            corruptionTint = new Color(0.3f, 0.5f, 0.25f, 1f),
            corruptionIntensity = 0.65f,
            corruptionType = BossCorruptionType.MemoryVines,
            corruptionSpreadRadius = 2.0f
        };
    }

    /// <summary>
    /// Jealousy — Space boss on island_envy. Mirrors with shatter corruption.
    /// </summary>
    public static BossCharacterData CreateJealousyBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_jealousy",
            displayName = "Jealousy",
            islandId = "island_envy",
            element = CombatUnit.Element.Space,

            primaryColor = new Color(0.6f, 0.3f, 0.7f, 1f),
            accentColor = new Color(0.85f, 0.6f, 0.95f, 1f),
            glowColor = new Color(0.75f, 0.5f, 0.9f, 1f),
            auraColor = new Color(0.6f, 0.3f, 0.7f, 0.55f),

            bossSlotIndex = 2,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.1f, 1.1f, 1.1f),
            battleScale = new Vector3(1.3f, 1.3f, 1.3f),

            idleState = new BossAnimationState("Idle", 5f, 6, true),
            attackState = new BossAnimationState("Attack", 12f, 8, false),
            specialState = new BossAnimationState("MirrorShatter", 10f, 10, false),
            defeatState = new BossAnimationState("Defeat", 6f, 12, false),

            corruptionTint = new Color(0.5f, 0.25f, 0.6f, 1f),
            corruptionIntensity = 0.7f,
            corruptionType = BossCorruptionType.MirrorShatter,
            corruptionSpreadRadius = 1.8f
        };
    }

    /// <summary>
    /// Lust — Water boss on island_lust. Enchanted shrine with haze corruption.
    /// </summary>
    public static BossCharacterData CreateLustBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_lust",
            displayName = "Lust",
            islandId = "island_lust",
            element = CombatUnit.Element.Water,

            primaryColor = new Color(0.9f, 0.4f, 0.5f, 1f),
            accentColor = new Color(1f, 0.65f, 0.72f, 1f),
            glowColor = new Color(0.95f, 0.5f, 0.6f, 1f),
            auraColor = new Color(0.9f, 0.4f, 0.5f, 0.55f),

            bossSlotIndex = 3,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.0f, 1.0f, 1.0f),
            battleScale = new Vector3(1.25f, 1.25f, 1.25f),

            idleState = new BossAnimationState("Idle", 4f, 6, true),
            attackState = new BossAnimationState("Attack", 10f, 8, false),
            specialState = new BossAnimationState("EnchantWave", 8f, 10, false),
            defeatState = new BossAnimationState("Defeat", 5f, 12, false),

            corruptionTint = new Color(0.7f, 0.3f, 0.4f, 1f),
            corruptionIntensity = 0.6f,
            corruptionType = BossCorruptionType.EnchantHaze,
            corruptionSpreadRadius = 2.2f
        };
    }

    /// <summary>
    /// Anger — Fire boss on island_anger. Scorched clearing with flame corruption.
    /// </summary>
    public static BossCharacterData CreateAngerBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_anger",
            displayName = "Anger",
            islandId = "island_anger",
            element = CombatUnit.Element.Fire,

            primaryColor = new Color(0.9f, 0.2f, 0.1f, 1f),
            accentColor = new Color(1f, 0.45f, 0.2f, 1f),
            glowColor = new Color(1f, 0.35f, 0.15f, 1f),
            auraColor = new Color(0.9f, 0.2f, 0.1f, 0.6f),

            bossSlotIndex = 4,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.25f, 1.25f, 1.25f),
            battleScale = new Vector3(1.45f, 1.45f, 1.45f),

            idleState = new BossAnimationState("Idle", 5f, 6, true),
            attackState = new BossAnimationState("Attack", 14f, 8, false),
            specialState = new BossAnimationState("RageBurst", 12f, 10, false),
            defeatState = new BossAnimationState("Defeat", 6f, 12, false),

            corruptionTint = new Color(0.75f, 0.1f, 0.05f, 1f),
            corruptionIntensity = 0.85f,
            corruptionType = BossCorruptionType.RageFlames,
            corruptionSpreadRadius = 2.8f
        };
    }

    /// <summary>
    /// Ego — Air boss on island_ego. Mountain peak with fracture corruption.
    /// </summary>
    public static BossCharacterData CreateEgoBoss()
    {
        return new BossCharacterData
        {
            bossId = "boss_ego",
            displayName = "Ego",
            islandId = "island_ego",
            element = CombatUnit.Element.Air,

            primaryColor = new Color(0.95f, 0.9f, 0.8f, 1f),
            accentColor = new Color(1f, 0.98f, 0.95f, 1f),
            glowColor = new Color(0.98f, 0.95f, 0.88f, 1f),
            auraColor = new Color(0.95f, 0.9f, 0.8f, 0.5f),

            bossSlotIndex = 5,
            spritePivot = new Vector2(0.5f, 0.05f),
            spritePixelsPerUnit = 96f,

            explorationScale = new Vector3(1.3f, 1.3f, 1.3f),
            battleScale = new Vector3(1.5f, 1.5f, 1.5f),

            idleState = new BossAnimationState("Idle", 4f, 6, true),
            attackState = new BossAnimationState("Attack", 11f, 8, false),
            specialState = new BossAnimationState("EgoShatter", 9f, 12, false),
            defeatState = new BossAnimationState("Defeat", 7f, 14, false),

            corruptionTint = new Color(0.8f, 0.75f, 0.65f, 1f),
            corruptionIntensity = 0.9f,
            corruptionType = BossCorruptionType.EgoFracture,
            corruptionSpreadRadius = 3.0f
        };
    }

    // ──────────────────────────────────────────────────────────────────
    //  ElementalCharacterFactory Integration
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the exploration model for this boss using ElementalCharacterFactory.
    /// </summary>
    public Transform BuildExplorationModel(Transform parent, Vector3 offset)
    {
        return ElementalCharacterFactory.BuildExplorationEnemyModel(
            parent, element, offset, explorationScale);
    }

    /// <summary>
    /// Builds the battle model for this boss using ElementalCharacterFactory.
    /// </summary>
    public Transform BuildBattleModel(Transform parent, Vector3 offset)
    {
        return ElementalCharacterFactory.BuildBattleEnemyModel(
            parent, element, offset, battleScale);
    }

    /// <summary>
    /// Gets the sprite from FuturisticSpriteLibrary for this boss.
    /// </summary>
    public Sprite GetBattleSprite()
    {
        return FuturisticSpriteLibrary.GetEnemyBossBattleSprite(element, bossSlotIndex);
    }

    /// <summary>
    /// Gets the overworld sprite from FuturisticSpriteLibrary for this boss.
    /// </summary>
    public Sprite GetOverworldSprite()
    {
        return FuturisticSpriteLibrary.GetEnemyBossOverworldSprite(element, bossSlotIndex);
    }

    /// <summary>
    /// Gets the BossNarrativeMechanic defaults for this boss.
    /// </summary>
    public BossNarrativeMechanic GetNarrativeMechanic()
    {
        return BossNarrativeMechanic.GetDefaultForIsland(islandId);
    }
}
