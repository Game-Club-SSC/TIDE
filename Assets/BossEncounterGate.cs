using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossEncounterGate : MonoBehaviour
{
    public const int DefaultDefeatsForBadEnding = 4;

    private const string DefeatsKeyPrefix = "TIDE_FINAL_BOSS_DEFEATS_";
    private static readonly bool EnablePersistentSaveData = true;
    private static readonly Dictionary<string, int> runtimeDefeatCounts = new Dictionary<string, int>();

    [Header("Threshold")]
    [SerializeField] private string islandId = "island_lust";
    [SerializeField] [Range(0f, 100f)] private float bossUnlockThresholdPercent = IslandRestorationTracker.DefaultBossUnlockThresholdPercent;

    [Header("Boss Targets")]
    [SerializeField] private GameObject bossVisuals;
    [SerializeField] private Collider bossInteractionCollider;
    [SerializeField] private EnemyTrigger bossTrigger;

    [Header("Events")]
    public UnityEvent OnBossUnlocked;
    public UnityEvent OnBossLocked;

    [Header("Bad Ending Rule")]
    [SerializeField] [Min(1)] private int defeatsForBadEnding = DefaultDefeatsForBadEnding;
    [SerializeField] private bool treatAsFinalBoss;
    public UnityEvent OnBadEndingThresholdReached;

    private bool isBossUnlocked;
    private IslandRestorationTracker tracker;
    private float nextTrackerBindAttemptTime;

    public bool IsBossUnlocked => isBossUnlocked;
    public bool IsTrackedFinalBoss => treatAsFinalBoss;
    public string TrackedIslandId => IslandThemeRegistry.ResolveIslandId(islandId);
    public int DefeatsForBadEndingThreshold => Mathf.Max(1, defeatsForBadEnding);

    public bool MatchesIslandForDefeatTracking(string candidateIslandId)
    {
        if (!treatAsFinalBoss)
        {
            return false;
        }

        string scopedSelfIsland = IslandThemeRegistry.ResolveIslandId(islandId);
        string scopedCandidate = IslandThemeRegistry.ResolveIslandId(candidateIslandId);
        return scopedSelfIsland == scopedCandidate;
    }

    public int GetDefeatCount()
    {
        if (!treatAsFinalBoss)
        {
            return 0;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            return gsm.GetFinalBossDefeatCount(TrackedIslandId);
        }

        if (!EnablePersistentSaveData)
        {
            string defeatsKey = GetDefeatsKey();
            if (runtimeDefeatCounts.TryGetValue(defeatsKey, out int runtimeDefeats))
            {
                return Mathf.Max(0, runtimeDefeats);
            }

            return 0;
        }

        return PlayerPrefs.GetInt(GetDefeatsKey(), 0);
    }

    public bool RecordBossDefeatAttempt(bool playerWon)
    {
        if (!treatAsFinalBoss || playerWon)
        {
            return false;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            bool reachedBadEndingFromGameState = gsm.RecordFinalBossDefeatAttempt(TrackedIslandId, DefeatsForBadEndingThreshold);
            if (reachedBadEndingFromGameState)
            {
                Debug.LogWarning($"[BossEncounterGate] Final boss defeat threshold reached ({gsm.GetFinalBossDefeatCount(TrackedIslandId)}/{DefeatsForBadEndingThreshold}).");
                OnBadEndingThresholdReached?.Invoke();
            }

            return reachedBadEndingFromGameState;
        }

        int defeats = Mathf.Max(0, GetDefeatCount()) + 1;
        if (EnablePersistentSaveData)
        {
            PlayerPrefs.SetInt(GetDefeatsKey(), defeats);
            PlayerPrefs.Save();
        }
        else
        {
            runtimeDefeatCounts[GetDefeatsKey()] = defeats;
        }

        bool reachedBadEnding = defeats >= DefeatsForBadEndingThreshold;
        if (reachedBadEnding)
        {
            Debug.LogWarning($"[BossEncounterGate] Final boss defeat threshold reached ({defeats}/{DefeatsForBadEndingThreshold}).");
            OnBadEndingThresholdReached?.Invoke();
        }

        return reachedBadEnding;
    }

    public void ResetBossDefeatAttempts()
    {
        if (!treatAsFinalBoss)
        {
            return;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.SetFinalBossDefeatCount(TrackedIslandId, 0);
            return;
        }

        if (EnablePersistentSaveData)
        {
            PlayerPrefs.DeleteKey(GetDefeatsKey());
            PlayerPrefs.Save();
        }
        else
        {
            runtimeDefeatCounts.Remove(GetDefeatsKey());
        }
    }

    public void SetDefeatCount(int defeats)
    {
        if (!treatAsFinalBoss)
        {
            return;
        }

        int sanitized = Mathf.Max(0, defeats);
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.SetFinalBossDefeatCount(TrackedIslandId, sanitized);
            return;
        }

        if (EnablePersistentSaveData)
        {
            PlayerPrefs.SetInt(GetDefeatsKey(), sanitized);
            PlayerPrefs.Save();
        }
        else
        {
            runtimeDefeatCounts[GetDefeatsKey()] = sanitized;
        }
    }

    private void OnEnable()
    {
        isBossUnlocked = false;
        ApplyBossState();
        nextTrackerBindAttemptTime = 0f;
        TryBindTracker();

        EvaluateState(false);
    }

    private void Update()
    {
        if (tracker == null && Time.unscaledTime >= nextTrackerBindAttemptTime)
        {
            TryBindTracker();
            EvaluateState(false);
            nextTrackerBindAttemptTime = Time.unscaledTime + 1f;
        }
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnRestorationChanged -= HandleRestorationChanged;
            tracker = null;
        }
    }

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        string targetIsland = IslandThemeRegistry.ResolveIslandId(islandId);
        if (changedIslandId != targetIsland)
        {
            return;
        }

        EvaluateState(true);
    }

    private void EvaluateState(bool invokeEvents)
    {
        if (tracker == null)
        {
            return;
        }

        string targetIsland = IslandThemeRegistry.ResolveIslandId(islandId);
        float percent = tracker.GetRestorationPercent(targetIsland);
        bool nowUnlocked = tracker.IsRestorationAtOrAbove(targetIsland, bossUnlockThresholdPercent);
        bool stateChanged = nowUnlocked != isBossUnlocked;

        isBossUnlocked = nowUnlocked;
        ApplyBossState();

        if (!invokeEvents || !stateChanged)
        {
            return;
        }

        if (isBossUnlocked)
        {
            Debug.Log($"[BossEncounterGate] Boss unlocked on '{targetIsland}' at {percent:F1}% (threshold: {bossUnlockThresholdPercent}%).");
            PlayBossIntroSting();
            OnBossUnlocked?.Invoke();
        }
        else
        {
            Debug.Log($"[BossEncounterGate] Boss locked on '{targetIsland}' at {percent:F1}% (threshold: {bossUnlockThresholdPercent}%).");
            OnBossLocked?.Invoke();
        }
    }

    private void TryBindTracker()
    {
        if (tracker != null)
        {
            return;
        }

        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnRestorationChanged += HandleRestorationChanged;
        }
    }

    private static void PlayBossIntroSting()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleBossIntro();
        }
    }

    private void ApplyBossState()
    {
        if (bossVisuals != null)
        {
            bossVisuals.SetActive(isBossUnlocked);
        }

        if (bossInteractionCollider != null)
        {
            bossInteractionCollider.enabled = isBossUnlocked;
        }

        if (bossTrigger != null)
        {
            bossTrigger.enabled = isBossUnlocked;
        }
    }

    private string GetDefeatsKey()
    {
        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(scopedIslandId))
        {
            scopedIslandId = string.IsNullOrEmpty(islandId) ? "unknown_island" : islandId;
        }

        return DefeatsKeyPrefix + scopedIslandId;
    }
}
