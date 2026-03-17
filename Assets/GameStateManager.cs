using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Exploration,
        Combat,
        Puzzle
    }

    public static GameStateManager Instance { get; private set; }

    public GameState currentState = GameState.Exploration;

    private IsometricPlayer player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void EnterCombat()
    {
        currentState = GameState.Combat;
        SetPlayerMovementLocked(true);

        Debug.Log("Combat Started! Player movement locked.");
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
        Debug.Log("Puzzle Started! Player movement locked.");
    }

    public void ExitPuzzle()
    {
        currentState = GameState.Exploration;
        SetPlayerMovementLocked(false);
    }

    private void CachePlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<IsometricPlayer>();
        }
    }

    private void SetPlayerMovementLocked(bool isLocked)
    {
        CachePlayer();
        if (player != null)
        {
            player.canMove = !isLocked;
        }
    }
}
