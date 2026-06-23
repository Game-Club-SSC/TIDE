using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Singleton that manages character dialogue sequences and inter-hero bonding levels.
/// Attach to a persistent GameObject (e.g. the GameStateManager).
/// </summary>
[DisallowMultipleComponent]
public class DialogueSystem : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Data types
    // ------------------------------------------------------------------ //

    public enum Emotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Worried,
        Determined
    }

    [Serializable]
    public struct DialogueEntry
    {
        public string speakerName;
        [TextArea(2, 6)]
        public string dialogueText;
        public Emotion emotion;

        [Tooltip("Optional heroId of a second hero involved in this line (for bonding).")]
        public string relatedHeroId;
    }

    // ------------------------------------------------------------------ //
    //  Singleton
    // ------------------------------------------------------------------ //

    public static DialogueSystem Instance { get; private set; }

    // ------------------------------------------------------------------ //
    //  Bonding
    // ------------------------------------------------------------------ //

    private const int MaxBondLevel = 100;

    // Key: sorted "heroA|heroB" pair string. Value: 0-100.
    private readonly Dictionary<string, int> bondLevels = new Dictionary<string, int>(StringComparer.Ordinal);

    // ------------------------------------------------------------------ //
    //  Dialogue state
    // ------------------------------------------------------------------ //

    private DialogueUI activeUI;
    private bool isDialogueActive;

    /// <summary>Fired when a dialogue sequence completes. Passes the list of entries shown.</summary>
    public event Action<List<DialogueEntry>> OnDialogueCompleted;

    /// <summary>Fired when a dialogue tree completes. Passes the treeId.</summary>
    public event Action<string> OnDialogueTreeCompleted;

    // ------------------------------------------------------------------ //
    //  Unity lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ------------------------------------------------------------------ //
    //  Dialogue API
    // ------------------------------------------------------------------ //

    /// <summary>True while a dialogue sequence is being displayed.</summary>
    public bool IsDialogueActive => isDialogueActive;

    /// <summary>
    /// Show a sequence of dialogue lines. Blocks player movement while active.
    /// </summary>
    public void ShowDialogue(List<DialogueEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        if (isDialogueActive)
        {
            return;
        }

        isDialogueActive = true;
        LockPlayerMovement(true);

        DialogueUI ui = EnsureDialogueUI();
        ui.PlaySequence(entries, OnSequenceComplete);
    }

    /// <summary>
    /// Show a single dialogue line (convenience overload).
    /// </summary>
    public void ShowDialogue(DialogueEntry entry)
    {
        List<DialogueEntry> single = new List<DialogueEntry> { entry };
        ShowDialogue(single);
    }

    /// <summary>
    /// Start a branching dialogue tree. Creates a <see cref="DialogueTreeRunner"/>
    /// that walks through nodes, shows choices, and applies effects.
    /// </summary>
    public void StartDialogueTree(DialogueTree tree)
    {
        if (tree == null || tree.rootNode == null)
        {
            return;
        }

        if (isDialogueActive)
        {
            return;
        }

        isDialogueActive = true;
        LockPlayerMovement(true);

        GameObject runnerObj = new GameObject($"DialogueTreeRunner_{tree.treeId}");
        DialogueTreeRunner runner = runnerObj.AddComponent<DialogueTreeRunner>();
        runner.OnTreeCompleted += HandleTreeCompleted;
        runner.StartTree(tree);
    }

    // ------------------------------------------------------------------ //
    //  Bonding API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Increase the bond between two heroes by the given amount.
    /// heroA and heroB are heroId strings (order does not matter).
    /// </summary>
    public void IncreaseBond(string heroA, string heroB, int amount)
    {
        if (string.IsNullOrEmpty(heroA) || string.IsNullOrEmpty(heroB))
        {
            return;
        }

        if (string.Equals(heroA, heroB, StringComparison.Ordinal))
        {
            return; // no self-bonding
        }

        string key = MakeBondKey(heroA, heroB);
        int current = 0;
        bondLevels.TryGetValue(key, out current);

        int newValue = Mathf.Clamp(current + amount, 0, MaxBondLevel);
        bondLevels[key] = newValue;
    }

    /// <summary>
    /// Get the bond level (0-100) between two heroes.
    /// </summary>
    public int GetBondLevel(string heroA, string heroB)
    {
        if (string.IsNullOrEmpty(heroA) || string.IsNullOrEmpty(heroB))
        {
            return 0;
        }

        string key = MakeBondKey(heroA, heroB);
        bondLevels.TryGetValue(key, out int value);
        return Mathf.Clamp(value, 0, MaxBondLevel);
    }

    /// <summary>
    /// Returns a human-readable summary of all hero pair relationships.
    /// </summary>
    public string GetRelationshipSummary()
    {
        if (bondLevels.Count == 0)
        {
            return "No bonds have formed yet.";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Hero Bonds:");

        List<string> sortedKeys = new List<string>(bondLevels.Keys);
        sortedKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            string key = sortedKeys[i];
            int level = bondLevels[key];
            string[] parts = key.Split('|');
            string label = DescribeBondLevel(level);
            sb.AppendLine($"  {parts[0]} <-> {parts[1]}: {level}/100 ({label})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the bond key for a given pair (useful for external persistence).
    /// </summary>
    public static string MakeBondKey(string heroA, string heroB)
    {
        int cmp = string.Compare(heroA, heroB, StringComparison.Ordinal);
        return cmp <= 0
            ? $"{heroA}|{heroB}"
            : $"{heroB}|{heroA}";
    }

    /// <summary>
    /// Get all bond data as a serializable dictionary (for save/load).
    /// </summary>
    public Dictionary<string, int> GetAllBonds()
    {
        return new Dictionary<string, int>(bondLevels, StringComparer.Ordinal);
    }

    /// <summary>
    /// Restore bond data from a saved dictionary.
    /// </summary>
    public void ApplyBondData(Dictionary<string, int> savedBonds)
    {
        bondLevels.Clear();
        if (savedBonds == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> pair in savedBonds)
        {
            bondLevels[pair.Key] = Mathf.Clamp(pair.Value, 0, MaxBondLevel);
        }
    }

    // ------------------------------------------------------------------ //
    //  Internal
    // ------------------------------------------------------------------ //

    private void OnSequenceComplete()
    {
        isDialogueActive = false;
        LockPlayerMovement(false);
        OnDialogueCompleted?.Invoke(null);
    }

    private void HandleTreeCompleted(string treeId)
    {
        isDialogueActive = false;
        LockPlayerMovement(false);
        OnDialogueTreeCompleted?.Invoke(treeId);
    }

    private DialogueUI EnsureDialogueUI()
    {
        if (activeUI != null)
        {
            return activeUI;
        }

        activeUI = FindFirstObjectByType<DialogueUI>();
        if (activeUI != null)
        {
            return activeUI;
        }

        GameObject uiObject = new GameObject("DialogueUI");
        activeUI = uiObject.AddComponent<DialogueUI>();
        return activeUI;
    }

    private void LockPlayerMovement(bool locked)
    {
        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            player.canMove = !locked;
        }
    }

    private static string DescribeBondLevel(int level)
    {
        if (level <= 0) return "Strangers";
        if (level <= 20) return "Uneasy";
        if (level <= 40) return "Acquaintances";
        if (level <= 60) return "Comrades";
        if (level <= 80) return "Close allies";
        return "Inseparable";
    }

    // ------------------------------------------------------------------ //
    //  Emotion -> color mapping
    // ------------------------------------------------------------------ //

    public static Color GetEmotionColor(Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Happy:      return new Color(0.30f, 0.78f, 0.30f, 1f); // green
            case Emotion.Sad:        return new Color(0.30f, 0.50f, 0.85f, 1f); // blue
            case Emotion.Angry:      return new Color(0.85f, 0.25f, 0.25f, 1f); // red
            case Emotion.Worried:    return new Color(0.85f, 0.70f, 0.20f, 1f); // yellow
            case Emotion.Determined: return new Color(0.80f, 0.45f, 0.90f, 1f); // purple
            default:                 return Color.white;
        }
    }

    /// <summary>
    /// Maps an emotion to a representative element color for portrait circles.
    /// </summary>
    public static Color GetEmotionPortraitColor(Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Happy:      return new Color(0.25f, 0.70f, 0.25f, 1f);
            case Emotion.Sad:        return new Color(0.25f, 0.40f, 0.80f, 1f);
            case Emotion.Angry:      return new Color(0.80f, 0.20f, 0.20f, 1f);
            case Emotion.Worried:    return new Color(0.80f, 0.65f, 0.15f, 1f);
            case Emotion.Determined: return new Color(0.70f, 0.35f, 0.85f, 1f);
            default:                 return new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }
}
