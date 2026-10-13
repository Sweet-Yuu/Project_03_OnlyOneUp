using UnityEngine;

// Lọc chuyển động trứng trong một hệ tọa độ tại một thời điểm, không trộn hai quỹ đạo.
public sealed class NestEggPresentation
{
    private const float SmoothTime = 0.06f;
    private const float MaxSpeed = 20f;
    private bool _initialized;
    private bool _attached;
    private uint _generation;
    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _velocity;
    private Pose _shown;

    public void Reset(NestEggFrame frame)
    {
        _initialized = true;
        _generation = frame.Generation;
        _attached = frame.Phase == NestEggPhase.Intact && frame.EggInNest;
        _shown = new Pose(frame.EggPosition, frame.EggRotation);
        SetCoordinates(new Pose(frame.NestPosition, frame.NestRotation));
    }

    private void SetCoordinates(Pose nest)
    {
        Quaternion inverse = Quaternion.Inverse(nest.rotation);
        _position = _attached ? inverse * (_shown.position - nest.position) : _shown.position;
        _rotation = _attached ? inverse * _shown.rotation : _shown.rotation;
        _velocity = Vector3.zero;
    }

    public NestEggFrame Present(NestEggFrame frame, Pose nest, float deltaTime)
    {
        if (!_initialized || _generation != frame.Generation) Reset(frame);
        deltaTime = Mathf.Max(0f, deltaTime);
        // Chỉ host xác nhận đã ra khỏi vùng chứa mới đổi hệ tọa độ.
        // Không dùng mất tiếp xúc hoặc hết thời gian chờ làm bằng chứng trứng đã rơi.
        bool attached = frame.Phase == NestEggPhase.Intact && frame.EggInNest;
        if (attached != _attached)
        {
            _attached = attached;
            // Giữ đúng pose đang thấy khi đổi hệ tọa độ, kể cả khi tổ lệch host vài mét.
            SetCoordinates(nest);
        }

        Quaternion inverse = Quaternion.Inverse(frame.NestRotation);
        Vector3 targetPosition = _attached
            ? inverse * (frame.EggPosition - frame.NestPosition) : frame.EggPosition;
        Quaternion targetRotation = _attached ? inverse * frame.EggRotation : frame.EggRotation;
        if (deltaTime > 0f)
        {
            Vector3 next = Vector3.SmoothDamp(_position, targetPosition, ref _velocity,
                SmoothTime, MaxSpeed, deltaTime);
            _position = Vector3.MoveTowards(_position, next, MaxSpeed * deltaTime);
            _rotation = Quaternion.RotateTowards(_rotation,
                Quaternion.Slerp(_rotation, targetRotation, 1f - Mathf.Exp(-deltaTime / SmoothTime)),
                540f * deltaTime);
        }
        _shown = _attached
            ? new Pose(nest.position + nest.rotation * _position, nest.rotation * _rotation)
            : new Pose(_position, _rotation);
        frame.EggPosition = _shown.position;
        frame.EggRotation = _shown.rotation;
        return frame;
    }
}
