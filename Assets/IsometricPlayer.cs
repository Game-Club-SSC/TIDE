using UnityEngine;
using UnityEngine.UI; // We need this to talk to the UI STANIMA Bar!

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;

    [Header("Stanima System")]
    [SerializeField] private float maxStanima = 100f;
    [SerializeField] private float stanimaDrainRate = 20f;
    [SerializeField] private float stanimaRegenRate = 15f;
    [SerializeField] private Image stanimaBarFill; // The UI bar we will link in Unity

    private Rigidbody rb;
    private Vector3 inputVector;
    
    private float currentSpeed;
    private float currentStanima;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; 
        
        currentSpeed = walkSpeed;
        currentStanima = maxStanima; // Start with a full tank
    }

    void Update()
    {
        // 1. Gather WASD Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputVector = new Vector3(h, 0f, v).normalized;

        // 2. Process the Sprint and STANIMA math
        HandleStanima();
    }

    void HandleStanima()
    {
        // If holding Left Shift, actually moving, AND we have Stanima left...
        if (Input.GetKey(KeyCode.LeftShift) && inputVector.magnitude > 0.1f && currentStanima > 0)
        {
            currentSpeed = sprintSpeed;
            currentStanima -= stanimaDrainRate * Time.deltaTime; // Drain the bar
        }
        else
        {
            currentSpeed = walkSpeed;
            // Regenerate Stanima if we are not sprinting
            if (currentStanima < maxStanima)
            {
                currentStanima += stanimaRegenRate * Time.deltaTime; // Fill the bar
            }
        }

        // Clamp Stanima so it never drops below 0 or goes above the maximum
        currentStanima = Mathf.Clamp(currentStanima, 0f, maxStanima);

        // If we linked a UI bar in the Inspector, update its visual fill amount
        if (stanimaBarFill != null)
        {
            stanimaBarFill.fillAmount = currentStanima / maxStanima;
        }
    }

    void FixedUpdate()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * inputVector.z + right * inputVector.x;
        
        // Notice we are using currentSpeed here now, not moveSpeed!
        Vector3 targetVelocity = moveDir * currentSpeed; 
        
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}
