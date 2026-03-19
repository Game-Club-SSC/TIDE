using System;
using UnityEngine;

public class IslandRestorationTracker : MonoBehaviour
{
    public float RestorationProgress => restorationProgress;
    public bool IsIslandRestored => restorationProgress >= 1f;

    public event Action<float> OnRestorationChanged;
    public event Action OnIslandRestored;

    private float restorationProgress;

    public void CompleteEncounter(float contribution)
    {
        if (contribution <= 0f)
        {
            return;
        }

        restorationProgress = Mathf.Clamp01(restorationProgress + contribution);
        Debug.Log($"[IslandRestorationTracker] Encounter complete. Restoration: {restorationProgress * 100:F0}%");
        OnRestorationChanged?.Invoke(restorationProgress);

        if (restorationProgress >= 1f)
        {
            Debug.Log("[IslandRestorationTracker] Island fully restored!");
            OnIslandRestored?.Invoke();
        }
    }

    public void ResetTracker()
    {
        restorationProgress = 0f;
        OnRestorationChanged?.Invoke(restorationProgress);
    }
}
