using System.Collections.Generic;
using UnityEngine;

// Nhận lại gói bị mất bằng các batch lặp; không chạy bù nhiều bước vật lý trong một tick.
public sealed class NestCarryInputBuffer
{
    public const int Capacity = 128;
    public const int BatchSize = 8;
    private readonly SortedDictionary<uint, NestCarryInput> _pending = new SortedDictionary<uint, NestCarryInput>();
    public uint Generation { get; private set; }
    public uint AppliedSequence { get; private set; }

    public void Reset(uint generation)
    {
        Generation = generation;
        AppliedSequence = 0;
        _pending.Clear();
    }

    public bool Add(NestCarryInput input)
    {
        if (input.Generation != Generation || input.Sequence <= AppliedSequence ||
            input.Sequence - AppliedSequence > Capacity || !Finite(input.GuestAnchor) ||
            _pending.ContainsKey(input.Sequence)) return false;
        _pending.Add(input.Sequence, input);
        return true;
    }

    public bool TryTake(out NestCarryInput input)
    {
        uint next = AppliedSequence + 1;
        if (!_pending.TryGetValue(next, out input)) return false;
        _pending.Remove(next);
        AppliedSequence = next;
        return true;
    }

    private static bool Finite(Vector3 value) =>
        !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
        !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
}
