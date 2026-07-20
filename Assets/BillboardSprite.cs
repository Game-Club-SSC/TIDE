using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class BillboardSprite : MonoBehaviour
{
    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Camera targetCamera;

    [Header("Y-Axis Lock")]
    [SerializeField] private bool keepUpright = true;

    private Transform cachedTransform;
    private SpriteRenderer spriteRenderer;

    public bool FaceCamera { get => faceCamera; set => faceCamera = value; }
    public bool LockYAxis { get => lockYAxis; set => lockYAxis = value; }
    public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }
    public Camera TargetCamera { get => targetCamera; set => targetCamera = value; }

    private void Awake()
    {
        cachedTransform = transform;
    }

    private void OnEnable()
    {
        cachedTransform = transform;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (!faceCamera)
        {
            return;
        }

        if (cachedTransform == null)
        {
            cachedTransform = transform;
        }

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 camForward = cam.transform.forward;
        if (lockYAxis)
        {
            camForward.y = 0f;
            if (camForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }
            camForward.Normalize();
        }

        Quaternion target = Quaternion.LookRotation(camForward, Vector3.up);
        if (rotationOffset != Vector3.zero)
        {
            target *= Quaternion.Euler(rotationOffset);
        }

        if (keepUpright)
        {
            Vector3 euler = target.eulerAngles;
            euler.z = 0f;
            target = Quaternion.Euler(euler);
        }

        cachedTransform.rotation = target;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        return spriteRenderer;
    }

    public void SetSortingOrder(int order)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
}
