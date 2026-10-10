using System.Collections.Generic;
using UnityEngine;

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

    public bool Sample(double time, out NestEggFrame result, bool relativeEgg = false)
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
        if (relativeEgg && ((a.EggInNest && b.EggInNest) || (a.EggSupported && b.EggSupported)) &&
            a.Phase == NestEggPhase.Intact && b.Phase == NestEggPhase.Intact)
        {
            // Nội suy trứng trong hệ tọa độ tổ để không cắt ngang sàn khi tổ xoay.
            Quaternion inverseA = Quaternion.Inverse(a.NestRotation);
            Quaternion inverseB = Quaternion.Inverse(b.NestRotation);
            Vector3 localPosition = Vector3.Lerp(inverseA * (a.EggPosition - a.NestPosition),
                inverseB * (b.EggPosition - b.NestPosition), t);
            Quaternion localRotation = Quaternion.Slerp(inverseA * a.EggRotation,
                inverseB * b.EggRotation, t);
            result.EggPosition = result.NestPosition + result.NestRotation * localPosition;
            result.EggRotation = result.NestRotation * localRotation;
        }
        return true;
    }
}
