using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AudioManagerTest : MonoBehaviour
{
    [ContextMenu("Run Audio Manager Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Audio Manager Tests ===");

        TestAudioManagerSingletonSurvivesSceneLoads();
        TestAudioManagerHandlesNullClipsGracefully();
        TestMuteToggleAffectsVolume();
        TestStingCooldownPreventsStacking();
        TestSceneLoadedTriggersExplorationBgm();

        Debug.Log("=== All Audio Manager Tests Passed ===");
    }

    private void TestAudioManagerSingletonSurvivesSceneLoads()
    {
        Debug.Log("Testing AudioManager survives scene loads via DontDestroyOnLoad...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_Survival");

            Assert.IsNotNull(AudioManager.Instance, "AudioManager.Instance should be set after Awake.");
            Assert.AreSame(manager, AudioManager.Instance, "Singleton should reference the same instance after Awake.");

            Object.DontDestroyOnLoad(manager.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  AudioManager singleton survival test passed");
    }

    private void TestAudioManagerHandlesNullClipsGracefully()
    {
        Debug.Log("Testing AudioManager gracefully no-ops when AudioClips are null...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_NullSafe");

            Assert.DoesNotThrow(() => manager.HandleCombatVictory(),
                "HandleCombatVictory should not throw when combat victory sting is null.");
            Assert.DoesNotThrow(() => manager.HandleBossIntro(),
                "HandleBossIntro should not throw when boss intro sting is null.");
            Assert.DoesNotThrow(() => manager.HandlePuzzleSolved(),
                "HandlePuzzleSolved should not throw when puzzle sting is null.");
            Assert.DoesNotThrow(() => manager.HandleTravel(),
                "HandleTravel should not throw when travel sting is null.");

            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.ExplorationBgm),
                "PlayCue(ExplorationBgm) should not throw with null clip.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Null-safe audio hook test passed");
    }

    private void TestMuteToggleAffectsVolume()
    {
        Debug.Log("Testing mute toggle updates reported volume...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_Mute");

            float initialVolume = manager.BgmVolume;
            Assert.Greater(initialVolume, 0f, "BGM volume should be positive before muting.");

            manager.SetMute(true);
            Assert.AreEqual(0f, manager.BgmVolume, "Muted BGM should report zero volume.");
            Assert.AreEqual(0f, manager.SfxVolume, "Muted SFX should report zero volume.");
            Assert.IsTrue(manager.IsMuted, "IsMuted should reflect the mute state.");

            manager.SetMute(false);
            Assert.AreEqual(initialVolume, manager.BgmVolume, "Unmuting should restore BGM volume.");
            Assert.IsFalse(manager.IsMuted, "IsMuted should clear after unmuting.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Mute toggle test passed");
    }

    private void TestStingCooldownPreventsStacking()
    {
        Debug.Log("Testing sting cooldown prevents rapid restacking...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_Cooldown");

            FieldInfo cooldownField = typeof(AudioManager).GetField(
                "stingCooldownSeconds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(cooldownField, "stingCooldownSeconds field not found.");
            cooldownField.SetValue(manager, 5f);

            int initialPlayedCount = manager.ActiveCue.HasValue ? 1 : 0;
            manager.HandleCombatVictory();
            manager.HandleCombatVictory();
            manager.HandleCombatVictory();

            AudioCue? active = manager.ActiveCue;
            Assert.IsTrue(active.HasValue, "ActiveCue should reflect last requested cue.");

            Assert.DoesNotThrow(() => manager.StopBgm(),
                "StopBgm should not throw and should clear active cue.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Sting cooldown test passed");
    }

    private void TestSceneLoadedTriggersExplorationBgm()
    {
        Debug.Log("Testing scene-loaded callback routes to exploration BGM cue...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_Scene");

            FieldInfo handlerField = typeof(AudioManager).GetMethod(
                "HandleSceneLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(handlerField, "HandleSceneLoaded method not found.");

            Scene activeScene = SceneManager.GetActiveScene();
            handlerField.Invoke(manager, new object[] { activeScene, LoadSceneMode.Single });

            Assert.AreEqual(AudioCue.ExplorationBgm, manager.ActiveCue.GetValueOrDefault(AudioCue.CombatBgm),
                "Loading the main scene should set ActiveCue to ExplorationBgm.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Scene-loaded cue routing test passed");
    }

    private static AudioManager CreateIsolatedAudioManager(string objectName)
    {
        Cleanup();
        GameObject audioObject = new GameObject(objectName);
        AudioManager manager = audioObject.AddComponent<AudioManager>();
        Assert.AreSame(manager, AudioManager.Instance,
            "AudioManager.Instance should reference the test instance after Awake.");
        return manager;
    }

    private static void Cleanup()
    {
        if (AudioManager.Instance != null)
        {
            Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }

        AudioManager[] audioManagers = Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
        for (int i = 0; i < audioManagers.Length; i++)
        {
            if (audioManagers[i] != null)
            {
                Object.DestroyImmediate(audioManagers[i].gameObject);
            }
        }
    }
}
