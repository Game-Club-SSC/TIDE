using System.Collections.Generic;
using UnityEngine;

public static class AncientTextAuthoring
{
    public const int MinimumRequiredCount = 18;

    private static readonly string[] BaselineSins =
    {
        "gluttony", "greed", "sloth", "wrath", "envy", "pride"
    };

    private static readonly string[] Acts = { "act1", "act2", "act3" };

    private static List<AncientTextData> baselineCache;
    private static List<AncientTextData> mergedCache;

    public static int BaselineCount => BaselineAuthoredTexts.Count;

    public static IReadOnlyList<AncientTextData> GetBaselineAuthoredTexts()
    {
        if (baselineCache == null)
        {
            baselineCache = BuildBaseline();
        }

        return baselineCache;
    }

    public static IReadOnlyList<AncientTextData> GetAllAuthoredTexts()
    {
        if (mergedCache == null)
        {
            mergedCache = BuildMerged();
        }

        return mergedCache;
    }

    public static AncientTextData GetById(string textId)
    {
        if (string.IsNullOrEmpty(textId))
        {
            return null;
        }

        IReadOnlyList<AncientTextData> all = GetAllAuthoredTexts();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && string.Equals(all[i].textId, textId, System.StringComparison.Ordinal))
            {
                return all[i];
            }
        }

        return null;
    }

    public static bool CoversAllSins(IReadOnlyList<AncientTextData> list)
    {
        if (list == null || list.Count == 0)
        {
            return false;
        }

        HashSet<string> found = new HashSet<string>();
        for (int i = 0; i < list.Count; i++)
        {
            AncientTextData data = list[i];
            if (data == null || string.IsNullOrEmpty(data.textId))
            {
                continue;
            }

            foreach (string sin in BaselineSins)
            {
                if (data.textId.IndexOf(sin, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found.Add(sin);
                }
            }
        }

        return found.Count >= BaselineSins.Length;
    }

    public static int CountEntriesForSin(string sin)
    {
        if (string.IsNullOrEmpty(sin))
        {
            return 0;
        }

        IReadOnlyList<AncientTextData> all = GetAllAuthoredTexts();
        int count = 0;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].textId.IndexOf(sin, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                count++;
            }
        }

        return count;
    }

    private static List<AncientTextData> BuildBaseline()
    {
        List<AncientTextData> list = new List<AncientTextData>();

        // Gluttony
        list.Add(MakeText("text_gluttony_act1_01", "The Hunger at Dawn", "We woke to a table that had not been set. We ate what the tide had not yet claimed. The hunger never left; it only learned new names."));
        list.Add(MakeText("text_gluttony_act2_01", "Salt and Memory", "The island kept offering more. Each plate we cleaned carved the next memory out of us. We remembered the feast; we forgot why we came."));
        list.Add(MakeText("text_gluttony_act3_01", "Empty Plate", "There is a place set at the long table where no one will sit again. The tide is full; the island is light. We move on lighter too."));

        // Greed
        list.Add(MakeText("text_greed_act1_01", "Counted Sands", "We hoarded the silver sands of the shore, convinced that enough was one more handful. The tide is patient; it took them back at dusk."));
        list.Add(MakeText("text_greed_act2_01", "The Coin Heart", "A merchant taught us to weigh our courage against our coin. We learned quickly. The coin was always heavier."));
        list.Add(MakeText("text_greed_act3_01", "Empty Pockets", "We left the island without the silver. The pockets remember the weight. So do we, when we reach for what is no longer there."));

        // Sloth
        list.Add(MakeText("text_sloth_act1_01", "Slow Tide", "The tide came in slow. We did not. It filled the bay while we still debated the route. The island does not wait for committees."));
        list.Add(MakeText("text_sloth_act2_01", "Drowsy Watch", "The watch fell asleep at the third bell. By the fifth, the rift was twice as wide. We do not blame the watch; the bell was heavy."));
        list.Add(MakeText("text_sloth_act3_01", "Footfalls", "Our steps are faster now. The island is almost through with us. We will not be drowsy at the end."));

        // Wrath
        list.Add(MakeText("text_wrath_act1_01", "Kindled Edge", "The first blow felt like sunrise. By the third it felt like weather. We mistook the heat for the cause, and the cause for the cure."));
        list.Add(MakeText("text_wrath_act2_01", "Furnace Hour", "The forge does not care whose temper it cools. We learned to swing before we learned to listen. The island heard us anyway."));
        list.Add(MakeText("text_wrath_act3_01", "Ashen Calm", "The fires on the island are out. The party is quieter. The cost of being heard was everything we had to shout with."));

        // Envy
        list.Add(MakeText("text_envy_act1_01", "Borrowed Faces", "The other party had a hero that laughed like ours used to. We watched for too long. The island noticed."));
        list.Add(MakeText("text_envy_act2_01", "Mirrored Step", "Our steps echoed theirs, but louder, as if volume could become belonging. The echo was our own, returned."));
        list.Add(MakeText("text_envy_act3_01", "Returned Gaze", "We stopped watching. The mirror that the island set up cracked on its own. We are not what they were, and that is enough."));

        // Pride
        list.Add(MakeText("text_pride_act1_01", "The High Step", "We crossed the threshold of the final island before the rest. The island was patient. We were not."));
        list.Add(MakeText("text_pride_act2_01", "Unasked Counsel", "We had advice for the tide, the trees, and the low sky. The island nodded at each. None of it altered."));
        list.Add(MakeText("text_pride_act3_01", "Lowered Head", "Before the rift we stood tall. After the conversation we did not. The island had not asked us to bow; it had only asked us to listen."));

        return list;
    }

    private static List<AncientTextData> BuildMerged()
    {
        HashSet<string> seen = new HashSet<string>();
        List<AncientTextData> merged = new List<AncientTextData>();

        IReadOnlyList<AncientTextData> baseline = GetBaselineAuthoredTexts();
        for (int i = 0; i < baseline.Count; i++)
        {
            AncientTextData data = baseline[i];
            if (data == null || string.IsNullOrEmpty(data.textId) || !data.IsValid())
            {
                continue;
            }

            if (seen.Add(data.textId))
            {
                merged.Add(data);
            }
        }

        AncientTextData[] loaded = Resources.LoadAll<AncientTextData>("AncientTexts");
        for (int i = 0; i < loaded.Length; i++)
        {
            AncientTextData data = loaded[i];
            if (data == null || string.IsNullOrEmpty(data.textId) || !data.IsValid())
            {
                continue;
            }

            if (seen.Add(data.textId))
            {
                merged.Add(data);
            }
        }

        return merged;
    }

    private static AncientTextData MakeText(string textId, string title, string body)
    {
        AncientTextData data = ScriptableObject.CreateInstance<AncientTextData>();
        data.textId = textId;
        data.title = title;
        data.body = body;
        data.name = $"Authored_{textId}";
        return data;
    }

    public static void ClearCache()
    {
        if (baselineCache != null)
        {
            for (int i = 0; i < baselineCache.Count; i++)
            {
                if (baselineCache[i] != null)
                {
                    Object.DestroyImmediate(baselineCache[i]);
                }
            }
        }
        baselineCache = null;
        mergedCache = null;
    }
}
