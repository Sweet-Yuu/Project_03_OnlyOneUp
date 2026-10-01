using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(OwnerNetworkTransform))]
public class NetworkTestMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    private const float Gravity = -20f;
    private CharacterController controller;
    private Camera localCamera;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // This test scene's ground is at y=0.95493. Separate the two players.
        transform.position = new Vector3(IsHost ? -2f : 2f, 2.1f, 0f);
        controller.enabled = true;
        localCamera = Camera.main;
        if (localCamera != null)
        {
            savedCameraPosition = localCamera.transform.position;
            savedCameraRotation = localCamera.transform.rotation;
        }
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        Vector2 input = Vector2.zero;
        Keyboard keys = Keyboard.current;
        bool jump = false;
        if (Application.isFocused && keys != null)
        {
            input.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f)
                - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f);
            input.y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f)
                - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f);
            jump = keys.spaceKey.wasPressedThisFrame;
        }

        if (controller.isGrounded)
        {
            verticalSpeed = -2f;
            if (jump) verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Gravity);
        }

        input = Vector2.ClampMagnitude(input, 1f);
        verticalSpeed += Gravity * Time.deltaTime;
        controller.Move(new Vector3(input.x * speed, verticalSpeed, input.y * speed) * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || localCamera == null) return;
        localCamera.transform.position = transform.position + new Vector3(0f, 5f, -8f);
        localCamera.transform.LookAt(transform.position + Vector3.up * 0.5f);
    }

    public override void OnNetworkDespawn()
    {
        controller.enabled = false;
        if (localCamera == null) return;
        localCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        localCamera = null;
    }
}
