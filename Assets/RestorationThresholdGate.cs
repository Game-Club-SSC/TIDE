using UnityEngine;
using UnityEngine.Events;

public class RestorationThresholdGate : MonoBehaviour
{
    [Header("Threshold")]
    [SerializeField] private string islandId = "";
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
        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnRestorationChanged += HandleRestorationChanged;
        }

        EvaluateState();
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnRestorationChanged -= HandleRestorationChanged;
        }

        tracker = null;
    }

    private void Update()
    {
        if (tracker == null)
        {
            tracker = IslandRestorationTracker.Instance;
            if (tracker != null)
            {
                tracker.OnRestorationChanged += HandleRestorationChanged;
                EvaluateState();
            }
        }
    }

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        if (!string.IsNullOrEmpty(islandId) && changedIslandId != islandId)
        {
            return;
        }

        EvaluateState();
    }

    private void EvaluateState()
    {
        if (tracker == null)
        {
            return;
        }

        string targetIsland = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        float percent = tracker.GetRestorationPercent(targetIsland);
        bool nowMet = percent >= thresholdPercent;

        if (nowMet == thresholdMet)
        {
            return;
        }

        thresholdMet = nowMet;

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(thresholdMet);
        }

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(!thresholdMet);
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
}
