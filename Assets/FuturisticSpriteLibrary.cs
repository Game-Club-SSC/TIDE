using System.Collections.Generic;
using UnityEngine;

public static class FuturisticSpriteLibrary
{
    private const int OverworldSize = 128;
    private const int BattleSize = 144;
    private const int IconSize = 96;
    private const float CharacterPixelsPerUnit = 96f;

    public sealed class PlayerStyleDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public CombatUnit.Element Element { get; }
        public bool IsPremium { get; }
        public int Cost { get; }
        public Color PrimaryColor { get; }
        public Color AccentColor { get; }
        public Color GlowColor { get; }
        public int PatternIndex { get; }

        public PlayerStyleDefinition(
            string id,
            string displayName,
            CombatUnit.Element element,
            bool isPremium,
            int cost,
            Color primaryColor,
            Color accentColor,
            Color glowColor,
            int patternIndex)
        {
            Id = id;
            DisplayName = displayName;
            Element = element;
            IsPremium = isPremium;
            Cost = Mathf.Max(0, cost);
            PrimaryColor = primaryColor;
            AccentColor = accentColor;
            GlowColor = glowColor;
            PatternIndex = patternIndex;
        }
    }

    private static readonly PlayerStyleDefinition[] PlayerStyles =
    {
        new PlayerStyleDefinition(
            "style_fire_vanguard",
            "Fire Vanguard",
            CombatUnit.Element.Fire,
            false,
            0,
            new Color(0.92f, 0.34f, 0.24f, 1f),
            new Color(1f, 0.72f, 0.36f, 1f),
            new Color(1f, 0.5f, 0.22f, 1f),
            0),
        new PlayerStyleDefinition(
            "style_water_cipher",
            "Water Cipher",
            CombatUnit.Element.Water,
            false,
            0,
            new Color(0.24f, 0.56f, 0.94f, 1f),
            new Color(0.55f, 0.88f, 1f, 1f),
            new Color(0.33f, 0.85f, 1f, 1f),
            1),
        new PlayerStyleDefinition(
            "style_earth_aegis",
            "Earth Aegis",
            CombatUnit.Element.Earth,
            false,
            0,
            new Color(0.38f, 0.66f, 0.36f, 1f),
            new Color(0.77f, 0.89f, 0.52f, 1f),
            new Color(0.68f, 0.84f, 0.42f, 1f),
            2),
        new PlayerStyleDefinition(
            "style_air_lancer",
            "Air Lancer",
            CombatUnit.Element.Air,
            false,
            0,
            new Color(0.66f, 0.82f, 0.95f, 1f),
            new Color(0.94f, 0.98f, 1f, 1f),
            new Color(0.7f, 0.94f, 1f, 1f),
            3),
        new PlayerStyleDefinition(
            "style_space_sentinel",
            "Space Sentinel",
            CombatUnit.Element.Space,
            false,
            0,
            new Color(0.38f, 0.36f, 0.62f, 1f),
            new Color(0.76f, 0.72f, 0.98f, 1f),
            new Color(0.63f, 0.54f, 0.95f, 1f),
            4),
        new PlayerStyleDefinition(
            "style_fire_photon",
            "Photon Ember",
            CombatUnit.Element.Fire,
            true,
            140,
            new Color(0.98f, 0.45f, 0.28f, 1f),
            new Color(1f, 0.88f, 0.58f, 1f),
            new Color(1f, 0.62f, 0.24f, 1f),
            5),
        new PlayerStyleDefinition(
            "style_water_glacier",
            "Glacier Nova",
            CombatUnit.Element.Water,
            true,
            160,
            new Color(0.37f, 0.7f, 1f, 1f),
            new Color(0.84f, 0.97f, 1f, 1f),
            new Color(0.58f, 0.92f, 1f, 1f),
            6),
        new PlayerStyleDefinition(
            "style_earth_chrome",
            "Chrome Terra",
            CombatUnit.Element.Earth,
            true,
            170,
            new Color(0.44f, 0.74f, 0.46f, 1f),
            new Color(0.91f, 0.98f, 0.66f, 1f),
            new Color(0.79f, 0.92f, 0.52f, 1f),
            7),
        new PlayerStyleDefinition(
            "style_air_halo",
            "Halo Strider",
            CombatUnit.Element.Air,
            true,
            180,
            new Color(0.73f, 0.9f, 0.99f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(0.82f, 0.97f, 1f, 1f),
            8),
        new PlayerStyleDefinition(
            "style_space_singularity",
            "Singularity Arc",
            CombatUnit.Element.Space,
            true,
            220,
            new Color(0.45f, 0.42f, 0.71f, 1f),
            new Color(0.9f, 0.84f, 1f, 1f),
            new Color(0.8f, 0.66f, 1f, 1f),
            9)
    };

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static string currentMainPlayerStyleId = string.Empty;
    private static Sprite shadowSprite;
    private static Sprite hitEffectSprite;
    private static readonly Dictionary<string, string> heroIdToStyleId = new Dictionary<string, string>
    {
        { "hero_fire", "style_fire_vanguard" },
        { "hero_water", "style_water_cipher" },
        { "hero_earth", "style_earth_aegis" },
        { "hero_air", "style_air_lancer" },
        { "hero_space", "style_space_sentinel" }
    };

    public static IReadOnlyList<PlayerStyleDefinition> GetPlayerStyles()
    {
        return PlayerStyles;
    }

    public static string CurrentMainPlayerStyleId => currentMainPlayerStyleId;

    public static void SetCurrentMainPlayerStyle(string styleId)
    {
        if (TryGetPlayerStyle(styleId, out _))
        {
            currentMainPlayerStyleId = styleId;
        }
    }

    public static bool TryGetPlayerStyle(string styleId, out PlayerStyleDefinition style)
    {
        for (int i = 0; i < PlayerStyles.Length; i++)
        {
            if (PlayerStyles[i].Id == styleId)
            {
                style = PlayerStyles[i];
                return true;
            }
        }

        style = PlayerStyles[0];
        return false;
    }

    public static string GetDefaultStyleIdForElement(CombatUnit.Element element)
    {
        for (int i = 0; i < PlayerStyles.Length; i++)
        {
            if (!PlayerStyles[i].IsPremium && PlayerStyles[i].Element == element)
            {
                return PlayerStyles[i].Id;
            }
        }

        return "style_earth_aegis";
    }

    public static string GetDefaultStyleIdForHero(HeroData hero)
    {
        if (hero != null && !string.IsNullOrEmpty(hero.heroId) && heroIdToStyleId.TryGetValue(hero.heroId, out string styleId))
        {
            return styleId;
        }

        return GetDefaultStyleIdForElement(hero != null ? hero.element : CombatUnit.Element.Earth);
    }

    public static Sprite GetPlayerOverworldSprite(string styleId)
    {
        if (!TryGetPlayerStyle(styleId, out PlayerStyleDefinition style))
        {
            styleId = GetDefaultStyleIdForElement(CombatUnit.Element.Earth);
            TryGetPlayerStyle(styleId, out style);
        }

        string key = $"player_ow_{style.Id}";
        if (spriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = BuildPlayerTexture(style, OverworldSize, false, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.04f),
            CharacterPixelsPerUnit);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetPlayerBattleSprite(string styleId)
    {
        if (!TryGetPlayerStyle(styleId, out PlayerStyleDefinition style))
        {
            styleId = GetDefaultStyleIdForElement(CombatUnit.Element.Earth);
            TryGetPlayerStyle(styleId, out style);
        }

        string key = $"player_battle_{style.Id}";
        if (spriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = BuildPlayerTexture(style, BattleSize, true, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.04f),
            CharacterPixelsPerUnit);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetPlayerStyleIcon(string styleId)
    {
        if (!TryGetPlayerStyle(styleId, out PlayerStyleDefinition style))
        {
            styleId = GetDefaultStyleIdForElement(CombatUnit.Element.Earth);
            TryGetPlayerStyle(styleId, out style);
        }

        string key = $"player_icon_{style.Id}";
        if (spriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = BuildPlayerTexture(style, IconSize, false, true);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            CharacterPixelsPerUnit);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetEnemyOverworldSprite(CombatUnit.Element element)
    {
        string key = $"enemy_ow_{(int)element}";
        if (spriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = BuildEnemyTexture(element, OverworldSize, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.04f),
            CharacterPixelsPerUnit);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetEnemyBattleSprite(CombatUnit.Element element)
    {
        string key = $"enemy_battle_{(int)element}";
        if (spriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = BuildEnemyTexture(element, BattleSize, true);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.05f),
            CharacterPixelsPerUnit);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetShadowSprite()
    {
        if (shadowSprite != null)
        {
            return shadowSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "FuturisticShadow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        float centerX = (size - 1) * 0.5f;
        float centerY = (size - 1) * 0.5f;
        float radiusX = size * 0.42f;
        float radiusY = size * 0.24f;

        for (int y = 0; y < size; y++)
        {
            float dy = (y - centerY) / radiusY;
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centerX) / radiusX;
                float d = dx * dx + dy * dy;
                if (d > 1f)
                {
                    continue;
                }

                float alpha = Mathf.Lerp(0.52f, 0.04f, d);
                pixels[row + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        shadowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            96f);
        shadowSprite.name = "futuristic_shadow_sprite";
        return shadowSprite;
    }

    public static Sprite GetHitEffectSprite()
    {
        if (hitEffectSprite != null)
        {
            return hitEffectSprite;
        }

        const int size = 72;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "FuturisticHitEffect";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        float center = (size - 1) * 0.5f;
        float radius = size * 0.42f;
        float innerRadius = size * 0.17f;
        float radiusSq = radius * radius;
        float innerSq = innerRadius * innerRadius;

        for (int y = 0; y < size; y++)
        {
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSq = dx * dx + dy * dy;
                if (distSq > radiusSq)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(distSq);
                float radial = Mathf.Clamp01(1f - dist / radius);
                float alpha = Mathf.Lerp(0.06f, 0.9f, radial);

                if (distSq <= innerSq)
                {
                    alpha = Mathf.Lerp(alpha, 0f, 0.88f);
                }

                float angle = Mathf.Atan2(dy, dx);
                float spikes = Mathf.Abs(Mathf.Cos(angle * 4f)) * 0.5f + 0.5f;
                alpha *= Mathf.Lerp(0.65f, 1f, spikes);

                pixels[row + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        hitEffectSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            96f);
        hitEffectSprite.name = "futuristic_hit_effect_sprite";
        return hitEffectSprite;
    }

    private static Texture2D BuildPlayerTexture(PlayerStyleDefinition style, int size, bool battleVariant, bool iconVariant)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = $"PlayerTex_{style.Id}_{size}";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = iconVariant ? FilterMode.Bilinear : FilterMode.Point;

        Color32[] pixels = new Color32[size * size];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        Color bodyColor = style.PrimaryColor;
        Color accentColor = style.AccentColor;
        Color glowColor = style.GlowColor;
        Color trimColor = Color.Lerp(style.PrimaryColor, Color.black, 0.52f);
        Color visorColor = Color.Lerp(style.AccentColor, Color.white, 0.35f);

        int cx = size / 2;
        int bodyBottom = Mathf.RoundToInt(size * 0.12f);
        int bodyTop = Mathf.RoundToInt(size * (battleVariant ? 0.75f : 0.72f));
        int bodyHalf = Mathf.RoundToInt(size * (battleVariant ? 0.18f : 0.17f));
        int shoulderHalf = bodyHalf + Mathf.RoundToInt(size * 0.04f);
        int shoulderBottom = bodyTop - Mathf.RoundToInt(size * 0.24f);
        int shoulderTop = bodyTop - Mathf.RoundToInt(size * 0.04f);
        int headRadius = Mathf.RoundToInt(size * 0.105f);
        int headY = bodyTop + Mathf.RoundToInt(size * 0.065f);

        DrawRoundedRect(pixels, size, cx - bodyHalf, bodyBottom, cx + bodyHalf, bodyTop, Mathf.RoundToInt(size * 0.04f), bodyColor);
        DrawRoundedRect(pixels, size, cx - shoulderHalf, shoulderBottom, cx + shoulderHalf, shoulderTop, Mathf.RoundToInt(size * 0.04f), Color.Lerp(bodyColor, Color.white, 0.07f));

        DrawCircle(pixels, size, cx, headY, headRadius, Color.Lerp(bodyColor, Color.white, 0.16f));
        DrawRoundedRect(
            pixels,
            size,
            cx - Mathf.RoundToInt(headRadius * 0.7f),
            headY - Mathf.RoundToInt(headRadius * 0.08f),
            cx + Mathf.RoundToInt(headRadius * 0.7f),
            headY + Mathf.RoundToInt(headRadius * 0.32f),
            Mathf.RoundToInt(size * 0.02f),
            visorColor);

        int plateBottom = bodyBottom + Mathf.RoundToInt(size * 0.18f);
        int plateTop = bodyTop - Mathf.RoundToInt(size * 0.18f);
        DrawRoundedRect(
            pixels,
            size,
            cx - Mathf.RoundToInt(bodyHalf * 0.67f),
            plateBottom,
            cx + Mathf.RoundToInt(bodyHalf * 0.67f),
            plateTop,
            Mathf.RoundToInt(size * 0.025f),
            trimColor);

        DrawRoundedRect(
            pixels,
            size,
            cx - Mathf.RoundToInt(bodyHalf * 0.58f),
            plateBottom + Mathf.RoundToInt(size * 0.03f),
            cx + Mathf.RoundToInt(bodyHalf * 0.58f),
            plateTop - Mathf.RoundToInt(size * 0.03f),
            Mathf.RoundToInt(size * 0.02f),
            Color.Lerp(bodyColor, trimColor, 0.5f));

        int leftLegX = cx - Mathf.RoundToInt(bodyHalf * 0.44f);
        int rightLegX = cx + Mathf.RoundToInt(bodyHalf * 0.44f);
        int legTop = bodyBottom + Mathf.RoundToInt(size * 0.21f);
        DrawRoundedRect(pixels, size, leftLegX - Mathf.RoundToInt(size * 0.05f), bodyBottom - 1, leftLegX + Mathf.RoundToInt(size * 0.05f), legTop, Mathf.RoundToInt(size * 0.02f), Color.Lerp(trimColor, Color.black, 0.2f));
        DrawRoundedRect(pixels, size, rightLegX - Mathf.RoundToInt(size * 0.05f), bodyBottom - 1, rightLegX + Mathf.RoundToInt(size * 0.05f), legTop, Mathf.RoundToInt(size * 0.02f), Color.Lerp(trimColor, Color.black, 0.2f));

        int stripeCount = style.IsPremium ? 4 : 3;
        for (int i = 0; i < stripeCount; i++)
        {
            float t = (i + 1f) / (stripeCount + 1f);
            int y = Mathf.RoundToInt(Mathf.Lerp(plateBottom + 4, plateTop - 4, t));
            DrawLine(
                pixels,
                size,
                cx - Mathf.RoundToInt(bodyHalf * 0.5f),
                y,
                cx + Mathf.RoundToInt(bodyHalf * 0.5f),
                y,
                Color.Lerp(accentColor, glowColor, i / Mathf.Max(1f, stripeCount - 1f)));
        }

        DrawElementMotif(pixels, size, style, cx, Mathf.RoundToInt((plateBottom + plateTop) * 0.5f));

        if (battleVariant)
        {
            int bladeOffset = Mathf.RoundToInt(size * 0.2f);
            DrawDiamond(pixels, size, cx - bladeOffset, shoulderBottom + 3, Mathf.RoundToInt(size * 0.045f), Mathf.RoundToInt(size * 0.08f), glowColor);
            DrawDiamond(pixels, size, cx + bladeOffset, shoulderBottom + 3, Mathf.RoundToInt(size * 0.045f), Mathf.RoundToInt(size * 0.08f), glowColor);
        }

        if (style.IsPremium)
        {
            DrawLine(
                pixels,
                size,
                cx - shoulderHalf,
                shoulderTop - 2,
                cx - Mathf.RoundToInt(bodyHalf * 0.75f),
                bodyTop - 2,
                glowColor);
            DrawLine(
                pixels,
                size,
                cx + shoulderHalf,
                shoulderTop - 2,
                cx + Mathf.RoundToInt(bodyHalf * 0.75f),
                bodyTop - 2,
                glowColor);
        }

        if (iconVariant)
        {
            DrawRoundedRect(
                pixels,
                size,
                Mathf.RoundToInt(size * 0.06f),
                Mathf.RoundToInt(size * 0.06f),
                Mathf.RoundToInt(size * 0.94f),
                Mathf.RoundToInt(size * 0.94f),
                Mathf.RoundToInt(size * 0.08f),
                new Color(0.07f, 0.09f, 0.14f, 0.35f));
            DrawRoundedRectStroke(
                pixels,
                size,
                Mathf.RoundToInt(size * 0.06f),
                Mathf.RoundToInt(size * 0.06f),
                Mathf.RoundToInt(size * 0.94f),
                Mathf.RoundToInt(size * 0.94f),
                Mathf.RoundToInt(size * 0.08f),
                2,
                Color.Lerp(glowColor, Color.white, 0.3f));
        }

        ApplyOutline(pixels, size, new Color(0.03f, 0.05f, 0.08f, 1f));
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D BuildEnemyTexture(CombatUnit.Element element, int size, bool battleVariant)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = $"EnemyTex_{element}_{size}";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;

        Color32[] pixels = new Color32[size * size];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        Color accent = GetElementAccentColor(element);
        Color baseColor = new Color(0.28f, 0.17f, 0.18f, 1f);
        Color armorColor = Color.Lerp(baseColor, accent, 0.18f);
        Color eyeColor = Color.Lerp(accent, Color.white, 0.3f);

        int cx = size / 2;
        int bottom = Mathf.RoundToInt(size * 0.12f);
        int top = Mathf.RoundToInt(size * (battleVariant ? 0.72f : 0.67f));
        int half = Mathf.RoundToInt(size * 0.2f);

        DrawDiamond(pixels, size, cx, Mathf.RoundToInt((bottom + top) * 0.5f), half, Mathf.RoundToInt(size * 0.24f), armorColor);
        DrawRoundedRect(pixels, size, cx - Mathf.RoundToInt(half * 0.58f), bottom - 1, cx + Mathf.RoundToInt(half * 0.58f), bottom + Mathf.RoundToInt(size * 0.16f), Mathf.RoundToInt(size * 0.03f), Color.Lerp(armorColor, Color.black, 0.32f));

        int hornY = top - Mathf.RoundToInt(size * 0.08f);
        DrawDiamond(pixels, size, cx - Mathf.RoundToInt(half * 0.8f), hornY, Mathf.RoundToInt(size * 0.06f), Mathf.RoundToInt(size * 0.11f), Color.Lerp(armorColor, accent, 0.24f));
        DrawDiamond(pixels, size, cx + Mathf.RoundToInt(half * 0.8f), hornY, Mathf.RoundToInt(size * 0.06f), Mathf.RoundToInt(size * 0.11f), Color.Lerp(armorColor, accent, 0.24f));

        int eyeY = Mathf.RoundToInt(bottom + size * 0.34f);
        int eyeOffset = Mathf.RoundToInt(size * 0.07f);
        DrawCircle(pixels, size, cx - eyeOffset, eyeY, Mathf.RoundToInt(size * 0.022f), eyeColor);
        DrawCircle(pixels, size, cx + eyeOffset, eyeY, Mathf.RoundToInt(size * 0.022f), eyeColor);

        DrawLine(
            pixels,
            size,
            cx - Mathf.RoundToInt(half * 0.45f),
            Mathf.RoundToInt(bottom + size * 0.22f),
            cx + Mathf.RoundToInt(half * 0.45f),
            Mathf.RoundToInt(bottom + size * 0.22f),
            Color.Lerp(accent, Color.white, 0.2f));

        if (battleVariant)
        {
            int shoulderY = Mathf.RoundToInt(bottom + size * 0.43f);
            DrawDiamond(pixels, size, cx - Mathf.RoundToInt(half * 1.1f), shoulderY, Mathf.RoundToInt(size * 0.08f), Mathf.RoundToInt(size * 0.12f), Color.Lerp(accent, armorColor, 0.35f));
            DrawDiamond(pixels, size, cx + Mathf.RoundToInt(half * 1.1f), shoulderY, Mathf.RoundToInt(size * 0.08f), Mathf.RoundToInt(size * 0.12f), Color.Lerp(accent, armorColor, 0.35f));
        }

        ApplyOutline(pixels, size, new Color(0.03f, 0.02f, 0.03f, 1f));
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Color GetElementAccentColor(CombatUnit.Element element)
    {
        switch (element)
        {
            case CombatUnit.Element.Fire:
                return new Color(1f, 0.44f, 0.26f, 1f);
            case CombatUnit.Element.Water:
                return new Color(0.35f, 0.79f, 1f, 1f);
            case CombatUnit.Element.Earth:
                return new Color(0.68f, 0.87f, 0.45f, 1f);
            case CombatUnit.Element.Air:
                return new Color(0.86f, 0.97f, 1f, 1f);
            case CombatUnit.Element.Space:
                return new Color(0.76f, 0.64f, 1f, 1f);
            default:
                return new Color(0.94f, 0.56f, 0.33f, 1f);
        }
    }

    private static void DrawElementMotif(Color32[] pixels, int size, PlayerStyleDefinition style, int centerX, int centerY)
    {
        int motifHalf = Mathf.RoundToInt(size * 0.045f);
        Color motifColor = Color.Lerp(style.AccentColor, Color.white, 0.15f);

        switch (style.Element)
        {
            case CombatUnit.Element.Fire:
                DrawDiamond(pixels, size, centerX, centerY + motifHalf / 2, motifHalf, motifHalf + 2, motifColor);
                DrawLine(pixels, size, centerX, centerY + motifHalf + 3, centerX, centerY + motifHalf + 8, style.GlowColor);
                break;
            case CombatUnit.Element.Water:
                DrawCircle(pixels, size, centerX, centerY + 2, motifHalf, motifColor);
                DrawDiamond(pixels, size, centerX, centerY - motifHalf + 2, Mathf.RoundToInt(motifHalf * 0.7f), motifHalf, Color.Lerp(motifColor, style.GlowColor, 0.4f));
                break;
            case CombatUnit.Element.Earth:
                DrawDiamond(pixels, size, centerX, centerY + 1, motifHalf + 1, motifHalf, motifColor);
                DrawLine(pixels, size, centerX - motifHalf - 1, centerY + 1, centerX + motifHalf + 1, centerY + 1, style.GlowColor);
                break;
            case CombatUnit.Element.Air:
                DrawLine(pixels, size, centerX - motifHalf - 1, centerY + 3, centerX + motifHalf + 1, centerY + 3, motifColor);
                DrawLine(pixels, size, centerX - motifHalf + 2, centerY - 1, centerX + motifHalf + 4, centerY - 1, style.GlowColor);
                DrawLine(pixels, size, centerX - motifHalf + 5, centerY - 5, centerX + motifHalf + 7, centerY - 5, style.AccentColor);
                break;
            case CombatUnit.Element.Space:
                DrawLine(pixels, size, centerX - motifHalf, centerY, centerX + motifHalf, centerY, motifColor);
                DrawLine(pixels, size, centerX, centerY - motifHalf, centerX, centerY + motifHalf, motifColor);
                DrawDiamond(pixels, size, centerX, centerY, Mathf.RoundToInt(motifHalf * 0.6f), Mathf.RoundToInt(motifHalf * 0.6f), style.GlowColor);
                break;
            default:
                DrawDiamond(pixels, size, centerX, centerY, motifHalf, motifHalf, motifColor);
                break;
        }

        if (style.PatternIndex >= 5)
        {
            int ringHalf = motifHalf + 6;
            DrawRoundedRectStroke(
                pixels,
                size,
                centerX - ringHalf,
                centerY - ringHalf,
                centerX + ringHalf,
                centerY + ringHalf,
                4,
                1,
                Color.Lerp(style.GlowColor, Color.white, 0.2f));
        }
    }

    private static void DrawRoundedRect(
        Color32[] pixels,
        int size,
        int xMin,
        int yMin,
        int xMax,
        int yMax,
        int radius,
        Color color)
    {
        int clampedRadius = Mathf.Max(0, radius);
        int startX = Mathf.Clamp(xMin, 0, size - 1);
        int endX = Mathf.Clamp(xMax, 0, size - 1);
        int startY = Mathf.Clamp(yMin, 0, size - 1);
        int endY = Mathf.Clamp(yMax, 0, size - 1);

        int radiusSq = clampedRadius * clampedRadius;

        for (int y = startY; y <= endY; y++)
        {
            int row = y * size;
            for (int x = startX; x <= endX; x++)
            {
                bool inCoreX = x >= xMin + clampedRadius && x <= xMax - clampedRadius;
                bool inCoreY = y >= yMin + clampedRadius && y <= yMax - clampedRadius;
                if (inCoreX || inCoreY || clampedRadius == 0)
                {
                    pixels[row + x] = color;
                    continue;
                }

                int cornerX = x < xMin + clampedRadius ? xMin + clampedRadius : xMax - clampedRadius;
                int cornerY = y < yMin + clampedRadius ? yMin + clampedRadius : yMax - clampedRadius;
                int dx = x - cornerX;
                int dy = y - cornerY;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    pixels[row + x] = color;
                }
            }
        }
    }

    private static void DrawRoundedRectStroke(
        Color32[] pixels,
        int size,
        int xMin,
        int yMin,
        int xMax,
        int yMax,
        int radius,
        int thickness,
        Color color)
    {
        int startX = Mathf.Clamp(xMin, 0, size - 1);
        int endX = Mathf.Clamp(xMax, 0, size - 1);
        int startY = Mathf.Clamp(yMin, 0, size - 1);
        int endY = Mathf.Clamp(yMax, 0, size - 1);

        int innerXMin = xMin + thickness;
        int innerYMin = yMin + thickness;
        int innerXMax = xMax - thickness;
        int innerYMax = yMax - thickness;
        int innerRadius = Mathf.Max(0, radius - thickness);

        for (int y = startY; y <= endY; y++)
        {
            int row = y * size;
            for (int x = startX; x <= endX; x++)
            {
                if (!IsPointInsideRoundedRect(x, y, xMin, yMin, xMax, yMax, radius))
                {
                    continue;
                }

                if (IsPointInsideRoundedRect(x, y, innerXMin, innerYMin, innerXMax, innerYMax, innerRadius))
                {
                    continue;
                }

                pixels[row + x] = color;
            }
        }
    }

    private static bool IsPointInsideRoundedRect(int x, int y, int xMin, int yMin, int xMax, int yMax, int radius)
    {
        if (x < xMin || x > xMax || y < yMin || y > yMax)
        {
            return false;
        }

        int clampedRadius = Mathf.Max(0, radius);
        if (clampedRadius == 0)
        {
            return true;
        }

        bool inCoreX = x >= xMin + clampedRadius && x <= xMax - clampedRadius;
        bool inCoreY = y >= yMin + clampedRadius && y <= yMax - clampedRadius;
        if (inCoreX || inCoreY)
        {
            return true;
        }

        int cornerX = x < xMin + clampedRadius ? xMin + clampedRadius : xMax - clampedRadius;
        int cornerY = y < yMin + clampedRadius ? yMin + clampedRadius : yMax - clampedRadius;
        int dx = x - cornerX;
        int dy = y - cornerY;
        return dx * dx + dy * dy <= clampedRadius * clampedRadius;
    }

    private static void DrawCircle(Color32[] pixels, int size, int centerX, int centerY, int radius, Color color)
    {
        int clampedRadius = Mathf.Max(1, radius);
        int radiusSq = clampedRadius * clampedRadius;
        int xMin = Mathf.Clamp(centerX - clampedRadius, 0, size - 1);
        int xMax = Mathf.Clamp(centerX + clampedRadius, 0, size - 1);
        int yMin = Mathf.Clamp(centerY - clampedRadius, 0, size - 1);
        int yMax = Mathf.Clamp(centerY + clampedRadius, 0, size - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            int row = y * size;
            int dy = y - centerY;
            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - centerX;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    pixels[row + x] = color;
                }
            }
        }
    }

    private static void DrawDiamond(Color32[] pixels, int size, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        int rx = Mathf.Max(1, radiusX);
        int ry = Mathf.Max(1, radiusY);

        int xMin = Mathf.Clamp(centerX - rx, 0, size - 1);
        int xMax = Mathf.Clamp(centerX + rx, 0, size - 1);
        int yMin = Mathf.Clamp(centerY - ry, 0, size - 1);
        int yMax = Mathf.Clamp(centerY + ry, 0, size - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            int row = y * size;
            for (int x = xMin; x <= xMax; x++)
            {
                float normalized = Mathf.Abs((x - centerX) / (float)rx) + Mathf.Abs((y - centerY) / (float)ry);
                if (normalized <= 1f)
                {
                    pixels[row + x] = color;
                }
            }
        }
    }

    private static void DrawLine(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        int x = x0;
        int y = y0;
        while (true)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                pixels[y * size + x] = color;
            }

            if (x == x1 && y == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private static void ApplyOutline(Color32[] pixels, int size, Color outlineColor)
    {
        Color32[] outlined = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            outlined[i] = pixels[i];
        }

        for (int y = 1; y < size - 1; y++)
        {
            int row = y * size;
            for (int x = 1; x < size - 1; x++)
            {
                int index = row + x;
                if (pixels[index].a > 0)
                {
                    continue;
                }

                bool touchesOpaque = false;
                for (int oy = -1; oy <= 1 && !touchesOpaque; oy++)
                {
                    int neighborRow = (y + oy) * size;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (pixels[neighborRow + (x + ox)].a > 0)
                        {
                            touchesOpaque = true;
                            break;
                        }
                    }
                }

                if (touchesOpaque)
                {
                    outlined[index] = outlineColor;
                }
            }
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = outlined[i];
        }
    }
}
