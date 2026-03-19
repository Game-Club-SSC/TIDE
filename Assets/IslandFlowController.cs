using UnityEngine;

public class IslandFlowController : MonoBehaviour
{
    [SerializeField] private IslandConfig islandConfig;
    [SerializeField] private IslandRestorationTracker tracker;

    private int currentEncounterIndex;
    private bool isActive;

    public bool IsActive => isActive;
    public int CurrentEncounterIndex => currentEncounterIndex;
    public int CurrentSubsection => currentEncounterIndex / 2;

    private void Awake()
    {
        if (tracker == null)
        {
            tracker = GetComponent<IslandRestorationTracker>();
        }

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
        islandConfig = config;
        currentEncounterIndex = 0;
        isActive = true;
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
        if (!isActive || islandConfig == null)
        {
            return;
        }

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
        if (!isActive)
        {
            return;
        }

        OnEncounterComplete();
    }

    public void OnReturnFromCombat()
    {
        if (!isActive)
        {
            return;
        }

        OnEncounterComplete();
    }

    private void LoadCurrentEncounter()
    {
        if (currentEncounterIndex >= islandConfig.encounters.Length)
        {
            OnIslandComplete();
            return;
        }

        EncounterDefinition encounter = islandConfig.encounters[currentEncounterIndex];
        int subsection = currentEncounterIndex / 2;
        int totalSubsections = islandConfig.encounters.Length / 2;

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
        if (encounter.enemyComposition != null && GameStateManager.Instance != null)
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
        if (encounter.puzzleData != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PendingPuzzleData = encounter.puzzleData;
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
