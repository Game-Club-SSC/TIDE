using UnityEngine;

public static class ProceduralAudioBuilder
{
    private const int SampleRate = 44100;

    public static AudioClip BuildExplorationBgm()
    {
        return BuildLoopedClip("Auto_ExplorationBgm", 8f, (t, n) =>
        {
            float chord = Mathf.Sin(2f * Mathf.PI * 110f * t)
                        + 0.6f * Mathf.Sin(2f * Mathf.PI * 165f * t)
                        + 0.4f * Mathf.Sin(2f * Mathf.PI * 220f * t);
            float pad = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.4f;
            float swell = Mathf.Sin(2f * Mathf.PI * t * 0.25f) * 0.3f + 0.7f;
            return (chord + pad) * 0.18f * swell;
        });
    }

    public static AudioClip BuildCombatBgm()
    {
        return BuildLoopedClip("Auto_CombatBgm", 6f, (t, n) =>
        {
            float bass = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 2f * t)) * 0.5f;
            float lead = Mathf.Sin(2f * Mathf.PI * 220f * t)
                       + 0.5f * Mathf.Sin(2f * Mathf.PI * 330f * t)
                       + 0.3f * Mathf.Sin(2f * Mathf.PI * 440f * t);
            float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * t);
            return (bass * 0.4f + lead * 0.18f) * pulse;
        });
    }

    public static AudioClip BuildPuzzleBgm()
    {
        return BuildLoopedClip("Auto_PuzzleBgm", 8f, (t, n) =>
        {
            float arp = Mathf.Sin(2f * Mathf.PI * 330f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t))
                      + 0.4f * Mathf.Sin(2f * Mathf.PI * 495f * t);
            float pad = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.4f;
            return (arp * 0.2f + pad * 0.15f);
        });
    }

    public static AudioClip BuildEndingBgm()
    {
        return BuildLoopedClip("Auto_EndingBgm", 10f, (t, n) =>
        {
            float pad = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.5f
                      + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.35f
                      + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.2f;
            float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);
            return pad * 0.2f * swell;
        });
    }

    public static AudioClip BuildCombatVictorySting()
    {
        return BuildOneShot("Auto_CombatVictorySting", 1.4f, (t, n) =>
        {
            float arp = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(523.25f, 1046.5f, t / 1.4f) * t);
            float decay = Mathf.Exp(-3f * t);
            return arp * 0.4f * decay;
        });
    }

    public static AudioClip BuildCombatDefeatSting()
    {
        return BuildOneShot("Auto_CombatDefeatSting", 1.6f, (t, n) =>
        {
            float pitch = Mathf.Lerp(220f, 65f, Mathf.Clamp01(t / 1.6f));
            float tone = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float decay = Mathf.Exp(-2.5f * t);
            return tone * 0.45f * decay;
        });
    }

    public static AudioClip BuildPuzzleSolvedSting()
    {
        return BuildOneShot("Auto_PuzzleSolvedSting", 0.8f, (t, n) =>
        {
            float chord = Mathf.Sin(2f * Mathf.PI * 660f * t)
                        + 0.6f * Mathf.Sin(2f * Mathf.PI * 880f * t)
                        + 0.4f * Mathf.Sin(2f * Mathf.PI * 1320f * t);
            float decay = Mathf.Exp(-3.5f * t);
            return chord * 0.25f * decay;
        });
    }

    public static AudioClip BuildBossIntroSting()
    {
        return BuildOneShot("Auto_BossIntroSting", 1.8f, (t, n) =>
        {
            float saw = 0f;
            for (int h = 1; h <= 5; h++)
            {
                saw += Mathf.Sin(2f * Mathf.PI * 110f * h * t) / h;
            }
            float rise = Mathf.Clamp01(t / 1.8f);
            float body = saw * 0.35f * rise;
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.4f * rise;
            return body + sub;
        });
    }

    public static AudioClip BuildTravelFanfareSting()
    {
        return BuildOneShot("Auto_TravelFanfareSting", 1.2f, (t, n) =>
        {
            float freq = t < 0.4f
                ? Mathf.Lerp(440f, 660f, t / 0.4f)
                : t < 0.8f
                    ? Mathf.Lerp(660f, 880f, (t - 0.4f) / 0.4f)
                    : Mathf.Lerp(880f, 1320f, (t - 0.8f) / 0.4f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float env = Mathf.Min(1f, t * 4f) * Mathf.Exp(-1.2f * t);
            return tone * 0.4f * env;
        });
    }

    private static AudioClip BuildLoopedClip(string name, float lengthSeconds, System.Func<float, int, float> sampleFn)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * lengthSeconds);
        float[] data = new float[sampleCount];
        for (int n = 0; n < sampleCount; n++)
        {
            float t = (float)n / SampleRate;
            data[n] = Mathf.Clamp(sampleFn(t, n), -1f, 1f);
        }
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, true);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip BuildOneShot(string name, float lengthSeconds, System.Func<float, int, float> sampleFn)
    {
        return BuildLoopedClip(name, lengthSeconds, sampleFn);
    }
}
