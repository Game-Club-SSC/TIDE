using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves any island identifier (canonical sin name, legacy vice name, or
/// numeric alias) to a stable sin root used for environment art lookup.
///
/// This is intentionally decoupled from <see cref="IslandThemeRegistry"/> so
/// that the in-progress legacy-&gt;canonical ID rename does not block the
/// environment-art pipeline. Replace/remove this helper once the rename is
/// complete everywhere.
/// </summary>
public static class IslandArtResolver
{
    /// <summary>
    /// Stable art asset root for each island. Matches the PNG naming convention
    /// in Assets/Resources/Sprites/Islands/.
    /// </summary>
    public static class SinRoot
    {
        public const string Greed = "greed";
        public const string Lust = "lust";
        public const string Wrath = "wrath";
        public const string Sloth = "sloth";
        public const string Pride = "pride";
        public const string Envy = "envy";
        public const string Gluttony = "gluttony";
    }

    private static readonly Dictionary<string, string> IslandIdToSinRoot =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Canonical sin IDs.
            { "island_lust", SinRoot.Lust },
            { "island_greed", SinRoot.Greed },
            { "island_gluttony", SinRoot.Gluttony },
            { "island_wrath", SinRoot.Wrath },
            { "island_sloth", SinRoot.Sloth },
            { "island_pride", SinRoot.Pride },
            { "island_envy", SinRoot.Envy },

            // Legacy vice IDs still used by DialogueContentLibrary, BossNarrativeMechanic, etc.
            { "island_anger", SinRoot.Wrath },
            { "island_desire", SinRoot.Sloth },
            { "island_ego", SinRoot.Pride },

            // Numeric aliases from IslandThemeRegistry.
            { "island_1", SinRoot.Lust },
            { "island_2", SinRoot.Wrath }, // legacy maps island_2 -> island_anger
            { "island_3", SinRoot.Greed },
            { "island_4", SinRoot.Sloth }, // legacy maps island_4 -> island_desire
            { "island_5", SinRoot.Pride }, // legacy maps island_5 -> island_ego
            { "island_6", SinRoot.Envy },
            { "island_7", SinRoot.Envy },
            { "island_8", SinRoot.Greed },
        };

    /// <summary>
    /// Returns the canonical sin root for an island id, or null if unknown.
    /// Accepts "island_wrath", "island_anger", "island_2", etc.
    /// </summary>
    public static string ToSinRoot(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        if (IslandIdToSinRoot.TryGetValue(islandId, out string sinRoot))
        {
            return sinRoot;
        }

        // If someone passes a raw sin root, return it as-is.
        if (IsKnownSinRoot(islandId))
        {
            return islandId.ToLowerInvariant();
        }

        Debug.LogWarning($"[IslandArtResolver] Unknown island id '{islandId}'. " +
                         "No environment art will be loaded.");
        return null;
    }

    /// <summary>
    /// Loads the ground texture for the given island id using the convention
    /// Assets/Resources/Sprites/Islands/{sin}_ground.png.
    /// </summary>
    public static Texture2D LoadGroundTexture(string islandId)
    {
        return LoadTexture(ToSinRoot(islandId), "ground");
    }

    /// <summary>
    /// Loads the wall texture for the given island id using the convention
    /// Assets/Resources/Sprites/Islands/{sin}_wall.png.
    /// </summary>
    public static Texture2D LoadWallTexture(string islandId)
    {
        return LoadTexture(ToSinRoot(islandId), "wall");
    }

    /// <summary>
    /// Loads the water texture for the given island id using the convention
    /// Assets/Resources/Sprites/Islands/{sin}_water.png.
    /// </summary>
    public static Texture2D LoadWaterTexture(string islandId)
    {
        return LoadTexture(ToSinRoot(islandId), "water");
    }

    public static bool IsKnownSinRoot(string sinRoot)
    {
        if (string.IsNullOrEmpty(sinRoot))
        {
            return false;
        }

        switch (sinRoot.ToLowerInvariant())
        {
            case SinRoot.Greed:
            case SinRoot.Lust:
            case SinRoot.Wrath:
            case SinRoot.Sloth:
            case SinRoot.Pride:
            case SinRoot.Envy:
            case SinRoot.Gluttony:
                return true;
            default:
                return false;
        }
    }

    private static Texture2D LoadTexture(string sinRoot, string textureType)
    {
        if (string.IsNullOrEmpty(sinRoot))
        {
            return null;
        }

        string path = $"Sprites/Islands/{sinRoot}_{textureType}";
        return Resources.Load<Texture2D>(path);
    }
}
