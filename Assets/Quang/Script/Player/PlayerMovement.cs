using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private bool isGrounded = true;
    private bool canJump = false;
    private Vector2 movementInput;
    public float speed = 5f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.1f;
    public Transform cameraTransform;
    private Vector3 moveDirection;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }
    
    void Update()
    {
        movementInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 camForward = cameraTransform.forward;
        Vector3 camright = cameraTransform.right;

        camForward.y = 0f;
        camright.y = 0f;
        camForward.Normalize();
        camright.Normalize();

        moveDirection = camForward * movementInput.y + camright * movementInput.x;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        if(isGrounded && inputActions.Player.Jump.WasPressedThisFrame())
        {
            canJump = true;
        }
    }

    void FixedUpdate()
    {
        if(canJump)
        {
            Jump();
            canJump = false;
        }
        rb.linearVelocity = new Vector3(moveDirection.x * speed, rb.linearVelocity.y, moveDirection.z * speed);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void Lift()
    {
        
    }
}
