using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates DialogueData ScriptableObjects for all 7 narrative chapters.
/// Uses Opus 4.7's dialogue content with full character voices.
/// Access via: TIDE > Populate Dialogue Data
/// </summary>
public static class DialogueDataPopulator
{
    private const string OutputFolder = "Assets/Resources/Dialogue";

    [MenuItem("TIDE/Populate Dialogue Data")]
    public static void PopulateAllDialogue()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = Path.GetDirectoryName(OutputFolder);
            string folderName = Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        created += CreateChapter0();
        created += CreateChapter1();
        created += CreateChapter2();
        created += CreateChapter3();
        created += CreateChapter4();
        created += CreateChapter5();
        created += CreateChapter6();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DialogueDataPopulator] Created {created} dialogue assets");
        EditorUtility.DisplayDialog("Dialogue Created",
            $"Created {created} DialogueData assets.\n\n" +
            "Chapter 0: The Gathering Tide (Greed)\n" +
            "Chapter 1: Salt and Song (Lust)\n" +
            "Chapter 2: The Burning Coast (Wrath)\n" +
            "Chapter 3: The Sleeping Shore (Sloth)\n" +
            "Chapter 4: The Mirror Peaks (Pride)\n" +
            "Chapter 5: The Envious Deep (Envy)\n" +
            "Chapter 6: The Last Tide (Gluttony)",
            "OK");
    }

    [MenuItem("TIDE/Populate Dialogue Data", true)]
    public static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }

    private static int CreateAsset(string id, DialogueData data)
    {
        string path = $"{OutputFolder}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<DialogueData>(path) != null)
        {
            Debug.Log($"[DialogueDataPopulator] Skipping {id} (exists)");
            return 0;
        }
        AssetDatabase.CreateAsset(data, path);
        return 1;
    }

    // ============================================================
    // Chapter 0: The Gathering Tide (Greed) — Act I
    // Tone: Tense, adventurous, cautiously optimistic
    // ============================================================
    private static int CreateChapter0()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_0_greed";
        data.chapterName = "The Gathering Tide";
        data.islandId = "greed";
        data.act = "Act I";
        data.tone = "Tense, adventurous, cautiously optimistic — five strangers thrown together.";
        data.ancientTextId = "text_greed_intro";
        data.relationshipImpact = "Baseline bonds form. Killian–Merrick begin a reluctant DPS/healer trust. Aether is set apart as the one who 'already knows.'";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Arrival",
                description = "The five heroes wake on the shore of Greed with no memory of how they arrived, only a shared certainty they must 'cleanse' the island.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Killian", text = "I don't know any of you. I don't know why I trust you to have my back. But I do. That bothers me more than the monsters.", emotion = "guarded" },
                    new DialogueLine { speaker = "Merrick", text = "Then let it bother you later! Freida, wall — Killian, when the Idol swings, I've got you. We're not losing anyone on day one.", emotion = "determined" },
                }
            },
            new StoryBeat {
                beatName = "First Combat",
                description = "First combat teaches them their elements complement one another; they win only by covering each other's weaknesses.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The battle is clumsy, uncoordinated — but the elements respond to each other like old friends. Fire warms Water. Earth roots Air. Space bends around them all.", emotion = "neutral" },
                }
            },
            new StoryBeat {
                beatName = "Ancient Text Discovery",
                description = "Freida discovers the first ancient text half-buried in golden silt.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Freida", text = "There's writing under this. Old. It says… 'You are not the first to stand here.' Aether — do you know what that means?", emotion = "curious" },
                    new DialogueLine { speaker = "Aether", text = "I know that the sea remembers every hand that ever touched it. Read on. Or don't. The words will find you regardless.", emotion = "knowing" },
                }
            },
            new StoryBeat {
                beatName = "Idol Defeated",
                description = "They defeat the Golden Idol and feel a wave of the world's 'tide' return to them — but Aether goes quiet, as if he already knew.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The Golden Idol shatters. A warmth returns to the shore — the tide recedes, and for a moment, the world feels whole. Aether says nothing. He is already looking at the next island.", emotion = "neutral" },
                }
            },
        };

        return CreateAsset("chapter_0_greed", data);
    }

    // ============================================================
    // Chapter 1: Salt and Song (Lust) — Act I
    // Tone: Warmer, playful, bonding
    // ============================================================
    private static int CreateChapter1()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_1_lust";
        data.chapterName = "Salt and Song";
        data.islandId = "lust";
        data.act = "Act I";
        data.tone = "Warmer, playful, bonding — the group starts to feel like friends.";
        data.ancientTextId = "text_lust_intro";
        data.relationshipImpact = "Strong bonding chapter. Briar–Freida trust deepens; Killian–Merrick tension softens into genuine concern. Party morale peaks here.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Siren's Illusion",
                description = "Lust's sirens turn the heroes' desires against them; they must resist illusions of the lives they think they want.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Briar", text = "The siren showed me a stage. Thousands of people. All of them cheering for me. …Is it strange that I wanted to stay?", emotion = "vulnerable" },
                    new DialogueLine { speaker = "Freida", text = "It's not strange. It means you know what you love. Just don't let it choose for you.", emotion = "gentle" },
                }
            },
            new StoryBeat {
                beatName = "Merrick's Sacrifice",
                description = "Merrick uses Pain Absorption for the first time to revive Briar, revealing the cost it takes on him.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Merrick", text = "See? Told you. Nobody dies while I'm breathing. Give me a second, the room's spinning a little.", emotion = "weak" },
                    new DialogueLine { speaker = "Killian", text = "You take their pain into yourself. That's not a power, Merrick. That's a wound you keep reopening.", emotion = "concerned" },
                }
            },
            new StoryBeat {
                beatName = "Second Text",
                description = "A second text reveals past heroes fought 'the same six.' The group laughs it off — coincidence.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The text speaks of five warriors who fought the same six beasts. The group reads it together, then sets it aside. It must be coincidence. It has to be.", emotion = "neutral" },
                }
            },
            new StoryBeat {
                beatName = "Campfire",
                description = "Killian opens up to Freida about his temper for the first time.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Killian", text = "I almost hit Merrick today. During the fight. The rage just — it wasn't mine. It was the island's. How do I tell the difference?", emotion = "honest" },
                    new DialogueLine { speaker = "Freida", text = "You just did. That's how.", emotion = "warm" },
                }
            },
        };

        return CreateAsset("chapter_1_lust", data);
    }

    // ============================================================
    // Chapter 2: The Burning Coast (Wrath) — Act I → Act II hinge
    // Tone: Adventurous but the first shadow falls
    // ============================================================
    private static int CreateChapter2()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_2_wrath";
        data.chapterName = "The Burning Coast";
        data.islandId = "wrath";
        data.act = "Act I";
        data.tone = "Adventurous but the first shadow falls — optimism cracks.";
        data.ancientTextId = "text_wrath_intro";
        data.relationshipImpact = "First real fracture — Killian frightens the group. Merrick's steadying role becomes central. Aether's evasion starts to unsettle everyone.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Wrath Amplifies",
                description = "Wrath amplifies Killian's rage; his power spikes but he nearly hits Merrick mid-battle.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Killian", text = "Get back — GET BACK, all of you, I can't— I don't know where the fire stops and I start!", emotion = "terrified" },
                    new DialogueLine { speaker = "Merrick", text = "Hey. Hey. Look at me. Not the island. Me. Breathe. The rage is theirs, not yours. Give it back to them.", emotion = "steady" },
                }
            },
            new StoryBeat {
                beatName = "The Truth Emerges",
                description = "The lust_deep and wrath_intro texts together confirm: every past cycle read these exact words and reached one conclusion.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Aether", text = "Anger is honest, at least. It is the first door everyone walks through when they begin to understand what we are.", emotion = "solemn" },
                    new DialogueLine { speaker = "Freida", text = "'What we are.' You keep saying that. Aether — what are we?", emotion = "demanding" },
                    new DialogueLine { speaker = "Aether", text = "…Ask the water again, once we reach the still islands. Not yet.", emotion = "evasive" },
                }
            },
            new StoryBeat {
                beatName = "Hollow Victory",
                description = "The Crimson Warlord falls, but the victory feels hollow — the ancient texts are no longer a game.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The Warlord's armor cracks and releases nothing but smoke. Just like the Idol. Just like all of them. Behind every crown of vice, the same emptiness. The same design.", emotion = "neutral" },
                }
            },
        };

        return CreateAsset("chapter_2_wrath", data);
    }

    // ============================================================
    // Chapter 3: The Sleeping Shore (Sloth) — Act II
    // Tone: Heavy, dreamlike, melancholic. The truth surfaces.
    // ============================================================
    private static int CreateChapter3()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_3_sloth";
        data.chapterName = "The Sleeping Shore";
        data.islandId = "sloth";
        data.act = "Act II";
        data.tone = "Heavy, dreamlike, melancholic. The truth surfaces.";
        data.ancientTextId = "text_sloth_intro";
        data.relationshipImpact = "Bonds are deepest and most fragile here. Killian–Merrick roles invert (Killian becomes protector). Everyone silently agrees not to speak the truth aloud — closeness through shared denial.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "False Memories",
                description = "Sloth's haze makes the heroes relive fragments of 'memories' that aren't theirs — past heroes' final days.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Freida", text = "I dreamed I was standing here a hundred years ago. Same shore. Different face. I said goodbye to four people I loved. …I don't want to talk about it.", emotion = "haunted" },
                    new DialogueLine { speaker = "Briar", text = "Then we won't. We'll just — keep going. That's allowed, right? To just keep going and not name it?", emotion = "quiet" },
                }
            },
            new StoryBeat {
                beatName = "Merrick's Confession",
                description = "Merrick, exhausted from constant reviving, confesses he's afraid of what he'll be without someone to save.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Merrick", text = "What happens to me if there's no one left to catch? I don't know who I am if I'm not the one who takes the pain.", emotion = "breaking" },
                    new DialogueLine { speaker = "Killian", text = "Then I'll be the one who takes yours, for once. Deal?", emotion = "gentle" },
                }
            },
            new StoryBeat {
                beatName = "The Truth Acknowledged",
                description = "The player fully realizes the truth: they are manifestations of Light and Dark, made to fade after the sixth island.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The haze lifts. The truth does not. They are what the ancient texts always said — conjured, temporary, necessary. Five lights burning bright and brief. The Somnolent falls. No one celebrates.", emotion = "neutral" },
                }
            },
        };

        return CreateAsset("chapter_3_sloth", data);
    }

    // ============================================================
    // Chapter 4: The Mirror Peaks (Pride) — Act II
    // Tone: Unsettling, cold, isolating. Denial curdles.
    // ============================================================
    private static int CreateChapter4()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_4_pride";
        data.chapterName = "The Mirror Peaks";
        data.islandId = "pride";
        data.act = "Act II";
        data.tone = "Unsettling, cold, isolating. Denial curdles.";
        data.ancientTextId = "text_pride_intro";
        data.relationshipImpact = "First genuine rift with lasting weight. Briar's resentment (her Bad-Ending seed) shows. Freida's fear of losing the group surfaces. The party's unity is now effortful, not natural.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Mirror Illusions",
                description = "Pride's Mirror Knights show each hero an idealized, immortal version of themselves — the person they'd be if they didn't have to fade.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Briar", text = "The mirror showed me a version of me that gets to grow old. That gets to make things no one asked her to make. Why do they get that and we don't?", emotion = "resentful" },
                    new DialogueLine { speaker = "Freida", text = "Because they aren't real, Briar. And you froze. I felt the Knight's blade an inch from me while you looked at a reflection.", emotion = "hurt" },
                    new DialogueLine { speaker = "Briar", text = "…I'm sorry. I am. I just—", emotion = "ashamed" },
                }
            },
            new StoryBeat {
                beatName = "Aether's Warning",
                description = "The sloth_deep text — a past hero's hope for 'the next of us' — lands like grief.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Aether", text = "Pride is the mirror that tells you you are owed a longer story. It is the cruelest island. Do not linger at your reflection, Briar. It does not love you back.", emotion = "firm" },
                }
            },
            new StoryBeat {
                beatName = "Fractured Peace",
                description = "A fragile group argument erupts and is smothered; no one wants to be the one who broke the peace.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The Grand Monarch falls. The mirrors shatter. But some of the reflections linger — not on the walls, but behind the eyes. No one speaks on the walk to the next shore.", emotion = "neutral" },
                }
            },
        };

        return CreateAsset("chapter_4_pride", data);
    }

    // ============================================================
    // Chapter 5: The Envious Deep (Envy) — Act II climax
    // Tone: Darkest point — melancholy tips toward despair before the turn
    // ============================================================
    private static int CreateChapter5()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_5_envy";
        data.chapterName = "The Envious Deep";
        data.islandId = "envy";
        data.act = "Act II";
        data.tone = "Darkest point — melancholy tips toward despair before the turn.";
        data.ancientTextId = "text_envy_final";
        data.relationshipImpact = "Rock bottom, then repair. Briar reconciles (avoiding her Bad Ending). Merrick's crisis is caught by Killian. The party chooses each other consciously — the emotional groundwork for acceptance.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Envy's Weapon",
                description = "Envy weaponizes the heroes' resentment of each other's powers; Briar's jealousy is turned against the party.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Merrick", text = "I can revive a body. I can take a broken arm, a burn, a drowning. I can't take this. I've never once been unable to help and I— I can't do anything.", emotion = "breaking" },
                    new DialogueLine { speaker = "Killian", text = "You already did. You kept us together long enough to get here. Let me carry the rest. You taught me how.", emotion = "strong" },
                }
            },
            new StoryBeat {
                beatName = "Reconciliation",
                description = "The group finally says it out loud: they are going to die, and soon, and they were never meant to be more than tools.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Briar", text = "I hated you all. For a while. For having pieces of the world I didn't. I'm sorry. If we only have a little left, I don't want to spend it jealous.", emotion = "honest" },
                    new DialogueLine { speaker = "Freida", text = "Then don't. Sit. Right here, with us. That's all any of the ones before us wanted, I think. Just this.", emotion = "peaceful" },
                }
            },
            new StoryBeat {
                beatName = "The Usurper Falls",
                description = "In the quiet after, the pride_intro text brings the first sliver of clarity, not comfort.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "The Usurper falls. The envy dissolves. And in the quiet, for the first time, they sit together without pretense. Not fighting. Not strategizing. Just being. The next island waits. They are ready.", emotion = "neutral" },
                }
            },
        };

        return CreateAsset("chapter_5_envy", data);
    }

    // ============================================================
    // Chapter 6: The Last Tide (Gluttony) — Act III
    // Tone: Quiet, accepting, tender. No more denial — just presence.
    // ============================================================
    private static int CreateChapter6()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.chapterId = "chapter_6_gluttony";
        data.chapterName = "The Last Tide";
        data.islandId = "gluttony";
        data.act = "Act III";
        data.tone = "Quiet, accepting, tender. No more denial — just presence.";
        data.ancientTextId = "text_envy_final";
        data.relationshipImpact = "Complete. Every arc resolves — Killian's peace, Merrick's letting-go, Briar's reconciliation, Freida's gratitude. The bonds built across six islands are what make the fade land. Acceptance, together.";

        data.storyBeats = new StoryBeat[]
        {
            new StoryBeat {
                beatName = "Final Approach",
                description = "The heroes reach the final island knowing exactly what victory costs. No one turns back.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Aether", text = "You wanted to know what we are. We are the pause between a wave rising and a wave falling. We were never supposed to last. But we were supposed to matter — and we did.", emotion = "serene" },
                }
            },
            new StoryBeat {
                beatName = "Last Conversations",
                description = "Brief, unhurried conversation before the Devourer — each says the thing they'd been avoiding.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Killian", text = "I spent so long afraid of what was inside me. Turns out it was just… this. All of you. I'm not angry anymore.", emotion = "at peace" },
                    new DialogueLine { speaker = "Merrick", text = "Promise me something. Whoever comes next — they won't wake up as strangers. Leave them something. So they're a little less alone than we were.", emotion = "hopeful" },
                    new DialogueLine { speaker = "Freida", text = "Then let's write it down. One last text. For the next five. …I'm glad it was you four. Truly.", emotion = "grateful" },
                    new DialogueLine { speaker = "Briar", text = "Me too. Ready?", emotion = "calm" },
                }
            },
            new StoryBeat {
                beatName = "The Final Battle",
                description = "They fight as one, movements flawless because they finally trust completely.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "They move like water through stone. No hesitation. No wasted breath. Every element in harmony — not because they were designed to fit, but because they chose to. The Devourer falls. The tide returns. The world breathes.", emotion = "neutral" },
                }
            },
            new StoryBeat {
                beatName = "The Fade",
                description = "The Devourer falls; balance is restored; the heroes begin to fade. The final text appears as they dissolve into light and dark.",
                lines = new DialogueLine[]
                {
                    new DialogueLine { speaker = "Narrator", text = "They feel it first in their hands — a softness, like morning fog. Freida's fingers become light. Merrick's warmth becomes shadow. Briar's breath becomes wind. Killian's fire becomes stars. Aether smiles.", emotion = "bittersweet" },
                    new DialogueLine { speaker = "Aether", text = "See? Not so bad. Just a very long, beautiful dream. And someone else will dream it soon.", emotion = "peaceful" },
                    new DialogueLine { speaker = "Narrator", text = "They write their last words on the final stone. Not for themselves — for the next five. For the strangers who will wake on a golden shore and wonder why they trust each other so quickly. The answer is simple: because someone loved them first. The tide recedes. Silence. Then, somewhere, a new shore. A new tide. A new beginning.", emotion = "bittersweet" },
                }
            },
        };

        return CreateAsset("chapter_6_gluttony", data);
    }
}
