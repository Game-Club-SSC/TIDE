using UnityEngine;

/// <summary>
/// Static definition of 10 additional ancient text fragments spread across
/// the seven islands. Each text is written in a poetic/archaic style and
/// provides deeper lore about the 100-year cycle, the heroes, and the
/// corruption that plagues the world.
/// </summary>
public static class ExpandedAncientTexts
{
    public struct ExtraTextDefinition
    {
        public string textId;
        public string title;
        public string body;
        public string islandId;
        public float restorationRequired;
        public string relatedHeroId;
    }

    public static readonly ExtraTextDefinition[] AllTexts =
    {
        // ---------------------------------------------------------------
        // Island 1 -- Lust
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_naming_ritual",
            title = "The Naming Ritual",
            body =
                "In the age before the tide turned bitter, the elders gathered each hero " +
                "upon the shore of still water and whispered the elemental name into their " +
                "ears. Thus were they called -- not by the names their mothers gave, but by " +
                "the name the world required. Fire, Water, Earth, Air, Space -- five words " +
                "that held the weight of a thousand seasons. To bear an elemental name was to " +
                "accept that one's former self had already begun to fade.",
            islandId = "island_lust",
            restorationRequired = 25f,
            relatedHeroId = "hero_fire"
        },
        new ExtraTextDefinition
        {
            textId = "extra_first_tide_map",
            title = "First Tide Map",
            body =
                "Upon a stone tablet older than any living memory, seven circles are drawn " +
                "in a spiral pattern, each ring smaller than the last. At the centre burns " +
                "a single mark -- the point where all corruption converges. The mapmaker wrote: " +
                "\"When the tide falls, the circles appear. When the tide rises, the circles " +
                "must be filled. Should all seven rings be sealed, the world may yet endure " +
                "another hundred years.\"",
            islandId = "island_lust",
            restorationRequired = 50f,
            relatedHeroId = "hero_water"
        },

        // ---------------------------------------------------------------
        // Island 2 -- Gluttony
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_consumption_cycle",
            title = "The Consumption Cycle",
            body =
                "The corruption does not destroy; it devours. Where balance falters, hunger " +
                "grows, and what is hungered for is never enough. The first corruption was " +
                "born of a feast that never ended -- the earth gave freely, and the people " +
                "took without pause. In their greed they mistook abundance for permission. " +
                "Now the islands swell with that same endless wanting, and only the tide can " +
                "sate what was never meant to be filled.",
            islandId = "island_gluttony",
            restorationRequired = 30f,
            relatedHeroId = "hero_earth"
        },
        new ExtraTextDefinition
        {
            textId = "extra_past_hero_journal",
            title = "Past Heroes' Journal Entry",
            body =
                "Day of Ash, Year of the Fading. We arrived at the third island and found it " +
                "consumed by a fog that tasted of iron. My companion spoke not a word for " +
                "three days. When finally she opened her mouth, it was not her voice that " +
                "emerged but that of someone long dead. I recorded her words verbatim: \"The " +
                "cycle cannot be broken, only endured. We are not the first. We will not be " +
                "the last. Pray that when you fade, the tide remembers your name.\"",
            islandId = "island_gluttony",
            restorationRequired = 60f,
            relatedHeroId = "hero_space"
        },

        // ---------------------------------------------------------------
        // Island 3 -- Greed
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_price_of_gold",
            title = "The Price of Gold",
            body =
                "They say the third island once glittered with veins of gold so rich that " +
                "the rivers ran yellow. Miners came from every shore, and with each ingot " +
                "extracted, the corruption deepened. The gold was not a gift; it was a lure. " +
                "What the earth offers freely, it can reclaim with interest. By the time the " +
                "last vein was severed, the island had already begun to sink beneath the " +
                "weight of its own wealth. Remember this: greed is not the desire for more " +
                "-- it is the inability to stop.",
            islandId = "island_greed",
            restorationRequired = 40f,
            relatedHeroId = "hero_earth"
        },

        // ---------------------------------------------------------------
        // Island 4 -- Sloth
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_stagnation_warning",
            title = "The Stagnation Warning",
            body =
                "When the heroes grow weary and the battles blur into one another, beware " +
                "the quiet voice that whispers: \"Let another carry this burden.\" For it is " +
                "not rest that heals the world but the will to rise again. The corruption " +
                "feeds upon inaction as surely as fire feeds upon wind. The fourth island " +
                "learned this truth too late -- its people lay down to sleep and never woke, " +
                "and the tide rose over them while they dreamed.",
            islandId = "island_sloth",
            restorationRequired = 20f,
            relatedHeroId = "hero_air"
        },
        new ExtraTextDefinition
        {
            textId = "extra_childs_letter",
            title = "A Child's Letter",
            body =
                "Dearest mother, I write to you from the edge of the fourth island where " +
                "the grass has stopped growing. Father says the heroes will come soon and " +
                "make it right again. I have drawn a picture of them for you -- five figures " +
                "standing in a circle, holding hands. I do not know their names but I know " +
                "their colours: red, blue, green, white, and violet. Please keep this letter " +
                "safe. When the tide comes, I want them to know that someone was waiting.",
            islandId = "island_sloth",
            restorationRequired = 55f,
            relatedHeroId = "hero_fire"
        },

        // ---------------------------------------------------------------
        // Island 5 -- Wrath
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_burning_truth",
            title = "The Burning Truth",
            body =
                "Fire is not the enemy. The scholars erred when they cursed the flame, for " +
                "fire consumes only what was already dying. The fifth island burns because " +
                "the corruption there has grown so dense that only purification can reach " +
                "its core. Let the hero of flame not fear the blaze they carry -- it is " +
                "not destruction but renewal. Every forest that burns returns greener. " +
                "Every island that burns returns restored. The cycle demands it.",
            islandId = "island_wrath",
            restorationRequired = 35f,
            relatedHeroId = "hero_fire"
        },

        // ---------------------------------------------------------------
        // Island 6 -- Envy
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_mirror_of_souls",
            title = "Mirror of Souls",
            body =
                "In the heart of the sixth island lies a mirror forged not of glass but " +
                "of crystallised regret. Those who gaze upon it see not their own reflection " +
                "but the lives they might have lived, the choices they did not make. This " +
                "is the origin of the Envy -- for comparison is the seed of sorrow. The " +
                "mirror was created by the last hero of Space, who could not bear to watch " +
                "their companions fade and wished to see a world where none of them had " +
                "been chosen at all.",
            islandId = "island_envy",
            restorationRequired = 45f,
            relatedHeroId = "hero_space"
        },

        // ---------------------------------------------------------------
        // Island 7 -- Pride
        // ---------------------------------------------------------------
        new ExtraTextDefinition
        {
            textId = "extra_final_declaration",
            title = "The Final Declaration",
            body =
                "I, the last of the five, stand upon the seventh shore and speak these words " +
                "so that the tide may carry them beyond the edge of forgetting. We were " +
                "summoned not by choice but by need. We fought not for glory but because " +
                "the alternative was silence. If our names are erased, let the record show " +
                "that we stood where none would stand, and we gave what none could give. " +
                "To the heroes who follow: the cycle is cruel, but you are not. Rise.",
            islandId = "island_pride",
            restorationRequired = 70f,
            relatedHeroId = "hero_air"
        }
    };

    /// <summary>
    /// Attempts to find an expanded text by its ID. Returns true if found.
    /// </summary>
    public static bool TryGetText(string textId, out ExtraTextDefinition result)
    {
        result = default;
        if (string.IsNullOrEmpty(textId) || AllTexts == null)
        {
            return false;
        }

        for (int i = 0; i < AllTexts.Length; i++)
        {
            if (AllTexts[i].textId == textId)
            {
                result = AllTexts[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the number of expanded texts available for a given island.
    /// </summary>
    public static int CountForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId) || AllTexts == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < AllTexts.Length; i++)
        {
            if (AllTexts[i].islandId == islandId)
            {
                count++;
            }
        }

        return count;
    }
}
