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

        CachePlayer();
        if (player != null)
        {
            player.canMove = false;
        }

        Debug.Log("Combat Started! Player movement locked.");
    }

    public void EndCombat()
    {
        currentState = GameState.Exploration;

        CachePlayer();
        if (player != null)
        {
            player.canMove = true;
        }
    }

    private void CachePlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<IsometricPlayer>();
        }
    }
}
