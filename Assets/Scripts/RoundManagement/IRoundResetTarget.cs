using System;
using UnityEngine;

public interface IRoundResetTarget
{
    event Action RoundFailed;
    float GripDistance { get; }
    bool IsPlaying { get; }
    bool CanPlayerMove(uint sequence);
    bool TryRestartRound(Vector3 center);
}
