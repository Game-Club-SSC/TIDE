using UnityEngine;

/// <summary>
/// Validates that ProceduralAudioBuilder can generate all required SFX
/// for the GDD V2 combat, UI, and interaction systems.
/// </summary>
[ContextMenu("Run SFX Content Validation")]
public class SfxContentValidationTest : MonoBehaviour
{
    [ContextMenu("Validate All SFX Content")]
    public void RunTests()
    {
        Debug.Log("=== SFX Content Validation (Issue #277) ===");

        TestCombatSfx();
        TestInteractionSfx();
        TestUiSfx();

        Debug.Log("=== All SFX Content Tests Passed ===");
    }

    private void TestCombatSfx()
    {
        Debug.Log("Validating combat SFX...");

        AudioClip hit = ProceduralAudioBuilder.BuildAttackHitSfx();
        Assert.IsNotNull(hit, "Attack hit SFX should generate.");

        AudioClip miss = ProceduralAudioBuilder.BuildAttackMissSfx();
        Assert.IsNotNull(miss, "Attack miss SFX should generate.");

        AudioClip crit = ProceduralAudioBuilder.BuildAttackCritSfx();
        Assert.IsNotNull(crit, "Attack crit SFX should generate.");

        AudioClip heal = ProceduralAudioBuilder.BuildHealSfx();
        Assert.IsNotNull(heal, "Heal SFX should generate.");

        AudioClip tideBreak = ProceduralAudioBuilder.BuildTideBreakSfx();
        Assert.IsNotNull(tideBreak, "Tide break SFX should generate.");

        AudioClip qteSuccess = ProceduralAudioBuilder.BuildQTESuccessSfx();
        Assert.IsNotNull(qteSuccess, "QTE success SFX should generate.");

        AudioClip qteFail = ProceduralAudioBuilder.BuildQTEFailSfx();
        Assert.IsNotNull(qteFail, "QTE fail SFX should generate.");

        Debug.Log("Combat SFX validated: 7 sounds.");
    }

    private void TestInteractionSfx()
    {
        Debug.Log("Validating interaction SFX...");

        AudioClip tileTake = ProceduralAudioBuilder.BuildTileTakeSfx();
        Assert.IsNotNull(tileTake, "Tile take SFX should generate.");

        AudioClip tilePlace = ProceduralAudioBuilder.BuildTilePlaceSfx();
        Assert.IsNotNull(tilePlace, "Tile place SFX should generate.");

        AudioClip boatDepart = ProceduralAudioBuilder.BuildBoatDepartSfx();
        Assert.IsNotNull(boatDepart, "Boat depart SFX should generate.");

        AudioClip boatArrive = ProceduralAudioBuilder.BuildBoatArriveSfx();
        Assert.IsNotNull(boatArrive, "Boat arrive SFX should generate.");

        AudioClip puzzleMilestone = ProceduralAudioBuilder.BuildPuzzleMilestoneSfx();
        Assert.IsNotNull(puzzleMilestone, "Puzzle milestone SFX should generate.");

        Debug.Log("Interaction SFX validated: 5 sounds.");
    }

    private void TestUiSfx()
    {
        Debug.Log("Validating UI SFX...");

        AudioClip menuClick = ProceduralAudioBuilder.BuildMenuClickSfx();
        Assert.IsNotNull(menuClick, "Menu click SFX should generate.");

        AudioClip menuOpen = ProceduralAudioBuilder.BuildMenuOpenSfx();
        Assert.IsNotNull(menuOpen, "Menu open SFX should generate.");

        AudioClip menuClose = ProceduralAudioBuilder.BuildMenuCloseSfx();
        Assert.IsNotNull(menuClose, "Menu close SFX should generate.");

        AudioClip levelUp = ProceduralAudioBuilder.BuildLevelUpSfx();
        Assert.IsNotNull(levelUp, "Level up SFX should generate.");

        AudioClip gearEquip = ProceduralAudioBuilder.BuildGearEquipSfx();
        Assert.IsNotNull(gearEquip, "Gear equip SFX should generate.");

        AudioClip dialogueAdvance = ProceduralAudioBuilder.BuildDialogueAdvanceSfx();
        Assert.IsNotNull(dialogueAdvance, "Dialogue advance SFX should generate.");

        AudioClip statusApply = ProceduralAudioBuilder.BuildStatusEffectApplySfx();
        Assert.IsNotNull(statusApply, "Status effect apply SFX should generate.");

        AudioClip statusExpire = ProceduralAudioBuilder.BuildStatusEffectExpireSfx();
        Assert.IsNotNull(statusExpire, "Status effect expire SFX should generate.");

        AudioClip ancientText = ProceduralAudioBuilder.BuildAncientTextSfx();
        Assert.IsNotNull(ancientText, "Ancient text SFX should generate.");

        Debug.Log("UI SFX validated: 9 sounds.");
    }
}
