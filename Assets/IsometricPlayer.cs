using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    public bool canMove = true;

    private Rigidbody rb;
    private Vector3 inputVector;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; 
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
        // 3. Camera-relative movement math
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Flatten the camera vectors
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // 4. Apply Velocity (keeping Y velocity intact for normal gravity)
        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        Vector3 targetVelocity = moveDir * currentSpeed; 
        
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}
