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

    [Header("Auto Quality")]
    [SerializeField] private bool enableAutoQualityDownscale = true;
    [SerializeField, Min(1)] private int minQualityLevel;
    [SerializeField, Min(0.1f)] private float qualityCheckInterval = 2f;
    [SerializeField, Min(1)] private int qualityDropThresholdFrames = 3;
    [SerializeField, Min(1)] private int qualityRestoreThresholdFrames = 60;

    public int TargetFrameRate => targetFrameRate;
    public int MaxHeroes => maxHeroes;
    public int MaxEnemies => maxEnemies;
    public float MaxFrameMs => maxFrameMs;

    private readonly Queue<float> recentFrameMs = new Queue<float>();
    private float lastFrameTime;
    private int maxSampleCount;
    private float qualityCheckTimer;
    private int overBudgetStreak;
    private int underBudgetStreak;

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
        RecalculateMaxSampleCount();
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
            while (recentFrameMs.Count > maxSampleCount)
            {
                recentFrameMs.Dequeue();
            }
            RecomputeStats();
        }
        lastFrameTime = Time.unscaledTime;

        if (enableAutoQualityDownscale)
        {
            UpdateAutoQuality();
        }
    }

    public void RecordFrame(float frameMs)
    {
        if (frameMs <= 0f)
        {
            return;
        }

        recentFrameMs.Enqueue(frameMs);
        while (recentFrameMs.Count > maxSampleCount)
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
        overBudgetStreak = 0;
        underBudgetStreak = 0;
    }

    private void RecalculateMaxSampleCount()
    {
        float fps = targetFrameRate > 0 ? targetFrameRate : 60f;
        maxSampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleWindowSeconds * fps));
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

    private void UpdateAutoQuality()
    {
        qualityCheckTimer -= Time.unscaledDeltaTime;
        if (qualityCheckTimer > 0f)
        {
            return;
        }

        qualityCheckTimer = qualityCheckInterval;

        if (!IsMeetingBudget)
        {
            underBudgetStreak = 0;
            overBudgetStreak++;
            if (overBudgetStreak >= qualityDropThresholdFrames)
            {
                int current = QualitySettings.GetQualityLevel();
                if (current > minQualityLevel)
                {
                    QualitySettings.SetQualityLevel(current - 1, true);
                    Debug.Log($"[PerformanceBudgetMonitor] Quality downgraded to {QualitySettings.names[QualitySettings.GetQualityLevel()]} (avg {CurrentAverageFrameMs:F1}ms > {maxFrameMs:F1}ms).");
                }
                overBudgetStreak = 0;
            }
        }
        else
        {
            overBudgetStreak = 0;
            underBudgetStreak++;
            int maxLevel = QualitySettings.names.Length - 1;
            if (underBudgetStreak >= qualityRestoreThresholdFrames)
            {
                int current = QualitySettings.GetQualityLevel();
                if (current < maxLevel)
                {
                    QualitySettings.SetQualityLevel(current + 1, true);
                    Debug.Log($"[PerformanceBudgetMonitor] Quality upgraded to {QualitySettings.names[QualitySettings.GetQualityLevel()]} (avg {CurrentAverageFrameMs:F1}ms <= {maxFrameMs:F1}ms).");
                }
                underBudgetStreak = 0;
            }
        }
    }
}
