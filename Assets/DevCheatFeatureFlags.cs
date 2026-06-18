using System.Collections.Generic;

public static class DevCheatFeatureFlags
{
    private const int TotalFlags = 32;

    private static bool invincibility;
    private static bool oneHitKill;
    private static bool infiniteMP;
    private static bool fullPartyUnlocked;
    private static bool unlockAllIslands;
    private static bool maxGold;
    private static bool allSpellsAvailable;
    private static bool allGearAvailable;
    private static bool skipTutorial;
    private static bool noClip;
    private static float speedMultiplier = 1f;
    private static bool forceCrit;
    private static bool forceFlee;
    private static bool disableEncounters;
    private static bool disablePuzzles;
    private static bool forceWin;
    private static bool forceLose;
    private static bool forceLevelUp;
    private static bool forceEncounter;
    private static bool forceBossSpawn;
    private static bool forceEndingGood;
    private static bool forceEndingBad;
    private static bool showDebugOverlay = true;
    private static bool showAiDebug;
    private static bool showDamageNumbers;
    private static bool showHitboxes;
    private static bool pauseEnemies;
    private static bool pauseTime;
    private static bool slowMotion;
    private static bool skipCutscenes;
    private static bool unlockAllCosmetics;
    private static bool enableConsole;
    private static bool logVerbose;

    public static int TotalFlagCount => TotalFlags;

    public static bool GetInvincibility() => invincibility;
    public static void SetInvincibility(bool v) => invincibility = v;

    public static bool GetOneHitKill() => oneHitKill;
    public static void SetOneHitKill(bool v) => oneHitKill = v;

    public static bool GetInfiniteMP() => infiniteMP;
    public static void SetInfiniteMP(bool v) => infiniteMP = v;

    public static bool GetFullPartyUnlocked() => fullPartyUnlocked;
    public static void SetFullPartyUnlocked(bool v) => fullPartyUnlocked = v;

    public static bool GetUnlockAllIslands() => unlockAllIslands;
    public static void SetUnlockAllIslands(bool v) => unlockAllIslands = v;

    public static bool GetMaxGold() => maxGold;
    public static void SetMaxGold(bool v) => maxGold = v;

    public static bool GetAllSpellsAvailable() => allSpellsAvailable;
    public static void SetAllSpellsAvailable(bool v) => allSpellsAvailable = v;

    public static bool GetAllGearAvailable() => allGearAvailable;
    public static void SetAllGearAvailable(bool v) => allGearAvailable = v;

    public static bool GetSkipTutorial() => skipTutorial;
    public static void SetSkipTutorial(bool v) => skipTutorial = v;

    public static bool GetNoClip() => noClip;
    public static void SetNoClip(bool v) => noClip = v;

    public static float GetSpeedMultiplier() => speedMultiplier;
    public static void SetSpeedMultiplier(float v) => speedMultiplier = v < 0.1f ? 0.1f : v;

    public static bool GetForceCrit() => forceCrit;
    public static void SetForceCrit(bool v) => forceCrit = v;

    public static bool GetForceFlee() => forceFlee;
    public static void SetForceFlee(bool v) => forceFlee = v;

    public static bool GetDisableEncounters() => disableEncounters;
    public static void SetDisableEncounters(bool v) => disableEncounters = v;

    public static bool GetDisablePuzzles() => disablePuzzles;
    public static void SetDisablePuzzles(bool v) => disablePuzzles = v;

    public static bool GetForceWin() => forceWin;
    public static void SetForceWin(bool v) => forceWin = v;

    public static bool GetForceLose() => forceLose;
    public static void SetForceLose(bool v) => forceLose = v;

    public static bool GetForceLevelUp() => forceLevelUp;
    public static void SetForceLevelUp(bool v) => forceLevelUp = v;

    public static bool GetForceEncounter() => forceEncounter;
    public static void SetForceEncounter(bool v) => forceEncounter = v;

    public static bool GetForceBossSpawn() => forceBossSpawn;
    public static void SetForceBossSpawn(bool v) => forceBossSpawn = v;

    public static bool GetForceEndingGood() => forceEndingGood;
    public static void SetForceEndingGood(bool v) => forceEndingGood = v;

    public static bool GetForceEndingBad() => forceEndingBad;
    public static void SetForceEndingBad(bool v) => forceEndingBad = v;

    public static bool GetShowDebugOverlay() => showDebugOverlay;
    public static void SetShowDebugOverlay(bool v) => showDebugOverlay = v;

    public static bool GetShowAiDebug() => showAiDebug;
    public static void SetShowAiDebug(bool v) => showAiDebug = v;

    public static bool GetShowDamageNumbers() => showDamageNumbers;
    public static void SetShowDamageNumbers(bool v) => showDamageNumbers = v;

    public static bool GetShowHitboxes() => showHitboxes;
    public static void SetShowHitboxes(bool v) => showHitboxes = v;

    public static bool GetPauseEnemies() => pauseEnemies;
    public static void SetPauseEnemies(bool v) => pauseEnemies = v;

    public static bool GetPauseTime() => pauseTime;
    public static void SetPauseTime(bool v) => pauseTime = v;

    public static bool GetSlowMotion() => slowMotion;
    public static void SetSlowMotion(bool v) => slowMotion = v;

    public static bool GetSkipCutscenes() => skipCutscenes;
    public static void SetSkipCutscenes(bool v) => skipCutscenes = v;

    public static bool GetUnlockAllCosmetics() => unlockAllCosmetics;
    public static void SetUnlockAllCosmetics(bool v) => unlockAllCosmetics = v;

    public static bool GetEnableConsole() => enableConsole;
    public static void SetEnableConsole(bool v) => enableConsole = v;

    public static bool GetLogVerbose() => logVerbose;
    public static void SetLogVerbose(bool v) => logVerbose = v;

    public static IEnumerable<string> GetAllFlagIds()
    {
        return new[]
        {
            "invincibility", "oneHitKill", "infiniteMP", "fullParty", "unlockAllIslands",
            "maxGold", "allSpells", "allGear", "skipTutorial", "noClip",
            "speedMultiplier", "forceCrit", "forceFlee", "disableEncounters", "disablePuzzles",
            "forceWin", "forceLose", "forceLevelUp", "forceEncounter", "forceBossSpawn",
            "forceEndingGood", "forceEndingBad", "showDebugOverlay", "showAiDebug", "showDamageNumbers",
            "showHitboxes", "pauseEnemies", "pauseTime", "slowMotion", "skipCutscenes",
            "unlockAllCosmetics", "enableConsole"
        };
    }

    public static bool LogicOk()
    {
        int count = 0;
        foreach (string _ in GetAllFlagIds())
        {
            count++;
        }
        return count == 32;
    }

    public static void ResetAllForDebug()
    {
        invincibility = false;
        oneHitKill = false;
        infiniteMP = false;
        fullPartyUnlocked = false;
        unlockAllIslands = false;
        maxGold = false;
        allSpellsAvailable = false;
        allGearAvailable = false;
        skipTutorial = false;
        noClip = false;
        speedMultiplier = 1f;
        forceCrit = false;
        forceFlee = false;
        disableEncounters = false;
        disablePuzzles = false;
        forceWin = false;
        forceLose = false;
        forceLevelUp = false;
        forceEncounter = false;
        forceBossSpawn = false;
        forceEndingGood = false;
        forceEndingBad = false;
        showDebugOverlay = true;
        showAiDebug = false;
        showDamageNumbers = false;
        showHitboxes = false;
        pauseEnemies = false;
        pauseTime = false;
        slowMotion = false;
        skipCutscenes = false;
        unlockAllCosmetics = false;
        enableConsole = false;
        logVerbose = false;
    }
}
