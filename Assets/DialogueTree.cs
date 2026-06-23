using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A branching dialogue tree. The tree is walked node-by-node by <see cref="DialogueTreeRunner"/>.
/// Each node contains a dialogue line and optional choices that lead to other nodes.
/// </summary>
[Serializable]
public class DialogueTree
{
    public string treeId;
    public string title;
    public DialogueTreeNode rootNode;

    [Tooltip("Flat list of every node in this tree. Used by DialogueTreeRunner to resolve node IDs at runtime.")]
    public List<DialogueTreeNode> allNodes = new List<DialogueTreeNode>();
}

/// <summary>
/// A single node in a dialogue tree. Holds one dialogue line plus branching logic.
/// </summary>
[Serializable]
public class DialogueTreeNode
{
    public string nodeId;
    public DialogueSystem.DialogueEntry entry;

    [Tooltip("If empty, auto-advance to nextNode. If has choices, show choice UI.")]
    public DialogueTreeChoice[] choices;

    [Tooltip("Next node when no choices (auto-advance). Ignored if choices.Length > 0.")]
    public string nextNodeId;

    [Tooltip("Conditions that must be met for this node to be available.")]
    public DialogueTreeCondition[] conditions;

    [Tooltip("Effects triggered when this node is reached.")]
    public DialogueTreeEffect[] effects;
}

/// <summary>
/// A player-selectable choice presented at a dialogue node.
/// </summary>
[Serializable]
public class DialogueTreeChoice
{
    public string choiceText;
    public string nextNodeId;

    [Tooltip("If true, this choice increases bonding between the speaker and relatedHeroId.")]
    public bool increasesBond;
    public int bondAmount = 5;

    [Tooltip("Required bond level to see this choice (0 = always visible).")]
    public int requiredBondLevel;

    [Tooltip("Required story act to see this choice (0 = always visible).")]
    public int requiredStoryAct;
}

/// <summary>
/// A condition that gates availability of a node or choice.
/// </summary>
[Serializable]
public class DialogueTreeCondition
{
    public DialogueConditionType type;
    public string targetId;
    public int intValue;
}

/// <summary>
/// An effect that fires when a node is reached.
/// </summary>
[Serializable]
public class DialogueTreeEffect
{
    public DialogueEffectType type;
    public string targetId;
    public int intValue;
}

public enum DialogueConditionType
{
    BondLevel,
    StoryAct,
    IslandRestored,
    HasAncientText,
    QuestCompleted
}

public enum DialogueEffectType
{
    IncreaseBond,
    GrantXP,
    UnlockTideBreak,
    SetFlag,
    GiveItem
}
