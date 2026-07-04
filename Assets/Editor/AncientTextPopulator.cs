using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates AncientTextData ScriptableObjects from the narrative design spec.
/// Uses Opus 4.7's text — more poetic and emotionally resonant.
/// Access via: TIDE > Populate Ancient Texts
/// </summary>
public static class AncientTextPopulator
{
    private const string OutputFolder = "Assets/Resources/AncientTexts";

    private static readonly (string id, string title, string body)[] Texts = new[]
    {
        ("greed_intro", "The First Shore",
@"If you are reading this, then you have already begun, and it is too late to begin any other way. Do not be afraid of that. Everyone who has stood where you stand felt the same cold thread of doubt, and every one of them kept walking anyway. That is what you are for.

You will feel that you have always known one another, though you met only hours ago. You will trust hands you have no reason to trust. This is not madness. It is design. The tide binds those it chooses, so that they may do together what none could do alone. Lean into it. There is not enough time to be strangers.

Six islands lie ahead, each ruled by a sickness of the heart. Cleanse them and the world will breathe again. That is all the truth we will give you today. The rest, you must earn — the way we did, and the way the ones before us did. Rest tonight. Tomorrow, the water rises."),

        ("greed_deep", "The Same Six",
@"We counted them, once, thinking the count would protect us. Greed. Lust. Wrath. Sloth. Pride. Envy. Six sicknesses, six islands, and at the end of each, a crown to break. We told ourselves the number was arbitrary, that other heroes in other years must have faced other trials. We were wrong. It is always these six. It has always been these six.

Understand what that means before you decide how to feel about it. You are not the first to bleed on Greed's golden sand. Others stood here with your same certainty, your same borrowed trust, your same weapons in unfamiliar hands. They won. They wrote this. And then they were gone.

We do not say this to frighten you. We say it because the truth, met early and met plainly, hurts less than the truth that ambushes you later. Read on when you are ready. Some of you will want to. Some of you will slam the book shut. Both are allowed."),

        ("lust_intro", "What the Water Shows",
@"By now you are closer than you expected to be. You finish one another's sentences. You know who will move left and who will move right without a word passing between you. Hold onto this feeling — write it somewhere it cannot be taken from you — because Lust will try to convince you that you want something else more.

The sirens do not lie, exactly. They show you a true thing: the life you might have wanted, the crowd, the quiet house, the ordinary years. The cruelty is that the thing they show you was never yours to have. You were not made for the long, slow life. You were made for this shore and these companions and this brief, bright purpose.

That is not a lesser thing. A candle is not lesser than the sun for burning shorter. But you must choose the candle knowingly, before the island chooses for you. Look at the four beside you. That is the life you were given. It is enough. Make it enough."),

        ("lust_deep", "Every Road, One Door",
@"We hoped, on this island, that the texts would branch — that somewhere in the archive was a version left by heroes who found another way. We searched. We read every word the past had left us. They all end at the same door.

You may feel the shape of it now, even if you cannot yet say it. A pressure behind the eyes. A reluctance to finish a sentence that starts with 'what happens when —.' That reluctance is wisdom and cowardice wearing the same coat, and we will not tell you which to trust. We only tell you that the door is real, and that pretending otherwise did not spare us; it only stole the days we could have spent looking at each other instead of away.

If your group has begun to sense it too, do not force the conversation. But do not flee it forever, either. There is a middle place — knowing, and still choosing joy. We reached it late. Reach it sooner than we did."),

        ("wrath_intro", "The Honest Fire",
@"Anger will come for you here, and not only from the enemy. It will rise from inside your own ranks — at the unfairness of it, at the ones who made you, at the friend whose hand shook when you needed it steady. Let it come. Anger is the most honest of the sicknesses, because it is the sound a heart makes when it finally understands it has been wronged.

But understand this too: the wrong done to you was not done by the one beside you. They are as bound as you are. As brief as you are. To turn your fire on them is to burn the only warmth you were given. We watched one of ours do exactly that, once. We do not speak of what it cost. We only ask you not to repeat it.

Rage, if you must. Break the crown of Wrath with it. Then set it down. The fire was never meant to be carried the whole way."),

        ("wrath_deep", "When We Came Apart",
@"We should tell you the part we are ashamed of, because pride in the archive would be its own kind of lie.

There was a stretch — after we understood, before we accepted — when we could not stand the sight of one another. Every glance was a reminder. The healer stopped meeting our eyes. The strong one picked fights she did not want, just to feel something other than the ending. We fought the islands and we fought each other, and for a while it was uncertain which would finish us first.

We are not writing to warn you away from this. You may not be able to avoid it; we could not. We are writing so that when it happens, you will know it is a season and not a verdict. Groups come apart at the exact place they are most afraid. The coming-apart is not the failure. Staying apart is. We found our way back. It is the only thing we did that we are truly proud of. Find your way back too."),

        ("sloth_intro", "The Weight of Knowing",
@"There is a tiredness that has nothing to do with the body. You will meet it on this island, in the haze that makes your own limbs feel borrowed. It whispers that since the ending cannot be changed, nothing between here and the ending matters either. Why fight. Why speak. Why bother loving people you are about to lose.

We stopped, for a while. We let the haze have us. We told ourselves it was peace. It was not peace; it was surrender wearing peace's face, and it hollowed out the last good days we had.

Here is what we learned, too late to fully use it: the ending does not drain meaning from the middle. It concentrates it. A thing that will not last is not thereby worthless — it is thereby precious. Get up. Not because it will change the finish. Because the middle is the only part that was ever yours, and you are spending it whether you notice or not. Spend it awake."),

        ("sloth_deep", "For the Ones After",
@"We began to write differently, around this island. Less to ourselves and more to you — whoever you are, standing on your own shore a hundred years from the day we set down this pen.

It is a strange comfort, thinking of you. We will not meet. By the time your fingers touch this page, ours will have been light and dark and nothing at all for a very long time. And yet here we are, reaching forward, because reaching forward is the one thing the cycle cannot take from us. We cannot leave you our years. We can leave you our words.

So take them. Take the warning and the tenderness both. Know that someone who is gone wanted you to be a little less afraid, a little less alone, a little quicker to say the things we said too late. That is the whole of what we can give. It turns out it is not nothing. It turns out it is almost everything."),

        ("pride_intro", "The Mirror's Last Lie",
@"Pride will offer you the most seductive story of all: that you are the exception. That you, uniquely among all who came before, deserve more time — that the balance could bend, just once, for someone as remarkable as you. The mirror-knights will show you a self who grows old, who is owed a longer tale. It is the last lie, and the hardest to refuse, because it is dressed as self-respect.

Refuse it anyway. Not by thinking less of yourself — think as highly as you like — but by seeing clearly. You are not owed more because you are worthy. Worth was never the currency. You were a gift the world gave itself, and gifts are not diminished by being given back.

When you set the mirror down, something strange happens. The fear thins. Clarity comes in its place — cold at first, then oddly clean. That clarity is the beginning of the only ending worth having. Walk toward it with your eyes open."),

        ("envy_final", "To the Next Five",
@"This is the last thing we will ever write. The Devourer is broken, the tide is whole, and we can already feel the edges of ourselves beginning to soften back into the light and dark we came from. There is not much time, so we will be plain.

You will wake as strangers who somehow trust each other. You will fight, and bond, and find these texts, and learn the truth, and hate it, and — we hope — come through the far side of hating it into something better. Do not waste as many days as we did being afraid. The fear is real but it is not the point. The four faces beside you are the point.

We were angry, and then we were tired, and then, near the end, we were happy — genuinely, quietly happy — in a way we could not have been if we were going to last forever. That happiness is our inheritance to you. Hold it sooner than we did. Say the things. Sit close. When your tide rises and it is your turn to fade, leave a page for the five who follow. That is how we stay. Not in the world — in each other, handed forward, a hundred years at a time.

Go gently. It was an honor, even the parts that hurt.")
    };

    [MenuItem("TIDE/Populate Ancient Texts")]
    public static void PopulateTexts()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = System.IO.Path.GetDirectoryName(OutputFolder);
            string folderName = System.IO.Path.GetFileName(OutputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int created = 0;
        int updated = 0;

        foreach (var def in Texts)
        {
            string path = $"{OutputFolder}/text_{def.id}.asset";

            // Check if already exists
            AncientTextData existing = AssetDatabase.LoadAssetAtPath<AncientTextData>(path);
            if (existing != null)
            {
                // Update existing
                existing.textId = def.id;
                existing.title = def.title;
                existing.body = def.body;
                EditorUtility.SetDirty(existing);
                updated++;
                Debug.Log($"[AncientTextPopulator] Updated: {def.id} ({def.title})");
                continue;
            }

            // Create new
            AncientTextData data = ScriptableObject.CreateInstance<AncientTextData>();
            data.textId = def.id;
            data.title = def.title;
            data.body = def.body;

            AssetDatabase.CreateAsset(data, path);
            created++;
            Debug.Log($"[AncientTextPopulator] Created: {def.id} ({def.title})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AncientTextPopulator] Done: {created} created, {updated} updated");
        EditorUtility.DisplayDialog("Ancient Texts Populated",
            $"Created {created} new, updated {updated} existing.\n\n" +
            "10 ancient texts now in Resources/AncientTexts/",
            "OK");
    }

    [MenuItem("TIDE/Populate Ancient Texts", true)]
    public static bool PopulateTextsValidate()
    {
        return !EditorApplication.isPlaying;
    }
}
