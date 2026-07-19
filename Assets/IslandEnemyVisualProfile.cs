using UnityEngine;

public enum EnemyBodyShape
{
    Humanoid,
    Beast,
    Slime,
    Elemental,
    Construct
}

public enum ParticleSystemType
{
    None,
    Corruption,
    Sparks,
    Smoke,
    Embers
}

[CreateAssetMenu(fileName = "IslandEnemyVisualProfile", menuName = "TIDE/Island Enemy Visual Profile")]
public class IslandEnemyVisualProfile : ScriptableObject
{
    public string islandId;

    [Header("Colors")]
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.gray;
    public Color eyeColor = Color.yellow;
    public Color corruptionColor = new Color(0.4f, 0f, 0.6f);

    [Header("Scale")]
    public Vector3 baseScale = Vector3.one;
    public float bossScaleMultiplier = 1.5f;

    [Header("Shapes")]
    public EnemyBodyShape bodyShape = EnemyBodyShape.Humanoid;

    [Header("Effects")]
    public bool hasCorruptionAura;
    public float auraIntensity = 0.5f;
    public ParticleSystemType idleParticle = ParticleSystemType.None;

    [Header("Movement")]
    public float movementSpeedMultiplier = 1f;
    public bool floatsAboveGround;
    public float floatHeight = 0.3f;

    /// <summary>
    /// Returns a pre-configured profile for the given island id.
    /// Returns null if the island id is not one of the six island themes.
    /// </summary>
    public static IslandEnemyVisualProfile GetDefault(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        IslandEnemyVisualProfile profile = CreateInstance<IslandEnemyVisualProfile>();
        profile.islandId = islandId;

        switch (islandId)
        {
            case "island_lust":
                profile.primaryColor = new Color(0.72f, 0.11f, 0.18f);        // Deep crimson
                profile.secondaryColor = new Color(0.85f, 0.69f, 0.22f);      // Gold accents
                profile.eyeColor = new Color(0.95f, 0.75f, 0.30f);            // Warm gold
                profile.corruptionColor = new Color(0.80f, 0.10f, 0.35f);     // Rose corruption
                profile.baseScale = new Vector3(1f, 1f, 1f);
                profile.bossScaleMultiplier = 1.4f;
                profile.bodyShape = EnemyBodyShape.Elemental;
                profile.hasCorruptionAura = true;
                profile.auraIntensity = 0.6f;
                profile.idleParticle = ParticleSystemType.Sparks;
                profile.movementSpeedMultiplier = 1.1f;
                profile.floatsAboveGround = true;
                profile.floatHeight = 0.25f;
                break;

            case "island_greed":
                profile.primaryColor = new Color(0.78f, 0.62f, 0.18f);        // Metallic gold/bronze
                profile.secondaryColor = new Color(0.65f, 0.15f, 0.30f);      // Gem-like ruby
                profile.eyeColor = new Color(0.20f, 0.90f, 0.75f);            // Gem teal
                profile.corruptionColor = new Color(0.60f, 0.45f, 0.10f);     // Tarnished gold
                profile.baseScale = new Vector3(1.1f, 1.1f, 1.1f);
                profile.bossScaleMultiplier = 1.5f;
                profile.bodyShape = EnemyBodyShape.Construct;
                profile.hasCorruptionAura = false;
                profile.auraIntensity = 0.3f;
                profile.idleParticle = ParticleSystemType.Sparks;
                profile.movementSpeedMultiplier = 0.9f;
                profile.floatsAboveGround = false;
                profile.floatHeight = 0f;
                break;

            case "island_desire":
                profile.primaryColor = new Color(0.45f, 0.35f, 0.52f);        // Muted purple
                profile.secondaryColor = new Color(0.55f, 0.52f, 0.58f);      // Grey-lavender
                profile.eyeColor = new Color(0.70f, 0.65f, 0.78f);            // Dull lilac
                profile.corruptionColor = new Color(0.30f, 0.22f, 0.40f);     // Deep violet
                profile.baseScale = new Vector3(0.9f, 0.8f, 0.9f);
                profile.bossScaleMultiplier = 1.6f;
                profile.bodyShape = EnemyBodyShape.Slime;
                profile.hasCorruptionAura = true;
                profile.auraIntensity = 0.3f;
                profile.idleParticle = ParticleSystemType.Smoke;
                profile.movementSpeedMultiplier = 0.5f;
                profile.floatsAboveGround = true;
                profile.floatHeight = 0.15f;
                break;

            case "island_anger":
                profile.primaryColor = new Color(0.90f, 0.25f, 0.10f);        // Fiery red-orange
                profile.secondaryColor = new Color(0.12f, 0.10f, 0.10f);      // Black accents
                profile.eyeColor = new Color(1.0f, 0.60f, 0.10f);             // Molten orange
                profile.corruptionColor = new Color(0.85f, 0.15f, 0.05f);     // Blood red
                profile.baseScale = new Vector3(1.15f, 1.15f, 1.15f);
                profile.bossScaleMultiplier = 1.6f;
                profile.bodyShape = EnemyBodyShape.Humanoid;
                profile.hasCorruptionAura = true;
                profile.auraIntensity = 0.8f;
                profile.idleParticle = ParticleSystemType.Embers;
                profile.movementSpeedMultiplier = 1.3f;
                profile.floatsAboveGround = false;
                profile.floatHeight = 0f;
                break;

            case "island_envy":
                profile.primaryColor = new Color(0.30f, 0.62f, 0.28f);        // Sickly green
                profile.secondaryColor = new Color(0.75f, 0.85f, 0.80f);      // Mirror-like silver
                profile.eyeColor = new Color(0.50f, 0.95f, 0.45f);            // Jealous green
                profile.corruptionColor = new Color(0.20f, 0.45f, 0.18f);     // Rot green
                profile.baseScale = new Vector3(1f, 1f, 1f);
                profile.bossScaleMultiplier = 1.5f;
                profile.bodyShape = EnemyBodyShape.Elemental;
                profile.hasCorruptionAura = true;
                profile.auraIntensity = 0.5f;
                profile.idleParticle = ParticleSystemType.Corruption;
                profile.movementSpeedMultiplier = 1.0f;
                profile.floatsAboveGround = true;
                profile.floatHeight = 0.35f;
                break;

            case "island_ego":
                profile.primaryColor = new Color(0.95f, 0.92f, 0.80f);        // Brilliant white-gold
                profile.secondaryColor = new Color(0.82f, 0.68f, 0.15f);      // Regal gold
                profile.eyeColor = new Color(0.95f, 0.85f, 0.30f);            // Royal amber
                profile.corruptionColor = new Color(0.70f, 0.60f, 0.10f);     // Corroded gold
                profile.baseScale = new Vector3(1.1f, 1.25f, 1.1f);
                profile.bossScaleMultiplier = 1.8f;
                profile.bodyShape = EnemyBodyShape.Construct;
                profile.hasCorruptionAura = false;
                profile.auraIntensity = 0.2f;
                profile.idleParticle = ParticleSystemType.Sparks;
                profile.movementSpeedMultiplier = 0.95f;
                profile.floatsAboveGround = false;
                profile.floatHeight = 0f;
                break;

            default:
                Debug.LogWarning($"[IslandEnemyVisualProfile] Unknown island id '{islandId}'. Returning null.");
                DestroyImmediate(profile);
                return null;
        }

        return profile;
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(islandId);
    }
}
