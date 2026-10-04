using System;
using UnityEngine;

// Exercises production logic with Unity's managed Vector types, without a running scene.
internal static class PlayerLogicTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }
    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check((actual - expected).sqrMagnitude < 0.000001f, message);

    private static Vector3 Limit(Vector3 move, Vector3 position, bool first = true) =>
        CarryDistanceConstraint.Limit(move, position, Vector3.zero, 4f, 6f, 6f, .02f, first);

    public static void Main()
    {
        Near(Limit(Vector3.right * .2f, Vector3.right * 5f), Vector3.right * .2f,
            "Free movement inside carry range");
        Near(Limit(Vector3.right * .2f, Vector3.right * 6f), Vector3.zero,
            "Outward motion stops at maximum");
        Near(Limit(Vector3.left * .2f, Vector3.right * 4f), Vector3.zero,
            "Inward motion stops at minimum");
        Near(Limit(Vector3.zero, Vector3.right * 8f), Vector3.left * .12f,
            "Existing network error recovers at bounded speed");
        Near(Limit(Vector3.zero, Vector3.zero), Vector3.left * .12f,
            "Coincident first player gets deterministic direction");
        Near(Limit(Vector3.zero, Vector3.zero, false), Vector3.right * .12f,
            "Coincident second player gets opposite direction");
        Vector3 start = new Vector3(3f, 4f, 0f);
        Vector3 step = Vector3.up * 2f;
        Vector3 end = start + Limit(step, start);
        Check(Math.Abs(end.magnitude - 6f) < .0001f, "Jump uses full 3D carry distance");
        Near(CarryDistanceConstraint.Limit(Vector3.right, Vector3.right * 10f,
            Vector3.zero, 4f, 16f, 6f, .02f, true), Vector3.right,
            "User's additional reach remains supported");

        var preparation = new PlayerPreparation();
        Vector3 target = new Vector3(4f, 2f, 3f);
        preparation.Begin(1, target);
        Check(!preparation.Ready(1, target), "Position alone does not acknowledge setup");
        preparation.Confirm(0, true, "old");
        Check(!preparation.Ready(1, target), "Stale reply is ignored");
        preparation.Confirm(1, true, "ok");
        Check(preparation.Ready(1, target), "Matching reply and position release setup");
        Check(!preparation.Ready(1, target + Vector3.right), "Wrong synced position blocks setup");
        preparation.Begin(2, target);
        Check(!preparation.Ready(2, target), "Retry clears previous acknowledgment");
        preparation.Confirm(2, false, "blocked");
        Check(preparation.Rejected(2), "Owner rejection is reported");
        Check(!preparation.Rejected(1), "Rejection belongs only to current sequence");
        Check(preparation.TryBeginLocal(2), "New owner placement is accepted");
        preparation.CompleteLocal(true);
        Check(!preparation.TryBeginLocal(2) && preparation.LocalPlacementSucceeded,
            "Duplicate placement does not reset completed local setup");
        Check(!preparation.TryBeginLocal(1), "Older owner placement cannot teleport again");
        preparation.Reset();
        Check(preparation.LocalSequence == 0 && !preparation.LocalPlacementSucceeded,
            "Respawn clears local gate");
        Check(preparation.TryBeginLocal(1), "New session can restart sequence numbering");

        var machine = new OnlinePlayerStateMachine();
        Check(machine.CurrentState.Kind == OnlineMovementState.Idle, "Initial idle");
        Vector2 motion = machine.Tick(Vector2.one, false);
        Check(machine.CurrentState.Kind == OnlineMovementState.Walk && Math.Abs(motion.magnitude - 1f) < .0001f,
            "Walk starts in same tick and diagonal input is normalized");
        var walk = machine.CurrentState;
        machine.Tick(Vector2.right, false);
        Check(ReferenceEquals(walk, machine.CurrentState), "Walking reuses state instance");
        motion = machine.Tick(Vector2.zero, true);
        Check(machine.CurrentState.Kind == OnlineMovementState.Idle && motion == Vector2.zero && machine.JumpPressed,
            "Idle still forwards a jump request to motor");
        machine.Tick(Vector2.zero, false);
        Check(!machine.JumpPressed, "Jump request is not latched");
        machine.Tick(Vector2.right, true);
        machine.Reset();
        Check(machine.CurrentState.Kind == OnlineMovementState.Idle && !machine.JumpPressed && machine.MoveInput == Vector2.zero,
            "Preparation lock resets state and input");
        Console.WriteLine("PASS: " + assertions + " player logic assertions.");
    }
}
