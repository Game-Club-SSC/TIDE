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

        GameStateManager.Instance.EnterCombat();
        Destroy(gameObject);
    }
}
