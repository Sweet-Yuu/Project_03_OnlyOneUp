using UnityEngine;

public interface INestEggCarryPhysics
{
    bool IsCarried { get; }
    void Reset(Pose nest, Vector3 restPosition);
    void Stop();
    void ReportJump(bool host, double time);
    void Step(Pose nest, float deltaTime);
}
