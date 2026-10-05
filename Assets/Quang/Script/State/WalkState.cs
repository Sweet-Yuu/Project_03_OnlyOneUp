using UnityEngine;

public class WalkState : PlayerState
{
    public WalkState(PlayerMovement playerMovement) : base(playerMovement){}

    public override void Enter()
    {
        playerMovement.animator.CrossFade("Walk", 0.1f);
    }

    public override void Execute()
    {
        if(playerMovement.movementInput == Vector2.zero)
        {
            playerMovement.ChangeState(playerMovement.idleState);
        }
    }

    public override void FixedUpdate()
    {
        playerMovement.rb.linearVelocity = new Vector3(playerMovement.moveDirection.x * playerMovement.speed, playerMovement.rb.linearVelocity.y, playerMovement.moveDirection.z * playerMovement.speed);
        // 2. LOGIC XOAY NGƯỜI ĐÃ SỬA LỖI
        // Tạo một vector copy để không làm hỏng vector di chuyển gốc
        Vector3 lookDirection = playerMovement.moveDirection;
        
        // ÉP PHẲNG TRỤC Y: Bắt buộc hướng nhìn phải luôn song song với mặt đất
        lookDirection.y = 0f; 

        // Kiểm tra lookDirection (đã bỏ Y) thay vì moveDirection
        if (lookDirection != Vector3.zero)
        {
            // Bây giờ LookRotation sẽ chỉ tính toán góc xoay quanh trục Y (trái/phải), không bao giờ bị ngã (lên/xuống) nữa
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            playerMovement.transform.rotation = Quaternion.Slerp(
                playerMovement.transform.rotation, 
                targetRotation, 
                Time.fixedDeltaTime * playerMovement.rotationSpeed
            );
        }
    }

    public override void Exit()
    {
        base.Exit();
        // Thực hiện các hành động khi thoát khỏi trạng thái đi bộ
        Debug.Log("Exiting Walk State");
    }
    
}
