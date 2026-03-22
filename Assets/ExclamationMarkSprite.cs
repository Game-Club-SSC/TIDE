using UnityEngine;

public static class ExclamationMarkSprite
{
    private static Sprite cachedSprite;

    public static Sprite GetSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        Texture2D texture = GenerateTexture();
        cachedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        return cachedSprite;
    }

    private static Texture2D GenerateTexture()
    {
        int width = 32;
        int height = 64;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color transparent = Color.clear;
        Color yellow = new Color(1f, 0.9f, 0.1f, 1f);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparent;
        }

        int barWidth = 8;
        int barHeight = 40;
        int barXStart = (width - barWidth) / 2;
        int barYStart = 20;

        for (int y = barYStart; y < barYStart + barHeight; y++)
        {
            for (int x = barXStart; x < barXStart + barWidth; x++)
            {
                pixels[y * width + x] = yellow;
            }
        }

        int dotSize = 8;
        int dotXStart = (width - dotSize) / 2;
        int dotYStart = 4;

        for (int y = dotYStart; y < dotYStart + dotSize; y++)
        {
            for (int x = dotXStart; x < dotXStart + dotSize; x++)
            {
                pixels[y * width + x] = yellow;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}
