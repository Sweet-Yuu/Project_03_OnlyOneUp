using UnityEngine;
using UnityEngine.InputSystem;

public class IdleState : PlayerState
{
    public IdleState(PlayerMovement playerMovement) : base(playerMovement){}
    public override void Enter()
    {
        playerMovement.animator.CrossFade("IdleCarrying", 0.1f);
    }

    public override void Execute()
    {
        if(playerMovement.movementInput != Vector2.zero)
        {
            playerMovement.ChangeState(playerMovement.walkState);
        }
    }

    public override void FixedUpdate()
    {
        playerMovement.rb.linearVelocity = new Vector3(0f, playerMovement.rb.linearVelocity.y, 0f);
    }

    public override void Exit()
    {
    }
}
