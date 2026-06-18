using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DifficultyModeService : MonoBehaviour
{
    public enum Difficulty
    {
        Story,
        Standard,
        Hardcore
    }

    public static DifficultyModeService Instance { get; private set; }

    [SerializeField] private Difficulty currentDifficulty = Difficulty.Standard;

    public event Action<Difficulty> OnDifficultyChanged;

    public Difficulty CurrentDifficulty => currentDifficulty;
    public bool IsHardcore => currentDifficulty == Difficulty.Hardcore;
    public bool IsStory => currentDifficulty == Difficulty.Story;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        if (currentDifficulty == difficulty)
        {
            return;
        }

        currentDifficulty = difficulty;
        OnDifficultyChanged?.Invoke(difficulty);
        Debug.Log($"[DifficultyModeService] Difficulty set to {difficulty}.");
    }

    public float GetDamageMultiplierForPlayer()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Story: return 1.2f;
            case Difficulty.Standard: return 1.0f;
            case Difficulty.Hardcore: return 0.8f;
            default: return 1.0f;
        }
    }

    public float GetDamageMultiplierForEnemy()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Story: return 0.7f;
            case Difficulty.Standard: return 1.0f;
            case Difficulty.Hardcore: return 1.35f;
            default: return 1.0f;
        }
    }

    public float GetXpMultiplier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Story: return 0.8f;
            case Difficulty.Standard: return 1.0f;
            case Difficulty.Hardcore: return 1.5f;
            default: return 1.0f;
        }
    }

    public int GetCurrencyMultiplier()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Story: return 1;
            case Difficulty.Standard: return 1;
            case Difficulty.Hardcore: return 0;
        }
        return 1;
    }

    public bool AllowsFleeInCombat()
    {
        return currentDifficulty != Difficulty.Hardcore;
    }
}
