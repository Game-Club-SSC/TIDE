using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyTrigger : MonoBehaviour
{
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

        if (GameStateManager.Instance.HasActiveFlowController)
        {
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
