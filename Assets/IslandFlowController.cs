using UnityEngine;

public class IslandFlowController : MonoBehaviour
{
    [SerializeField] private IslandConfig islandConfig;
    [SerializeField] private IslandRestorationTracker tracker;

    private int currentEncounterIndex;
    private bool isActive;
    private bool awaitingEncounterResolution;
    private string activeIslandId;
    private bool hasLoggedDeadlockWarning;

    public string IslandId => activeIslandId;

    public bool IsActive => isActive;
    public int CurrentEncounterIndex => currentEncounterIndex;
    public int CurrentSubsection => currentEncounterIndex / 2;

    private void Awake()
    {
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
        hasLoggedDeadlockWarning = false;

        int restoredIndex = GetNextIncompleteEncounterIndex();
        currentEncounterIndex = Mathf.Clamp(restoredIndex, 0, islandConfig.encounters.Length);

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

        EncounterDefinition encounter = islandConfig.encounters[currentEncounterIndex];
        string encounterId = GetEncounterId(encounter, currentEncounterIndex);
        bool recorded = tracker.RecordEncounterCompletion(activeIslandId, encounterId, encounter.type, encounter.restorationValue);

        int subsection = currentEncounterIndex / 2;
        if (recorded)
        {
            Debug.Log($"[IslandFlowController] Subsection {subsection + 1} {encounter.type} complete. Restoration: {tracker.GetRestorationPercent(activeIslandId):F1}%");
        }

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
            Debug.Log("[IslandFlowController] Combat encounter failed. Flow remains on current encounter.");
            LoadCurrentEncounter();
            return;
        }

        OnEncounterComplete();
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
        if (tracker != null && tracker.HasClearedEncounter(activeIslandId, encounterId))
        {
            currentEncounterIndex++;
            LoadCurrentEncounter();
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
        awaitingEncounterResolution = true;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetBossDefeatTrackingContext(activeIslandId, IsBossEncounter(encounter));
        }

        if (encounter.encounterConfig != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PendingEnemyComposition = EnemyComposition.FromEncounterConfig(encounter.encounterConfig);
        }
        else if (encounter.enemyComposition != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PendingEnemyComposition = encounter.enemyComposition;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterCombatScene();
        }
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
        Debug.Log($"[IslandFlowController] Island {islandConfig.viceName} fully restored! ({tracker.GetRestorationPercent(activeIslandId):F1}%)");
    }

    private static string GetEncounterId(EncounterDefinition encounter, int index)
    {
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
        int lockedBossIndex = -1;

        for (int i = 0; i < islandConfig.encounters.Length; i++)
        {
            EncounterDefinition encounter = islandConfig.encounters[i];
            string encounterId = GetEncounterId(encounter, i);
            if (!tracker.HasClearedEncounter(activeIslandId, encounterId))
            {
                if (IsBossEncounter(encounter))
                {
                    if (IsBossUnlocked())
                    {
                        return i;
                    }

                    if (lockedBossIndex < 0)
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
            if (IsBossEncounter(encounter))
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
