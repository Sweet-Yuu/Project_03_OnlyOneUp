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
    [Header("Carry distance - prototype")]
    [SerializeField, Min(0f)] private float carrySlack = 0.1f;
    [SerializeField, Min(0.1f)] private float correctionSpeed = 6f;

    private NetworkNest nest;
    private NetworkTestMovement partner;

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

        Vector3 requestedMove =
            new Vector3(input.x * speed, verticalSpeed, input.y * speed)
            * Time.deltaTime;

        Vector3 limitedMove = LimitCarryMovement(requestedMove);

        // Tránh tích lũy vận tốc lên/xuống khi bị giới hạn khoảng cách.
        if (Time.deltaTime > 0f)
        {
            bool upwardBlocked =
                verticalSpeed > 0f &&
                limitedMove.y < requestedMove.y - 0.0001f;

            bool downwardBlocked =
                verticalSpeed < 0f &&
                limitedMove.y > requestedMove.y + 0.0001f;

            if (upwardBlocked || downwardBlocked)
                verticalSpeed = limitedMove.y / Time.deltaTime;
        }

        CollisionFlags flags = controller.Move(limitedMove);

        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;

        if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
            verticalSpeed = -2f;
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

    private void FindCarryReferences()
    {
        if (nest == null)
            nest = FindFirstObjectByType<NetworkNest>();

        if (partner != null && partner.IsSpawned)
            return;

        partner = null;

        // Đọc các nhân vật đã được spawn trên máy hiện tại.
        // Không dùng ConnectedClientsList vì Client không quản lý danh sách đó.
        foreach (NetworkObject obj in
                NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            if (!obj.IsPlayerObject || obj.OwnerClientId == OwnerClientId)
                continue;

            if (obj.TryGetComponent(out NetworkTestMovement other))
            {
                partner = other;
                break;
            }
        }
    }

    private Vector3 LimitCarryMovement(Vector3 requestedMove)
    {
        FindCarryReferences();

        if (nest == null || !nest.IsSpawned ||
            partner == null || !partner.IsSpawned)
        {
            return requestedMove;
        }

        float gripDistance = nest.GripDistance;

        if (gripDistance <= 0.01f)
            return requestedMove;

        float minDistance = Mathf.Max(0.01f, gripDistance - carrySlack);
        float maxDistance = gripDistance + carrySlack;

        Vector3 currentPosition = transform.position;
        Vector3 partnerPosition = partner.transform.position;

        Vector3 wantedPosition = currentPosition + requestedMove;
        Vector3 offset = wantedPosition - partnerPosition;

        float distance = offset.magnitude;

        // Nằm trong khoảng cho phép thì di chuyển bình thường.
        if (distance >= minDistance && distance <= maxDistance)
            return requestedMove;

        Vector3 direction;

        if (distance > 0.001f)
        {
            direction = offset / distance;
        }
        else
        {
            // Nếu vị trí dự kiến trùng nhau, dùng hướng hiện tại.
            Vector3 currentOffset = currentPosition - partnerPosition;

            if (currentOffset.sqrMagnitude > 0.000001f)
            {
                direction = currentOffset.normalized;
            }
            else
            {
                // Hai nhân vật trùng vị trí: chọn hai hướng đối nhau.
                direction = OwnerClientId < partner.OwnerClientId
                    ? Vector3.left
                    : Vector3.right;
            }
        }

        // Quá gần → đưa ra khoảng cách tối thiểu.
        // Quá xa → đưa về khoảng cách tối đa.
        float targetDistance = Mathf.Clamp(
            distance, minDistance, maxDistance);

        Vector3 limitedPosition =
            partnerPosition + direction * targetDistance;

        Vector3 limitedMove = limitedPosition - currentPosition;

        float maxStep = Mathf.Max(
            requestedMove.magnitude,
            correctionSpeed * Time.deltaTime);

        return Vector3.ClampMagnitude(limitedMove, maxStep);
    }
}
