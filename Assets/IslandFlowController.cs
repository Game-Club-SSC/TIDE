using UnityEngine;

public class IslandFlowController : MonoBehaviour
{
    [SerializeField] private IslandConfig islandConfig;
    [SerializeField] private IslandRestorationTracker tracker;

    private int currentEncounterIndex;
    private bool isActive;
    private bool awaitingEncounterResolution;

    public string IslandId => islandConfig != null ? islandConfig.islandId : string.Empty;

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
        if (islandConfig.encounters == null || islandConfig.encounters.Length == 0)
        {
            Debug.LogError($"[IslandFlowController] Island '{islandConfig.viceName}' has no encounters configured.");
            isActive = false;
            awaitingEncounterResolution = false;
            return;
        }

        currentEncounterIndex = 0;
        isActive = true;
        awaitingEncounterResolution = false;
        tracker.ResetIsland(islandConfig.islandId);

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
        tracker.RecordEncounterCompletion(islandConfig.islandId, encounterId, encounter.type, encounter.restorationValue);

        int subsection = currentEncounterIndex / 2;
        Debug.Log($"[IslandFlowController] Subsection {subsection + 1} {encounter.type} complete. Restoration: {tracker.GetRestorationPercent(islandConfig.islandId):F1}%");

        currentEncounterIndex++;

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
        Debug.Log($"[IslandFlowController] Island {islandConfig.viceName} fully restored! ({tracker.GetRestorationPercent(islandConfig.islandId):F1}%)");
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
}
