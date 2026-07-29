#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only procedural generator for the 18 island environment tilesets (6 islands × 3 textures).
/// Regenerates Assets/Resources/Sprites/Islands/<sin>_{ground,wall,water}.png
/// so artists and designers can iterate without external tools.
///
/// The output is placeholder-grade; human-authored art can overwrite the PNGs
/// and the runtime convention loader will pick up the replacements automatically.
/// </summary>
public static class TilesetTextureGenerator
{
    private const int Size = 128;
    private const string OutputDir = "Assets/Resources/Sprites/Islands";

    private static class SinRoot
    {
        public const string Greed = "greed";
        public const string Lust = "lust";
        public const string Anger = "anger";
        public const string Desire = "desire";
        public const string Ego = "ego";
        public const string Envy = "envy";
    }

    private readonly struct Palette
    {
        public readonly Color Base;
        public readonly Color Highlight;
        public readonly Color Dark;
        public readonly Color Accent;

        public Palette(Color baseColor, Color highlight, Color dark, Color accent)
        {
            Base = baseColor;
            Highlight = highlight;
            Dark = dark;
            Accent = accent;
        }
    }

    private static readonly Palette[] GroundPalettes = new Palette[6];
    private static readonly Palette[] WallPalettes = new Palette[6];
    private static readonly Palette[] WaterPalettes = new Palette[6];
    private static readonly string[] SinOrder = new string[6];

    [MenuItem("TIDE/Generate Island Tilesets")]
    public static void GenerateAll()
    {
        InitializePalettes();

        if (!Directory.Exists(OutputDir))
        {
            Directory.CreateDirectory(OutputDir);
        }

        for (int i = 0; i < SinOrder.Length; i++)
        {
            string sin = SinOrder[i];
            SaveTexture(sin, "ground", GenerateGround(GroundPalettes[i], sin));
            SaveTexture(sin, "wall", GenerateWall(WallPalettes[i], sin));
            SaveTexture(sin, "water", GenerateWater(WaterPalettes[i], sin));
        }

        AssetDatabase.Refresh();
        Debug.Log($"[TilesetTextureGenerator] Generated {SinOrder.Length * 3} textures in {OutputDir}.");
    }

    private static void InitializePalettes()
    {
        SinOrder[0] = SinRoot.Greed;
        GroundPalettes[0] = new Palette(H(0.62f, 0.50f, 0.20f), H(0.95f, 0.88f, 0.55f), H(0.15f, 0.10f, 0.05f), H(0.80f, 0.75f, 0.45f));
        WallPalettes[0] = new Palette(H(0.50f, 0.40f, 0.15f), H(0.80f, 0.70f, 0.35f), H(0.25f, 0.18f, 0.08f), H(0.65f, 0.55f, 0.25f));
        WaterPalettes[0] = new Palette(H(0.60f, 0.55f, 0.30f), H(0.25f, 0.18f, 0.08f), H(0.15f, 0.10f, 0.05f), H(0.85f, 0.80f, 0.50f));

        SinOrder[1] = SinRoot.Lust;
        GroundPalettes[1] = new Palette(H(0.72f, 0.30f, 0.35f), H(0.95f, 0.70f, 0.65f), H(0.18f, 0.02f, 0.06f), H(0.85f, 0.55f, 0.60f));
        WallPalettes[1] = new Palette(H(0.55f, 0.15f, 0.20f), H(0.85f, 0.40f, 0.45f), H(0.25f, 0.05f, 0.10f), H(0.70f, 0.35f, 0.40f));
        WaterPalettes[1] = new Palette(H(0.80f, 0.35f, 0.45f), H(0.45f, 0.05f, 0.18f), H(0.18f, 0.02f, 0.06f), H(0.95f, 0.65f, 0.70f));

        SinOrder[2] = SinRoot.Anger;
        GroundPalettes[2] = new Palette(H(0.80f, 0.25f, 0.10f), H(1.00f, 0.75f, 0.45f), H(0.20f, 0.02f, 0.02f), H(0.95f, 0.50f, 0.25f));
        WallPalettes[2] = new Palette(H(0.60f, 0.15f, 0.08f), H(0.90f, 0.40f, 0.20f), H(0.30f, 0.04f, 0.04f), H(0.80f, 0.30f, 0.15f));
        WaterPalettes[2] = new Palette(H(0.85f, 0.35f, 0.15f), H(0.40f, 0.05f, 0.05f), H(0.20f, 0.02f, 0.02f), H(1.00f, 0.70f, 0.35f));

        SinOrder[3] = SinRoot.Desire;
        GroundPalettes[3] = new Palette(H(0.45f, 0.35f, 0.52f), H(0.78f, 0.70f, 0.82f), H(0.08f, 0.05f, 0.12f), H(0.65f, 0.55f, 0.70f));
        WallPalettes[3] = new Palette(H(0.35f, 0.25f, 0.40f), H(0.60f, 0.52f, 0.65f), H(0.15f, 0.10f, 0.22f), H(0.50f, 0.42f, 0.58f));
        WaterPalettes[3] = new Palette(H(0.50f, 0.42f, 0.55f), H(0.18f, 0.12f, 0.22f), H(0.08f, 0.05f, 0.12f), H(0.72f, 0.65f, 0.78f));

        SinOrder[4] = SinRoot.Ego;
        GroundPalettes[4] = new Palette(H(0.75f, 0.70f, 0.50f), H(1.00f, 0.98f, 0.90f), H(0.15f, 0.12f, 0.05f), H(0.92f, 0.90f, 0.75f));
        WallPalettes[4] = new Palette(H(0.70f, 0.60f, 0.30f), H(0.92f, 0.85f, 0.55f), H(0.30f, 0.25f, 0.10f), H(0.85f, 0.78f, 0.50f));
        WaterPalettes[4] = new Palette(H(0.75f, 0.72f, 0.55f), H(0.35f, 0.30f, 0.15f), H(0.15f, 0.12f, 0.05f), H(0.95f, 0.92f, 0.75f));

        SinOrder[5] = SinRoot.Envy;
        GroundPalettes[5] = new Palette(H(0.30f, 0.62f, 0.28f), H(0.65f, 0.90f, 0.60f), H(0.05f, 0.12f, 0.04f), H(0.50f, 0.80f, 0.45f));
        WallPalettes[5] = new Palette(H(0.20f, 0.42f, 0.18f), H(0.45f, 0.70f, 0.42f), H(0.08f, 0.20f, 0.06f), H(0.35f, 0.60f, 0.32f));
        WaterPalettes[5] = new Palette(H(0.35f, 0.65f, 0.32f), H(0.10f, 0.25f, 0.08f), H(0.05f, 0.12f, 0.04f), H(0.60f, 0.90f, 0.55f));
    }

    private static Color H(float r, float g, float b)
    {
        return new Color(r, g, b);
    }

    private static void SaveTexture(string sin, string type, Texture2D texture)
    {
        string filename = $"{sin}_{type}.png";
        string path = Path.Combine(OutputDir, filename);
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Debug.Log($"[TilesetTextureGenerator] Wrote {path}");
    }

    private static Texture2D GenerateGround(Palette palette, string seedLabel)
    {
        int seed = seedLabel.GetHashCode();
        Texture2D texture = new Texture2D(Size, Size);
        int[] scales = { 32, 16, 8, 4 };
        float[] weights = { 0.5f, 0.3f, 0.15f, 0.05f };

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float h = FbmNoise(x, y, seed, scales, weights);
                h = 0.35f + h * 0.55f + PeriodicNoise(x, y, 2, seed + 7) * 0.15f;
                Color c = Lerp3(palette.Dark, palette.Base, palette.Highlight, h);
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D GenerateWall(Palette palette, string seedLabel)
    {
        int seed = seedLabel.GetHashCode();
        Texture2D texture = new Texture2D(Size, Size);
        int brickH = 16;
        int halfBrick = brickH / 2;
        Color mortar = new Color(0.22f, 0.20f, 0.18f);

        for (int y = 0; y < Size; y++)
        {
            int row = y / brickH;
            int rowOffset = (row % 2) * halfBrick;
            for (int x = 0; x < Size; x++)
            {
                int bx = (x + rowOffset) % Size;
                bool mortarH = (y % brickH) <= 1;
                bool mortarV = (bx % brickH) <= 1;
                Color c;
                if (mortarH || mortarV)
                {
                    c = mortar;
                }
                else
                {
                    int brickX = bx / brickH;
                    float h = Hash01(brickX, row, seed);
                    float n = PeriodicNoise(x, y, 4, seed + 3);
                    float t = 0.2f + h * 0.35f + n * 0.12f;
                    c = Color.Lerp(palette.Base, palette.Highlight, t);
                }
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D GenerateWater(Palette palette, string seedLabel)
    {
        int seed = seedLabel.GetHashCode();
        Texture2D texture = new Texture2D(Size, Size);
        int wavesY = 3;
        int noiseScale = 16;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float n = PeriodicNoise(x, y, noiseScale, seed);
                float phase = 2f * Mathf.PI * y * wavesY / Size;
                phase += x * 0.08f + n * 1.2f;
                float wave = (Mathf.Sin(phase) + 1f) * 0.5f;

                Color c;
                if (wave > 0.78f)
                {
                    c = Color.Lerp(palette.Base, palette.Accent, (wave - 0.78f) / 0.22f);
                }
                else
                {
                    c = Color.Lerp(palette.Highlight, palette.Base, wave);
                }
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        return texture;
    }

    private static float FbmNoise(int x, int y, int seed, int[] scales, float[] weights)
    {
        float total = 0f;
        float wsum = 0f;
        for (int i = 0; i < scales.Length; i++)
        {
            total += PeriodicNoise(x, y, scales[i], seed + i) * weights[i];
            wsum += weights[i];
        }
        return wsum > 0f ? total / wsum : 0.5f;
    }

    private static float PeriodicNoise(int x, int y, int scale, int seed)
    {
        if (scale <= 0)
        {
            return 0.5f;
        }

        int periodX = Size / scale;
        int periodY = Size / scale;
        int cx = (x / scale) % periodX;
        int cy = (y / scale) % periodY;
        float fx = Smooth((x % scale) / (float)scale);
        float fy = Smooth((y % scale) / (float)scale);

        float v00 = Hash01(cx, cy, seed);
        float v10 = Hash01((cx + 1) % periodX, cy, seed);
        float v01 = Hash01(cx, (cy + 1) % periodY, seed);
        float v11 = Hash01((cx + 1) % periodX, (cy + 1) % periodY, seed);

        float v0 = Mathf.Lerp(v00, v10, fx);
        float v1 = Mathf.Lerp(v01, v11, fx);
        return Mathf.Lerp(v0, v1, fy);
    }

    private static float Hash01(int x, int y, int seed)
    {
        uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995u;
        h ^= h >> 15;
        return (h & 0x7FFFFFFFu) / (float)0x7FFFFFFFu;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static Color Lerp3(Color a, Color b, Color c, float t)
    {
        t = Mathf.Clamp01(t);
        if (t < 0.5f)
        {
            return Color.Lerp(a, b, t * 2f);
        }
        return Color.Lerp(b, c, (t - 0.5f) * 2f);
    }
}
#endif
