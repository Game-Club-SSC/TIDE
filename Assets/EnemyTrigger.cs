using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class EnemyTrigger : MonoBehaviour
{
    [SerializeField] private EncounterConfig encounterConfig;

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

        GameStateManager.Instance.EnterCombatScene();

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }
}
