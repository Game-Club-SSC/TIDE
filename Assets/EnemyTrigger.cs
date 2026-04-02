using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class EnemyTrigger : MonoBehaviour
{
    [SerializeField] private EncounterConfig encounterConfig;
    [SerializeField] private string islandId = "island_lust";
    [SerializeField] private string encounterIdOverride = "";
    [SerializeField] private float restorationValue = 0.001f;

    private bool startupClearCheckComplete;

    private void Start()
    {
        if (TryDisableAsClearedEncounter())
        {
            return;
        }
    }

    private void Update()
    {
        if (startupClearCheckComplete)
        {
            return;
        }

        TryDisableAsClearedEncounter();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
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
            Transform playerTransform = ResolvePlayerTransform(other);
            Vector3 returnPosition = playerTransform != null ? playerTransform.position : other.transform.position;
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

    private bool TryDisableAsClearedEncounter()
    {
        string trackedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(trackedEncounterId))
        {
            startupClearCheckComplete = true;
            return false;
        }

        if (IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        startupClearCheckComplete = true;
        if (!ShouldDisableAsClearedEncounter())
        {
            return false;
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        Destroy(gameObject);
        return true;
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
        return IslandThemeRegistry.ResolveIslandId(islandId);
    }

    private static bool IsPlayerCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return collider.GetComponentInParent<IsometricPlayer>() != null;
    }

    private static Transform ResolvePlayerTransform(Collider collider)
    {
        if (collider == null)
        {
            return null;
        }

        IsometricPlayer player = collider.GetComponentInParent<IsometricPlayer>();
        if (player != null)
        {
            return player.transform;
        }

        return collider.transform;
    }
}
