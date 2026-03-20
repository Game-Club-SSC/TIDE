using UnityEngine;
using UnityEngine.Events;

public class BossEncounterGate : MonoBehaviour
{
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

    private bool isBossUnlocked;
    private IslandRestorationTracker tracker;

    public bool IsBossUnlocked => isBossUnlocked;

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
}
