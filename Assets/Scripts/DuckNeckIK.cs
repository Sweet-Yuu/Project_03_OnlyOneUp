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
    private NetworkTestMovement player;
    private NetworkNest nest;
    public bool Ready { get; private set; }
    public float Reach { get; private set; }
    public float MaximumReach => neckLength * player.NeckStretchLimit + headLength;
    public Vector3 RootLocal { get; private set; }

    private void Awake()
    {
        player = GetComponent<NetworkTestMovement>();
        Transform neck = Find(transform, "Neck_01");
        if (neck == null) return;
        Transform n2 = neck.Find("Neck_02");
        Transform n3 = n2 != null ? n2.Find("Neck_03") : null;
        Transform head = n3 != null ? n3.Find("Head") : null;
        if (head == null) return;
        beak = head.Find("BreakHeadGrip") ?? head.Find("BeakGripPoint");
        if (beak == null)
        {
            Debug.LogWarning("Duck IK: create BreakHeadGrip or BeakGripPoint under the Head bone.", this);
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
        headLength = Vector3.Distance(head.position, beak.position);
        neckLength = Reach - headLength;
        Ready = Reach > 0.001f;
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root)
        {
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
        // Reset each solve to avoid accumulating twist across frames.
        for (int i = 0; i < chain.Length; i++)
        {
            chain[i].localRotation = rest[i];
            chain[i].localPosition = restPositions[i];
        }
        if (nest == null || !nest.CarryReady) return;
        Transform target = nest.GetGrip(player.OwnerClientId);
        if (target == null) return;
        float distance = Vector3.Distance(chain[0].position, target.position);
        float stretch = Mathf.Clamp((distance / 0.985f - headLength) / Mathf.Max(0.001f, neckLength),
            1f, player.NeckStretchLimit);
        // Lengthen only the neck segments; the head and beak keep their size.
        for (int i = 1; i < chain.Length; i++) chain[i].localPosition = restPositions[i] * stretch;
        // CCD includes the head-to-beak segment, preserving the contact offset.
        for (int iteration = 0; iteration < 80; iteration++)
        {
            if ((beak.position - target.position).sqrMagnitude < 0.000001f) break;
            for (int i = chain.Length - 1; i >= 0; i--)
            {
                Vector3 from = beak.position - chain[i].position;
                Vector3 to = target.position - chain[i].position;
                if (from.sqrMagnitude < 0.0000001f || to.sqrMagnitude < 0.0000001f) continue;
                Quaternion delta = Quaternion.FromToRotation(from, to);
                chain[i].rotation = Quaternion.Slerp(Quaternion.identity, delta, 0.65f) * chain[i].rotation;
            }
        }
    }
}
