using UnityEngine;

public struct NestCarryState
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Pose Pose => new Pose(Position, Rotation);

    public static NestCarryState FromFrame(NestEggFrame frame) => new NestCarryState
    {
        Position = frame.NestPosition,
        Rotation = frame.NestRotation,
        Velocity = frame.NestVelocity
    };
}
