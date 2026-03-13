using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 inputVector;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Stop the cube from rolling around like a dice
        rb.freezeRotation = true; 
    }

    void Update()
    {
        // 1. Gather WASD Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputVector = new Vector3(h, 0f, v).normalized;
    }

    void FixedUpdate()
    {
        // 2. Adjust movement to match the Isometric Camera angle
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Flatten the camera vectors so we don't accidentally walk into the sky/ground
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // 3. Calculate final direction and move the Rigidbody
        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
    }
}