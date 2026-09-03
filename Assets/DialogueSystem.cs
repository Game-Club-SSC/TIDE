using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private DialogueTreeRunner activeTreeRunner;
    private GameObject activeTreeRunnerObj;
    private bool isDialogueActive;
    private IsometricPlayer movementLockedPlayer;
    private bool movementLockSnapshot;
    private bool hasMovementLockSnapshot;

    // ------------------------------------------------------------------ //
    //  Dialogue narrative state (issue #297)
    //  Durable ledger for authored dialogue effects: story flags, Tide Break
    //  unlocks, and one-shot rewards. Captured by GameStateManager into the
    //  top-level "dialogueState" save section and restored on load.
    // ------------------------------------------------------------------ //

    private readonly Dictionary<string, bool> dialogueFlags = new Dictionary<string, bool>(StringComparer.Ordinal);
    private readonly HashSet<string> unlockedTideBreakKeys = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> grantedRewardKeys = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<DialoguePendingRewardEntry> pendingRewards = new List<DialoguePendingRewardEntry>();

    /// <summary>True if the dialogue ledger has the flag recorded (regardless of live service state).</summary>
    public bool IsDialogueFlagSet(string flagId)
    {
        return !string.IsNullOrEmpty(flagId) && dialogueFlags.TryGetValue(flagId, out bool value) && value;
    }

    /// <summary>True if the one-shot reward keyed by treeId|nodeId|effectIndex was already delivered.</summary>
    public bool HasGrantedReward(string rewardKey)
    {
        return !string.IsNullOrEmpty(rewardKey) && grantedRewardKeys.Contains(rewardKey);
    }

    public static string MakeRewardKey(string treeId, string nodeId, int effectIndex)
    {
        return $"{treeId ?? string.Empty}|{nodeId ?? string.Empty}|{effectIndex}";
    }

    public static string MakeTideBreakUnlockKey(string heroId, string abilityName)
    {
        return $"{heroId ?? string.Empty}|{abilityName ?? string.Empty}";
    }

    /// <summary>
    /// Applies an authored dialogue effect to durable gameplay state. Successful
    /// one-shot effects are recorded in the granted ledger so a replayed tree
    /// (or a save/load cycle) never delivers them twice. Effects that cannot be
    /// applied because a target service is missing are kept in the pending
    /// ledger with full details — never silently discarded — and are retried
    /// when the save data is applied again.
    /// </summary>
    public bool ApplyDialogueEffect(DialogueTreeEffect effect, string heroId, string treeId, string nodeId, int effectIndex)
    {
        if (effect == null)
        {
            return false;
        }

        string rewardKey = MakeRewardKey(treeId, nodeId, effectIndex);

        // Flags are idempotent and persisted through their own list; every
        // other effect type is one-shot per tree node.
        bool isOneShot = effect.type != DialogueEffectType.SetFlag;
        if (isOneShot && !string.IsNullOrEmpty(rewardKey) && grantedRewardKeys.Contains(rewardKey))
        {
            return true; // already delivered on an earlier walk of this tree
        }

        bool applied = TryApplyEffectToServices(effect, heroId);

        if (effect.type == DialogueEffectType.SetFlag && !string.IsNullOrEmpty(effect.targetId))
        {
            // Persist the flag even when StoryProgressionService is absent; it
            // is pushed into the service on the next save load.
            dialogueFlags[effect.targetId] = effect.intValue != 0;
        }

        if (applied)
        {
            if (isOneShot && !string.IsNullOrEmpty(rewardKey))
            {
                grantedRewardKeys.Add(rewardKey);
            }

            if (effect.type == DialogueEffectType.UnlockTideBreak
                && !string.IsNullOrEmpty(effect.targetId)
                && !string.IsNullOrEmpty(heroId))
            {
                unlockedTideBreakKeys.Add(MakeTideBreakUnlockKey(heroId, effect.targetId));
            }
        }
        else
        {
            pendingRewards.Add(new DialoguePendingRewardEntry
            {
                treeId = treeId,
                nodeId = nodeId,
                effectIndex = effectIndex,
                effectType = (int)effect.type,
                targetId = effect.targetId,
                intValue = effect.intValue,
                heroId = heroId
            });
            Debug.LogWarning($"[DialogueSystem] Dialogue effect '{effect.type}' (targetId='{effect.targetId}', intValue={effect.intValue}) could not be applied now; queued as a pending reward (tree '{treeId}', node '{nodeId}', effect {effectIndex}). It will be retried on the next save load.");
        }

        return applied;
    }

    /// <summary>
    /// Attempts to apply an authored dialogue effect directly to the live
    /// gameplay services. Logs an error with the full effect details when a
    /// required service is missing so the effect is never silently dropped.
    /// Never throws.
    /// </summary>
    public static bool TryApplyEffectToServices(DialogueTreeEffect effect, string heroId)
    {
        if (effect == null)
        {
            return false;
        }

        switch (effect.type)
        {
            case DialogueEffectType.GrantXP:
                return TryApplyGrantXp(effect);

            case DialogueEffectType.UnlockTideBreak:
                return TryApplyUnlockTideBreak(effect, heroId);

            case DialogueEffectType.SetFlag:
                return TryApplySetFlag(effect);

            case DialogueEffectType.GiveItem:
                return TryApplyGiveItem(effect);

            // BUGFIX: Added IncreaseBond case so that IncreaseBond effects
            // routed through ApplyDurableEffect can be retried directly via
            // TryApplyEffectToServices when no DialogueSystem is present.
            case DialogueEffectType.IncreaseBond:
                return TryApplyIncreaseBond(effect);

            default:
                return false;
        }
    }

    private static bool TryApplyGrantXp(DialogueTreeEffect effect)
    {
        if (string.IsNullOrEmpty(effect.targetId) || effect.intValue <= 0)
        {
            return false;
        }

        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (progression == null)
        {
            LogEffectFailure(effect, null, "HeroProgressionManager is not available.");
            return false;
        }

        // BUGFIX: Return the result of GrantXp instead of unconditionally true.
        // Previously, if HeroProgressionManager existed but had no levelingConfig
        // (or the hero was at max level), GrantXp returned false but TryApplyGrantXp
        // returned true — marking the reward as delivered and losing it permanently
        // since it would never be retried from pendingRewards on load.
        return progression.GrantXp(effect.targetId, effect.intValue);
    }

    private static bool TryApplyUnlockTideBreak(DialogueTreeEffect effect, string heroId)
    {
        if (string.IsNullOrEmpty(effect.targetId))
        {
            return false;
        }

        if (string.IsNullOrEmpty(heroId))
        {
            LogEffectFailure(effect, null, "the dialogue node has no relatedHeroId to attach the Tide Break unlock to.");
            return false;
        }

        TideBreakProgressionManager tideBreaks = TideBreakProgressionManager.Instance;
        if (tideBreaks == null)
        {
            LogEffectFailure(effect, heroId, "TideBreakProgressionManager is not available.");
            return false;
        }

        if (tideBreaks.HasTideBreak(heroId, effect.targetId))
        {
            return true; // idempotent: already unlocked
        }

        bool unlocked = tideBreaks.UnlockTideBreak(heroId, effect.targetId);
        if (!unlocked)
        {
            LogEffectFailure(effect, heroId, $"no TideBreak named '{effect.targetId}' exists in the TideBreakData catalog.");
        }

        return unlocked;
    }

    private static bool TryApplySetFlag(DialogueTreeEffect effect)
    {
        if (string.IsNullOrEmpty(effect.targetId))
        {
            return false;
        }

        StoryProgressionService story = StoryProgressionService.Instance;
        if (story == null)
        {
            LogEffectFailure(effect, null, "StoryProgressionService is not available; the flag is recorded in the dialogue ledger and will be applied on the next save load.");
            return false;
        }

        story.SetFlag(effect.targetId, effect.intValue != 0);
        return true;
    }

    private static bool TryApplyGiveItem(DialogueTreeEffect effect)
    {
        if (effect.intValue <= 0)
        {
            return false;
        }

        HeroProgressionManager progression = HeroProgressionManager.Instance;

        // Gear rewards: the effect's item id resolves against registered gear sets.
        if (!string.IsNullOrEmpty(effect.targetId) && IsKnownGearSetId(effect.targetId))
        {
            PlayerGearInventory inventory = PlayerGearInventory.Instance;
            if (inventory == null)
            {
                LogEffectFailure(effect, null, $"the item id '{effect.targetId}' is a registered gear set but PlayerGearInventory is not available.");
                return false;
            }

            GearInstance awarded = inventory.AddGear(effect.targetId, GearDropService.GearRarity.Common);
            if (awarded != null)
            {
                return true;
            }

            LogEffectFailure(effect, null, $"gear award for '{effect.targetId}' failed despite a registered gear set.");
            return false;
        }

        // Non-gear items fall back to the legacy currency reward.
        if (progression == null)
        {
            LogEffectFailure(effect, null, "the item id is not a registered gear set and HeroProgressionManager is not available for the currency fallback.");
            return false;
        }

        progression.AddCurrency(effect.intValue);
        return true;
    }

    private static bool TryApplyIncreaseBond(DialogueTreeEffect effect)
    {
        if (string.IsNullOrEmpty(effect.targetId) || effect.intValue <= 0)
        {
            return false;
        }

        DialogueSystem sys = DialogueSystem.Instance;
        if (sys == null)
        {
            LogEffectFailure(effect, null, "DialogueSystem is not available.");
            return false;
        }

        sys.IncreaseBond(effect.targetId, "player", effect.intValue);
        return true;
    }

    private static bool IsKnownGearSetId(string gearSetId)
    {
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        GearSetData[] sets = progression != null ? progression.AvailableGearSets : null;
        if (sets == null)
        {
            return false;
        }

        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] != null && sets[i].setId == gearSetId)
            {
                return true;
            }
        }

        return false;
    }

    private static void LogEffectFailure(DialogueTreeEffect effect, string heroId, string reason)
    {
        Debug.LogError($"[DialogueSystem] Dialogue effect not applied: type={effect.type}, targetId='{effect.targetId}', intValue={effect.intValue}, heroId='{heroId ?? "null"}'. Reason: {reason}");
    }

    // ------------------------------------------------------------------ //
    //  Dialogue state save / restore (top-level "dialogueState" section)
    // ------------------------------------------------------------------ //

    [Serializable]
    public sealed class DialogueStateSaveData
    {
        public List<string> setFlagIds = new List<string>();
        public List<bool> setFlagValues = new List<bool>();
        public List<string> unlockedTideBreakHeroIds = new List<string>();
        public List<string> unlockedTideBreakNames = new List<string>();
        public List<string> grantedRewardKeys = new List<string>();
        public List<DialoguePendingRewardEntry> pendingRewards = new List<DialoguePendingRewardEntry>();
        public List<string> bondKeys = new List<string>();
        public List<int> bondLevels = new List<int>();
    }

    [Serializable]
    public sealed class DialoguePendingRewardEntry
    {
        public string treeId;
        public string nodeId;
        public int effectIndex;
        public int effectType;
        public string targetId;
        public int intValue;
        public string heroId;
    }

    public DialogueStateSaveData CaptureDialogueStateSaveData()
    {
        DialogueStateSaveData saveData = new DialogueStateSaveData();

        foreach (KeyValuePair<string, bool> pair in dialogueFlags)
        {
            saveData.setFlagIds.Add(pair.Key);
            saveData.setFlagValues.Add(pair.Value);
        }

        foreach (string key in unlockedTideBreakKeys)
        {
            string[] parts = key.Split('|');
            saveData.unlockedTideBreakHeroIds.Add(parts.Length > 0 ? parts[0] : string.Empty);
            saveData.unlockedTideBreakNames.Add(parts.Length > 1 ? parts[1] : string.Empty);
        }

        foreach (string key in grantedRewardKeys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                saveData.grantedRewardKeys.Add(key);
            }
        }

        if (pendingRewards.Count > 0)
        {
            saveData.pendingRewards = new List<DialoguePendingRewardEntry>(pendingRewards);
        }

        List<string> sortedBondKeys = new List<string>(bondLevels.Keys);
        sortedBondKeys.Sort(StringComparer.Ordinal);
        for (int i = 0; i < sortedBondKeys.Count; i++)
        {
            string key = sortedBondKeys[i];
            saveData.bondKeys.Add(key);
            saveData.bondLevels.Add(Mathf.Clamp(bondLevels[key], 0, MaxBondLevel));
        }

        return saveData;
    }

    public void ApplyDialogueStateSaveData(DialogueStateSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        bondLevels.Clear();
        if (saveData.bondKeys != null && saveData.bondLevels != null)
        {
            int bondCount = Mathf.Min(saveData.bondKeys.Count, saveData.bondLevels.Count);
            for (int i = 0; i < bondCount; i++)
            {
                if (TryNormalizeBondKey(saveData.bondKeys[i], out string key))
                {
                    bondLevels[key] = Mathf.Clamp(saveData.bondLevels[i], 0, MaxBondLevel);
                }
            }
        }

        grantedRewardKeys.Clear();
        if (saveData.grantedRewardKeys != null)
        {
            for (int i = 0; i < saveData.grantedRewardKeys.Count; i++)
            {
                if (!string.IsNullOrEmpty(saveData.grantedRewardKeys[i]))
                {
                    grantedRewardKeys.Add(saveData.grantedRewardKeys[i]);
                }
            }
        }

        unlockedTideBreakKeys.Clear();
        if (saveData.unlockedTideBreakHeroIds != null && saveData.unlockedTideBreakNames != null)
        {
            int count = Mathf.Min(saveData.unlockedTideBreakHeroIds.Count, saveData.unlockedTideBreakNames.Count);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(saveData.unlockedTideBreakNames[i]))
                {
                    unlockedTideBreakKeys.Add(MakeTideBreakUnlockKey(saveData.unlockedTideBreakHeroIds[i], saveData.unlockedTideBreakNames[i]));
                }
            }
        }

        // Push restored unlocks into the Tide Break progression manager.
        if (TideBreakProgressionManager.Instance != null)
        {
            foreach (string key in unlockedTideBreakKeys)
            {
                string[] parts = key.Split('|');
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                {
                    TideBreakProgressionManager.Instance.UnlockTideBreak(parts[0], parts[1]);
                }
            }
        }

        dialogueFlags.Clear();
        if (saveData.setFlagIds != null && saveData.setFlagValues != null)
        {
            int count = Mathf.Min(saveData.setFlagIds.Count, saveData.setFlagValues.Count);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(saveData.setFlagIds[i]))
                {
                    dialogueFlags[saveData.setFlagIds[i]] = saveData.setFlagValues[i];
                }
            }
        }

        // Push restored flags into StoryProgressionService (what conditions read).
        if (StoryProgressionService.Instance != null)
        {
            foreach (KeyValuePair<string, bool> pair in dialogueFlags)
            {
                StoryProgressionService.Instance.SetFlag(pair.Key, pair.Value);
            }
        }

        // Retry pending rewards now that services are (hopefully) present.
        pendingRewards.Clear();
        if (saveData.pendingRewards != null)
        {
            for (int i = 0; i < saveData.pendingRewards.Count; i++)
            {
                DialoguePendingRewardEntry entry = saveData.pendingRewards[i];
                if (entry == null)
                {
                    continue;
                }

                string rewardKey = MakeRewardKey(entry.treeId, entry.nodeId, entry.effectIndex);
                bool isOneShot = entry.effectType != (int)DialogueEffectType.SetFlag;
                if (isOneShot && grantedRewardKeys.Contains(rewardKey))
                {
                    continue;
                }

                DialogueTreeEffect effect = new DialogueTreeEffect
                {
                    type = (DialogueEffectType)entry.effectType,
                    targetId = entry.targetId,
                    intValue = entry.intValue
                };

                if (TryApplyEffectToServices(effect, entry.heroId))
                {
                    if (entry.effectType != (int)DialogueEffectType.SetFlag)
                    {
                        grantedRewardKeys.Add(MakeRewardKey(entry.treeId, entry.nodeId, entry.effectIndex));
                    }

                    if (entry.effectType == (int)DialogueEffectType.UnlockTideBreak
                        && !string.IsNullOrEmpty(entry.targetId)
                        && !string.IsNullOrEmpty(entry.heroId))
                    {
                        unlockedTideBreakKeys.Add(MakeTideBreakUnlockKey(entry.heroId, entry.targetId));
                    }
                }
                else
                {
                    // Service still missing; keep the entry so the next load retries it.
                    pendingRewards.Add(entry);
                }
            }
        }

        Debug.Log($"[DialogueSystem] Restored dialogue state: {dialogueFlags.Count} flags, {bondLevels.Count} bonds, {unlockedTideBreakKeys.Count} Tide Break unlocks, {grantedRewardKeys.Count} granted rewards, {pendingRewards.Count} pending.");
    }

    public void ResetDialogueStateForDebug()
    {
        dialogueFlags.Clear();
        unlockedTideBreakKeys.Clear();
        grantedRewardKeys.Clear();
        pendingRewards.Clear();
        bondLevels.Clear();
    }

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
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        RestorePlayerMovement();
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
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
        pendingEntries = entries;

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
        activeTreeRunner = runner;
        activeTreeRunnerObj = runnerObj;
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
            if (TryNormalizeBondKey(pair.Key, out string key))
            {
                bondLevels[key] = Mathf.Clamp(pair.Value, 0, MaxBondLevel);
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Internal
    // ------------------------------------------------------------------ //

    private List<DialogueEntry> pendingEntries;

    private void OnSequenceComplete()
    {
        isDialogueActive = false;
        LockPlayerMovement(false);
        OnDialogueCompleted?.Invoke(pendingEntries);
        pendingEntries = null;
    }

    private void HandleTreeCompleted(string treeId)
    {
        if (activeTreeRunner != null)
        {
            activeTreeRunner.OnTreeCompleted -= HandleTreeCompleted;
            activeTreeRunner = null;
        }

        if (activeTreeRunnerObj != null)
        {
            Destroy(activeTreeRunnerObj);
            activeTreeRunnerObj = null;
        }

        isDialogueActive = false;
        LockPlayerMovement(false);
        OnDialogueTreeCompleted?.Invoke(treeId);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (activeTreeRunner != null)
        {
            activeTreeRunner.OnTreeCompleted -= HandleTreeCompleted;
        }

        activeTreeRunner = null;
        activeTreeRunnerObj = null;
        activeUI = null;
        pendingEntries = null;
        isDialogueActive = false;
        RestorePlayerMovement();
        Debug.Log($"[DialogueSystem] Cancelled scene-owned dialogue after loading '{scene.name}'.");
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

    private static bool TryNormalizeBondKey(string savedKey, out string normalizedKey)
    {
        normalizedKey = null;
        if (string.IsNullOrWhiteSpace(savedKey))
        {
            return false;
        }

        string[] parts = savedKey.Split('|');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        string heroA = parts[0].Trim();
        string heroB = parts[1].Trim();
        if (string.Equals(heroA, heroB, StringComparison.Ordinal))
        {
            return false;
        }

        normalizedKey = MakeBondKey(heroA, heroB);
        return true;
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
