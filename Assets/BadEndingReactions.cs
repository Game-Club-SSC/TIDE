using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides character-specific dialogue and emotional reactions for the bad ending.
/// Each hero reacts according to their flaw: Freida's greed, Briar's envy,
/// Killian's anger, and Merrador's inability to cope. The MC delivers the
/// final reflection.
/// </summary>
public static class BadEndingReactions
{
    // ------------------------------------------------------------------ //
    //  Data types
    // ------------------------------------------------------------------ //

    public struct CharacterReaction
    {
        public string heroId;
        public string heroName;
        public string element;
        public string[] despairLines;
        public string[] blameLines;
        public string[] isolationLines;
        public string finalWords;
    }

    // ------------------------------------------------------------------ //
    //  Pre-configured reactions
    // ------------------------------------------------------------------ //

    private static readonly CharacterReaction[] AllReactions = new CharacterReaction[]
    {
        // --- Freida (Earth) ---
        // Flaw: Greedy for attention. Worried about losing friends, she clung
        // to them too tightly and pushed them away.
        new CharacterReaction
        {
            heroId = "hero_earth",
            heroName = "Freida",
            element = "Earth",
            despairLines = new[]
            {
                "I tried to hold us together. I tried so hard.",
                "I could feel all of you slipping away, and I... I couldn't let go.",
                "This is what happens when you grip something too tightly. It crumbles."
            },
            blameLines = new[]
            {
                "If you had just listened to me. If you had just stayed close.",
                "I did everything for this group. Everything. And none of you saw it.",
                "You were all so busy looking at the horizon that you forgot who was standing right in front of you."
            },
            isolationLines = new[]
            {
                "I got greedy for your attention. I know that now. But knowing doesn't change anything.",
                "The more I reached for you, the further you drifted. And now there is no one left to reach for.",
                "I tried to hold us together, but my hands were too tight. And now... there is nothing left to hold."
            },
            finalWords = "I tried to hold us together. I tried so hard. But you were all slipping away, and I... I got greedy for your attention. I'm sorry."
        },

        // --- Briar (Air) ---
        // Flaw: Jealous of others' powers. Feeling underappreciated, she
        // withdrew her help in battle, which contributed to the party's fall.
        new CharacterReaction
        {
            heroId = "hero_air",
            heroName = "Briar",
            element = "Air",
            despairLines = new[]
            {
                "I was jealous. I know that now.",
                "I watched you all grow stronger, and something inside me... curdled.",
                "The wind does not envy the earth. But I envied all of you."
            },
            blameLines = new[]
            {
                "You never appreciated what I could do. Never once asked how I was feeling.",
                "I stopped helping because I wanted to matter more than you. And now none of us matter.",
                "You all had your powers, your purpose. I had nothing but the space between you."
            },
            isolationLines = new[]
            {
                "I pulled away because I thought it would make you notice me. Instead, it made me invisible.",
                "The air is everywhere and nowhere. That is what I became to all of you.",
                "I chose silence when I should have chosen trust. And now the silence is all there is."
            },
            finalWords = "I was jealous. I know that now. I stopped helping because I wanted to matter more than you. And now none of us matter."
        },

        // --- Killian (Fire) ---
        // Flaw: Realized they would all die regardless, and took his despair
        // out on the environment rather than processing the grief.
        new CharacterReaction
        {
            heroId = "hero_fire",
            heroName = "Killian",
            element = "Fire",
            despairLines = new[]
            {
                "I knew this was coming. I always knew.",
                "You can feel it in the flame, you know. The way it eats everything and then... goes out.",
                "I burned because I was afraid. And I was afraid because I knew the truth."
            },
            blameLines = new[]
            {
                "I took it out on everything around me because I couldn't face it.",
                "The anger... it was easier than grief. So much easier.",
                "You all pretended it would be fine. I couldn't pretend. So I burned."
            },
            isolationLines = new[]
            {
                "I destroyed things because I wanted to feel like I had control over something. Anything.",
                "Fire does not grieve. Fire consumes. And I consumed everything I loved.",
                "I knew we would die no matter what. So I burned the world to make the ending come faster."
            },
            finalWords = "I knew this was coming. I always knew. I took it out on everything around me because I couldn't face it. The anger... it was easier than grief."
        },

        // --- Merrick (Water) ---
        // Flaw: Attached to the attention gained from absorbing others' pain.
        // He defined himself by his sacrifice and eventually could not handle
        // the weight of his own suffering.
        new CharacterReaction
        {
            heroId = "hero_water",
            heroName = "Merrick",
            element = "Water",
            despairLines = new[]
            {
                "I thought I could absorb everyone's pain. I thought that made me strong.",
                "Water takes the shape of whatever holds it. I forgot that I was the one holding it.",
                "I gave and gave until there was nothing left. And still you asked for more."
            },
            blameLines = new[]
            {
                "You needed me to be strong. And I needed you to need me. And that is the problem.",
                "I traded my pain for your attention. And I thought that was a fair deal.",
                "You saw my sacrifice and called it kindness. But it was need. It was always need."
            },
            isolationLines = new[]
            {
                "But I couldn't absorb my own. And now there is nothing left to give.",
                "The river eventually runs dry. I just did not think it would happen so soon.",
                "I thought if I carried enough of your pain, you would never leave me. But the water rose, and it drowned us all."
            },
            finalWords = "I thought I could absorb everyone's pain. I thought that made me strong. But I couldn't absorb my own. And now there is nothing left to give."
        }
    };

    private static readonly CharacterReaction MCReaction = new CharacterReaction
    {
        heroId = "hero_mc",
        heroName = "The Chosen",
        element = "Varies",
        despairLines = new[]
        {
            "We were born for this purpose. We fulfilled it. And now... there is no more need for us.",
            "The tide always returns to balance. Even if that balance means our absence.",
            "I watched each of them fall. And I understood, with each loss, what the tide demands."
        },
        blameLines = new[]
        {
            "I should have seen it. I should have seen what the tide was doing to each of them.",
            "They were falling apart, and I was so focused on the mission that I missed the signs.",
            "The tide does not care about friendship. It only cares about equilibrium."
        },
        isolationLines = new[]
        {
            "And now I am the last. The tide has taken everyone else.",
            "Being the one who survives is not a gift. It is the tide's cruelest joke.",
            "I understand now. The tide always returns to balance. Even if that balance is emptiness."
        },
        finalWords = "We were born for this purpose. We fulfilled it. And now... there is no more need for us. I understand now. The tide always returns to balance."
    };

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns the bad ending reaction for the given hero.
    /// Falls back to the MC reaction if the heroId is not recognized.
    /// </summary>
    public static CharacterReaction GetReaction(string heroId)
    {
        for (int i = 0; i < AllReactions.Length; i++)
        {
            if (string.Equals(AllReactions[i].heroId, heroId, System.StringComparison.Ordinal))
            {
                return AllReactions[i];
            }
        }

        Debug.LogWarning($"[BadEndingReactions] No reaction found for heroId '{heroId}'. Returning MC reaction.");
        return MCReaction;
    }

    /// <summary>
    /// Returns the bad ending reaction for the given hero, using their element.
    /// Convenience overload for cases where only the element is known.
    /// </summary>
    public static CharacterReaction GetReaction(CombatUnit.Element element)
    {
        string heroId = ElementToHeroId(element);
        return GetReaction(heroId);
    }

    /// <summary>
    /// Returns all four companion hero reactions (excludes MC).
    /// </summary>
    public static CharacterReaction[] GetAllReactions()
    {
        return AllReactions;
    }

    /// <summary>
    /// Returns all reactions including the MC.
    /// </summary>
    public static CharacterReaction[] GetAllReactionsIncludingMC()
    {
        CharacterReaction[] all = new CharacterReaction[AllReactions.Length + 1];
        AllReactions.CopyTo(all, 0);
        all[AllReactions.Length] = MCReaction;
        return all;
    }

    /// <summary>
    /// Returns the MC's specific reaction -- the one who survives the bad ending.
    /// </summary>
    public static CharacterReaction GetMCReaction()
    {
        return MCReaction;
    }

    /// <summary>
    /// Returns a list of DialogueEntry sequences for the bad ending cutscene,
    /// iterating through each companion's despair and blame lines before the
    /// MC's final words. Suitable for feeding into <see cref="DialogueSystem.ShowDialogue"/>.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> BuildBadEndingDialogue()
    {
        List<DialogueSystem.DialogueEntry> entries = new List<DialogueSystem.DialogueEntry>();

        // Each companion reacts in sequence
        for (int i = 0; i < AllReactions.Length; i++)
        {
            CharacterReaction reaction = AllReactions[i];

            // Despair
            for (int d = 0; d < reaction.despairLines.Length; d++)
            {
                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = reaction.heroName,
                    dialogueText = reaction.despairLines[d],
                    emotion = DialogueSystem.Emotion.Sad
                });
            }

            // Blame
            for (int b = 0; b < reaction.blameLines.Length; b++)
            {
                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = reaction.heroName,
                    dialogueText = reaction.blameLines[b],
                    emotion = DialogueSystem.Emotion.Angry
                });
            }

            // Isolation
            for (int s = 0; s < reaction.isolationLines.Length; s++)
            {
                entries.Add(new DialogueSystem.DialogueEntry
                {
                    speakerName = reaction.heroName,
                    dialogueText = reaction.isolationLines[s],
                    emotion = DialogueSystem.Emotion.Worried
                });
            }

            // Final words
            entries.Add(new DialogueSystem.DialogueEntry
            {
                speakerName = reaction.heroName,
                dialogueText = reaction.finalWords,
                emotion = DialogueSystem.Emotion.Determined
            });
        }

        // MC's reaction
        for (int m = 0; m < MCReaction.despairLines.Length; m++)
        {
            entries.Add(new DialogueSystem.DialogueEntry
            {
                speakerName = MCReaction.heroName,
                dialogueText = MCReaction.despairLines[m],
                emotion = DialogueSystem.Emotion.Sad
            });
        }

        entries.Add(new DialogueSystem.DialogueEntry
        {
            speakerName = MCReaction.heroName,
            dialogueText = MCReaction.finalWords,
            emotion = DialogueSystem.Emotion.Determined
        });

        return entries;
    }

    /// <summary>
    /// Returns a single line for a specific character and category, useful for
    /// partial display in the ending sequence (e.g., showing one line per hero
    /// during silhouette fade-outs).
    /// </summary>
    public static string GetRandomLine(string heroId, ReactionCategory category)
    {
        CharacterReaction reaction = GetReaction(heroId);
        string[] lines;

        switch (category)
        {
            case ReactionCategory.Despair:
                lines = reaction.despairLines;
                break;
            case ReactionCategory.Blame:
                lines = reaction.blameLines;
                break;
            case ReactionCategory.Isolation:
                lines = reaction.isolationLines;
                break;
            case ReactionCategory.Final:
                return reaction.finalWords;
            default:
                return reaction.finalWords;
        }

        if (lines == null || lines.Length == 0)
        {
            return string.Empty;
        }

        return lines[Random.Range(0, lines.Length)];
    }

    // ------------------------------------------------------------------ //
    //  Enums
    // ------------------------------------------------------------------ //

    public enum ReactionCategory
    {
        Despair,
        Blame,
        Isolation,
        Final
    }

    // ------------------------------------------------------------------ //
    //  Utility
    // ------------------------------------------------------------------ //

    private static string ElementToHeroId(CombatUnit.Element element)
    {
        switch (element)
        {
            case CombatUnit.Element.Earth:  return "hero_earth";
            case CombatUnit.Element.Air:    return "hero_air";
            case CombatUnit.Element.Fire:   return "hero_fire";
            case CombatUnit.Element.Water:  return "hero_water";
            case CombatUnit.Element.Space:  return "hero_space";
            default:                        return "hero_mc";
        }
    }
}
