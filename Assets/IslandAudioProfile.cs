using UnityEngine;

[CreateAssetMenu(fileName = "IslandAudioProfile", menuName = "TIDE/Island Audio Profile")]
public class IslandAudioProfile : ScriptableObject
{
    [Header("Island Identity")]
    [Tooltip("Must match IslandConfig.islandId for lookup.")]
    public string islandId = "island_lust";

    [Header("Exploration BGM")]
    public AudioClip explorationBgm;

    [Header("Combat BGM")]
    public AudioClip combatBgm;

    [Header("Puzzle BGM")]
    public AudioClip puzzleBgm;

    [Header("Boss BGM")]
    public AudioClip bossBgm;

    [Header("Ambient / Atmosphere")]
    [Tooltip("Looping ambient clip played while exploring this island.")]
    public AudioClip ambientLoop;
    [Range(0f, 1f)] public float ambientVolume = 0.5f;

    [Header("SFX Overrides")]
    [Tooltip("Played on Tide Break activation for this island.")]
    public AudioClip tideBreakSfx;
    [Tooltip("Played on healing action for this island.")]
    public AudioClip healSfx;
    [Tooltip("Played on attack hit for this island.")]
    public AudioClip attackHitSfx;
    [Tooltip("Played on attack miss for this island.")]
    public AudioClip attackMissSfx;
    [Tooltip("Played on attack crit for this island.")]
    public AudioClip attackCritSfx;
    [Tooltip("Played on tile take for this island.")]
    public AudioClip tileTakeSfx;
    [Tooltip("Played on tile place for this island.")]
    public AudioClip tilePlaceSfx;
    [Tooltip("Played on boat depart for this island.")]
    public AudioClip boatDepartSfx;
    [Tooltip("Played on boat arrive for this island.")]
    public AudioClip boatArriveSfx;
    [Tooltip("Played on menu click for this island.")]
    public AudioClip menuClickSfx;
    [Tooltip("Played on level up for this island.")]
    public AudioClip levelUpSfx;
    [Tooltip("Played on dialogue advance for this island.")]
    public AudioClip dialogueAdvanceSfx;
    [Tooltip("Played on status effect apply for this island.")]
    public AudioClip statusEffectApplySfx;
    [Tooltip("Played on status effect expire for this island.")]
    public AudioClip statusEffectExpireSfx;
    [Tooltip("Played on ancient text found for this island.")]
    public AudioClip ancientTextFoundSfx;

    [Header("Act-Based Tone Shift")]
    [Tooltip("Pitch multiplier per act: index 0 = Act I, 1 = Act II, 2 = Act III. " +
             "Values >1 brighten, <1 darken. Applied to BGM AudioSource.")]
    public float[] actPitchMultipliers = new float[] { 1.05f, 1.0f, 0.92f };

    [Tooltip("Volume multiplier per act: index 0 = Act I, 1 = Act II, 2 = Act III. " +
             "Applied on top of the base bgm volume.")]
    public float[] actVolumeMultipliers = new float[] { 1.0f, 0.85f, 0.7f };

    /// <summary>
    /// Returns the pitch multiplier for the given act (1-indexed: 1=ActI, 2=ActII, 3=ActIII).
    /// Falls back to 1.0 if the array is not populated for that act.
    /// </summary>
    public float GetActPitch(int actNumber)
    {
        int index = actNumber - 1;
        if (actPitchMultipliers == null || index < 0 || index >= actPitchMultipliers.Length)
            return 1f;
        return actPitchMultipliers[index];
    }

    /// <summary>
    /// Returns the volume multiplier for the given act (1-indexed).
    /// Falls back to 1.0 if the array is not populated for that act.
    /// </summary>
    public float GetActVolumeMultiplier(int actNumber)
    {
        int index = actNumber - 1;
        if (actVolumeMultipliers == null || index < 0 || index >= actVolumeMultipliers.Length)
            return 1f;
        return actVolumeMultipliers[index];
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(islandId);
    }
}
