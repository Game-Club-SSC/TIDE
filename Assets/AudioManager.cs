using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AudioCue
{
    ExplorationBgm,
    CombatBgm,
    PuzzleBgm,
    EndingBgm,
    CombatVictory,
    CombatDefeat,
    PuzzleSolved,
    BossIntro,
    TravelFanfare,
    ActIIExplorationBgm,
    ActIIIExplorationBgm
}

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip explorationBgm;
    [SerializeField] private AudioClip combatBgm;
    [SerializeField] private AudioClip puzzleBgm;
    [SerializeField] private AudioClip endingBgm;
    [SerializeField] private AudioClip actIIExplorationBgm;
    [SerializeField] private AudioClip actIIIExplorationBgm;

    [Header("Stingers")]
    [SerializeField] private AudioClip combatVictorySting;
    [SerializeField] private AudioClip combatDefeatSting;
    [SerializeField] private AudioClip puzzleSolvedSting;
    [SerializeField] private AudioClip bossIntroSting;
    [SerializeField] private AudioClip travelFanfareSting;

    [Header("Island Audio Profile")]
    [SerializeField, Tooltip("Global default profiles keyed by island id. " +
         "Islands with an IslandAudioProfile on their IslandConfig take priority.")]
    private IslandAudioProfile[] islandProfiles;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool mute = false;
    [SerializeField] private float bgmFadeSeconds = 0.35f;
    [SerializeField] private float stingCooldownSeconds = 0.1f;

    private AudioSource bgmSource;
    private AudioSource stingSource;
    private AudioSource ambientSource;
    private AudioCue? activeCue;
    private IslandAudioProfile activeIslandProfile;
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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.OnStoryActChanged -= HandleStoryActChanged;
            gsm.OnStoryActChanged += HandleStoryActChanged;
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            GameStateManager gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                gsm.OnStoryActChanged -= HandleStoryActChanged;
            }

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

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.volume = 0f;
            ambientSource.spatialBlend = 0f;
        }
    }

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

        if (string.Equals(sceneName, GameStateManager.CombatSceneName, System.StringComparison.Ordinal))
        {
            PlayCue(AudioCue.CombatBgm);
        }
        else if (string.Equals(sceneName, GameStateManager.PuzzleSceneName, System.StringComparison.Ordinal))
        {
            PlayCue(AudioCue.PuzzleBgm);
        }
        else if (string.Equals(sceneName, GameStateManager.MainSceneName, System.StringComparison.Ordinal))
        {
            // Reset BGM pitch to default when returning to exploration
            if (bgmSource != null)
            {
                bgmSource.pitch = 1f;
            }

            GameStateManager gsm = GameStateManager.Instance;
            GameStateManager.StoryAct act = gsm != null ? gsm.CurrentStoryAct : GameStateManager.StoryAct.ActI;
            switch (act)
            {
                case GameStateManager.StoryAct.ActII:
                    PlayCue(AudioCue.ActIIExplorationBgm);
                    break;
                case GameStateManager.StoryAct.ActIII:
                    PlayCue(AudioCue.ActIIIExplorationBgm);
                    break;
                default:
                    PlayCue(AudioCue.ExplorationBgm);
                    break;
            }
        }
    }

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
        else
        {
            PlaySting(cue, clip);
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
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null && !mute)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (stingSource != null && !mute)
        {
            stingSource.volume = sfxVolume;
        }
    }

    public void HandleCombatVictory()
    {
        PlayCue(AudioCue.CombatVictory);
    }

    public void HandleCombatDefeat()
    {
        PlayCue(AudioCue.CombatDefeat);
    }

    public void HandlePuzzleSolved()
    {
        PlayCue(AudioCue.PuzzleSolved);
    }

    public void HandleBossIntro()
    {
        PlayCue(AudioCue.BossIntro);
    }

    public void HandleTravel()
    {
        PlayCue(AudioCue.TravelFanfare);
    }

    public void HandleEndingMusic()
    {
        PlayCue(AudioCue.EndingBgm);
    }

    public void HandleStoryActChanged(GameStateManager.StoryAct act)
    {
        if (activeCue != AudioCue.ExplorationBgm)
        {
            return;
        }

        switch (act)
        {
            case GameStateManager.StoryAct.ActII:
                PlayCue(AudioCue.ActIIExplorationBgm);
                break;
            case GameStateManager.StoryAct.ActIII:
                PlayCue(AudioCue.ActIIIExplorationBgm);
                break;
        }
    }

    // ----- Island-Specific Audio -----

    /// <summary>
    /// Plays exploration BGM and ambient audio for the given island config.
    /// Falls back to default exploration BGM if the island has no audio profile.
    /// </summary>
    public void PlayIslandAudio(IslandConfig islandConfig)
    {
        if (islandConfig == null)
        {
            PlayCue(AudioCue.ExplorationBgm);
            return;
        }

        IslandAudioProfile profile = islandConfig.audioProfile ?? FindIslandProfile(islandConfig.islandId);
        activeIslandProfile = profile;

        if (profile == null)
        {
            PlayCue(AudioCue.ExplorationBgm);
            return;
        }

        EnsureSources();

        // Play island-specific exploration BGM if assigned, else fall back to default
        if (profile.explorationBgm != null)
        {
            ApplyActToneShift(profile);
            PlayBgm(AudioCue.ExplorationBgm, profile.explorationBgm);
            OnCuePlayed?.Invoke(AudioCue.ExplorationBgm);
        }
        else
        {
            PlayCue(AudioCue.ExplorationBgm);
        }

        // Start island ambient loop
        if (profile.ambientLoop != null)
        {
            ambientSource.clip = profile.ambientLoop;
            ambientSource.volume = profile.ambientVolume * BgmVolume;
            ambientSource.Play();
        }
        else
        {
            ambientSource.Stop();
        }
    }

    /// <summary>
    /// Plays combat BGM for the given island, with boss variant if flagged.
    /// </summary>
    public void PlayIslandCombatAudio(IslandConfig islandConfig, bool isBoss)
    {
        if (activeIslandProfile == null)
        {
            PlayCue(isBoss ? AudioCue.BossIntro : AudioCue.CombatBgm);
            return;
        }

        AudioClip clip = isBoss ? activeIslandProfile.bossBgm : activeIslandProfile.combatBgm;
        if (clip == null)
        {
            PlayCue(isBoss ? AudioCue.BossIntro : AudioCue.CombatBgm);
            return;
        }

        EnsureSources();
        ApplyActToneShift(activeIslandProfile);
        PlayBgm(AudioCue.CombatBgm, clip);
        OnCuePlayed?.Invoke(AudioCue.CombatBgm);
    }

    /// <summary>
    /// Plays puzzle BGM for the current island.
    /// </summary>
    public void PlayIslandPuzzleAudio(IslandConfig islandConfig)
    {
        if (activeIslandProfile == null)
        {
            PlayCue(AudioCue.PuzzleBgm);
            return;
        }

        AudioClip clip = activeIslandProfile.puzzleBgm;
        if (clip == null)
        {
            PlayCue(AudioCue.PuzzleBgm);
            return;
        }

        EnsureSources();
        ApplyActToneShift(activeIslandProfile);
        PlayBgm(AudioCue.PuzzleBgm, clip);
        OnCuePlayed?.Invoke(AudioCue.PuzzleBgm);
    }

    /// <summary>
    /// Plays an SFX override from the island profile if available.
    /// Returns true if an island-specific clip was played.
    /// </summary>
    public bool PlayIslandSfx(string sfxKey)
    {
        if (activeIslandProfile == null)
        {
            return false;
        }

        AudioClip clip = null;
        switch (sfxKey)
        {
            case "tidebreak": clip = activeIslandProfile.tideBreakSfx; break;
            case "heal": clip = activeIslandProfile.healSfx; break;
            case "attack_hit": clip = activeIslandProfile.attackHitSfx; break;
        }

        if (clip == null)
        {
            return false;
        }

        EnsureSources();
        stingSource.PlayOneShot(clip, SfxVolume);
        return true;
    }

    /// <summary>
    /// Clears the active island profile, reverting to default audio.
    /// </summary>
    public void ClearIslandAudio()
    {
        activeIslandProfile = null;
        if (ambientSource != null)
        {
            ambientSource.Stop();
        }
        if (bgmSource != null)
        {
            bgmSource.pitch = 1f;
        }
    }

    public IslandAudioProfile ActiveIslandProfile => activeIslandProfile;

    private IslandAudioProfile FindIslandProfile(string islandId)
    {
        if (islandProfiles == null || string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        for (int i = 0; i < islandProfiles.Length; i++)
        {
            if (islandProfiles[i] != null &&
                string.Equals(islandProfiles[i].islandId, islandId, StringComparison.Ordinal))
            {
                return islandProfiles[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Applies act-based pitch and volume modifiers to the BGM source.
    /// Act I is brighter, Act II neutral, Act III somber.
    /// </summary>
    private void ApplyActToneShift(IslandAudioProfile profile)
    {
        if (profile == null || bgmSource == null)
        {
            return;
        }

        int actNumber = 1;
        if (GameStateManager.Instance != null)
        {
            actNumber = (int)GameStateManager.Instance.CurrentStoryAct;
        }

        float pitch = profile.GetActPitch(actNumber);
        bgmSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        float actVolMult = profile.GetActVolumeMultiplier(actNumber);
        bgmSource.volume = BgmVolume * actVolMult;
    }

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

    private AudioClip ResolveClip(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.ExplorationBgm: return GetOrGenerateClip(cue, ref explorationBgm, ProceduralAudioBuilder.BuildExplorationBgm);
            case AudioCue.CombatBgm: return GetOrGenerateClip(cue, ref combatBgm, ProceduralAudioBuilder.BuildCombatBgm);
            case AudioCue.PuzzleBgm: return GetOrGenerateClip(cue, ref puzzleBgm, ProceduralAudioBuilder.BuildPuzzleBgm);
            case AudioCue.EndingBgm: return GetOrGenerateClip(cue, ref endingBgm, ProceduralAudioBuilder.BuildEndingBgm);
            case AudioCue.CombatVictory: return GetOrGenerateClip(cue, ref combatVictorySting, ProceduralAudioBuilder.BuildCombatVictorySting);
            case AudioCue.CombatDefeat: return GetOrGenerateClip(cue, ref combatDefeatSting, ProceduralAudioBuilder.BuildCombatDefeatSting);
            case AudioCue.PuzzleSolved: return GetOrGenerateClip(cue, ref puzzleSolvedSting, ProceduralAudioBuilder.BuildPuzzleSolvedSting);
            case AudioCue.BossIntro: return GetOrGenerateClip(cue, ref bossIntroSting, ProceduralAudioBuilder.BuildBossIntroSting);
            case AudioCue.TravelFanfare: return GetOrGenerateClip(cue, ref travelFanfareSting, ProceduralAudioBuilder.BuildTravelFanfareSting);
            case AudioCue.ActIIExplorationBgm: return GetOrGenerateClip(cue, ref actIIExplorationBgm, ProceduralAudioBuilder.BuildExplorationBgmActII);
            case AudioCue.ActIIIExplorationBgm: return GetOrGenerateClip(cue, ref actIIIExplorationBgm, ProceduralAudioBuilder.BuildExplorationBgmActIII);
            default: return null;
        }
    }

    private AudioClip GetOrGenerateClip(AudioCue cue, ref AudioClip field, System.Func<AudioClip> builder)
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

    private static bool IsBgmCue(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.ExplorationBgm:
            case AudioCue.CombatBgm:
            case AudioCue.PuzzleBgm:
            case AudioCue.EndingBgm:
            case AudioCue.ActIIExplorationBgm:
            case AudioCue.ActIIIExplorationBgm:
                return true;
            default:
                return false;
        }
    }
}
