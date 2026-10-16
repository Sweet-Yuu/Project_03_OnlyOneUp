using System;
using System.Collections.Generic;
using UnityEngine;

public static class NestPredictionValidation
{
    private static int _assertions;
    private static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
        _assertions++;
    }
    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check(Vector3.Distance(actual, expected) < .0001f, message);

    private static NestCarrySimulation CreateSimulation()
    {
        var root = new GameObject("Geometry test");
        var left = new GameObject("Left");
        var right = new GameObject("Right");
        left.transform.SetParent(root.transform); right.transform.SetParent(root.transform);
        left.transform.localPosition = Vector3.left; right.transform.localPosition = Vector3.right;
        var geometry = new NestCarryPose();
        Check(geometry.Initialize(root.transform, left.transform, right.transform, out _), "Geometry initialized");
        UnityEngine.Object.DestroyImmediate(root);
        return new NestCarrySimulation(geometry, .025f);
    }

    private static NestEggFrame Snapshot(NestCarryState state, uint sequence, double time, uint generation = 1) =>
        new NestEggFrame
        {
            Generation = generation, Time = time, AppliedCarrySequence = sequence, CarryStepDuration = .02f,
            Phase = NestEggPhase.Intact, EggSupported = true,
            NestPosition = state.Position, NestRotation = state.Rotation, NestVelocity = state.Velocity,
            EggPosition = state.Position + Vector3.up * 2, EggRotation = state.Rotation
        };

    public static int Run()
    {
        _assertions = 0;
        TestReplay();
        TestInputBuffer();
        TestVisualCorrection();
        TestDelayedTransport();
        return _assertions;
    }

    private static void TestReplay()
    {
        var simulation = CreateSimulation();
        var prediction = new NestCarryPrediction(simulation);
        var initial = new NestCarryState { Rotation = Quaternion.identity };
        prediction.Reset(Snapshot(initial, 0, 0));
        for (int i = 1; i <= 6; i++)
            prediction.Predict(Vector3.left, Vector3.right + Vector3.forward * i * .2f);
        NestCarryInput[] inputs = prediction.GetResendBatch();
        NestCarryState host = initial;
        for (int i = 0; i < 3; i++) host = simulation.Step(host, inputs[i], .02f);
        NestCarryState latestBefore = prediction.Current;
        Check(prediction.Reconcile(Snapshot(host, 3, .06)), "Accept acknowledgement of input 3");
        Check(prediction.LastMatchedError < .0001f, "Match historical input 3, not current input 6");
        Near(prediction.Current.Position, latestBefore.Position, "A correct delayed ACK never pulls current prediction backward");
        Near(prediction.Current.Velocity, latestBefore.Velocity, "Replay restores filter velocity as well as pose");
        Check(prediction.PendingCount == 3, "Retain only inputs after acknowledged sequence");

        host.Position += new Vector3(.3f, .1f, 0);
        host.Velocity += Vector3.right;
        var corrected = Snapshot(host, 3, .08);
        NestCarryState expected = host;
        for (int i = 3; i < 6; i++) expected = simulation.Step(expected, inputs[i], .02f);
        Check(prediction.Reconcile(corrected), "Accept new authoritative state with same ACK sequence");
        Near(prediction.Current.Position, expected.Position, "Replay all pending inputs from corrected historical state");
        Near(prediction.Current.Velocity, expected.Velocity, "Corrected velocity participates in replay");
        Check(!prediction.Reconcile(corrected), "Reject duplicate snapshot");
        Check(!prediction.Reconcile(Snapshot(host, 2, .1)), "Reject ACK regression");
        Check(!prediction.Reconcile(Snapshot(host, 99, .1)), "Reject ACK for unsent input");
        Check(!prediction.Reconcile(Snapshot(host, 3, .1, 2)), "Different generation requires explicit reset");
        int pending = prediction.PendingCount;
        NestCarryState beforePreview = prediction.Current;
        prediction.Preview(Vector3.left, Vector3.right * 3, .01f);
        Near(prediction.Current.Position, beforePreview.Position, "Render preview does not mutate simulation");
        Check(prediction.PendingCount == pending, "Render preview cannot create network inputs");
        prediction.Reset(Snapshot(initial, 0, 1, 2));
        Check(prediction.PendingCount == 0, "Round reset drops old inputs");
        Check(!prediction.Reconcile(Snapshot(host, 3, 2, 1)), "Old round cannot rewind new round");
        for (int i = 0; i < NestCarryInputBuffer.Capacity; i++)
            Check(prediction.Predict(Vector3.left, Vector3.right), "History accepts bounded pending input");
        Check(!prediction.Predict(Vector3.left, Vector3.right), "History freezes instead of losing unacknowledged data");
        var sent = new HashSet<uint>();
        for (int i = 0; i < 30; i++) foreach (var command in prediction.GetResendBatch()) sent.Add(command.Sequence);
        Check(sent.Count == NestCarryInputBuffer.Capacity, "Resends cover entire window on high RTT");
    }

    private static void TestInputBuffer()
    {
        var buffer = new NestCarryInputBuffer();
        buffer.Reset(1);
        var second = new NestCarryInput { Generation = 1, Sequence = 2, GuestAnchor = Vector3.right };
        Check(buffer.Add(second), "Accept out-of-order input");
        Check(!buffer.TryTake(out _), "Do not apply input 2 before input 1");
        Check(!buffer.Add(second), "Duplicate resend does not double-apply");
        var first = second; first.Sequence = 1;
        Check(buffer.Add(first), "Missing input can be recovered");
        Check(buffer.TryTake(out var input) && input.Sequence == 1, "Consume recovered first input");
        Check(buffer.TryTake(out input) && input.Sequence == 2, "Then consume second input");
        Check(!buffer.Add(first), "Reject old consumed input");
        first.Sequence = 3; first.GuestAnchor.x = float.NaN;
        Check(!buffer.Add(first), "Reject non-finite input");
        first.GuestAnchor = Vector3.right; first.Sequence = 1000;
        Check(!buffer.Add(first), "Reject sequence outside bounded window");
        buffer.Reset(2);
        Check(!buffer.Add(second) && !buffer.TryTake(out _), "Round reset clears queued packets and rejects old generation");
    }

    private static void TestVisualCorrection()
    {
        var presenter = new NestReconciledPresentation();
        var frame = Snapshot(new NestCarryState { Rotation = Quaternion.identity }, 0, 0);
        presenter.Reset(frame);
        Pose before = new Pose(Vector3.right * 5, Quaternion.Euler(0, 30, 0));
        Pose after = new Pose(Vector3.right * 2, Quaternion.Euler(0, -20, 0));
        presenter.Correct(before, after);
        var shown = presenter.Present(frame, after, 0, .2f, 4, 90);
        Near(shown.NestPosition, before.position, "Rewind does not change displayed position immediately");
        Check(Quaternion.Angle(shown.NestRotation, before.rotation) < .001f, "Rewind preserves displayed rotation");
        after.position += Vector3.forward;
        shown = presenter.Present(frame, after, 0, .2f, 4, 90);
        Near(shown.NestPosition, before.position + Vector3.forward, "New movement is fully visible while correction is pending");
        for (int i = 0; i < 200; i++) shown = presenter.Present(frame, after, .016f, .2f, 4, 90);
        Near(shown.NestPosition, after.position, "Visual error decays to replayed simulation");
    }

    private static void TestDelayedTransport()
    {
        var simulation = CreateSimulation();
        var prediction = new NestCarryPrediction(simulation);
        var buffer = new NestCarryInputBuffer();
        var host = new NestCarryState { Rotation = Quaternion.identity };
        prediction.Reset(Snapshot(host, 0, 0)); buffer.Reset(1);
        var commands = new List<(int Due, NestCarryInput[] Batch)>();
        var snapshots = new List<(int Due, NestEggFrame Frame)>();
        Vector3 lastGuest = Vector3.right;
        uint lastApplied = 0;
        // 200 ms RTT + jitter và mất một số gói. Chạy simulation thật, giả lập khâu vận chuyển.
        for (int tick = 1; tick <= 400; tick++)
        {
            if (tick <= 200)
                prediction.Predict(Vector3.left, Vector3.right + Vector3.forward * tick * .04f);
            if (tick % 7 != 0) commands.Add((tick + 5 + tick % 3, prediction.GetResendBatch()));
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Due > tick) continue;
                foreach (var input in commands[i].Batch) buffer.Add(input);
                commands.RemoveAt(i);
            }
            if (buffer.TryTake(out var applied))
            {
                Check(applied.Sequence == lastApplied + 1, "Jitter/loss cannot skip or double-consume inputs");
                lastApplied = applied.Sequence; lastGuest = applied.GuestAnchor;
            }
            host = simulation.Step(host, new NestCarryInput { HostAnchor = Vector3.left, GuestAnchor = lastGuest }, .02f);
            if (tick % 2 == 0 && tick % 11 != 0)
                snapshots.Add((tick + 5 + tick % 3, Snapshot(host, buffer.AppliedSequence, tick * .02)));
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                if (snapshots[i].Due > tick) continue;
                prediction.Reconcile(snapshots[i].Frame);
                snapshots.RemoveAt(i);
            }
            if (tick == 4) Check(prediction.Current.Position.z > .03f && buffer.AppliedSequence == 0,
                "Client moves before first input reaches host");
            Check(prediction.PendingCount < NestCarryInputBuffer.Capacity, "Transport window does not starve behind high RTT");
        }
        Check(buffer.AppliedSequence == 200 && prediction.PendingCount == 0, "All 200 inputs eventually acknowledged after stopping");
        Near(prediction.Current.Position, host.Position, "Client converges to host after all input acknowledgements");
    }
}
