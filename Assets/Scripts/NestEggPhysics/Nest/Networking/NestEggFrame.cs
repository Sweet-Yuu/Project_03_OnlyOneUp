using System;
using Unity.Netcode;
using UnityEngine;

// A single timestamp describes BOTH bodies. Do not interpolate each body on
// a different NetworkTransform timeline: that makes the egg slide through the tray.
public struct NestEggFrame : INetworkSerializable, IEquatable<NestEggFrame>
{
    public double Time;
    public uint Generation;
    public NestEggPhase Phase;
    public byte EggDamage;
    public uint ImpactSequence;
    public bool EggSupported;
    public bool EggInNest;
    public uint AppliedCarrySequence;
    public float CarryStepDuration;
    public Vector3 NestVelocity;
    public Vector3 NestPosition;
    public Quaternion NestRotation;
    public Vector3 EggPosition;
    public Vector3 EggVelocity;
    public Quaternion EggRotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Time);
        serializer.SerializeValue(ref Generation);
        serializer.SerializeValue(ref Phase);
        serializer.SerializeValue(ref EggDamage);
        serializer.SerializeValue(ref ImpactSequence);
        serializer.SerializeValue(ref EggSupported);
        serializer.SerializeValue(ref EggInNest);
        serializer.SerializeValue(ref AppliedCarrySequence);
        serializer.SerializeValue(ref CarryStepDuration);
        serializer.SerializeValue(ref NestVelocity);
        serializer.SerializeValue(ref NestPosition);
        serializer.SerializeValue(ref NestRotation);
        serializer.SerializeValue(ref EggPosition);
        serializer.SerializeValue(ref EggVelocity);
        serializer.SerializeValue(ref EggRotation);
    }

    public bool Equals(NestEggFrame other) => Time.Equals(other.Time) &&
        Generation == other.Generation && Phase == other.Phase && EggSupported == other.EggSupported &&
        EggInNest == other.EggInNest && EggDamage == other.EggDamage && ImpactSequence == other.ImpactSequence &&
        AppliedCarrySequence == other.AppliedCarrySequence && CarryStepDuration.Equals(other.CarryStepDuration) &&
        NestVelocity.Equals(other.NestVelocity) &&
        NestPosition.Equals(other.NestPosition) && NestRotation.Equals(other.NestRotation) &&
        EggPosition.Equals(other.EggPosition) && EggVelocity.Equals(other.EggVelocity) && EggRotation.Equals(other.EggRotation);
}
