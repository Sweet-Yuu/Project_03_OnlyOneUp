using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;

    private PlayerMotor motor;
    private PlayerLook look;

    private Vector2 moveInput;

    private void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;

        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

        if (motor == null)
            Debug.LogError($"[{nameof(InputManager)}] Missing PlayerMotor on {name}.");

        if (look == null)
            Debug.LogError($"[{nameof(InputManager)}] Missing PlayerLook on {name}.");
    }

    private void Update()
    {
        if (motor == null) return;

        moveInput = onFoot.Movement.ReadValue<Vector2>();
        motor.ProcessMove(moveInput);
    }

    private void LateUpdate()
    {
        if (look == null) return;

        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();

        onFoot.Jump.started += OnJumpStarted;
        onFoot.Jump.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        onFoot.Jump.started -= OnJumpStarted;
        onFoot.Jump.canceled -= OnJumpCanceled;

        onFoot.Disable();
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx) => motor.Jump();
    private void OnJumpCanceled(InputAction.CallbackContext ctx) => motor.OnJumpCanceled();
}