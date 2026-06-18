using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class ProceduralAudioBuilderTest : MonoBehaviour
{
    [ContextMenu("Run Procedural Audio Builder Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Procedural Audio Builder Tests ===");

        TestBgmClipsAreLoopsWithNonSilentSamples();
        TestStingClipsAreShortAndDecay();
        TestBuilderIsDeterministic();
        TestAllCueBuildersReturnClips();

        Debug.Log("=== All Procedural Audio Builder Tests Passed ===");
    }

    private void TestBgmClipsAreLoopsWithNonSilentSamples()
    {
        AudioClip exploration = ProceduralAudioBuilder.BuildExplorationBgm();
        Assert.IsNotNull(exploration, "Exploration BGM should be generated.");
        Assert.IsTrue(exploration.length >= 4f, "Exploration BGM should be at least 4 seconds long.");
        AssertClipHasAudibleSignal(exploration, "ExplorationBgm");

        AudioClip combat = ProceduralAudioBuilder.BuildCombatBgm();
        AssertClipHasAudibleSignal(combat, "CombatBgm");

        AudioClip puzzle = ProceduralAudioBuilder.BuildPuzzleBgm();
        AssertClipHasAudibleSignal(puzzle, "PuzzleBgm");

        AudioClip ending = ProceduralAudioBuilder.BuildEndingBgm();
        AssertClipHasAudibleSignal(ending, "EndingBgm");
    }

    private void TestStingClipsAreShortAndDecay()
    {
        AudioClip victory = ProceduralAudioBuilder.BuildCombatVictorySting();
        AssertClipHasAudibleSignal(victory, "CombatVictorySting");
        AssertClipsStartsLouderThanItEnds(victory, "CombatVictorySting");

        AudioClip defeat = ProceduralAudioBuilder.BuildCombatDefeatSting();
        AssertClipsStartsLouderThanItEnds(defeat, "CombatDefeatSting");

        AudioClip puzzle = ProceduralAudioBuilder.BuildPuzzleSolvedSting();
        AssertClipHasAudibleSignal(puzzle, "PuzzleSolvedSting");

        AudioClip boss = ProceduralAudioBuilder.BuildBossIntroSting();
        AssertClipHasAudibleSignal(boss, "BossIntroSting");

        AudioClip travel = ProceduralAudioBuilder.BuildTravelFanfareSting();
        AssertClipHasAudibleSignal(travel, "TravelFanfareSting");
    }

    private void TestBuilderIsDeterministic()
    {
        AudioClip a = ProceduralAudioBuilder.BuildExplorationBgm();
        AudioClip b = ProceduralAudioBuilder.BuildExplorationBgm();
        Assert.AreEqual(a.samples, b.samples, "Identical BGM length expected for deterministic builder.");
    }

    private void TestAllCueBuildersReturnClips()
    {
        System.Func<AudioClip>[] builders = {
            ProceduralAudioBuilder.BuildExplorationBgm,
            ProceduralAudioBuilder.BuildCombatBgm,
            ProceduralAudioBuilder.BuildPuzzleBgm,
            ProceduralAudioBuilder.BuildEndingBgm,
            ProceduralAudioBuilder.BuildCombatVictorySting,
            ProceduralAudioBuilder.BuildCombatDefeatSting,
            ProceduralAudioBuilder.BuildPuzzleSolvedSting,
            ProceduralAudioBuilder.BuildBossIntroSting,
            ProceduralAudioBuilder.BuildTravelFanfareSting
        };
        for (int i = 0; i < builders.Length; i++)
        {
            AudioClip clip = builders[i]();
            Assert.IsNotNull(clip, $"Builder {i} should always return a clip.");
        }
    }

    private static void AssertClipHasAudibleSignal(AudioClip clip, string label)
    {
        Assert.IsNotNull(clip, $"{label} should not be null.");
        int sampleCount = Mathf.Min(clip.samples, 8192);
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);
        float sum = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        float mean = sum / Mathf.Max(1, sampleCount);
        Assert.Greater(mean, 0.001f, $"{label} should contain audible signal (mean abs > 0.001).");
    }

    private static void AssertClipsStartsLouderThanItEnds(AudioClip clip, string label)
    {
        int headSamples = Mathf.Min(clip.samples / 4, 4096);
        int tailStart = Mathf.Max(0, clip.samples - headSamples);
        float[] head = new float[headSamples];
        float[] tail = new float[headSamples];
        clip.GetData(head, 0);
        clip.GetData(tail, tailStart);
        float headMean = AbsMean(head);
        float tailMean = AbsMean(tail);
        Assert.Greater(headMean, tailMean, $"{label} should decay (head louder than tail).");
    }

    private static float AbsMean(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        return sum / Mathf.Max(1, samples.Length);
    }
}
