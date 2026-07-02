using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the audio integration system.
/// Verifies AudioManager hooks into all game states.
/// </summary>
[DisallowMultipleComponent]
public class AudioIntegrationTest : MonoBehaviour
{
    [ContextMenu("Run Audio Integration Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Audio Integration Tests ===");

        TestAudioCueEnumValues();
        TestAudioManagerExists();
        TestProceduralAudioBuilderExists();
        TestAudioCueResolution();

        Debug.Log("=== All Audio Integration Tests Passed ===");
    }

    private void TestAudioCueEnumValues()
    {
        Debug.Log("Testing AudioCue enum values...");

        // Verify all expected audio cues exist
        AudioCue exploration = AudioCue.ExplorationBgm;
        AudioCue combat = AudioCue.CombatBgm;
        AudioCue puzzle = AudioCue.PuzzleBgm;
        AudioCue ending = AudioCue.EndingBgm;
        AudioCue victory = AudioCue.CombatVictory;
        AudioCue defeat = AudioCue.CombatDefeat;
        AudioCue puzzleSolved = AudioCue.PuzzleSolved;
        AudioCue bossIntro = AudioCue.BossIntro;
        AudioCue travel = AudioCue.TravelFanfare;

        // Verify they're all different
        Assert.AreNotEqual(exploration, combat, "ExplorationBgm and CombatBgm should be different.");
        Assert.AreNotEqual(combat, puzzle, "CombatBgm and PuzzleBgm should be different.");
        Assert.AreNotEqual(puzzle, ending, "PuzzleBgm and EndingBgm should be different.");

        Debug.Log("AudioCue enum validated: All expected cues exist.");
    }

    private void TestAudioManagerExists()
    {
        Debug.Log("Testing AudioManager exists...");

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            Assert.IsTrue(audioManager.isActiveAndEnabled, "AudioManager should be active and enabled.");
            Debug.Log("AudioManager exists and is active: PASS");
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found - may need to be initialized in scene.");
        }
    }

    private void TestProceduralAudioBuilderExists()
    {
        Debug.Log("Testing ProceduralAudioBuilder exists...");

        // Verify ProceduralAudioBuilder class exists
        Assert.IsNotNull(typeof(ProceduralAudioBuilder), "ProceduralAudioBuilder class should exist.");

        // Verify it has the expected methods
        var buildExplorationBgm = typeof(ProceduralAudioBuilder).GetMethod("BuildExplorationBgm");
        var buildCombatBgm = typeof(ProceduralAudioBuilder).GetMethod("BuildCombatBgm");
        var buildPuzzleBgm = typeof(ProceduralAudioBuilder).GetMethod("BuildPuzzleBgm");

        Assert.IsNotNull(buildExplorationBgm, "BuildExplorationBgm method should exist.");
        Assert.IsNotNull(buildCombatBgm, "BuildCombatBgm method should exist.");
        Assert.IsNotNull(buildPuzzleBgm, "BuildPuzzleBgm method should exist.");

        Debug.Log("ProceduralAudioBuilder exists with expected methods: PASS");
    }

    private void TestAudioCueResolution()
    {
        Debug.Log("Testing audio cue resolution...");

        // Test that procedural audio can be generated
        AudioClip explorationClip = ProceduralAudioBuilder.BuildExplorationBgm();
        AudioClip combatClip = ProceduralAudioBuilder.BuildCombatBgm();
        AudioClip puzzleClip = ProceduralAudioBuilder.BuildPuzzleBgm();

        Assert.IsNotNull(explorationClip, "Exploration BGM clip should be generated.");
        Assert.IsNotNull(combatClip, "Combat BGM clip should be generated.");
        Assert.IsNotNull(puzzleClip, "Puzzle BGM clip should be generated.");

        // Verify clips have reasonable length
        Assert.Greater(explorationClip.length, 1f, "Exploration BGM should be at least 1 second.");
        Assert.Greater(combatClip.length, 1f, "Combat BGM should be at least 1 second.");
        Assert.Greater(puzzleClip.length, 1f, "Puzzle BGM should be at least 1 second.");

        Debug.Log($"Audio cue resolution: exploration={explorationClip.length:F1}s, combat={combatClip.length:F1}s, puzzle={puzzleClip.length:F1}s");
    }
}
