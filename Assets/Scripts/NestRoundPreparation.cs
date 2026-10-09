using System;
using UnityEngine;
using Unity.Netcode;

// Server round workflow. Emits commands; owns no Rigidbody, egg or NetworkVariable.
public sealed class NestRoundPreparation
{
    private readonly Transform transform, leftGrip, rightGrip;
    private readonly UnityEngine.Object context;
    private NetworkObject preparingHost, preparingGuest;
    private uint preparationSequence;
    private readonly RoundPreparationGate preparationGate = new RoundPreparationGate();
    private Vector3 previousHostPosition, previousGuestPosition;
    private bool havePreviousPositions;
    private Vector3 startCenter;
    private float stableDuration, preparationTimeout;
    private float GripDistance => Vector3.Distance(leftGrip.position, rightGrip.position);
    public NestRoundStage Stage { get; private set; }
    public uint ReleasedSequence { get; private set; }
    public event Action Started;
    public event Action Released;
    public event Action<Vector3, Vector3> Place;

    public NestRoundPreparation(Transform root, Transform left, Transform right, UnityEngine.Object context)
    { transform = root; leftGrip = left; rightGrip = right; this.context = context; }
    public void Configure(Vector3 center, float stable, float timeout)
    { startCenter = center; stableDuration = stable; preparationTimeout = timeout; }
    public void Reset()
    {
        Stage = NestRoundStage.WaitingForPlayers;
        ReleasedSequence = 0;
        preparingHost = preparingGuest = null;
        havePreviousPositions = false;
    }

    public bool Step(NetworkObject host, NetworkObject guest)
    {
        if (host != preparingHost || guest != preparingGuest)
        {
            preparingHost = host;
            preparingGuest = guest;
            Begin(host, guest);
            return false;
        }
        if (Stage == NestRoundStage.Playing) return true;
        if (Stage == NestRoundStage.SetupFailed) return false;

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
            Released?.Invoke();
            ReleasedSequence = preparationSequence;
            Stage = NestRoundStage.Playing;
            return false;
        }
        Stage = next;
        if (next != NestRoundStage.PlacingNest) return false;
        Vector3 leftTarget = hostMovement.CarryPoint;
        Vector3 rightTarget = guestMovement.CarryPoint;
        Vector3 axis = rightTarget - leftTarget;
        if (axis.sqrMagnitude < 0.0001f)
        {
            FailPreparation("Carry points overlap. Check CarryAnchor and grip positions.");
            return false;
        }
        Place?.Invoke(leftTarget, rightTarget);
        Stage = NestRoundStage.PlacingNest;
        return false;
    }
    public void Begin(NetworkObject host, NetworkObject guest)
    {
        Stage = NestRoundStage.PreparingPlayers;
        ReleasedSequence = 0;
        Started?.Invoke();
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
        Stage = NestRoundStage.SetupFailed;
        Debug.LogError("Nest preparation: " + reason, context);
    }
}
