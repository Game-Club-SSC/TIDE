using System.Collections.Generic;

public static class PlayerCustomizationCatalog
{
    public sealed class PaletteEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public bool IsPremium { get; }
        public int Cost { get; }

        public PaletteEntry(string id, string displayName, bool isPremium, int cost)
        {
            Id = id;
            DisplayName = displayName;
            IsPremium = isPremium;
            Cost = cost;
        }
    }

    private static readonly List<PaletteEntry> Palettes = new List<PaletteEntry>
    {
        new PaletteEntry("palette_default", "Default Tides", false, 0),
        new PaletteEntry("palette_sunset", "Sunset Tide", false, 0),
        new PaletteEntry("palette_forest", "Forest Tide", false, 0),
        new PaletteEntry("palette_storm", "Storm Tide", true, 100),
        new PaletteEntry("palette_cosmic", "Cosmic Tide", true, 150),
        new PaletteEntry("palette_celestial", "Celestial Tide", true, 200),
        new PaletteEntry("palette_obsidian", "Obsidian Tide", true, 180)
    };

    private static readonly HashSet<string> Unlocked = new HashSet<string> { "palette_default" };

    public static int GetDefaultPaletteCount()
    {
        int count = 0;
        for (int i = 0; i < Palettes.Count; i++)
        {
            if (!Palettes[i].IsPremium) count++;
        }
        return count;
    }

    public static int GetPremiumPaletteCount()
    {
        int count = 0;
        for (int i = 0; i < Palettes.Count; i++)
        {
            if (Palettes[i].IsPremium) count++;
        }
        return count;
    }

    public static int GetRequiredCurrencyFor(string paletteId)
    {
        PaletteEntry entry = GetPalette(paletteId);
        return entry != null ? entry.Cost : 0;
    }

    public static bool IsPaletteUnlocked(string paletteId)
    {
        return Unlocked.Contains(paletteId);
    }

    public static bool UnlockPalette(string paletteId)
    {
        if (string.IsNullOrEmpty(paletteId) || !HasPalette(paletteId))
        {
            return false;
        }

        if (Unlocked.Add(paletteId))
        {
            return true;
        }

        return false;
    }

    public static IEnumerable<string> GetAllPaletteIds()
    {
        for (int i = 0; i < Palettes.Count; i++)
        {
            yield return Palettes[i].Id;
        }
    }

    public static PaletteEntry GetPalette(string paletteId)
    {
        if (string.IsNullOrEmpty(paletteId))
        {
            return null;
        }

        for (int i = 0; i < Palettes.Count; i++)
        {
            if (string.Equals(Palettes[i].Id, paletteId, System.StringComparison.Ordinal))
            {
                return Palettes[i];
            }
        }

        return null;
    }

    public static bool HasPalette(string paletteId)
    {
        return GetPalette(paletteId) != null;
    }

    public static void ResetForDebug()
    {
        Unlocked.Clear();
        Unlocked.Add("palette_default");
    }
}
