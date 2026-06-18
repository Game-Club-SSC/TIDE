using System.Collections.Generic;

public static class NarrativeBeatsData
{
    public const string GoodEndingBeatId = "narrative_good_ending";
    public const string BadEndingBeatId = "narrative_bad_ending";
    public const string SelfHarmBeatId = "narrative_self_harm";
    public const string AcceptanceConversationId = "narrative_acceptance";
    public const string PreFinalBossConversationId = "narrative_pre_final_boss";

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
        new BeatDefinition(PreFinalBossConversationId, "Last Watch", "The party shares a final watch before descending to the rift.")
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
