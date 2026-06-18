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
    TravelFanfare
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

    [Header("Stingers")]
    [SerializeField] private AudioClip combatVictorySting;
    [SerializeField] private AudioClip combatDefeatSting;
    [SerializeField] private AudioClip puzzleSolvedSting;
    [SerializeField] private AudioClip bossIntroSting;
    [SerializeField] private AudioClip travelFanfareSting;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool mute = false;
    [SerializeField] private float bgmFadeSeconds = 0.35f;
    [SerializeField] private float stingCooldownSeconds = 0.1f;

    private AudioSource bgmSource;
    private AudioSource stingSource;
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
        EnsureSources();
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
            PlayCue(AudioCue.ExplorationBgm);
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
            case AudioCue.ExplorationBgm: return explorationBgm;
            case AudioCue.CombatBgm: return combatBgm;
            case AudioCue.PuzzleBgm: return puzzleBgm;
            case AudioCue.EndingBgm: return endingBgm;
            case AudioCue.CombatVictory: return combatVictorySting;
            case AudioCue.CombatDefeat: return combatDefeatSting;
            case AudioCue.PuzzleSolved: return puzzleSolvedSting;
            case AudioCue.BossIntro: return bossIntroSting;
            case AudioCue.TravelFanfare: return travelFanfareSting;
            default: return null;
        }
    }

    private static bool IsBgmCue(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.ExplorationBgm:
            case AudioCue.CombatBgm:
            case AudioCue.PuzzleBgm:
            case AudioCue.EndingBgm:
                return true;
            default:
                return false;
        }
    }
}
