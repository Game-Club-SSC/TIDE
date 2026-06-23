using System;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines the narrative mechanic for each boss fight.
/// Each vice boss has unique combat modifiers and narrative dialogue that reflect
/// the vice's psychological influence on the party during the encounter.
/// </summary>
[CreateAssetMenu(fileName = "BossNarrativeMechanic", menuName = "TIDE/Boss Narrative Mechanic")]
public class BossNarrativeMechanic : ScriptableObject
{
    [Header("Identity")]
    public string bossName;
    public string islandId;

    [Header("Combat Modifiers")]
    [Tooltip("Damage multiplier for player team during this boss fight")]
    public float teamDamageMultiplier = 1f;
    [Tooltip("Defense multiplier for player team")]
    public float teamDefenseMultiplier = 1f;
    [Tooltip("Whether team-up skills are weakened")]
    public bool weakenTeamUps;
    [Tooltip("Whether boss can cause friendly fire between party members")]
    public bool causesFriendlyFire;
    [Tooltip("Whether boss amplifies negative emotions (jealousy/anger)")]
    public bool amplifiesNegativeEmotions;
    [Tooltip("Friendly fire probability when causesFriendlyFire is true (0-1)")]
    [Range(0f, 1f)]
    public float friendlyFireChance;

    [Header("Narrative")]
    [TextArea(3, 8)]
    public string introDescription;
    [TextArea(3, 8)]
    public string midFightDialogue;
    [TextArea(3, 8)]
    public string defeatDialogue;

    [Header("Setting")]
    public string locationDescription;
    public Color atmosphereColor = Color.white;

    /// <summary>
    /// Returns pre-configured narrative mechanics for each boss, keyed by canonical island ID.
    /// </summary>
    public static BossNarrativeMechanic[] GetDefaults()
    {
        BossNarrativeMechanic[] defaults = new BossNarrativeMechanic[6];

        // Greed - island_greed (temple full of gold)
        defaults[0] = CreateInstance<BossNarrativeMechanic>();
        defaults[0].bossName = "Greed";
        defaults[0].islandId = "island_greed";
        defaults[0].teamDamageMultiplier = 0.9f;
        defaults[0].weakenTeamUps = true;
        defaults[0].introDescription = "The temple glitters with stolen gold. Every coin whispers a promise. One of you reaches out — and the floor trembles beneath mountains of treasure.";
        defaults[0].midFightDialogue = "The gold shifts and writhes. It is not wealth. It is hunger given form.";
        defaults[0].defeatDialogue = "The coins scatter like dead leaves. The temple was never real. Only the wanting was.";
        defaults[0].locationDescription = "A temple full of gold and coins";
        defaults[0].atmosphereColor = new Color(0.85f, 0.75f, 0.2f, 1f);

        // Attachment - island_sloth (garden of memories)
        defaults[1] = CreateInstance<BossNarrativeMechanic>();
        defaults[1].bossName = "Attachment";
        defaults[1].islandId = "island_sloth";
        defaults[1].teamDamageMultiplier = 0.85f;
        defaults[1].teamDefenseMultiplier = 0.9f;
        defaults[1].weakenTeamUps = true;
        defaults[1].introDescription = "The garden blooms with memories. Each flower holds a face you’ve tried to forget. The boss does not attack your bodies — it attacks your hearts.";
        defaults[1].midFightDialogue = "You fight for someone who is no longer here. Can you let them go?";
        defaults[1].defeatDialogue = "The flowers wither. The memories remain, but they no longer hold you.";
        defaults[1].locationDescription = "A garden blooming with memories of the past";
        defaults[1].atmosphereColor = new Color(0.4f, 0.7f, 0.4f, 1f);

        // Jealousy - island_envy (beach mirrors)
        defaults[2] = CreateInstance<BossNarrativeMechanic>();
        defaults[2].bossName = "Jealousy";
        defaults[2].islandId = "island_envy";
        defaults[2].amplifiesNegativeEmotions = true;
        defaults[2].causesFriendlyFire = true;
        defaults[2].friendlyFireChance = 0.1f;
        defaults[2].introDescription = "The beach mirrors show not your face, but what you wish you were. Your allies look different through the glass — better, stronger, more worthy.";
        defaults[2].midFightDialogue = "Why should they have what you do not? The mirrors do not lie. They only show the truths you’d rather ignore.";
        defaults[2].defeatDialogue = "The mirrors crack. In the shards, you see only yourself — and that was always enough.";
        defaults[2].locationDescription = "A beach lined with mirrors that show idealized reflections";
        defaults[2].atmosphereColor = new Color(0.6f, 0.3f, 0.7f, 1f);

        // Lust - island_lust (enchanted moura)
        defaults[3] = CreateInstance<BossNarrativeMechanic>();
        defaults[3].bossName = "Lust";
        defaults[3].islandId = "island_lust";
        defaults[3].teamDamageMultiplier = 1.1f;
        defaults[3].introDescription = "The enchanted moura smile invitingly. Their beauty hides teeth. This is a test of gear, goods, and the wisdom to see through enchantment.";
        defaults[3].midFightDialogue = "You want what they offer. Everyone does. That is the trap.";
        defaults[3].defeatDialogue = "The moura’s smile fades. Beneath it, nothing. It was always nothing.";
        defaults[3].locationDescription = "A coastal shrine guarded by enchanted moura";
        defaults[3].atmosphereColor = new Color(0.9f, 0.4f, 0.5f, 1f);

        // Anger - island_wrath (burning air)
        defaults[4] = CreateInstance<BossNarrativeMechanic>();
        defaults[4].bossName = "Anger";
        defaults[4].islandId = "island_wrath";
        defaults[4].causesFriendlyFire = true;
        defaults[4].friendlyFireChance = 0.15f;
        defaults[4].amplifiesNegativeEmotions = true;
        defaults[4].introDescription = "The air burns with unspoken words. Every grievance becomes a weapon. The boss feeds on the knowledge you’ve gained — and turns it against you and each other.";
        defaults[4].midFightDialogue = "Say it. Say what you’ve been holding back. The boss grins. It has been waiting.";
        defaults[4].defeatDialogue = "The fire dies. The words remain unspoken, but they no longer burn.";
        defaults[4].locationDescription = "A scorched clearing where rage takes physical form";
        defaults[4].atmosphereColor = new Color(0.9f, 0.2f, 0.1f, 1f);

        // Pride - island_pride (mountain peak)
        defaults[5] = CreateInstance<BossNarrativeMechanic>();
        defaults[5].bossName = "Pride";
        defaults[5].islandId = "island_pride";
        defaults[5].amplifiesNegativeEmotions = true;
        defaults[5].introDescription = "The mountain peak offers clarity. And clarity, sometimes, is cruelty. The boss convinces each of you that you are better than the others.";
        defaults[5].midFightDialogue = "You’ve defeated five bosses. Each one tried to drive you apart. This one is no different — except it knows it.";
        defaults[5].defeatDialogue = "The peak crumbles. Every boss tried to turn you against each other. You are still here. That is the answer.";
        defaults[5].locationDescription = "A mountain peak where clarity becomes cruelty";
        defaults[5].atmosphereColor = new Color(0.95f, 0.9f, 0.8f, 1f);

        return defaults;
    }

    /// <summary>
    /// Looks up the default narrative mechanic for the given canonical island ID.
    /// Returns null if no default is defined for that island.
    /// </summary>
    public static BossNarrativeMechanic GetDefaultForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        BossNarrativeMechanic[] defaults = GetDefaults();
        for (int i = 0; i < defaults.Length; i++)
        {
            if (string.Equals(defaults[i].islandId, islandId, StringComparison.Ordinal))
            {
                return defaults[i];
            }
        }

        return null;
    }
}
