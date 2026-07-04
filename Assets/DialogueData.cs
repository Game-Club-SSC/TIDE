using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Character speaking (Killian, Merrick, Freida, Briar, Aether, Narrator)")]
    public string speaker;

    [TextArea(3, 8)]
    [Tooltip("What the character says")]
    public string text;

    [Tooltip("Optional emotion tag for portrait/animation (angry, sad, hopeful, neutral, etc.)")]
    public string emotion;
}

[System.Serializable]
public class StoryBeat
{
    [Tooltip("Short label for this story beat (e.g. 'First ancient text found')")]
    public string beatName;

    [TextArea(2, 4)]
    [Tooltip("Description of what happens in this beat")]
    public string description;

    [Tooltip("Dialogue lines for this beat")]
    public DialogueLine[] lines;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "TIDE/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Chapter Info")]
    [Tooltip("Unique chapter identifier (e.g. 'chapter_0_greed')")]
    public string chapterId;

    [Tooltip("Display name for this chapter")]
    public string chapterName;

    [Tooltip("Island this chapter takes place on")]
    public string islandId;

    [Tooltip("Narrative act (Act I, Act II, Act III)")]
    public string act;

    [TextArea(2, 4)]
    [Tooltip("Tone/mood description for audio and visual systems")]
    public string tone;

    [Header("Story Beats")]
    [Tooltip("Sequential story beats in this chapter")]
    public StoryBeat[] storyBeats;

    [Header("Ancient Text")]
    [Tooltip("ID of the AncientTextData discovered in this chapter")]
    public string ancientTextId;

    [Header("Relationship")]
    [TextArea(2, 4)]
    [Tooltip("Description of how character relationships shift in this chapter")]
    public string relationshipImpact;
}
