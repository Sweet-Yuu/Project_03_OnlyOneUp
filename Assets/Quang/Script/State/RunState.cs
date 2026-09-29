using UnityEngine;

public class RunState : PlayerState
{
    public RunState(PlayerMovement playerMovement) : base(playerMovement){}
    public override void Enter()
    {
        base.Enter();
    }

    public override void Execute()
    {
        base.Execute();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void Exit()
    {
        base.Exit();
    }
}
