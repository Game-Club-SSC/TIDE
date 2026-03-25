using UnityEngine;

[DisallowMultipleComponent]
public class CombatDebugEntry : MonoBehaviour
{
    [SerializeField] private bool enableDebugCombatKey;
    [SerializeField] private KeyCode debugCombatKey = KeyCode.F8;

    private void Update()
    {
#if UNITY_EDITOR
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
#endif
    }
}
