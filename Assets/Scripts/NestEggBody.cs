using System;
using UnityEngine;

// Not a separate NetworkObject: NetworkNest replicates this body's pose together
// with the tray in one frame. Only the server ever simulates this Rigidbody.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class NestEggBody : MonoBehaviour
{
    public const float Height =  3.06f;
    public Rigidbody Body { get; private set; }
    public event Action Broken;
    public bool IsSimulating => authority && phase == NestEggPhase.Intact;
    public bool IsSupportedByNest => IsSimulating && !float.IsNegativeInfinity(_lastNestSupportTime) &&
        (Body.IsSleeping() || Time.fixedTime - _lastNestSupportTime <= Time.fixedDeltaTime * 1.5f);
    private float _lastNestSupportTime = float.NegativeInfinity;

    private Transform nest;
    private Collider shell;
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private bool authority;
    private NestEggPhase phase;
    private float ignoreImpactUntil;
    private bool presentationInitialized;

    public void Initialize(Transform nestRoot, GameObject visualPrefab)
    {
        // This wraps a native Unity object: create it on the main thread after
        // AddComponent has completed, never in a MonoBehaviour field initializer.
        properties = new MaterialPropertyBlock();
        nest = nestRoot;
        Body = GetComponent<Rigidbody>();
        Body.isKinematic = true;
        Body.useGravity = false;
        Body.mass = 0.5f;
        Body.linearDamping = 0.05f;
        Body.angularDamping = 0.1f;
        Body.solverIterations = 12;
        Body.solverVelocityIterations = 4;
        var capsule = GetComponent<CapsuleCollider>();
        capsule.radius = 0.99f;
        capsule.height = Height;
        capsule.direction = 1;
        shell = capsule;
        shell.enabled = false;

        GameObject model;
        if (visualPrefab != null)
        {
            model = Instantiate(visualPrefab, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.transform.SetParent(transform, false);
        }
        model.name = "EggVisual";
        foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody b in model.GetComponentsInChildren<Rigidbody>(true))
        {
            b.isKinematic = true;
            b.detectCollisions = false;
        }
        foreach (Camera c in model.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        foreach (AudioListener a in model.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
        foreach (Light light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
        renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            float scale = Mathf.Min(Height / Mathf.Max(bounds.size.y, 0.001f),
                1.98f / Mathf.Max(bounds.size.x, bounds.size.z, 0.001f));
            Vector3 center = transform.InverseTransformPoint(bounds.center);
            model.transform.localScale *= scale;
            model.transform.localPosition = -center * scale;
        }
        SetPresentation(NestEggPhase.Waiting);
    }

    public void SetAuthority(bool server)
    {
        authority = server;
        Body.interpolation = server ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        Freeze();
    }

    private void Freeze()
    {
        _lastNestSupportTime = float.NegativeInfinity;
        if (!Body.isKinematic)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }
        Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        Body.isKinematic = true;
        Body.useGravity = false;
        shell.enabled = false;
    }

    public void WaitForPlayers()
    {
        Freeze();
        SetPresentation(NestEggPhase.Waiting);
    }

    public void ResetOnServer(Vector3 position, Quaternion rotation)
    {
        if (!authority) return;
        Freeze();
        Body.position = position;
        Body.rotation = rotation;
        Body.isKinematic = false;
        Body.useGravity = true;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        shell.enabled = true;
        ignoreImpactUntil = Time.time + 0.35f;
        SetPresentation(NestEggPhase.Intact);
        Body.WakeUp();
    }

    public void IgnorePlayer(GameObject player)
    {
        foreach (Collider c in player.GetComponentsInChildren<Collider>(true))
            Physics.IgnoreCollision(shell, c);
    }

    private void OnCollisionEnter(Collision collision)
    {
        RecordNestSupport(collision);
        if (!IsSimulating || Time.time < ignoreImpactUntil) return;
        // The nest is safe at any impact speed. Landing on the environment
        // (ground, obstacles, etc.) breaks the egg; only the server decides.
        if (!collision.collider.transform.IsChildOf(nest))
            BreakOnServer();
    }

    private void OnCollisionStay(Collision collision)
    {
        RecordNestSupport(collision);
        // A ground contact may begin during the short spawn grace period.
        if (IsSimulating && Time.time >= ignoreImpactUntil &&
            !collision.collider.transform.IsChildOf(nest)) BreakOnServer();
    }

    private void RecordNestSupport(Collision collision)
    {
        if (!IsSimulating || !collision.collider.transform.IsChildOf(nest)) return;
        // Chỉ tiếp xúc nâng đỡ mới kéo hình ảnh trứng theo tổ; chạm thành bên không đủ.
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > 0.5f)
            {
                _lastNestSupportTime = Time.fixedTime;
                return;
            }
        }
    }

    public void BreakOnServer()
    {
        if (!IsSimulating) return;
        Freeze();
        SetPresentation(NestEggPhase.Broken);
        Broken?.Invoke();
    }

    public void ApplyRemote(NestEggFrame frame)
    {
        if (authority) return;
        transform.SetPositionAndRotation(frame.EggPosition, frame.EggRotation);
        SetPresentation(frame.Phase);
    }

    private void SetPresentation(NestEggPhase next)
    {
        if (presentationInitialized && phase == next) return;
        presentationInitialized = true;
        phase = next;
        if (renderers == null) return;
        foreach (Renderer r in renderers)
        {
            r.enabled = next != NestEggPhase.Waiting;
            if (next == NestEggPhase.Broken)
            {
                properties.Clear();
                properties.SetColor("_BaseColor", new Color(1f, 0.15f, 0.1f));
                properties.SetColor("_Color", new Color(1f, 0.15f, 0.1f));
                r.SetPropertyBlock(properties);
            }
            else r.SetPropertyBlock(null);
        }
    }
}
