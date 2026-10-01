using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Animator animator;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public PlayerInputActions inputActions;
    [HideInInspector] public Vector2 movementInput;
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public bool isSprinting;

    [Header("Movement Stats")]
    public float speed = 5f;
    public float runSpeed = 9f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.1f;
    public float rotationSpeed = 10f;
    public Transform cameraTransform;

    public PlayerState currentState;
    public IdleState idleState;
    public WalkState walkState;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
        idleState = new IdleState(this);
        walkState = new WalkState(this);
    }

    void Start()
    {
        ChangeState(idleState);
    }

    void OnEnable() => inputActions.Enable();

    void OnDisable() => inputActions.Disable();
    
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
        if(currentState != null) currentState.Execute();
    }

    void FixedUpdate()
    {
       
        if(currentState != null) currentState.FixedUpdate();
    }

    public void ChangeState(PlayerState newState)
    {
        if(currentState == newState) return;
        if(currentState != null) currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
