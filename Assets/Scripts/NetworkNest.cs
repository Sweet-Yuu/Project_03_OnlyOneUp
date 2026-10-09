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
    private NestRoundPreparation preparation;
    private readonly NestCarryPose carryPose = new NestCarryPose();
    private NestEggLifecycle eggs;

    private readonly NetworkVariable<NestEggFrame> frame = new NetworkVariable<NestEggFrame>();
    private readonly NestEggFrameBuffer history = new NestEggFrameBuffer();
    private Rigidbody body;
    private NestEggBody egg => eggs?.Egg;
    private bool validSetup;
    private bool placed;
    private uint generation;
    private double nextSnapshot;
    private NestEggPhase phase;

    public float GripDistance => leftGrip == null || rightGrip == null ? 0f :
        Vector3.Distance(leftGrip.position, rightGrip.position);
    public NestEggPhase EggPhase => phase;
    public bool HasEggTest => enableEggTest;
    public bool CarryReady => IsSpawned && validSetup &&
        (enableEggTest ? roundStage.Value == NestRoundStage.Playing : true);
    public Transform GetGrip(ulong owner) =>
        owner == Unity.Netcode.NetworkManager.ServerClientId ? leftGrip : rightGrip;

    public Vector3 PredictGrip(ulong owner, Vector3 hostAnchor, Vector3 guestAnchor)
    {
        if (!NestCarryPose.IsValidAxis(guestAnchor - hostAnchor)) return GetGrip(owner).position;
        Pose pose = carryPose.Calculate(hostAnchor, guestAnchor);
        return pose.position + pose.rotation * transform.InverseTransformPoint(GetGrip(owner).position);
    }
    public NestRoundStage RoundStage => roundStage.Value;
    public bool CanPlayerMove(uint sequence) => IsSpawned &&
        roundStage.Value == NestRoundStage.Playing && sequence != 0 && releasedPreparation.Value == sequence;
    public bool CanResetEgg => IsSpawned && IsServer && placed && egg != null;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        preparation = new NestRoundPreparation(transform, leftGrip, rightGrip, this);
        preparation.Started += OnPreparationStarted;
        preparation.Place += PlacePreparedNest;
        preparation.Released += ResetEggOnServer;
        eggs = new NestEggLifecycle(transform, body, nestFloor);
        eggs.Broken += OnEggBroken;
    }

    public override void OnNetworkSpawn()
    {
        validSetup = false;
        placed = false;
        phase = NestEggPhase.Waiting;
        history.Clear();
        preparation.Reset();
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
        if (!carryPose.Initialize(transform, leftGrip, rightGrip, out string error))
        {
            Debug.LogError(error, this);
            return;
        }
        // Existing movement-only scenes keep their NetworkTransform setup.
        if (!enableEggTest)
        {
            validSetup = true;
            return;
        }
        eggs.Create(eggVisualPrefab, IsServer);
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
                    preparation.Reset();
                }
                placed = false;
                phase = NestEggPhase.Waiting;
                eggs.Wait();
                generation++;
                PublishFrame();
            }
            return;
        }

        if (enableEggTest && !PrepareRound(host, guest)) return;

        bool duckPair = host.GetComponent<NetworkTestMovement>() is NetworkTestMovement hm && hm.HasDuckRig &&
            guest.GetComponent<NetworkTestMovement>() is NetworkTestMovement gm && gm.HasDuckRig;
        Vector3 leftTarget = enableEggTest || duckPair
            ? host.GetComponent<NetworkTestMovement>().CarryPoint
            : host.transform.position + Vector3.up * carryHeight;
        Vector3 rightTarget = enableEggTest || duckPair
            ? guest.GetComponent<NetworkTestMovement>().CarryPoint
            : guest.transform.position + Vector3.up * carryHeight;
        if (!NestCarryPose.IsValidAxis(rightTarget - leftTarget)) return;
        Pose pose = carryPose.Calculate(leftTarget, rightTarget);
        Vector3 position = pose.position;
        Quaternion rotation = pose.rotation;

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
        eggs.Tick(host, guest);
    }

    private bool PrepareRound(NetworkObject host, NetworkObject guest)
    {
        preparation.Configure(startCenter, stableDuration, preparationTimeout);
        bool ready = preparation.Step(host, guest);
        SyncPreparation();
        return ready;
    }

    private void SyncPreparation()
    {
        roundStage.Value = preparation.Stage;
        releasedPreparation.Value = preparation.ReleasedSequence;
    }

    private void OnPreparationStarted()
    {
        placed = false;
        phase = NestEggPhase.Waiting;
        eggs.Wait();
        generation++;
        PublishFrame();
    }

    private void PlacePreparedNest(Vector3 left, Vector3 right)
    {
        Pose pose = carryPose.Calculate(left, right);
        body.position = pose.position;
        body.rotation = pose.rotation;
        placed = true;
        generation++;
        PublishFrame();
    }

    public void RetryPreparationOnServer()
    {
        if (!IsServer || !enableEggTest || roundStage.Value != NestRoundStage.SetupFailed) return;
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest)) return;
        preparation.Configure(startCenter, stableDuration, preparationTimeout);
        preparation.Begin(host, guest);
        SyncPreparation();
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
        if (!TryGetPlayers(out NetworkObject host, out NetworkObject guest)) host = guest = null;
        eggs.Reset(eggDropClearance, host, guest);
        phase = NestEggPhase.Intact;
        generation++;
        PublishFrame();
    }

    public void DropEggForTestOnServer()
    {
        if (!CanResetEgg || egg == null) return;
        eggs.DropForTest();
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

    private void DestroyEgg() => eggs?.Dispose();

    public override void OnDestroy()
    {
        DestroyEgg();
        base.OnDestroy();
    }
}
