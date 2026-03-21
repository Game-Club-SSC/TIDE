using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStateManager : MonoBehaviour
{
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
    private bool isTransitioning;
    private bool hasHandledSceneLoad;
    private PartySetupUI partySetupUI;

    public PuzzleData PendingPuzzleData { get; set; }
    public int[,] PendingPuzzleLayout { get; set; }
    public Vector2Int PendingPuzzleSealedTile { get; set; }
    public EnemyComposition PendingEnemyComposition { get; set; }
    public string PendingPuzzleIslandId { get; set; }
    public IslandFlowController FlowController { get; set; }
    public bool HasActiveFlowController => FlowController != null && FlowController.IsActive;
    public IslandRestorationTracker RestorationTracker => IslandRestorationTracker.Instance;
    private bool isFlowControlledCombat;
    private bool deferredFlowFromCombat;
    private bool hasDeferredFlowFromCombatResult;
    private bool deferredFlowFromCombatResult;
    private bool deferredFlowFromPuzzle;
    private string pendingSolvedPuzzleBoxId;

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

        isFlowControlledCombat = HasActiveFlowController;
        hasDeferredFlowFromCombatResult = false;
        deferredFlowFromCombatResult = false;
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
        PendingPuzzleIslandId = string.Empty;
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
        PendingPuzzleIslandId = string.Empty;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void MarkPuzzleSolved()
    {
        PuzzleSolved = true;
    }

    public void OnCombatEnded(bool playerWon)
    {
        if (HasActiveFlowController)
        {
            hasDeferredFlowFromCombatResult = true;
            deferredFlowFromCombatResult = playerWon;
        }

        StartCoroutine(ReturnFromCombatAfterDelay(1.5f));
    }

    private IEnumerator ReturnFromCombatAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isTransitioning)
        {
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

        if (scene.name == MainSceneName)
        {
            if (PuzzleSolved && !string.IsNullOrEmpty(pendingSolvedPuzzleBoxId))
            {
                PuzzleBoxInteractable[] boxes = FindObjectsByType<PuzzleBoxInteractable>(FindObjectsSortMode.None);
                for (int i = 0; i < boxes.Length; i++)
                {
                    if (boxes[i].GetInstanceID().ToString() == pendingSolvedPuzzleBoxId)
                    {
                        boxes[i].MarkSolved();
                        break;
                    }
                }
                pendingSolvedPuzzleBoxId = null;
            }

            PuzzleSolved = false;

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

            hasPendingReturnPosition = false;

            if (!isTransitioning)
            {
                currentState = GameState.Exploration;
                SetPlayerMovementLocked(false);
            }

            EnsurePartySetupUI();

            if (isFlowControlledCombat)
            {
                deferredFlowFromCombat = true;
                isFlowControlledCombat = false;
            }
            else if (HasActiveFlowController && !string.IsNullOrEmpty(PendingPuzzleIslandId))
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

    private void EnsurePartySetupUI()
    {
        if (partySetupUI != null)
        {
            return;
        }

        partySetupUI = FindFirstObjectByType<PartySetupUI>();
        if (partySetupUI != null)
        {
            return;
        }

        GameObject partyUiObject = new GameObject("PartySetupUI");
        partyUiObject.transform.SetParent(transform, false);
        partySetupUI = partyUiObject.AddComponent<PartySetupUI>();
        Debug.Log("[GameStateManager] PartySetupUI created.");
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
}
