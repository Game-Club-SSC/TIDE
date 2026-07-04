using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton audio manager that controls music, SFX, and ambient audio.
/// All methods log their calls so designers can see what's being triggered.
/// Audio clips are serialized placeholders -- assign real clips in the Inspector.
///
/// NOTE: This class is superseded by AudioManager, which now includes all
/// SFX types, per-island BGM, act transitions, and PlayerPrefs volume
/// persistence.保留 for reference only.
/// </summary>
[System.Obsolete("Use AudioManager instead. GameAudioManager is retained for reference only.")]
[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Audio_MasterVolume";
    private const string MusicVolumeKey = "Audio_MusicVolume";
    private const string SfxVolumeKey = "Audio_SFXVolume";
    private const string AmbientVolumeKey = "Audio_AmbientVolume";
    private const float DefaultCrossfadeDuration = 0.8f;

    public static GameAudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 0.5f;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip bossBattleMusic;
    [SerializeField] private AudioClip puzzleMusic;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip endingMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip menuOpenClip;
    [SerializeField] private AudioClip menuCloseClip;
    [SerializeField] private AudioClip tideTakeClip;
    [SerializeField] private AudioClip tidePlaceClip;
    [SerializeField] private AudioClip puzzleSolvedClip;
    [SerializeField] private AudioClip combatHitClip;
    [SerializeField] private AudioClip combatMissClip;
    [SerializeField] private AudioClip combatCritClip;
    [SerializeField] private AudioClip tideBreakUnleashClip;
    [SerializeField] private AudioClip clashWinClip;
    [SerializeField] private AudioClip clashLoseClip;
    [SerializeField] private AudioClip qteSuccessClip;
    [SerializeField] private AudioClip qteFailClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip gearEquipClip;
    [SerializeField] private AudioClip healClip;
    [SerializeField] private AudioClip ancientTextFoundClip;
    [SerializeField] private AudioClip dialogueAdvanceClip;
    [SerializeField] private AudioClip boatDepartClip;
    [SerializeField] private AudioClip boatArriveClip;
    [SerializeField] private AudioClip statusEffectApplyClip;
    [SerializeField] private AudioClip statusEffectExpireClip;

    [Header("Ambient Clips")]
    [SerializeField] private AudioClip windClip;
    [SerializeField] private AudioClip rainClip;
    [SerializeField] private AudioClip oceanClip;
    [SerializeField] private AudioClip forestClip;
    [SerializeField] private AudioClip cavesClip;

    private MusicTrack currentTrack = MusicTrack.Exploration;
    private Coroutine crossfadeCoroutine;
    private bool isMuted;
    private float savedMusicVolume;
    private float savedAmbientVolume;

    public MusicTrack CurrentTrack => currentTrack;
    public bool IsMuted => isMuted;

    // ----- Lifecycle -----

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameAudioManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        LoadVolumeSettings();
        ApplyVolumes();

        Debug.Log("[GameAudioManager] Initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ----- Audio Source Setup -----

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = CreateAudioSource("MusicSource", 0.8f);
        }

        if (sfxSource == null)
        {
            sfxSource = CreateAudioSource("SFXSource", 0f);
            sfxSource.loop = false;
        }

        if (ambientSource == null)
        {
            ambientSource = CreateAudioSource("AmbientSource", 0f);
            ambientSource.loop = true;
        }
    }

    private AudioSource CreateAudioSource(string sourceName, float spatialBlend)
    {
        GameObject sourceObj = new GameObject(sourceName);
        sourceObj.transform.SetParent(transform, false);
        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        return source;
    }

    // ----- Music -----

    public void PlayMusic(MusicTrack track, bool crossfade = true)
    {
        Debug.Log($"[GameAudioManager] PlayMusic: {track} (crossfade={crossfade})");

        AudioClip clip = GetMusicClip(track);
        if (clip == null)
        {
            Debug.LogWarning($"[GameAudioManager] No clip assigned for music track: {track}");
            return;
        }

        currentTrack = track;

        if (crossfade && musicSource.isPlaying)
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip, DefaultCrossfadeDuration));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        Debug.Log($"[GameAudioManager] StopMusic (fadeOut={fadeOut})");

        if (!musicSource.isPlaying)
        {
            return;
        }

        if (fadeOut)
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(FadeOutMusic(DefaultCrossfadeDuration));
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void FadeMusic(float targetVolume, float duration)
    {
        Debug.Log($"[GameAudioManager] FadeMusic: target={targetVolume:F2}, duration={duration:F1}s");

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(FadeToVolume(targetVolume, duration));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        if (duration <= 0f)
        {
            musicSource.clip = newClip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
            crossfadeCoroutine = null;
            yield break;
        }

        float startVolume = musicSource.volume;
        float halfDuration = duration * 0.5f;

        // Fade out current track
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
            yield return null;
        }

        // Swap clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new track
        elapsed = 0f;
        float targetVolume = musicVolume * masterVolume;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / halfDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        crossfadeCoroutine = null;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        if (duration <= 0f)
        {
            musicSource.Stop();
            musicSource.volume = 0f;
            crossfadeCoroutine = null;
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
        crossfadeCoroutine = null;
    }

    private IEnumerator FadeToVolume(float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            musicSource.volume = targetVolume * masterVolume;
            crossfadeCoroutine = null;
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume * masterVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume * masterVolume;
        crossfadeCoroutine = null;
    }

    private AudioClip GetMusicClip(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.Exploration: return explorationMusic;
            case MusicTrack.Battle: return battleMusic;
            case MusicTrack.BossBattle: return bossBattleMusic;
            case MusicTrack.Puzzle: return puzzleMusic;
            case MusicTrack.Menu: return menuMusic;
            case MusicTrack.Ending: return endingMusic;
            default: return null;
        }
    }

    // ----- SFX -----

    public void PlaySFX(SFXType type)
    {
        Debug.Log($"[GameAudioManager] PlaySFX: {type}");

        AudioClip clip = GetSFXClip(type);
        if (clip == null)
        {
            Debug.LogWarning($"[GameAudioManager] No clip assigned for SFX type: {type}");
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    public void PlaySFXAtPosition(SFXType type, Vector3 position)
    {
        Debug.Log($"[GameAudioManager] PlaySFXAtPosition: {type} at {position}");

        AudioClip clip = GetSFXClip(type);
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
    }

    private AudioClip GetSFXClip(SFXType type)
    {
        switch (type)
        {
            case SFXType.ButtonClick: return buttonClickClip;
            case SFXType.MenuOpen: return menuOpenClip;
            case SFXType.MenuClose: return menuCloseClip;
            case SFXType.TideTake: return tideTakeClip;
            case SFXType.TidePlace: return tidePlaceClip;
            case SFXType.PuzzleSolved: return puzzleSolvedClip;
            case SFXType.CombatHit: return combatHitClip;
            case SFXType.CombatMiss: return combatMissClip;
            case SFXType.CombatCrit: return combatCritClip;
            case SFXType.TideBreakUnleash: return tideBreakUnleashClip;
            case SFXType.ClashWin: return clashWinClip;
            case SFXType.ClashLose: return clashLoseClip;
            case SFXType.QTESuccess: return qteSuccessClip;
            case SFXType.QTEFail: return qteFailClip;
            case SFXType.LevelUp: return levelUpClip;
            case SFXType.GearEquip: return gearEquipClip;
            case SFXType.Heal: return healClip;
            case SFXType.AncientTextFound: return ancientTextFoundClip;
            case SFXType.DialogueAdvance: return dialogueAdvanceClip;
            case SFXType.BoatDepart: return boatDepartClip;
            case SFXType.BoatArrive: return boatArriveClip;
            case SFXType.StatusEffectApply: return statusEffectApplyClip;
            case SFXType.StatusEffectExpire: return statusEffectExpireClip;
            default: return null;
        }
    }

    // ----- Ambient -----

    public void PlayAmbient(AmbientType type)
    {
        Debug.Log($"[GameAudioManager] PlayAmbient: {type}");

        if (type == AmbientType.None)
        {
            ambientSource.Stop();
            return;
        }

        AudioClip clip = GetAmbientClip(type);
        if (clip == null)
        {
            Debug.LogWarning($"[GameAudioManager] No clip assigned for ambient type: {type}");
            return;
        }

        if (ambientSource.clip == clip && ambientSource.isPlaying)
        {
            return;
        }

        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume * masterVolume;
        ambientSource.Play();
    }

    public void StopAmbient(bool fadeOut = true)
    {
        Debug.Log($"[GameAudioManager] StopAmbient (fadeOut={fadeOut})");

        if (fadeOut)
        {
            StartCoroutine(FadeOutAmbient(DefaultCrossfadeDuration));
        }
        else
        {
            ambientSource.Stop();
        }
    }

    private IEnumerator FadeOutAmbient(float duration)
    {
        if (duration <= 0f)
        {
            ambientSource.Stop();
            ambientSource.volume = 0f;
            yield break;
        }

        float startVolume = ambientSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        ambientSource.Stop();
        ambientSource.volume = 0f;
    }

    private AudioClip GetAmbientClip(AmbientType type)
    {
        switch (type)
        {
            case AmbientType.Wind: return windClip;
            case AmbientType.Rain: return rainClip;
            case AmbientType.Ocean: return oceanClip;
            case AmbientType.Forest: return forestClip;
            case AmbientType.Caves: return cavesClip;
            default: return null;
        }
    }

    // ----- Volume Control -----

    public void SetVolume(VolumeChannel channel, float volume)
    {
        volume = Mathf.Clamp01(volume);
        Debug.Log($"[GameAudioManager] SetVolume: {channel} = {volume:F2}");

        switch (channel)
        {
            case VolumeChannel.Master:
                masterVolume = volume;
                break;
            case VolumeChannel.Music:
                musicVolume = volume;
                break;
            case VolumeChannel.SFX:
                sfxVolume = volume;
                break;
            case VolumeChannel.Ambient:
                ambientVolume = volume;
                break;
        }

        ApplyVolumes();
        SaveVolumeSettings();
    }

    public float GetVolume(VolumeChannel channel)
    {
        switch (channel)
        {
            case VolumeChannel.Master: return masterVolume;
            case VolumeChannel.Music: return musicVolume;
            case VolumeChannel.SFX: return sfxVolume;
            case VolumeChannel.Ambient: return ambientVolume;
            default: return 0f;
        }
    }

    private void ApplyVolumes()
    {
        float effectiveMusicVol = musicVolume * masterVolume;
        float effectiveSfxVol = sfxVolume * masterVolume;
        float effectiveAmbientVol = ambientVolume * masterVolume;

        if (musicSource != null)
        {
            musicSource.volume = effectiveMusicVol;
        }

        if (sfxSource != null)
        {
            // OneShot volume is set at call time, but we apply master to the source itself too
            sfxSource.volume = effectiveSfxVol;
        }

        if (ambientSource != null)
        {
            ambientSource.volume = effectiveAmbientVol;
        }
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetFloat(AmbientVolumeKey, ambientVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        ambientVolume = PlayerPrefs.GetFloat(AmbientVolumeKey, 0.5f);
    }

    // ----- Mute -----

    public void MuteAll()
    {
        Debug.Log("[GameAudioManager] MuteAll");
        isMuted = true;

        savedMusicVolume = musicVolume;
        savedAmbientVolume = ambientVolume;

        if (musicSource != null)
        {
            musicSource.volume = 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 0f;
        }

        if (ambientSource != null)
        {
            ambientSource.volume = 0f;
        }
    }

    public void UnmuteAll()
    {
        Debug.Log("[GameAudioManager] UnmuteAll");
        isMuted = false;

        musicVolume = savedMusicVolume;
        ambientVolume = savedAmbientVolume;
        ApplyVolumes();
    }

    public void ToggleMute()
    {
        if (isMuted)
        {
            UnmuteAll();
        }
        else
        {
            MuteAll();
        }
    }
}

// ----- Enums -----

public enum MusicTrack
{
    Exploration,
    Battle,
    BossBattle,
    Puzzle,
    Menu,
    Ending
}

public enum SFXType
{
    ButtonClick,
    MenuOpen,
    MenuClose,
    TideTake,
    TidePlace,
    PuzzleSolved,
    CombatHit,
    CombatMiss,
    CombatCrit,
    TideBreakUnleash,
    ClashWin,
    ClashLose,
    QTESuccess,
    QTEFail,
    LevelUp,
    GearEquip,
    Heal,
    AncientTextFound,
    DialogueAdvance,
    BoatDepart,
    BoatArrive,
    StatusEffectApply,
    StatusEffectExpire
}

public enum AmbientType
{
    None,
    Wind,
    Rain,
    Ocean,
    Forest,
    Caves
}

public enum VolumeChannel
{
    Master,
    Music,
    SFX,
    Ambient
}
