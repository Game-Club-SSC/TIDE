using UnityEngine;

public static class ProceduralAudioBuilder
{
    private const int SampleRate = 44100;

    // ======================================================================
    //  Existing BGM builders
    // ======================================================================

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

    // ======================================================================
    //  Existing sting builders
    // ======================================================================

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

    // ======================================================================
    //  Act-based exploration BGM variants
    // ======================================================================

    public static AudioClip BuildExplorationBgmActII()
    {
        return BuildLoopedClip("Auto_ExplorationBgm_ActII", 10f, (t, n) =>
        {
            float chord = Mathf.Sin(2f * Mathf.PI * 98f * t)
                        + 0.5f * Mathf.Sin(2f * Mathf.PI * 146.83f * t)
                        + 0.3f * Mathf.Sin(2f * Mathf.PI * 196f * t);
            float pad = Mathf.Sin(2f * Mathf.PI * 49f * t) * 0.5f;
            float dissonance = 0.15f * Mathf.Sin(2f * Mathf.PI * 103.83f * t);
            float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.12f * t);
            return (chord + pad + dissonance) * 0.16f * swell;
        });
    }

    public static AudioClip BuildExplorationBgmActIII()
    {
        return BuildLoopedClip("Auto_ExplorationBgm_ActIII", 12f, (t, n) =>
        {
            float root = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.5f;
            float fifth = Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.3f;
            float octave = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.15f;
            float breath = Mathf.Sin(2f * Mathf.PI * 0.08f * t) * 0.4f + 0.6f;
            return (root + fifth + octave) * 0.18f * breath;
        });
    }

    // ======================================================================
    //  New SFX builders
    // ======================================================================

    public static AudioClip BuildAttackHitSfx()
    {
        return BuildOneShot("Auto_AttackHitSfx", 0.15f, (t, n) =>
        {
            // Percussive impact: noise burst + low thump
            float thump = Mathf.Sin(2f * Mathf.PI * 80f * t * Mathf.Exp(-20f * t));
            float noise = (n % 7 - 3) / 3f * Mathf.Exp(-40f * t);
            return (thump * 0.6f + noise * 0.3f) * Mathf.Exp(-15f * t);
        });
    }

    public static AudioClip BuildAttackMissSfx()
    {
        return BuildOneShot("Auto_AttackMissSfx", 0.2f, (t, n) =>
        {
            // Whoosh: filtered noise sweep
            float freq = Mathf.Lerp(2000f, 400f, t / 0.2f);
            float noise = (n % 11 - 5) / 5f;
            float filter = Mathf.Sin(2f * Mathf.PI * freq * t);
            return noise * filter * 0.2f * Mathf.Exp(-8f * t);
        });
    }

    public static AudioClip BuildAttackCritSfx()
    {
        return BuildOneShot("Auto_AttackCritSfx", 0.2f, (t, n) =>
        {
            // Enhanced impact with sparkle overtone
            float impact = Mathf.Sin(2f * Mathf.PI * 120f * t * Mathf.Exp(-25f * t));
            float sparkle = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.3f;
            float noise = (n % 7 - 3) / 3f * Mathf.Exp(-35f * t);
            return (impact * 0.5f + sparkle * 0.3f + noise * 0.2f) * Mathf.Exp(-12f * t);
        });
    }

    public static AudioClip BuildHealSfx()
    {
        return BuildOneShot("Auto_HealSfx", 0.5f, (t, n) =>
        {
            // Warm ascending chord
            float freq = Mathf.Lerp(440f, 880f, t / 0.5f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t)
                       + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 1.5f);
            float env = Mathf.Sin(Mathf.PI * t / 0.5f) * Mathf.Exp(-1.5f * t);
            return tone * 0.18f * env;
        });
    }

    public static AudioClip BuildTideBreakSfx()
    {
        return BuildOneShot("Auto_TideBreakSfx", 1.0f, (t, n) =>
        {
            // Dramatic power surge: rising + impact
            float rise = Mathf.Lerp(110f, 440f, Mathf.Clamp01(t / 0.6f));
            float body = Mathf.Sin(2f * Mathf.PI * rise * t);
            float impact = t > 0.5f ? Mathf.Sin(2f * Mathf.PI * 55f * (t - 0.5f)) * Mathf.Exp(-5f * (t - 0.5f)) : 0f;
            float envelope = t < 0.6f ? t / 0.6f : Mathf.Exp(-3f * (t - 0.6f));
            return (body * 0.3f + impact * 0.4f) * envelope;
        });
    }

    public static AudioClip BuildQTESuccessSfx()
    {
        return BuildOneShot("Auto_QTESuccessSfx", 0.3f, (t, n) =>
        {
            // Quick positive chime
            float chord = Mathf.Sin(2f * Mathf.PI * 880f * t)
                        + 0.6f * Mathf.Sin(2f * Mathf.PI * 1100f * t)
                        + 0.4f * Mathf.Sin(2f * Mathf.PI * 1320f * t);
            float env = Mathf.Exp(-6f * t);
            return chord * 0.2f * env;
        });
    }

    public static AudioClip BuildQTEFailSfx()
    {
        return BuildOneShot("Auto_QTEFailSfx", 0.4f, (t, n) =>
        {
            // Negative buzz
            float saw = 0f;
            for (int h = 1; h <= 3; h++)
            {
                saw += Mathf.Sin(2f * Mathf.PI * 150f * h * t) / h;
            }
            float env = Mathf.Exp(-4f * t);
            return saw * 0.25f * env;
        });
    }

    public static AudioClip BuildTileTakeSfx()
    {
        return BuildOneShot("Auto_TileTakeSfx", 0.1f, (t, n) =>
        {
            // Soft pickup: short ascending tone
            float freq = Mathf.Lerp(600f, 900f, t / 0.1f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            return tone * 0.3f * Mathf.Exp(-20f * t);
        });
    }

    public static AudioClip BuildTilePlaceSfx()
    {
        return BuildOneShot("Auto_TilePlaceSfx", 0.1f, (t, n) =>
        {
            // Placement click: short noise burst
            float click = Mathf.Sin(2f * Mathf.PI * 200f * t);
            float noise = (n % 5 - 2) / 2f * Mathf.Exp(-50f * t);
            return (click * 0.3f + noise * 0.2f) * Mathf.Exp(-25f * t);
        });
    }

    public static AudioClip BuildBoatDepartSfx()
    {
        return BuildOneShot("Auto_BoatDepartSfx", 0.6f, (t, n) =>
        {
            // Horn + whoosh
            float horn = Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 330f * t);
            float whoosh = (n % 7 - 3) / 3f * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(800f, 200f, t / 0.6f) * t);
            float env = Mathf.Sin(Mathf.PI * t / 0.6f);
            return (horn * 0.2f + whoosh * 0.15f) * env;
        });
    }

    public static AudioClip BuildBoatArriveSfx()
    {
        return BuildOneShot("Auto_BoatArriveSfx", 0.4f, (t, n) =>
        {
            // Descending chime
            float freq = Mathf.Lerp(880f, 440f, t / 0.4f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t)
                       + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2f);
            float env = Mathf.Exp(-3f * t);
            return tone * 0.2f * env;
        });
    }

    public static AudioClip BuildMenuClickSfx()
    {
        return BuildOneShot("Auto_MenuClickSfx", 0.05f, (t, n) =>
        {
            // UI click: short high tone
            float tone = Mathf.Sin(2f * Mathf.PI * 1000f * t);
            return tone * 0.25f * Mathf.Exp(-40f * t);
        });
    }

    public static AudioClip BuildMenuOpenSfx()
    {
        return BuildOneShot("Auto_MenuOpenSfx", 0.15f, (t, n) =>
        {
            // Slide up
            float freq = Mathf.Lerp(300f, 800f, t / 0.15f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            return tone * 0.2f * Mathf.Exp(-8f * t);
        });
    }

    public static AudioClip BuildMenuCloseSfx()
    {
        return BuildOneShot("Auto_MenuCloseSfx", 0.15f, (t, n) =>
        {
            // Slide down
            float freq = Mathf.Lerp(800f, 300f, t / 0.15f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            return tone * 0.2f * Mathf.Exp(-8f * t);
        });
    }

    public static AudioClip BuildLevelUpSfx()
    {
        return BuildOneShot("Auto_LevelUpSfx", 0.8f, (t, n) =>
        {
            // Celebratory ascending sequence
            float phase = t / 0.8f;
            float freq;
            if (phase < 0.25f) freq = Mathf.Lerp(440f, 554f, phase / 0.25f);
            else if (phase < 0.5f) freq = Mathf.Lerp(554f, 659f, (phase - 0.25f) / 0.25f);
            else if (phase < 0.75f) freq = Mathf.Lerp(659f, 880f, (phase - 0.5f) / 0.25f);
            else freq = Mathf.Lerp(880f, 1320f, (phase - 0.75f) / 0.25f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float env = Mathf.Sin(Mathf.PI * phase) * 0.8f + 0.2f;
            return tone * 0.25f * env;
        });
    }

    public static AudioClip BuildGearEquipSfx()
    {
        return BuildOneShot("Auto_GearEquipSfx", 0.2f, (t, n) =>
        {
            // Metallic click
            float click1 = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-60f * t);
            float click2 = Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-30f * t) * (t > 0.03f ? 1f : 0f);
            return (click1 * 0.3f + click2 * 0.2f);
        });
    }

    public static AudioClip BuildDialogueAdvanceSfx()
    {
        return BuildOneShot("Auto_DialogueAdvanceSfx", 0.05f, (t, n) =>
        {
            // Soft blip
            float tone = Mathf.Sin(2f * Mathf.PI * 660f * t);
            return tone * 0.2f * Mathf.Exp(-30f * t);
        });
    }

    public static AudioClip BuildStatusEffectApplySfx()
    {
        return BuildOneShot("Auto_StatusEffectApplySfx", 0.3f, (t, n) =>
        {
            // Shimmer: high-frequency modulation
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1200f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 12f * t));
            float env = Mathf.Exp(-4f * t);
            return shimmer * 0.15f * env;
        });
    }

    public static AudioClip BuildStatusEffectExpireSfx()
    {
        return BuildOneShot("Auto_StatusEffectExpireSfx", 0.3f, (t, n) =>
        {
            // Fade out tone
            float freq = Mathf.Lerp(600f, 200f, t / 0.3f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float env = Mathf.Exp(-3f * t);
            return tone * 0.2f * env;
        });
    }

    public static AudioClip BuildAncientTextSfx()
    {
        return BuildOneShot("Auto_AncientTextSfx", 0.5f, (t, n) =>
        {
            // Mysterious tone: detuned pads
            float tone = Mathf.Sin(2f * Mathf.PI * 277f * t) * 0.4f
                       + Mathf.Sin(2f * Mathf.PI * 280f * t) * 0.3f  // slight detune
                       + Mathf.Sin(2f * Mathf.PI * 415f * t) * 0.2f;
            float env = Mathf.Sin(Mathf.PI * t / 0.5f) * 0.9f + 0.1f;
            return tone * 0.2f * env;
        });
    }

    public static AudioClip BuildPuzzleMilestoneSfx()
    {
        return BuildOneShot("Auto_PuzzleMilestoneSfx", 0.4f, (t, n) =>
        {
            // Partial completion chime
            float chord = Mathf.Sin(2f * Mathf.PI * 660f * t)
                        + 0.5f * Mathf.Sin(2f * Mathf.PI * 830f * t);
            float env = Mathf.Exp(-5f * t);
            return chord * 0.2f * env;
        });
    }

    // ======================================================================
    //  New BGM builders
    // ======================================================================

    public static AudioClip BuildBossBattleBgm()
    {
        return BuildLoopedClip("Auto_BossBattleBgm", 6f, (t, n) =>
        {
            // More intense combat variant with heavier bass and faster pulse
            float bass = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 3f * t)) * 0.6f;
            float lead = Mathf.Sin(2f * Mathf.PI * 277f * t)
                       + 0.5f * Mathf.Sin(2f * Mathf.PI * 415f * t)
                       + 0.3f * Mathf.Sin(2f * Mathf.PI * 554f * t);
            float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 6f * t);
            float growl = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.3f;
            return (bass * 0.45f + lead * 0.2f + growl) * pulse;
        });
    }

    public static AudioClip BuildMenuBgm()
    {
        return BuildLoopedClip("Auto_MenuBgm", 8f, (t, n) =>
        {
            // Calm ambient menu music
            float pad = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.4f
                      + Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.3f
                      + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.15f;
            float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.2f * t);
            return pad * 0.15f * swell;
        });
    }

    public static AudioClip BuildActTransitionSting()
    {
        return BuildOneShot("Auto_ActTransitionSting", 1.5f, (t, n) =>
        {
            // Dramatic tone shift cue: low rumble + rising tension
            float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            float tension = Mathf.Lerp(220f, 440f, Mathf.Clamp01(t / 1.5f));
            float body = Mathf.Sin(2f * Mathf.PI * tension * t) * 0.3f;
            float envelope = t < 0.3f ? t / 0.3f : Mathf.Exp(-1.5f * (t - 0.3f));
            return (rumble * 0.3f + body) * envelope;
        });
    }

    // ======================================================================
    //  Per-island BGM variants (unique harmonic color per vice)
    // ======================================================================

    /// <summary>
    /// Greed: bright, glittering arpeggios (major key, gold shimmer).
    /// </summary>
    public static AudioClip BuildIslandGreedBgm()
    {
        return BuildLoopedClip("Auto_IslandGreedBgm", 8f, (t, n) =>
        {
            float arp1 = Mathf.Sin(2f * Mathf.PI * 523f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * t));
            float arp2 = Mathf.Sin(2f * Mathf.PI * 659f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3f * t));
            float shimmer = Mathf.Sin(2f * Mathf.PI * 2000f * t) * 0.1f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 8f * t));
            float pad = Mathf.Sin(2f * Mathf.PI * 131f * t) * 0.3f;
            return (arp1 * 0.15f + arp2 * 0.1f + shimmer + pad) * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.3f * t));
        });
    }

    /// <summary>
    /// Desire: slow, drowsy drones with minimal movement (low minor).
    /// </summary>
    public static AudioClip BuildIslandDesireBgm()
    {
        return BuildLoopedClip("Auto_IslandDesireBgm", 10f, (t, n) =>
        {
            float drone = Mathf.Sin(2f * Mathf.PI * 73.4f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.3f;
            float swell = Mathf.Sin(2f * Mathf.PI * 0.1f * t) * 0.3f + 0.7f;
            float overtone = Mathf.Sin(2f * Mathf.PI * 147f * t) * 0.15f * swell;
            return (drone * 0.2f + overtone) * swell;
        });
    }

    /// <summary>
    /// Envy: unsettling dissonant intervals (minor 2nds, eerie).
    /// </summary>
    public static AudioClip BuildIslandEnvyBgm()
    {
        return BuildLoopedClip("Auto_IslandEnvyBgm", 8f, (t, n) =>
        {
            float dissonance = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.4f
                             + Mathf.Sin(2f * Mathf.PI * 233f * t) * 0.35f; // minor 2nd
            float whisper = (n % 13 - 6) / 6f * 0.1f * Mathf.Sin(2f * Mathf.PI * 440f * t);
            float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.4f * t);
            return (dissonance * 0.18f + whisper) * pulse;
        });
    }

    /// <summary>
    /// Lust: warm, sensual pads (smooth sine, slow movement).
    /// </summary>
    public static AudioClip BuildIslandLustBgm()
    {
        return BuildLoopedClip("Auto_IslandLustBgm", 8f, (t, n) =>
        {
            float pad = Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.4f
                      + Mathf.Sin(2f * Mathf.PI * 294f * t) * 0.3f
                      + Mathf.Sin(2f * Mathf.PI * 392f * t) * 0.15f;
            float breath = Mathf.Sin(2f * Mathf.PI * 0.25f * t) * 0.2f + 0.8f;
            return pad * 0.18f * breath;
        });
    }

    /// <summary>
    /// Anger: aggressive, pounding (square bass, fast pulse).
    /// </summary>
    public static AudioClip BuildIslandAngerBgm()
    {
        return BuildLoopedClip("Auto_IslandAngerBgm", 6f, (t, n) =>
        {
            float bass = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 2.5f * t)) * 0.55f;
            float distortion = Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.4f
                             + Mathf.Sin(2f * Mathf.PI * 370f * t) * 0.2f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 5f * t);
            return (bass * 0.35f + distortion * 0.2f) * pulse;
        });
    }

    /// <summary>
    /// Ego: grand, majestic brass-like tone (rich harmonics, stately tempo).
    /// </summary>
    public static AudioClip BuildIslandEgoBgm()
    {
        return BuildLoopedClip("Auto_IslandEgoBgm", 8f, (t, n) =>
        {
            float brass = 0f;
            for (int h = 1; h <= 6; h++)
            {
                brass += Mathf.Sin(2f * Mathf.PI * 165f * h * t) / h;
            }
            float stately = Mathf.Sin(2f * Mathf.PI * 0.3f * t) * 0.2f + 0.8f;
            float sub = Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.3f;
            return (brass * 0.2f + sub) * stately;
        });
    }

    // ======================================================================
    //  Internal helpers
    // ======================================================================


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
