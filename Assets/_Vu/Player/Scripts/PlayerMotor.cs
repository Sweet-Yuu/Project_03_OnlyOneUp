using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("References")]
    public PlayerData playerData;
    [SerializeField] private Transform camTransform;

    private CharacterController controller;
    private StaminaSystem stamina;

    [Header("State Variables")]
    private Vector3 moveDirection;
    private float verticalVelocity;
    private bool isGrounded;

    [Header("Juice / Game Feel")]
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJumping;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<StaminaSystem>();

        if (controller == null)
            Debug.LogError($"[{nameof(PlayerMotor)}] Missing CharacterController on {name}.");

        if (playerData == null)
            Debug.LogError($"[{nameof(PlayerMotor)}] PlayerData is not assigned on {name}.");
        else if (playerData.airCap <= 0f)
            Debug.LogWarning($"[{nameof(PlayerMotor)}] {playerData.name}.airCap is {playerData.airCap} � " +
                              "air control will be effectively disabled. Set it to a positive value (e.g. 30).");

        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;
    }
    private void Update()
    {
        if (controller == null || playerData == null) return;

        float dt = Time.deltaTime;

        // 1. Ground check & coyote time
        isGrounded = controller.isGrounded;
        if (isGrounded)
        {
            coyoteTimer = playerData.coyoteTime;
            isJumping = false;

            if (verticalVelocity < 0)
                verticalVelocity = -2f; // keeps controller "stuck" to ground
        }
        else
        {
            coyoteTimer -= dt;
        }

        // 2. Cool down jump buffer
        if (jumpBufferTimer > 0)
            jumpBufferTimer -= dt;

        // 3. Trigger jump
        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            ExecuteJump();
        }

        // 4. Dynamic gravity
        ApplyGravity(dt);

        // 5. Apply movement
        Vector3 finalVelocity = moveDirection + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalVelocity * dt);
    }

    public void ProcessMove(Vector2 input)
    {
        if (camTransform == null || playerData == null) return;

        Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 rawDir = camTransform.right * input.x + camForward * input.y;
        Vector3 targetDir = rawDir.normalized;
        float inputStrength = Mathf.Clamp01(input.magnitude); // respects analog stick push amount

        if (isGrounded)
        {
            if (jumpBufferTimer <= 0)
            {
                ApplyFriction();
            }

            if (input.magnitude > 0.1f)
            {
                moveDirection = Vector3.MoveTowards(
                    moveDirection,
                    targetDir * playerData.speed * inputStrength,
                    playerData.accel * Time.deltaTime);
            }
        }
        else
        {
            CalculateAirStrafe(targetDir);
        }
    }

    private void CalculateAirStrafe(Vector3 wishDir)
    {
        if (wishDir.sqrMagnitude < 0.01f) return;

        float wishSpeed = playerData.maxAirSpeed;

        // Cap the *target* speed used for the addSpeed check only.
        float cappedWishSpeed = Mathf.Min(wishSpeed, playerData.airCap);

        // How much speed we already have *toward* the wish direction.
        float currentSpeedInWishDir = Vector3.Dot(moveDirection, wishDir);

        // How much more speed we're allowed to add in that direction this frame.
        float addSpeed = cappedWishSpeed - currentSpeedInWishDir;
        if (addSpeed <= 0f) return;

        float accelSpeed = playerData.airAccelerate * wishSpeed * Time.deltaTime;
        accelSpeed = Mathf.Min(accelSpeed, addSpeed);

        moveDirection += wishDir * accelSpeed;

        // Hard cap: prevents any strafe-timing trick from exceeding maxAirSpeed,
        // so continuous jumping never builds up extra speed.
        if (moveDirection.magnitude > wishSpeed)
        {
            moveDirection = moveDirection.normalized * wishSpeed;
        }
    }
    private void ApplyFriction()
    {
        float speed = moveDirection.magnitude;
        if (speed <= 0.01f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        float drop = speed * playerData.friction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0f);

        moveDirection *= (newSpeed / speed);
    }

    private void ApplyGravity(float dt)
    {
        float currentGravity = playerData.gravity;
        bool nearApex = Mathf.Abs(verticalVelocity) < playerData.apexThreshold;

        if (nearApex)
        {
            // Floaty hang time at the top of the arc � the single biggest
            // "game feel" win for a jump, used in Celeste, Hollow Knight, etc.
            currentGravity *= playerData.apexGravityMultiplier;
        }
        else if (verticalVelocity < 0)
        {
            currentGravity *= playerData.fallGravityMultiplier;
        }
        else if (verticalVelocity > 0 && !isJumping)
        {
            // Player released jump early -> cut the arc short (variable jump height).
            currentGravity *= playerData.jumpCutGravityMultiplier;
        }

        verticalVelocity += currentGravity * dt;

        // Terminal velocity clamp � critical for tall-drop games (Only Up-style):
        // without this, a long fall can build enough speed for CharacterController.Move()
        // to skip past thin platforms in a single frame (tunneling).
        if (verticalVelocity < -playerData.maxFallSpeed)
        {
            verticalVelocity = -playerData.maxFallSpeed;
        }
    }

    public void Jump()
    {
        jumpBufferTimer = playerData.jumpBufferTime;
    }

    public void OnJumpCanceled()
    {
        if (verticalVelocity > 0)
        {
            isJumping = false;
        }
    }

    private void ExecuteJump()
    {
        if (stamina != null && (stamina.IsExhausted || stamina.CurrentStamina < playerData.jumpStaminaCost))
        {
            jumpBufferTimer = 0;
            return;
        }

        verticalVelocity = Mathf.Sqrt(playerData.jumpHeight * -2f * playerData.gravity);

        stamina?.UseStamina(playerData.jumpStaminaCost);

        jumpBufferTimer = 0;
        coyoteTimer = 0;
        isJumping = true;
    }
}