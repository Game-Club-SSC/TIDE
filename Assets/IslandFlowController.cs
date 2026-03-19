using UnityEngine;

public class IslandFlowController : MonoBehaviour
{
    [SerializeField] private IslandConfig islandConfig;
    [SerializeField] private IslandRestorationTracker tracker;

    private int currentEncounterIndex;
    private bool isActive;

    public bool IsActive => isActive;
    public int CurrentEncounterIndex => currentEncounterIndex;

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
        tracker.ResetTracker();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlowController = this;
        }

        Debug.Log($"[IslandFlowController] Starting island: {islandConfig.viceName}");
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
        tracker.CompleteEncounter(encounter.restorationValue);
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
        Debug.Log($"[IslandFlowController] Loading encounter {currentEncounterIndex + 1}/{islandConfig.encounters.Length}: {encounter.type}");

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
        if (encounter.puzzleLayout != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PendingPuzzleLayout = encounter.puzzleLayout.GetGrid();
            GameStateManager.Instance.PendingPuzzleSealedTile = encounter.puzzleLayout.GetSealedPosition();
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
        Debug.Log($"[IslandFlowController] Island {islandConfig.viceName} fully restored!");
    }
}
