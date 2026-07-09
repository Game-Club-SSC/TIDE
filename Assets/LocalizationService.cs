using System.Collections.Generic;
using UnityEngine;

public static class LocalizationService
{
    public enum Language
    {
        English,
        Spanish
    }

    private const string LanguagePrefKey = "Localization_Language";

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
            { "ui.difficulty.hardcore", new Dictionary<Language, string> { { Language.English, "Hardcore" }, { Language.Spanish, "Dificil" } } },

            { "ui.audio.title", new Dictionary<Language, string> { { Language.English, "AUDIO SETTINGS" }, { Language.Spanish, "AJUSTES DE AUDIO" } } },
            { "ui.audio.bgm", new Dictionary<Language, string> { { Language.English, "BGM Volume" }, { Language.Spanish, "Volumen BGM" } } },
            { "ui.audio.sfx", new Dictionary<Language, string> { { Language.English, "SFX Volume" }, { Language.Spanish, "Volumen SFX" } } },
            { "ui.audio.mute", new Dictionary<Language, string> { { Language.English, "Mute All" }, { Language.Spanish, "Silenciar Todo" } } },
            { "ui.audio.language", new Dictionary<Language, string> { { Language.English, "Language" }, { Language.Spanish, "Idioma" } } },
            { "ui.close", new Dictionary<Language, string> { { Language.English, "CLOSE" }, { Language.Spanish, "CERRAR" } } },

            { "ui.minimap", new Dictionary<Language, string> { { Language.English, "MINIMAP" }, { Language.Spanish, "MINIMAPA" } } },
            { "ui.restoration", new Dictionary<Language, string> { { Language.English, "Restoration" }, { Language.Spanish, "Restauracion" } } },
            { "ui.quest_journal", new Dictionary<Language, string> { { Language.English, "Quest Journal" }, { Language.Spanish, "Diario de Misiones" } } },
            { "ui.select_target", new Dictionary<Language, string> { { Language.English, "Select Target" }, { Language.Spanish, "Seleccionar Objetivo" } } },
            { "ui.select_ally", new Dictionary<Language, string> { { Language.English, "Select Ally" }, { Language.Spanish, "Seleccionar Aliado" } } },
            { "ui.select_skill", new Dictionary<Language, string> { { Language.English, "Select Skill" }, { Language.Spanish, "Seleccionar Habilidad" } } },
            { "ui.no_skill", new Dictionary<Language, string> { { Language.English, "No Skill" }, { Language.Spanish, "Sin Habilidad" } } },
            { "ui.clash", new Dictionary<Language, string> { { Language.English, "CLASH!" }, { Language.Spanish, "ENFRENTAMIENTO!" } } },
            { "ui.critical", new Dictionary<Language, string> { { Language.English, "CRITICAL!" }, { Language.Spanish, "CRITICO!" } } },
            { "ui.executing", new Dictionary<Language, string> { { Language.English, "Executing..." }, { Language.Spanish, "Ejecutando..." } } },
            { "ui.retreated", new Dictionary<Language, string> { { Language.English, "RETREATED" }, { Language.Spanish, "RETIRADA" } } },
            { "ui.carrying", new Dictionary<Language, string> { { Language.English, "Carrying" }, { Language.Spanish, "Llevando" } } },
            { "ui.reset", new Dictionary<Language, string> { { Language.English, "Reset" }, { Language.Spanish, "Reiniciar" } } },
            { "ui.press_continue", new Dictionary<Language, string> { { Language.English, "[Press Enter to continue]" }, { Language.Spanish, "[Pulsa Enter para continuar]" } } },
            { "ui.return_title", new Dictionary<Language, string> { { Language.English, "Return to Title" }, { Language.Spanish, "Volver al Titulo" } } },
            { "ui.tide_stabilization", new Dictionary<Language, string> { { Language.English, "TIDE STABILIZATION" }, { Language.Spanish, "ESTABILIZACION DE MAREA" } } },
            { "ui.tide_break_ready", new Dictionary<Language, string> { { Language.English, "TIDE BREAK READY!" }, { Language.Spanish, "RUPTURA DE MAREA LISTA!" } } },
            { "ui.enemy_tb_ready", new Dictionary<Language, string> { { Language.English, "ENEMY TB READY!" }, { Language.Spanish, "ENEMIGO RM LISTO!" } } },

            { "island.greed", new Dictionary<Language, string> { { Language.English, "Island of Greed" }, { Language.Spanish, "Isla de la Codicia" } } },
            { "island.desire", new Dictionary<Language, string> { { Language.English, "Island of Desire" }, { Language.Spanish, "Isla del Deseo" } } },
            { "island.envy", new Dictionary<Language, string> { { Language.English, "Island of Envy" }, { Language.Spanish, "Isla de la Envidia" } } },
            { "island.lust", new Dictionary<Language, string> { { Language.English, "Island of Lust" }, { Language.Spanish, "Isla de la Lujuria" } } },
            { "island.anger", new Dictionary<Language, string> { { Language.English, "Island of Anger" }, { Language.Spanish, "Isla de la Ira" } } },
            { "island.ego", new Dictionary<Language, string> { { Language.English, "Island of Ego" }, { Language.Spanish, "Isla del Ego" } } },

            { "error.null_reference", new Dictionary<Language, string> { { Language.English, "A required component is missing." }, { Language.Spanish, "Falta un componente requerido." } } },
            { "error.save_failed", new Dictionary<Language, string> { { Language.English, "Failed to save game data." }, { Language.Spanish, "Error al guardar los datos." } } },
            { "error.load_failed", new Dictionary<Language, string> { { Language.English, "Failed to load game data." }, { Language.Spanish, "Error al cargar los datos." } } },
            { "error.invalid_action", new Dictionary<Language, string> { { Language.English, "Invalid action." }, { Language.Spanish, "Accion invalida." } } },

            { "tutorial.move", new Dictionary<Language, string> { { Language.English, "Use WASD or arrow keys to move." }, { Language.Spanish, "Usa WASD o flechas para moverte." } } },
            { "tutorial.interact", new Dictionary<Language, string> { { Language.English, "Press E to interact." }, { Language.Spanish, "Pulsa E para interactuar." } } },
            { "tutorial.dash", new Dictionary<Language, string> { { Language.English, "Press Shift to dash." }, { Language.Spanish, "Pulsa Shift para esprintar." } } },
            { "tutorial.combat_basics", new Dictionary<Language, string> { { Language.English, "Choose your action during each turn." }, { Language.Spanish, "Elige tu accion en cada turno." } } },
            { "tutorial.tide_mechanic", new Dictionary<Language, string> { { Language.English, "Match tiles to build Tide momentum." }, { Language.Spanish, "Combina fichas para acumular impulso de marea." } } },

            { "status.burn", new Dictionary<Language, string> { { Language.English, "Burn: Takes damage each turn." }, { Language.Spanish, "Quemadura: Recibe dano cada turno." } } },
            { "status.poison", new Dictionary<Language, string> { { Language.English, "Poison: Takes increasing damage." }, { Language.Spanish, "Veneno: Recibe dano creciente." } } },
            { "status.stun", new Dictionary<Language, string> { { Language.English, "Stun: Cannot act this turn." }, { Language.Spanish, "Aturdimiento: No puede actuar." } } },
            { "status.shield", new Dictionary<Language, string> { { Language.English, "Shield: Absorbs incoming damage." }, { Language.Spanish, "Escudo: Absorbe el dano entrante." } } },
            { "status.haste", new Dictionary<Language, string> { { Language.English, "Haste: Acts faster." }, { Language.Spanish, "Prisa: Actua mas rapido." } } },

            { "npc.greeting.generic", new Dictionary<Language, string> { { Language.English, "Greetings, traveler." }, { Language.Spanish, "Saludos, viajero." } } },
            { "npc.farewell.generic", new Dictionary<Language, string> { { Language.English, "Safe travels." }, { Language.Spanish, "Buen viaje." } } },
            { "npc.merchant.welcome", new Dictionary<Language, string> { { Language.English, "See anything you like?" }, { Language.Spanish, "¿Ves algo que te guste?" } } },

            { "ancient.text.fragment_1", new Dictionary<Language, string> { { Language.English, "The tide rose and swallowed the old world..." }, { Language.Spanish, "La marea subio y se trago el mundo antiguo..." } } },
            { "ancient.text.fragment_2", new Dictionary<Language, string> { { Language.English, "Seven sins, seven islands, one redemption." }, { Language.Spanish, "Siete pecados, siete islas, una redencion." } } },

            { "narrative.act_i.intro", new Dictionary<Language, string> { { Language.English, "The islands drift in silence..." }, { Language.Spanish, "Las islas flotan en silencio..." } } },
            { "narrative.act_ii.intro", new Dictionary<Language, string> { { Language.English, "The tide shifts. A new darkness rises." }, { Language.Spanish, "La marea cambia. Una nueva oscuridad surge." } } },
            { "narrative.act_iii.intro", new Dictionary<Language, string> { { Language.English, "The final act begins." }, { Language.Spanish, "El acto final comienza." } } },

            { "boss.greed.intro", new Dictionary<Language, string> { { Language.English, "You dare covet what is mine?" }, { Language.Spanish, "¿Osas codiciar lo que es mio?" } } },
            { "boss.desire.intro", new Dictionary<Language, string> { { Language.English, "Why bother..." }, { Language.Spanish, "Para que molestarse..." } } },
            { "boss.envy.intro", new Dictionary<Language, string> { { Language.English, "You have what I deserve." }, { Language.Spanish, "Tienes lo que merezco." } } },
            { "boss.lust.intro", new Dictionary<Language, string> { { Language.English, "Come closer..." }, { Language.Spanish, "Acercate..." } } },
            { "boss.anger.intro", new Dictionary<Language, string> { { Language.English, "BURN!" }, { Language.Spanish, "¡ARDE!" } } },
            { "boss.ego.intro", new Dictionary<Language, string> { { Language.English, "Kneel before perfection." }, { Language.Spanish, "Arrodillate ante la perfeccion." } } },
            { "boss.greed.intro", new Dictionary<Language, string> { { Language.English, "I hunger..." }, { Language.Spanish, "Tengo hambre..." } } },

            { "ending.good.text", new Dictionary<Language, string> { { Language.English, "The islands find peace. The tide recedes." }, { Language.Spanish, "Las islas encuentran paz. La marea retrocede." } } },
            { "ending.bad.text", new Dictionary<Language, string> { { Language.English, "The tide consumes all. Darkness remains." }, { Language.Spanish, "La marea lo consume todo. La oscuridad permanece." } } },

            { "gear.weapon.sword", new Dictionary<Language, string> { { Language.English, "Sword" }, { Language.Spanish, "Espada" } } },
            { "gear.weapon.staff", new Dictionary<Language, string> { { Language.English, "Staff" }, { Language.Spanish, "Baston" } } },
            { "gear.weapon.bow", new Dictionary<Language, string> { { Language.English, "Bow" }, { Language.Spanish, "Arco" } } },
            { "gear.armor.light", new Dictionary<Language, string> { { Language.English, "Light Armor" }, { Language.Spanish, "Armadura Ligera" } } },
            { "gear.armor.heavy", new Dictionary<Language, string> { { Language.English, "Heavy Armor" }, { Language.Spanish, "Armadura Pesada" } } },
            { "gear.accessory.ring", new Dictionary<Language, string> { { Language.English, "Ring" }, { Language.Spanish, "Anillo" } } }
        };

    static LocalizationService()
    {
        LoadLanguage();
    }

    public static void SetLanguage(Language language)
    {
        CurrentLanguage = language;
        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save();
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

    private static void LoadLanguage()
    {
        int saved = PlayerPrefs.GetInt(LanguagePrefKey, 0);
        if (System.Enum.IsDefined(typeof(Language), saved))
        {
            CurrentLanguage = (Language)saved;
        }
        else
        {
            CurrentLanguage = Language.English;
        }
    }
}
