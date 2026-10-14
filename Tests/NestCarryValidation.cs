using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NestCarryValidation
{
    private static int _assertions;

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
        _assertions++;
    }

    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check(Vector3.Distance(actual, expected) < 0.001f, message);

    private static NestEggFrame Frame(double time, uint generation = 1) => new NestEggFrame
    {
        Time = time, Generation = generation, Phase = NestEggPhase.Intact,
        EggSupported = true, NestRotation = Quaternion.identity,
        EggPosition = new Vector3(1, 2, 0), EggRotation = Quaternion.identity
    };

    public static void Run()
    {
        try
        {
            TestSmoothing();
            _assertions += NestImpactValidation.Run();
            _assertions += WalkSpeedValidation.Run();
            TestTimeline();
            TestPresentation();
            TestReconciliation();
            TestClientPhysics();
            _assertions += NestPredictionValidation.Run();
            _assertions += NestEggPresentationValidation.Run();
            _assertions += NestEggPhysicsValidation.Run();
            _assertions += PlayerJumpValidation.Run();
            _assertions += EggDamageFeedbackValidation.Run();
            _assertions += NetworkRoundManagerValidation.Run();
            TestFallTrajectory();
            string result = $"PASS: {_assertions} nest carry assertions using Unity runtime math/physics components.";
            File.WriteAllText(Path.Combine(Application.dataPath, "../result.txt"), result);
            Debug.Log(result);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "../result.txt"), "FAIL: " + error);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void TestFallTrajectory()
    {
        var history = new NestEggFrameBuffer();
        NestEggFrame a = Frame(1), b = Frame(2);
        a.EggVelocity = Vector3.right * 2;
        b.EggVelocity = Vector3.right * 4;
        history.Add(a);
        history.Add(b);
        Check(history.Sample(1.5, out NestEggFrame sample), "Client samples trajectory velocity");
        Near(sample.EggVelocity, Vector3.right * 3, "Client interpolates authoritative egg velocity");
        NestEggFrame changed = a;
        changed.EggVelocity = Vector3.zero;
        Check(!a.Equals(changed), "Velocity-only changes are included in snapshot equality");
        var nest = new GameObject("Trajectory nest");
        var egg = new GameObject("Trajectory egg");
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        EggFallTrajectory trajectory = null;
        try
        {
            floor.transform.position = new Vector3(2000, 0, 0);
            floor.transform.localScale = new Vector3(100, 1, 100);
            Physics.SyncTransforms();
            trajectory = new EggFallTrajectory(nest.transform, egg.transform);
            Vector3 position = new Vector3(2000, 10, 0);
            trajectory.Present(position, Vector3.right * 4, true);
            var lines = nest.GetComponentsInChildren<LineRenderer>();
            Check(lines.Length == 2, "Trajectory creates path and landing ring");
            Check(lines[0].enabled && lines[1].enabled && lines[0].positionCount > 2,
                "Falling egg displays curved path and detected landing point");
            Near(lines[0].GetPosition(0), position, "Trajectory starts at displayed egg position");
            Vector3 end = lines[0].GetPosition(lines[0].positionCount - 1);
            Check(end.x > position.x && Mathf.Abs(end.y - .54f) < .02f,
                "Predicted contact respects horizontal velocity and real floor collider");
            Check(!ShaderUtil.ShaderHasError(lines[0].sharedMaterial.shader), "Trajectory shader compiles");
            trajectory.Present(position, Vector3.zero, false);
            Check(!lines[0].enabled && !lines[1].enabled, "Carried/broken/reset egg hides trajectory");
            floor.GetComponent<Collider>().enabled = false;
            trajectory.Present(position, Vector3.right, true);
            Check(lines[0].enabled && !lines[1].enabled, "No surface in prediction range produces no false landing marker");
        }
        finally
        {
            trajectory?.Dispose();
            UnityEngine.Object.DestroyImmediate(nest);
            UnityEngine.Object.DestroyImmediate(egg);
            UnityEngine.Object.DestroyImmediate(floor);
        }
    }

    private static void TestSmoothing()
    {
        var smoother = new NestPositionSmoother();
        smoother.Reset(Vector3.zero);
        Vector3 first = smoother.Step(Vector3.right, 0.1f, 0.02f);
        Check(first.x > 0 && first.x < 0.2f, "Packet step must not teleport the physical floor");
        float last = first.x;
        for (int i = 0; i < 60; i++)
        {
            float next = smoother.Step(Vector3.right, 0.1f, 0.02f).x;
            Check(next >= last && next <= 1.00001f, "Converges without overshoot");
            last = next;
        }
        Check(last > 0.999f, "Settles at target when movement stops");
        smoother.Reset(new Vector3(40, 5, 30));
        Near(smoother.Step(new Vector3(40, 5, 30), .1f, .02f), new Vector3(40, 5, 30),
            "Round reset discards old position and velocity");
        Near(smoother.Step(Vector3.zero, .1f, 0), new Vector3(40, 5, 30), "Paused frame is stable");

        smoother.Reset(Vector3.zero);
        float rawLast = 0, rawVelocity = 0, filteredLast = 0, filteredVelocity = 0;
        float rawPeak = 0, filteredPeak = 0;
        for (int i = 0; i < 100; i++)
        {
            // 10 Hz vị trí đến theo từng gói, trong khi vật lý chạy 50 Hz.
            float target = (i / 5) * 0.5f;
            float rawNextVelocity = (target - rawLast) / .02f;
            rawPeak = Mathf.Max(rawPeak, Mathf.Abs(rawNextVelocity - rawVelocity) / .02f);
            float filtered = smoother.Step(Vector3.right * target, .1f, .02f).x;
            float filteredNextVelocity = (filtered - filteredLast) / .02f;
            filteredPeak = Mathf.Max(filteredPeak, Mathf.Abs(filteredNextVelocity - filteredVelocity) / .02f);
            rawLast = target; rawVelocity = rawNextVelocity;
            filteredLast = filtered; filteredVelocity = filteredNextVelocity;
        }
        Check(filteredPeak < rawPeak * .3f, "Packet bursts must have substantially lower acceleration spikes");
    }

    private static void TestTimeline()
    {
        var buffer = new NestEggFrameBuffer();
        Check(!buffer.Add(Frame(0, 0)), "Ignore uninitialized network variable");
        NestEggFrame a = Frame(1), b = Frame(2);
        b.NestPosition = new Vector3(4, 0, 0);
        b.NestRotation = Quaternion.Euler(0, 120, 0);
        b.EggPosition = b.NestPosition + b.NestRotation * a.EggPosition;
        b.EggRotation = b.NestRotation;
        Check(buffer.Add(a) && buffer.Add(b), "Accept increasing timeline");
        Check(!buffer.Add(a) && !buffer.Add(b), "Ignore stale and duplicate packets");
        Check(buffer.Sample(1.5, out NestEggFrame sample, true), "Sample midpoint");
        Near(Quaternion.Inverse(sample.NestRotation) * (sample.EggPosition - sample.NestPosition),
            a.EggPosition, "Supported egg keeps relative position while tray rotates");
        buffer.Sample(20, out sample, true);
        Near(sample.EggPosition, b.EggPosition, "Packet stall holds final complete egg frame");
        Check(buffer.Add(Frame(3, 2)) && buffer.Count == 1, "Round reset flushes old trajectory");
        Check(!buffer.Add(Frame(4, 1)), "Old round packet cannot undo reset");
        buffer.Clear();
        Check(!buffer.Sample(0, out sample), "Despawn clears timeline");

        b.EggSupported = false;
        buffer.Add(a); buffer.Add(b);
        buffer.Sample(1.5, out sample, true);
        Near(sample.EggPosition, Vector3.Lerp(a.EggPosition, b.EggPosition, .5f),
            "Airborne transition uses authoritative world trajectory");
    }

    private static void TestPresentation()
    {
        var presentation = new NestClientPresentation();
        NestEggFrame frame = Frame(1);
        presentation.Reset(frame);
        Pose target = new Pose(new Vector3(2, 0, 0), Quaternion.Euler(0, 15, 0));
        NestEggFrame sample = presentation.Step(frame, target, .04f, 3, 20, .016f);
        Check(sample.NestPosition.x > 0, "Local motion responds without another host packet");
        Near(Quaternion.Inverse(sample.NestRotation) * (sample.EggPosition - sample.NestPosition),
            frame.EggPosition, "Supported egg receives same presentation correction as nest");
        for (int i = 0; i < 100; i++)
            sample = presentation.Step(frame, new Pose(Vector3.right * 100, Quaternion.Euler(0, 90, 0)),
                .04f, 3, 20, .016f);
        Check(sample.NestPosition.magnitude <= 3.001f, "Prediction displacement is bounded");
        Check(Quaternion.Angle(sample.NestRotation, frame.NestRotation) <= 20.01f,
            "Prediction rotation is bounded");
        frame.EggSupported = false;
        for (int i = 0; i < 10; i++) sample = presentation.Step(frame, target, .04f, 3, 20, .016f);
        Near(sample.EggPosition, frame.EggPosition, "Falling egg stops following anticipated tray");
        frame.Phase = NestEggPhase.Broken;
        presentation.Reset(frame);
        sample = presentation.Step(frame, target, .04f, 3, 20, .016f);
        Check(sample.Phase == NestEggPhase.Broken, "Client never changes host break decision");
        frame = Frame(2, 2);
        frame.NestPosition = Vector3.right * 40;
        presentation.Reset(frame);
        sample = presentation.Step(frame, new Pose(frame.NestPosition, frame.NestRotation), .04f, 3, 20, .016f);
        Near(sample.NestPosition, frame.NestPosition, "New round does not interpolate from old round");
    }

    private static void TestReconciliation()
    {
        var presentation = new NestClientPresentation();
        NestEggFrame frame = Frame(1);
        presentation.Reset(frame);
        Pose predicted = new Pose(Vector3.right * 2, Quaternion.Euler(0, 15, 0));
        NestEggFrame shown = frame;
        for (int i = 0; i < 120; i++)
            shown = presentation.Step(frame, predicted, .04f, 3, 20, .016f);

        // Gói mới lệch xa vị trí đang hiển thị: không được dịch chuyển quá tốc độ sửa sai.
        frame.Time = 2;
        frame.NestPosition = Vector3.left * 4;
        frame.EggPosition = frame.NestPosition + new Vector3(1, 2, 0);
        frame.NestRotation = Quaternion.Euler(0, -45, 0);
        NestEggFrame before = shown;
        shown = presentation.Step(frame, predicted, .04f, 3, 20, .016f);
        Check(Vector3.Distance(shown.NestPosition, before.NestPosition) <= 4f * .016f + .0001f,
            "Host correction is speed-limited even when the prediction boundary moves abruptly");
        Check(Quaternion.Angle(shown.NestRotation, before.NestRotation) <= 90f * .016f + .001f,
            "Host rotation correction is speed-limited");

        // Mất gói qua ngưỡng 300 ms: ngừng dự đoán, không cộng delta đổi mục tiêu vào pose.
        before = shown;
        Pose authority = new Pose(frame.NestPosition, frame.NestRotation);
        shown = presentation.Step(frame, authority, .04f, 3, 20, .016f, false);
        Check(Vector3.Distance(shown.NestPosition, before.NestPosition) <= 4f * .016f + .0001f,
            "Switching from prediction to host after a stall cannot snap");
        for (int i = 0; i < 250; i++)
            shown = presentation.Step(frame, authority, .04f, 3, 20, .016f, false);
        Near(shown.NestPosition, frame.NestPosition, "Correction eventually reaches host position");

        before = shown;
        shown = presentation.Step(frame, predicted, .04f, 3, 20, .016f, true);
        Check(Vector3.Distance(shown.NestPosition, before.NestPosition) <= 4f * .016f + .0001f,
            "Resuming prediction does not inject the discontinuous target change");

        // Khi host im lặng nhưng local tiếp tục chạy, không tích lũy dự đoán vô hạn.
        presentation.Reset(Frame(1));
        frame = Frame(1);
        for (int i = 0; i < 200; i++)
        {
            shown = presentation.Step(frame, new Pose(Vector3.right * i * .16f, Quaternion.identity),
                .04f, 3, 20, .016f);
            Check(shown.NestPosition.magnitude <= 3.001f, "Continuous prediction remains bounded during packet loss");
        }
    }

    private static void TestClientPhysics()
    {
        var nest = new GameObject("Test nest");
        var obj = new GameObject("Test egg");
        try
        {
            var egg = obj.AddComponent<NestEggBody>();
            egg.Initialize(nest.transform, null);
            egg.SetAuthority(false);
            egg.ApplyRemote(Frame(1));
            Check(egg.Body.isKinematic && !egg.Body.useGravity, "Client egg cannot simulate gravity");
            Check(!obj.GetComponent<CapsuleCollider>().enabled, "Client egg cannot produce contact impulses");
            egg.ResetOnServer(Vector3.up * 10, Quaternion.identity);
            Check(egg.Body.isKinematic && !egg.IsSimulating, "Client cannot turn on authoritative egg physics");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(nest);
        }
    }
}
