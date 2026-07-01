using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AudioCue
{
    // BGM
    ExplorationBgm,
    CombatBgm,
    PuzzleBgm,
    EndingBgm,
    BossBattleBgm,
    MenuBgm,

    // Per-island BGM
    IslandGreedBgm,
    IslandSlothBgm,
    IslandEnvyBgm,
    IslandLustBgm,
    IslandWrathBgm,
    IslandPrideBgm,
    IslandGluttonyBgm,

    // Stingers
    CombatVictory,
    CombatDefeat,
    PuzzleSolved,
    BossIntro,
    TravelFanfare,
    ActTransition,

    // Combat SFX
    AttackHit,
    AttackMiss,
    AttackCrit,
    Heal,
    TideBreakActivation,
    QTESuccess,
    QTEFail,

    // Interaction SFX
    TileTake,
    TilePlace,
    BoatDepart,
    BoatArrive,
    PuzzleMilestone,

    // UI SFX
    MenuClick,
    MenuOpen,
    MenuClose,
    LevelUp,
    GearEquip,
    DialogueAdvance,
    StatusEffectApply,
    StatusEffectExpire,
    AncientTextFound
}

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string BgmVolumeKey = "Audio_BgmVolume";
    private const string SfxVolumeKey = "Audio_SfxVolume";
    private const string MutedKey = "Audio_Muted";

    public static AudioManager Instance { get; private set; }

    // ----- BGM clips -----
    [Header("Background Music")]
    [SerializeField] private AudioClip explorationBgm;
    [SerializeField] private AudioClip combatBgm;
    [SerializeField] private AudioClip puzzleBgm;
    [SerializeField] private AudioClip endingBgm;
    [SerializeField] private AudioClip bossBattleBgm;
    [SerializeField] private AudioClip menuBgm;

    [Header("Per-Island Music")]
    [SerializeField] private AudioClip islandGreedBgm;
    [SerializeField] private AudioClip islandSlothBgm;
    [SerializeField] private AudioClip islandEnvyBgm;
    [SerializeField] private AudioClip islandLustBgm;
    [SerializeField] private AudioClip islandWrathBgm;
    [SerializeField] private AudioClip islandPrideBgm;
    [SerializeField] private AudioClip islandGluttonyBgm;

    [Header("Stingers")]
    [SerializeField] private AudioClip combatVictorySting;
    [SerializeField] private AudioClip combatDefeatSting;
    [SerializeField] private AudioClip puzzleSolvedSting;
    [SerializeField] private AudioClip bossIntroSting;
    [SerializeField] private AudioClip travelFanfareSting;
    [SerializeField] private AudioClip actTransitionSting;

    [Header("Combat SFX")]
    [SerializeField] private AudioClip attackHitSfx;
    [SerializeField] private AudioClip attackMissSfx;
    [SerializeField] private AudioClip attackCritSfx;
    [SerializeField] private AudioClip healSfx;
    [SerializeField] private AudioClip tideBreakActivationSfx;
    [SerializeField] private AudioClip qteSuccessSfx;
    [SerializeField] private AudioClip qteFailSfx;

    [Header("Interaction SFX")]
    [SerializeField] private AudioClip tileTakeSfx;
    [SerializeField] private AudioClip tilePlaceSfx;
    [SerializeField] private AudioClip boatDepartSfx;
    [SerializeField] private AudioClip boatArriveSfx;
    [SerializeField] private AudioClip puzzleMilestoneSfx;

    [Header("UI SFX")]
    [SerializeField] private AudioClip menuClickSfx;
    [SerializeField] private AudioClip menuOpenSfx;
    [SerializeField] private AudioClip menuCloseSfx;
    [SerializeField] private AudioClip levelUpSfx;
    [SerializeField] private AudioClip gearEquipSfx;
    [SerializeField] private AudioClip dialogueAdvanceSfx;
    [SerializeField] private AudioClip statusEffectApplySfx;
    [SerializeField] private AudioClip statusEffectExpireSfx;
    [SerializeField] private AudioClip ancientTextFoundSfx;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool mute = false;
    [SerializeField] private float bgmFadeSeconds = 0.35f;
    [SerializeField] private float stingCooldownSeconds = 0.1f;

    private AudioSource bgmSource;
    private AudioSource stingSource;
    private AudioSource sfxSource;
    private AudioCue? activeCue;
    private float lastStingTime = -10f;
    private Coroutine bgmFadeRoutine;

    public bool IsMuted => mute;
    public float BgmVolume => mute ? 0f : bgmVolume;
    public float SfxVolume => mute ? 0f : sfxVolume;
    public AudioCue? ActiveCue => activeCue;

    public event Action<AudioCue> OnCuePlayed;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
        EnsureSources();
        ApplyVolumes();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = BgmVolume;
            bgmSource.spatialBlend = 0f;
        }

        if (stingSource == null)
        {
            stingSource = gameObject.AddComponent<AudioSource>();
            stingSource.loop = false;
            stingSource.playOnAwake = false;
            stingSource.volume = SfxVolume;
            stingSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = SfxVolume;
            sfxSource.spatialBlend = 0f;
        }
    }

    // ======================================================================
    //  Scene routing
    // ======================================================================

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        string sceneName = scene.name;
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        if (string.Equals(sceneName, GameStateManager.CombatSceneName, StringComparison.Ordinal))
        {
            PlayCue(AudioCue.CombatBgm);
        }
        else if (string.Equals(sceneName, GameStateManager.PuzzleSceneName, StringComparison.Ordinal))
        {
            PlayCue(AudioCue.PuzzleBgm);
        }
        else if (string.Equals(sceneName, GameStateManager.MainSceneName, StringComparison.Ordinal))
        {
            PlayCue(AudioCue.ExplorationBgm);
        }
        else
        {
            // Per-island BGM routing
            AudioCue? islandCue = ResolveIslandCue(sceneName);
            if (islandCue.HasValue)
            {
                PlayCue(islandCue.Value);
            }
        }
    }

    private static AudioCue? ResolveIslandCue(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        switch (sceneName)
        {
            case "level_greed": return AudioCue.IslandGreedBgm;
            case "level_sloth": return AudioCue.IslandSlothBgm;
            case "level_envy": return AudioCue.IslandEnvyBgm;
            case "level_lust": return AudioCue.IslandLustBgm;
            case "level_wrath": return AudioCue.IslandWrathBgm;
            case "level_pride": return AudioCue.IslandPrideBgm;
            case "level_gluttony": return AudioCue.IslandGluttonyBgm;
            default: return null;
        }
    }

    // ======================================================================
    //  Public play API
    // ======================================================================

    public void PlayCue(AudioCue cue)
    {
        EnsureSources();

        AudioClip clip = ResolveClip(cue);
        if (clip == null)
        {
            activeCue = cue;
            return;
        }

        if (IsBgmCue(cue))
        {
            PlayBgm(cue, clip);
        }
        else if (IsStingCue(cue))
        {
            PlaySting(cue, clip);
        }
        else
        {
            PlaySfx(cue, clip);
        }

        OnCuePlayed?.Invoke(cue);
    }

    public void StopBgm()
    {
        if (bgmSource == null)
        {
            return;
        }

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        bgmSource.Stop();
        activeCue = null;
    }

    // ======================================================================
    //  Convenience handler methods
    // ======================================================================

    // BGM handlers
    public void HandleCombatVictory() { PlayCue(AudioCue.CombatVictory); }
    public void HandleCombatDefeat() { PlayCue(AudioCue.CombatDefeat); }
    public void HandlePuzzleSolved() { PlayCue(AudioCue.PuzzleSolved); }
    public void HandleBossIntro() { PlayCue(AudioCue.BossIntro); }
    public void HandleTravel() { PlayCue(AudioCue.TravelFanfare); }
    public void HandleEndingMusic() { PlayCue(AudioCue.EndingBgm); }
    public void HandleBossBattleBgm() { PlayCue(AudioCue.BossBattleBgm); }
    public void HandleMenuBgm() { PlayCue(AudioCue.MenuBgm); }
    public void HandleActTransition() { PlayCue(AudioCue.ActTransition); }

    // Combat SFX handlers
    public void HandleAttackHit() { PlayCue(AudioCue.AttackHit); }
    public void HandleAttackMiss() { PlayCue(AudioCue.AttackMiss); }
    public void HandleAttackCrit() { PlayCue(AudioCue.AttackCrit); }
    public void HandleHeal() { PlayCue(AudioCue.Heal); }
    public void HandleTideBreakActivation() { PlayCue(AudioCue.TideBreakActivation); }
    public void HandleQTESuccess() { PlayCue(AudioCue.QTESuccess); }
    public void HandleQTEFail() { PlayCue(AudioCue.QTEFail); }

    // Interaction SFX handlers
    public void HandleTileTake() { PlayCue(AudioCue.TileTake); }
    public void HandleTilePlace() { PlayCue(AudioCue.TilePlace); }
    public void HandleBoatDepart() { PlayCue(AudioCue.BoatDepart); }
    public void HandleBoatArrive() { PlayCue(AudioCue.BoatArrive); }
    public void HandlePuzzleMilestone() { PlayCue(AudioCue.PuzzleMilestone); }

    // UI SFX handlers
    public void HandleMenuClick() { PlayCue(AudioCue.MenuClick); }
    public void HandleMenuOpen() { PlayCue(AudioCue.MenuOpen); }
    public void HandleMenuClose() { PlayCue(AudioCue.MenuClose); }
    public void HandleLevelUp() { PlayCue(AudioCue.LevelUp); }
    public void HandleGearEquip() { PlayCue(AudioCue.GearEquip); }
    public void HandleDialogueAdvance() { PlayCue(AudioCue.DialogueAdvance); }
    public void HandleStatusEffectApply() { PlayCue(AudioCue.StatusEffectApply); }
    public void HandleStatusEffectExpire() { PlayCue(AudioCue.StatusEffectExpire); }
    public void HandleAncientTextFound() { PlayCue(AudioCue.AncientTextFound); }

    // ======================================================================
    //  Volume control with PlayerPrefs persistence
    // ======================================================================

    public void SetMute(bool isMuted)
    {
        mute = isMuted;

        if (bgmSource != null)
        {
            bgmSource.mute = isMuted;
        }

        if (stingSource != null)
        {
            stingSource.mute = isMuted;
        }

        if (sfxSource != null)
        {
            sfxSource.mute = isMuted;
        }

        PlayerPrefs.SetInt(MutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null && !mute)
        {
            bgmSource.volume = bgmVolume;
        }

        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (stingSource != null && !mute)
        {
            stingSource.volume = sfxVolume;
        }

        if (sfxSource != null && !mute)
        {
            sfxSource.volume = sfxVolume;
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    // ======================================================================
    //  Internal playback
    // ======================================================================

    private void PlayBgm(AudioCue cue, AudioClip clip)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            activeCue = cue;
            return;
        }

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(CrossfadeBgm(clip));
        activeCue = cue;
    }

    private void PlaySting(AudioCue cue, AudioClip clip)
    {
        if (stingSource == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastStingTime < stingCooldownSeconds)
        {
            return;
        }

        lastStingTime = now;
        stingSource.PlayOneShot(clip, SfxVolume);
    }

    private void PlaySfx(AudioCue cue, AudioClip clip)
    {
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, SfxVolume);
    }

    private IEnumerator CrossfadeBgm(AudioClip clip)
    {
        float elapsed = 0f;
        float startVolume = bgmSource.volume;

        while (elapsed < bgmFadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, bgmFadeSeconds));
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.loop = true;
        bgmSource.Play();

        elapsed = 0f;
        float targetVolume = BgmVolume;
        while (elapsed < bgmFadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, bgmFadeSeconds));
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeRoutine = null;
    }

    // ======================================================================
    //  Clip resolution (get-or-generate pattern)
    // ======================================================================

    private AudioClip ResolveClip(AudioCue cue)
    {
        switch (cue)
        {
            // BGM
            case AudioCue.ExplorationBgm: return GetOrGenerateClip(cue, ref explorationBgm, ProceduralAudioBuilder.BuildExplorationBgm);
            case AudioCue.CombatBgm: return GetOrGenerateClip(cue, ref combatBgm, ProceduralAudioBuilder.BuildCombatBgm);
            case AudioCue.PuzzleBgm: return GetOrGenerateClip(cue, ref puzzleBgm, ProceduralAudioBuilder.BuildPuzzleBgm);
            case AudioCue.EndingBgm: return GetOrGenerateClip(cue, ref endingBgm, ProceduralAudioBuilder.BuildEndingBgm);
            case AudioCue.BossBattleBgm: return GetOrGenerateClip(cue, ref bossBattleBgm, ProceduralAudioBuilder.BuildBossBattleBgm);
            case AudioCue.MenuBgm: return GetOrGenerateClip(cue, ref menuBgm, ProceduralAudioBuilder.BuildMenuBgm);

            // Per-island BGM
            case AudioCue.IslandGreedBgm: return GetOrGenerateClip(cue, ref islandGreedBgm, ProceduralAudioBuilder.BuildIslandGreedBgm);
            case AudioCue.IslandSlothBgm: return GetOrGenerateClip(cue, ref islandSlothBgm, ProceduralAudioBuilder.BuildIslandSlothBgm);
            case AudioCue.IslandEnvyBgm: return GetOrGenerateClip(cue, ref islandEnvyBgm, ProceduralAudioBuilder.BuildIslandEnvyBgm);
            case AudioCue.IslandLustBgm: return GetOrGenerateClip(cue, ref islandLustBgm, ProceduralAudioBuilder.BuildIslandLustBgm);
            case AudioCue.IslandWrathBgm: return GetOrGenerateClip(cue, ref islandWrathBgm, ProceduralAudioBuilder.BuildIslandWrathBgm);
            case AudioCue.IslandPrideBgm: return GetOrGenerateClip(cue, ref islandPrideBgm, ProceduralAudioBuilder.BuildIslandPrideBgm);
            case AudioCue.IslandGluttonyBgm: return GetOrGenerateClip(cue, ref islandGluttonyBgm, ProceduralAudioBuilder.BuildIslandGluttonyBgm);

            // Stingers
            case AudioCue.CombatVictory: return GetOrGenerateClip(cue, ref combatVictorySting, ProceduralAudioBuilder.BuildCombatVictorySting);
            case AudioCue.CombatDefeat: return GetOrGenerateClip(cue, ref combatDefeatSting, ProceduralAudioBuilder.BuildCombatDefeatSting);
            case AudioCue.PuzzleSolved: return GetOrGenerateClip(cue, ref puzzleSolvedSting, ProceduralAudioBuilder.BuildPuzzleSolvedSting);
            case AudioCue.BossIntro: return GetOrGenerateClip(cue, ref bossIntroSting, ProceduralAudioBuilder.BuildBossIntroSting);
            case AudioCue.TravelFanfare: return GetOrGenerateClip(cue, ref travelFanfareSting, ProceduralAudioBuilder.BuildTravelFanfareSting);
            case AudioCue.ActTransition: return GetOrGenerateClip(cue, ref actTransitionSting, ProceduralAudioBuilder.BuildActTransitionSting);

            // Combat SFX
            case AudioCue.AttackHit: return GetOrGenerateClip(cue, ref attackHitSfx, ProceduralAudioBuilder.BuildAttackHitSfx);
            case AudioCue.AttackMiss: return GetOrGenerateClip(cue, ref attackMissSfx, ProceduralAudioBuilder.BuildAttackMissSfx);
            case AudioCue.AttackCrit: return GetOrGenerateClip(cue, ref attackCritSfx, ProceduralAudioBuilder.BuildAttackCritSfx);
            case AudioCue.Heal: return GetOrGenerateClip(cue, ref healSfx, ProceduralAudioBuilder.BuildHealSfx);
            case AudioCue.TideBreakActivation: return GetOrGenerateClip(cue, ref tideBreakActivationSfx, ProceduralAudioBuilder.BuildTideBreakSfx);
            case AudioCue.QTESuccess: return GetOrGenerateClip(cue, ref qteSuccessSfx, ProceduralAudioBuilder.BuildQTESuccessSfx);
            case AudioCue.QTEFail: return GetOrGenerateClip(cue, ref qteFailSfx, ProceduralAudioBuilder.BuildQTEFailSfx);

            // Interaction SFX
            case AudioCue.TileTake: return GetOrGenerateClip(cue, ref tileTakeSfx, ProceduralAudioBuilder.BuildTileTakeSfx);
            case AudioCue.TilePlace: return GetOrGenerateClip(cue, ref tilePlaceSfx, ProceduralAudioBuilder.BuildTilePlaceSfx);
            case AudioCue.BoatDepart: return GetOrGenerateClip(cue, ref boatDepartSfx, ProceduralAudioBuilder.BuildBoatDepartSfx);
            case AudioCue.BoatArrive: return GetOrGenerateClip(cue, ref boatArriveSfx, ProceduralAudioBuilder.BuildBoatArriveSfx);
            case AudioCue.PuzzleMilestone: return GetOrGenerateClip(cue, ref puzzleMilestoneSfx, ProceduralAudioBuilder.BuildPuzzleMilestoneSfx);

            // UI SFX
            case AudioCue.MenuClick: return GetOrGenerateClip(cue, ref menuClickSfx, ProceduralAudioBuilder.BuildMenuClickSfx);
            case AudioCue.MenuOpen: return GetOrGenerateClip(cue, ref menuOpenSfx, ProceduralAudioBuilder.BuildMenuOpenSfx);
            case AudioCue.MenuClose: return GetOrGenerateClip(cue, ref menuCloseSfx, ProceduralAudioBuilder.BuildMenuCloseSfx);
            case AudioCue.LevelUp: return GetOrGenerateClip(cue, ref levelUpSfx, ProceduralAudioBuilder.BuildLevelUpSfx);
            case AudioCue.GearEquip: return GetOrGenerateClip(cue, ref gearEquipSfx, ProceduralAudioBuilder.BuildGearEquipSfx);
            case AudioCue.DialogueAdvance: return GetOrGenerateClip(cue, ref dialogueAdvanceSfx, ProceduralAudioBuilder.BuildDialogueAdvanceSfx);
            case AudioCue.StatusEffectApply: return GetOrGenerateClip(cue, ref statusEffectApplySfx, ProceduralAudioBuilder.BuildStatusEffectApplySfx);
            case AudioCue.StatusEffectExpire: return GetOrGenerateClip(cue, ref statusEffectExpireSfx, ProceduralAudioBuilder.BuildStatusEffectExpireSfx);
            case AudioCue.AncientTextFound: return GetOrGenerateClip(cue, ref ancientTextFoundSfx, ProceduralAudioBuilder.BuildAncientTextSfx);

            default: return null;
        }
    }

    private AudioClip GetOrGenerateClip(AudioCue cue, ref AudioClip field, Func<AudioClip> builder)
    {
        if (field == null)
        {
            field = builder();
            if (field != null)
            {
                field.name = $"AutoAudio_{cue}";
            }
        }
        return field;
    }

    // ======================================================================
    //  Cue classification
    // ======================================================================

    private static bool IsBgmCue(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.ExplorationBgm:
            case AudioCue.CombatBgm:
            case AudioCue.PuzzleBgm:
            case AudioCue.EndingBgm:
            case AudioCue.BossBattleBgm:
            case AudioCue.MenuBgm:
            case AudioCue.IslandGreedBgm:
            case AudioCue.IslandSlothBgm:
            case AudioCue.IslandEnvyBgm:
            case AudioCue.IslandLustBgm:
            case AudioCue.IslandWrathBgm:
            case AudioCue.IslandPrideBgm:
            case AudioCue.IslandGluttonyBgm:
                return true;
            default:
                return false;
        }
    }

    private static bool IsStingCue(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.CombatVictory:
            case AudioCue.CombatDefeat:
            case AudioCue.PuzzleSolved:
            case AudioCue.BossIntro:
            case AudioCue.TravelFanfare:
            case AudioCue.ActTransition:
                return true;
            default:
                return false;
        }
    }

    // ======================================================================
    //  Volume persistence
    // ======================================================================

    private void LoadVolumeSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        mute = PlayerPrefs.GetInt(MutedKey, 0) == 1;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = mute ? 0f : bgmVolume;
            bgmSource.mute = mute;
        }

        if (stingSource != null)
        {
            stingSource.volume = mute ? 0f : sfxVolume;
            stingSource.mute = mute;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = mute ? 0f : sfxVolume;
            sfxSource.mute = mute;
        }
    }
}
