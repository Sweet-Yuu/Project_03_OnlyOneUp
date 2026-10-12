using System;
using UnityEngine;

public sealed class PlayerJumpValidation : IPlayerMovementConstraint
{
    private bool _block;
    public Vector3 Limit(Vector3 movement, float deltaTime) => _block ? Vector3.zero : movement;

    public static int Run()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var player = new GameObject("Jump signal test");
        try
        {
            ground.transform.position = new Vector3(100, 0, 0);
            ground.transform.localScale = new Vector3(10, 1, 10);
            player.transform.position = new Vector3(100, 1.6f, 0);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = .5f;
            controller.minMoveDistance = 0;
            var constraint = new PlayerJumpValidation();
            var motor = new PlayerCharacterMotor(controller, constraint);
            Physics.SyncTransforms();
            controller.Move(Vector3.down * .2f);
            if (!controller.isGrounded) throw new Exception("Jump test must begin on actual ground");
            motor.Move(Vector2.zero, true, 5, 6, .02f, 3, 3);
            if (!motor.JumpedThisMove) throw new Exception("Grounded takeoff must notify egg gameplay");
            motor.Move(Vector2.zero, true, 5, 6, .02f, 3, 3);
            if (motor.JumpedThisMove) throw new Exception("Airborne jump press cannot count as another takeoff");
            motor.Stop();
            controller.enabled = false;
            player.transform.position = new Vector3(100, 1.6f, 0);
            controller.enabled = true;
            Physics.SyncTransforms();
            controller.Move(Vector3.down * .2f);
            constraint._block = true;
            motor.Move(Vector2.zero, true, 5, 6, .02f, 3, 3);
            if (motor.JumpedThisMove) throw new Exception("A fully blocked takeoff cannot launch the egg");
            return 4;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(ground);
        }
    }
}
