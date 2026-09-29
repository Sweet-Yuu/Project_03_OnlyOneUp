using UnityEngine;

public class WalkState : PlayerState
{
    public WalkState(PlayerMovement playerMovement) : base(playerMovement)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // Thực hiện các hành động khi vào trạng thái đi bộ
        Debug.Log("Entering Walk State");
    }

    public override void Execute()
    {
        base.Execute();
        // Thực hiện các hành động trong trạng thái đi bộ
        // Ví dụ: Xử lý di chuyển của người chơi
    }

    public override void Exit()
    {
        base.Exit();
        // Thực hiện các hành động khi thoát khỏi trạng thái đi bộ
        Debug.Log("Exiting Walk State");
    }
    
}
