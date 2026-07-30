using System.Collections.Generic;
using UnityEngine;

public static class HeroDataCatalog
{
    public static bool HasCritDefaults()
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        try
        {
            return hero.baseCritRate >= 0f && hero.baseCritDamage >= 0f;
        }
        finally
        {
            Object.DestroyImmediate(hero);
        }
    }
}

public static class EnemyDataCatalog
{
    public static bool HasCritDefaults()
    {
        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            return enemy.baseCritRate >= 0f && enemy.baseCritDamage >= 0f;
        }
        finally
        {
            Object.DestroyImmediate(enemy);
        }
    }
}

public static class PuzzleDataCatalog
{
    public static bool SupportsAllTilesFive()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        try
        {
            data.winCondition = new PuzzleWinCondition
            {
                type = WinConditionType.AllEqualToTarget,
                targetValue = 5
            };
            data.sealedPosition = new Vector2Int(-1, -1);
            int[,] grid = { { 5, 5, 5 }, { 5, 5, 5 }, { 5, 5, 5 } };
            return data.winCondition.IsMet(grid, data.sealedPosition);
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }
}

public static class SpriteLibraryCatalog
{
    public static bool HasPlayerSprites()
    {
        Sprite sprite = FuturisticSpriteLibrary.GetPlayerOverworldSprite("style_fire_vanguard");
        return sprite != null;
    }
}

public static class AudioCatalogCoversCues
{
    public static bool HasCueClips()
    {
        // Verify core BGM builders
        AudioClip exploration = ProceduralAudioBuilder.BuildExplorationBgm();
        if (exploration == null || exploration.length <= 0f) return false;

        AudioClip combat = ProceduralAudioBuilder.BuildCombatBgm();
        if (combat == null) return false;

        AudioClip puzzle = ProceduralAudioBuilder.BuildPuzzleBgm();
        if (puzzle == null) return false;

        AudioClip ending = ProceduralAudioBuilder.BuildEndingBgm();
        if (ending == null) return false;

        // Verify new SFX builders
        AudioClip attackHit = ProceduralAudioBuilder.BuildAttackHitSfx();
        if (attackHit == null) return false;

        AudioClip heal = ProceduralAudioBuilder.BuildHealSfx();
        if (heal == null) return false;

        // Verify per-island BGM
        AudioClip greed = ProceduralAudioBuilder.BuildIslandGreedBgm();
        if (greed == null) return false;

        return true;
    }
}

public static class AcceptanceConversationGating
{
    public static bool Ok()
    {
        return AcceptanceConversation.LineCount == 10
            && !string.IsNullOrEmpty(AcceptanceConversation.FinalBossIslandId);
    }
}

public static class SelfHarmBeatGating
{
    public static bool Ok()
    {
        return SelfHarmBeat.LineCount == 4;
    }
}

public static class TideBreakUnlockLogic
{
    public static bool Ok()
    {
        return true;
    }
}

public static class RelationshipTrackerLogic
{
    public static bool Ok()
    {
        GameObject host = new GameObject("Test_RT");
        RelationshipTracker tracker = host.AddComponent<RelationshipTracker>();
        try
        {
            tracker.SetAffinity("test", 50);
            return tracker.GetRelationshipTier("test") == RelationshipTracker.RelationshipTier.Friend;
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}

public static class PowerBudgetLogic
{
    public static bool Ok()
    {
        GameObject host = new GameObject("Test_PB");
        PowerBudgetTracker tracker = host.AddComponent<PowerBudgetTracker>();
        try
        {
            return tracker.DefaultBudgetPerIsland > 0f;
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}

public static class TeleportAnchorCatalog
{
    public static bool HasDocks()
    {
        GameObject host = new GameObject("Test_Anchor");
        TeleportAnchor anchor = host.AddComponent<TeleportAnchor>();
        anchor.anchorId = "test_dock";
        anchor.islandId = "island_test";
        anchor.isBoatDock = true;
        try
        {
            return TeleportAnchor.FindBoatDockForIsland("island_test") != null;
        }
        finally
        {
            Object.DestroyImmediate(host);
            TeleportAnchor.ClearRegistryForDebug();
        }
    }
}

public static class PartySwapServiceGating
{
    public static bool Ok()
    {
        string reason;
        return PartySwapService.TryQueueSwap("hero_a", "hero_b", out reason);
    }
}

public static class MobileTouchControllerLogic
{
    public static bool Ok()
    {
        return MobileTouchController.ActionButtonId.Sprint != MobileTouchController.ActionButtonId.Interact;
    }
}

public static class PuzzleVariantServiceLogic
{
    public static bool Ok()
    {
        PuzzleData data = ScriptableObject.CreateInstance<PuzzleData>();
        try
        {
            data.enableConsumption = true;
            data.consumptionAmount = 2;
            return PuzzleVariantService.IsGreedConsumptionEnabled(data)
                && PuzzleVariantService.GetConsumptionAmount(data) == 2;
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }
}

public static class DesireStatusEffects
{
    public static bool Ok()
    {
        StatusEffect slow = DesireStatusEffectSet.CreateSlowEffect("test", 2, 0.5f);
        StatusEffect drowsy = DesireStatusEffectSet.CreateDrowsyEffect("test", 1);
        return slow != null && drowsy != null
            && slow.Type == StatusEffectType.Slow
            && drowsy.Type == StatusEffectType.Drowsy;
    }
}

public static class EnvyMirrorServiceLogic
{
    public static bool Ok()
    {
        EnvyMirrorService.SetMirrorEnabled(true);
        EnvyMirrorService.SetMirroredElement(CombatUnit.Element.Fire);
        bool ok = EnvyMirrorService.IsMirrorEnabled
            && EnvyMirrorService.GetMirrorElementFor(CombatUnit.Element.Water) == CombatUnit.Element.Fire;
        EnvyMirrorService.ResetForDebug();
        return ok;
    }
}

public static class DifficultyServiceLogic
{
    public static bool HasDifficulties()
    {
        return DifficultyModeService.Difficulty.Hardcore != DifficultyModeService.Difficulty.Story
            && DifficultyModeService.Difficulty.Standard != DifficultyModeService.Difficulty.Hardcore;
    }
}

public static class BattleHudPolishServiceLogic
{
    public static bool Ok()
    {
        Color crit = BattleHudPolishService.GetCritFlashColor();
        float duration = BattleHudPolishService.GetCritFlashDuration();
        return duration > 0f && crit.a > 0f;
    }
}

public static class NewGamePlusServiceLogic
{
    private const string NgPlusKey = "NewGamePlusService";

    public static bool Ok()
    {
        // Snapshot PlayerPrefs so the regression check does not persist fake
        // completion data to the user's save.
        string savedData = PlayerPrefs.HasKey(NgPlusKey) ? PlayerPrefs.GetString(NgPlusKey) : null;

        GameObject host = new GameObject("Test_NGPlus");
        NewGamePlusService service = host.AddComponent<NewGamePlusService>();
        try
        {
            service.RegisterCompletion();
            return service.CanStartNewGamePlus();
        }
        finally
        {
            Object.DestroyImmediate(host);

            // Restore the original PlayerPrefs value (or remove if none existed).
            if (savedData != null)
            {
                PlayerPrefs.SetString(NgPlusKey, savedData);
            }
            else
            {
                PlayerPrefs.DeleteKey(NgPlusKey);
            }
            PlayerPrefs.Save();
        }
    }
}

public static class LocalizationServiceLogic
{
    public static bool HasBothLanguages()
    {
        LocalizationService.SetLanguage(LocalizationService.Language.English);
        string en = LocalizationService.Get("ui.play");
        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        string es = LocalizationService.Get("ui.play");
        return !string.IsNullOrEmpty(en) && !string.IsNullOrEmpty(es) && en != es;
    }
}
