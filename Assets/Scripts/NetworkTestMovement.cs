using Unity.Netcode;
using UnityEngine;

// Network adapter/composition root. Serialized fields and public API remain here
// so existing scene/prefab data and NetworkNest references remain compatible.
[RequireComponent(typeof(CharacterController), typeof(OwnerNetworkTransform))]
public class NetworkTestMovement : NetworkBehaviour, IPlayerMovementConstraint
{
    [Header("Online animation")]
    [SerializeField] private Animator onlineAnimator;
    [SerializeField, Min(0f)] private float animationFade = 0.1f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [Tooltip("Higher values reach the same jump height faster, with less time near the apex.")]
    [SerializeField, Range(1f, 6f)] private float riseGravityMultiplier = 3f;
    [Tooltip("Gravity multiplier while descending. Higher values give a faster, heavier landing without increasing jump height.")]
    [SerializeField, Range(1f, 6f)] private float fallGravityMultiplier = 3f;
    [Header("Local camera")]
    [SerializeField] private Transform cameraTrackingTarget;
    [Header("Carry distance - prototype")]
    [SerializeField, Min(0f)] private float carrySlack = 0.1f;
    [SerializeField, Min(0.1f)] private float correctionSpeed = 6f;
    [SerializeField] private Transform carryAnchor;
    [Header("Duck neck reach")]
    [SerializeField, Range(1f, 3f)] private float neckStretchLimit = 1.8f;
    [SerializeField, Range(0.5f, 1.2f)] private float minimumDuckDistanceRatio = 0.95f;
    public float NeckStretchLimit => Mathf.Clamp(neckStretchLimit, 1f, 3f);
    [Header("Duck body lean")]
    [SerializeField, Range(0f, 40f)] private float maximumChestLean = 20f;
    [SerializeField, Range(0.2f, 0.95f)] private float chestLeanStart = 0.65f;
    [SerializeField, Min(0.1f)] private float chestLeanSpeed = 6f;
    public float MaximumChestLean => maximumChestLean;
    public float ChestLeanStart => chestLeanStart;
    public float ChestLeanSpeed => chestLeanSpeed;
    [Header("Duck wing spread")]
    [SerializeField, Range(0f, 100f)] private float maximumWingSpread = 65f;
    [Tooltip("Fraction of available extra neck stretch at which the wings reach full spread. Spreading starts as soon as the neck stretches while movement is held.")]
    [SerializeField, Range(0.2f, 0.95f)] private float wingSpreadStart = 0.65f;
    [SerializeField, Min(0.1f)] private float wingSpreadSpeed = 5f;
    public float MaximumWingSpread => maximumWingSpread;
    public float WingSpreadStart => wingSpreadStart;
    public float WingSpreadSpeed => wingSpreadSpeed;
    [SerializeField, Range(0f, 30f)] private float wingTipSpread = 12f;
    [SerializeField, Range(0f, 35f)] private float primaryFanSpread = 24f;
    public float WingTipSpread => wingTipSpread;
    public float PrimaryFanSpread => primaryFanSpread;
    // Preserve the user's existing extra 10 units of maximum reach.
    private const float ExtraCarryReach = 10f;

    private readonly NetworkVariable<OnlineMovementState> movementState =
        new NetworkVariable<OnlineMovementState>(OnlineMovementState.Idle,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool IsMovingForWings => IsSpawned && movementState.Value == OnlineMovementState.Walk;
    private readonly PlayerPreparation preparation = new PlayerPreparation();
    private OnlinePlayerStateMachine stateMachine;
    private IOnlinePlayerInput inputSource;
    private CharacterController controller;
    private OwnerNetworkTransform networkTransform;
    private PlayerCharacterMotor motor;
    private PlayerSpawnProbe spawnProbe;
    private LocalPlayerCamera playerCamera;
    private PlayerAnimationPresenter animationPresenter;
    private NetworkNest nest;
    private NetworkTestMovement partner;
    private DuckNeckIK neckIK;

    public Vector3 CarryPoint => carryAnchor != null ? carryAnchor.position : transform.position;
    public Vector3 CarryOffset => carryAnchor != null
        ? transform.InverseTransformPoint(carryAnchor.position) : Vector3.zero;
    public bool HasCarryAnchor => carryAnchor != null;
    public bool HasDuckRig => neckIK != null && neckIK.Ready;
    public float BodyRadius => controller.radius;

    // Flat ground, forward-facing anchors and unit root scales are still required.
    public float GetCarryRootDistance(NetworkTestMovement other, float gripDistance) =>
        gripDistance + CarryOffset.z + other.CarryOffset.z;

    public bool PreparationRejected(uint sequence) => preparation.Rejected(sequence);
    public string PreparationDetails(uint sequence) =>
        preparation.Describe(OwnerClientId, sequence, transform.position);
    public bool PreparedOnServer(uint sequence) => IsServer && preparation.Ready(sequence, transform.position);
    public bool StartPositionIsClear(Vector3 position, out string reason) =>
        spawnProbe.StartPositionIsClear(position, out reason);
    public bool TryGetStartPosition(Vector3 approximate, out Vector3 position) =>
        spawnProbe.TryGetStartPosition(approximate, out position);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = false;
        networkTransform = GetComponent<OwnerNetworkTransform>();
        if (carryAnchor == null) carryAnchor = transform.Find("CarryAnchor");
        neckIK = GetComponent<DuckNeckIK>();
        if (neckIK == null) neckIK = gameObject.AddComponent<DuckNeckIK>();
        if (onlineAnimator == null) onlineAnimator = GetComponentInChildren<Animator>();
        stateMachine = new OnlinePlayerStateMachine();
        inputSource = new KeyboardPlayerInput();
        motor = new PlayerCharacterMotor(controller, this);
        spawnProbe = new PlayerSpawnProbe(controller);
        playerCamera = new LocalPlayerCamera(transform);
        animationPresenter = new PlayerAnimationPresenter(onlineAnimator, this);
    }

    public override void OnNetworkSpawn()
    {
        preparation.Reset();
        stateMachine.Reset();
        movementState.OnValueChanged += OnMovementStateChanged;
        if (IsOwner) movementState.Value = OnlineMovementState.Idle;
        OnMovementStateChanged(movementState.Value, movementState.Value);
        animationPresenter.Validate();
        if (!IsOwner) return;
        // Preserve the prototype's waiting position; round preparation places both players later.
        transform.position = new Vector3(IsHost ? -2f : 2f, 2.1f, 0f);
        controller.enabled = true;
        playerCamera.Bind(cameraTrackingTarget);
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;
        if (nest == null) nest = FindFirstObjectByType<NetworkNest>();
        if (nest == null || (nest.HasEggTest &&
            (!nest.CanPlayerMove(preparation.LocalSequence) || !preparation.LocalPlacementSucceeded)))
        {
            motor.Stop();
            stateMachine.Reset();
            movementState.Value = OnlineMovementState.Idle;
            return;
        }

        OnlinePlayerInput input = inputSource.Read();
        Vector2 move = stateMachine.Tick(input.Move, input.Jump);
        movementState.Value = stateMachine.CurrentState.Kind;
        motor.Move(playerCamera.ToWorldMovement(move), stateMachine.JumpPressed, speed, jumpHeight,
            Time.deltaTime, fallGravityMultiplier, riseGravityMultiplier);
        if (partner != null && partner.IsSpawned)
            transform.rotation = PlayerCharacterMotor.FacingRotation(transform.position, partner.transform.position);
    }

    private void LateUpdate()
    {
        if (IsSpawned && IsOwner) playerCamera.Tick();
    }

    public override void OnNetworkDespawn()
    {
        movementState.OnValueChanged -= OnMovementStateChanged;
        stateMachine.Reset();
        motor.Stop();
        controller.enabled = false;
        playerCamera.Unbind();
        nest = null;
        partner = null;
    }

    private void OnMovementStateChanged(OnlineMovementState previous, OnlineMovementState next) =>
        animationPresenter.Show(next, animationFade);

    public void PrepareOnServer(uint sequence, Vector3 position, Vector3 partnerPosition)
    {
        if (!IsServer) return;
        preparation.Begin(sequence, position);
        PrepareOwnerRpc(sequence, position, partnerPosition);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void PrepareOwnerRpc(uint sequence, Vector3 position, Vector3 partnerPosition)
    {
        if (!preparation.TryBeginLocal(sequence)) return;
        motor.Stop();
        controller.enabled = false;
        transform.SetPositionAndRotation(position, PlayerCharacterMotor.FacingRotation(position, partnerPosition));
        controller.enabled = true;
        controller.Move(Vector3.down * 0.2f);
        preparation.CompleteLocal(controller.isGrounded && Vector3.Distance(transform.position, position) < 0.15f);
        networkTransform.Teleport(transform.position, transform.rotation, transform.localScale);
        ConfirmPreparationRpc(sequence, preparation.LocalPlacementSucceeded,
            $"ownerGrounded={controller.isGrounded}, ownerPosition={transform.position:F3}, " +
            $"ownerError={Vector3.Distance(transform.position, position):F3}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ConfirmPreparationRpc(uint sequence, bool grounded, string details) =>
        preparation.Confirm(sequence, grounded, details);

    Vector3 IPlayerMovementConstraint.Limit(Vector3 requestedMove, float deltaTime)
    {
        FindCarryReferences();
        if (nest == null || !nest.IsSpawned || partner == null || !partner.IsSpawned || nest.GripDistance <= 0.01f)
            return requestedMove;
        float rootDistance = GetCarryRootDistance(partner, nest.GripDistance);
        if (neckIK != null && neckIK.Ready && partner.neckIK != null && partner.neckIK.Ready && nest.CarryReady)
        {
            // Evaluate the moving nest as well as both necks, rather than tethering
            // the owner to yesterday's grip position (which prevents shared travel).
            float initial = ReachError(Vector3.zero);
            float allowed = Mathf.Max(0f, initial);
            if (ReachError(requestedMove) <= allowed) return requestedMove;
            // Remove the outward component while retaining tangential movement.
            const float epsilon = 0.002f;
            Vector3 gradient = new Vector3(
                ReachError(Vector3.right * epsilon) - ReachError(Vector3.left * epsilon),
                ReachError(Vector3.up * epsilon) - ReachError(Vector3.down * epsilon),
                ReachError(Vector3.forward * epsilon) - ReachError(Vector3.back * epsilon)).normalized;
            float outward = Vector3.Dot(requestedMove, gradient);
            if (outward > 0f)
            {
                Vector3 tangent = requestedMove - gradient * outward;
                // Small inward curvature allowance for travel around the boundary.
                tangent -= gradient * Mathf.Min(0.01f, tangent.sqrMagnitude / Mathf.Max(0.1f, neckIK.Reach));
                if (ReachError(tangent) <= allowed) return tangent;
            }
            float low = 0f, high = 1f;
            for (int i = 0; i < 14; i++)
            {
                float mid = (low + high) * 0.5f;
                if (ReachError(requestedMove * mid) <= allowed) low = mid;
                else high = mid;
            }
            return requestedMove * low;
        }
        return CarryDistanceConstraint.Limit(requestedMove, transform.position, partner.transform.position,
            Mathf.Max(0.01f, rootDistance - carrySlack), rootDistance + carrySlack + ExtraCarryReach,
            correctionSpeed, deltaTime, OwnerClientId < partner.OwnerClientId);
    }
    private float ReachError(Vector3 move)
    {
        Vector3 mine = transform.position + move;
        Vector3 theirs = partner.transform.position;
        Quaternion myRotation = PlayerCharacterMotor.FacingRotation(mine, theirs);
        Quaternion theirRotation = PlayerCharacterMotor.FacingRotation(theirs, mine);
        Vector3 myAnchor = mine + myRotation * Vector3.Scale(CarryOffset, transform.lossyScale);
        Vector3 theirAnchor = theirs + theirRotation * Vector3.Scale(partner.CarryOffset, partner.transform.lossyScale);
        bool host = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId;
        Vector3 h = host ? myAnchor : theirAnchor;
        Vector3 g = host ? theirAnchor : myAnchor;
        float minimumDistance = Mathf.Max(BodyRadius + partner.BodyRadius,
            GetCarryRootDistance(partner, nest.GripDistance) *
            Mathf.Max(minimumDuckDistanceRatio, partner.minimumDuckDistanceRatio));
        Vector3 horizontalSeparation = mine - theirs;
        horizontalSeparation.y = 0f;
        return Mathf.Max(minimumDistance - horizontalSeparation.magnitude, Mathf.Max(
            neckIK.ReachErrorAt(mine, myRotation, nest.PredictGrip(OwnerClientId, h, g)),
            partner.neckIK.ReachErrorAt(theirs, theirRotation, nest.PredictGrip(partner.OwnerClientId, h, g))));
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
}
