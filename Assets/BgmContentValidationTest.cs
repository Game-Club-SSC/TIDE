using UnityEngine;

/// <summary>
/// Validates that ProceduralAudioBuilder can generate all required BGM tracks
/// for the GDD V2 six-island campaign. Run via ContextMenu in the Editor.
/// </summary>
public class BgmContentValidationTest : MonoBehaviour
{
    [ContextMenu("Validate All BGM Content")]
    public void RunTests()
    {
        Debug.Log("=== BGM Content Validation (Issue #276) ===");

        TestGenericBgmTracks();
        TestIslandBgmTracks();
        TestStingerTracks();

        Debug.Log("=== All BGM Content Tests Passed ===");
    }

    private void TestGenericBgmTracks()
    {
        Debug.Log("Validating generic BGM tracks...");

        AudioClip exploration = ProceduralAudioBuilder.BuildExplorationBgm();
        Assert.IsNotNull(exploration, "Exploration BGM should generate.");
        Assert.IsTrue(exploration.samples > 0, "Exploration BGM should have audio data.");

        AudioClip combat = ProceduralAudioBuilder.BuildCombatBgm();
        Assert.IsNotNull(combat, "Combat BGM should generate.");

        AudioClip puzzle = ProceduralAudioBuilder.BuildPuzzleBgm();
        Assert.IsNotNull(puzzle, "Puzzle BGM should generate.");

        AudioClip ending = ProceduralAudioBuilder.BuildEndingBgm();
        Assert.IsNotNull(ending, "Ending BGM should generate.");

        AudioClip bossBattle = ProceduralAudioBuilder.BuildBossBattleBgm();
        Assert.IsNotNull(bossBattle, "Boss Battle BGM should generate.");

        AudioClip menu = ProceduralAudioBuilder.BuildMenuBgm();
        Assert.IsNotNull(menu, "Menu BGM should generate.");

        Debug.Log("Generic BGM tracks validated: 6 tracks.");
    }

    private void TestIslandBgmTracks()
    {
        Debug.Log("Validating per-island BGM tracks...");

        AudioClip greed = ProceduralAudioBuilder.BuildIslandGreedBgm();
        Assert.IsNotNull(greed, "Greed island BGM should generate.");

        AudioClip lust = ProceduralAudioBuilder.BuildIslandLustBgm();
        Assert.IsNotNull(lust, "Lust island BGM should generate.");

        AudioClip desire = ProceduralAudioBuilder.BuildIslandDesireBgm();
        Assert.IsNotNull(desire, "Desire island BGM should generate.");

        AudioClip anger = ProceduralAudioBuilder.BuildIslandAngerBgm();
        Assert.IsNotNull(anger, "Anger island BGM should generate.");

        AudioClip envy = ProceduralAudioBuilder.BuildIslandEnvyBgm();
        Assert.IsNotNull(envy, "Envy island BGM should generate.");

        AudioClip ego = ProceduralAudioBuilder.BuildIslandEgoBgm();
        Assert.IsNotNull(ego, "Ego island BGM should generate.");

        Debug.Log("Island BGM tracks validated: 6 tracks (Greed, Lust, Desire, Anger, Envy, Ego).");
    }

    private void TestStingerTracks()
    {
        Debug.Log("Validating stinger tracks...");

        AudioClip victory = ProceduralAudioBuilder.BuildCombatVictorySting();
        Assert.IsNotNull(victory, "Victory stinger should generate.");

        AudioClip defeat = ProceduralAudioBuilder.BuildCombatDefeatSting();
        Assert.IsNotNull(defeat, "Defeat stinger should generate.");

        AudioClip puzzleSolved = ProceduralAudioBuilder.BuildPuzzleSolvedSting();
        Assert.IsNotNull(puzzleSolved, "Puzzle solved stinger should generate.");

        AudioClip bossIntro = ProceduralAudioBuilder.BuildBossIntroSting();
        Assert.IsNotNull(bossIntro, "Boss intro stinger should generate.");

        AudioClip travel = ProceduralAudioBuilder.BuildTravelFanfareSting();
        Assert.IsNotNull(travel, "Travel fanfare should generate.");

        AudioClip actTransition = ProceduralAudioBuilder.BuildActTransitionSting();
        Assert.IsNotNull(actTransition, "Act transition stinger should generate.");

        Debug.Log("Stinger tracks validated: 6 stingers.");
    }
}
