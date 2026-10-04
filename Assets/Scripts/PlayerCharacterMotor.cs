using UnityEngine;

public interface IPlayerMovementConstraint
{
    Vector3 Limit(Vector3 requestedMove, float deltaTime);
}

// CharacterController mechanics, independent of input, animation and networking.
public sealed class PlayerCharacterMotor
{
    private const float Gravity = -20f;
    private readonly CharacterController controller;
    private readonly IPlayerMovementConstraint constraint;
    private float verticalSpeed;

    public PlayerCharacterMotor(CharacterController controller, IPlayerMovementConstraint constraint)
    {
        this.controller = controller;
        this.constraint = constraint;
    }

    public void Stop() => verticalSpeed = 0f;

    public void Move(Vector2 input, bool jump, float speed, float jumpHeight, float deltaTime)
    {
        if (controller.isGrounded)
        {
            verticalSpeed = -2f;
            if (jump) verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Gravity);
        }

        input = Vector2.ClampMagnitude(input, 1f);
        verticalSpeed += Gravity * deltaTime;

        Vector3 requestedMove =
            new Vector3(input.x * speed, verticalSpeed, input.y * speed)
            * deltaTime;

        Vector3 limitedMove = constraint.Limit(requestedMove, deltaTime);

        // Tránh tích lũy vận tốc lên/xuống khi bị giới hạn khoảng cách.
        if (deltaTime > 0f)
        {
            bool upwardBlocked =
                verticalSpeed > 0f &&
                limitedMove.y < requestedMove.y - 0.0001f;

            bool downwardBlocked =
                verticalSpeed < 0f &&
                limitedMove.y > requestedMove.y + 0.0001f;

            if (upwardBlocked || downwardBlocked)
                verticalSpeed = limitedMove.y / deltaTime;
        }

        CollisionFlags flags = controller.Move(limitedMove);

        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;

        if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
            verticalSpeed = -2f;

    }

    public static Quaternion FacingRotation(Vector3 position, Vector3 target)
    {
        // Turn around the vertical axis only, even when the partner jumps.
        Vector3 direction = target - position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction, Vector3.up)
            : Quaternion.identity;
    }
}
