using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerformanceBudgetMonitor : MonoBehaviour
{
    public static PerformanceBudgetMonitor Instance { get; private set; }

    [Header("Budget")]
    [SerializeField, Min(1)] private int targetFrameRate = 60;
    [SerializeField, Min(1)] private int maxHeroes = 6;
    [SerializeField, Min(1)] private int maxEnemies = 6;
    [SerializeField, Min(0.001f)] private float maxFrameMs = 16.67f;
    [SerializeField, Min(0.1f)] private float sampleWindowSeconds = 5f;

    public int TargetFrameRate => targetFrameRate;
    public int MaxHeroes => maxHeroes;
    public int MaxEnemies => maxEnemies;
    public float MaxFrameMs => maxFrameMs;

    private readonly Queue<float> recentFrameMs = new Queue<float>();
    private float lastFrameTime;

    public float CurrentAverageFrameMs { get; private set; }
    public float MinObservedFrameMs { get; private set; } = float.MaxValue;
    public float MaxObservedFrameMs { get; private set; }

    public bool IsMeetingBudget => CurrentAverageFrameMs > 0f && CurrentAverageFrameMs <= maxFrameMs;

    public bool IsWithinUnitCap(int heroCount, int enemyCount)
    {
        return heroCount <= maxHeroes && enemyCount <= maxEnemies;
    }

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

    private void Update()
    {
        if (lastFrameTime > 0f)
        {
            float frameMs = Time.unscaledDeltaTime * 1000f;
            recentFrameMs.Enqueue(frameMs);
            if (recentFrameMs.Count > 600)
            {
                recentFrameMs.Dequeue();
            }
            RecomputeStats();
        }
        lastFrameTime = Time.unscaledTime;
    }

    public void RecordFrame(float frameMs)
    {
        if (frameMs <= 0f)
        {
            return;
        }

        recentFrameMs.Enqueue(frameMs);
        if (recentFrameMs.Count > 600)
        {
            recentFrameMs.Dequeue();
        }
        RecomputeStats();
    }

    public IReadOnlyCollection<float> RecentFrameMsSnapshot()
    {
        return recentFrameMs;
    }

    public void ResetForDebug()
    {
        recentFrameMs.Clear();
        CurrentAverageFrameMs = 0f;
        MinObservedFrameMs = float.MaxValue;
        MaxObservedFrameMs = 0f;
    }

    private void RecomputeStats()
    {
        if (recentFrameMs.Count == 0)
        {
            return;
        }

        float sum = 0f;
        float min = float.MaxValue;
        float max = 0f;
        foreach (float frame in recentFrameMs)
        {
            sum += frame;
            if (frame < min) min = frame;
            if (frame > max) max = frame;
        }
        CurrentAverageFrameMs = sum / recentFrameMs.Count;
        MinObservedFrameMs = min;
        MaxObservedFrameMs = max;
    }
}
