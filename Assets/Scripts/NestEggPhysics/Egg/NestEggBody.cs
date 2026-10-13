using System;
using System.Collections.Generic;
using UnityEngine;

// Not a separate NetworkObject: NetworkNest replicates this body's pose together
// with the tray in one frame. Only the server ever simulates this Rigidbody.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class NestEggBody : MonoBehaviour, INestEggPhysicsBody
{
    public const float Height =  3.06f;
    public Rigidbody Body { get; private set; }
    public event Action Broken;
    public byte DamageCount { get; private set; }
    public bool IsSimulating => authority && phase == NestEggPhase.Intact;
    public bool IsSupportedByNest => IsSimulating && (_carried ||
        (!float.IsNegativeInfinity(_lastNestSupportTime) &&
        (Body.IsSleeping() || Time.fixedTime - _lastNestSupportTime <= Time.fixedDeltaTime * 1.5f)));
    private float _lastNestSupportTime = float.NegativeInfinity;
    private bool _carried;
    private Collider[] _overlaps = new Collider[32];
    private readonly HashSet<Collider> _ignoredPlayerColliders = new HashSet<Collider>();
    private readonly HashSet<Collider> _environmentContacts = new HashSet<Collider>();
    private readonly HashSet<Collider> _currentContacts = new HashSet<Collider>();
    private bool _progressiveDamage;
    private float _lastImpactTime = float.NegativeInfinity;
    private NestEggDamageVisual _damageVisual;

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

    public void ConfigureProgressiveDamage(GameObject brokenVisual)
    {
        _progressiveDamage = true;
        _damageVisual?.Dispose();
        _damageVisual = new NestEggDamageVisual(transform, renderers, brokenVisual);
        _damageVisual.Show(phase, DamageCount);
    }

    public void SetAuthority(bool server)
    {
        authority = server;
        Body.interpolation = server ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        Freeze();
    }

    private void Freeze()
    {
        _carried = false;
        _environmentContacts.Clear();
        _currentContacts.Clear();
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
        _ignoredPlayerColliders.Clear();
        _lastImpactTime = float.NegativeInfinity;
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
        {
            _ignoredPlayerColliders.Add(c);
            Physics.IgnoreCollision(shell, c);
        }
    }

    public void BeginCarry(Pose pose)
    {
        if (!IsSimulating) return;
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        Body.isKinematic = true;
        Body.useGravity = false;
        Body.position = pose.position;
        Body.rotation = pose.rotation;
        _carried = true;
    }

    public void MoveCarried(Pose pose)
    {
        if (!IsSimulating || !_carried) return;
        Body.MovePosition(pose.position);
        Body.MoveRotation(pose.rotation);
        if (Time.time < ignoreImpactUntil) return;
        // Kinematic không phát contact với vật tĩnh: kiểm tra capsule để vẫn vỡ
        // khi va môi trường; chỉ bỏ qua tổ và collider của hai người đang mang.
        Vector3 axis = pose.rotation * Vector3.up * (Height * 0.5f - 0.99f);
        int count;
        do
        {
            count = Physics.OverlapCapsuleNonAlloc(pose.position - axis, pose.position + axis,
                0.99f, _overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count < _overlaps.Length) break;
            Array.Resize(ref _overlaps, _overlaps.Length * 2);
        } while (true);
        _currentContacts.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider other = _overlaps[i];
            if (!IsEnvironment(other)) continue;
            _currentContacts.Add(other);
            TouchEnvironment(other);
            if (!IsSimulating) break;
        }
        _environmentContacts.IntersectWith(_currentContacts);
    }

    public void ReleaseCarry(Vector3 velocity, Vector3 angularVelocity)
    {
        if (!IsSimulating || !_carried) return;
        _carried = false;
        _lastNestSupportTime = float.NegativeInfinity;
        Body.isKinematic = false;
        Body.useGravity = true;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Body.linearVelocity = velocity;
        Body.angularVelocity = angularVelocity;
        Body.WakeUp();
    }

    private void OnCollisionEnter(Collision collision)
    {
        RecordNestSupport(collision);
        if (!_carried) TouchEnvironment(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        RecordNestSupport(collision);
        // Contact bắt đầu trong thời gian bảo vệ spawn vẫn được tính đúng một lần.
        if (!_carried) TouchEnvironment(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!_carried) _environmentContacts.Remove(collision.collider);
    }

    private bool IsEnvironment(Collider other) => other != null && other != shell &&
        !other.isTrigger && !other.transform.IsChildOf(nest) &&
        !_ignoredPlayerColliders.Contains(other) &&
        !Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer);

    private void TouchEnvironment(Collider other)
    {
        if (!IsSimulating || Time.time < ignoreImpactUntil || !IsEnvironment(other)) return;
        bool sameObject = false;
        foreach (Collider contact in _environmentContacts)
            if (contact != null && (contact.gameObject == other.gameObject ||
                (other.attachedRigidbody != null && contact.attachedRigidbody == other.attachedRigidbody)))
            { sameObject = true; break; }
        if (!_environmentContacts.Add(other) || sameObject) return;
        // Gộp các contact trong cùng bước vật lý; tì liên tục không mất thêm lượt.
        if (Time.fixedTime == _lastImpactTime) return;
        _lastImpactTime = Time.fixedTime;
        if (!_progressiveDamage || DamageCount >= 2)
        {
            Vector3 surface = other.ClosestPoint(Body.position);
            BreakOnServer();
            if (_progressiveDamage)
            {
                // Đặt model lòng trắng/lòng đỏ trên điểm va chạm thay vì treo ở tâm vỏ.
                Body.position = surface + Vector3.up * (Height * .5f);
                Body.rotation = Quaternion.identity;
            }
        }
        else SetPresentation(NestEggPhase.Intact, (byte)(DamageCount + 1));
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

    public void DamageFromNestImpact()
    {
        if (!IsSimulating) return;
        if (!_progressiveDamage || DamageCount >= 2) BreakOnServer();
        else SetPresentation(NestEggPhase.Intact, (byte)(DamageCount + 1));
    }

    public void FallFromNestImpact(Vector3 velocity)
    {
        if (!IsSimulating) return;
        // Không reset trứng: giữ nguyên số vết nứt khi bị hất khỏi tổ.
        _carried = true;
        ReleaseCarry(velocity, Vector3.Cross(Vector3.up, velocity));
    }

    public void BreakOnServer()
    {
        if (!IsSimulating) return;
        Freeze();
        SetPresentation(NestEggPhase.Broken, 3);
        Broken?.Invoke();
    }

    public void ApplyRemote(NestEggFrame frame)
    {
        if (authority) return;
        transform.SetPositionAndRotation(frame.EggPosition, frame.EggRotation);
        SetPresentation(frame.Phase, frame.EggDamage);
    }

    private void SetPresentation(NestEggPhase next, byte damage = 0)
    {
        damage = next == NestEggPhase.Broken ? (byte)3 : next == NestEggPhase.Waiting ? (byte)0 : (byte)Mathf.Min(damage, 2);
        if (presentationInitialized && phase == next && DamageCount == damage) return;
        presentationInitialized = true;
        phase = next;
        DamageCount = damage;
        if (_damageVisual != null)
        {
            _damageVisual.Show(next, damage);
            return;
        }
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

    private void OnDestroy() => _damageVisual?.Dispose();
}
