using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollowPolishBridge : MonoBehaviour
{
    [SerializeField] private Vector3 lookAhead;

    public void SetLookAhead(Vector3 value)
    {
        lookAhead = value;
    }

    public Vector3 GetLookAhead()
    {
        return lookAhead;
    }
}
