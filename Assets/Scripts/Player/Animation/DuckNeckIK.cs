using UnityEngine;

// Runs after the nest's interpolated presentation. Never moves the gameplay root.
[DefaultExecutionOrder(1000)]
public sealed class DuckNeckIK : MonoBehaviour
{
    private Transform[] chain;
    private Transform beak;
    private Quaternion[] rest;
    private Vector3[] restPositions;
    private float neckLength;
    private float headLength;
    private Transform chest;
    private Quaternion chestRest;
    private Quaternion lean = Quaternion.identity;
    private Vector3 headOffsetLocal;
    private WingPose leftWing;
    private WingPose rightWing;
    private WingPose leftTip;
    private WingPose rightTip;
    private readonly FeatherPose[] feathers = new FeatherPose[10];
    private float wingSpread;

    private sealed class FeatherPose
    {
        private readonly Transform bone;
        private readonly Quaternion closed;
        private readonly Vector3 parentAxis;
        private readonly float fanPosition;

        public FeatherPose(Transform bone, Vector3 worldAxis, float fanPosition)
        {
            this.bone = bone;
            closed = bone.localRotation;
            parentAxis = bone.parent.InverseTransformDirection(worldAxis);
            this.fanPosition = fanPosition;
        }

        public void Apply(float amount, float angle)
        {
            if (bone != null)
                bone.localRotation = Quaternion.AngleAxis(angle * amount * fanPosition, parentAxis) * closed;
        }
    }

    // Derive each hinge from its own rest geometry, avoiding mirrored bone-axis assumptions.
    private sealed class WingPose
    {
        private readonly Transform bone;
        private readonly Quaternion restRotation;
        private readonly Vector3 axis;
        private readonly float outwardAngle;

        public WingPose(Transform bone, Transform tip, Transform root)
        {
            this.bone = bone;
            restRotation = bone.localRotation;
            Vector3 along = (tip.position - bone.position).normalized;
            float side = Vector3.Dot(bone.position - root.position, root.right) >= 0f ? 1f : -1f;
            Vector3 outward = root.right * side;
            Vector3 hinge = Vector3.Cross(along, outward);
            outwardAngle = Vector3.Angle(along, outward);
            axis = hinge.sqrMagnitude > 0.000001f
                ? bone.parent.InverseTransformDirection(hinge.normalized) : Vector3.zero;
        }

        public void Apply(float amount, float maximumAngle)
        {
            if (bone == null) return;
            bone.localRotation = axis.sqrMagnitude > 0f
                ? Quaternion.AngleAxis(Mathf.Min(maximumAngle, outwardAngle) * amount, axis) * restRotation
                : restRotation;
        }
    }
    private NetworkTestMovement player;
    private NetworkNest nest;
    public bool Ready { get; private set; }
    public float Reach { get; private set; }
    public float MaximumReach => neckLength * player.NeckStretchLimit + headLength;
    public Vector3 RootLocal { get; private set; }
    public float ReachErrorAt(Vector3 rootPosition, Quaternion rootRotation, Vector3 grip)
    {
        Vector3 neckRoot = rootPosition + rootRotation * Vector3.Scale(RootLocal, transform.lossyScale);
        Vector3 offset = rootRotation * headOffsetLocal;
        return Vector3.Distance(neckRoot, grip - offset) - neckLength * player.NeckStretchLimit * 0.985f;
    }

    private void Awake()
    {
        player = GetComponent<NetworkTestMovement>();
        Transform neck = Find(transform, "Neck_01");
        if (neck == null)
        {
            Debug.LogWarning("Duck IK: no active Neck_01 found under this Player.", this);
            return;
        }
        Transform n2 = neck.Find("Neck_02");
        Transform n3 = n2 != null ? n2.Find("Neck_03") : null;
        Transform head = n3 != null ? n3.Find("Head") : null;
        if (head == null) return;
        beak = head.Find("BreakGripPoint");
        if (beak == null)
        {
            Debug.LogWarning("Duck IK: create BreakHeadGrip, BeakGripPoint or BreakGripPoint under the active Head bone.", this);
            return;
        }
        chain = new[] { neck, n2, n3, head };
        rest = new Quaternion[chain.Length];
        restPositions = new Vector3[chain.Length];
        for (int i = 0; i < chain.Length; i++)
        {
            rest[i] = chain[i].localRotation;
            restPositions[i] = chain[i].localPosition;
            Reach += Vector3.Distance(chain[i].position,
                i + 1 < chain.Length ? chain[i + 1].position : beak.position);
        }
        RootLocal = transform.InverseTransformPoint(neck.position);
        chest = neck.parent;
        if (chest != null && chest.name == "Chest") chestRest = chest.localRotation;
        else chest = null;
        headLength = Vector3.Distance(head.position, beak.position);
        headOffsetLocal = Quaternion.Inverse(transform.rotation) * (beak.position - head.position);
        neckLength = Reach - headLength;
        leftWing = CreateWing(neck.parent, "Wing.L", "WingTip.L");
        rightWing = CreateWing(neck.parent, "Wing.R", "WingTip.R");
        leftTip = CreateWing(neck.parent, "WingTip.L", "WingTip.L_end");
        rightTip = CreateWing(neck.parent, "WingTip.R", "WingTip.R_end");
        InitializeFeathers(neck.parent, "L", 0);
        InitializeFeathers(neck.parent, "R", 5);
        Ready = Reach > 0.001f;
    }

    private void InitializeFeathers(Transform rigRoot, string sideName, int offset)
    {
        Transform tip = Find(rigRoot, "WingTip." + sideName);
        Transform shoulder = Find(rigRoot, "Wing." + sideName);
        if (tip == null || shoulder == null) return;
        float side = Vector3.Dot(shoulder.position - transform.position, transform.right) >= 0f ? 1f : -1f;
        // The feathers fan in the wing plane. Its normal follows the same
        // outward hinge as the shoulder, independent of imported bone roll.
        Vector3 axis = Vector3.Cross((tip.position - shoulder.position).normalized, transform.right * side).normalized;
        int found = 0;
        for (int i = 0; i < 5; i++)
        {
            Transform feather = Find(tip, $"DEF_Primary_{i + 1:00}.{sideName}");
            if (feather == null) continue;
            feathers[offset + i] = new FeatherPose(feather, axis, (2 - i) * 0.5f);
            found++;
        }
        if (found != 5)
            Debug.LogWarning($"Duck fan: found {found}/5 Primary bones on {sideName}. Import the expanded wing FBX with all deform bones.", this);
    }

    private WingPose CreateWing(Transform rigRoot, string boneName, string tipName)
    {
        if (rigRoot == null) return null;
        Transform bone = Find(rigRoot, boneName);
        Transform tip = bone != null ? Find(bone, tipName) : null;
        if (bone == null || tip == null)
        {
            Debug.LogWarning($"Duck wings: missing {boneName}/{tipName}; this wing will keep its original pose.", this);
            return null;
        }
        return new WingPose(bone, tip, transform);
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            // Replacement models can coexist with disabled old rigs in the prefab.
            // Never bind presentation to a rig the player cannot see.
            if (!child.gameObject.activeSelf) continue;
            if (child.name == name) return child;
            Transform result = Find(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void LateUpdate()
    {
        if (!Ready || player == null || !player.IsSpawned) return;
        if (nest == null) nest = FindFirstObjectByType<NetworkNest>();
        if (chest != null) chest.localRotation = chestRest;
        // Reset each solve to avoid accumulating twist across frames.
        for (int i = 0; i < chain.Length; i++)
        {
            chain[i].localRotation = rest[i];
            chain[i].localPosition = restPositions[i];
        }
        Transform target = nest != null && nest.CarryReady ? nest.GetGrip(player.OwnerClientId) : null;
        Transform head = chain[chain.Length - 1];
        // Capture the upright head orientation before leaning the chest.
        Quaternion headRotation = head.rotation;
        Quaternion wantedLean = Quaternion.identity;
        float reachFraction = target != null
            ? Vector3.Distance(target.position, chain[0].position) / Mathf.Max(0.001f, MaximumReach - headLength)
            : 0f;
        if (target != null && chest != null)
        {
            Vector3 toGrip = target.position - chain[0].position;
            float tension = Mathf.InverseLerp(player.ChestLeanStart, 1f,
                toGrip.magnitude / Mathf.Max(0.001f, MaximumReach - headLength));
            Vector3 direction = Vector3.ProjectOnPlane(toGrip, transform.up).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 worldAxis = Vector3.Cross(transform.up, direction).normalized;
                Vector3 parentAxis = chest.parent.InverseTransformDirection(worldAxis);
                wantedLean = Quaternion.AngleAxis(player.MaximumChestLean * Mathf.SmoothStep(0f, 1f, tension), parentAxis);
            }
        }
        lean = Quaternion.Slerp(lean, wantedLean, 1f - Mathf.Exp(-player.ChestLeanSpeed * Time.deltaTime));
        if (chest != null) chest.localRotation = lean * chestRest;
        // Movement constraints use the actual leaned neck root on the next update.
        RootLocal = transform.InverseTransformPoint(chain[0].position);
        if (target == null)
        {
            ApplyWings(0f);
            return;
        }
        head.rotation = headRotation;
        Vector3 headOffset = beak.position - head.position;
        float requiredNeckLength = Vector3.Distance(chain[0].position, target.position - headOffset);
        float stretch = Mathf.Clamp(requiredNeckLength / (0.985f * Mathf.Max(0.001f, neckLength)),
            1f, player.NeckStretchLimit);
        // Use actual segment extension after chest lean, not distance to the nest.
        // The replicated movement state lets remote players fold wings on release too.
        float extraStretch = Mathf.Max(0f, stretch - 1f);
        float fullSpreadStretch = Mathf.Max(0.001f,
            (player.NeckStretchLimit - 1f) * player.WingSpreadStart);
        ApplyWings(player.IsMovingForWings
            ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(extraStretch / fullSpreadStretch)) : 0f);
        // Lengthen only the neck segments; the head and beak keep their size.
        for (int i = 1; i < chain.Length; i++) chain[i].localPosition = restPositions[i] * stretch;
        // CCD includes the head-to-beak segment, preserving the contact offset.
        for (int iteration = 0; iteration < 80; iteration++)
        {
            if ((beak.position - target.position).sqrMagnitude < 0.000001f) break;
            for (int i = chain.Length - 2; i >= 0; i--)
            {
                Vector3 from = head.position - chain[i].position;
                Vector3 to = target.position - headOffset - chain[i].position;
                if (from.sqrMagnitude < 0.0000001f || to.sqrMagnitude < 0.0000001f) continue;
                Quaternion delta = Quaternion.FromToRotation(from, to);
                chain[i].rotation = Quaternion.Slerp(Quaternion.identity, delta, 0.65f) * chain[i].rotation;
                head.rotation = headRotation;
            }
        }
    }

    private void ApplyWings(float wantedSpread)
    {
        wingSpread = Mathf.Lerp(wingSpread, wantedSpread,
            1f - Mathf.Exp(-player.WingSpreadSpeed * Time.deltaTime));
        leftWing?.Apply(wingSpread, player.MaximumWingSpread);
        rightWing?.Apply(wingSpread, player.MaximumWingSpread);
        leftTip?.Apply(wingSpread, player.WingTipSpread);
        rightTip?.Apply(wingSpread, player.WingTipSpread);
        foreach (FeatherPose feather in feathers) feather?.Apply(wingSpread, player.PrimaryFanSpread);
    }
}
