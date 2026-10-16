using UnityEngine;

// Presentation only. Network state is supplied by NetworkTestMovement.
public sealed class PlayerAnimationPresenter
{
    private readonly Animator _animator;
    private readonly Object _context;
    private readonly Transform[] _transforms;
    private readonly Vector3[] _positions;
    private readonly Quaternion[] _rotations;
    private readonly Vector3[] _scales;
    private static readonly int Walk = Animator.StringToHash("Base Layer.Walk");
    private static readonly int Walking = Animator.StringToHash("Base Layer.Walking");
    private static readonly int SideWalk = Animator.StringToHash("Base Layer.Side_Walk");
    private bool _walking;
    private bool _sideWalking;
    private int _activeWalkState;

    public void Tick(float moveSpeed, float referenceSpeed, float response, float maxPlayback, float deltaTime,
        bool sideWalking = false)
    {
        if (_animator == null) return;
        if (float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed)) moveSpeed = 0f;
        _sideWalking = sideWalking;
        bool walking = moveSpeed > (_walking ? 0.05f : 0.15f);
        if (walking != _walking)
            Show(walking ? OnlineMovementState.Walk : OnlineMovementState.Idle, 0f);
        if (!_walking) return;
        int desiredState = ResolveWalkState();
        if (desiredState != 0 && desiredState != _activeWalkState)
        {
            // Giữ pha bước chân khi đổi hướng để tránh bắt đầu lại mỗi lần nhấn phím.
            float phase = Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
            _animator.CrossFade(desiredState, 0.1f, 0, phase);
            _activeWalkState = desiredState;
        }
        float target = Mathf.Clamp(moveSpeed / Mathf.Max(0.01f, referenceSpeed), 0f, maxPlayback);
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, response) * Mathf.Max(0f, deltaTime));
        _animator.speed = Mathf.Lerp(_animator.speed, target, blend);
    }

    public PlayerAnimationPresenter(Animator animator, Object context)
    {
        _animator = animator;
        _context = context;
        _transforms = animator != null ? animator.GetComponentsInChildren<Transform>(true) : new Transform[0];
        _positions = new Vector3[_transforms.Length];
        _rotations = new Quaternion[_transforms.Length];
        _scales = new Vector3[_transforms.Length];
        // Lưu tư thế prefab trong Awake, trước khi Animator và IK bắt đầu cập nhật.
        for (int i = 0; i < _transforms.Length; i++)
        {
            _positions[i] = _transforms[i].localPosition;
            _rotations[i] = _transforms[i].localRotation;
            _scales[i] = _transforms[i].localScale;
        }
        if (_animator != null)
        {
            _animator.applyRootMotion = false;
            _animator.enabled = false;
        }
    }

    public void Validate()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            Debug.LogWarning("Gán Animator và controller chứa clip walking cho player. Không cần clip idle.", _context);
        else if (!_animator.HasState(0, SideWalk))
            Debug.LogWarning("Controller cần state Base Layer.Side_Walk để phát animation đi ngang.", _context);
    }

    private int ResolveWalkState()
    {
        if (_sideWalking && _animator.HasState(0, SideWalk)) return SideWalk;
        if (_animator.HasState(0, Walking)) return Walking;
        return _animator.HasState(0, Walk) ? Walk : 0;
    }

    public void Show(OnlineMovementState state, float fade)
    {
        if (_animator == null) return;
        if (state == OnlineMovementState.Walk && _animator.runtimeAnimatorController != null)
        {
            if (_walking) return;
            _walking = true;
            _animator.enabled = true;
            _animator.speed = 0f;
            // Chọn đúng clip ngay từ lúc đứng yên bắt đầu đi, không qua clip mặc định.
            _activeWalkState = ResolveWalkState();
            if (_activeWalkState != 0) _animator.Play(_activeWalkState, 0, 0f);
            else
            {
                _animator.Rebind();
                _animator.Update(0f);
            }
            return;
        }

        _walking = false;
        _activeWalkState = 0;
        _animator.enabled = false;
        _animator.speed = 0f;
        // Không giữ nguyên frame bước chân cuối. IK cổ/cánh vẫn chạy ở LateUpdate.
        for (int i = 0; i < _transforms.Length; i++)
        {
            if (_transforms[i] == null) continue;
            _transforms[i].localPosition = _positions[i];
            _transforms[i].localRotation = _rotations[i];
            _transforms[i].localScale = _scales[i];
        }
    }
}
