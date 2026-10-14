using UnityEngine;

// Vật lý hỗ trợ trong hệ tọa độ tổ; khi hất ra thì trả quyền mô phỏng cho Rigidbody.
public sealed class NestEggCarryPhysics : INestEggCarryPhysics
{
    private readonly INestEggPhysicsBody _egg;
    private readonly CooperativeJumpWindow _jumps;
    private readonly float _travel;
    private readonly float _launchUp;
    private readonly float _launchSide;
    private readonly float _releaseTilt;
    private const float TiltExitClearance = 1.5f;
    private const float TiltExitSpeed = 6f;
    private float _exitHeight;
    private bool _exiting;
    private Vector3 _rest;
    private Vector3 _offset;
    private Vector3 _velocity;
    private Vector3 _previousNestVelocity;
    private Pose _previousNest;
    private bool _launchPending;
    public bool IsCarried { get; private set; }

    public NestEggCarryPhysics(INestEggPhysicsBody egg, float window, float travel,
        float launchUp, float launchSide, float releaseTilt = 70f)
    {
        _egg = egg;
        _jumps = new CooperativeJumpWindow(window);
        _travel = Mathf.Max(0f, travel);
        _releaseTilt = Mathf.Clamp(releaseTilt, 0f, 90f);
        _launchUp = Mathf.Max(0f, launchUp);
        _launchSide = Mathf.Max(0f, launchSide);
    }

    public void Reset(Pose nest, Vector3 restPosition)
    {
        Stop();
        _rest = restPosition;
        _previousNest = nest;
        _offset = _velocity = _previousNestVelocity = Vector3.zero;
        IsCarried = _egg.IsSimulating;
        if (IsCarried) _egg.BeginCarry(new Pose(nest.position + nest.rotation * _rest, nest.rotation));
    }

    public void Stop()
    {
        IsCarried = _launchPending = false;
        _exiting = false;
        _exitHeight = 0f;
        _jumps.Reset();
    }

    public void ReportJump(bool host, double time)
    {
        if (!IsCarried || !_egg.IsSimulating) return;
        if (_jumps.Add(host, time)) _launchPending = true;
    }

    public void Step(Pose nest, float deltaTime)
    {
        if (!IsCarried || !_egg.IsSimulating || deltaTime <= 0f) return;
        Vector3 nestVelocity = (nest.position - _previousNest.position) / deltaTime;
        bool tipped = Vector3.Angle(nest.rotation * Vector3.up, Vector3.up) > _releaseTilt + .001f;
        _exiting |= tipped;
        // Chỉ vượt thành sau khi tổ quá nghiêng. Lúc còn an toàn, trứng luôn lăn
        // ngang tại đáy; không nâng theo khoảng cách tới viền như trước.
        // Di chuyển từng bước khi thoát để không teleport trứng lên miệng tổ.
        if (_exiting && !_launchPending && _exitHeight < TiltExitClearance)
        {
            _exitHeight = Mathf.MoveTowards(_exitHeight, TiltExitClearance, TiltExitSpeed * deltaTime);
            _egg.MoveCarried(new Pose(nest.position + nest.rotation *
                (_rest + _offset + Vector3.up * _exitHeight), nest.rotation * RollRotation()));
            _previousNest = nest;
            _previousNestVelocity = nestVelocity;
            return;
        }
        if (_launchPending || _exiting)
        {
            // Kế thừa vận tốc tại điểm đặt trứng, gồm cả chuyển động quay của tổ.
            Vector3 local = _rest + _offset + Vector3.up * _exitHeight;
            Vector3 pointVelocity = (nest.position + nest.rotation * local -
                (_previousNest.position + _previousNest.rotation * local)) / deltaTime;
            Vector3 side = Vector3.ProjectOnPlane(nest.rotation * Vector3.right, Vector3.up).normalized;
            // Quá góc an toàn: giữ đà lăn, hỗ trợ vượt thành collider sâu của tổ.
            // Hướng ra miệng tổ đã nghiêng nên trứng thoát xuống phía thấp, không nhảy thẳng lên trời.
            Vector3 velocity = Vector3.ClampMagnitude(pointVelocity, 30f) + nest.rotation * _velocity;
            Vector3 spin = nest.rotation * Vector3.Cross(Vector3.up, _velocity);
            if (_launchPending)
            {
                velocity += Vector3.up * _launchUp + side * _launchSide;
                spin += nest.rotation * Vector3.forward * -3f;
            }
            else
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Physics.gravity, nest.rotation * Vector3.up).normalized;
                velocity += nest.rotation * Vector3.up * TiltExitSpeed + downhill * 3f;
            }
            _egg.ReleaseCarry(velocity, spin);
            Stop();
            return;
        }

        // Không kéo trứng về tâm bằng lò xo: trọng lực dọc sàn đưa trứng tới viền thấp.
        // Giữ một phần quán tính và lực cản để cú đáp đất không tạo dao động quá mạnh.
        Vector3 acceleration = (nestVelocity - _previousNestVelocity) / deltaTime;
        Vector3 localForce = Quaternion.Inverse(nest.rotation) * (Physics.gravity - acceleration * 0.08f);
        localForce.y = 0f;
        localForce = Vector3.ClampMagnitude(localForce, 6f);
        float remaining = Mathf.Min(deltaTime, 0.1f);
        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, 0.01f);
            _velocity += (localForce - _velocity * 1.5f) * step;
            _offset += _velocity * step;
            if (_offset.magnitude > _travel)
            {
                _offset = Vector3.ClampMagnitude(_offset, _travel);
                Vector3 normal = _offset.normalized;
                _velocity -= normal * Mathf.Max(0f, Vector3.Dot(_velocity, normal));
            }
            remaining -= step;
        }
        // Góc lăn bị giới hạn để hình capsule không xuyên sàn khi trứng nghiêng.
        _egg.MoveCarried(new Pose(nest.position + nest.rotation * (_rest + _offset), nest.rotation * RollRotation()));
        _previousNest = nest;
        _previousNestVelocity = nestVelocity;
    }

    private Quaternion RollRotation() => Quaternion.Euler(_offset.z * 22f, 0f, -_offset.x * 22f);
}
