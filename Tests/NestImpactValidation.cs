using System;
using UnityEngine;

public static class NestImpactValidation
{
    public static int Run()
    {
        var nest = new GameObject("Impact test nest");
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var eggObject = new GameObject("Impact test egg");
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        try
        {
            nest.transform.position = new Vector3(1000, 1000, 1000);
            nest.AddComponent<BoxCollider>();
            nest.AddComponent<Rigidbody>().isKinematic = true;
            obstacle.transform.position = nest.transform.position + Vector3.right * .8f;
            var detector = new NestImpactDetector(nest.transform);
            Physics.SyncTransforms();
            Check(detector.Sample(null, null, null, ~0, out _), "Kinematic nest detects static obstacle");
            Check(!detector.Sample(null, null, null, ~0, out _), "Continuous contact does not retrigger");
            obstacle.transform.position += Vector3.right * 5;
            Physics.SyncTransforms();
            Check(!detector.Sample(null, null, null, ~0, out _), "Separated contact cleared");
            obstacle.transform.position -= Vector3.right * 5;
            Physics.SyncTransforms();
            Check(detector.Sample(null, null, null, ~0, out _), "Reentry triggers again");
            var excluded = new NestImpactDetector(nest.transform);
            Check(!excluded.Sample(obstacle.transform, null, null, ~0, out _), "Player colliders ignored");
            var egg = eggObject.AddComponent<NestEggBody>();
            egg.Initialize(nest.transform, null);
            egg.SetAuthority(true);
            egg.ConfigureProgressiveDamage(null);
            egg.ResetOnServer(nest.transform.position + Vector3.up * 4, Quaternion.identity);
            egg.DamageFromNestImpact();
            Check(egg.DamageCount == 1, "Nest impact cracks egg once");
            egg.BeginCarry(new Pose(egg.Body.position, Quaternion.identity));
            egg.FallFromNestImpact(new Vector3(6, 8, 0));
            Check(!egg.Body.isKinematic && egg.Body.useGravity && egg.DamageCount == 1,
                "Fall releases physics and preserves cracks");
            egg.DamageFromNestImpact();
            egg.DamageFromNestImpact();
            Check(!egg.IsSimulating && egg.DamageCount == 3, "Third crack breaks egg");
            egg.ResetOnServer(nest.transform.position + Vector3.up * 4, Quaternion.identity);
            egg.BeginCarry(new Pose(egg.Body.position, Quaternion.identity));
            egg.FallFromNestImpact(new Vector3(2, 1, 0));
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(NestEggBody).GetField("ignoreImpactUntil", flags).SetValue(egg, -1f);
            typeof(NestEggBody).GetMethod("TouchEnvironment", flags)
                .Invoke(egg, new object[] { obstacle.GetComponent<Collider>() });
            Check(!egg.IsSimulating && egg.DamageCount == 3, "Released egg breaks on first environment contact");
            egg.ResetOnServer(nest.transform.position + Vector3.up * 4, Quaternion.identity);
            typeof(NestEggBody).GetField("ignoreImpactUntil", flags).SetValue(egg, -1f);
            typeof(NestEggBody).GetMethod("TouchEnvironment", flags)
                .Invoke(egg, new object[] { obstacle.GetComponent<Collider>() });
            Check(egg.IsSimulating && egg.DamageCount == 1, "Reset clears fatal landing state");
            return 10;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(eggObject);
            UnityEngine.Object.DestroyImmediate(obstacle);
            UnityEngine.Object.DestroyImmediate(nest);
        }
    }
}
