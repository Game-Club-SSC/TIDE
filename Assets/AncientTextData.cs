using UnityEngine;

[CreateAssetMenu(fileName = "AncientTextData", menuName = "TIDE/Ancient Text Data")]
public class AncientTextData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key for persistence and lookup (e.g. island1_text_01).")]
    public string textId = "ancient_text_01";

    [Header("Content")]
    public string title = "Ancient Tablet";

    [TextArea(6, 16)]
    public string body = "The tide is not a weapon. It is a burden shared by those born to carry both light and shadow.";

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(textId) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body);
    }
}
