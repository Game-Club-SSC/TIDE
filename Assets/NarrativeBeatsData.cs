using System.Collections.Generic;

public static class NarrativeBeatsData
{
    public const string GoodEndingBeatId = "narrative_good_ending";
    public const string BadEndingBeatId = "narrative_bad_ending";
    public const string SelfHarmBeatId = "narrative_self_harm";
    public const string AcceptanceConversationId = "narrative_acceptance";
    public const string PreFinalBossConversationId = "narrative_pre_final_boss";

    // Dialogue tree beat IDs
    public const string CeremonyDialogueId = "dialogue_ceremony_ch01";
    public const string CharacterIntroDialogueId = "dialogue_character_intro_ch02";
    public const string AncientTextReactionActIId = "dialogue_ancient_text_act1";
    public const string AncientTextReactionActIIId = "dialogue_ancient_text_act2";
    public const string AncientTextReactionActIIIId = "dialogue_ancient_text_act3";
    public const string PreBossGreedDialogueId = "dialogue_pre_boss_greed";
    public const string PreBossAttachmentDialogueId = "dialogue_pre_boss_attachment";
    public const string PreBossJealousyDialogueId = "dialogue_pre_boss_jealousy";
    public const string PreBossLustDialogueId = "dialogue_pre_boss_lust";
    public const string PreBossAngerDialogueId = "dialogue_pre_boss_anger";
    public const string PreBossEgoDialogueId = "dialogue_pre_boss_ego";
    public const string AcceptanceDialogueId = "dialogue_acceptance_act3";

    public sealed class BeatDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public BeatDefinition(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    private static readonly List<BeatDefinition> BeatCatalog = new List<BeatDefinition>
    {
        new BeatDefinition(GoodEndingBeatId, "The Tide Holds", "The party accepts the cost and seals the rift together."),
        new BeatDefinition(BadEndingBeatId, "The Tide Breaks", "A hero falls to the cost and the rift widens."),
        new BeatDefinition(SelfHarmBeatId, "The Scar Remains", "A main character bears the rift's cost in their own body."),
        new BeatDefinition(AcceptanceConversationId, "Acceptance", "Before the final confrontation, a quiet acceptance among the party."),
        new BeatDefinition(PreFinalBossConversationId, "Last Watch", "The party shares a final watch before descending to the rift."),
        new BeatDefinition(CeremonyDialogueId, "The Ceremony Awakens", "The five heroes awaken to their true nature during the coming-of-age ceremony."),
        new BeatDefinition(CharacterIntroDialogueId, "Five Strangers", "The heroes meet for the first time and travel to the first island."),
        new BeatDefinition(AncientTextReactionActIId, "The First Fragments", "The party discovers ancient texts and realizes their purpose."),
        new BeatDefinition(AncientTextReactionActIIId, "Darker Pages", "The ancient texts reveal darker truths about the cycle."),
        new BeatDefinition(AncientTextReactionActIIIId, "The Last Words", "The final texts accept the cost of balance."),
        new BeatDefinition(PreBossGreedDialogueId, "Before the Temple", "The party confronts the temptation of greed."),
        new BeatDefinition(PreBossAttachmentDialogueId, "The Garden of What Was", "The party faces memories of what they've lost."),
        new BeatDefinition(PreBossJealousyDialogueId, "The Mirror Beach", "The party confronts the lies of comparison."),
        new BeatDefinition(PreBossLustDialogueId, "The Enchanted Shore", "The party resists the pull of enchantment."),
        new BeatDefinition(PreBossAngerDialogueId, "The Burning Words", "The party speaks the words they've been holding back."),
        new BeatDefinition(PreBossEgoDialogueId, "The Last Peak", "The party faces the enemy that wears their own face."),
        new BeatDefinition(AcceptanceDialogueId, "Acceptance", "Before the final confrontation, the party accepts what comes.")
    };

    public static IReadOnlyList<BeatDefinition> GetAllBeats()
    {
        return BeatCatalog;
    }

    public static bool ContainsId(string beatId)
    {
        if (string.IsNullOrEmpty(beatId))
        {
            return false;
        }

        for (int i = 0; i < BeatCatalog.Count; i++)
        {
            if (string.Equals(BeatCatalog[i].Id, beatId, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
