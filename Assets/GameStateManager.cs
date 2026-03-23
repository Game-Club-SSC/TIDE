using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStateManager : MonoBehaviour
{
    private sealed class PuzzleRuntimeState
    {
        public readonly int[] tileValues = new int[9];
        public bool hasGrid;
        public bool solved;
    }

    public enum GameState
    {
        Exploration,
        Combat,
        Puzzle,
        Transition
    }

    public const string MainSceneName = "level_1";
    public const string PuzzleSceneName = "PuzzleScene";
    public const string CombatSceneName = "CombatScene";

    public static GameStateManager Instance { get; private set; }

    public GameState currentState = GameState.Exploration;
    public bool PuzzleSolved { get; private set; }
    public bool IsTransitioning => isTransitioning;

    private const float FadeDuration = 0.2f;

    private CanvasGroup fadeCanvasGroup;
    private IsometricPlayer player;
    private Vector3 pendingReturnPosition;
    private bool hasPendingReturnPosition;
    private Vector3 pendingCameraPosition;
    private Quaternion pendingCameraRotation;
    private bool hasPendingCameraTransform;
    private bool isTransitioning;
    private bool hasHandledSceneLoad;

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
    private bool isFlowControlledCombat;
    private bool deferredFlowFromCombat;
    private bool hasDeferredFlowFromCombatResult;
    private bool deferredFlowFromCombatResult;
    private bool deferredFlowFromPuzzle;
    private string pendingSolvedPuzzleBoxId;
    private bool returnToPuzzleAfterCombat;
    private bool hasPendingCombatReturnPosition;
    private Coroutine cameraSnapRoutine;
    private readonly Dictionary<string, PuzzleRuntimeState> puzzleRuntimeStates = new Dictionary<string, PuzzleRuntimeState>();

    public float GetIslandRestorationPercent(string islandId)
    {
        if (IslandRestorationTracker.Instance == null)
        {
            return 0f;
        }

        return IslandRestorationTracker.Instance.GetRestorationPercent(islandId);
    }

    public IslandRestorationState GetIslandRestorationState(string islandId)
    {
        if (IslandRestorationTracker.Instance == null)
        {
            return new IslandRestorationState(islandId);
        }

        return IslandRestorationTracker.Instance.GetRestorationState(islandId);
    }

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
        EnsureRestorationTracker();
        EnsureProgressionManager();
        EnsureFadeCanvas();
    }

    private void Start()
    {
        if (!hasHandledSceneLoad)
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        Instance = null;
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
        CaptureExplorationReturnPosition();
        BeginCombatTransition();
    }

    public void EnterCombatSceneFromExploration(string islandId, string encounterId, float restorationValue, Vector3 returnPosition)
    {
        if (!CanEnterCombatScene())
        {
            return;
        }

        returnToPuzzleAfterCombat = false;
        PendingCombatIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        PendingCombatEncounterId = encounterId;
        PendingCombatRestorationValue = Mathf.Max(0.001f, restorationValue);
        pendingReturnPosition = returnPosition;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = true;
        CaptureExplorationCameraTransform();
        BeginCombatTransition();
    }

    public void EnterCombatSceneFromPuzzle(string islandId, string encounterId, float restorationValue = 0.001f)
    {
        if (!CanEnterCombatFromPuzzle())
        {
            return;
        }

        returnToPuzzleAfterCombat = true;
        PendingCombatIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        PendingCombatEncounterId = encounterId;
        PendingCombatRestorationValue = Mathf.Max(0.001f, restorationValue);
        hasPendingCombatReturnPosition = false;
        BeginCombatTransition();
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

        pendingReturnPosition = returnPosition;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        pendingSolvedPuzzleBoxId = puzzleBoxId;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void EnterPuzzleSceneForced(Vector3 returnPosition)
    {
        if (isTransitioning)
        {
            return;
        }

        pendingReturnPosition = returnPosition;
        hasPendingReturnPosition = true;
        hasPendingCombatReturnPosition = false;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void SavePuzzleRuntimeState(string puzzleBoxId, int[,] grid, bool solved)
    {
        if (string.IsNullOrEmpty(puzzleBoxId) || grid == null || grid.GetLength(0) != 3 || grid.GetLength(1) != 3)
        {
            return;
        }

        if (!puzzleRuntimeStates.TryGetValue(puzzleBoxId, out PuzzleRuntimeState state))
        {
            state = new PuzzleRuntimeState();
            puzzleRuntimeStates[puzzleBoxId] = state;
        }

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                state.tileValues[row * 3 + col] = Mathf.Clamp(grid[row, col], 1, 10);
            }
        }

        state.hasGrid = true;
        state.solved = state.solved || solved;
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

        grid = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = state.tileValues[row * 3 + col];
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

        state.solved = true;
    }

    public void CompletePuzzleInExploration(string puzzleBoxId, string islandId, string encounterId, float restorationValue)
    {
        MarkPuzzleBoxSolved(puzzleBoxId);

        string scopedIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        string scopedEncounterId = string.IsNullOrEmpty(encounterId) ? "__puzzle_complete__" : encounterId;
        float contribution = restorationValue > 0f ? restorationValue : 0.2f;

        if (IslandRestorationTracker.Instance != null)
        {
            IslandRestorationTracker.Instance.RecordEncounterCompletion(
                scopedIslandId,
                scopedEncounterId,
                EncounterType.Puzzle,
                contribution);
            Debug.Log($"[GameStateManager] Recorded puzzle completion for island '{scopedIslandId}', encounter '{scopedEncounterId}'.");
        }

        OnPuzzleCompleted?.Invoke();

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

    public void OnCombatEnded(bool playerWon)
    {
        if (playerWon && IslandRestorationTracker.Instance != null && !string.IsNullOrEmpty(PendingCombatEncounterId))
        {
            string islandId = string.IsNullOrEmpty(PendingCombatIslandId) ? "default" : PendingCombatIslandId;
            float contribution = PendingCombatRestorationValue > 0f ? PendingCombatRestorationValue : 0.001f;
            IslandRestorationTracker.Instance.RecordEncounterCompletion(
                islandId,
                PendingCombatEncounterId,
                EncounterType.Combat,
                contribution);
            Debug.Log($"[GameStateManager] Recorded combat completion for island '{islandId}', encounter '{PendingCombatEncounterId}'.");
        }

        if (playerWon)
        {
            GrantBattleRewards();
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
        CachePlayer();
        EnsureRestorationTracker();

        if (scene.name == MainSceneName)
        {
            bool returnedFromPuzzleScene = hasPendingReturnPosition && !hasPendingCombatReturnPosition;

            EnsureMainSceneRuntimeComponents();
            ApplySolvedPuzzleBoxesInScene();

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

                // Record restoration if island ID provided
                if (IslandRestorationTracker.Instance != null)
                {
                    string islandId = PendingPuzzleIslandId;
                    if (string.IsNullOrEmpty(islandId))
                    {
                        islandId = "default";
                    }

                    string encounterId = PendingPuzzleEncounterId;
                    if (string.IsNullOrEmpty(encounterId))
                    {
                        encounterId = "__puzzle_complete__";
                    }

                    float contribution = PendingPuzzleRestorationValue > 0f ? PendingPuzzleRestorationValue : 0.2f;

                    IslandRestorationTracker.Instance.RecordEncounterCompletion(
                        islandId,
                        encounterId,
                        EncounterType.Puzzle,
                        contribution);
                    Debug.Log($"[GameStateManager] Recorded puzzle completion for island '{islandId}', encounter '{encounterId}'.");
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

            if (hasPendingReturnPosition && player != null)
            {
                player.transform.position = pendingReturnPosition;
                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                }
            }

            ApplyPendingCameraTransformIfAvailable();

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
        else if (scene.name == PuzzleSceneName)
        {
            player = null;

            if (!isTransitioning)
            {
                currentState = GameState.Puzzle;
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

        pendingReturnPosition = player.transform.position;
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
        if (HeroProgressionManager.Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("HeroProgressionManager");
        managerObject.AddComponent<HeroProgressionManager>();
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
}
