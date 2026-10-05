using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum NestEggPhase : byte { Waiting, Intact, Broken }

// A single timestamp describes BOTH bodies. Do not interpolate each body on
// a different NetworkTransform timeline: that makes the egg slide through the tray.
public struct NestEggFrame : INetworkSerializable, IEquatable<NestEggFrame>
{
    public double Time;
    public uint Generation;
    public NestEggPhase Phase;
    public Vector3 NestPosition;
    public Quaternion NestRotation;
    public Vector3 EggPosition;
    public Quaternion EggRotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Time);
        serializer.SerializeValue(ref Generation);
        serializer.SerializeValue(ref Phase);
        serializer.SerializeValue(ref NestPosition);
        serializer.SerializeValue(ref NestRotation);
        serializer.SerializeValue(ref EggPosition);
        serializer.SerializeValue(ref EggRotation);
    }

    public bool Equals(NestEggFrame other) => Time.Equals(other.Time) &&
        Generation == other.Generation && Phase == other.Phase &&
        NestPosition.Equals(other.NestPosition) && NestRotation.Equals(other.NestRotation) &&
        EggPosition.Equals(other.EggPosition) && EggRotation.Equals(other.EggRotation);
}

public sealed class NestEggFrameBuffer
{
    private readonly List<NestEggFrame> frames = new List<NestEggFrame>(32);
    public int Count => frames.Count;
    public void Clear() => frames.Clear();

    public bool Add(NestEggFrame frame)
    {
        if (frame.Generation == 0) return false;
        if (frames.Count > 0)
        {
            NestEggFrame last = frames[frames.Count - 1];
            if (frame.Generation < last.Generation) return false;
            if (frame.Generation == last.Generation && frame.Time <= last.Time) return false;
            if (frame.Generation != last.Generation) frames.Clear();
        }
        frames.Add(frame);
        if (frames.Count > 32) frames.RemoveAt(0);
        return true;
    }

    public bool Sample(double time, out NestEggFrame result)
    {
        result = default;
        if (frames.Count == 0) return false;
        while (frames.Count > 2 && frames[1].Time <= time) frames.RemoveAt(0);
        if (time <= frames[0].Time || frames.Count == 1)
        {
            result = frames[0];
            return true;
        }
        NestEggFrame a = frames[0];
        NestEggFrame b = frames[1];
        if (time >= b.Time)
        {
            // Hold the latest complete frame during packet stalls; no guessed physics.
            result = b;
            return true;
        }
        float t = (float)((time - a.Time) / (b.Time - a.Time));
        result = a;
        result.Time = time;
        result.NestPosition = Vector3.Lerp(a.NestPosition, b.NestPosition, t);
        result.NestRotation = Quaternion.Slerp(a.NestRotation, b.NestRotation, t);
        result.EggPosition = Vector3.Lerp(a.EggPosition, b.EggPosition, t);
        result.EggRotation = Quaternion.Slerp(a.EggRotation, b.EggRotation, t);
        return true;
    }
}
