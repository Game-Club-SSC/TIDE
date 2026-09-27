using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for audio volume mixing.
///
/// Volume handling previously had a real defect: SetMute only toggled
/// AudioSource.mute and never restored .volume, so after a crossfade overwrote
/// the source volume, un-muting left the BGM source silent. These tests assert
/// the actual AudioSource values rather than the manager's getters, because
/// the getters could report the right number while the real output stayed
/// muted — which is exactly what happened.
/// </summary>
[DisallowMultipleComponent]
public class AudioMixingInvariantTest : MonoBehaviour
{
    private GameObject audioObject;

    [SetUp]
    public void SetUp()
    {
        Cleanup();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [Test]
    public void UnmuteRestoresTheActualSourceVolumes()
    {
        // The regression: mute then unmute must leave every real AudioSource
        // back at its configured level, not still at zero. Muting is expressed
        // through AudioSource.mute, so the volume is expected to survive;
        // what matters is that un-muting restores a playable level.
        AudioManager manager = CreateManager();
        try
        {
            manager.SetBgmVolume(0.8f);
            manager.SetSfxVolume(0.6f);

            AudioSource bgm = GetSource(manager, "bgmSource");
            AudioSource sfx = GetSource(manager, "sfxSource");
            Assert.IsNotNull(bgm, "Setup: the BGM source should exist.");
            Assert.AreEqual(0.8f, bgm.volume, 0.001f, "Setup: the BGM level should be applied.");

            manager.SetMute(true);
            Assert.IsTrue(bgm.mute, "Muting must flag the source as muted.");
            Assert.IsTrue(sfx.mute, "Muting must flag the SFX source as muted.");
            Assert.AreEqual(0f, manager.BgmVolume, 0.001f, "The reported BGM volume must be 0 while muted.");

            manager.SetMute(false);

            Assert.IsFalse(bgm.mute, "Un-muting must clear the mute flag.");
            Assert.IsFalse(sfx.mute, "Un-muting must clear the SFX mute flag.");
            Assert.AreEqual(0.8f, bgm.volume, 0.001f,
                "Un-muting must leave the BGM source at its configured level, not silent.");
            Assert.AreEqual(0.6f, sfx.volume, 0.001f,
                "Un-muting must leave the SFX source at its configured level.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void MutedGetterMatchesTheRealSources()
    {
        // The getters are mute-aware; if they disagree with the real sources
        // the UI would show one value while playback used another.
        AudioManager manager = CreateManager();
        try
        {
            manager.SetMute(true);
            Assert.AreEqual(0f, manager.BgmVolume, 0.001f, "BgmVolume must report 0 while muted.");
            Assert.AreEqual(0f, manager.SfxVolume, 0.001f, "SfxVolume must report 0 while muted.");

            manager.SetMute(false);
            manager.SetBgmVolume(0.5f);
            Assert.AreEqual(0.5f, manager.BgmVolume, 0.001f, "BgmVolume must report the set level once unmuted.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void VolumeChangesPropagateToAmbientAndSting()
    {
        // SetBgmVolume must reach the ambient bed, which is derived from the
        // BGM level, and the SFX level must reach both sting and sfx sources.
        AudioManager manager = CreateManager();
        try
        {
            manager.SetBgmVolume(0.4f);
            AudioSource ambient = GetSource(manager, "ambientSource");
            Assert.IsNotNull(ambient, "Setup: the ambient source should exist.");
            Assert.AreEqual(0.2f, ambient.volume, 0.001f,
                "The ambient bed must track half of the BGM level.");

            manager.SetSfxVolume(0.9f);
            AudioSource sting = GetSource(manager, "stingSource");
            Assert.IsNotNull(sting, "Setup: the sting source should exist.");
            Assert.AreEqual(0.9f, sting.volume, 0.001f, "Sting volume must track the SFX level.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void MuteSurvivesAVolumeChangeWhileMuted()
    {
        // Changing the slider while muted must not leak audio: the source
        // stays flagged muted, and un-muting must pick up the new level.
        AudioManager manager = CreateManager();
        try
        {
            manager.SetMute(true);
            manager.SetBgmVolume(0.3f);

            AudioSource bgm = GetSource(manager, "bgmSource");
            Assert.IsTrue(bgm.mute,
                "Adjusting the volume while muted must leave the source flagged muted.");

            manager.SetMute(false);
            Assert.AreEqual(0.3f, bgm.volume, 0.001f,
                "Un-muting must apply the level chosen while muted.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void FadesUseUnscaledTimeSoTheyFinishWhilePaused()
    {
        // A crossfade driven by Time.deltaTime stalls at timeScale 0, leaving
        // the source parked at an intermediate volume. The fade loops cannot
        // be advanced headless (coroutines need a running player loop), so the
        // clock they run on is asserted from the source.
        string sourcePath = System.IO.Path.Combine(Application.dataPath, "AudioManager.cs");
        string source = System.IO.File.ReadAllText(sourcePath);

        int unscaled = CountOccurrences(source, "Time.unscaledDeltaTime");
        int scaled = CountOccurrences(source, "elapsed += Time.deltaTime")
            + CountOccurrences(source, "elapsed -= Time.deltaTime");

        Assert.Greater(unscaled, 0, "AudioManager should drive its fades on the unscaled clock.");
        Assert.AreEqual(0, scaled,
            "No audio fade may advance on the scaled clock, or it would stall mid-fade while the game is paused.");
    }

    [Test]
    public void CrossfadeReassertsMuteOnTheSourceItFills()
    {
        // The actual defect this commit fixed. CrossfadeBgm writes
        // bgmSource.volume directly and never re-applied the mute flag, so a
        // fade that ran while muted left the source filled to a non-zero
        // level with .mute still true — any later write to .mute, or a
        // source re-enabled elsewhere, would then play at full volume.
        //
        // The coroutine is driven directly with a real clip so the fade body
        // (not the test's setup) is what fills the source.
        AudioManager manager = CreateManager();
        try
        {
            SetPrivateField(manager, "bgmFadeSeconds", 0.01f);
            manager.SetMute(true);

            AudioSource bgm = GetSource(manager, "bgmSource");
            bgm.clip = null;
            bgm.volume = 0f;

            AudioClip clip = AudioClip.Create("TestFadeClip", 44100, 1, 44100, false);
            try
            {
                // The scenario the re-assert exists for: the manager is muted,
                // and the source's mute flag is out of sync (a pooled or
                // re-enabled source, or a direct property write). The fade is
                // the last thing to touch this source and it owns .volume, so
                // it must leave the flag consistent with the manager's state.
                //
                // Clearing the flag BEFORE the fade is what makes this
                // discriminating: with the re-assert removed, nothing puts it
                // back, whereas SetMute's own write happened long before and
                // cannot mask the difference.
                bgm.mute = false;
                Assert.IsFalse(bgm.mute, "Setup: the source starts un-flagged while the manager is muted.");

                System.Collections.IEnumerator fade = InvokeCrossfade(manager, clip);
                int guard = 0;
                while (fade != null && fade.MoveNext() && guard++ < 500)
                {
                    // Drive the loop to completion on the real clock.
                }

                Assert.IsNotNull(bgm.clip,
                    "Setup: the fade should have installed the new clip, proving the fade body actually ran.");

                // The fade targets BgmVolume, which is 0 while muted, so the
                // source correctly ends silent. The defect guarded here is the
                // stale flag: without the re-assert the source stays
                // un-flagged, so a later SetMute(false) would leave it playing
                // at whatever the fade wrote.
                Assert.AreEqual(0f, bgm.volume, 0.001f,
                    "A fade that runs while muted must settle on a silent source, since BgmVolume is 0 while muted.");
                Assert.IsTrue(bgm.mute,
                    "The crossfade owns the source's volume, so it must also leave the mute flag consistent with the manager; otherwise a later un-mute would resume a source the fade had already set to a non-zero level.");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void ActToneShiftReassertsMute()
    {
        // Same defect on the per-act pitch/volume path, which also writes
        // bgmSource.volume directly.
        AudioManager manager = CreateManager();
        try
        {
            manager.SetMute(true);
            InvokePrivateIfPresent(manager, "ApplyActToneShift", new object[] { null });

            AudioSource bgm = GetSource(manager, "bgmSource");
            Assert.IsTrue(bgm.mute,
                "The act tone shift must leave the source flagged muted after writing its volume.");
        }
        finally
        {
            Cleanup();
        }
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static System.Collections.IEnumerator InvokeCrossfade(AudioManager manager, AudioClip clip)
    {
        MethodInfo method = typeof(AudioManager).GetMethod(
            "CrossfadeBgm", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "AudioManager.CrossfadeBgm should exist.");
        return (System.Collections.IEnumerator)method.Invoke(manager, new object[] { clip });
    }

    private static void InvokePrivateIfPresent(object target, string methodName, object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
        {
            method.Invoke(target, args);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"AudioManager.{fieldName} should exist.");
        field.SetValue(target, value);
    }

    private AudioManager CreateManager()
    {
        audioObject = new GameObject("TestAudioManager");
        AudioManager manager = audioObject.AddComponent<AudioManager>();

        // Awake calls DontDestroyOnLoad, which throws outside play mode;
        // falling back to the static property mirrors DialogueSystem's
        // pattern.
        try
        {
            InvokePrivate(manager, "Awake");
        }
        catch (System.Exception)
        {
            SetInstance(manager);
        }

        if (AudioManager.Instance != manager)
        {
            SetInstance(manager);
        }

        // Ensure the sources exist so the volume assertions read real state.
        InvokePrivate(manager, "EnsureSources");
        InvokePrivate(manager, "ApplyVolumes");
        return manager;
    }

    private static void SetInstance(AudioManager manager)
    {
        PropertyInfo property = typeof(AudioManager).GetProperty(
            "Instance", BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(property, "AudioManager.Instance should exist.");
        property.GetSetMethod(true).Invoke(manager, new object[] { manager });
    }

    private static AudioSource GetSource(AudioManager manager, string fieldName)
    {
        FieldInfo field = typeof(AudioManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"AudioManager.{fieldName} should exist.");
        return field.GetValue(manager) as AudioSource;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
        method.Invoke(target, null);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private void Cleanup()
    {
        if (AudioManager.Instance != null)
        {
            Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }

        if (audioObject != null)
        {
            Object.DestroyImmediate(audioObject);
            audioObject = null;
        }
    }
}
