using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class NetworkNest : NetworkBehaviour
{
    [SerializeField] private Transform leftGrip;
    [SerializeField] private Transform rightGrip;
    [SerializeField] private float carryHeight = 0.4f;
    [Header("Host-authoritative egg test")]
    [SerializeField] private bool enableEggTest;
    [SerializeField] private GameObject eggVisualPrefab;
    [SerializeField] private BoxCollider nestFloor;
    [SerializeField, Min(0.1f)] private float eggBreakSpeed = 6f;
    [SerializeField, Min(0f)] private float eggDropClearance = 0.2f;
    [SerializeField, Range(0.05f, 0.25f)] private float presentationDelay = 0.1f;

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
        egg.Initialize(transform, eggVisualPrefab, eggBreakSpeed);
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
            if (placed)
            {
                placed = false;
                phase = NestEggPhase.Waiting;
                if (egg != null) egg.WaitForPlayers();
                generation++;
                PublishFrame();
            }
            return;
        }

        Vector3 leftTarget = host.transform.position + Vector3.up * carryHeight;
        Vector3 rightTarget = guest.transform.position + Vector3.up * carryHeight;
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
