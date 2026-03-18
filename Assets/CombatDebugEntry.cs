using UnityEngine;

[DisallowMultipleComponent]
public class CombatDebugEntry : MonoBehaviour
{
    [SerializeField] private KeyCode debugCombatKey = KeyCode.C;

    private void Update()
    {
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
