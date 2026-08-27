using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Walks through a <see cref="DialogueTree"/> node by node, showing dialogue lines
/// and presenting choices to the player. Spawns itself as a new GameObject
/// via <see cref="DialogueSystem.StartDialogueTree"/>.
/// </summary>
[DisallowMultipleComponent]
public class DialogueTreeRunner : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Constants
    // ------------------------------------------------------------------ //

    private const float TypewriterBaseSpeed = 0.025f;
    private const float PanelHeightRatio = 0.28f;
    private const float ChoiceButtonHeight = 44f;
    private const float ChoiceButtonSpacing = 8f;
    private const float ChoicePanelMaxHeight = 320f;

    // ------------------------------------------------------------------ //
    //  Runtime state
    // ------------------------------------------------------------------ //

    private DialogueTree tree;
    private DialogueTreeNode currentNode;
    private Dictionary<string, DialogueTreeNode> nodeLookup;

    private Canvas canvas;
    private CanvasGroup panelGroup;
    private Image portraitImage;
    private Text speakerText;
    private Text bodyText;
    private Text continuePrompt;
    private RectTransform choiceContainer;

    private bool isTyping;
    private bool skipRequested;
    private bool waitingForAdvance;
    private Coroutine typewriterRoutine;
    private IsometricPlayer movementLockedPlayer;
    private bool movementLockSnapshot;
    private bool hasMovementLockSnapshot;

    /// <summary>Human-readable issues for effects that could not be applied, surfaced in the dialogue panel.</summary>
    private readonly List<string> effectDeliveryIssues = new List<string>();

    /// <summary>Fired when the tree finishes. Passes the treeId.</summary>
    public event Action<string> OnTreeCompleted;

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Begin walking the given dialogue tree. Creates its own UI canvas.
    /// </summary>
    public void StartTree(DialogueTree dialogueTree)
    {
        if (dialogueTree == null || dialogueTree.rootNode == null)
        {
            Debug.LogWarning("[DialogueTreeRunner] Null tree or root node provided.");
            Destroy(gameObject);
            return;
        }

        tree = dialogueTree;
        BuildNodeLookup();
        effectDeliveryIssues.Clear();

        LockPlayerMovement(true);
        EnsureCanvas();
        ShowPanel();
        ShowNode(tree.rootNode);
    }

    // ------------------------------------------------------------------ //
    //  Node lookup
    // ------------------------------------------------------------------ //

    private void BuildNodeLookup()
    {
        nodeLookup = new Dictionary<string, DialogueTreeNode>(StringComparer.Ordinal);

        if (tree.allNodes != null)
        {
            for (int i = 0; i < tree.allNodes.Count; i++)
            {
                DialogueTreeNode node = tree.allNodes[i];
                if (node != null && !string.IsNullOrEmpty(node.nodeId))
                {
                    if (!nodeLookup.ContainsKey(node.nodeId))
                    {
                        nodeLookup[node.nodeId] = node;
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueTreeRunner] Duplicate nodeId '{node.nodeId}' in tree '{tree.treeId}'.");
                    }
                }
            }
        }

        // Fallback: traverse from root to catch any nodes not in allNodes
        if (tree.rootNode != null && !string.IsNullOrEmpty(tree.rootNode.nodeId))
        {
            CollectNodesRecursive(tree.rootNode, 0);
        }
    }

    private const int MaxNodeLookupDepth = 256;

    private void CollectNodesRecursive(DialogueTreeNode node, int depth)
    {
        if (node == null || string.IsNullOrEmpty(node.nodeId)) return;
        if (nodeLookup.ContainsKey(node.nodeId)) return;
        if (depth >= MaxNodeLookupDepth)
        {
            Debug.LogWarning($"[DialogueTreeRunner] Node lookup depth limit ({MaxNodeLookupDepth}) reached at node '{node.nodeId}'. Possible cycle in dialogue tree '{tree.treeId}'.");
            return;
        }

        nodeLookup[node.nodeId] = node;

        if (node.choices != null)
        {
            for (int i = 0; i < node.choices.Length; i++)
            {
                string nextId = node.choices[i].nextNodeId;
                if (!string.IsNullOrEmpty(nextId) && !nodeLookup.TryGetValue(nextId, out DialogueTreeNode next))
                {
                    next = tree.allNodes?.Find(n => n != null && n.nodeId == nextId);
                    if (next != null)
                    {
                        nodeLookup[next.nodeId] = next;
                    }
                }
                if (!string.IsNullOrEmpty(nextId) && nodeLookup.TryGetValue(nextId, out next))
                {
                    CollectNodesRecursive(next, depth + 1);
                }
            }
        }

        if (!string.IsNullOrEmpty(node.nextNodeId) && !nodeLookup.TryGetValue(node.nextNodeId, out DialogueTreeNode autoNext))
        {
            autoNext = tree.allNodes?.Find(n => n != null && n.nodeId == node.nextNodeId);
            if (autoNext != null)
            {
                nodeLookup[autoNext.nodeId] = autoNext;
            }
        }
        if (!string.IsNullOrEmpty(node.nextNodeId) && nodeLookup.TryGetValue(node.nextNodeId, out autoNext))
        {
            CollectNodesRecursive(autoNext, depth + 1);
        }
    }

    // ------------------------------------------------------------------ //
    //  Update (advance / skip)
    // ------------------------------------------------------------------ //

    private void Update()
    {
        if (panelGroup == null || panelGroup.alpha < 0.5f) return;
        if (currentNode == null) return;

        bool pressedAdvance = Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space);

        if (!pressedAdvance) return;

        if (isTyping)
        {
            skipRequested = true;
            return;
        }

        if (waitingForAdvance)
        {
            waitingForAdvance = false;
            AdvanceToNextNode();
        }
    }

    // ------------------------------------------------------------------ //
    //  Node display
    // ------------------------------------------------------------------ //

    private void ShowNode(DialogueTreeNode node)
    {
        if (node == null)
        {
            CompleteTree();
            return;
        }

        currentNode = node;

        DialogueSystem.DialogueEntry entry = node.entry;
        if (string.IsNullOrWhiteSpace(entry.speakerName) && string.IsNullOrWhiteSpace(entry.dialogueText))
        {
            Debug.LogWarning("[DialogueTreeRunner] Skipping a node with a null or empty dialogue entry.");
            AdvanceToNextNode();
            return;
        }

        // Evaluate conditions -- skip node if not met
        if (!EvaluateConditions(node))
        {
            AdvanceAfterConditionFailure(node);
            return;
        }

        // Apply effects for reaching this node
        ApplyEffects(node);

        // Display the dialogue entry
        ShowEntry(node.entry);
    }

    private void ShowEntry(DialogueSystem.DialogueEntry entry)
    {
        if (speakerText != null)
        {
            speakerText.text = entry.speakerName ?? "???";
        }

        Color emotionColor = DialogueSystem.GetEmotionColor(entry.emotion);
        if (speakerText != null) speakerText.color = emotionColor;
        if (portraitImage != null) portraitImage.color = DialogueSystem.GetEmotionPortraitColor(entry.emotion);
        if (continuePrompt != null) continuePrompt.gameObject.SetActive(false);

        HideChoices();

        waitingForAdvance = false;

        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        // Surface any authored effects that could not be applied: they are
        // queued durably, so the player is told instead of the reward silently
        // vanishing.
        string displayText = entry.dialogueText;
        if (effectDeliveryIssues.Count > 0)
        {
            displayText += "\n\n<color=#ff8a8a>" + string.Join(" ", effectDeliveryIssues) + "</color>";
            effectDeliveryIssues.Clear();
        }

        skipRequested = false;
        typewriterRoutine = StartCoroutine(TypewriterRoutine(displayText));
    }

    // ------------------------------------------------------------------ //
    //  Typewriter effect
    // ------------------------------------------------------------------ //

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;

        if (bodyText != null) bodyText.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            OnTypewriterComplete();
            yield break;
        }

        for (int i = 0; i <= fullText.Length; i++)
        {
            if (skipRequested)
            {
                if (bodyText != null) bodyText.text = fullText;
                break;
            }

            if (bodyText != null) bodyText.text = fullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(TypewriterBaseSpeed);
        }

        isTyping = false;
        OnTypewriterComplete();
    }

    private void OnTypewriterComplete()
    {
        typewriterRoutine = null;

        if (currentNode.choices != null && currentNode.choices.Length > 0)
        {
            ShowChoices(currentNode.choices);
        }
        else
        {
            waitingForAdvance = true;
            if (continuePrompt != null) continuePrompt.gameObject.SetActive(true);
        }
    }

    // ------------------------------------------------------------------ //
    //  Choices
    // ------------------------------------------------------------------ //

    private void ShowChoices(DialogueTreeChoice[] choices)
    {
        if (choiceContainer == null) return;

        // Clear previous buttons
        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceContainer.GetChild(i).gameObject);
        }

        List<DialogueTreeChoice> available = FilterAvailableChoices(choices);

        if (available.Count == 0)
        {
            // No valid choices -- fall back to auto-advance
            waitingForAdvance = true;
            if (continuePrompt != null) continuePrompt.gameObject.SetActive(true);
            return;
        }

        choiceContainer.gameObject.SetActive(true);
        if (continuePrompt != null) continuePrompt.gameObject.SetActive(false);

        float totalHeight = available.Count * ChoiceButtonHeight + (available.Count - 1) * ChoiceButtonSpacing;
        totalHeight = Mathf.Min(totalHeight, ChoicePanelMaxHeight);
        choiceContainer.sizeDelta = new Vector2(choiceContainer.sizeDelta.x, totalHeight);

        for (int i = 0; i < available.Count; i++)
        {
            CreateChoiceButton(available[i], i, available.Count);
        }
    }

    private List<DialogueTreeChoice> FilterAvailableChoices(DialogueTreeChoice[] choices)
    {
        List<DialogueTreeChoice> available = new List<DialogueTreeChoice>();
        DialogueSystem sys = DialogueSystem.Instance;

        for (int i = 0; i < choices.Length; i++)
        {
            DialogueTreeChoice choice = choices[i];

            // Bond requirement
            if (choice.requiredBondLevel > 0)
            {
                string heroId = currentNode.entry.relatedHeroId;
                if (sys == null || string.IsNullOrEmpty(heroId))
                {
                    continue;
                }

                int bond = sys.GetBondLevel(heroId, "player");
                if (bond < choice.requiredBondLevel)
                {
                    continue;
                }
            }

            // Story act requirement
            if (choice.requiredStoryAct > 0)
            {
                if (StoryProgressionService.Instance == null
                    || (int)StoryProgressionService.Instance.HighestActReached < choice.requiredStoryAct)
                {
                    continue;
                }
            }

            available.Add(choice);
        }

        return available;
    }

    private void CreateChoiceButton(DialogueTreeChoice choice, int index, int total)
    {
        GameObject buttonObj = new GameObject($"Choice_{index}");
        buttonObj.transform.SetParent(choiceContainer, false);

        Image bg = buttonObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.30f, 0.55f, 1f);
        colors.pressedColor = new Color(0.18f, 0.22f, 0.42f, 1f);
        colors.selectedColor = new Color(0.25f, 0.30f, 0.55f, 1f);
        btn.colors = colors;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * (ChoiceButtonHeight + ChoiceButtonSpacing));
        rect.sizeDelta = new Vector2(0f, ChoiceButtonHeight);

        // Choice label
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(buttonObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = choice.choiceText;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = new Vector2(-16f, 0f);

        // Hook up click
        DialogueTreeChoice captured = choice;
        btn.onClick.AddListener(() => OnChoiceSelected(captured));
    }

    private void OnChoiceSelected(DialogueTreeChoice choice)
    {
        // Apply bond increase from choice
        if (choice.increasesBond && DialogueSystem.Instance != null)
        {
            string heroId = currentNode.entry.relatedHeroId;
            if (!string.IsNullOrEmpty(heroId))
            {
                DialogueSystem.Instance.IncreaseBond(heroId, "player", choice.bondAmount);
            }
        }

        HideChoices();

        if (string.IsNullOrEmpty(choice.nextNodeId))
        {
            CompleteTree();
            return;
        }

        if (nodeLookup.TryGetValue(choice.nextNodeId, out DialogueTreeNode nextNode))
        {
            ShowNode(nextNode);
        }
        else
        {
            Debug.LogWarning($"[DialogueTreeRunner] Choice target node '{choice.nextNodeId}' not found in tree '{tree.treeId}'.");
            CompleteTree();
        }
    }

    // ------------------------------------------------------------------ //
    //  Navigation
    // ------------------------------------------------------------------ //

    private void AdvanceToNextNode()
    {
        if (currentNode == null)
        {
            CompleteTree();
            return;
        }

        if (!string.IsNullOrEmpty(currentNode.nextNodeId))
        {
            if (nodeLookup.TryGetValue(currentNode.nextNodeId, out DialogueTreeNode nextNode))
            {
                ShowNode(nextNode);
                return;
            }

            Debug.LogWarning($"[DialogueTreeRunner] nextNodeId '{currentNode.nextNodeId}' not found in tree '{tree.treeId}'.");
        }

        CompleteTree();
    }

    private void AdvanceAfterConditionFailure(DialogueTreeNode failedNode)
    {
        if (failedNode == null)
        {
            CompleteTree();
            return;
        }

        string targetNodeId = string.IsNullOrEmpty(failedNode.conditionFailureNodeId)
            ? failedNode.nextNodeId
            : failedNode.conditionFailureNodeId;

        if (string.IsNullOrEmpty(targetNodeId))
        {
            CompleteTree();
            return;
        }

        if (nodeLookup.TryGetValue(targetNodeId, out DialogueTreeNode nextNode))
        {
            ShowNode(nextNode);
            return;
        }

        Debug.LogWarning($"[DialogueTreeRunner] condition fallback '{targetNodeId}' not found in tree '{tree.treeId}'.");
        CompleteTree();
    }

    // ------------------------------------------------------------------ //
    //  Conditions
    // ------------------------------------------------------------------ //

    private bool EvaluateConditions(DialogueTreeNode node)
    {
        if (node.conditions == null || node.conditions.Length == 0) return true;

        DialogueSystem sys = DialogueSystem.Instance;

        for (int i = 0; i < node.conditions.Length; i++)
        {
            DialogueTreeCondition cond = node.conditions[i];

            switch (cond.type)
            {
                case DialogueConditionType.BondLevel:
                    if (cond.intValue <= 0)
                    {
                        break;
                    }

                    if (sys == null || !TryResolveBondPair(cond.targetId, out string heroA, out string heroB))
                    {
                        return false;
                    }

                    if (sys.GetBondLevel(heroA, heroB) < cond.intValue) return false;
                    break;

                case DialogueConditionType.StoryAct:
                    if (StoryProgressionService.Instance == null)
                    {
                        Debug.LogWarning("[DialogueTreeRunner] StoryProgressionService not available for StoryAct condition.");
                        return false;
                    }
                    if ((int)StoryProgressionService.Instance.CurrentAct < cond.intValue)
                    {
                        return false;
                    }
                    break;

                case DialogueConditionType.IslandRestored:
                    if (IslandRestorationTracker.Instance == null)
                    {
                        Debug.LogWarning("[DialogueTreeRunner] IslandRestorationTracker not available for IslandRestored condition.");
                        return false;
                    }
                    if (!IslandRestorationTracker.Instance.IsIslandRestored(cond.targetId))
                    {
                        return false;
                    }
                    break;

                case DialogueConditionType.HasAncientText:
                    if (!ExpandedAncientTexts.TryGetText(cond.targetId, out _))
                    {
                        return false;
                    }
                    if (AncientTextRevealDirector.Instance == null || !AncientTextRevealDirector.Instance.IsFragmentDiscovered(cond.targetId))
                    {
                        return false;
                    }
                    break;

                case DialogueConditionType.QuestCompleted:
                    if (StoryProgressionService.Instance == null)
                    {
                        Debug.LogWarning("[DialogueTreeRunner] StoryProgressionService not available for QuestCompleted condition.");
                        return false;
                    }
                    if (!StoryProgressionService.Instance.IsQuestCompleted(cond.targetId))
                    {
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    private static bool TryResolveBondPair(string targetId, out string heroA, out string heroB)
    {
        heroA = null;
        heroB = null;
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        string[] pair = targetId.Split('|');
        if (pair.Length == 1)
        {
            heroA = pair[0].Trim();
            heroB = "player";
        }
        else if (pair.Length == 2)
        {
            heroA = pair[0].Trim();
            heroB = pair[1].Trim();
        }

        return !string.IsNullOrEmpty(heroA)
            && !string.IsNullOrEmpty(heroB)
            && !string.Equals(heroA, heroB, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------ //
    //  Effects
    // ------------------------------------------------------------------ //

    private void ApplyEffects(DialogueTreeNode node)
    {
        if (node.effects == null || node.effects.Length == 0) return;

        DialogueSystem sys = DialogueSystem.Instance;

        for (int i = 0; i < node.effects.Length; i++)
        {
            DialogueTreeEffect effect = node.effects[i];

            switch (effect.type)
            {
                case DialogueEffectType.IncreaseBond:
                    if (sys != null && !string.IsNullOrEmpty(effect.targetId))
                    {
                        sys.IncreaseBond(effect.targetId, "player", effect.intValue);
                    }
                    break;

                case DialogueEffectType.GrantXP:
                case DialogueEffectType.UnlockTideBreak:
                case DialogueEffectType.SetFlag:
                case DialogueEffectType.GiveItem:
                    ApplyDurableEffect(effect, i);
                    break;
            }
        }
    }

    /// <summary>
    /// Routes a durable effect (XP, Tide Break unlock, story flag, item/gear
    /// reward) through <see cref="DialogueSystem.ApplyDialogueEffect"/> so it is
    /// written to gameplay state AND recorded in the persistent dialogue ledger
    /// (never silently discarded, never double-delivered). Falls back to a direct
    /// service application when no DialogueSystem is present.
    /// </summary>
    private void ApplyDurableEffect(DialogueTreeEffect effect, int effectIndex)
    {
        string heroId = currentNode != null
            ? currentNode.entry.relatedHeroId
            : null;
        string treeId = tree != null ? tree.treeId : null;
        string nodeId = currentNode != null ? currentNode.nodeId : null;

        bool applied;
        DialogueSystem sys = DialogueSystem.Instance;
        if (sys != null)
        {
            applied = sys.ApplyDialogueEffect(effect, heroId, treeId, nodeId, effectIndex);
        }
        else
        {
            // No DialogueSystem: apply directly and warn on failure. Nothing is
            // recorded, so a replayed tree can re-deliver — defense-in-depth only.
            applied = DialogueSystem.TryApplyEffectToServices(effect, heroId);
            if (!applied)
            {
                Debug.LogWarning($"[DialogueTreeRunner] Could not apply {effect.type} effect (targetId='{effect.targetId}', intValue={effect.intValue}) and no DialogueSystem is present to queue it.");
            }
        }

        if (!applied)
        {
            RecordEffectDeliveryIssue(effect);
        }
    }

    private void RecordEffectDeliveryIssue(DialogueTreeEffect effect)
    {
        string label;
        switch (effect.type)
        {
            case DialogueEffectType.GrantXP: label = "XP"; break;
            case DialogueEffectType.UnlockTideBreak: label = "Tide Break"; break;
            case DialogueEffectType.SetFlag: label = "story flag"; break;
            case DialogueEffectType.GiveItem: label = "item"; break;
            default: label = "effect"; break;
        }

        string detail = $"{label} '{effect.targetId}' could not be delivered now; it is queued and will be applied on the next save load.";
        effectDeliveryIssues.Add(detail);
        Debug.LogWarning($"[DialogueTreeRunner] {detail}");
    }

    // ------------------------------------------------------------------ //
    //  Completion
    // ------------------------------------------------------------------ //

    private void CompleteTree()
    {
        HideChoices();
        HidePanel();
        LockPlayerMovement(false);

        string completedTreeId = tree != null ? tree.treeId : null;
        OnTreeCompleted?.Invoke(completedTreeId);

        Destroy(gameObject, 0.15f);
    }

    private void LockPlayerMovement(bool locked)
    {
        if (locked)
        {
            if (hasMovementLockSnapshot)
            {
                return;
            }

            movementLockedPlayer = FindFirstObjectByType<IsometricPlayer>();
            if (movementLockedPlayer == null)
            {
                return;
            }

            movementLockSnapshot = movementLockedPlayer.canMove;
            hasMovementLockSnapshot = true;
            movementLockedPlayer.canMove = false;
            return;
        }

        RestorePlayerMovement();
    }

    private void RestorePlayerMovement()
    {
        if (!hasMovementLockSnapshot)
        {
            return;
        }

        if (movementLockedPlayer != null)
        {
            movementLockedPlayer.canMove = movementLockSnapshot;
        }

        movementLockedPlayer = null;
        hasMovementLockSnapshot = false;
    }

    private void OnDestroy()
    {
        RestorePlayerMovement();
    }

    // ------------------------------------------------------------------ //
    //  UI helpers
    // ------------------------------------------------------------------ //

    private void HideChoices()
    {
        if (choiceContainer == null) return;

        choiceContainer.gameObject.SetActive(false);

        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceContainer.GetChild(i).gameObject);
        }
    }

    // ------------------------------------------------------------------ //
    //  Canvas construction (procedural, mirrors DialogueUI pattern)
    // ------------------------------------------------------------------ //

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        // --- Canvas ---
        GameObject canvasObj = new GameObject("DialogueTreeCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 910; // above DialogueUI's 900

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Panel background ---
        GameObject panelObj = CreateUIElement("DialoguePanel", canvasObj.transform);
        panelGroup = panelObj.AddComponent<CanvasGroup>();
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, PanelHeightRatio);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.06f, 0.10f, 0.94f);

        // --- Portrait circle ---
        GameObject portraitObj = CreateUIElement("Portrait", panelObj.transform);
        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.type = Image.Type.Filled;
        portraitImage.fillMethod = Image.FillMethod.Radial360;
        portraitImage.fillClockwise = true;

        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(70f, 0f);
        portraitRect.sizeDelta = new Vector2(80f, 80f);

        // --- Speaker name ---
        GameObject speakerObj = CreateUIElement("SpeakerName", panelObj.transform);
        speakerText = speakerObj.AddComponent<Text>();
        speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        speakerText.fontSize = 26;
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.color = Color.white;

        RectTransform speakerRect = speakerObj.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.anchoredPosition = new Vector2(120f, -10f);
        speakerRect.sizeDelta = new Vector2(-140f, 36f);

        // --- Body text ---
        GameObject bodyObj = CreateUIElement("BodyText", panelObj.transform);
        bodyText = bodyObj.AddComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyText.fontSize = 22;
        bodyText.color = Color.white;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = 1.15f;

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(120f, 12f);
        bodyRect.offsetMax = new Vector2(-30f, -42f);

        // --- Continue prompt ---
        GameObject promptObj = CreateUIElement("ContinuePrompt", panelObj.transform);
        continuePrompt = promptObj.AddComponent<Text>();
        continuePrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        continuePrompt.fontSize = 16;
        continuePrompt.fontStyle = FontStyle.Italic;
        continuePrompt.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        continuePrompt.text = "[Press Enter to continue]";
        continuePrompt.alignment = TextAnchor.LowerRight;

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.offsetMin = new Vector2(120f, 4f);
        promptRect.offsetMax = new Vector2(-20f, -4f);

        // --- Choice container (positioned above the dialogue panel) ---
        GameObject choiceObj = CreateUIElement("ChoiceContainer", canvasObj.transform);
        choiceContainer = choiceObj.GetComponent<RectTransform>();
        choiceContainer.anchorMin = new Vector2(0.15f, PanelHeightRatio);
        choiceContainer.anchorMax = new Vector2(0.85f, PanelHeightRatio);
        choiceContainer.pivot = new Vector2(0.5f, 0f);
        choiceContainer.anchoredPosition = Vector2.zero;
        choiceContainer.sizeDelta = new Vector2(0f, 0f);

        choiceContainer.gameObject.SetActive(false);

        // Start hidden
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = false;
    }

    private void ShowPanel()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = true;
        }
    }

    private void HidePanel()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
        }
    }

    private static GameObject CreateUIElement(string elementName, Transform parent)
    {
        GameObject obj = new GameObject(elementName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return obj;
    }
}
