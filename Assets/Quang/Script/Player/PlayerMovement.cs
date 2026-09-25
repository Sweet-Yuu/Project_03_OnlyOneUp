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
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        if(isGrounded && inputActions.Player.Jump.WasPressedThisFrame())
        {
            canJump = true;
        }
        movementInput = inputActions.Player.Move.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if(canJump)
        {
            Jump();
            canJump = false;
        }
        rb.linearVelocity = new Vector3(movementInput.x * speed, rb.linearVelocity.y, movementInput.y * speed);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }
}
