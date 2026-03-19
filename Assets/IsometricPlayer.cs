using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    public bool canMove = true;

    private Rigidbody rb;
    private Camera cachedMainCamera;
    private Vector3 inputVector;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cachedMainCamera = Camera.main;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        if (!canMove)
        {
            inputVector = Vector3.zero;
            currentSpeed = 0f;
            return;
        }

        // 1. Gather WASD Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputVector = new Vector3(h, 0f, v).normalized;

        // 2. Sprinting Check
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        // 3. Camera-relative movement math
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        Camera activeCamera = cachedMainCamera;
        Vector3 forward = activeCamera != null ? activeCamera.transform.forward : Vector3.forward;
        Vector3 right = activeCamera != null ? activeCamera.transform.right : Vector3.right;

        // Flatten the camera vectors
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // 4. Apply Velocity (keeping Y velocity intact for normal gravity)
        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        Vector3 targetVelocity = moveDir * currentSpeed; 
        
#if UNITY_2020_3_OR_NEWER
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
#else
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
#endif
    }
}
