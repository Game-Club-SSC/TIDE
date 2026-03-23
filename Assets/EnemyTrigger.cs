using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class EnemyTrigger : MonoBehaviour
{
    [SerializeField] private EncounterConfig encounterConfig;
    [SerializeField] private string islandId = "default";
    [SerializeField] private string encounterIdOverride = "";
    [SerializeField] private float restorationValue = 0.001f;

    private void Start()
    {
        if (ShouldDisableAsClearedEncounter())
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanEnterCombatScene())
        {
            return;
        }

        if (encounterConfig != null)
        {
            GameStateManager.Instance.PendingEnemyComposition = EnemyComposition.FromEncounterConfig(encounterConfig);
        }

        if (GameStateManager.Instance.HasActiveFlowController)
        {
            Collider flowTriggerCollider = GetComponent<Collider>();
            if (flowTriggerCollider != null)
            {
                flowTriggerCollider.enabled = false;
            }

            Destroy(gameObject, 0.1f);
            return;
        }

        string trackedEncounterId = ResolveTrackingEncounterId();
        if (!string.IsNullOrEmpty(trackedEncounterId))
        {
            Vector3 returnPosition = other.transform.position;
            GameStateManager.Instance.EnterCombatSceneFromExploration(
                ResolveTrackingIslandId(),
                trackedEncounterId,
                Mathf.Max(0.001f, restorationValue),
                returnPosition);
        }
        else
        {
            GameStateManager.Instance.EnterCombatScene();
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }

    private bool ShouldDisableAsClearedEncounter()
    {
        string trackedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(trackedEncounterId))
        {
            return false;
        }

        if (IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        return IslandRestorationTracker.Instance.HasClearedEncounter(ResolveTrackingIslandId(), trackedEncounterId);
    }

    private string ResolveTrackingEncounterId()
    {
        if (!string.IsNullOrEmpty(encounterIdOverride))
        {
            return encounterIdOverride;
        }

        if (encounterConfig != null && !string.IsNullOrEmpty(encounterConfig.encounterId))
        {
            return encounterConfig.encounterId;
        }

        return string.Empty;
    }

    private string ResolveTrackingIslandId()
    {
        return string.IsNullOrEmpty(islandId) ? "default" : islandId;
    }
}
