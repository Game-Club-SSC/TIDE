using UnityEngine;

[DisallowMultipleComponent]
public class CombatDebugEntry : MonoBehaviour
{
    [SerializeField] private bool enableDebugCombatKey;
    [SerializeField] private KeyCode debugCombatKey = KeyCode.C;

    private void Update()
    {
        if (!enableDebugCombatKey)
        {
            return;
        }

        if (!Input.GetKeyDown(debugCombatKey))
        {
            return;
        }

        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanEnterCombatScene())
        {
            return;
        }

        GameStateManager.Instance.EnterCombatScene();
    }
}
