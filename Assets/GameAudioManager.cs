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
    // All functionality has been moved to AudioManager.cs
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
