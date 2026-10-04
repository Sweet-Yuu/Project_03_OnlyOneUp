using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class NetworkNest : NetworkBehaviour
{
    [SerializeField] private Transform leftGrip;
    [SerializeField] private Transform rightGrip;
    [SerializeField] private float carryHeight = 20f;
    [Header("Host-authoritative egg test")]
    [SerializeField] private bool enableEggTest;
    [SerializeField] private GameObject eggVisualPrefab;
    [SerializeField] private BoxCollider nestFloor;
    [SerializeField, Min(0f)] private float eggDropClearance = 0.2f;
    [SerializeField, Range(0.05f, 0.25f)] private float presentationDelay = 0.1f;
    [Header("Round preparation (flat starting ground)")]
    [SerializeField] private Vector3 startCenter = new Vector3(0f, 2.1f, 0f);
    [SerializeField, Min(0.1f)] private float stableDuration = 0.2f;
    [SerializeField, Min(2f)] private float preparationTimeout = 15f;
    private readonly NetworkVariable<NestRoundStage> roundStage = new NetworkVariable<NestRoundStage>();
    private readonly NetworkVariable<uint> releasedPreparation = new NetworkVariable<uint>();
    private NetworkObject preparingHost, preparingGuest;
    private uint preparationSequence;
    private readonly RoundPreparationGate preparationGate = new RoundPreparationGate();
    private Vector3 previousHostPosition, previousGuestPosition;
    private bool havePreviousPositions;

    private readonly NetworkVariable<NestEggFrame> frame = new NetworkVariable<NestEggFrame>();
    private readonly NestEggFrameBuffer history = new NestEggFrameBuffer();
    private Rigidbody body;
    private NestEggBody egg;
    private Vector3 gripMidpoint;
    private Quaternion localGripFrame;
    private bool validSetup;
    private bool placed;
    private uint generation;
    private double nextSnapshot;
    private NestEggPhase phase;
    private NetworkObject ignoredHost;
    private NetworkObject ignoredGuest;

    public float GripDistance => leftGrip == null || rightGrip == null ? 0f :
        Vector3.Distance(leftGrip.position, rightGrip.position);
    public NestEggPhase EggPhase => phase;
    public bool HasEggTest => enableEggTest;
    public NestRoundStage RoundStage => roundStage.Value;
    public bool CanPlayerMove(uint sequence) => IsSpawned &&
        roundStage.Value == NestRoundStage.Playing && sequence != 0 && releasedPreparation.Value == sequence;
    public bool CanResetEgg => IsSpawned && IsServer && placed && egg != null;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    public override void OnNetworkSpawn()
    {
        validSetup = false;
        placed = false;
        phase = NestEggPhase.Waiting;
        history.Clear();
        ignoredHost = ignoredGuest = null;
        preparingHost = preparingGuest = null;
        if (IsServer)
        {
            roundStage.Value = NestRoundStage.WaitingForPlayers;
            releasedPreparation.Value = 0;
        }
        body.interpolation = IsServer ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;

        if (leftGrip == null || rightGrip == null || (enableEggTest && nestFloor == null))
        {
            Debug.LogError("Nest: assign LeftGrip, RightGrip and the Bottom BoxCollider.", this);
            return;
        }
        if ((transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            Debug.LogError("Nest root must have world scale (1, 1, 1). Scale the visual child instead.", this);
            return;
        }
        Vector3 left = transform.InverseTransformPoint(leftGrip.position);
        Vector3 right = transform.InverseTransformPoint(rightGrip.position);
        Vector3 axis = right - left;
        if (axis.sqrMagnitude < 0.001f || Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up)) > 0.99f)
        {
            Debug.LogError("Nest: invalid grip positions.", this);
            return;
        }
        gripMidpoint = (left + right) * 0.5f;
        localGripFrame = Quaternion.LookRotation(axis, Vector3.up);
        // Existing movement-only scenes keep their NetworkTransform setup.
        if (!enableEggTest)
        {
            validSetup = true;
            return;
        }
        CreateEgg();
        egg.SetAuthority(IsServer);
        validSetup = true;

        if (IsServer)
        {
            generation = frame.Value.Generation + 1;
            PublishFrame();
        }
        else
        {
            frame.OnValueChanged += ReceiveFrame;
            ReceiveFrame(default, frame.Value);
        }
    }

    private void CreateEgg()
    {
        // This presentation object has no independent NetworkTransform/NetworkObject.
        // Only the server simulates it; the pair snapshot controls it on clients.
        var eggObject = new GameObject("NetworkEgg (synced by Nest)");
        egg = eggObject.AddComponent<NestEggBody>();
        egg.Initialize(transform, eggVisualPrefab);
        egg.Broken += OnEggBroken;
    }

    private bool TryGetPlayers(out NetworkObject host, out NetworkObject guest)
    {
        host = guest = null;
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.ClientId == Unity.Netcode.NetworkManager.ServerClientId) host = client.PlayerObject;
            else guest = client.PlayerObject;
        }
        return host != null && guest != null;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || !validSetup) return;
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest))
        {
            if (placed || (enableEggTest && roundStage.Value != NestRoundStage.WaitingForPlayers))
            {
                if (enableEggTest)
                {
                    roundStage.Value = NestRoundStage.WaitingForPlayers;
                    releasedPreparation.Value = 0;
                    preparingHost = preparingGuest = null;
                }
                placed = false;
                phase = NestEggPhase.Waiting;
                if (egg != null) egg.WaitForPlayers();
                generation++;
                PublishFrame();
            }
            return;
        }

        if (enableEggTest && !PrepareRound(host, guest)) return;

        Vector3 leftTarget = enableEggTest
            ? host.GetComponent<NetworkTestMovement>().CarryPoint
            : host.transform.position + Vector3.up * carryHeight;
        Vector3 rightTarget = enableEggTest
            ? guest.GetComponent<NetworkTestMovement>().CarryPoint
            : guest.transform.position + Vector3.up * carryHeight;
        Vector3 axis = rightTarget - leftTarget;
        if (axis.sqrMagnitude < 0.001f || Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up)) > 0.99f) return;
        Quaternion rotation = Quaternion.LookRotation(axis, Vector3.up) * Quaternion.Inverse(localGripFrame);
        Vector3 position = (leftTarget + rightTarget) * 0.5f - rotation * gripMidpoint;

        if (!placed)
        {
            body.position = position;
            body.rotation = rotation;
            placed = true;
            ResetEggOnServer();
        }
        else
        {
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }
        if (egg != null && host != ignoredHost)
        {
            egg.IgnorePlayer(host.gameObject);
            ignoredHost = host;
        }
        if (egg != null && guest != ignoredGuest)
        {
            egg.IgnorePlayer(guest.gameObject);
            ignoredGuest = guest;
        }
        if (egg != null && egg.IsSimulating && egg.Body.position.y < -50f) egg.BreakOnServer();
    }

    private bool PrepareRound(NetworkObject host, NetworkObject guest)
    {
        if (host != preparingHost || guest != preparingGuest)
        {
            preparingHost = host;
            preparingGuest = guest;
            BeginPreparation(host, guest);
            return false;
        }
        if (roundStage.Value == NestRoundStage.Playing) return true;
        if (roundStage.Value == NestRoundStage.SetupFailed) return false;

        var hostMovement = host.GetComponent<NetworkTestMovement>();
        var guestMovement = guest.GetComponent<NetworkTestMovement>();
        if ((hostMovement != null && hostMovement.PreparationRejected(preparationSequence)) ||
            (guestMovement != null && guestMovement.PreparationRejected(preparationSequence)))
        {
            FailPreparation("Owner could not stand at the requested position. " +
                hostMovement?.PreparationDetails(preparationSequence) + " | " +
                guestMovement?.PreparationDetails(preparationSequence));
            return false;
        }
        bool ready = hostMovement != null && guestMovement != null &&
            hostMovement.PreparedOnServer(preparationSequence) &&
            guestMovement.PreparedOnServer(preparationSequence) &&
            Mathf.Abs(hostMovement.CarryPoint.y - guestMovement.CarryPoint.y) < 0.03f &&
            Mathf.Abs(Vector3.Distance(hostMovement.CarryPoint, guestMovement.CarryPoint) - GripDistance) < 0.05f;

        Vector3 h = host.transform.position;
        Vector3 g = guest.transform.position;
        bool still = havePreviousPositions && (h - previousHostPosition).sqrMagnitude < 0.000001f &&
            (g - previousGuestPosition).sqrMagnitude < 0.000001f;
        previousHostPosition = h;
        previousGuestPosition = g;
        havePreviousPositions = true;
        NestRoundStage next = preparationGate.Step(Time.unscaledTime, Time.fixedDeltaTime,
            ready, still, stableDuration, preparationTimeout);
        if (next == NestRoundStage.SetupFailed)
        {
            FailPreparation("Timed out. " + hostMovement?.PreparationDetails(preparationSequence) + " | " +
                guestMovement?.PreparationDetails(preparationSequence) +
                $" | rootDistance={Vector3.Distance(h, g):F3}, gripDistance={GripDistance:F3}, still={still}, " +
                $"hostCarryPoint={hostMovement?.CarryPoint}, guestCarryPoint={guestMovement?.CarryPoint}. Both machines must run the same new build.");
            return false;
        }
        if (next == NestRoundStage.Playing)
        {
            // One full physics step has elapsed since the tray was teleported.
            ResetEggOnServer();
            releasedPreparation.Value = preparationSequence;
            roundStage.Value = NestRoundStage.Playing;
            return false;
        }
        roundStage.Value = next;
        if (next != NestRoundStage.PlacingNest) return false;
        Vector3 leftTarget = hostMovement.CarryPoint;
        Vector3 rightTarget = guestMovement.CarryPoint;
        Vector3 axis = rightTarget - leftTarget;
        if (axis.sqrMagnitude < 0.0001f)
        {
            FailPreparation("Carry points overlap. Check CarryAnchor and grip positions.");
            return false;
        }
        Quaternion rotation = Quaternion.LookRotation(axis, Vector3.up) * Quaternion.Inverse(localGripFrame);
        body.position = (leftTarget + rightTarget) * 0.5f - rotation * gripMidpoint;
        body.rotation = rotation;
        placed = true;
        generation++;
        PublishFrame();
        roundStage.Value = NestRoundStage.PlacingNest;
        return false;
    }

    private void BeginPreparation(NetworkObject host, NetworkObject guest)
    {
        roundStage.Value = NestRoundStage.PreparingPlayers;
        releasedPreparation.Value = 0;
        placed = false;
        phase = NestEggPhase.Waiting;
        egg.WaitForPlayers();
        generation++;
        PublishFrame();
        havePreviousPositions = false;
        preparationGate.Begin(Time.unscaledTime);
        preparationSequence++;
        var h = host.GetComponent<NetworkTestMovement>();
        var g = guest.GetComponent<NetworkTestMovement>();
        if (h == null || g == null)
        {
            FailPreparation("Missing NetworkTestMovement.");
            return;
        }
        if (!h.HasCarryAnchor || !g.HasCarryAnchor ||
            Mathf.Abs(h.CarryOffset.x) > 0.01f || Mathf.Abs(g.CarryOffset.x) > 0.01f ||
            h.CarryOffset.z <= 0f || g.CarryOffset.z <= 0f ||
            Mathf.Abs(h.CarryOffset.y - g.CarryOffset.y) > 0.01f ||
            (h.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f ||
            (g.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            FailPreparation("Assign CarryAnchor on both players: local X=0, Z>0, equal local Y, root world scale=(1,1,1).");
            return;
        }
        float rootDistance = h.GetCarryRootDistance(g, GripDistance);
        Vector3 left = transform.InverseTransformPoint(leftGrip.position);
        Vector3 right = transform.InverseTransformPoint(rightGrip.position);
        if (Mathf.Abs(left.y - right.y) > 0.02f ||
            !h.TryGetStartPosition(startCenter + Vector3.left * rootDistance * 0.5f, out Vector3 hp) ||
            !g.TryGetStartPosition(startCenter + Vector3.right * rootDistance * 0.5f, out Vector3 gp) ||
            Mathf.Abs(hp.y - gp.y) > 0.03f)
        {
            FailPreparation("Need two movement components, equal-height grips and flat ground under both starting positions.");
            return;
        }
        if (rootDistance < h.BodyRadius + g.BodyRadius + 0.05f)
        {
            FailPreparation($"Player colliders are too wide: rootDistance={rootDistance:F2}, combined radii={h.BodyRadius + g.BodyRadius:F2}. Adjust grip spacing or player collider size.");
            return;
        }
        Physics.SyncTransforms();
        if (!h.StartPositionIsClear(hp, out string hostObstacle))
        {
            FailPreparation("Host: " + hostObstacle);
            return;
        }
        if (!g.StartPositionIsClear(gp, out string guestObstacle))
        {
            FailPreparation("Client: " + guestObstacle);
            return;
        }
        h.PrepareOnServer(preparationSequence, hp, gp);
        g.PrepareOnServer(preparationSequence, gp, hp);
    }

    private void FailPreparation(string reason)
    {
        roundStage.Value = NestRoundStage.SetupFailed;
        Debug.LogError("Nest preparation: " + reason, this);
    }

    public void RetryPreparationOnServer()
    {
        if (IsServer && enableEggTest && roundStage.Value == NestRoundStage.SetupFailed &&
            TryGetPlayers(out NetworkObject host, out NetworkObject guest)) BeginPreparation(host, guest);
    }

    // Capture after physics; both bodies share the same network timestamp.
    private void LateUpdate()
    {
        if (!IsSpawned || !validSetup || !enableEggTest) return;
        if (IsServer)
        {
            if (NetworkManager.ServerTime.Time >= nextSnapshot) PublishFrame();
        }
        else if (history.Sample(NetworkManager.ServerTime.Time - presentationDelay, out NestEggFrame sample))
        {
            transform.SetPositionAndRotation(sample.NestPosition, sample.NestRotation);
            egg.ApplyRemote(sample);
            phase = sample.Phase;
        }
    }

    private void PublishFrame()
    {
        if (!IsServer || egg == null) return;
        double time = NetworkManager.ServerTime.Time;
        frame.Value = new NestEggFrame
        {
            Time = time,
            Generation = generation,
            Phase = phase,
            NestPosition = body.position,
            NestRotation = body.rotation,
            EggPosition = egg.Body.position,
            EggRotation = egg.Body.rotation
        };
        nextSnapshot = time + 1.0 / Mathf.Max(1, (int)NetworkManager.NetworkConfig.TickRate);
    }

    private void ReceiveFrame(NestEggFrame previous, NestEggFrame current)
    {
        if (!history.Add(current)) return;
        if (history.Count == 1)
        {
            transform.SetPositionAndRotation(current.NestPosition, current.NestRotation);
            egg.ApplyRemote(current);
            phase = current.Phase;
        }
    }

    public void ResetEggOnServer()
    {
        if (!CanResetEgg || egg == null) return;
        Vector3 floorTop = nestFloor.transform.TransformPoint(
            nestFloor.center + Vector3.up * nestFloor.size.y * 0.5f);
        Vector3 localTop = transform.InverseTransformPoint(floorTop);
        Vector3 position = body.position + body.rotation *
            (localTop + Vector3.up * (NestEggBody.Height * 0.5f + eggDropClearance));
        egg.ResetOnServer(position, body.rotation);
        // Collider re-enabling during reset can clear ignored pairs.
        ignoredHost = ignoredGuest = null;
        if (TryGetPlayers(out NetworkObject host, out NetworkObject guest))
        {
            egg.IgnorePlayer(host.gameObject);
            egg.IgnorePlayer(guest.gameObject);
            ignoredHost = host;
            ignoredGuest = guest;
        }
        phase = NestEggPhase.Intact;
        generation++;
        PublishFrame();
    }

    public void DropEggForTestOnServer()
    {
        if (!CanResetEgg || egg == null) return;
        Bounds bounds = nestFloor.bounds;
        foreach (Collider c in GetComponentsInChildren<Collider>()) bounds.Encapsulate(c.bounds);
        Vector3 position = new Vector3(bounds.max.x + 1f, bounds.max.y + 1f, bounds.center.z);
        egg.ResetOnServer(position, Quaternion.identity);
        ignoredHost = ignoredGuest = null;
        phase = NestEggPhase.Intact;
        generation++;
        PublishFrame();
    }

    private void OnEggBroken()
    {
        if (!IsServer) return;
        phase = NestEggPhase.Broken;
        nextSnapshot = 0;
    }

    public override void OnNetworkDespawn()
    {
        frame.OnValueChanged -= ReceiveFrame;
        history.Clear();
        validSetup = placed = false;
        phase = NestEggPhase.Waiting;
        DestroyEgg();
    }

    private void DestroyEgg()
    {
        if (egg == null) return;
        egg.Broken -= OnEggBroken;
        Destroy(egg.gameObject);
        egg = null;
    }

    public override void OnDestroy()
    {
        DestroyEgg();
        base.OnDestroy();
    }
}
