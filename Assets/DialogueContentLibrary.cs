using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static library of all story dialogue content across 7 chapters.
/// Provides DialogueEntry sequences, DialogueTree objects, and beat text
/// for wiring into <see cref="DialogueTrigger"/>, <see cref="NarrativeBeatDirector"/>,
/// and <see cref="DialogueSystem"/>.
/// </summary>
public static class DialogueContentLibrary
{
    // ------------------------------------------------------------------ //
    //  Hero IDs (canonical)
    // ------------------------------------------------------------------ //

    public const string HeroEarth = "hero_earth";
    public const string HeroAir = "hero_air";
    public const string HeroFire = "hero_fire";
    public const string HeroWater = "hero_water";
    public const string HeroMC = "hero_mc";

    // ------------------------------------------------------------------ //
    //  1. Ceremony Intro Dialogue (Chapter 0 / 1)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Opening ceremony where the five heroes are summoned to the archipelago.
    /// Narrator sets the tone, then each hero reacts to the calling.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetCeremonyIntroDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The tide pulls five souls from the edges of the world. They do not know each other. They do not know why they have been chosen.",
                emotion = DialogueSystem.Emotion.Neutral
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The archipelago rises from the sea like a held breath. Six islands. Six vices. One chance to restore what was broken.",
                emotion = DialogueSystem.Emotion.Neutral
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I felt the pull in my chest before I saw the water. Like something was calling me home... except I have never been here before.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "Home? This place smells like salt and old regret. I don't like it. But I couldn't stop walking toward it.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "The water here... it remembers something. I can feel it pressing against me, asking me to listen.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "I see five lights on the shore. Each one a different color. I think we are supposed to find each other.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "And so the five converge on the first island. Strangers bound by a force none of them chose.",
                emotion = DialogueSystem.Emotion.Neutral
            }
        };
    }

    /// <summary>
    /// Post-ceremony party formation dialogue. The heroes introduce themselves
    /// and reluctantly agree to work together.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetCeremonyPartyFormationDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "So... we are all here because the tide said so? That is not much of a reason.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "Doesn't matter why. We are here. We move forward or we rot here. I choose to move.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "I think it matters why. If we don't understand what brought us here, we are just walking blind.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The wind is telling me something. It always does, out here. But I don't trust it yet.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We can argue about trust later. Right now, we need shelter and a plan.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "Fine. But after this is over, I want answers. Real ones.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroEarth
            }
        };
    }

    // ------------------------------------------------------------------ //
    //  2. Character Introduction Dialogue (Chapter 2)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns the character introduction dialogue for each hero.
    /// Key: heroId. Value: dialogue entries that showcase personality.
    /// </summary>
    public static Dictionary<string, List<DialogueSystem.DialogueEntry>> GetCharacterIntroductions()
    {
        return new Dictionary<string, List<DialogueSystem.DialogueEntry>>
        {
            [HeroEarth] = new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Freida",
                    dialogueText = "I am Freida. I grew up in a village where no one ever stayed. People drift. That is what they do.",
                    emotion = DialogueSystem.Emotion.Sad,
                    relatedHeroId = HeroEarth
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Freida",
                    dialogueText = "So I learned to hold on. To everything. Friends, places, moments. If you grip hard enough, nothing can leave you.",
                    emotion = DialogueSystem.Emotion.Determined,
                    relatedHeroId = HeroEarth
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Freida",
                    dialogueText = "That is my strength and my flaw, I know. But I would rather hold too tightly than watch someone walk away again.",
                    emotion = DialogueSystem.Emotion.Worried,
                    relatedHeroId = HeroEarth
                }
            },
            [HeroAir] = new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Briar",
                    dialogueText = "I am Briar. I have always been able to read the wind. It tells me things about people that they would rather keep hidden.",
                    emotion = DialogueSystem.Emotion.Neutral,
                    relatedHeroId = HeroAir
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Briar",
                    dialogueText = "You learn a lot about someone when you know what they are afraid of. And you learn even more about yourself when you realize you are jealous of what they have.",
                    emotion = DialogueSystem.Emotion.Sad,
                    relatedHeroId = HeroAir
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Briar",
                    dialogueText = "I don't mean to be bitter. But the wind carries everything. Even the things I wish I could forget.",
                    emotion = DialogueSystem.Emotion.Worried,
                    relatedHeroId = HeroAir
                }
            },
            [HeroFire] = new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Killian",
                    dialogueText = "Name's Killian. I burn things. Not on purpose. Well... sometimes on purpose.",
                    emotion = DialogueSystem.Emotion.Angry,
                    relatedHeroId = HeroFire
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Killian",
                    dialogueText = "Fire is honest. It doesn't pretend to be anything other than what it is. I wish I could say the same about people.",
                    emotion = DialogueSystem.Emotion.Determined,
                    relatedHeroId = HeroFire
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Killian",
                    dialogueText = "I know I am difficult. I know I push people away. But when everything is ending anyway, what is the point of being gentle?",
                    emotion = DialogueSystem.Emotion.Angry,
                    relatedHeroId = HeroFire
                }
            },
            [HeroWater] = new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Merrick",
                    dialogueText = "I am Merrick. Water is... patient. It finds every crack, fills every space. I try to do the same.",
                    emotion = DialogueSystem.Emotion.Neutral,
                    relatedHeroId = HeroWater
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Merrick",
                    dialogueText = "When someone is hurting, I take it in. All of it. It is what I do. It is what water does. It carries what others cannot.",
                    emotion = DialogueSystem.Emotion.Sad,
                    relatedHeroId = HeroWater
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "Merrick",
                    dialogueText = "I know it looks like I am trying to be the hero. I am not. I just... need to be needed. That is the truth of it.",
                    emotion = DialogueSystem.Emotion.Worried,
                    relatedHeroId = HeroWater
                }
            },
            [HeroMC] = new List<DialogueSystem.DialogueEntry>
            {
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "MC",
                    dialogueText = "I don't remember much before the tide called me. Just a feeling. Like I was supposed to be somewhere.",
                    emotion = DialogueSystem.Emotion.Neutral,
                    relatedHeroId = HeroMC
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "MC",
                    dialogueText = "I am not the strongest here. I am not the wisest. But I think I am the one who has to keep us moving forward.",
                    emotion = DialogueSystem.Emotion.Determined,
                    relatedHeroId = HeroMC
                },
                new DialogueSystem.DialogueEntry
                {
                    speakerName = "MC",
                    dialogueText = "If that makes me stubborn, so be it. Someone has to decide when to run and when to fight.",
                    emotion = DialogueSystem.Emotion.Determined,
                    relatedHeroId = HeroMC
                }
            }
        };
    }

    // ------------------------------------------------------------------ //
    //  3. Ancient Text Discovery Reactions (Progressive Tone Shift)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns dialogue reactions for ancient text discoveries across the game.
    /// Tone shifts from curious to grave to resigned as the truth is revealed.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetAncientTextDiscoveryReactions(int discoveryIndex)
    {
        switch (discoveryIndex)
        {
            case 0: // First discovery - curiosity
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "These carvings are old. Older than anything I have felt in the wind.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "It speaks of a cycle. The tide rises, the vices return, and five are chosen to face them.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroWater
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "Chosen? We didn't choose this. We were taken.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "Read the last line. It says the five who are chosen... do not return.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroFire
                    }
                };

            case 1: // Second discovery - unease
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "More text. This one is a record. Names. Dozens of them, from centuries past.",
                        emotion = DialogueSystem.Emotion.Worried,
                        relatedHeroId = HeroWater
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "Each name has five entries. Five people per cycle. None of them survived.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "I don't want to read any more of these. I don't want to know what happens to us.",
                        emotion = DialogueSystem.Emotion.Worried,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "It doesn't matter what we want. The text is already written. We are just walking through it.",
                        emotion = DialogueSystem.Emotion.Determined,
                        relatedHeroId = HeroFire
                    }
                };

            case 2: // Third discovery - dread
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "This one is different. It is not a record. It is a warning.",
                        emotion = DialogueSystem.Emotion.Worried,
                        relatedHeroId = HeroMC
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "The tide does not heal. It balances. And balance requires sacrifice.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroWater
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "Every cycle, the five restore the islands. And then the tide takes them. That is the price.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "No. I refuse. There has to be another way.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "I told you. Fire is honest. The text is honest. You are the only one still pretending.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroFire
                    }
                };

            case 3: // Fourth discovery - resignation
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "I have read it three times now. The words do not change.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroWater
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "Then we stop reading. We focus on what is in front of us.",
                        emotion = DialogueSystem.Emotion.Determined,
                        relatedHeroId = HeroMC
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "The wind has gone quiet. Even it knows there is nothing left to say.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "I am not letting go. Whatever comes, I am holding on to all of you.",
                        emotion = DialogueSystem.Emotion.Determined,
                        relatedHeroId = HeroEarth
                    }
                };

            default: // Final discovery - acceptance
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Narrator",
                        dialogueText = "The last inscription is the simplest. It reads: 'The tide remembers what the world forgets.'",
                        emotion = DialogueSystem.Emotion.Neutral
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "Maybe that is enough. Maybe remembering is the point.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroWater
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "Then let's make sure there is something worth remembering.",
                        emotion = DialogueSystem.Emotion.Determined,
                        relatedHeroId = HeroFire
                    }
                };
        }
    }

    // ------------------------------------------------------------------ //
    //  4. Pre-Boss Conversations (6 Bosses)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns pre-boss conversation entries for the given island ID.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetPreBossConversation(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return new List<DialogueSystem.DialogueEntry>();
        }

        switch (islandId)
        {
            case "island_greed":
                return GetPreBossGreedDialogue();
            case "island_desire":
                return GetPreBossAttachmentDialogue();
            case "island_envy":
                return GetPreBossJealousyDialogue();
            case "island_lust":
                return GetPreBossLustDialogue();
            case "island_anger":
                return GetPreBossAngerDialogue();
            case "island_ego":
                return GetPreBossEgoDialogue();
            default:
                return new List<DialogueSystem.DialogueEntry>();
        }
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossGreedDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "Do you feel that? The ground is humming. Like something under there is... hungry.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "It is gold. I can smell it. Everyone can. That is the problem.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The wind says this place takes more than it gives. Be careful what you pick up.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
                {
                speakerName = "MC",
                dialogueText = "We are not here for gold. We are here to restore the island. Nothing else matters.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossAttachmentDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "This garden... it feels like memory. Like every flower holds someone you have lost.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I can see my mother's face in the petals. She is smiling. She never smiled like that.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "It is a trick. The garden shows you what you want, then it buries you in it.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "Then we walk through it with our eyes open. We don't let the garden choose for us.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossJealousyDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The mirrors. They are everywhere. And they all show someone better than me.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I see a version of myself who never lost anyone. It is... painful.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "I see a version of myself who doesn't care. That one is smiling. I hate him.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "Do not look too long. The mirrors don't show truth. They show want.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We are what we are. That is enough. Now break the glass and move.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossLustDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "The moura are singing. It is beautiful. Too beautiful.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "Their song promises everything. Rest. Safety. A place where no one leaves.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I want to listen. I know I shouldn't, but I want to.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "Don't. Enchantment is just another word for trap. Cover your ears and fight.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We came here to restore balance. Not to be consumed by it. Stay focused.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossAngerDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "The air is burning. I can feel it in my teeth. Someone is angry here. Very angry.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "It is not just the boss. I can feel the anger between us too. It is building.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "The fire here does not burn wood. It burns patience. And patience is all we have left.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "If we fight each other, we lose. The anger wants us to break apart.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "Then we hold together. Whatever it says, whatever it makes us feel, we hold together.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    private static List<DialogueSystem.DialogueEntry> GetPreBossEgoDialogue()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "The peak. We can see all six islands from here. Every one we have restored.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroMC
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "And every one that almost broke us. This is the last one. I can feel it.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The wind is different here. It doesn't whisper anymore. It speaks clearly.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "It says we are better than we think. That we have earned this.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "Ego is the last vice for a reason. It whispers that you don't need anyone. Remember that it is lying.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We need each other. That has always been true. Let's finish this together.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            }
        };
    }

    // ------------------------------------------------------------------ //
    //  5. Acceptance Conversation (Act III, Pre-Final Boss)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// The party discusses what they have learned and accepts their fate together
    /// before facing the final challenge.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetAcceptanceConversation()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We have restored five islands. One remains. And after that... the tide will take what it is owed.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroMC
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I have been thinking about what the texts said. The five who are chosen do not return. I am scared.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "We are all scared. But fear is not the same as weakness. I learned that from the fire.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "I used to be jealous of all of you. Your strengths. Your clarity. But now I see... we were all broken the same way.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "I carried your pain because I thought it made me useful. But what I should have said is that I needed you to carry mine too.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I held on too tightly. I know that now. But I don't regret it. Not one moment.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "Then we face this last one the way we faced all the others. Together. Eyes open. No more running.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "Whatever happens after this, we gave each other something the tide cannot take. We gave each other purpose.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The five stand at the edge of the last island. The tide is calm. The path is clear. They walk forward together.",
                emotion = DialogueSystem.Emotion.Neutral
            }
        };
    }

    // ------------------------------------------------------------------ //
    //  6. Good Ending Narrator Closing
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Narrator lines for the good ending sequence after the final boss is defeated.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetGoodEndingNarratorClosing()
    {
        return new List<DialogueSystem.DialogueEntry>
        {
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The six vices fall. The islands settle. The tide, at last, is still.",
                emotion = DialogueSystem.Emotion.Neutral
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The five stand on the shore as the sun lowers. They know what comes next. They have always known.",
                emotion = DialogueSystem.Emotion.Neutral
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I can feel it. The pull. It is gentle now. Not like before.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroEarth
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The wind is saying goodbye. It is the softest I have ever heard it.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroAir
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "I thought fire would be the last thing I felt. But it is not. It is warmth. It is enough.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroFire
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Merrick",
                dialogueText = "I don't need to carry anything anymore. The water is finally calm.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroWater
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We did what we came to do. The world will forget us. But we will not forget each other.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The tide takes them, as it was always meant to. But this time, it is not a punishment. It is a release.",
                emotion = DialogueSystem.Emotion.Neutral
            },
            new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The book closes. The children are quiet. And in the silence, something like peace settles over the room.",
                emotion = DialogueSystem.Emotion.Neutral
            }
        };
    }

    // ------------------------------------------------------------------ //
    //  7. Bad Ending Dialogue (SelfHarmBeat - Full Text)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Full dialogue sequence for the bad ending. This is the SelfHarmBeat
    /// content: the party falls one by one, leaving only the MC.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetBadEndingDialogue()
    {
        return BadEndingReactions.BuildBadEndingDialogue();
    }

    // ------------------------------------------------------------------ //
    //  8. Relationship-Dependent Dialogue Variations
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns dialogue that varies based on the bond level between two heroes.
    /// Called at key story moments to reflect the relationship state.
    /// </summary>
    public static List<DialogueSystem.DialogueEntry> GetRelationshipDialogue(string heroA, string heroB, int bondLevel)
    {
        // Determine affinity tier
        bool isHighAffinity = bondLevel >= 60;
        bool isLowAffinity = bondLevel < 20;
        string key = MakePairKey(heroA, heroB);

        if (isHighAffinity)
        {
            return GetHighAffinityDialogue(key);
        }

        if (isLowAffinity)
        {
            return GetLowAffinityDialogue(key);
        }

        return GetMidAffinityDialogue(key);
    }

    private static List<DialogueSystem.DialogueEntry> GetHighAffinityDialogue(string pairKey)
    {
        switch (pairKey)
        {
            case "hero_earth|hero_fire":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "You are the only one who never pulled away when I held on too tight.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "Fire doesn't run from heat. And you... you are a warm place to be.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroFire
                    }
                };
            case "hero_air|hero_water":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "You listen when no one else does. I forget to be jealous when I am near you.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "And you remind me that not all pain needs to be carried. Some of it can be released.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroWater
                    }
                };
            case "hero_earth|hero_mc":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "You never let go. Even when I was too much. Thank you.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "You held this group together when I couldn't. We needed that. I needed that.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroMC
                    }
                };
            default:
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "We have been through a lot together. I am glad you are here.",
                        emotion = DialogueSystem.Emotion.Happy,
                        relatedHeroId = HeroMC
                    }
                };
        }
    }

    private static List<DialogueSystem.DialogueEntry> GetLowAffinityDialogue(string pairKey)
    {
        switch (pairKey)
        {
            case "hero_earth|hero_fire":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "You push away everything I try to give you. I don't know how to reach you.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "Maybe you shouldn't try. Some fires are not meant to be held.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroFire
                    }
                };
            case "hero_air|hero_water":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "You absorb everyone's pain but never share your own. I can't trust someone who hides.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "And I can't trust someone who reads my thoughts and holds them against me.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroWater
                    }
                };
            case "hero_earth|hero_mc":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "You don't listen. You just decide and expect us to follow.",
                        emotion = DialogueSystem.Emotion.Angry,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "Someone has to make the hard calls. I am sorry if that feels like I am ignoring you.",
                        emotion = DialogueSystem.Emotion.Sad,
                        relatedHeroId = HeroMC
                    }
                };
            default:
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "We need to work together. Even if we don't like each other. The tide doesn't care about our feelings.",
                        emotion = DialogueSystem.Emotion.Determined,
                        relatedHeroId = HeroMC
                    }
                };
        }
    }

    private static List<DialogueSystem.DialogueEntry> GetMidAffinityDialogue(string pairKey)
    {
        switch (pairKey)
        {
            case "hero_earth|hero_fire":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Freida",
                        dialogueText = "I don't understand you, Killian. But I think I am starting to.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroEarth
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Killian",
                        dialogueText = "Give it time. I am not easy. But I am honest.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroFire
                    }
                };
            case "hero_air|hero_water":
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Briar",
                        dialogueText = "The wind doesn't tell me everything about you. Just... enough to be curious.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroAir
                    },
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "Merrick",
                        dialogueText = "Then let me show you the rest. When you are ready.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroWater
                    }
                };
            default:
                return new List<DialogueSystem.DialogueEntry>
                {
                    new DialogueSystem.DialogueEntry
                    {
                        speakerName = "MC",
                        dialogueText = "We are getting better at this. Working together. I can feel it.",
                        emotion = DialogueSystem.Emotion.Neutral,
                        relatedHeroId = HeroMC
                    }
                };
        }
    }

    // ------------------------------------------------------------------ //
    //  9. NarrativeBeatDirector Integration Helpers
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns beat title and body text for a given beat ID.
    /// Used by NarrativeBeatDirector to get the text content for each beat.
    /// </summary>
    public static void GetBeatContent(string beatId, out string title, out string body)
    {
        switch (beatId)
        {
            case "beat_intro_tension":
                title = "Campfire Friction";
                body = "Fire: We waste daylight arguing while the island rots.\n"
                     + "Water: And if we rush without balance, we become the same rot.\n"
                     + "Earth: Save it. We move together or not at all.\n"
                     + "Air: Then start with one section. Small. Clean.\n"
                     + "Space: One step in balance is still a step forward.";
                break;

            case "beat_pre_guard_combat":
                title = "Before the Guard";
                body = "Air: That guard is feeding off the sealed tile.\n"
                     + "Earth: Break the formation, then the lock should loosen.\n"
                     + "Fire: Fine. We cut through, then rebalance the field.";
                break;

            case "beat_post_restoration_reflection":
                title = "After the First Shift";
                body = "Water: The island feels lighter... but only for now.\n"
                     + "Space: Balance never lasts. It must be renewed.\n"
                     + "Fire: Then we keep moving before it slips again.";
                break;

            case "beat_act_two_revelation":
                title = "What The Texts Meant";
                body = "Earth: These records are not warnings. They are instructions.\n"
                     + "Water: Every century, the same march. The same ending.\n"
                     + "Air: Then the silence between us is not fear. It is recognition.";
                break;

            case "beat_act_three_acceptance":
                title = "Acceptance Before The Last Shore";
                body = "Fire: We know what waits after this.\n"
                     + "Space: Knowing does not empty the path. It only clarifies it.\n"
                     + "Earth: Then we finish what we were sent here to finish, together.";
                break;

            case "beat_ending_good":
                title = "Sunset In Balance";
                body = "The six enemies are gone, and with them the need for the chosen five.\n"
                     + "The party understands at last that they were born only to restore balance.\n"
                     + "Facing the sunset together, they accept that peace costs them their own fading light.";
                break;

            case "beat_ending_bad":
                title = "Sunset Without Purpose";
                body = "The party falls before finishing its purpose, and only the main character remains.\n"
                     + "Despair twists fate into meaninglessness instead of acceptance.\n"
                     + "On the hill at sunset, he dies believing the cycle ended in nothing.";
                break;

            default:
                title = "Unknown Beat";
                body = string.Empty;
                break;
        }
    }

    /// <summary>
    /// Returns ceremony intro dialogue wired as a DialogueTree for branching paths.
    /// </summary>
    public static DialogueTree GetCeremonyIntroTree()
    {
        DialogueTree tree = new DialogueTree
        {
            treeId = "tree_ceremony_intro",
            title = "Ceremony Intro"
        };

        DialogueTreeNode root = new DialogueTreeNode
        {
            nodeId = "ceremony_root",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The tide pulls five souls from the edges of the world. Do you answer the call?",
                emotion = DialogueSystem.Emotion.Neutral
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "Step forward into the light",
                    nextNodeId = "ceremony_accept",
                    increasesBond = true,
                    bondAmount = 5
                },
                new DialogueTreeChoice
                {
                    choiceText = "Hesitate at the shore",
                    nextNodeId = "ceremony_hesitate"
                }
            }
        };

        DialogueTreeNode accept = new DialogueTreeNode
        {
            nodeId = "ceremony_accept",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "I don't know why, but I know I have to go. The tide is calling, and I cannot ignore it.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroMC
            },
            nextNodeId = "ceremony_converge"
        };

        DialogueTreeNode hesitate = new DialogueTreeNode
        {
            nodeId = "ceremony_hesitate",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "Something holds me back. Fear, maybe. Or the weight of what I don't understand.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroMC
            },
            nextNodeId = "ceremony_converge"
        };

        DialogueTreeNode converge = new DialogueTreeNode
        {
            nodeId = "ceremony_converge",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "Five lights on the shore. Each one a different color. The tide does not wait for the willing. It takes the ready.",
                emotion = DialogueSystem.Emotion.Neutral
            }
        };

        tree.rootNode = root;
        tree.allNodes = new List<DialogueTreeNode> { root, accept, hesitate, converge };

        return tree;
    }

    /// <summary>
    /// Returns acceptance conversation dialogue as a branching tree for Act III.
    /// Player choices affect the emotional resolution.
    /// </summary>
    public static DialogueTree GetAcceptanceTree()
    {
        DialogueTree tree = new DialogueTree
        {
            treeId = "tree_acceptance_act3",
            title = "Acceptance Before The Last Shore"
        };

        DialogueTreeNode root = new DialogueTreeNode
        {
            nodeId = "accept_root",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "MC",
                dialogueText = "We have restored five islands. One remains. And after that... the tide will take what it is owed.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroMC
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "We face this together, no matter what",
                    nextNodeId = "accept_together",
                    increasesBond = true,
                    bondAmount = 10,
                    requiredBondLevel = 0
                },
                new DialogueTreeChoice
                {
                    choiceText = "I am afraid of what comes next",
                    nextNodeId = "accept_afraid",
                    increasesBond = true,
                    bondAmount = 5
                },
                new DialogueTreeChoice
                {
                    choiceText = "We should prepare for the worst",
                    nextNodeId = "accept_prepare"
                }
            }
        };

        DialogueTreeNode together = new DialogueTreeNode
        {
            nodeId = "accept_together",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Freida",
                dialogueText = "I have been holding on to all of you since the beginning. I am not letting go now.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroEarth
            },
            nextNodeId = "accept_final"
        };

        DialogueTreeNode afraid = new DialogueTreeNode
        {
            nodeId = "accept_afraid",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Killian",
                dialogueText = "Fear is honest. But courage is not the absence of fear. It is the choice to move anyway.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroFire
            },
            nextNodeId = "accept_final"
        };

        DialogueTreeNode prepare = new DialogueTreeNode
        {
            nodeId = "accept_prepare",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Briar",
                dialogueText = "The wind has already told me what to expect. But I will stand beside you regardless.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroAir
            },
            nextNodeId = "accept_final"
        };

        DialogueTreeNode final = new DialogueTreeNode
        {
            nodeId = "accept_final",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "The five stand at the edge of the last island. The tide is calm. The path is clear. They walk forward together.",
                emotion = DialogueSystem.Emotion.Neutral
            }
        };

        tree.rootNode = root;
        tree.allNodes = new List<DialogueTreeNode> { root, together, afraid, prepare, final };

        return tree;
    }

    // ------------------------------------------------------------------ //
    //  Utility
    // ------------------------------------------------------------------ //

    private static string MakePairKey(string heroA, string heroB)
    {
        int cmp = string.Compare(heroA, heroB, StringComparison.Ordinal);
        return cmp <= 0
            ? $"{heroA}|{heroB}"
            : $"{heroB}|{heroA}";
    }
}
