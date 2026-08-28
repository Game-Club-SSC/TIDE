using UnityEngine;

public class IslandFlowController : MonoBehaviour
{
    [SerializeField] private IslandConfig islandConfig;
    [SerializeField] private IslandRestorationTracker tracker;

    private int currentEncounterIndex;
    private bool isActive;
    private bool awaitingEncounterResolution;
    private string activeIslandId;
    // A combat encounter spends one power point when it first opens.  Keep
    // the encounter key so a failed or fled battle can be retried without
    // spending the point again.
    private string budgetConsumedEncounterId;
    private bool hasLoggedDeadlockWarning;

    public string IslandId => activeIslandId;

    public bool IsActive => isActive;
    public int CurrentEncounterIndex => currentEncounterIndex;
    public int CurrentSubsection => currentEncounterIndex / 2;

    private void Awake()
    {
        // This controller owns the in-progress encounter while gameplay moves
        // through the shared exploration, puzzle, and combat scenes.
        DontDestroyOnLoad(gameObject);
        ResolveTrackerReference();
    }

    private void ResolveTrackerReference()
    {
        if (tracker != null)
        {
            return;
        }

        if (IslandRestorationTracker.Instance != null)
        {
            tracker = IslandRestorationTracker.Instance;
            return;
        }

        tracker = GetComponent<IslandRestorationTracker>();
        if (tracker != null)
        {
            return;
        }

        tracker = FindFirstObjectByType<IslandRestorationTracker>();
        if (tracker == null)
        {
            tracker = gameObject.AddComponent<IslandRestorationTracker>();
        }
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlowController = this;
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.FlowController == this)
        {
            GameStateManager.Instance.FlowController = null;
        }

        isActive = false;
        activeIslandId = string.Empty;
    }

    public void StartIsland(IslandConfig config)
    {
        if (config == null)
        {
            Debug.LogError("[IslandFlowController] Cannot start island flow with null config.");
            return;
        }

        ResolveTrackerReference();
        if (tracker == null)
        {
            Debug.LogError("[IslandFlowController] Missing IslandRestorationTracker; cannot start island flow.");
            return;
        }

        islandConfig = config;
        activeIslandId = IslandThemeRegistry.ResolveIslandId(islandConfig.islandId);
        if (islandConfig.encounters == null || islandConfig.encounters.Length == 0)
        {
            Debug.LogError($"[IslandFlowController] Island '{islandConfig.viceName}' has no encounters configured.");
            isActive = false;
            awaitingEncounterResolution = false;
            return;
        }

        isActive = true;
        awaitingEncounterResolution = false;
        budgetConsumedEncounterId = null;
        hasLoggedDeadlockWarning = false;

        currentEncounterIndex = GetNextIncompleteEncounterIndex();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlowController = this;
        }

        Debug.Log($"[IslandFlowController] Starting island: {islandConfig.viceName} ({islandConfig.encounters.Length} encounters)");
        LoadCurrentEncounter();
    }

    public void OnEncounterComplete()
    {
        if (!isActive || islandConfig == null || !awaitingEncounterResolution)
        {
            return;
        }

        awaitingEncounterResolution = false;

        if (currentEncounterIndex >= islandConfig.encounters.Length)
        {
            Debug.LogWarning("[IslandFlowController] OnEncounterComplete called but no pending encounter.");
            return;
        }

        if (tracker == null)
        {
            Debug.LogWarning("[IslandFlowController] Tracker is null in OnEncounterComplete.");
            return;
        }

        EncounterDefinition encounter = islandConfig.encounters[currentEncounterIndex];
        string encounterId = GetEncounterId(encounter, currentEncounterIndex);
        bool recorded = tracker.RecordEncounterCompletion(activeIslandId, encounterId, encounter.type, encounter.restorationValue);

        int subsection = currentEncounterIndex / 2;
        if (recorded)
        {
            Debug.Log($"[IslandFlowController] Subsection {subsection + 1} {encounter.type} complete. Restoration: {tracker.GetRestorationPercent(activeIslandId):F1}%");
        }

        // The completed encounter is no longer eligible for a retry.  Clear
        // its budget marker before selecting the next encounter.
        budgetConsumedEncounterId = null;
        currentEncounterIndex = GetNextIncompleteEncounterIndex();
        if (currentEncounterIndex >= islandConfig.encounters.Length)
        {
            OnIslandComplete();
            return;
        }

        LoadCurrentEncounter();
    }

    public void OnReturnFromPuzzle()
    {
        if (!isActive || !awaitingEncounterResolution)
        {
            return;
        }

        OnEncounterComplete();
    }

    public void OnReturnFromCombat(bool playerWon)
    {
        if (!isActive || !awaitingEncounterResolution)
        {
            return;
        }

        if (!playerWon)
        {
            awaitingEncounterResolution = false;
            Debug.Log("[IslandFlowController] Combat encounter failed or fled. Resuming flow on current encounter.");
            LoadCurrentEncounter();
            return;
        }

        OnEncounterComplete();
    }

    public void StopFlowForDebug()
    {
        AbortFlowAfterFatalError();
    }

    public void ResetForNewGame()
    {
        islandConfig = null;
        currentEncounterIndex = 0;
        isActive = false;
        awaitingEncounterResolution = false;
        activeIslandId = string.Empty;
        budgetConsumedEncounterId = null;
        hasLoggedDeadlockWarning = false;
    }

    public void PauseForHub()
    {
        isActive = false;
        awaitingEncounterResolution = false;
        budgetConsumedEncounterId = null;
        hasLoggedDeadlockWarning = false;
    }

    public void AbortFlowAfterFatalError()
    {
        isActive = false;
        awaitingEncounterResolution = false;
        activeIslandId = string.Empty;
        budgetConsumedEncounterId = null;
        hasLoggedDeadlockWarning = false;
    }

    private void LoadCurrentEncounter()
    {
        if (islandConfig == null || islandConfig.encounters == null || islandConfig.encounters.Length == 0)
        {
            Debug.LogWarning("[IslandFlowController] No encounters available to load.");
            isActive = false;
            awaitingEncounterResolution = false;
            return;
        }

        if (currentEncounterIndex >= islandConfig.encounters.Length)
        {
            OnIslandComplete();
            return;
        }

        EncounterDefinition encounter = islandConfig.encounters[currentEncounterIndex];
        if (IsBossEncounter(encounter) && !IsBossUnlocked())
        {
            int fallbackIndex = FindNextIncompleteNonBossEncounterIndex();
            if (fallbackIndex >= 0)
            {
                currentEncounterIndex = fallbackIndex;
                encounter = islandConfig.encounters[currentEncounterIndex];
            }
            else
            {
                Debug.Log($"[IslandFlowController] Boss encounter for '{activeIslandId}' remains locked until 75% restoration.");
                if (!hasLoggedDeadlockWarning)
                {
                    float currentPercent = tracker != null ? tracker.GetRestorationPercent(activeIslandId) : 0f;
                    Debug.LogWarning($"[IslandFlowController] No non-boss encounters remain while boss is locked for '{activeIslandId}' ({currentPercent:F1}% < 75%). Check encounter restoration totals/content balance.");
                    hasLoggedDeadlockWarning = true;
                }
                awaitingEncounterResolution = false;
                return;
            }
        }

        string encounterId = GetEncounterId(encounter, currentEncounterIndex);
        while (tracker != null && tracker.HasClearedEncounter(activeIslandId, encounterId) && currentEncounterIndex < islandConfig.encounters.Length)
        {
            currentEncounterIndex++;
            if (currentEncounterIndex >= islandConfig.encounters.Length)
                break;
            encounter = islandConfig.encounters[currentEncounterIndex];
            encounterId = GetEncounterId(encounter, currentEncounterIndex);
        }

        if (currentEncounterIndex >= islandConfig.encounters.Length)
        {
            OnIslandComplete();
            return;
        }

        if (encounter == null)
        {
            Debug.LogWarning("[IslandFlowController] No remaining encounters after skipping cleared ones.");
            isActive = false;
            return;
        }

        int subsection = currentEncounterIndex / 2;
        int totalSubsections = Mathf.Max(1, (islandConfig.encounters.Length + 1) / 2);

        Debug.Log($"[IslandFlowController] Loading Subsection {subsection + 1}/{totalSubsections} — {encounter.type}");

        if (encounter.type == EncounterType.Combat)
        {
            LoadCombatEncounter(encounter);
        }
        else
        {
            LoadPuzzleEncounter(encounter);
        }
    }

    private void LoadCombatEncounter(EncounterDefinition encounter)
    {
        GameStateManager gameState = GameStateManager.Instance;
        if (gameState == null)
        {
            awaitingEncounterResolution = false;
            Debug.LogError("[IslandFlowController] Cannot load combat encounter without GameStateManager.");
            return;
        }

        if (!gameState.CanEnterCombatScene())
        {
            awaitingEncounterResolution = false;
            Debug.LogWarning($"[IslandFlowController] Combat encounter '{GetEncounterId(encounter, currentEncounterIndex)}' is waiting for the current scene transition to finish.");
            return;
        }

        string encounterId = GetEncounterId(encounter, currentEncounterIndex);
        PowerBudgetTracker budgetTracker = PowerBudgetTracker.Instance;
        bool budgetAlreadyConsumed = string.Equals(
            budgetConsumedEncounterId,
            encounterId,
            System.StringComparison.Ordinal);
        if (!budgetAlreadyConsumed
            && budgetTracker != null
            && !budgetTracker.TryConsumeBudget(activeIslandId, 1f))
        {
            // Do not advance the index.  The encounter is still uncleared and
            // must remain available if the budget is restored later.
            awaitingEncounterResolution = false;
            Debug.LogWarning($"[IslandFlowController] Power budget exhausted for '{activeIslandId}'. Combat encounter '{encounterId}' remains uncleared.");
            return;
        }

        if (!budgetAlreadyConsumed)
        {
            // Mark the reservation before loading the scene.  A failed or
            // fled combat returns through LoadCurrentEncounter and reaches
            // this method again with the same encounter key.
            budgetConsumedEncounterId = encounterId;
        }

        awaitingEncounterResolution = true;
        gameState.PendingCombatIslandId = IslandThemeRegistry.ResolveIslandId(activeIslandId);
        gameState.PendingCombatEncounterId = encounterId;
        gameState.PendingCombatRestorationValue = Mathf.Max(0.001f, encounter.restorationValue);
        gameState.PendingEnemyComposition = null;
        gameState.SetBossDefeatTrackingContext(activeIslandId, IsBossEncounter(encounter));

        if (encounter.encounterConfig != null)
        {
            gameState.PendingEnemyComposition = EnemyComposition.FromEncounterConfig(encounter.encounterConfig);
        }
        else if (encounter.enemyComposition != null)
        {
            gameState.PendingEnemyComposition = encounter.enemyComposition;
        }

        // EnterCombatScene preserves the context above when this controller
        // owns the active flow.  CombatSceneBootstrap uses it for enemy tier,
        // boss presentation, restoration, and reward context.
        gameState.EnterCombatScene();
    }

    private void LoadPuzzleEncounter(EncounterDefinition encounter)
    {
        awaitingEncounterResolution = true;

        if (encounter.puzzleData != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PendingPuzzleData = encounter.puzzleData;
            GameStateManager.Instance.PendingPuzzleIslandId = IslandId;
            GameStateManager.Instance.PendingPuzzleEncounterId = encounter.encounterId;
            GameStateManager.Instance.PendingPuzzleRestorationValue = encounter.restorationValue;
        }

        if (GameStateManager.Instance != null)
        {
            IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
            Vector3 returnPos = player != null ? player.transform.position : Vector3.zero;
            GameStateManager.Instance.EnterPuzzleSceneForced(returnPos);
        }
    }

    private void OnIslandComplete()
    {
        isActive = false;
        float restorationPercent = tracker != null ? tracker.GetRestorationPercent(activeIslandId) : 0f;
        Debug.Log($"[IslandFlowController] Island {islandConfig.viceName} fully restored! ({restorationPercent:F1}%)");
    }

    private static string GetEncounterId(EncounterDefinition encounter, int index)
    {
        if (encounter == null)
        {
            return $"missing_{index}";
        }

        if (!string.IsNullOrEmpty(encounter.encounterId))
        {
            return encounter.encounterId;
        }

        return encounter.type == EncounterType.Combat
            ? $"combat_{index / 2 + 1}"
            : $"puzzle_{index / 2 + 1}";
    }

    private int GetNextIncompleteEncounterIndex()
    {
        if (tracker == null || islandConfig == null || islandConfig.encounters == null)
        {
            return 0;
        }

        int nonBossFallback = -1;
        int unlockedBossIndex = -1;
        int lockedBossIndex = -1;

        for (int i = 0; i < islandConfig.encounters.Length; i++)
        {
            EncounterDefinition encounter = islandConfig.encounters[i];
            if (encounter == null)
            {
                continue;
            }

            string encounterId = GetEncounterId(encounter, i);
            if (!tracker.HasClearedEncounter(activeIslandId, encounterId))
            {
                if (IsBossEncounter(encounter))
                {
                    if (IsBossUnlocked())
                    {
                        if (unlockedBossIndex < 0)
                        {
                            unlockedBossIndex = i;
                        }
                    }
                    else if (lockedBossIndex < 0)
                    {
                        lockedBossIndex = i;
                    }

                    continue;
                }

                if (nonBossFallback < 0)
                {
                    nonBossFallback = i;
                }
            }
        }

        // Preserve the configured encounter order. An unlocked boss must not
        // bypass an earlier incomplete combat or puzzle encounter.
        if (unlockedBossIndex >= 0
            && (nonBossFallback < 0 || unlockedBossIndex < nonBossFallback))
        {
            return unlockedBossIndex;
        }

        if (nonBossFallback >= 0)
        {
            hasLoggedDeadlockWarning = false;
            return nonBossFallback;
        }

        if (lockedBossIndex >= 0)
        {
            return lockedBossIndex;
        }

        return islandConfig.encounters.Length;
    }

    private int FindNextIncompleteNonBossEncounterIndex()
    {
        if (tracker == null || islandConfig == null || islandConfig.encounters == null)
        {
            return -1;
        }

        for (int i = 0; i < islandConfig.encounters.Length; i++)
        {
            EncounterDefinition encounter = islandConfig.encounters[i];
            if (encounter == null || IsBossEncounter(encounter))
            {
                continue;
            }

            string encounterId = GetEncounterId(encounter, i);
            if (!tracker.HasClearedEncounter(activeIslandId, encounterId))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsBossUnlocked()
    {
        if (tracker == null || islandConfig == null)
        {
            return false;
        }

        return tracker.IsRestorationAtOrAbove(activeIslandId, IslandRestorationTracker.DefaultBossUnlockThresholdPercent);
    }

    private static bool IsBossEncounter(EncounterDefinition encounter)
    {
        if (encounter == null)
        {
            return false;
        }

        if (encounter.isBossEncounter)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(encounter.encounterId)
            && encounter.encounterId.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }
}
