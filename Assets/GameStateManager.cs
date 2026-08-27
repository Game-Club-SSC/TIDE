using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameStateManager : MonoBehaviour
{
    private sealed class PuzzleRuntimeState
    {
        public int[] tileValues = Array.Empty<int>();
        public int gridRows;
        public int gridCols;
        public bool hasGrid;
        public bool solved;
    }

    private sealed class AncientTextRuntimeState
    {
        public string title;
        public string body;
        public bool discovered;
    }

    [Serializable]
    public sealed class PuzzleRuntimeSaveEntry
    {
        public string puzzleBoxId;
        public int[] tileValues = Array.Empty<int>();
        public int gridRows;
        public int gridCols;
        public bool hasGrid;
        public bool solved;
    }

    [Serializable]
    public sealed class AncientTextRuntimeSaveEntry
    {
        public string textId;
        public string title;
        public string body;
        public bool discovered;
    }

    [Serializable]
    public sealed class WorldStateSaveData
    {
        public List<PuzzleRuntimeSaveEntry> puzzleStates = new List<PuzzleRuntimeSaveEntry>();
        public List<AncientTextRuntimeSaveEntry> ancientTextStates = new List<AncientTextRuntimeSaveEntry>();
        public List<string> completedNarrativeBeatIds = new List<string>();
        public IslandRestorationTracker.TrackerSnapshot restorationSnapshot;
        public GearProgressionSaveData gearProgression;
        public PlayerGearInventory.GearInventorySaveData gearInventory;
        public IslandProgressionManager.ProgressionSnapshot progressionSnapshot;
        public StoryProgressionSaveData storyProgression;
        public HeroProgressionManager.HeroProgressionSnapshot heroProgression;
        public HeroProgressionManager.PartyCompositionSnapshot partyComposition;
        public bool ceremonyIntroCompleted;
        public TideBreakProgressionManager.TideBreakSaveData tideBreakProgression;
        public AncientTextRevealDirector.RevealDirectorSaveData ancientTextReveal;
        public HubStateSaveData hubState;
        public DialogueSystem.DialogueStateSaveData dialogueState;
    }

    [Serializable]
    public sealed class HubStateSaveData
    {
        public bool hasReturnPosition;
        public float returnX;
        public float returnY;
        public float returnZ;
    }

    [Serializable]
    public sealed class StoryProgressionSaveData
    {
        public int currentAct;
        public int highestActReached;
        public int finalBossDefeatThreshold;
        public int minimumRestorationRuleMode;
        public string pendingBadEndingThresholdEventIslandId;
        public string endingBranch;
        public bool endingTriggered;
        public List<string> thresholdOnlyBossVictoryIslandIds = new List<string>();
        public List<string> thresholdOnlyProceedIslandIds = new List<string>();
        public List<FinalBossDefeatSaveEntry> finalBossDefeats = new List<FinalBossDefeatSaveEntry>();
    }

    public enum GameState
    {
        Exploration,
        Combat,
        Puzzle,
        Transition
    }

    public enum StoryAct
    {
        ActI = 1,
        ActII = 2,
        ActIII = 3
    }

    public enum EndingBranch
    {
        None,
        Good,
        Bad
    }

    public enum MinimumRestorationBadEndingRuleMode
    {
        OptionalContentOnly,
        BossDefeatedAtThreshold,
        ProceededAtThreshold
    }

    public const string MainSceneName = "level_1";
    public const string PuzzleSceneName = "PuzzleScene";
    public const string CombatSceneName = "CombatScene";
    public const string HubSceneName = "HubScene";
    public const string TitleSceneName = "TitleScene";

    public static GameStateManager Instance { get; private set; }

    public GameState currentState = GameState.Exploration;
    public bool PuzzleSolved { get; private set; }
    public bool IsTransitioning => isTransitioning;
    public bool HasLoadedWorldState => hasLoadedWorldState;
    public bool HasPersistedWorldState => PlayerPrefs.HasKey(WorldStateSaveKey);

    /// <summary>
    /// True when a persisted world state exists AND parses as valid save data.
    /// The title screen's Continue button uses this so a corrupt save never
    /// enables loading (issue #294). JsonUtility.FromJson throws on malformed
    /// input, so corrupt payloads are rejected structurally first, then via a
    /// guarded parse.
    /// </summary>
    public bool HasLoadableWorldState()
    {
        if (!PlayerPrefs.HasKey(WorldStateSaveKey))
        {
            return false;
        }

        string payload = PlayerPrefs.GetString(WorldStateSaveKey, string.Empty);
        if (string.IsNullOrEmpty(payload))
        {
            return false;
        }

        // Structural pre-check mirrors WorldSaveService.ValidateSaveJson so
        // obvious corruption never reaches the JSON parser.
        if (!payload.Contains("{") || !payload.Contains("}") || !payload.Contains("\"puzzleStates\""))
        {
            return false;
        }

        try
        {
            return JsonUtility.FromJson<WorldStateSaveData>(payload) != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public StoryAct CurrentStoryAct => currentStoryAct;
    public StoryAct HighestStoryActReached => highestStoryActReached;
    public EndingBranch ResolvedEndingBranch => resolvedEndingBranch;
    public bool IsEndingTriggered => endingTriggered;
    public MinimumRestorationBadEndingRuleMode MinimumRestorationBadEndingRule => minimumRestorationBadEndingRuleMode;
    public bool CeremonyIntroCompleted
    {
        get => ceremonyIntroCompleted;
        set
        {
            if (ceremonyIntroCompleted == value)
            {
                return;
            }

            ceremonyIntroCompleted = value;
            SaveWorldState();
        }
    }

    private const float FadeDuration = 0.2f;
    private const float ExplorationPositionSyncInterval = 0.2f;
    private const float RestorationThresholdEpsilon = 0.01f;
    private const string WorldStateSaveKey = "TIDE_WORLD_STATE_V1";

    [Header("Default Spawn Positions")]
    [SerializeField] private Vector3 defaultSmithyPosition = new Vector3(15f, 31.54f, 2.69f);
    [SerializeField] private Vector3 defaultSmithyScale = new Vector3(1.4f, 1.1f, 1.4f);
    [SerializeField] private Vector3 defaultBoatPosition = new Vector3(8.5f, 31.54f, 2.69f);
    [SerializeField] private Vector3 defaultBoatScale = new Vector3(2.2f, 0.75f, 1.25f);
    [SerializeField] private Vector3 defaultExplorationSpawnPosition = new Vector3(12f, 31.54f, 2.69f);

    private CanvasGroup fadeCanvasGroup;
    private IsometricPlayer player;
    private Vector3 pendingReturnPosition;
    private bool hasPendingReturnPosition;
    private Vector3 pendingCameraPosition;
    private Quaternion pendingCameraRotation;
    private bool hasPendingCameraTransform;
    private bool isTransitioning;
    private bool hasHandledSceneLoad;
    private float explorationPositionSyncTimer;
    private Vector3 lastKnownExplorationPlayerPosition;
    private bool hasLastKnownExplorationPlayerPosition;
    private Vector3 hubReturnPosition;
    private bool hasHubReturnPosition;

    public PuzzleData PendingPuzzleData { get; set; }
    public int[,] PendingPuzzleLayout { get; set; }
    public Vector2Int PendingPuzzleSealedTile { get; set; }
    public EnemyComposition PendingEnemyComposition { get; set; }
    public string PendingPuzzleIslandId { get; set; }
    public string PendingPuzzleEncounterId { get; set; }
    public float PendingPuzzleRestorationValue { get; set; }
    public string PendingCombatIslandId { get; set; }
    public string PendingCombatEncounterId { get; set; }
    public float PendingCombatRestorationValue { get; set; }
    public IslandFlowController FlowController { get; set; }
    public bool HasActiveFlowController => FlowController != null && FlowController.IsActive;
    public IslandRestorationTracker RestorationTracker => IslandRestorationTracker.Instance;

    public event Action OnPuzzleCompleted;
    public event Action<StoryAct> OnStoryActChanged;
    private bool isFlowControlledCombat;
    private bool deferredFlowFromCombat;
    private bool hasDeferredFlowFromCombatResult;
    private bool deferredFlowFromCombatResult;
    private bool deferredFlowFromPuzzle;
    private string pendingSolvedPuzzleBoxId;
    private bool returnToPuzzleAfterCombat;
    private bool hasPendingCombatReturnPosition;
    private Coroutine cameraSnapRoutine;
    private bool hasLoadedWorldState;
    private readonly Dictionary<string, PuzzleRuntimeState> puzzleRuntimeStates = new Dictionary<string, PuzzleRuntimeState>();
    private readonly Dictionary<string, AncientTextRuntimeState> ancientTextRuntimeStates = new Dictionary<string, AncientTextRuntimeState>();
    private readonly HashSet<string> completedNarrativeBeatIds = new HashSet<string>();
    private readonly HashSet<string> thresholdOnlyBossVictoryIslandIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> thresholdOnlyProceedIslandIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> finalBossDefeatCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private string pendingBossIslandIdForDefeatTracking;
    private bool isSavingWorldState;
    private bool isLoadingWorldState;
    private bool hasBootstrappedFlowForCurrentScene;
    private StoryAct currentStoryAct = StoryAct.ActI;
    private StoryAct highestStoryActReached = StoryAct.ActI;
    private EndingBranch resolvedEndingBranch = EndingBranch.None;
    private bool endingTriggered;
    private bool ceremonyIntroCompleted;
    private string observedActiveIslandId;
    private string pendingBadEndingThresholdEventIslandId;
    private int finalBossDefeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding;
    private MinimumRestorationBadEndingRuleMode minimumRestorationBadEndingRuleMode = MinimumRestorationBadEndingRuleMode.OptionalContentOnly;

    [Header("Feature Gates")]
    [SerializeField] private bool enableCosmeticProgressionEconomyForCurrentSlice;
    [SerializeField] private bool enablePersistentSaveData = true;
    [SerializeField] private bool enableDeveloperGodMode = true;
    [SerializeField] private bool autoStartIslandFlowOnMainScene;

    [Serializable]
    public sealed class FinalBossDefeatSaveEntry
    {
        public string islandId;
        public int defeats;
    }

    [Serializable]
    public sealed class FinalBossDefeatSaveCollection
    {
        public List<FinalBossDefeatSaveEntry> entries = new List<FinalBossDefeatSaveEntry>();
    }

    private const string FinalBossDefeatsSaveKey = "TIDE_FINAL_BOSS_DEFEATS_V1";

    public float GetIslandRestorationPercent(string islandId)
    {
        if (IslandRestorationTracker.Instance == null)
        {
            return 0f;
        }

        return IslandRestorationTracker.Instance.GetRestorationPercent(IslandThemeRegistry.ResolveIslandId(islandId));
    }

    public IslandRestorationState GetIslandRestorationState(string islandId)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (IslandRestorationTracker.Instance == null)
        {
            return new IslandRestorationState(scopedIslandId);
        }

        return IslandRestorationTracker.Instance.GetRestorationState(scopedIslandId);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyDuplicateComponent();
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyRuntimeFeatureGates();
        if (Application.isEditor || Debug.isDebugBuild)
        {
            enableDeveloperGodMode = true;
        }
        EnsureRestorationTracker();
        EnsureProgressionManager();
        EnsureGearInventory();
        EnsureDialogueSystem();
        BindProgressionManagerEvents();
        LoadWorldState();
        IslandProgressionManager.Instance?.ReconcileStateFromRestoration();
        EnsureFadeCanvas();
        EnsureAudioManager();
        EnsureDeveloperTools();
        ReconcileStoryProgressionFromIslandState();
    }

    private void ApplyRuntimeFeatureGates()
    {
        HeroProgressionManager.ConfigureRuntimeCosmeticProgressionEconomy(enableCosmeticProgressionEconomyForCurrentSlice);
    }

    private void Start()
    {
        if (!hasHandledSceneLoad)
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    private void Update()
    {
        if (isTransitioning || currentState != GameState.Exploration)
        {
            return;
        }

        explorationPositionSyncTimer -= Time.unscaledDeltaTime;
        if (explorationPositionSyncTimer > 0f)
        {
            return;
        }

        explorationPositionSyncTimer = ExplorationPositionSyncInterval;
        CachePlayer();
        if (player == null)
        {
            return;
        }

        Vector3 playerPosition = player.transform.position;
        if (!IsFiniteVector(playerPosition))
        {
            return;
        }

        lastKnownExplorationPlayerPosition = playerPosition;
        hasLastKnownExplorationPlayerPosition = true;

        if (IslandProgressionManager.Instance != null)
        {
            IslandProgressionManager.Instance.RecordIslandReturnPosition(
                IslandProgressionManager.Instance.ActiveIslandId,
                playerPosition);
        }
    }

    private void OnDestroy()
    {
        ReleaseSingletonOwnership();
    }

    private void OnApplicationQuit()
    {
        ReleaseSingletonOwnership();
    }

    private void ReleaseSingletonOwnership()
    {
        if (Instance != this)
        {
            return;
        }

        SaveWorldState();
        UnbindProgressionManagerEvents();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void DestroyDuplicateComponent()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
            return;
        }

        DestroyImmediate(this);
    }

    public bool CanEnterPuzzle()
    {
        return !isTransitioning && currentState == GameState.Exploration;
    }

    public bool CanEnterCombatScene()
    {
        return !isTransitioning && currentState == GameState.Exploration;
    }

    public bool CanEnterCombatFromPuzzle()
    {
        return !isTransitioning && currentState == GameState.Puzzle;
    }

    public void EnterCombat()
    {
        currentState = GameState.Combat;
        SetPlayerMovementLocked(true);
    }

    public void EnterCombatScene()
    {
        if (!CanEnterCombatScene())
        {
            return;
        }

        returnToPuzzleAfterCombat = false;
        PendingCombatIslandId = null;
        PendingCombatEncounterId = null;
        PendingCombatRestorationValue = 0f;
        if (!HasActiveFlowController)
        {
            pendingBossIslandIdForDefeatTracking = null;
        }
        CaptureExplorationReturnPosition();
        BeginCombatTransition();
    }

    public void EnterCombatSceneFromExploration(string islandId, string encounterId, float restorationValue, Vector3 returnPosition, bool isBossEncounter = false)
    {
        if (!CanEnterCombatScene())
        {
            return;
        }

        returnToPuzzleAfterCombat = false;
        PendingCombatIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        PendingCombatEncounterId = encounterId;
        PendingCombatRestorationValue = Mathf.Max(0.001f, restorationValue);
        pendingReturnPosition = ResolveSafeReturnPosition(returnPosition);
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = true;
        pendingBossIslandIdForDefeatTracking = (isBossEncounter || IsBossEncounterId(PendingCombatEncounterId))
            ? PendingCombatIslandId
            : null;
        finalBossDefeatsForBadEndingThreshold = ResolveFinalBossDefeatThreshold(pendingBossIslandIdForDefeatTracking);
        CaptureExplorationCameraTransform();
        PlayerGearInventory.Instance?.BeginNewCombat();
        BeginCombatTransition();
    }

    public void EnterCombatSceneFromPuzzle(string islandId, string encounterId, float restorationValue = 0.001f)
    {
        if (!CanEnterCombatFromPuzzle())
        {
            return;
        }

        returnToPuzzleAfterCombat = true;
        PendingCombatIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        PendingCombatEncounterId = encounterId;
        PendingCombatRestorationValue = Mathf.Max(0.001f, restorationValue);
        hasPendingCombatReturnPosition = false;
        PlayerGearInventory.Instance?.BeginNewCombat();
        BeginCombatTransition();
    }

    public void SetBossDefeatTrackingContext(string islandId, bool shouldTrack)
    {
        if (!shouldTrack)
        {
            pendingBossIslandIdForDefeatTracking = null;
            finalBossDefeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding;
            return;
        }

        pendingBossIslandIdForDefeatTracking = IslandThemeRegistry.ResolveIslandId(islandId);
        finalBossDefeatsForBadEndingThreshold = ResolveFinalBossDefeatThreshold(pendingBossIslandIdForDefeatTracking);
    }

    private void BeginCombatTransition()
    {
        isFlowControlledCombat = HasActiveFlowController;
        hasDeferredFlowFromCombatResult = false;
        deferredFlowFromCombatResult = false;

        if (!isFlowControlledCombat && currentState != GameState.Puzzle)
        {
            PuzzleSolved = false;
        }

        StartCoroutine(TransitionToScene(CombatSceneName, GameState.Combat));
    }

    public void EndCombat()
    {
        currentState = GameState.Exploration;
        SetPlayerMovementLocked(false);
    }

    public void EnterPuzzle()
    {
        currentState = GameState.Puzzle;
        SetPlayerMovementLocked(true);
    }

    public void ExitPuzzle()
    {
        currentState = GameState.Exploration;
        SetPlayerMovementLocked(false);
    }

    public void EnterPuzzleScene(Vector3 returnPosition, string puzzleBoxId = null)
    {
        if (!CanEnterPuzzle())
        {
            return;
        }

        pendingReturnPosition = ResolveSafeReturnPosition(returnPosition);
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingBossIslandIdForDefeatTracking = null;
        pendingSolvedPuzzleBoxId = puzzleBoxId;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void EnterPuzzleSceneForced(Vector3 returnPosition)
    {
        if (isTransitioning)
        {
            return;
        }

        pendingReturnPosition = ResolveSafeReturnPosition(returnPosition);
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingBossIslandIdForDefeatTracking = null;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void SavePuzzleRuntimeState(string puzzleBoxId, int[,] grid, bool solved)
    {
        if (string.IsNullOrEmpty(puzzleBoxId) || grid == null || grid.GetLength(0) < 1 || grid.GetLength(1) < 1)
        {
            return;
        }

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        if (!puzzleRuntimeStates.TryGetValue(puzzleBoxId, out PuzzleRuntimeState state))
        {
            state = new PuzzleRuntimeState();
            puzzleRuntimeStates[puzzleBoxId] = state;
        }

        state.gridRows = rows;
        state.gridCols = cols;
        state.tileValues = new int[rows * cols];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                state.tileValues[row * cols + col] = Mathf.Clamp(grid[row, col], 1, 10);
            }
        }

        state.hasGrid = true;
        state.solved = state.solved || solved;
        SaveWorldState();
    }

    public bool TryGetPuzzleRuntimeGrid(string puzzleBoxId, out int[,] grid, out bool solved)
    {
        grid = null;
        solved = false;

        if (string.IsNullOrEmpty(puzzleBoxId))
        {
            return false;
        }

        if (!puzzleRuntimeStates.TryGetValue(puzzleBoxId, out PuzzleRuntimeState state))
        {
            return false;
        }

        solved = state.solved;
        if (!state.hasGrid)
        {
            return false;
        }

        int rows = Mathf.Max(1, state.gridRows);
        int cols = Mathf.Max(1, state.gridCols);
        grid = new int[rows, cols];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                grid[row, col] = index < state.tileValues.Length
                    ? Mathf.Clamp(state.tileValues[index], 1, 10)
                    : 5;
            }
        }

        return true;
    }

    public bool IsPuzzleBoxSolved(string puzzleBoxId)
    {
        if (string.IsNullOrEmpty(puzzleBoxId))
        {
            return false;
        }

        return puzzleRuntimeStates.TryGetValue(puzzleBoxId, out PuzzleRuntimeState state) && state.solved;
    }

    public void MarkPuzzleBoxSolved(string puzzleBoxId)
    {
        if (string.IsNullOrEmpty(puzzleBoxId))
        {
            return;
        }

        if (!puzzleRuntimeStates.TryGetValue(puzzleBoxId, out PuzzleRuntimeState state))
        {
            state = new PuzzleRuntimeState();
            puzzleRuntimeStates[puzzleBoxId] = state;
        }

        if (!state.solved)
        {
            state.solved = true;
            SaveWorldState();
        }
    }

    public void RegisterAncientText(string textId, string title, string body)
    {
        if (string.IsNullOrEmpty(textId))
        {
            return;
        }

        bool changed = false;
        if (!ancientTextRuntimeStates.TryGetValue(textId, out AncientTextRuntimeState state))
        {
            state = new AncientTextRuntimeState();
            ancientTextRuntimeStates[textId] = state;
            changed = true;
        }

        if (!string.IsNullOrEmpty(title) && !string.Equals(state.title, title, StringComparison.Ordinal))
        {
            state.title = title;
            changed = true;
        }

        if (!string.IsNullOrEmpty(body) && !string.Equals(state.body, body, StringComparison.Ordinal))
        {
            state.body = body;
            changed = true;
        }

        if (changed)
        {
            SaveWorldState();
        }
    }

    public bool DiscoverAncientText(string textId)
    {
        if (string.IsNullOrEmpty(textId))
        {
            return false;
        }

        if (!ancientTextRuntimeStates.TryGetValue(textId, out AncientTextRuntimeState state))
        {
            state = new AncientTextRuntimeState();
            ancientTextRuntimeStates[textId] = state;
        }

        bool wasNewDiscovery = !state.discovered;
        state.discovered = true;
        if (wasNewDiscovery)
        {
            SaveWorldState();
        }
        return wasNewDiscovery;
    }

    public bool IsAncientTextDiscovered(string textId)
    {
        if (string.IsNullOrEmpty(textId))
        {
            return false;
        }

        return ancientTextRuntimeStates.TryGetValue(textId, out AncientTextRuntimeState state) && state.discovered;
    }

    public bool TryGetAncientTextEntry(string textId, out string title, out string body, out bool discovered)
    {
        title = string.Empty;
        body = string.Empty;
        discovered = false;

        if (string.IsNullOrEmpty(textId))
        {
            return false;
        }

        if (!ancientTextRuntimeStates.TryGetValue(textId, out AncientTextRuntimeState state))
        {
            return false;
        }

        title = string.IsNullOrEmpty(state.title) ? textId : state.title;
        body = string.IsNullOrEmpty(state.body) ? string.Empty : state.body;
        discovered = state.discovered;
        return true;
    }

    public string[] GetDiscoveredAncientTextIds()
    {
        List<string> discoveredIds = new List<string>();
        foreach (KeyValuePair<string, AncientTextRuntimeState> pair in ancientTextRuntimeStates)
        {
            if (pair.Value != null && pair.Value.discovered)
            {
                discoveredIds.Add(pair.Key);
            }
        }

        discoveredIds.Sort(StringComparer.Ordinal);
        return discoveredIds.ToArray();
    }

    public bool MarkNarrativeBeatCompleted(string beatId)
    {
        if (string.IsNullOrEmpty(beatId))
        {
            return false;
        }

        bool added = completedNarrativeBeatIds.Add(beatId);
        if (added)
        {
            SaveWorldState();
        }

        return added;
    }

    public bool IsNarrativeBeatCompleted(string beatId)
    {
        if (string.IsNullOrEmpty(beatId))
        {
            return false;
        }

        return completedNarrativeBeatIds.Contains(beatId);
    }

    public int GetFinalBossDefeatCount(string islandId)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId)
            || !finalBossDefeatCounts.TryGetValue(scopedIslandId, out int defeats))
        {
            return 0;
        }

        return Mathf.Max(0, defeats);
    }

    public int GetConfiguredFinalBossDefeatThreshold(string islandId)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            return BossEncounterGate.DefaultDefeatsForBadEnding;
        }

        int sceneThreshold = ResolveFinalBossDefeatThreshold(scopedIslandId);
        if (!IsFinalIslandForEnding(scopedIslandId))
        {
            return sceneThreshold;
        }

        if (sceneThreshold != BossEncounterGate.DefaultDefeatsForBadEnding)
        {
            return sceneThreshold;
        }

        return Mathf.Max(1, finalBossDefeatsForBadEndingThreshold);
    }

    public bool RecordFinalBossDefeatAttempt(string islandId, int defeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            return false;
        }

        int defeats = GetFinalBossDefeatCount(scopedIslandId) + 1;
        finalBossDefeatCounts[scopedIslandId] = defeats;
        SaveWorldState();

        if (defeats >= Mathf.Max(1, defeatsForBadEndingThreshold))
        {
            SetEndingBranch(EndingBranch.Bad);
            PlayEndingSequence(resolvedEndingBranch);
            return true;
        }

        return false;
    }

    public void SetFinalBossDefeatCount(string islandId, int defeats)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            return;
        }

        int sanitized = Mathf.Max(0, defeats);
        if (sanitized <= 0)
        {
            finalBossDefeatCounts.Remove(scopedIslandId);
        }
        else
        {
            finalBossDefeatCounts[scopedIslandId] = sanitized;
        }

        SaveWorldState();
    }

    public bool RecordFinalBossDefeatAttemptAndQueueEvent(string islandId, int defeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding)
    {
        bool reachedBadEnding = RecordFinalBossDefeatAttempt(islandId, defeatsForBadEndingThreshold);
        if (reachedBadEnding)
        {
            pendingBadEndingThresholdEventIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        }

        return reachedBadEnding;
    }

    public string[] GetThresholdOnlyBossVictoryIslandIds()
    {
        return BuildSortedIslandArray(thresholdOnlyBossVictoryIslandIds);
    }

    public string[] GetThresholdOnlyProceedIslandIds()
    {
        return BuildSortedIslandArray(thresholdOnlyProceedIslandIds);
    }

    public int GetConfiguredFinalBossDefeatThreshold()
    {
        return Mathf.Max(1, finalBossDefeatsForBadEndingThreshold);
    }

    public string GetFinalProgressionIslandIdForDebug()
    {
        return GetFinalProgressionIslandIdOrEmpty();
    }

    public void SetMinimumRestorationBadEndingRuleModeForDebug(MinimumRestorationBadEndingRuleMode mode)
    {
        minimumRestorationBadEndingRuleMode = mode;
        SaveWorldState();
    }

    public void ForceStoryActForDebug(StoryAct act)
    {
        currentStoryAct = ClampStoryAct(act);
        highestStoryActReached = currentStoryAct;
        SaveWorldState();
    }

    public void ForceEndingBranchForDebug(EndingBranch branch)
    {
        resolvedEndingBranch = branch;
        endingTriggered = branch != EndingBranch.None;
        if (branch == EndingBranch.None)
        {
            pendingBadEndingThresholdEventIslandId = null;
        }

        SaveWorldState();
    }

    public void RefreshStoryProgressionForDebug()
    {
        ReconcileStoryProgressionFromIslandState();
    }

    public void ResetStoryProgressionForDebug()
    {
        ancientTextRuntimeStates.Clear();
        completedNarrativeBeatIds.Clear();
        currentStoryAct = StoryAct.ActI;
        highestStoryActReached = StoryAct.ActI;
        finalBossDefeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding;
        resolvedEndingBranch = EndingBranch.None;
        endingTriggered = false;
        ceremonyIntroCompleted = false;
        minimumRestorationBadEndingRuleMode = MinimumRestorationBadEndingRuleMode.OptionalContentOnly;
        pendingBadEndingThresholdEventIslandId = null;
        thresholdOnlyBossVictoryIslandIds.Clear();
        thresholdOnlyProceedIslandIds.Clear();
        finalBossDefeatCounts.Clear();
        observedActiveIslandId = IslandProgressionManager.Instance != null
            ? IslandProgressionManager.Instance.ActiveIslandId
            : IslandThemeRegistry.GetActiveIslandId();

        NarrativeBeatDirector narrativeBeatDirector = FindFirstObjectByType<NarrativeBeatDirector>();
        if (narrativeBeatDirector != null)
        {
            narrativeBeatDirector.ResetForDebug();
        }

        SaveWorldState();
    }

    private void ReconcileStoryProgressionFromIslandState()
    {
        StoryAct targetAct = DetermineStoryActFromProgression();
        AdvanceStoryActIfHigher(targetAct);
    }

    private StoryAct DetermineStoryActFromProgression()
    {
        if (IslandProgressionManager.Instance == null)
        {
            return currentStoryAct;
        }

        int islandIndex = IslandProgressionManager.Instance.GetIslandProgressIndex(IslandProgressionManager.Instance.ActiveIslandId);
        if (islandIndex >= IslandThemeRegistry.ProgressionOrder.Count - 1)
        {
            return StoryAct.ActIII;
        }

        if (islandIndex >= 3)
        {
            return StoryAct.ActII;
        }

        return StoryAct.ActI;
    }

    private void AdvanceStoryActIfHigher(StoryAct act)
    {
        StoryAct clampedAct = ClampStoryAct(act);
        bool changed = false;
        bool actAdvanced = false;

        if ((int)clampedAct > (int)highestStoryActReached)
        {
            highestStoryActReached = clampedAct;
            changed = true;
        }

        if ((int)clampedAct > (int)currentStoryAct)
        {
            currentStoryAct = clampedAct;
            changed = true;
            actAdvanced = true;
        }

        if (changed)
        {
            SaveWorldState();

            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.HandleActTransition();
            }
        }

        if (actAdvanced)
        {
            OnStoryActChanged?.Invoke(currentStoryAct);
        }
    }

    private void SetEndingBranch(EndingBranch branch)
    {
        if (branch == EndingBranch.None)
        {
            resolvedEndingBranch = EndingBranch.None;
            endingTriggered = false;
            SaveWorldState();
            return;
        }

        if (endingTriggered && resolvedEndingBranch == branch)
        {
            return;
        }

        resolvedEndingBranch = branch;
        endingTriggered = true;
        SaveWorldState();
    }

    private static void PlayEndingSequence(EndingBranch branch)
    {
        EndingSequenceDirector director = EndingSequenceDirector.Instance;
        if (director == null)
        {
            GameObject go = new GameObject("EndingSequenceDirector");
            director = go.AddComponent<EndingSequenceDirector>();
        }

        director.PlayEnding(branch);
    }

    private static StoryAct ClampStoryAct(StoryAct act)
    {
        int value = Mathf.Clamp((int)act, (int)StoryAct.ActI, (int)StoryAct.ActIII);
        return (StoryAct)value;
    }

    private static string[] BuildSortedIslandArray(HashSet<string> islandIds)
    {
        List<string> ordered = new List<string>();
        if (islandIds == null)
        {
            return ordered.ToArray();
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (islandIds.Contains(islandId))
            {
                ordered.Add(islandId);
            }
        }

        foreach (string islandId in islandIds)
        {
            if (!ordered.Contains(islandId))
            {
                ordered.Add(islandId);
            }
        }

        return ordered.ToArray();
    }

    private StoryProgressionSaveData CaptureStoryProgressionSaveData()
    {
        StoryProgressionSaveData saveData = new StoryProgressionSaveData
        {
            currentAct = (int)currentStoryAct,
            highestActReached = (int)highestStoryActReached,
            finalBossDefeatThreshold = finalBossDefeatsForBadEndingThreshold,
            minimumRestorationRuleMode = (int)minimumRestorationBadEndingRuleMode,
            pendingBadEndingThresholdEventIslandId = pendingBadEndingThresholdEventIslandId,
            endingBranch = resolvedEndingBranch.ToString(),
            endingTriggered = endingTriggered
        };

        foreach (string islandId in thresholdOnlyBossVictoryIslandIds)
        {
            saveData.thresholdOnlyBossVictoryIslandIds.Add(islandId);
        }

        foreach (string islandId in thresholdOnlyProceedIslandIds)
        {
            saveData.thresholdOnlyProceedIslandIds.Add(islandId);
        }

        foreach (KeyValuePair<string, int> pair in finalBossDefeatCounts)
        {
            saveData.finalBossDefeats.Add(new FinalBossDefeatSaveEntry
            {
                islandId = pair.Key,
                defeats = Mathf.Max(0, pair.Value)
            });
        }

        return saveData;
    }

    private void ApplyStoryProgressionSaveData(StoryProgressionSaveData saveData)
    {
        currentStoryAct = StoryAct.ActI;
        highestStoryActReached = StoryAct.ActI;
        finalBossDefeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding;
        minimumRestorationBadEndingRuleMode = MinimumRestorationBadEndingRuleMode.OptionalContentOnly;
        resolvedEndingBranch = EndingBranch.None;
        endingTriggered = false;
        pendingBadEndingThresholdEventIslandId = null;
        thresholdOnlyBossVictoryIslandIds.Clear();
        thresholdOnlyProceedIslandIds.Clear();
        finalBossDefeatCounts.Clear();

        if (saveData == null)
        {
            return;
        }

        currentStoryAct = ClampStoryAct((StoryAct)saveData.currentAct);
        highestStoryActReached = ClampStoryAct((StoryAct)Mathf.Max(saveData.highestActReached, saveData.currentAct));
        finalBossDefeatsForBadEndingThreshold = Mathf.Max(1, saveData.finalBossDefeatThreshold > 0
            ? saveData.finalBossDefeatThreshold
            : BossEncounterGate.DefaultDefeatsForBadEnding);
        minimumRestorationBadEndingRuleMode = Enum.IsDefined(typeof(MinimumRestorationBadEndingRuleMode), saveData.minimumRestorationRuleMode)
            ? (MinimumRestorationBadEndingRuleMode)saveData.minimumRestorationRuleMode
            : MinimumRestorationBadEndingRuleMode.OptionalContentOnly;
        pendingBadEndingThresholdEventIslandId = string.IsNullOrEmpty(saveData.pendingBadEndingThresholdEventIslandId)
            ? null
            : IslandThemeRegistry.ResolveIslandId(saveData.pendingBadEndingThresholdEventIslandId);
        endingTriggered = saveData.endingTriggered;

        if (!string.IsNullOrEmpty(saveData.endingBranch)
            && Enum.TryParse(saveData.endingBranch, true, out EndingBranch parsedEndingBranch))
        {
            resolvedEndingBranch = parsedEndingBranch;
        }

        if (saveData.thresholdOnlyBossVictoryIslandIds != null)
        {
            for (int i = 0; i < saveData.thresholdOnlyBossVictoryIslandIds.Count; i++)
            {
                string sourceIslandId = saveData.thresholdOnlyBossVictoryIslandIds[i];
                string islandId = string.IsNullOrEmpty(sourceIslandId)
                    ? string.Empty
                    : IslandThemeRegistry.ResolveIslandId(sourceIslandId);
                if (!string.IsNullOrEmpty(islandId))
                {
                    thresholdOnlyBossVictoryIslandIds.Add(islandId);
                }
            }
        }

        if (saveData.thresholdOnlyProceedIslandIds != null)
        {
            for (int i = 0; i < saveData.thresholdOnlyProceedIslandIds.Count; i++)
            {
                string sourceIslandId = saveData.thresholdOnlyProceedIslandIds[i];
                string islandId = string.IsNullOrEmpty(sourceIslandId)
                    ? string.Empty
                    : IslandThemeRegistry.ResolveIslandId(sourceIslandId);
                if (!string.IsNullOrEmpty(islandId))
                {
                    thresholdOnlyProceedIslandIds.Add(islandId);
                }
            }
        }

        if (saveData.finalBossDefeats != null)
        {
            for (int i = 0; i < saveData.finalBossDefeats.Count; i++)
            {
                FinalBossDefeatSaveEntry entry = saveData.finalBossDefeats[i];
                if (entry == null)
                {
                    continue;
                }

                string islandId = IslandThemeRegistry.ResolveIslandId(entry.islandId);
                if (!string.IsNullOrEmpty(islandId))
                {
                    finalBossDefeatCounts[islandId] = Mathf.Max(0, entry.defeats);
                }
            }
        }
    }

    public void SaveWorldState()
    {
        if (!enablePersistentSaveData)
        {
            return;
        }

        if (isSavingWorldState || isLoadingWorldState)
        {
            return;
        }

        isSavingWorldState = true;
        try
        {
            WorldStateSaveData saveData = new WorldStateSaveData();

            foreach (KeyValuePair<string, PuzzleRuntimeState> pair in puzzleRuntimeStates)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                PuzzleRuntimeSaveEntry entry = new PuzzleRuntimeSaveEntry
                {
                    puzzleBoxId = pair.Key,
                    hasGrid = pair.Value.hasGrid,
                    solved = pair.Value.solved,
                    gridRows = pair.Value.gridRows,
                    gridCols = pair.Value.gridCols,
                    tileValues = new int[pair.Value.tileValues.Length]
                };

                for (int i = 0; i < entry.tileValues.Length; i++)
                {
                    entry.tileValues[i] = pair.Value.tileValues[i];
                }

                saveData.puzzleStates.Add(entry);
            }

            foreach (KeyValuePair<string, AncientTextRuntimeState> pair in ancientTextRuntimeStates)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                AncientTextRuntimeSaveEntry entry = new AncientTextRuntimeSaveEntry
                {
                    textId = pair.Key,
                    title = pair.Value.title,
                    body = pair.Value.body,
                    discovered = pair.Value.discovered
                };
                saveData.ancientTextStates.Add(entry);
            }

            foreach (string beatId in completedNarrativeBeatIds)
            {
                if (!string.IsNullOrEmpty(beatId))
                {
                    saveData.completedNarrativeBeatIds.Add(beatId);
                }
            }

            EnsureRestorationTracker();

            if (IslandRestorationTracker.Instance != null)
            {
                saveData.restorationSnapshot = IslandRestorationTracker.Instance.CaptureSnapshot();
            }

            if (HeroProgressionManager.Instance != null)
            {
                saveData.gearProgression = HeroProgressionManager.Instance.CaptureGearSnapshot();
            }

            if (PlayerGearInventory.Instance != null)
            {
                saveData.gearInventory = PlayerGearInventory.Instance.CaptureGearInventorySnapshot();
            }

            if (IslandProgressionManager.Instance != null)
            {
                saveData.progressionSnapshot = IslandProgressionManager.Instance.CaptureSnapshot();
            }

            saveData.storyProgression = CaptureStoryProgressionSaveData();
            saveData.ceremonyIntroCompleted = ceremonyIntroCompleted;

            if (TideBreakProgressionManager.Instance != null)
            {
                saveData.tideBreakProgression = TideBreakProgressionManager.Instance.GetSaveData();
            }

            if (AncientTextRevealDirector.Instance != null)
            {
                saveData.ancientTextReveal = AncientTextRevealDirector.Instance.CaptureSaveData();
            }

            if (HeroProgressionManager.Instance != null)
            {
                saveData.heroProgression = HeroProgressionManager.Instance.CaptureHeroProgressionSnapshot();
                saveData.partyComposition = HeroProgressionManager.Instance.CapturePartyCompositionSnapshot();
            }

            saveData.hubState = CaptureHubState();

            if (DialogueSystem.Instance != null)
            {
                saveData.dialogueState = DialogueSystem.Instance.CaptureDialogueStateSaveData();
            }

            string payload = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(WorldStateSaveKey, payload);
            PlayerPrefs.Save();
        }
        finally
        {
            isSavingWorldState = false;
        }
    }

    public void LoadWorldState()
    {
        hasLoadedWorldState = true;

        if (!enablePersistentSaveData)
        {
            return;
        }

        if (isLoadingWorldState)
        {
            return;
        }

        if (!PlayerPrefs.HasKey(WorldStateSaveKey))
        {
            return;
        }

        if (!HasLoadableWorldState())
        {
            Debug.LogError("[GameStateManager] Persisted world state is invalid and cannot be loaded.");
            return;
        }

        string payload = PlayerPrefs.GetString(WorldStateSaveKey, string.Empty);
        if (string.IsNullOrEmpty(payload))
        {
            return;
        }

        WorldStateSaveData saveData = JsonUtility.FromJson<WorldStateSaveData>(payload);
        if (saveData == null)
        {
            return;
        }

        isLoadingWorldState = true;

        try
        {
            puzzleRuntimeStates.Clear();
            if (saveData.puzzleStates != null)
            {
                for (int i = 0; i < saveData.puzzleStates.Count; i++)
                {
                    PuzzleRuntimeSaveEntry entry = saveData.puzzleStates[i];
                    if (entry == null || string.IsNullOrEmpty(entry.puzzleBoxId))
                    {
                        continue;
                    }

                    int legacyRows = entry.gridRows > 0 ? entry.gridRows : 3;
                    int legacyCols = entry.gridCols > 0 ? entry.gridCols : 3;
                    PuzzleRuntimeState state = new PuzzleRuntimeState
                    {
                        hasGrid = entry.hasGrid,
                        solved = entry.solved,
                        gridRows = legacyRows,
                        gridCols = legacyCols,
                        tileValues = new int[legacyRows * legacyCols]
                    };

                    if (entry.tileValues != null)
                    {
                        int copyLength = Mathf.Min(state.tileValues.Length, entry.tileValues.Length);
                        for (int j = 0; j < copyLength; j++)
                        {
                            state.tileValues[j] = Mathf.Clamp(entry.tileValues[j], 1, 10);
                        }
                    }

                    puzzleRuntimeStates[entry.puzzleBoxId] = state;
                }
            }

            ancientTextRuntimeStates.Clear();
            if (saveData.ancientTextStates != null)
            {
                for (int i = 0; i < saveData.ancientTextStates.Count; i++)
                {
                    AncientTextRuntimeSaveEntry entry = saveData.ancientTextStates[i];
                    if (entry == null || string.IsNullOrEmpty(entry.textId))
                    {
                        continue;
                    }

                    AncientTextRuntimeState state = new AncientTextRuntimeState
                    {
                        title = entry.title,
                        body = entry.body,
                        discovered = entry.discovered
                    };

                    ancientTextRuntimeStates[entry.textId] = state;
                }
            }

            completedNarrativeBeatIds.Clear();
            if (saveData.completedNarrativeBeatIds != null)
            {
                for (int i = 0; i < saveData.completedNarrativeBeatIds.Count; i++)
                {
                    string beatId = saveData.completedNarrativeBeatIds[i];
                    if (!string.IsNullOrEmpty(beatId))
                    {
                        completedNarrativeBeatIds.Add(beatId);
                    }
                }
            }

            if (IslandRestorationTracker.Instance != null && saveData.restorationSnapshot != null)
            {
                IslandRestorationTracker.Instance.ApplySnapshot(saveData.restorationSnapshot);
            }

            if (HeroProgressionManager.Instance != null && saveData.gearProgression != null)
            {
                HeroProgressionManager.Instance.ApplyGearSnapshot(saveData.gearProgression);
            }

            if (PlayerGearInventory.Instance != null && saveData.gearInventory != null)
            {
                PlayerGearInventory.Instance.ApplyGearInventorySnapshot(saveData.gearInventory);
            }

            if (IslandProgressionManager.Instance != null && saveData.progressionSnapshot != null)
            {
                IslandProgressionManager.Instance.ApplySnapshot(saveData.progressionSnapshot);
            }

            ApplyStoryProgressionSaveData(saveData.storyProgression);
            ceremonyIntroCompleted = saveData.ceremonyIntroCompleted;

            if (TideBreakProgressionManager.Instance != null && saveData.tideBreakProgression != null)
            {
                TideBreakProgressionManager.Instance.ApplySaveData(saveData.tideBreakProgression);
            }

            if (AncientTextRevealDirector.Instance != null && saveData.ancientTextReveal != null)
            {
                AncientTextRevealDirector.Instance.ApplySaveData(saveData.ancientTextReveal);
            }

            if (HeroProgressionManager.Instance != null)
            {
                if (saveData.partyComposition != null)
                {
                    HeroProgressionManager.Instance.ApplyPartyCompositionSnapshot(saveData.partyComposition);
                }

                if (saveData.heroProgression != null)
                {
                    HeroProgressionManager.Instance.ApplyHeroProgressionSnapshot(saveData.heroProgression);
                }
            }

            ApplyHubState(saveData.hubState);

            if (DialogueSystem.Instance != null && saveData.dialogueState != null)
            {
                DialogueSystem.Instance.ApplyDialogueStateSaveData(saveData.dialogueState);
            }

            // Saves created before dialogue bonds were added still carry
            // fragment-earned relationship progress in the reveal director.
            AncientTextRevealDirector.Instance?.ReconcileDialogueBonds();

            PuzzleGuardSpawner guardSpawner = FindFirstObjectByType<PuzzleGuardSpawner>();
            if (guardSpawner != null)
            {
                guardSpawner.RefreshGuards();
            }
        }
        finally
        {
            isLoadingWorldState = false;
        }
    }

    public void CompletePuzzleInExploration(string puzzleBoxId, string islandId, string encounterId, float restorationValue)
    {
        MarkPuzzleBoxSolved(puzzleBoxId);

        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        string scopedEncounterId = ResolvePuzzleEncounterId(encounterId, puzzleBoxId, scopedIslandId);
        float contribution = restorationValue > 0f ? restorationValue : 0.2f;

        if (IslandRestorationTracker.Instance != null)
        {
            bool recorded = IslandRestorationTracker.Instance.RecordEncounterCompletion(
                scopedIslandId,
                scopedEncounterId,
                EncounterType.Puzzle,
                contribution);
            if (recorded)
            {
                Debug.Log($"[GameStateManager] Recorded puzzle completion for island '{scopedIslandId}', encounter '{scopedEncounterId}'.");
            }
        }

        SaveWorldState();

        OnPuzzleCompleted?.Invoke();
        PlayPuzzleSolvedSting();

        PuzzleGuardSpawner guardSpawner = FindFirstObjectByType<PuzzleGuardSpawner>();
        if (guardSpawner != null)
        {
            guardSpawner.RefreshGuards();
        }
    }

    public void MarkPuzzleSolved()
    {
        PuzzleSolved = true;
    }

    private static void PlayPuzzleSolvedSting()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandlePuzzleSolved();
        }
    }

    public void OnCombatEnded(bool playerWon)
    {
        OnCombatEnded(playerWon, false);
    }

    public void OnCombatEnded(bool playerWon, bool playerFled)
    {
        string bossTrackedIslandId = ResolvePendingBossIslandId();
        bool isBossEncounter = !string.IsNullOrEmpty(bossTrackedIslandId);
        bool isFinalBossEncounter = isBossEncounter && IsFinalIslandForEnding(bossTrackedIslandId);
        float preBossRestorationPercent = isBossEncounter
            ? GetIslandRestorationPercent(bossTrackedIslandId)
            : 0f;

        if (!playerWon && !playerFled)
        {
            NotifyBossDefeatAttempt();
        }

        if (playerWon && isBossEncounter)
        {
            TrackBossVictoryThresholdProgress(bossTrackedIslandId, preBossRestorationPercent);
        }

        if (playerWon && IslandRestorationTracker.Instance != null && !string.IsNullOrEmpty(PendingCombatEncounterId) && !string.IsNullOrEmpty(PendingCombatIslandId))
        {
            string islandId = IslandThemeRegistry.ResolveIslandId(PendingCombatIslandId);
            float contribution = PendingCombatRestorationValue > 0f ? PendingCombatRestorationValue : 0.001f;
            bool recorded = IslandRestorationTracker.Instance.RecordEncounterCompletion(
                islandId,
                PendingCombatEncounterId,
                EncounterType.Combat,
                contribution);
            if (recorded)
            {
                Debug.Log($"[GameStateManager] Recorded combat completion for island '{islandId}', encounter '{PendingCombatEncounterId}'.");
            }
        }

        if (playerWon)
        {
            GrantBattleRewards();
        }

        SaveWorldState();

        if (playerWon && isFinalBossEncounter)
        {
            ResolveFinalEndingAfterBossVictory();
        }

        if (playerFled)
        {
            Debug.Log("[GameStateManager] Player fled combat. No restoration, rewards, or defeat penalties applied.");
        }

        if (HasActiveFlowController)
        {
            hasDeferredFlowFromCombatResult = true;
            deferredFlowFromCombatResult = playerWon;
        }

        StartCoroutine(ReturnFromCombatAfterDelay(1.5f));
    }

    private void GrantBattleRewards()
    {
        if (HeroProgressionManager.Instance == null)
        {
            Debug.LogWarning("[GameStateManager] HeroProgressionManager not found. Skipping XP rewards.");
            return;
        }

        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[GameStateManager] PartyManager not found. Skipping XP rewards.");
            return;
        }

        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogWarning("[GameStateManager] BattleManager not found. Skipping XP rewards.");
            return;
        }

        int totalXp = HeroProgressionManager.Instance.GetTotalXpFromEnemies(battleManager);
        if (totalXp <= 0)
        {
            Debug.Log("[GameStateManager] No XP to grant from defeated enemies.");
            return;
        }

        HeroData[] active = PartyManager.Instance.GetActiveParty();
        HeroData[] reserve = PartyManager.Instance.GetReserveParty();

        HeroProgressionManager.Instance.GrantBattleXp(totalXp, active, reserve);

        float reserveMultiplier = HeroProgressionManager.Instance.LevelingConfig != null
            ? HeroProgressionManager.Instance.LevelingConfig.reserveXpMultiplier
            : 0.5f;
        int reserveXp = Mathf.RoundToInt(totalXp * reserveMultiplier);

        Debug.Log($"[GameStateManager] Granted {totalXp} XP to active party, {reserveXp} XP to reserve.");

        TryRollGearDrop();
    }

    private void TryRollGearDrop()
    {
        GearDropService dropService = GearDropService.ActiveInstance;
        if (dropService == null)
        {
            return;
        }

        string islandId = !string.IsNullOrEmpty(PendingCombatIslandId)
            ? IslandThemeRegistry.ResolveIslandId(PendingCombatIslandId)
            : IslandThemeRegistry.GetActiveIslandId();

        string enemyType = !string.IsNullOrEmpty(PendingCombatEncounterId) ? PendingCombatEncounterId : "generic";

        if (dropService.TryRollDrop(enemyType, islandId, out string gearSetId, out GearDropService.GearRarity rarity))
        {
            PlayerGearInventory inventory = PlayerGearInventory.Instance;
            GearInstance awarded = inventory != null ? inventory.AddGear(gearSetId, rarity) : null;
            if (awarded != null)
            {
                inventory.RecordBattleDrop(awarded);
                Debug.Log($"[GameStateManager] Gear drop awarded: {gearSetId} ({rarity}) from {enemyType} on {islandId} -> inventory instance '{awarded.instanceId}'.");
                BattleHud battleHud = FindFirstObjectByType<BattleHud>();
                if (battleHud != null)
                {
                    battleHud.ShowGearDrop(PlayerGearInventory.GetDropSummary(awarded));
                }
            }
            else
            {
                Debug.Log($"[GameStateManager] Gear drop rolled but not awarded: {gearSetId} ({rarity}) from {enemyType} on {islandId}.");
            }
        }
    }

    private IEnumerator ReturnFromCombatAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isTransitioning)
        {
            yield break;
        }

        bool shouldReturnToPuzzle = returnToPuzzleAfterCombat;
        returnToPuzzleAfterCombat = false;
        PendingCombatIslandId = null;
        PendingCombatEncounterId = null;
        PendingCombatRestorationValue = 0f;
        pendingBossIslandIdForDefeatTracking = null;

        if (shouldReturnToPuzzle)
        {
            StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
            yield break;
        }

        ReturnToMainScene();
    }

    public void ReturnToMainScene()
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionToScene(MainSceneName, GameState.Exploration));
    }

    private IEnumerator TransitionToScene(string sceneName, GameState targetState)
    {
        if (isTransitioning)
        {
            yield break;
        }

        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            Debug.LogError($"[GameStateManager] Scene '{sceneName}' not found!");
            yield break;
        }

        isTransitioning = true;
        currentState = GameState.Transition;
        SetPlayerMovementLocked(true);

        yield return FadeCanvas(1f, FadeDuration);

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        yield return null;

        currentState = targetState;
        SetPlayerMovementLocked(targetState != GameState.Exploration);

        yield return FadeCanvas(0f, FadeDuration);

        isTransitioning = false;

        if (deferredFlowFromCombat)
        {
            deferredFlowFromCombat = false;
            if (HasActiveFlowController && hasDeferredFlowFromCombatResult)
            {
                FlowController.OnReturnFromCombat(deferredFlowFromCombatResult);
            }

            hasDeferredFlowFromCombatResult = false;
            deferredFlowFromCombatResult = false;
        }
        else if (deferredFlowFromPuzzle)
        {
            deferredFlowFromPuzzle = false;
            if (HasActiveFlowController)
            {
                FlowController.OnReturnFromPuzzle();
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        hasHandledSceneLoad = true;
        hasBootstrappedFlowForCurrentScene = false;
        CachePlayer();
        EnsureRestorationTracker();

        if (scene.name == MainSceneName)
        {
            bool returnedFromPuzzleScene = hasPendingReturnPosition && !hasPendingCombatReturnPosition;

            EnsureMainSceneRuntimeComponents();
            ApplySolvedPuzzleBoxesInScene();
            LoadFinalBossDefeatStateIfAvailable();
            NotifyPendingBadEndingThresholdEventIfNeeded();

            if (PuzzleSolved)
            {
                if (!string.IsNullOrEmpty(pendingSolvedPuzzleBoxId))
                {
                    PuzzleBoxInteractable[] boxes = FindObjectsByType<PuzzleBoxInteractable>(FindObjectsSortMode.None);
                    for (int i = 0; i < boxes.Length; i++)
                    {
                        if (string.Equals(boxes[i].GetPuzzleBoxId(), pendingSolvedPuzzleBoxId, StringComparison.Ordinal))
                        {
                            boxes[i].MarkSolved();
                            break;
                        }
                    }
                }

                // Flow-controlled island encounters record restoration in IslandFlowController.OnEncounterComplete.
                // Avoid double-counting puzzle restoration here when returning from a flow puzzle.
                bool flowControlsPuzzleCompletion = HasActiveFlowController && returnedFromPuzzleScene;

                if (!flowControlsPuzzleCompletion && IslandRestorationTracker.Instance != null)
                {
                    string islandId = PendingPuzzleIslandId;
                    islandId = IslandThemeRegistry.ResolveIslandId(islandId);

                    string encounterId = PendingPuzzleEncounterId;
                    encounterId = ResolvePuzzleEncounterId(encounterId, pendingSolvedPuzzleBoxId, islandId);

                    float contribution = PendingPuzzleRestorationValue > 0f ? PendingPuzzleRestorationValue : 0.2f;

                    bool recorded = IslandRestorationTracker.Instance.RecordEncounterCompletion(
                        islandId,
                        encounterId,
                        EncounterType.Puzzle,
                        contribution);
                    if (recorded)
                    {
                        Debug.Log($"[GameStateManager] Recorded puzzle completion for island '{islandId}', encounter '{encounterId}'.");
                        SaveWorldState();
                    }
                }

                // Clear pending restoration fields
                PendingPuzzleIslandId = null;
                PendingPuzzleEncounterId = null;
                PendingPuzzleRestorationValue = 0f;

                // Trigger puzzle completed event to change ground color
                OnPuzzleCompleted?.Invoke();
                Debug.Log("[GameStateManager] Puzzle completed - ground color changed to white.");
            }

            PuzzleSolved = false;
            pendingSolvedPuzzleBoxId = null;

            if (returnedFromPuzzleScene)
            {
                pendingSolvedPuzzleBoxId = null;
                PendingPuzzleData = null;
                PendingPuzzleLayout = null;
                PendingPuzzleSealedTile = new Vector2Int(-1, -1);
                PendingPuzzleIslandId = null;
                PendingPuzzleEncounterId = null;
                PendingPuzzleRestorationValue = 0f;
            }

            if (player != null && TryResolveSafeExplorationSpawnPosition(out Vector3 safeExplorationSpawn))
            {
                player.transform.position = safeExplorationSpawn;
                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                }

                lastKnownExplorationPlayerPosition = safeExplorationSpawn;
                hasLastKnownExplorationPlayerPosition = true;

                if (IslandProgressionManager.Instance != null)
                {
                    IslandProgressionManager.Instance.RecordIslandReturnPosition(
                        IslandProgressionManager.Instance.ActiveIslandId,
                        safeExplorationSpawn);
                }
            }

            ApplyPendingCameraTransformIfAvailable();

            if (IslandProgressionManager.Instance != null)
            {
                observedActiveIslandId = IslandProgressionManager.Instance.ActiveIslandId;
            }

            ReconcileStoryProgressionFromIslandState();

            hasPendingReturnPosition = false;
            hasPendingCombatReturnPosition = false;

            if (!isTransitioning)
            {
                currentState = GameState.Exploration;
                SetPlayerMovementLocked(false);
            }

            SnapFollowCameraToPlayer();

            if (cameraSnapRoutine != null)
            {
                StopCoroutine(cameraSnapRoutine);
            }

            cameraSnapRoutine = StartCoroutine(SnapFollowCameraAfterLoad());

            // Trigger the ceremony intro on first load if not yet completed
            if (!ceremonyIntroCompleted && !isTransitioning)
            {
                CeremonyIntroDirector introDirector = FindFirstObjectByType<CeremonyIntroDirector>();
                if (introDirector != null)
                {
                    introDirector.PlayCeremonyIntro();
                }
            }

            if (isFlowControlledCombat)
            {
                deferredFlowFromCombat = true;
                isFlowControlledCombat = false;
            }
            else if (HasActiveFlowController && returnedFromPuzzleScene)
            {
                deferredFlowFromPuzzle = true;
            }
        }
        else if (scene.name == HubSceneName)
        {
            EnsureHubSceneRuntimeComponents();

            if (player != null && TryResolveSafeExplorationSpawnPosition(out Vector3 safeHubSpawn))
            {
                player.transform.position = safeHubSpawn;
                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                }

                lastKnownExplorationPlayerPosition = safeHubSpawn;
                hasLastKnownExplorationPlayerPosition = true;

                if (IslandProgressionManager.Instance != null)
                {
                    IslandProgressionManager.Instance.RecordIslandReturnPosition(
                        IslandProgressionManager.Instance.ActiveIslandId,
                        safeHubSpawn);
                }
            }

            if (!isTransitioning)
            {
                currentState = GameState.Exploration;
                SetPlayerMovementLocked(false);
            }

            SnapFollowCameraToPlayer();

            if (cameraSnapRoutine != null)
            {
                StopCoroutine(cameraSnapRoutine);
            }

            cameraSnapRoutine = StartCoroutine(SnapFollowCameraAfterLoad());

            hasPendingReturnPosition = false;
            hasPendingCombatReturnPosition = false;
        }
        else if (scene.name == PuzzleSceneName)
        {
            player = null;

            if (!isTransitioning)
            {
                currentState = GameState.Puzzle;
            }

            if (isFlowControlledCombat)
            {
                deferredFlowFromCombat = true;
                isFlowControlledCombat = false;
            }
        }
        else if (scene.name == CombatSceneName)
        {
            player = null;

            if (!isTransitioning)
            {
                currentState = GameState.Combat;
            }
        }
    }

    private void CachePlayer()
    {
        player = FindFirstObjectByType<IsometricPlayer>();
    }

    private void SetPlayerMovementLocked(bool isLocked)
    {
        CachePlayer();
        if (player != null)
        {
            player.canMove = !isLocked;
        }
    }

    private void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("SceneFadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;
    }

    private IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void CaptureExplorationReturnPosition()
    {
        CachePlayer();
        if (player == null)
        {
            return;
        }

        pendingReturnPosition = ResolveSafeReturnPosition(player.transform.position);
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = true;
        CaptureExplorationCameraTransform();
    }

    private void CaptureExplorationCameraTransform()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        pendingCameraPosition = activeCamera.transform.position;
        pendingCameraRotation = activeCamera.transform.rotation;
        hasPendingCameraTransform = true;
    }

    private void EnsureRestorationTracker()
    {
        if (IslandRestorationTracker.Instance != null)
        {
            return;
        }

        GameObject trackerObject = new GameObject("IslandRestorationTracker");
        trackerObject.AddComponent<IslandRestorationTracker>();
    }

    private void EnsureProgressionManager()
    {
        if (HeroProgressionManager.Instance == null)
        {
            GameObject managerObject = new GameObject("HeroProgressionManager");
            managerObject.AddComponent<HeroProgressionManager>();
        }

        if (IslandProgressionManager.Instance == null)
        {
            GameObject progressionObject = new GameObject("IslandProgressionManager");
            progressionObject.AddComponent<IslandProgressionManager>();
        }

        if (TideBreakProgressionManager.Instance == null)
        {
            GameObject tideBreakObject = new GameObject("TideBreakProgressionManager");
            tideBreakObject.AddComponent<TideBreakProgressionManager>();
        }

        if (AncientTextRevealDirector.Instance == null)
        {
            GameObject revealDirectorObject = new GameObject("AncientTextRevealDirector");
            revealDirectorObject.AddComponent<AncientTextRevealDirector>();
        }
    }

    private void EnsureGearInventory()
    {
        if (PlayerGearInventory.Instance == null)
        {
            GameObject inventoryObject = new GameObject("PlayerGearInventory");
            inventoryObject.AddComponent<PlayerGearInventory>();
        }
    }

    private void EnsureDialogueSystem()
    {
        if (DialogueSystem.Instance != null)
        {
            return;
        }

        GameObject dialogueObject = new GameObject("DialogueSystem");
        dialogueObject.AddComponent<DialogueSystem>();
    }

    private void BindProgressionManagerEvents()
    {
        if (IslandProgressionManager.Instance == null)
        {
            return;
        }

        IslandProgressionManager.Instance.OnActiveIslandChanged -= HandleActiveIslandChanged;
        IslandProgressionManager.Instance.OnActiveIslandChanged += HandleActiveIslandChanged;
        observedActiveIslandId = IslandProgressionManager.Instance.ActiveIslandId;

        // Wire hero level-ups to TideBreak progression
        if (HeroProgressionManager.Instance != null)
        {
            HeroProgressionManager.Instance.OnHeroLeveledUp -= HandleHeroLeveledUpForTideBreaks;
            HeroProgressionManager.Instance.OnHeroLeveledUp += HandleHeroLeveledUpForTideBreaks;
        }

        // Wire island changes to ancient text reveal checks
        IslandProgressionManager.Instance.OnActiveIslandChanged -= HandleActiveIslandChangedForReveals;
        IslandProgressionManager.Instance.OnActiveIslandChanged += HandleActiveIslandChangedForReveals;
    }

    private void UnbindProgressionManagerEvents()
    {
        if (IslandProgressionManager.Instance == null)
        {
            return;
        }

        IslandProgressionManager.Instance.OnActiveIslandChanged -= HandleActiveIslandChanged;
        IslandProgressionManager.Instance.OnActiveIslandChanged -= HandleActiveIslandChangedForReveals;

        if (HeroProgressionManager.Instance != null)
        {
            HeroProgressionManager.Instance.OnHeroLeveledUp -= HandleHeroLeveledUpForTideBreaks;
        }
    }

    private void HandleActiveIslandChanged(string islandId)
    {
        string previousIslandId = observedActiveIslandId;
        observedActiveIslandId = IslandThemeRegistry.ResolveIslandId(islandId);

        bool suppressStorySideEffects = DevCheatService.Instance != null && DevCheatService.Instance.SuppressStoryProgressionSideEffects;
        if (suppressStorySideEffects)
        {
            return;
        }

        int previousIslandIndex = IslandProgressionManager.Instance != null
            ? IslandProgressionManager.Instance.GetIslandProgressIndex(previousIslandId)
            : -1;
        int nextIslandIndex = IslandProgressionManager.Instance != null
            ? IslandProgressionManager.Instance.GetIslandProgressIndex(observedActiveIslandId)
            : -1;

        if (!string.IsNullOrEmpty(previousIslandId)
            && !string.Equals(previousIslandId, observedActiveIslandId, StringComparison.Ordinal)
            && nextIslandIndex > previousIslandIndex
            && thresholdOnlyBossVictoryIslandIds.Contains(previousIslandId))
        {
            thresholdOnlyProceedIslandIds.Add(previousIslandId);
        }

        ReconcileStoryProgressionFromIslandState();
    }

    private void HandleHeroLeveledUpForTideBreaks(string heroId, int newLevel)
    {
        if (TideBreakProgressionManager.Instance != null)
        {
            TideBreakProgressionManager.Instance.OnHeroLevelUp(heroId, newLevel);
        }
    }

    private void HandleActiveIslandChangedForReveals(string islandId)
    {
        if (AncientTextRevealDirector.Instance == null)
        {
            return;
        }

        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        float restorationPercent = GetIslandRestorationPercent(resolvedIslandId);
        AncientTextRevealDirector.Instance.CheckForNewReveals(resolvedIslandId, restorationPercent);
    }

    private void EnsureDeveloperTools()
    {
        if (!IsDeveloperGodModeAllowed())
        {
            return;
        }

        if (FindFirstObjectByType<DevModeController>() != null)
        {
            return;
        }

        GameObject devToolsObject = new GameObject("DevModeController");
        devToolsObject.AddComponent<DevModeController>();
    }

    private static void EnsureAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("AudioManager");
        audioObject.AddComponent<AudioManager>();
    }

    public bool IsDeveloperGodModeAllowed()
    {
        return enableDeveloperGodMode && (Application.isEditor || Debug.isDebugBuild);
    }

    public void ClearPersistentWorldStateForDebug(bool includeBossDefeatState = true)
    {
        PlayerPrefs.DeleteKey(WorldStateSaveKey);
        if (includeBossDefeatState)
        {
            PlayerPrefs.DeleteKey(FinalBossDefeatsSaveKey);
        }

        PlayerPrefs.Save();
    }

    public void ResetRuntimeWorldStateForDebug()
    {
        puzzleRuntimeStates.Clear();
        ancientTextRuntimeStates.Clear();
        completedNarrativeBeatIds.Clear();
        thresholdOnlyBossVictoryIslandIds.Clear();
        thresholdOnlyProceedIslandIds.Clear();
        finalBossDefeatCounts.Clear();
        currentStoryAct = StoryAct.ActI;
        highestStoryActReached = StoryAct.ActI;
        finalBossDefeatsForBadEndingThreshold = BossEncounterGate.DefaultDefeatsForBadEnding;
        resolvedEndingBranch = EndingBranch.None;
        endingTriggered = false;
        ceremonyIntroCompleted = false;
        minimumRestorationBadEndingRuleMode = MinimumRestorationBadEndingRuleMode.OptionalContentOnly;
        pendingBadEndingThresholdEventIslandId = null;
        observedActiveIslandId = IslandProgressionManager.Instance != null
            ? IslandProgressionManager.Instance.ActiveIslandId
            : IslandThemeRegistry.GetActiveIslandId();

        PendingPuzzleData = null;
        PendingPuzzleLayout = null;
        PendingPuzzleSealedTile = new Vector2Int(-1, -1);
        PendingEnemyComposition = null;
        PendingPuzzleIslandId = null;
        PendingPuzzleEncounterId = null;
        PendingPuzzleRestorationValue = 0f;
        PendingCombatIslandId = null;
        PendingCombatEncounterId = null;
        PendingCombatRestorationValue = 0f;
        pendingSolvedPuzzleBoxId = null;
        pendingBossIslandIdForDefeatTracking = null;
        PuzzleSolved = false;

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ResetDialogueStateForDebug();
        }

        if (StoryProgressionService.Instance != null)
        {
            StoryProgressionService.Instance.ClearFlagsForDebug();
        }
    }

    public void ResetPuzzleRuntimeStateForDebug()
    {
        puzzleRuntimeStates.Clear();
        pendingSolvedPuzzleBoxId = null;
        PendingPuzzleData = null;
        PendingPuzzleLayout = null;
        PendingPuzzleSealedTile = new Vector2Int(-1, -1);
        PendingPuzzleIslandId = null;
        PendingPuzzleEncounterId = null;
        PendingPuzzleRestorationValue = 0f;
        PuzzleSolved = false;
        SaveWorldState();
    }

    private void ApplySolvedPuzzleBoxesInScene()
    {
        PuzzleBoxInteractable[] boxes = FindObjectsByType<PuzzleBoxInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < boxes.Length; i++)
        {
            PuzzleBoxInteractable box = boxes[i];
            if (box == null)
            {
                continue;
            }

            if (IsPuzzleBoxSolved(box.GetPuzzleBoxId()))
            {
                box.MarkSolved();
            }
        }
    }

    private void EnsureMainSceneRuntimeComponents()
    {
        if (FindFirstObjectByType<PuzzleOverlayController>() == null)
        {
            GameObject overlayObject = new GameObject("PuzzleOverlayController");
            overlayObject.AddComponent<PuzzleOverlayController>();
        }

        if (FindFirstObjectByType<AncientTextSceneBootstrap>() == null)
        {
            GameObject ancientTextBootstrap = new GameObject("AncientTextSceneBootstrap");
            ancientTextBootstrap.AddComponent<AncientTextSceneBootstrap>();
        }

        if (FindFirstObjectByType<CeremonyIntroDirector>() == null)
        {
            GameObject ceremonyDirector = new GameObject("CeremonyIntroDirector");
            ceremonyDirector.AddComponent<CeremonyIntroDirector>();
        }

        EnsureDialogueSystem();

        if (FindFirstObjectByType<NarrativeBeatDirector>() == null)
        {
            GameObject narrativeDirector = new GameObject("NarrativeBeatDirector");
            narrativeDirector.AddComponent<NarrativeBeatDirector>();
        }

        PuzzleGuardSpawner spawner = FindFirstObjectByType<PuzzleGuardSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObject = new GameObject("PuzzleGuardSpawner");
            spawner = spawnerObject.AddComponent<PuzzleGuardSpawner>();
        }

        if (spawner != null)
        {
            spawner.RefreshGuards();
        }

        EnsureSmithyInteractable();
        EnsureIslandBoatInteractable();
        EnsurePauseMenu();
    }

    private void EnsureHubSceneRuntimeComponents()
    {
        // Dialogue system backs the hub narrative/NPC stations.
        EnsureDialogueSystem();

        EnsureSmithyInteractable();
        EnsureIslandBoatInteractable();
        EnsurePauseMenu();
    }

    /// <summary>
    /// Ensures the exploration pause menu (issue #294) exists in the scene.
    /// Runtime-created so level_1 and HubScene both get it without scene edits.
    /// </summary>
    private void EnsurePauseMenu()
    {
        if (FindFirstObjectByType<PauseMenuUI>() != null)
        {
            return;
        }

        GameObject pauseObject = new GameObject("PauseMenuUI");
        pauseObject.AddComponent<PauseMenuUI>();
    }

    private void EnsureIslandFlowController()
    {
        if (isTransitioning || currentState != GameState.Exploration)
        {
            return;
        }

        if (IslandProgressionManager.Instance == null)
        {
            return;
        }

        // The hub is a service hub, not a corrupted island: no island flow runs there.
        if (IslandThemeRegistry.IsHubIslandId(IslandProgressionManager.Instance.ActiveIslandId))
        {
            return;
        }

        IslandFlowController flowController = FindFirstObjectByType<IslandFlowController>();
        if (flowController == null)
        {
            GameObject flowObject = new GameObject("IslandFlowController");
            flowController = flowObject.AddComponent<IslandFlowController>();
        }

        if (flowController == null)
        {
            return;
        }

        if (IslandProgressionManager.Instance == null)
        {
            return;
        }

        IslandConfig activeConfig = IslandProgressionManager.Instance.GetActiveIslandConfig();
        if (activeConfig == null)
        {
            return;
        }

        string targetIslandId = IslandThemeRegistry.ResolveIslandId(activeConfig.islandId);
        if (flowController.IsActive && string.Equals(flowController.IslandId, targetIslandId, StringComparison.Ordinal))
        {
            return;
        }

        if (autoStartIslandFlowOnMainScene)
        {
            flowController.StartIsland(activeConfig);
        }
    }

    public void StartActiveIslandFlowForDebug()
    {
        if (IslandProgressionManager.Instance == null)
        {
            return;
        }

        IslandConfig activeConfig = IslandProgressionManager.Instance.GetActiveIslandConfig();
        if (activeConfig == null)
        {
            return;
        }

        IslandFlowController flowController = FindFirstObjectByType<IslandFlowController>();
        if (flowController == null)
        {
            GameObject flowObject = new GameObject("IslandFlowController");
            flowController = flowObject.AddComponent<IslandFlowController>();
        }

        flowController.StartIsland(activeConfig);
    }

    public void CancelActiveIslandFlowForDebug()
    {
        IslandFlowController flowController = FindFirstObjectByType<IslandFlowController>();
        if (flowController != null)
        {
            flowController.StopFlowForDebug();
        }

        if (FlowController == flowController)
        {
            FlowController = null;
        }
    }

    public void HandleIslandTravelFlowReset()
    {
        hasBootstrappedFlowForCurrentScene = false;
        EnsureIslandFlowController();
    }

    public bool TravelToIsland(string destinationIslandId, Vector3 destinationSpawn)
    {
        if (isTransitioning)
        {
            return false;
        }

        if (currentState != GameState.Exploration)
        {
            return false;
        }

        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(destinationIslandId);
        if (string.IsNullOrEmpty(resolvedIslandId))
        {
            return false;
        }

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager == null)
        {
            return false;
        }

        if (!progressionManager.CanTravelToIsland(resolvedIslandId))
        {
            return false;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return false;
        }

        Vector3 safeSpawn = ResolveSafeReturnPosition(destinationSpawn);
        if (!IsFiniteVector(safeSpawn))
        {
            return false;
        }

        string previousIslandId = progressionManager.ActiveIslandId;
        Vector3 currentPosition = player.transform.position;
        if (IsFiniteVector(currentPosition))
        {
            progressionManager.RecordIslandReturnPosition(previousIslandId, currentPosition);
        }

        if (!progressionManager.TrySetActiveIslandForTravel(resolvedIslandId))
        {
            return false;
        }

        pendingReturnPosition = safeSpawn;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingBossIslandIdForDefeatTracking = null;

        HandleIslandTravelFlowReset();
        SaveWorldState();
        Debug.Log($"[GameStateManager] Travel fade pipeline: '{previousIslandId}' -> '{resolvedIslandId}' at {safeSpawn}.");

        StartCoroutine(TravelFadeAndRepositionRoutine(safeSpawn));
        return true;
    }

    public void RecordHubReturnPosition(Vector3 worldPosition)
    {
        if (!IsFiniteVector(worldPosition))
        {
            return;
        }

        hubReturnPosition = worldPosition;
        hasHubReturnPosition = true;
    }

    public bool TryGetHubReturnPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!hasHubReturnPosition || !IsFiniteVector(hubReturnPosition))
        {
            return false;
        }

        worldPosition = hubReturnPosition;
        return true;
    }

    private HubStateSaveData CaptureHubState()
    {
        HubStateSaveData saveData = new HubStateSaveData
        {
            hasReturnPosition = hasHubReturnPosition && IsFiniteVector(hubReturnPosition)
        };

        if (saveData.hasReturnPosition)
        {
            saveData.returnX = hubReturnPosition.x;
            saveData.returnY = hubReturnPosition.y;
            saveData.returnZ = hubReturnPosition.z;
        }

        return saveData;
    }

    private void ApplyHubState(HubStateSaveData saveData)
    {
        hasHubReturnPosition = false;
        hubReturnPosition = Vector3.zero;

        if (saveData == null || !saveData.hasReturnPosition)
        {
            return;
        }

        Vector3 candidate = new Vector3(saveData.returnX, saveData.returnY, saveData.returnZ);
        if (!IsFiniteVector(candidate))
        {
            return;
        }

        hubReturnPosition = candidate;
        hasHubReturnPosition = true;
    }

    public bool TravelToHub(Vector3 destinationSpawn)
    {
        if (isTransitioning)
        {
            return false;
        }

        if (currentState != GameState.Exploration)
        {
            return false;
        }

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager == null)
        {
            return false;
        }

        string hubIslandId = IslandThemeRegistry.ResolveIslandId(IslandThemeRegistry.HubIslandId);
        if (string.IsNullOrEmpty(hubIslandId))
        {
            return false;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return false;
        }

        Vector3 safeSpawn = ResolveSafeReturnPosition(destinationSpawn);
        if (!IsFiniteVector(safeSpawn))
        {
            return false;
        }

        string previousIslandId = progressionManager.ActiveIslandId;
        Vector3 currentPosition = player.transform.position;
        if (IsFiniteVector(currentPosition))
        {
            progressionManager.RecordIslandReturnPosition(previousIslandId, currentPosition);
        }

        if (!progressionManager.TrySetActiveIslandForTravel(hubIslandId))
        {
            return false;
        }

        pendingReturnPosition = safeSpawn;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingBossIslandIdForDefeatTracking = null;

        // No island flow runs at the hub; just clear the bootstrap flag so a
        // later return to an island scene starts fresh flow.
        hasBootstrappedFlowForCurrentScene = false;
        SaveWorldState();
        Debug.Log($"[GameStateManager] Travel to hub from '{previousIslandId}' at {safeSpawn}.");

        StartCoroutine(TransitionToScene(HubSceneName, GameState.Exploration));
        return true;
    }

    public bool TravelFromHubToIsland(string destinationIslandId, Vector3 destinationSpawn)
    {
        if (isTransitioning)
        {
            return false;
        }

        if (currentState != GameState.Exploration)
        {
            return false;
        }

        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(destinationIslandId);
        if (string.IsNullOrEmpty(resolvedIslandId))
        {
            return false;
        }

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager == null)
        {
            return false;
        }

        if (!progressionManager.CanTravelToIsland(resolvedIslandId))
        {
            return false;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return false;
        }

        Vector3 safeSpawn = ResolveSafeReturnPosition(destinationSpawn);
        if (!IsFiniteVector(safeSpawn))
        {
            return false;
        }

        // Persist where the player was standing in the hub so return travel
        // lands them back at the same spot.
        Vector3 currentPosition = player.transform.position;
        if (IsFiniteVector(currentPosition))
        {
            RecordHubReturnPosition(currentPosition);
            progressionManager.RecordIslandReturnPosition(IslandThemeRegistry.HubIslandId, currentPosition);
        }

        if (!progressionManager.TrySetActiveIslandForTravel(resolvedIslandId))
        {
            return false;
        }

        pendingReturnPosition = safeSpawn;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingBossIslandIdForDefeatTracking = null;

        // Island flow bootstraps via LateUpdate once level_1 has loaded.
        hasBootstrappedFlowForCurrentScene = false;
        SaveWorldState();
        Debug.Log($"[GameStateManager] Travel from hub to '{resolvedIslandId}' at {safeSpawn}.");

        StartCoroutine(TransitionToScene(MainSceneName, GameState.Exploration));
        return true;
    }

    private IEnumerator TravelFadeAndRepositionRoutine(Vector3 destinationSpawn)
    {
        EnsureFadeCanvas();
        isTransitioning = true;
        yield return FadeCanvas(1f, FadeDuration);

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            player.transform.position = destinationSpawn;
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }
        }

        TopDownFollowCamera followCamera = FindFirstObjectByType<TopDownFollowCamera>();
        if (followCamera != null)
        {
            followCamera.SetTarget(player != null ? player.transform : null, false);
            followCamera.SnapToCurrentTarget();
        }

        if (IslandProgressionManager.Instance != null && IsFiniteVector(destinationSpawn))
        {
            IslandProgressionManager.Instance.RecordIslandReturnPosition(IslandProgressionManager.Instance.ActiveIslandId, destinationSpawn);
        }

        yield return FadeCanvas(0f, FadeDuration);
        isTransitioning = false;
        hasPendingReturnPosition = false;
    }

    private static string ResolvePuzzleEncounterId(string encounterId, string puzzleBoxId, string islandId)
    {
        if (!string.IsNullOrEmpty(encounterId))
        {
            return encounterId;
        }

        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        string stablePuzzleId = string.IsNullOrEmpty(puzzleBoxId) ? "unknown_box" : puzzleBoxId;
        return $"__puzzle_complete__::{scopedIslandId}::{stablePuzzleId}";
    }

    private static bool IsBossEncounterId(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        return encounterId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void LateUpdate()
    {
        if (hasBootstrappedFlowForCurrentScene)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != MainSceneName)
        {
            return;
        }

        if (isTransitioning || currentState != GameState.Exploration)
        {
            return;
        }

        EnsureIslandFlowController();
        hasBootstrappedFlowForCurrentScene = true;
    }

    private void EnsureSmithyInteractable()
    {
        if (FindFirstObjectByType<SmithyInteractable>() != null)
        {
            return;
        }

        GameObject smithyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        smithyObject.name = "SmithyStation";
        smithyObject.transform.position = defaultSmithyPosition;
        smithyObject.transform.localScale = defaultSmithyScale;

        smithyObject.AddComponent<SmithyInteractable>();
    }

    private void EnsureIslandBoatInteractable()
    {
        if (FindFirstObjectByType<IslandBoatInteractable>() != null)
        {
            return;
        }

        GameObject boatObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        boatObject.name = "IslandBoat";
        boatObject.transform.position = defaultBoatPosition;
        boatObject.transform.localScale = defaultBoatScale;

        Renderer renderer = boatObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.18f, 0.36f, 0.52f, 1f);
        }

        boatObject.AddComponent<IslandBoatInteractable>();
    }

    private void SnapFollowCameraToPlayer()
    {
        CachePlayer();
        if (player == null)
        {
            return;
        }

        TopDownFollowCamera followCamera = FindFirstObjectByType<TopDownFollowCamera>();
        if (followCamera != null)
        {
            followCamera.SetTarget(player.transform, false);
            followCamera.ResetToDefaultOffset();
            followCamera.SnapToCurrentTarget();
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        Vector3 currentPosition = activeCamera.transform.position;
        Vector3 toPlayer = player.transform.position;
        activeCamera.transform.position = new Vector3(toPlayer.x, currentPosition.y, toPlayer.z);
    }

    private IEnumerator SnapFollowCameraAfterLoad()
    {
        yield return null;
        ApplyPendingCameraTransformIfAvailable();
        SnapFollowCameraToPlayer();
        yield return new WaitForEndOfFrame();
        ApplyPendingCameraTransformIfAvailable();
        SnapFollowCameraToPlayer();
        cameraSnapRoutine = null;
    }

    private void ApplyPendingCameraTransformIfAvailable()
    {
        if (!hasPendingCameraTransform)
        {
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        activeCamera.transform.position = pendingCameraPosition;
        activeCamera.transform.rotation = pendingCameraRotation;

        TopDownFollowCamera followCamera = activeCamera.GetComponent<TopDownFollowCamera>();
        if (followCamera != null)
        {
            followCamera.CaptureCurrentOffsetAsDefault();
        }

        hasPendingCameraTransform = false;
    }

    private void NotifyBossDefeatAttempt()
    {
        string trackedIslandId = ResolvePendingBossIslandId();
        if (string.IsNullOrEmpty(trackedIslandId) || !IsFinalIslandForEnding(trackedIslandId))
        {
            return;
        }

        if (RecordFinalBossDefeatAttempt(trackedIslandId, finalBossDefeatsForBadEndingThreshold))
        {
            pendingBadEndingThresholdEventIslandId = trackedIslandId;
            Debug.LogWarning($"[GameStateManager] Bad ending threshold reached after repeated defeats on '{trackedIslandId}'.");
        }
    }

    private string ResolvePendingBossIslandId()
    {
        if (!string.IsNullOrEmpty(pendingBossIslandIdForDefeatTracking))
        {
            return IslandThemeRegistry.ResolveIslandId(pendingBossIslandIdForDefeatTracking);
        }

        if (IsBossEncounterId(PendingCombatEncounterId))
        {
            return IslandThemeRegistry.ResolveIslandId(PendingCombatIslandId);
        }

        return string.Empty;
    }

    private bool IsFinalIslandForEnding(string islandId)
    {
        return IslandProgressionManager.Instance != null
            && IslandProgressionManager.Instance.IsFinalIsland(islandId);
    }

    private void TrackBossVictoryThresholdProgress(string islandId, float preBossRestorationPercent)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            return;
        }

        if (IsAtOrNearBossUnlockThreshold(preBossRestorationPercent))
        {
            thresholdOnlyBossVictoryIslandIds.Add(scopedIslandId);
        }
        else
        {
            thresholdOnlyBossVictoryIslandIds.Remove(scopedIslandId);
            thresholdOnlyProceedIslandIds.Remove(scopedIslandId);
        }
    }

    private void ResolveFinalEndingAfterBossVictory()
    {
        if (endingTriggered)
        {
            return;
        }

        SetEndingBranch(ShouldResolveBadEnding() ? EndingBranch.Bad : EndingBranch.Good);

        if (!endingTriggered)
        {
            return;
        }

        OnFinalBossDefeated();
    }

    public void OnFinalBossDefeated()
    {
        if (!endingTriggered)
        {
            return;
        }

        Debug.Log($"[GameStateManager] Final boss defeated. Routing to {(resolvedEndingBranch == EndingBranch.Bad ? "bad" : "good")} ending cutscene.");

        if (resolvedEndingBranch == EndingBranch.Good)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.HandleEndingMusic();
            }

            NarrativeBeatDirector director = FindFirstObjectByType<NarrativeBeatDirector>();
            if (director != null && !IsNarrativeBeatCompleted(NarrativeBeatDirector.GoodEndingBeatIdPublic))
            {
                director.ForceShowGoodEndingBeatForDebug();
            }
        }
        else
        {
            NarrativeBeatDirector director = FindFirstObjectByType<NarrativeBeatDirector>();
            if (director != null && !IsNarrativeBeatCompleted(NarrativeBeatDirector.BadEndingBeatIdPublic))
            {
                director.ForceShowBadEndingBeatForDebug();
            }
        }
        PlayEndingSequence(resolvedEndingBranch);
    }

    private bool ShouldResolveBadEnding()
    {
        string finalIslandId = GetFinalProgressionIslandIdOrEmpty();

        if (!string.IsNullOrEmpty(finalIslandId)
            && GetFinalBossDefeatCount(finalIslandId) >= Mathf.Max(1, finalBossDefeatsForBadEndingThreshold))
        {
            return true;
        }

        return ShouldTriggerMinimumRestorationBadEnding();
    }

    private static string GetFinalProgressionIslandIdOrEmpty()
    {
        if (IslandProgressionManager.Instance == null)
        {
            return string.Empty;
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        if (progressionOrder == null || progressionOrder.Count == 0)
        {
            return string.Empty;
        }

        return IslandThemeRegistry.ResolveIslandId(progressionOrder[progressionOrder.Count - 1]);
    }

    private bool ShouldTriggerMinimumRestorationBadEnding()
    {
        switch (minimumRestorationBadEndingRuleMode)
        {
            case MinimumRestorationBadEndingRuleMode.BossDefeatedAtThreshold:
                return AreAllRequiredIslandsFlagged(thresholdOnlyBossVictoryIslandIds, true, false);
            case MinimumRestorationBadEndingRuleMode.ProceededAtThreshold:
                return AreAllRequiredIslandsFlagged(thresholdOnlyProceedIslandIds, false, false);
            default:
                return AreAllRequiredIslandsFlagged(thresholdOnlyBossVictoryIslandIds, true, true);
        }
    }

    private bool AreAllRequiredIslandsFlagged(HashSet<string> flaggedIslands, bool includeFinalIsland, bool onlyIslandsWithOptionalContent)
    {
        if (flaggedIslands == null)
        {
            return false;
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        int requiredIslandCount = 0;

        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            bool isFinalIsland = i == progressionOrder.Count - 1;
            if (!includeFinalIsland && isFinalIsland)
            {
                continue;
            }

            if (onlyIslandsWithOptionalContent && !HasOptionalPreBossRestorationAvailable(islandId))
            {
                continue;
            }

            requiredIslandCount++;
            if (!flaggedIslands.Contains(islandId))
            {
                return false;
            }
        }

        return requiredIslandCount > 0;
    }

    private bool HasOptionalPreBossRestorationAvailable(string islandId)
    {
        IslandConfig config = IslandThemeRegistry.GetConfig(islandId);
        if (config == null || config.encounters == null)
        {
            return false;
        }

        float nonBossContribution = 0f;
        for (int i = 0; i < config.encounters.Length; i++)
        {
            EncounterDefinition encounter = config.encounters[i];
            if (encounter == null || encounter.isBossEncounter || IsBossEncounterId(encounter.encounterId))
            {
                continue;
            }

            nonBossContribution += Mathf.Max(0f, encounter.restorationValue);
        }

        return nonBossContribution > (IslandRestorationTracker.DefaultBossUnlockThresholdPercent / 100f) + RestorationThresholdEpsilon;
    }

    private static bool IsAtOrNearBossUnlockThreshold(float restorationPercent)
    {
        return restorationPercent <= IslandRestorationTracker.DefaultBossUnlockThresholdPercent + RestorationThresholdEpsilon;
    }

    private int ResolveFinalBossDefeatThreshold(string islandId)
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            return BossEncounterGate.DefaultDefeatsForBadEnding;
        }

        BossEncounterGate[] gates = FindObjectsByType<BossEncounterGate>(FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            BossEncounterGate gate = gates[i];
            if (gate != null && gate.MatchesIslandForDefeatTracking(scopedIslandId))
            {
                return gate.DefeatsForBadEndingThreshold;
            }
        }

        return BossEncounterGate.DefaultDefeatsForBadEnding;
    }

    private void NotifyPendingBadEndingThresholdEventIfNeeded()
    {
        if (string.IsNullOrEmpty(pendingBadEndingThresholdEventIslandId))
        {
            return;
        }

        BossEncounterGate[] gates = FindObjectsByType<BossEncounterGate>(FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            BossEncounterGate gate = gates[i];
            if (gate == null || !gate.MatchesIslandForDefeatTracking(pendingBadEndingThresholdEventIslandId))
            {
                continue;
            }

            gate.OnBadEndingThresholdReached?.Invoke();
            pendingBadEndingThresholdEventIslandId = null;
            return;
        }

        Debug.LogWarning($"[GameStateManager] Unable to resolve pending bad ending event gate for island '{pendingBadEndingThresholdEventIslandId}'. Will retry after the next scene load.");
    }

    private bool TryResolveSafeExplorationSpawnPosition(out Vector3 spawnPosition)
    {
        if (TryGetPendingReturnPosition(out spawnPosition))
        {
            return true;
        }

        if (TryGetProgressionReturnPosition(out spawnPosition))
        {
            return true;
        }

        if (TryGetLastKnownReturnPosition(out spawnPosition))
        {
            return true;
        }

        IsometricPlayer existingPlayer = FindFirstObjectByType<IsometricPlayer>();
        if (existingPlayer != null)
        {
            spawnPosition = ResolveSafeReturnPosition(existingPlayer.transform.position);
            return true;
        }

        if (TryGetBoatDestinationSpawn(out Vector3 boatSpawn))
        {
            spawnPosition = ResolveSafeReturnPosition(boatSpawn);
            return true;
        }

        spawnPosition = ResolveSafeReturnPosition(defaultExplorationSpawnPosition);
        return true;
    }

    private bool TryGetPendingReturnPosition(out Vector3 returnPosition)
    {
        returnPosition = Vector3.zero;
        if (!hasPendingReturnPosition || !IsFiniteVector(pendingReturnPosition))
        {
            return false;
        }

        returnPosition = ResolveSafeReturnPosition(pendingReturnPosition);
        return true;
    }

    private bool TryGetProgressionReturnPosition(out Vector3 returnPosition)
    {
        returnPosition = Vector3.zero;
        if (IslandProgressionManager.Instance == null)
        {
            return false;
        }

        if (!IslandProgressionManager.Instance.TryGetIslandReturnPosition(
                IslandProgressionManager.Instance.ActiveIslandId,
                out Vector3 persistedReturnPosition)
            || !IsFiniteVector(persistedReturnPosition))
        {
            return false;
        }

        returnPosition = ResolveSafeReturnPosition(persistedReturnPosition);
        return true;
    }

    private bool TryGetLastKnownReturnPosition(out Vector3 returnPosition)
    {
        returnPosition = Vector3.zero;
        if (!hasLastKnownExplorationPlayerPosition || !IsFiniteVector(lastKnownExplorationPlayerPosition))
        {
            return false;
        }

        returnPosition = ResolveSafeReturnPosition(lastKnownExplorationPlayerPosition);
        return true;
    }

    private Vector3 ResolveSafeReturnPosition(Vector3 candidatePosition)
    {
        if (!IsFiniteVector(candidatePosition))
        {
            return defaultExplorationSpawnPosition;
        }

        float y = candidatePosition.y;
        if (Mathf.Abs(y) < 0.001f)
        {
            y = defaultExplorationSpawnPosition.y;
        }

        if (Mathf.Abs(y) < 0.001f)
        {
            y = 1f;
        }

        Vector3 basePosition = new Vector3(candidatePosition.x, y, candidatePosition.z);
        if (IsSafeExplorationReturnPosition(basePosition))
        {
            return basePosition;
        }

        float[] offsets = { 2f, 4f, 6f, 8f, 10f };
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            for (int j = 0; j < directions.Length; j++)
            {
                Vector3 testPosition = basePosition + directions[j] * offsets[i];
                if (IsSafeExplorationReturnPosition(testPosition))
                {
                    return testPosition;
                }
            }
        }

        return defaultExplorationSpawnPosition;
    }

    private bool IsSafeExplorationReturnPosition(Vector3 candidatePosition)
    {
        Collider[] overlaps = Physics.OverlapSphere(candidatePosition, 0.6f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            IsometricPlayer overlapPlayer = overlap.GetComponentInParent<IsometricPlayer>();
            if (player != null && overlapPlayer == player)
            {
                continue;
            }

            if (overlap.GetComponentInParent<EnemyTrigger>() != null
                || overlap.GetComponentInParent<OverworldEnemy>() != null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetBoatDestinationSpawn(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        IslandBoatInteractable boat = FindFirstObjectByType<IslandBoatInteractable>();
        if (boat == null)
        {
            return false;
        }

        if (!boat.TryGetSpawnPositionForIsland(IslandThemeRegistry.GetActiveIslandId(), out spawnPosition))
        {
            return false;
        }

        return IsFiniteVector(spawnPosition);
    }

    private static bool IsFiniteVector(Vector3 value)
    {
        return IsFiniteFloat(value.x) && IsFiniteFloat(value.y) && IsFiniteFloat(value.z);
    }

    private static bool IsFiniteFloat(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void SaveFinalBossDefeatState()
    {
        SaveWorldState();
    }

    private void LoadFinalBossDefeatStateIfAvailable()
    {
        if (!enablePersistentSaveData || finalBossDefeatCounts.Count > 0)
        {
            return;
        }

        if (!PlayerPrefs.HasKey(FinalBossDefeatsSaveKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(FinalBossDefeatsSaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        FinalBossDefeatSaveCollection payload = JsonUtility.FromJson<FinalBossDefeatSaveCollection>(json);
        if (payload == null || payload.entries == null)
        {
            return;
        }

        bool migratedLegacyData = false;
        for (int i = 0; i < payload.entries.Count; i++)
        {
            FinalBossDefeatSaveEntry entry = payload.entries[i];
            if (entry == null)
            {
                continue;
            }

            string scopedEntryIsland = IslandThemeRegistry.ResolveIslandId(entry.islandId);
            if (string.IsNullOrEmpty(scopedEntryIsland))
            {
                continue;
            }

            finalBossDefeatCounts[scopedEntryIsland] = Mathf.Max(0, entry.defeats);
            migratedLegacyData = true;
        }

        if (migratedLegacyData)
        {
            SaveWorldState();
            PlayerPrefs.DeleteKey(FinalBossDefeatsSaveKey);
            PlayerPrefs.Save();
        }
    }

    // ======================================================================
    //  Issue #294: title screen / pause menu entry points (additive API)
    // ======================================================================

    /// <summary>
    /// Full new-game reset: clears persisted save data, resets all runtime
    /// world state, restoration, island progression, hero progression, tide
    /// break progression, and gear inventory, then persists a fresh save.
    /// Mirrors DevCheatService.FullResetAllState but is self-contained so the
    /// title screen does not depend on dev-cheat services.
    /// </summary>
    public void ResetWorldStateForNewGame()
    {
        ClearPersistentWorldStateForDebug(true);
        ResetRuntimeWorldStateForDebug();

        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.ResetAllIslandsForDebug();
        }

        IslandProgressionManager progression = IslandProgressionManager.Instance;
        if (progression != null)
        {
            progression.ResetProgressionForDebug();
        }

        HeroProgressionManager heroProgression = HeroProgressionManager.Instance;
        if (heroProgression != null)
        {
            heroProgression.ResetProgressionForDebug();
        }

        TideBreakProgressionManager tideBreakProgression = TideBreakProgressionManager.Instance;
        if (tideBreakProgression != null)
        {
            tideBreakProgression.ResetProgressionForDebug();
        }

        PlayerGearInventory gearInventory = PlayerGearInventory.Instance;
        if (gearInventory != null)
        {
            gearInventory.ResetInventoryForDebug();
        }

        WorldSaveService saveService = WorldSaveService.Instance;
        if (saveService != null)
        {
            saveService.Clear();
        }

        PlayerPrefs.Save();
        HandleIslandTravelFlowReset();
        SaveWorldState();
        Debug.Log("[GameStateManager] New game reset complete.");
    }

    /// <summary>
    /// Loads the persisted world state, then transitions to the scene that
    /// matches the restored progression (hub when the active island is the
    /// hub, otherwise the main island scene). Used by Continue on the title
    /// screen and by the pause menu's Load action.
    /// </summary>
    public bool LoadWorldStateAndRestoreScene()
    {
        if (isTransitioning || isLoadingWorldState)
        {
            Debug.Log("[GameStateManager] Load ignored: a load or scene transition is already in progress.");
            return false;
        }

        if (!HasLoadableWorldState())
        {
            Debug.LogError("[GameStateManager] Load ignored: persisted world state is missing or invalid.");
            return false;
        }

        LoadWorldState();

        if (!Application.isPlaying)
        {
            // Edit-mode verification path: restore state only, no scene load.
            Debug.Log("[GameStateManager] Loaded world state (edit mode: scene transition skipped).");
            return true;
        }

        string targetScene = MainSceneName;
        if (IslandProgressionManager.Instance != null
            && IslandThemeRegistry.IsHubIslandId(IslandProgressionManager.Instance.ActiveIslandId))
        {
            targetScene = HubSceneName;
        }

        Debug.Log($"[GameStateManager] Restoring saved world state in scene '{targetScene}'.");
        StartCoroutine(TransitionToScene(targetScene, GameState.Exploration));
        return true;
    }

    /// <summary>
    /// Transitions back to the title screen (issue #294).
    /// </summary>
    public void ReturnToTitleScene()
    {
        if (isTransitioning || !Application.isPlaying)
        {
            return;
        }

        StartCoroutine(TransitionToScene(TitleSceneName, GameState.Exploration));
    }
}
