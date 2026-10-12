using System.Collections.Generic;
using UnityEngine;

public sealed class NestCarryPrediction
{
    private readonly INestCarrySimulation _simulation;
    private readonly List<NestCarryInput> _inputs = new List<NestCarryInput>();
    private readonly List<NestCarryState> _states = new List<NestCarryState>();
    private uint _nextSequence;
    private uint _acknowledged;
    private double _lastFrameTime;
    private int _resendCursor;
    public uint Generation { get; private set; }
    public NestCarryState Current { get; private set; }
    public int PendingCount => _inputs.Count;
    public float StepDuration { get; private set; }
    public float LastMatchedError { get; private set; }

    public NestCarryPrediction(INestCarrySimulation simulation) => _simulation = simulation;

    public void Reset(NestEggFrame frame)
    {
        Generation = frame.Generation;
        _acknowledged = frame.AppliedCarrySequence;
        _nextSequence = _acknowledged + 1;
        _lastFrameTime = frame.Time;
        StepDuration = Mathf.Clamp(frame.CarryStepDuration, 0.005f, 0.1f);
        Current = NestCarryState.FromFrame(frame);
        _inputs.Clear();
        _states.Clear();
        LastMatchedError = 0f;
        _resendCursor = 0;
    }

    public bool Predict(Vector3 hostAnchor, Vector3 guestAnchor)
    {
        if (Generation == 0 || _inputs.Count >= NestCarryInputBuffer.Capacity) return false;
        var input = new NestCarryInput
        {
            Generation = Generation, Sequence = _nextSequence++,
            HostAnchor = hostAnchor, GuestAnchor = guestAnchor
        };
        Current = _simulation.Step(Current, input, StepDuration);
        _inputs.Add(input);
        _states.Add(Current);
        return true;
    }

    public NestCarryInput[] GetResendBatch()
    {
        int count = Mathf.Min(_inputs.Count, NestCarryInputBuffer.BatchSize);
        var batch = new NestCarryInput[count];
        if (count == 0) return batch;
        batch[0] = _inputs[0];
        // Luôn gửi lại đầu vào đang chặn hàng đợi, các phần còn lại quay vòng.
        // Không giới hạn cửa sổ gửi ở 8 input khi RTT dài hơn 8 tick.
        for (int i = 1; i < count; i++)
            batch[i] = _inputs[1 + _resendCursor++ % (_inputs.Count - 1)];
        _resendCursor %= Mathf.Max(1, _inputs.Count - 1);
        return batch;
    }

    public bool Reconcile(NestEggFrame frame)
    {
        if (frame.Generation != Generation || frame.Time <= _lastFrameTime ||
            frame.AppliedCarrySequence < _acknowledged || frame.AppliedCarrySequence >= _nextSequence) return false;
        NestCarryState authority = NestCarryState.FromFrame(frame);
        int removeCount = 0;
        for (int i = 0; i < _inputs.Count && _inputs[i].Sequence <= frame.AppliedCarrySequence; i++)
        {
            // Đối chiếu đúng input host vừa áp dụng, không so với pose mới nhất của client.
            if (_inputs[i].Sequence == frame.AppliedCarrySequence)
                LastMatchedError = Vector3.Distance(_states[i].Position, authority.Position);
            removeCount++;
        }
        _inputs.RemoveRange(0, removeCount);
        _states.RemoveRange(0, removeCount);
        _acknowledged = frame.AppliedCarrySequence;
        _lastFrameTime = frame.Time;
        Current = authority;
        for (int i = 0; i < _inputs.Count; i++)
        {
            Current = _simulation.Step(Current, _inputs[i], StepDuration);
            _states[i] = Current;
        }
        return true;
    }

    // Hiển thị phần lẻ giữa hai tick mà không thêm input hoặc thay đổi lịch sử.
    public Pose Preview(Vector3 hostAnchor, Vector3 guestAnchor, float deltaTime) =>
        _simulation.Step(Current, new NestCarryInput { HostAnchor = hostAnchor, GuestAnchor = guestAnchor },
            Mathf.Clamp(deltaTime, 0f, StepDuration)).Pose;
}
