using UnityEngine;
using UnityEngine.Events;

public class RestorationThresholdGate : MonoBehaviour
{
    [Header("Threshold")]
    [SerializeField] private string islandId = "island_lust";
    [SerializeField] [Range(0f, 100f)] private float thresholdPercent = 80f;

    [Header("Targets")]
    [SerializeField] private GameObject objectToEnable;
    [SerializeField] private GameObject objectToDisable;

    [Header("Events")]
    public UnityEvent OnThresholdReached;
    public UnityEvent OnThresholdLost;

    private bool thresholdMet;
    private IslandRestorationTracker tracker;

    public bool ThresholdMet => thresholdMet;

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
        bool nowMet = tracker.IsRestorationAtOrAbove(targetIsland, thresholdPercent);
        bool stateChanged = nowMet != thresholdMet;

        thresholdMet = nowMet;
        ApplyThresholdState();

        if (!invokeEvents || !stateChanged)
        {
            return;
        }

        if (thresholdMet)
        {
            Debug.Log($"[RestorationThresholdGate] Threshold {thresholdPercent}% reached on '{targetIsland}'. Gate activated.");
            OnThresholdReached?.Invoke();
        }
        else
        {
            Debug.Log($"[RestorationThresholdGate] Threshold {thresholdPercent}% lost on '{targetIsland}'. Gate deactivated.");
            OnThresholdLost?.Invoke();
        }
    }

    private void ApplyThresholdState()
    {
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(thresholdMet);
        }

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(!thresholdMet);
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
}
