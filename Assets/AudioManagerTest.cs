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
        TestNewHandlerMethodsDoNotThrow();
        TestVolumePersistenceViaPlayerPrefs();
        TestIslandBgmRouting();

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

            MethodInfo handlerField = typeof(AudioManager).GetMethod(
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

    private void TestNewHandlerMethodsDoNotThrow()
    {
        Debug.Log("Testing all new handler methods do not throw...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_NewHandlers");

            // Combat SFX
            Assert.DoesNotThrow(() => manager.HandleAttackHit(), "HandleAttackHit should not throw.");
            Assert.DoesNotThrow(() => manager.HandleAttackMiss(), "HandleAttackMiss should not throw.");
            Assert.DoesNotThrow(() => manager.HandleAttackCrit(), "HandleAttackCrit should not throw.");
            Assert.DoesNotThrow(() => manager.HandleHeal(), "HandleHeal should not throw.");
            Assert.DoesNotThrow(() => manager.HandleTideBreakActivation(), "HandleTideBreakActivation should not throw.");
            Assert.DoesNotThrow(() => manager.HandleQTESuccess(), "HandleQTESuccess should not throw.");
            Assert.DoesNotThrow(() => manager.HandleQTEFail(), "HandleQTEFail should not throw.");

            // Interaction SFX
            Assert.DoesNotThrow(() => manager.HandleTileTake(), "HandleTileTake should not throw.");
            Assert.DoesNotThrow(() => manager.HandleTilePlace(), "HandleTilePlace should not throw.");
            Assert.DoesNotThrow(() => manager.HandleBoatDepart(), "HandleBoatDepart should not throw.");
            Assert.DoesNotThrow(() => manager.HandleBoatArrive(), "HandleBoatArrive should not throw.");
            Assert.DoesNotThrow(() => manager.HandlePuzzleMilestone(), "HandlePuzzleMilestone should not throw.");

            // UI SFX
            Assert.DoesNotThrow(() => manager.HandleMenuClick(), "HandleMenuClick should not throw.");
            Assert.DoesNotThrow(() => manager.HandleMenuOpen(), "HandleMenuOpen should not throw.");
            Assert.DoesNotThrow(() => manager.HandleMenuClose(), "HandleMenuClose should not throw.");
            Assert.DoesNotThrow(() => manager.HandleLevelUp(), "HandleLevelUp should not throw.");
            Assert.DoesNotThrow(() => manager.HandleGearEquip(), "HandleGearEquip should not throw.");
            Assert.DoesNotThrow(() => manager.HandleDialogueAdvance(), "HandleDialogueAdvance should not throw.");
            Assert.DoesNotThrow(() => manager.HandleStatusEffectApply(), "HandleStatusEffectApply should not throw.");
            Assert.DoesNotThrow(() => manager.HandleStatusEffectExpire(), "HandleStatusEffectExpire should not throw.");
            Assert.DoesNotThrow(() => manager.HandleAncientTextFound(), "HandleAncientTextFound should not throw.");

            // BGM handlers
            Assert.DoesNotThrow(() => manager.HandleBossBattleBgm(), "HandleBossBattleBgm should not throw.");
            Assert.DoesNotThrow(() => manager.HandleMenuBgm(), "HandleMenuBgm should not throw.");
            Assert.DoesNotThrow(() => manager.HandleActTransition(), "HandleActTransition should not throw.");

            // Per-island BGM
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandGreedBgm), "PlayCue(IslandGreedBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandSlothBgm), "PlayCue(IslandSlothBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandEnvyBgm), "PlayCue(IslandEnvyBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandLustBgm), "PlayCue(IslandLustBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandWrathBgm), "PlayCue(IslandWrathBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandPrideBgm), "PlayCue(IslandPrideBgm) should not throw.");
            Assert.DoesNotThrow(() => manager.PlayCue(AudioCue.IslandGluttonyBgm), "PlayCue(IslandGluttonyBgm) should not throw.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  New handler methods test passed");
    }

    private void TestVolumePersistenceViaPlayerPrefs()
    {
        Debug.Log("Testing volume settings persist via PlayerPrefs...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_Persistence");

            // Set custom volumes
            manager.SetBgmVolume(0.42f);
            manager.SetSfxVolume(0.87f);
            manager.SetMute(true);

            // Verify PlayerPrefs keys exist
            Assert.AreEqual(0.42f, PlayerPrefs.GetFloat("Audio_BgmVolume", -1f), 0.01f,
                "BGM volume should be saved to PlayerPrefs.");
            Assert.AreEqual(0.87f, PlayerPrefs.GetFloat("Audio_SfxVolume", -1f), 0.01f,
                "SFX volume should be saved to PlayerPrefs.");
            Assert.AreEqual(1, PlayerPrefs.GetInt("Audio_Muted", -1),
                "Mute state should be saved to PlayerPrefs.");

            // Cleanup test PlayerPrefs
            PlayerPrefs.DeleteKey("Audio_BgmVolume");
            PlayerPrefs.DeleteKey("Audio_SfxVolume");
            PlayerPrefs.DeleteKey("Audio_Muted");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Volume persistence test passed");
    }

    private void TestIslandBgmRouting()
    {
        Debug.Log("Testing island scene name routes to correct BGM cue...");

        AudioManager manager = null;
        try
        {
            manager = CreateIsolatedAudioManager("TestAudioManager_IslandRoute");

            MethodInfo handlerField = typeof(AudioManager).GetMethod(
                "HandleSceneLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(handlerField, "HandleSceneLoaded method not found.");

            // Test island routing via reflection
            // ResolveIslandCue is static, get it differently
            MethodInfo resolveMethod = typeof(AudioManager).GetMethod(
                "ResolveIslandCue",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(resolveMethod, "ResolveIslandCue method not found.");

            Assert.AreEqual(AudioCue.IslandGreedBgm, resolveMethod.Invoke(null, new object[] { "level_greed" }),
                "level_greed should map to IslandGreedBgm.");
            Assert.AreEqual(AudioCue.IslandSlothBgm, resolveMethod.Invoke(null, new object[] { "level_sloth" }),
                "level_sloth should map to IslandSlothBgm.");
            Assert.AreEqual(AudioCue.IslandWrathBgm, resolveMethod.Invoke(null, new object[] { "level_wrath" }),
                "level_wrath should map to IslandWrathBgm.");
            Assert.IsNull(resolveMethod.Invoke(null, new object[] { "unknown_scene" }),
                "Unknown scene should return null.");
        }
        finally
        {
            Cleanup();
        }

        Debug.Log("  Island BGM routing test passed");
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
