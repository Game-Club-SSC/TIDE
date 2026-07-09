using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static factory methods that produce authored <see cref="DialogueTree"/> instances
/// for every narrative beat across the game's 7 chapters.
/// All content is hardcoded in C#, following the existing codebase pattern.
///
/// Usage: call the relevant factory method and pass the result to
/// <see cref="DialogueSystem.StartDialogueTree"/> or wire it to a
/// <see cref="DialogueTrigger"/> component.
/// </summary>
public static class HeroDialogueContent
{
    // ================================================================== //
    //  Hero ID Constants
    // ================================================================== //

    public const string HeroEmber = "hero_ember";
    public const string HeroTidecaller = "hero_tidecaller";
    public const string HeroStoneheart = "hero_stoneheart";
    public const string HeroZephyr = "hero_zephyr";
    public const string HeroVoidwalker = "hero_voidwalker";

    // ================================================================== //
    //  Tree ID Constants
    // ================================================================== //

    public const string CeremonyTreeId = "tree_ceremony_ch01";
    public const string CharacterIntroTreeId = "tree_character_intro_ch02";
    public const string AncientTextReactionActIId = "tree_ancient_text_act1";
    public const string AncientTextReactionActIIId = "tree_ancient_text_act2";
    public const string AncientTextReactionActIIIId = "tree_ancient_text_act3";
    public const string PreBossGreedTreeId = "tree_pre_boss_greed";
    public const string PreBossAttachmentTreeId = "tree_pre_boss_attachment";
    public const string PreBossJealousyTreeId = "tree_pre_boss_jealousy";
    public const string PreBossLustTreeId = "tree_pre_boss_lust";
    public const string PreBossAngerTreeId = "tree_pre_boss_anger";
    public const string PreBossEgoTreeId = "tree_pre_boss_ego";
    public const string AcceptanceTreeId = "tree_acceptance_act3";

    // ================================================================== //
    //  Helper — builds a linear tree (no branching)
    // ================================================================== //

    private static DialogueTree MakeLinearTree(string treeId, string title, DialogueSystem.Emotion defaultEmotion, params (string speaker, string text, string heroId)[] lines)
    {
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = title,
            allNodes = new List<DialogueTreeNode>()
        };

        DialogueTreeNode prev = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_node_{i}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i].speaker,
                    dialogueText = lines[i].text,
                    emotion = defaultEmotion,
                    relatedHeroId = lines[i].heroId ?? string.Empty
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);

            if (prev != null)
            {
                prev.nextNodeId = nodeId;
            }
            else
            {
                tree.rootNode = node;
            }

            prev = node;
        }

        return tree;
    }

    // ================================================================== //
    //  A. Ceremony Dialogue (Ch 0/1)
    // ================================================================== //

    /// <summary>
    /// Dialogue tree for the coming-of-age ceremony.
    /// Plays after the tide flash, showing hero reactions to their awakening.
    /// Follows the existing CeremonyIntroDirector narrative cards.
    /// </summary>
    public static DialogueTree CeremonyDialogue()
    {
        string treeId = CeremonyTreeId;
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "The Ceremony",
            allNodes = new List<DialogueTreeNode>()
        };

        // --- Act I: The Ceremony ---
        string[][] lines = new string[][]
        {
            // nodeId, speaker, text, emotion, heroId
            new[] { "narrator_0", "Narrator", "Every hundred years, the Tide calls five souls...", "Neutral", "" },
            new[] { "narrator_1", "Narrator", "Born as ordinary children, they do not know what they are...", "Neutral", "" },
            new[] { "narrator_2", "Narrator", "Until the Ceremony reveals their true nature...", "Neutral", "" },
            // --- After the tide flash ---
            new[] { "ember_0", "Ember", "What... what is happening to me? My hands are glowing.", "Worried", HeroEmber },
            new[] { "tidecaller_0", "Tidecaller", "The water... it's singing. Can you hear it?", "Happy", HeroTidecaller },
            new[] { "stoneheart_0", "Stoneheart", "I feel the ground trembling beneath us. Not fear — recognition.", "Determined", HeroStoneheart },
            new[] { "zephyr_0", "Zephyr", "The wind knows my name. It always has. I just never listened.", "Happy", HeroZephyr },
            new[] { "voidwalker_0", "Voidwalker", "...", "Neutral", HeroVoidwalker },
            new[] { "voidwalker_1", "Voidwalker", "I see the spaces between things. The quiet where stories end.", "Sad", HeroVoidwalker },
            new[] { "narrator_3", "Narrator", "You are the Chosen. The Tide is your burden and your gift.", "Determined", "" },
        };

        DialogueTreeNode prev = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_{lines[i][0]}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i][1],
                    dialogueText = lines[i][2],
                    emotion = ParseEmotion(lines[i][3]),
                    relatedHeroId = lines[i][4]
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);
            if (prev != null) prev.nextNodeId = nodeId;
            else tree.rootNode = node;
            prev = node;
        }

        // --- Choice moment ---
        string choiceNodeId = $"{treeId}_choice_ember";
        DialogueTreeNode choiceNode = new DialogueTreeNode
        {
            nodeId = choiceNodeId,
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "So... what do we do now?",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroEmber
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "We'll carry it together.",
                    nextNodeId = $"{treeId}_together",
                    increasesBond = true,
                    bondAmount = 5
                },
                new DialogueTreeChoice
                {
                    choiceText = "I didn't ask for this.",
                    nextNodeId = $"{treeId}_reluctant",
                    increasesBond = false
                }
            }
        };

        tree.allNodes.Add(choiceNode);
        if (prev != null) prev.nextNodeId = choiceNodeId;

        // Together response
        DialogueTreeNode togetherNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_together",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Tidecaller",
                dialogueText = "The Tide chose us for a reason. Whatever comes, we face it as five.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroTidecaller
            },
            effects = new DialogueTreeEffect[]
            {
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroEmber, intValue = 5 },
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroTidecaller, intValue = 5 }
            }
        };

        // Reluctant response
        DialogueTreeNode reluctantNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_reluctant",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Stoneheart",
                dialogueText = "Nobody does. But the tide doesn't ask permission.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroStoneheart
            }
        };

        // Final node
        DialogueTreeNode finalNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_end",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Zephyr",
                dialogueText = "Then let's move. Standing still won't change what we are.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroZephyr
            }
        };

        tree.allNodes.Add(togetherNode);
        tree.allNodes.Add(reluctantNode);
        tree.allNodes.Add(finalNode);

        togetherNode.nextNodeId = finalNode.nodeId;
        reluctantNode.nextNodeId = finalNode.nodeId;

        return tree;
    }

    // ================================================================== //
    //  B. Character Introduction Dialogue (Ch 2)
    // ================================================================== //

    /// <summary>
    /// Dialogue tree where the five heroes meet and travel to the first island.
    /// Each hero's personality is on display. Includes a choice that affects bonds.
    /// </summary>
    public static DialogueTree CharacterIntroDialogue()
    {
        string treeId = CharacterIntroTreeId;
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "Five Strangers",
            allNodes = new List<DialogueTreeNode>()
        };

        string[][] lines = new string[][]
        {
            new[] { "ember_0", "Ember", "So we're really doing this. Five strangers, one mission, no plan.", "Worried", HeroEmber },
            new[] { "tidecaller_0", "Tidecaller", "We're not strangers. The Tide chose us together. That has to mean something.", "Happy", HeroTidecaller },
            new[] { "stoneheart_0", "Stoneheart", "Choice implies options. There were none. The tide called. We answered.", "Neutral", HeroStoneheart },
            new[] { "zephyr_0", "Zephyr", "Lighten up, Stone. At least the scenery's nice. Could be worse — could be raining.", "Happy", HeroZephyr },
            new[] { "voidwalker_0", "Voidwalker", "The texts in the ruins... they mention a cycle. This has happened before.", "Worried", HeroVoidwalker },
            new[] { "ember_1", "Ember", "What do you mean, before? Like... past Chosen?", "Worried", HeroEmber },
            new[] { "voidwalker_1", "Voidwalker", "Exactly like that. And the texts say the cycle doesn't always end well.", "Sad", HeroVoidwalker },
        };

        DialogueTreeNode prev = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_{lines[i][0]}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i][1],
                    dialogueText = lines[i][2],
                    emotion = ParseEmotion(lines[i][3]),
                    relatedHeroId = lines[i][4]
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);
            if (prev != null) prev.nextNodeId = nodeId;
            else tree.rootNode = node;
            prev = node;
        }

        // Choice: investigate texts or focus on the journey
        string choiceNodeId = $"{treeId}_choice";
        DialogueTreeNode choiceNode = new DialogueTreeNode
        {
            nodeId = choiceNodeId,
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "What should we focus on first?",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroEmber
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "Tell us more about the texts.",
                    nextNodeId = $"{treeId}_texts",
                    increasesBond = true,
                    bondAmount = 5
                },
                new DialogueTreeChoice
                {
                    choiceText = "Let's just get moving. We'll figure it out on the way.",
                    nextNodeId = $"{treeId}_journey",
                    increasesBond = true,
                    bondAmount = 5
                }
            }
        };

        tree.allNodes.Add(choiceNode);
        if (prev != null) prev.nextNodeId = choiceNodeId;

        // Texts path — bond with Voidwalker
        DialogueTreeNode textsNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_texts",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Voidwalker",
                dialogueText = "The fragments speak of balance — and what happens when it's lost. There are six seals. Six vices that must be confronted.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroVoidwalker
            },
            effects = new DialogueTreeEffect[]
            {
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroVoidwalker, intValue = 5 }
            }
        };

        // Journey path — bond with Ember
        DialogueTreeNode journeyNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_journey",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "That's the spirit. We'll learn what we need to learn when we need to learn it.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroEmber
            },
            effects = new DialogueTreeEffect[]
            {
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroEmber, intValue = 5 }
            }
        };

        // Convergence node
        DialogueTreeNode convergenceNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_convergence",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Tidecaller",
                dialogueText = "The island ahead... I can feel its pain. Something is deeply wrong there.",
                emotion = DialogueSystem.Emotion.Worried,
                relatedHeroId = HeroTidecaller
            }
        };

        DialogueTreeNode finalNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_final",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Stoneheart",
                dialogueText = "Then we walk toward it. That's what we were chosen for.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroStoneheart
            }
        };

        tree.allNodes.Add(textsNode);
        tree.allNodes.Add(journeyNode);
        tree.allNodes.Add(convergenceNode);
        tree.allNodes.Add(finalNode);

        textsNode.nextNodeId = convergenceNode.nodeId;
        journeyNode.nextNodeId = convergenceNode.nodeId;
        convergenceNode.nextNodeId = finalNode.nodeId;

        return tree;
    }

    // ================================================================== //
    //  C. Ancient Text Discovery Reactions (Progressive Tone Shift)
    // ================================================================== //

    /// <summary>
    /// Act I reaction: curious, hopeful. Heroes discover their first ancient texts.
    /// </summary>
    public static DialogueTree AncientTextReactionActI()
    {
        return MakeLinearTree(
            AncientTextReactionActIId,
            "The First Fragments",
            DialogueSystem.Emotion.Neutral,
            (HeroVoidwalker, "Another fragment. These texts are older than anything I've seen carved into stone.", HeroVoidwalker),
            (HeroEmber, "What do they say?", HeroEmber),
            (HeroVoidwalker, "They speak of a balance that was broken. And five who were meant to mend it.", HeroVoidwalker),
            (HeroTidecaller, "That's us, isn't it? This was always the plan.", HeroTidecaller),
            (HeroStoneheart, "Plans can be wrong. But the stone remembers what the words say.", HeroStoneheart),
            (HeroZephyr, "Well, if the ancient rocks say we're heroes, who are we to argue?", HeroZephyr)
        );
    }

    /// <summary>
    /// Act II reaction: unsettled, growing dread. The texts reveal darker truths.
    /// </summary>
    public static DialogueTree AncientTextReactionActII()
    {
        return MakeLinearTree(
            AncientTextReactionActIIId,
            "Darker Pages",
            DialogueSystem.Emotion.Sad,
            (HeroVoidwalker, "The texts grow darker here. They don't just describe the cycle — they warn about it.", HeroVoidwalker),
            (HeroEmber, "Warn about what? What aren't you saying?", HeroEmber),
            (HeroVoidwalker, "The five don't always survive. Sometimes... one falls.", HeroVoidwalker),
            (HeroZephyr, "Then we make sure that doesn't happen. Simple.", HeroZephyr),
            (HeroTidecaller, "Air is right. We've come too far to doubt now.", HeroTidecaller),
            (HeroStoneheart, "Doubt isn't the enemy. Forgetting why we're here is.", HeroStoneheart)
        );
    }

    /// <summary>
    /// Act III reaction: somber, resolved. The final texts acceptance of cost.
    /// </summary>
    public static DialogueTree AncientTextReactionActIII()
    {
        return MakeLinearTree(
            AncientTextReactionActIIIId,
            "The Last Words",
            DialogueSystem.Emotion.Sad,
            (HeroVoidwalker, "The final texts. They don't warn anymore. They accept.", HeroVoidwalker),
            (HeroStoneheart, "What do they say?", HeroStoneheart),
            (HeroVoidwalker, "That the tide must be balanced. And balance always costs something.", HeroVoidwalker),
            (HeroEmber, "We knew this. We've always known.", HeroEmber),
            (HeroTidecaller, "The question was never if. It was how we face it.", HeroTidecaller),
            (HeroZephyr, "Together. That's how. The only way that matters.", HeroZephyr)
        );
    }

    // ================================================================== //
    //  D. Pre-Boss Conversations (6 bosses)
    // ================================================================== //

    /// <summary>
    /// Pre-boss conversation for Greed (island_greed). The temple of gold.
    /// </summary>
    public static DialogueTree PreBossGreedDialogue()
    {
        return MakeLinearTree(
            PreBossGreedTreeId,
            "Before the Temple",
            DialogueSystem.Emotion.Neutral,
            (HeroEmber, "That temple... I can smell the gold from here. It's intoxicating.", HeroEmber),
            (HeroTidecaller, "It's not real gold. It's longing given form.", HeroTidecaller),
            (HeroStoneheart, "Doesn't matter if it's real. The wanting is. That's what makes it dangerous.", HeroStoneheart),
            (HeroZephyr, "Keep your hands in your pockets and your head clear. That's all we can do.", HeroZephyr),
            (HeroVoidwalker, "Greed doesn't take what you have. It makes you forget what matters.", HeroVoidwalker)
        );
    }

    /// <summary>
    /// Pre-boss conversation for Attachment (island_desire). The garden of memories.
    /// </summary>
    public static DialogueTree PreBossAttachmentDialogue()
    {
        return MakeLinearTree(
            PreBossAttachmentTreeId,
            "The Garden of What Was",
            DialogueSystem.Emotion.Sad,
            (HeroEmber, "This garden... it reminds me of someone I used to know. Someone I lost.", HeroEmber),
            (HeroTidecaller, "That's the point. It pulls you back to what you've lost.", HeroTidecaller),
            (HeroStoneheart, "Loss is a stone you carry. It doesn't get lighter. You just get stronger.", HeroStoneheart),
            (HeroZephyr, "Maybe it's not about carrying it. Maybe it's about knowing when to set it down.", HeroZephyr),
            (HeroVoidwalker, "Attachment isn't love. It's the fear of love ending.", HeroVoidwalker)
        );
    }

    /// <summary>
    /// Pre-boss conversation for Jealousy (island_envy). The beach of mirrors.
    /// </summary>
    public static DialogueTree PreBossJealousyDialogue()
    {
        return MakeLinearTree(
            PreBossJealousyTreeId,
            "The Mirror Beach",
            DialogueSystem.Emotion.Worried,
            (HeroEmber, "Those mirrors... why do I look... better than I am?", HeroEmber),
            (HeroTidecaller, "Because they show what you want to be, not what you are.", HeroTidecaller),
            (HeroStoneheart, "And what the others are. The comparisons that eat you alive.", HeroStoneheart),
            (HeroZephyr, "I don't need a mirror to know I'm enough. Neither do any of you.", HeroZephyr),
            (HeroVoidwalker, "Jealousy is just love with nowhere to go.", HeroVoidwalker)
        );
    }

    /// <summary>
    /// Pre-boss conversation for Lust (island_lust). The enchanted moura.
    /// </summary>
    public static DialogueTree PreBossLustDialogue()
    {
        return MakeLinearTree(
            PreBossLustTreeId,
            "The Enchanted Shore",
            DialogueSystem.Emotion.Worried,
            (HeroEmber, "The air feels thick here. Sweet. Dangerous.", HeroEmber),
            (HeroTidecaller, "Enchantment. It makes you want what you don't need.", HeroTidecaller),
            (HeroStoneheart, "Wanting is a weakness. Discipline is the answer.", HeroStoneheart),
            (HeroZephyr, "Or maybe just knowing the difference between want and need.", HeroZephyr),
            (HeroVoidwalker, "Lust isn't about desire. It's about emptiness pretending to be full.", HeroVoidwalker)
        );
    }

    /// <summary>
    /// Pre-boss conversation for Anger (island_anger). The burning clearing.
    /// </summary>
    public static DialogueTree PreBossAngerDialogue()
    {
        string treeId = PreBossAngerTreeId;
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "The Burning Words",
            allNodes = new List<DialogueTreeNode>()
        };

        string[][] lines = new string[][]
        {
            new[] { "ember_0", "Ember", "I can feel it already. The words I've been holding back.", "Angry", HeroEmber },
            new[] { "tidecaller_0", "Tidecaller", "Don't let them out. That's what the boss wants.", "Worried", HeroTidecaller },
            new[] { "stoneheart_0", "Stoneheart", "Some words need saying. Just... not in rage.", "Determined", HeroStoneheart },
            new[] { "zephyr_0", "Zephyr", "Anger is wind without direction. Give it direction and it becomes purpose.", "Determined", HeroZephyr },
            new[] { "voidwalker_0", "Voidwalker", "The boss feeds on what you won't say. So say it — to each other, not to it.", "Neutral", HeroVoidwalker },
        };

        DialogueTreeNode prev = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_{lines[i][0]}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i][1],
                    dialogueText = lines[i][2],
                    emotion = ParseEmotion(lines[i][3]),
                    relatedHeroId = lines[i][4]
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);
            if (prev != null) prev.nextNodeId = nodeId;
            else tree.rootNode = node;
            prev = node;
        }

        // Choice moment — say what you've been holding back
        DialogueTreeNode choiceNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_choice",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "Fine. I'll go first. Stoneheart — you act like nothing affects you, and it drives me crazy.",
                emotion = DialogueSystem.Emotion.Angry,
                relatedHeroId = HeroEmber
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "Let the others speak too.",
                    nextNodeId = $"{treeId}_share",
                    increasesBond = true,
                    bondAmount = 5
                },
                new DialogueTreeChoice
                {
                    choiceText = "Keep it together. Save it for the boss.",
                    nextNodeId = $"{treeId}_suppress",
                    increasesBond = false
                }
            }
        };

        DialogueTreeNode shareNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_share",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Stoneheart",
                dialogueText = "You're right. I do that. I'm sorry. The weight of being the steady one... it's heavier than it looks.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroStoneheart
            },
            effects = new DialogueTreeEffect[]
            {
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroStoneheart, intValue = 5 }
            }
        };

        DialogueTreeNode suppressNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_suppress",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Tidecaller",
                dialogueText = "Ember's right. Anger saved for the boss is anger used wisely.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroTidecaller
            }
        };

        DialogueTreeNode endNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_end",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Voidwalker",
                dialogueText = "Whatever you're holding — let it fuel you, not consume you. Now we fight.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroVoidwalker
            }
        };

        tree.allNodes.Add(choiceNode);
        tree.allNodes.Add(shareNode);
        tree.allNodes.Add(suppressNode);
        tree.allNodes.Add(endNode);

        if (prev != null) prev.nextNodeId = choiceNode.nodeId;
        shareNode.nextNodeId = endNode.nodeId;
        suppressNode.nextNodeId = endNode.nodeId;

        return tree;
    }

    /// <summary>
    /// Pre-boss conversation for Ego (island_ego). The mountain peak.
    /// </summary>
    public static DialogueTree PreBossEgoDialogue()
    {
        string treeId = PreBossEgoTreeId;
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "The Last Peak",
            allNodes = new List<DialogueTreeNode>()
        };

        string[][] lines = new string[][]
        {
            new[] { "ember_0", "Ember", "We've beaten five bosses. We're strong. We can do this.", "Determined", HeroEmber },
            new[] { "tidecaller_0", "Tidecaller", "That's exactly what Ego wants you to think.", "Worried", HeroTidecaller },
            new[] { "stoneheart_0", "Stoneheart", "Strength isn't the issue. Knowing its limits is.", "Neutral", HeroStoneheart },
            new[] { "zephyr_0", "Zephyr", "Ego whispers that you're better than everyone. The trick is remembering you're not.", "Determined", HeroZephyr },
            new[] { "voidwalker_0", "Voidwalker", "Ego is the last enemy because it wears your own face.", "Sad", HeroVoidwalker },
        };

        DialogueTreeNode prev = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_{lines[i][0]}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i][1],
                    dialogueText = lines[i][2],
                    emotion = ParseEmotion(lines[i][3]),
                    relatedHeroId = lines[i][4]
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);
            if (prev != null) prev.nextNodeId = nodeId;
            else tree.rootNode = node;
            prev = node;
        }

        // Final affirmation
        DialogueTreeNode finalNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_final",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "We've come too far to let ego be the thing that breaks us. Together. As five. As we started.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroEmber
            }
        };

        tree.allNodes.Add(finalNode);
        if (prev != null) prev.nextNodeId = finalNode.nodeId;

        return tree;
    }

    // ================================================================== //
    //  E. Acceptance Conversation (Act III, pre-final-boss)
    // ================================================================== //

    /// <summary>
    /// Full branching acceptance conversation before the final boss.
    /// The heroes confront what the journey has cost them and what comes next.
    /// </summary>
    public static DialogueTree AcceptanceDialogue()
    {
        string treeId = AcceptanceTreeId;
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "Acceptance",
            allNodes = new List<DialogueTreeNode>()
        };

        string[][] lines = new string[][]
        {
            new[] { "ember_0", "Ember", "This is it. The last island.", "Determined", HeroEmber },
            new[] { "tidecaller_0", "Tidecaller", "The texts call it the Shore of Self. Where the tide meets its source.", "Neutral", HeroTidecaller },
            new[] { "stoneheart_0", "Stoneheart", "I've carried the tide through every island. If I turn back now, the rift widens.", "Determined", HeroStoneheart },
            new[] { "zephyr_0", "Zephyr", "If you press on, it costs you. If you turn back, it costs everyone.", "Worried", HeroZephyr },
            new[] { "voidwalker_0", "Voidwalker", "The cost was always part of the equation. We just didn't want to read the fine print.", "Sad", HeroVoidwalker },
        };

        DialogueTreeNode prev = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string nodeId = $"{treeId}_{lines[i][0]}";
            DialogueTreeNode node = new DialogueTreeNode
            {
                nodeId = nodeId,
                entry = new DialogueSystem.DialogueEntry
                {
                    speakerName = lines[i][1],
                    dialogueText = lines[i][2],
                    emotion = ParseEmotion(lines[i][3]),
                    relatedHeroId = lines[i][4]
                },
                nextNodeId = string.Empty
            };

            tree.allNodes.Add(node);
            if (prev != null) prev.nextNodeId = nodeId;
            else tree.rootNode = node;
            prev = node;
        }

        // Choice moment
        string choiceNodeId = $"{treeId}_choice";
        DialogueTreeNode choiceNode = new DialogueTreeNode
        {
            nodeId = choiceNodeId,
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Ember",
                dialogueText = "Then we end this together. One way or another.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroEmber
            },
            choices = new DialogueTreeChoice[]
            {
                new DialogueTreeChoice
                {
                    choiceText = "We pay it together.",
                    nextNodeId = $"{treeId}_together",
                    increasesBond = true,
                    bondAmount = 10
                },
                new DialogueTreeChoice
                {
                    choiceText = "There has to be another way.",
                    nextNodeId = $"{treeId}_another",
                    increasesBond = false
                },
                new DialogueTreeChoice
                {
                    choiceText = "I need a moment.",
                    nextNodeId = $"{treeId}_moment",
                    increasesBond = false
                }
            }
        };

        tree.allNodes.Add(choiceNode);
        if (prev != null) prev.nextNodeId = choiceNodeId;

        // Together path
        DialogueTreeNode togetherNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_together",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Tidecaller",
                dialogueText = "That's all I needed to hear. The tide will carry us — even through the end.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = HeroTidecaller
            },
            effects = new DialogueTreeEffect[]
            {
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroEmber, intValue = 10 },
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroTidecaller, intValue = 10 },
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroStoneheart, intValue = 10 },
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroZephyr, intValue = 10 },
                new DialogueTreeEffect { type = DialogueEffectType.IncreaseBond, targetId = HeroVoidwalker, intValue = 10 }
            }
        };

        // Another way path
        DialogueTreeNode anotherNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_another",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Voidwalker",
                dialogueText = "I've searched every text, every fragment. This is the only path. But I respect the hope.",
                emotion = DialogueSystem.Emotion.Sad,
                relatedHeroId = HeroVoidwalker
            }
        };

        // Need a moment path
        DialogueTreeNode momentNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_moment",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Stoneheart",
                dialogueText = "Take it. We'll be here when you're ready. We're not going anywhere.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = HeroStoneheart
            }
        };

        // Convergence
        DialogueTreeNode convergeNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_converge",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Zephyr",
                dialogueText = "Whatever happens on that shore... we faced it together. That's what matters.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = HeroZephyr
            }
        };

        DialogueTreeNode finalNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_final",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = "Narrator",
                dialogueText = "And so the five walk toward the final shore, carrying the weight of every choice that brought them here.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = ""
            }
        };

        tree.allNodes.Add(togetherNode);
        tree.allNodes.Add(anotherNode);
        tree.allNodes.Add(momentNode);
        tree.allNodes.Add(convergeNode);
        tree.allNodes.Add(finalNode);

        togetherNode.nextNodeId = convergeNode.nodeId;
        anotherNode.nextNodeId = convergeNode.nodeId;
        momentNode.nextNodeId = convergeNode.nodeId;
        convergeNode.nextNodeId = finalNode.nodeId;

        return tree;
    }

    // ================================================================== //
    //  F. Relationship-Dependent Dialogue Helpers
    // ================================================================== //

    /// <summary>
    /// Returns a dialogue tree with bond-gated variations.
    /// High bond (>= 60): warm, trusting exchange.
    /// Low bond (< 30): tense, professional exchange.
    /// Uses <see cref="DialogueTreeCondition"/> with <see cref="DialogueConditionType.BondLevel"/>.
    /// </summary>
    public static DialogueTree RelationshipVariationDialogue(string heroA, string heroB)
    {
        string treeId = $"tree_relationship_{heroA}_{heroB}";
        DialogueTree tree = new DialogueTree
        {
            treeId = treeId,
            title = "Bond Check",
            allNodes = new List<DialogueTreeNode>()
        };

        // Entry node — the speaker addresses the other hero
        DialogueTreeNode entryNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_entry",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = FormatHeroName(heroA),
                dialogueText = "We need to talk.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = heroA
            }
        };

        // High bond path
        DialogueTreeNode highBondNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_high",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = FormatHeroName(heroB),
                dialogueText = "I know. And I'm glad you asked. We've been through enough to be honest with each other.",
                emotion = DialogueSystem.Emotion.Happy,
                relatedHeroId = heroB
            },
            conditions = new DialogueTreeCondition[]
            {
                new DialogueTreeCondition
                {
                    type = DialogueConditionType.BondLevel,
                    targetId = $"{heroA}|{heroB}",
                    intValue = 60
                }
            }
        };

        // Low bond path
        DialogueTreeNode lowBondNode = new DialogueTreeNode
        {
            nodeId = $"{treeId}_low",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = FormatHeroName(heroB),
                dialogueText = "What about it? We need to work together, feelings aside.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = heroB
            }
        };

        // High bond conclusion
        DialogueTreeNode highConclusion = new DialogueTreeNode
        {
            nodeId = $"{treeId}_high_end",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = FormatHeroName(heroA),
                dialogueText = "We trust each other. That's stronger than any tide.",
                emotion = DialogueSystem.Emotion.Determined,
                relatedHeroId = heroA
            }
        };

        // Low bond conclusion
        DialogueTreeNode lowConclusion = new DialogueTreeNode
        {
            nodeId = $"{treeId}_low_end",
            entry = new DialogueSystem.DialogueEntry
            {
                speakerName = FormatHeroName(heroA),
                dialogueText = "Agreed. The tide doesn't care about feelings. Neither should we.",
                emotion = DialogueSystem.Emotion.Neutral,
                relatedHeroId = heroA
            }
        };

        entryNode.nextNodeId = highBondNode.nodeId; // Runner evaluates conditions
        highBondNode.nextNodeId = highConclusion.nodeId;
        lowBondNode.nextNodeId = lowConclusion.nodeId;

        tree.allNodes.Add(entryNode);
        tree.allNodes.Add(highBondNode);
        tree.allNodes.Add(lowBondNode);
        tree.allNodes.Add(highConclusion);
        tree.allNodes.Add(lowConclusion);

        return tree;
    }

    // ================================================================== //
    //  Utility
    // ================================================================== //

    private static DialogueSystem.Emotion ParseEmotion(string emotion)
    {
        switch (emotion)
        {
            case "Happy": return DialogueSystem.Emotion.Happy;
            case "Sad": return DialogueSystem.Emotion.Sad;
            case "Angry": return DialogueSystem.Emotion.Angry;
            case "Worried": return DialogueSystem.Emotion.Worried;
            case "Determined": return DialogueSystem.Emotion.Determined;
            default: return DialogueSystem.Emotion.Neutral;
        }
    }

    private static string FormatHeroName(string heroId)
    {
        switch (heroId)
        {
            case HeroEmber: return "Ember";
            case HeroTidecaller: return "Tidecaller";
            case HeroStoneheart: return "Stoneheart";
            case HeroZephyr: return "Zephyr";
            case HeroVoidwalker: return "Voidwalker";
            default: return heroId;
        }
    }
}
