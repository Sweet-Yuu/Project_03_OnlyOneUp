using UnityEngine;

public abstract class PlayerState
{
    protected PlayerMovement playerMovement;
    public PlayerState(PlayerMovement playerMovement)
    {
        this.playerMovement = playerMovement;
    }

    public virtual void Enter(){}
    public virtual void Execute(){}
    public virtual void FixedUpdate(){}
    public virtual void Exit(){}
}
