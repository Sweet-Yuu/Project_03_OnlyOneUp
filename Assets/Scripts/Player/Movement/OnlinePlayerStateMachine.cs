using UnityEngine;

public enum OnlineMovementState : byte { Idle, Walk }

// Plain C# states: no Rigidbody, network calls or remote input here.
public abstract class OnlinePlayerState
{
    public abstract OnlineMovementState Kind { get; }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract Vector2 Tick(OnlinePlayerStateMachine context);
}

public sealed class OnlineIdleState : OnlinePlayerState
{
    public override OnlineMovementState Kind => OnlineMovementState.Idle;
    public override Vector2 Tick(OnlinePlayerStateMachine context)
    {
        if (context.MoveInput.sqrMagnitude > 0.01f)
        {
            context.ChangeState(context.WalkState);
            return context.MoveInput;
        }
        return Vector2.zero;
    }
}

public sealed class OnlineWalkState : OnlinePlayerState
{
    public override OnlineMovementState Kind => OnlineMovementState.Walk;
    public override Vector2 Tick(OnlinePlayerStateMachine context)
    {
        if (context.MoveInput.sqrMagnitude <= 0.01f)
        {
            context.ChangeState(context.IdleState);
            return Vector2.zero;
        }
        return context.MoveInput;
    }
}

public sealed class OnlinePlayerStateMachine
{
    public OnlineIdleState IdleState { get; } = new OnlineIdleState();
    public OnlineWalkState WalkState { get; } = new OnlineWalkState();
    public OnlinePlayerState CurrentState { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }

    public OnlinePlayerStateMachine() => Reset();

    public void ChangeState(OnlinePlayerState next)
    {
        if (CurrentState == next) return;
        CurrentState?.Exit();
        CurrentState = next;
        CurrentState.Enter();
    }

    public void Reset()
    {
        MoveInput = Vector2.zero;
        JumpPressed = false;
        ChangeState(IdleState);
    }

    // Input is supplied by the caller, making states independent of devices and networking.
    public Vector2 Tick(Vector2 input, bool jump)
    {
        JumpPressed = jump;
        MoveInput = Vector2.ClampMagnitude(input, 1f);
        return CurrentState.Tick(this);
    }
}
