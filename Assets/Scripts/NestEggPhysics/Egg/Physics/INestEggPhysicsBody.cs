using UnityEngine;

public interface INestEggPhysicsBody
{
    bool IsSimulating { get; }
    void BeginCarry(Pose pose);
    void MoveCarried(Pose pose);
    void ReleaseCarry(Vector3 velocity, Vector3 angularVelocity);
}
