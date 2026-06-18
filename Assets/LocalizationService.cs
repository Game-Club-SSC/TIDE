using System.Collections.Generic;
using UnityEngine;

public static class LocalizationService
{
    public enum Language
    {
        English,
        Spanish
    }

    public static Language CurrentLanguage { get; private set; } = Language.English;

    private static readonly Dictionary<string, Dictionary<Language, string>> LocalizedStrings =
        new Dictionary<string, Dictionary<Language, string>>
        {
            { "ui.play", new Dictionary<Language, string> { { Language.English, "Play" }, { Language.Spanish, "Jugar" } } },
            { "ui.options", new Dictionary<Language, string> { { Language.English, "Options" }, { Language.Spanish, "Opciones" } } },
            { "ui.exit", new Dictionary<Language, string> { { Language.English, "Exit" }, { Language.Spanish, "Salir" } } },
            { "ui.battle", new Dictionary<Language, string> { { Language.English, "Battle" }, { Language.Spanish, "Batalla" } } },
            { "ui.attack", new Dictionary<Language, string> { { Language.English, "Attack" }, { Language.Spanish, "Atacar" } } },
            { "ui.defend", new Dictionary<Language, string> { { Language.English, "Defend" }, { Language.Spanish, "Defender" } } },
            { "ui.skill", new Dictionary<Language, string> { { Language.English, "Skill" }, { Language.Spanish, "Habilidad" } } },
            { "ui.tidebreak", new Dictionary<Language, string> { { Language.English, "Tide Break" }, { Language.Spanish, "Ruptura de Marea" } } },
            { "ui.victory", new Dictionary<Language, string> { { Language.English, "Victory" }, { Language.Spanish, "Victoria" } } },
            { "ui.defeat", new Dictionary<Language, string> { { Language.English, "Defeat" }, { Language.Spanish, "Derrota" } } },
            { "ui.pause", new Dictionary<Language, string> { { Language.English, "Pause" }, { Language.Spanish, "Pausa" } } },
            { "ui.resume", new Dictionary<Language, string> { { Language.English, "Resume" }, { Language.Spanish, "Reanudar" } } },
            { "ui.save", new Dictionary<Language, string> { { Language.English, "Save" }, { Language.Spanish, "Guardar" } } },
            { "ui.load", new Dictionary<Language, string> { { Language.English, "Load" }, { Language.Spanish, "Cargar" } } },
            { "ui.acceptance.title", new Dictionary<Language, string> { { Language.English, "Acceptance" }, { Language.Spanish, "Aceptacion" } } },
            { "ui.endings.good", new Dictionary<Language, string> { { Language.English, "The Tide Holds" }, { Language.Spanish, "La Marea Aguanta" } } },
            { "ui.endings.bad", new Dictionary<Language, string> { { Language.English, "The Tide Breaks" }, { Language.Spanish, "La Marea Se Rompe" } } },
            { "ui.difficulty.story", new Dictionary<Language, string> { { Language.English, "Story" }, { Language.Spanish, "Historia" } } },
            { "ui.difficulty.standard", new Dictionary<Language, string> { { Language.English, "Standard" }, { Language.Spanish, "Estandar" } } },
            { "ui.difficulty.hardcore", new Dictionary<Language, string> { { Language.English, "Hardcore" }, { Language.Spanish, "Dificil" } } }
        };

    public static void SetLanguage(Language language)
    {
        CurrentLanguage = language;
        Debug.Log($"[LocalizationService] Language set to {language}.");
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (LocalizedStrings.TryGetValue(key, out Dictionary<Language, string> translations))
        {
            if (translations.TryGetValue(CurrentLanguage, out string value))
            {
                return value;
            }

            if (translations.TryGetValue(Language.English, out string fallback))
            {
                return fallback;
            }
        }

        return key;
    }

    public static bool HasKey(string key)
    {
        return !string.IsNullOrEmpty(key) && LocalizedStrings.ContainsKey(key);
    }

    public static IEnumerable<string> GetAllKeys()
    {
        return LocalizedStrings.Keys;
    }
}
