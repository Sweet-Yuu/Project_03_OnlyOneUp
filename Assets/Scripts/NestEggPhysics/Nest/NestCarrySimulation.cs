using UnityEngine;

// Host và client chạy cùng một phép tính, cùng bước thời gian và cùng trạng thái vận tốc.
public sealed class NestCarrySimulation : INestCarrySimulation
{
    private readonly INestCarryGeometry _geometry;
    private readonly float _responseTime;

    public NestCarrySimulation(INestCarryGeometry geometry, float responseTime)
    {
        _geometry = geometry;
        _responseTime = Mathf.Max(0.01f, responseTime);
    }

    public NestCarryState Step(NestCarryState state, NestCarryInput input, float deltaTime)
    {
        if (deltaTime <= 0f || !NestCarryPose.IsValidAxis(input.GuestAnchor - input.HostAnchor)) return state;
        Pose target = _geometry.Calculate(input.HostAnchor, input.GuestAnchor);
        state.Position = Vector3.SmoothDamp(state.Position, target.position, ref state.Velocity,
            _responseTime, Mathf.Infinity, deltaTime);
        state.Rotation = Quaternion.Slerp(state.Rotation, target.rotation,
            1f - Mathf.Exp(-deltaTime / _responseTime));
        return state;
    }
}
