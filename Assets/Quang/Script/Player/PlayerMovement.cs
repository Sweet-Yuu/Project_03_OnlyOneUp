using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public PlayerInputActions inputActions;
    [HideInInspector] public Vector2 movementInput;
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public bool isGrounded;

    [Header("Movement Stats")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.1f;
    public Transform cameraTransform;

    [Header("Player State")]
    public PlayerState currentState;
    public IdleState idleState;
    public WalkState walkState;
    public RunState runState;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
        idleState = new IdleState(this);
        walkState = new WalkState(this);
        runState = new RunState(this);

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
            
        }
    }

    void FixedUpdate()
    {
       
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
