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
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
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

    public bool CanEnterPuzzle()
    {
        return !PuzzleSolved && !isTransitioning;
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

    public void EnterPuzzleScene(Vector3 returnPosition)
    {
        if (!CanEnterPuzzle())
        {
            return;
        }

        pendingReturnPosition = returnPosition;
        hasPendingReturnPosition = true;
        StartCoroutine(TransitionToScene(PuzzleSceneName, GameState.Puzzle));
    }

    public void MarkPuzzleSolved()
    {
        PuzzleSolved = true;
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
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        CachePlayer();

        if (scene.name == MainSceneName)
        {
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
}
