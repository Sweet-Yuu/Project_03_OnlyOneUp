using System;
using UnityEditor;
using UnityEngine;

public static class NestEggPhysicsValidation
{
    private static int _assertions;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        _assertions++;
    }

    public static int Run()
    {
        _assertions = 0;
        TestJumpWindow();
        TestPhysics();
        TestDamage();
        TestCheckpoint();
        return _assertions;
    }

    private static void TestCheckpoint()
    {
        var root = new GameObject("Online checkpoint");
        var spawn = new GameObject("Spawn center");
        try
        {
            root.transform.position = new Vector3(40, 3, 150);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(4, 4, 4);
            collider.isTrigger = true;
            var checkpoint = root.AddComponent<OnlineCheckpoint>();
            typeof(OnlineCheckpoint).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).Invoke(checkpoint, null);
            Physics.SyncTransforms();
            Check(checkpoint.CheckpointId == 1 && checkpoint.SpawnCenter == root.transform.position,
                "Default checkpoint ID and spawn center use the checkpoint itself");
            Check(checkpoint.IsNear(root.transform.position, 0), "Player inside checkpoint is accepted");
            Check(checkpoint.IsNear(root.transform.position + Vector3.right * 7, 6),
                "Host proximity check tolerates delayed owner position");
            Check(!checkpoint.IsNear(Vector3.zero, 6), "Distant unrelated checkpoint is rejected");
            collider.enabled = false;
            Check(!checkpoint.IsNear(root.transform.position, 6), "Disabled checkpoint collider cannot be activated");
            spawn.transform.position = new Vector3(45, 4, 150);
            typeof(OnlineCheckpoint).GetField("_spawnCenter", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).SetValue(checkpoint, spawn.transform);
            Check(checkpoint.SpawnCenter == spawn.transform.position, "Optional safe spawn marker overrides visual flag pivot");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(spawn);
        }
    }

    private static void TestDamage()
    {
        var nest = new GameObject("Progressive damage nest");
        var obj = new GameObject("Progressive damage egg");
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var egg = obj.AddComponent<NestEggBody>();
            egg.Initialize(nest.transform, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Pink_egg_Unity.fbx"));
            egg.ConfigureProgressiveDamage(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Broken_egg2.fbx"));
            egg.SetAuthority(true);
            int breaks = 0;
            egg.Broken += () => breaks++;
            Pose pose = new Pose(Vector3.up * 30, Quaternion.identity);
            egg.ResetOnServer(pose.position, pose.rotation);
            egg.BeginCarry(pose);
            ExpireImpactTimers(egg);
            var shellRenderer = obj.transform.Find("EggVisual").GetComponentInChildren<Renderer>();
            Material original = shellRenderer.sharedMaterial;
            obstacle.transform.position = pose.position + Vector3.right * 20;
            trigger.transform.position = pose.position;
            trigger.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            egg.MoveCarried(pose);
            Check(egg.DamageCount == 0, "Trigger is not a damaging collision");

            for (int hit = 1; hit <= 3; hit++)
            {
                obstacle.transform.position = pose.position;
                Physics.SyncTransforms();
                ExpireImpactTimers(egg);
                egg.MoveCarried(pose);
                Check(egg.DamageCount == hit, "Each distinct impact increments damage exactly once");
                Check(egg.IsSimulating == (hit < 3), "Egg survives first two hits and breaks on third");
                if (hit < 3)
                {
                    Check(shellRenderer.enabled && shellRenderer.sharedMaterial != original,
                        "Cracked shell stays visible with a private material");
                    Check(shellRenderer.sharedMaterial.name.EndsWith("cracks " + hit), "Correct crack stage material");
                    for (int i = 0; i < 10; i++)
                    {
                        ExpireImpactTimers(egg);
                        egg.MoveCarried(pose);
                    }
                    Check(egg.DamageCount == hit, "Sustained contact cannot consume another hit in later physics steps");
                    obstacle.transform.position += Vector3.right * 20;
                    Physics.SyncTransforms();
                    egg.MoveCarried(pose);
                }
            }
            Check(breaks == 1 && !shellRenderer.enabled && obj.transform.Find("BrokenEggVisual").gameObject.activeSelf,
                "Third hit hides intact shell and shows existing broken-egg model once");
            egg.BreakOnServer();
            Check(breaks == 1, "Repeated callbacks after breaking cannot notify twice");
            egg.ResetOnServer(pose.position, pose.rotation);
            Check(egg.DamageCount == 0 && shellRenderer.sharedMaterial == original && shellRenderer.enabled,
                "Reset removes cracks and restores original shell material");
            Check(!obj.transform.Find("BrokenEggVisual").gameObject.activeSelf, "Reset hides broken model");

            // The dynamic collision adapter uses the same contact gate as the overlap path.
            var touch = typeof(NestEggBody).GetMethod("TouchEnvironment",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ExpireImpactTimers(egg);
            touch.Invoke(egg, new object[] { obstacle.GetComponent<Collider>() });
            Check(egg.DamageCount == 1, "Dynamic adapter accepts first environment contact");
            touch.Invoke(egg, new object[] { obstacle.GetComponent<Collider>() });
            Check(egg.DamageCount == 1, "Dynamic Stay/Enter cannot double count an active contact");

            egg.SetAuthority(false);
            var frame = new NestEggFrame { Generation = 5, Phase = NestEggPhase.Intact,
                EggDamage = 2, EggPosition = pose.position, EggRotation = pose.rotation };
            egg.ApplyRemote(frame);
            Check(egg.DamageCount == 2 && shellRenderer.sharedMaterial.name.EndsWith("cracks 2"),
                "Client/late join reconstructs crack stage directly from snapshot");
            touch.Invoke(egg, new object[] { trigger.GetComponent<Collider>() });
            Check(egg.DamageCount == 2 && egg.Body.isKinematic, "Client cannot advance damage from physics");
            frame.Phase = NestEggPhase.Broken;
            frame.EggDamage = 3;
            egg.ApplyRemote(frame);
            Check(!shellRenderer.enabled && obj.transform.Find("BrokenEggVisual").gameObject.activeSelf,
                "Client displays third-hit broken model");
            frame.Generation++;
            frame.Phase = NestEggPhase.Intact;
            frame.EggDamage = 0;
            egg.ApplyRemote(frame);
            Check(egg.DamageCount == 0 && shellRenderer.sharedMaterial == original,
                "Client reset clears the previous round's cracks");

            var buffer = new NestEggFrameBuffer();
            frame.Time = 1;
            buffer.Add(frame);
            var next = frame;
            next.EggDamage = 1;
            Check(!frame.Equals(next), "Damage participates in frame equality");
            next.Time = 2;
            buffer.Add(next);
            buffer.Sample(1.5, out var before);
            buffer.Sample(2, out var after);
            Check(before.EggDamage == 0 && after.EggDamage == 1, "Crack stages change on snapshot timeline, not by interpolation");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(nest);
            UnityEngine.Object.DestroyImmediate(obstacle);
            UnityEngine.Object.DestroyImmediate(trigger);
        }
    }

    private static void ExpireImpactTimers(NestEggBody egg)
    {
        foreach (string field in new[] { "ignoreImpactUntil", "_lastImpactTime" })
            typeof(NestEggBody).GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(egg, float.NegativeInfinity);
    }

    private static void TestJumpWindow()
    {
        var jumps = new CooperativeJumpWindow(.2);
        Check(!jumps.Add(true, 1) && !jumps.Add(true, 1.1), "One player cannot trigger a paired jump");
        Check(!jumps.Add(false, 1.31), "Jumps over 200 ms apart cannot launch");
        jumps.Reset();
        Check(!jumps.Add(false, 4.1) && jumps.Add(true, 4), "Reversed packet arrival still pairs jump timestamps");
        Check(!jumps.Add(true, 4) && !jumps.Add(false, 4.1), "Duplicates cannot reuse a pair");
        Check(!jumps.Add(false, 4.15), "Used host jump cannot pair with another guest jump");
        jumps.Reset();
        Check(!jumps.Add(false, double.NaN) && !jumps.Add(true, double.PositiveInfinity), "Reject invalid clocks");
        Check(!jumps.Add(true, 9), "Reset discards old partner event");
        Check(!jumps.Add(false, 8.5), "Delayed older jump does not match new jump");
        Check(jumps.Add(false, 9.1), "Fresh pair after reset is valid");
    }

    private static void TestPhysics()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nest.prefab");
        Check(prefab != null, "Tests use the project's actual nest collider geometry");
        var nest = UnityEngine.Object.Instantiate(prefab);
        var obj = new GameObject("Egg physics test");
        SimulationMode previousMode = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            nest.transform.SetPositionAndRotation(Vector3.up * 15, Quaternion.identity);
            var body = nest.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            BoxCollider floor = null;
            foreach (BoxCollider box in nest.GetComponentsInChildren<BoxCollider>())
                if (box.size.y < .1f) floor = box;
            Check(floor != null, "Locate real floor collider");
            Vector3 top = nest.transform.InverseTransformPoint(floor.transform.TransformPoint(
                floor.center + Vector3.up * floor.size.y * .5f));
            Vector3 rest = top + Vector3.up * (NestEggBody.Height * .5f + .03f);
            var egg = obj.AddComponent<NestEggBody>();
            egg.Initialize(nest.transform, null);
            egg.SetAuthority(true);
            egg.Body.interpolation = RigidbodyInterpolation.None;
            var carry = new NestEggCarryPhysics(egg, .2f, .75f, 15f, 5f);
            Pose start = new Pose(body.position, body.rotation);
            egg.ResetOnServer(start.position + rest, start.rotation);
            carry.Reset(start, rest);
            Physics.SyncTransforms();

            float maximumRoll = 0f;
            carry.ReportJump(true, 1);
            for (int i = 1; i <= 150; i++)
            {
                float t = i * .02f;
                float jump = Mathf.Max(0, 13.4f * t - 15f * t * t);
                Pose pose = new Pose(start.position + new Vector3(t * 3, jump, 0),
                    Quaternion.Euler(0, t * 12, jump * 9));
                body.MovePosition(pose.position);
                body.MoveRotation(pose.rotation);
                carry.Step(pose, .02f);
                Physics.Simulate(.02f);
                Vector3 local = Quaternion.Inverse(body.rotation) * (egg.Body.position - body.position);
                float horizontalTravel = Vector3.ProjectOnPlane(local - rest, Vector3.up).magnitude;
                maximumRoll = Mathf.Max(maximumRoll, horizontalTravel);
                Check(egg.IsSimulating && carry.IsCarried && egg.Body.isKinematic,
                    "Solo jump/landing must not launch or break the egg");
                Check(horizontalTravel <= .751f, "Solo motion stays inside the rim while below the tilt limit");
                Check(Mathf.Abs(local.y - rest.y) < .001f,
                    "Solo jump, landing and resting at rim never lift the egg away from the floor");
            }
            Check(maximumRoll > .74f, "A solo jump rolls the egg all the way to the rim");

            // Nghiêng từ từ với collider thật: kiểm tra ngưỡng và khả năng lăn ra khỏi thành tổ.
            body.position = start.position;
            body.rotation = start.rotation;
            egg.ResetOnServer(start.position + rest, start.rotation);
            carry.Reset(start, rest);
            for (int angle = 1; angle <= 80; angle++)
            {
                Pose tilted = new Pose(start.position, Quaternion.Euler(0, 0, angle));
                body.MoveRotation(tilted.rotation);
                carry.Step(tilted, .02f);
                Physics.Simulate(.02f);
                if (angle <= 70) Check(carry.IsCarried, "Tilt up to 70 degrees keeps rim support");
                if (angle <= 70)
                    Check(Mathf.Abs((Quaternion.Inverse(body.rotation) * (egg.Body.position - body.position)).y - rest.y) < .001f,
                        "Approaching the tilt limit keeps the egg on the floor");
            }
            for (int i = 0; i < 150; i++)
            {
                carry.Step(new Pose(body.position, body.rotation), .02f);
                Physics.Simulate(.02f);
            }
            Check(!carry.IsCarried && !egg.Body.isKinematic, "Tilt exit hands control to gravity");
            Check(Vector3.Distance(egg.Body.position, body.position) > 4f,
                $"Tilted egg rolls out of real nest colliders: {egg.Body.position - body.position}");

            foreach (Quaternion rotation in new[] { Quaternion.Euler(0, 170, 0),
                Quaternion.Euler(70, 125, 0), Quaternion.Euler(0, 125, -70) })
            {
                Pose safe = new Pose(start.position, rotation);
                egg.ResetOnServer(safe.position + rotation * rest, rotation);
                carry.Reset(safe, rest);
                carry.Step(safe, .02f);
                Check(carry.IsCarried, "Yaw and exactly 70 degrees do not release the egg");
            }
            foreach (Quaternion rotation in new[] { Quaternion.Euler(70.1f, 125, 0),
                Quaternion.Euler(0, 125, -70.1f) })
            {
                Pose unsafePose = new Pose(start.position, rotation);
                egg.ResetOnServer(unsafePose.position + rotation * rest, rotation);
                carry.Reset(unsafePose, rest);
                for (int step = 0; step < 16; step++) carry.Step(unsafePose, .02f);
                Check(!carry.IsCarried, "Both pitch and negative roll release above 70 degrees");
            }

            // Trả về pose đứng yên, phát hai cú nhảy đến ngược thứ tự.
            body.position = start.position;
            body.rotation = start.rotation;
            egg.ResetOnServer(start.position + rest, start.rotation);
            carry.Reset(start, rest);
            carry.ReportJump(false, 10.1);
            carry.ReportJump(true, 10);
            carry.Step(start, .02f);
            Check(!carry.IsCarried && !egg.Body.isKinematic && egg.Body.useGravity,
                "Paired jump hands control back to dynamic physics");
            Check(egg.Body.linearVelocity.y >= 14.9f && egg.Body.linearVelocity.x >= 4.9f,
                "Pair produces upward and outward launch velocity");
            Physics.SyncTransforms();
            for (int i = 0; i < 50; i++) Physics.Simulate(.02f);
            Check(egg.Body.position.x - body.position.x > 2.8f,
                $"Launch clears actual prefab rim instead of bouncing back into the nest: position={egg.Body.position - body.position}, velocity={egg.Body.linearVelocity}, active={egg.IsSimulating}");
            Check(!carry.IsCarried, "No automatic reattachment after launching");

            egg.ResetOnServer(start.position + rest, start.rotation);
            carry.Reset(start, rest);
            carry.ReportJump(true, 20);
            carry.ReportJump(false, 20.25);
            carry.Step(start, .02f);
            Check(carry.IsCarried && egg.Body.isKinematic, "Separated jumps remain assisted after reset");

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                // Editor simulation does not advance Time.time: expire spawn grace explicitly.
                typeof(NestEggBody).GetField("ignoreImpactUntil",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(egg, float.NegativeInfinity);
                obstacle.transform.position = start.position + rest;
                Physics.SyncTransforms();
                carry.Step(start, .02f);
                Check(!egg.IsSimulating, "Carried kinematic egg still breaks against static environment");
            }
            finally { UnityEngine.Object.DestroyImmediate(obstacle); }
            carry.Stop();
            egg.ResetOnServer(start.position + Vector3.right * 5, Quaternion.identity);
            carry.Step(start, .02f);
            Check(!egg.Body.isKinematic, "Explicit drop/debug action keeps free physics");

            egg.SetAuthority(false);
            egg.BeginCarry(start);
            egg.ReleaseCarry(Vector3.up * 9, Vector3.zero);
            Check(egg.Body.isKinematic && !egg.IsSimulating, "Client cannot launch or run authoritative physics");
        }
        finally
        {
            Physics.simulationMode = previousMode;
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(nest);
        }
    }
}
