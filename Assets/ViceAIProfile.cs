using UnityEngine;

/// <summary>
/// Identifies which island vice an enemy belongs to, determining its AI behavior pattern.
/// </summary>
public enum ViceType
{
    Lust,
    Anger,
    Greed,
    Desire,
    Ego,
    Envy
}

/// <summary>
/// ScriptableObject that defines the AI behavior weights for a specific vice type.
/// Each island vice (Lust, Anger, Greed, Desire, Ego, Envy) has distinct combat
/// tendencies driven by these weights. Assign profiles via the Inspector and look
/// them up at runtime through BattleManager.ConfigureViceAI.
/// </summary>
[CreateAssetMenu(fileName = "ViceAIProfile", menuName = "TIDE/Vice AI Profile")]
public class ViceAIProfile : ScriptableObject
{
    [Header("Identity")]
    public ViceType vice;

    [Header("Behavior Weights")]
    [Tooltip("How often enemies attack instead of defending. 1 = always attack, 0 = always defend.")]
    [Range(0f, 1f)]
    public float aggressionWeight;

    [Tooltip("How often enemies use skills when available. 1 = always use skill, 0 = never.")]
    [Range(0f, 1f)]
    public float skillUsageWeight;

    [Tooltip("Probability of targeting the weakest ally instead of a random one.")]
    [Range(0f, 1f)]
    public float targetWeakestWeight;

    [Tooltip("Probability of choosing Defend when HP falls below 30%.")]
    [Range(0f, 1f)]
    public float defendLowHpWeight;

    [Header("Special Mechanics")]
    [Tooltip("Whether this vice has a unique combat gimmick beyond the weight system.")]
    public bool hasViceGimmick;

    [Tooltip("Description of the vice's special combat mechanic.")]
    [TextArea(2, 4)]
    public string gimmickDescription;

    /// <summary>
    /// Creates a runtime ViceAIProfile instance with pre-configured defaults
    /// for the given vice type. Useful for testing or runtime setup.
    /// </summary>
    public static ViceAIProfile CreateDefault(ViceType vice)
    {
        ViceAIProfile profile = CreateInstance<ViceAIProfile>();
        profile.vice = vice;

        switch (vice)
        {
            case ViceType.Lust:
                // High aggression, fixates on a single target, low self-preservation.
                profile.aggressionWeight = 0.80f;
                profile.skillUsageWeight = 0.40f;
                profile.targetWeakestWeight = 0.90f;
                profile.defendLowHpWeight = 0.15f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Fixates on a single target, attacking repeatedly while ignoring other threats. Low self-preservation.";
                break;

            case ViceType.Anger:
                // Very high aggression, AoE-focused, gets stronger as HP drops.
                profile.aggressionWeight = 0.95f;
                profile.skillUsageWeight = 0.60f;
                profile.targetWeakestWeight = 0.20f;
                profile.defendLowHpWeight = 0.05f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Relentless aggression that intensifies as HP drops. Favors AoE attacks and almost never defends.";
                break;

            case ViceType.Greed:
                // Steals buffs and currency, fights defensively when threatened.
                profile.aggressionWeight = 0.50f;
                profile.skillUsageWeight = 0.70f;
                profile.targetWeakestWeight = 0.30f;
                profile.defendLowHpWeight = 0.40f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Steals buffs and currency from players. Hoards resources and fights defensively when threatened.";
                break;

            case ViceType.Desire:
                // Debuff-focused, weakens players before committing to attacks.
                profile.aggressionWeight = 0.40f;
                profile.skillUsageWeight = 0.80f;
                profile.targetWeakestWeight = 0.50f;
                profile.defendLowHpWeight = 0.30f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Weakens players with debuffs before committing to attacks. Prefers control over raw damage.";
                break;

            case ViceType.Ego:
                // Mirrors player abilities, adapts to counter player strategy.
                profile.aggressionWeight = 0.70f;
                profile.skillUsageWeight = 0.60f;
                profile.targetWeakestWeight = 0.40f;
                profile.defendLowHpWeight = 0.30f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Mirrors the player's abilities and element. Adapts to counter the player's strategy. Ties into the Envy Mirror mechanic.";
                break;

            case ViceType.Envy:
                // Copies the player's last used skill at reduced power.
                profile.aggressionWeight = 0.60f;
                profile.skillUsageWeight = 0.50f;
                profile.targetWeakestWeight = 0.50f;
                profile.defendLowHpWeight = 0.30f;
                profile.hasViceGimmick = true;
                profile.gimmickDescription = "Copies the player's last used skill at reduced power. Covets what others have. Ties into the existing Envy Covet mechanic.";
                break;
        }

        return profile;
    }

    public bool IsValid()
    {
        return aggressionWeight >= 0f
            && aggressionWeight <= 1f
            && skillUsageWeight >= 0f
            && skillUsageWeight <= 1f
            && targetWeakestWeight >= 0f
            && targetWeakestWeight <= 1f
            && defendLowHpWeight >= 0f
            && defendLowHpWeight <= 1f;
    }
}
