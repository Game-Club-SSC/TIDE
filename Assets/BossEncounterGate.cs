using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossEncounterGate : MonoBehaviour
{
    private const string DefeatsKeyPrefix = "TIDE_FINAL_BOSS_DEFEATS_";
    private static readonly bool EnablePersistentSaveData = false;
    private static readonly Dictionary<string, int> runtimeDefeatCounts = new Dictionary<string, int>();

    [Header("Threshold")]
    [SerializeField] private string islandId = "";
    [SerializeField] [Range(0f, 100f)] private float bossUnlockThresholdPercent = 75f;

    [Header("Boss Targets")]
    [SerializeField] private GameObject bossVisuals;
    [SerializeField] private Collider bossInteractionCollider;
    [SerializeField] private EnemyTrigger bossTrigger;

    [Header("Events")]
    public UnityEvent OnBossUnlocked;
    public UnityEvent OnBossLocked;

    [Header("Bad Ending Rule")]
    [SerializeField] [Min(1)] private int defeatsForBadEnding = 3;
    [SerializeField] private bool treatAsFinalBoss;
    public UnityEvent OnBadEndingThresholdReached;

    private bool isBossUnlocked;
    private IslandRestorationTracker tracker;

    public bool IsBossUnlocked => isBossUnlocked;
    public bool IsTrackedFinalBoss => treatAsFinalBoss;
    public string TrackedIslandId => string.IsNullOrEmpty(islandId) ? "default" : islandId;

    public bool MatchesIslandForDefeatTracking(string candidateIslandId)
    {
        if (!treatAsFinalBoss)
        {
            return false;
        }

        string scopedSelfIsland = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        string scopedCandidate = string.IsNullOrEmpty(candidateIslandId) ? "default" : candidateIslandId;
        return scopedSelfIsland == scopedCandidate;
    }

    public int GetDefeatCount()
    {
        if (!treatAsFinalBoss)
        {
            return 0;
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

        bool reachedBadEnding = defeats >= Mathf.Max(1, defeatsForBadEnding);
        if (reachedBadEnding)
        {
            Debug.LogWarning($"[BossEncounterGate] Final boss defeat threshold reached ({defeats}/{defeatsForBadEnding}).");
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
        TryBindTracker();

        EvaluateState(false);
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryBindTracker();
            EvaluateState(false);
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
        if (!string.IsNullOrEmpty(islandId) && changedIslandId != islandId)
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

        string targetIsland = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        float percent = tracker.GetRestorationPercent(targetIsland);
        bool nowUnlocked = percent >= bossUnlockThresholdPercent;
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
        string scopedIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        return DefeatsKeyPrefix + scopedIslandId;
    }
}
