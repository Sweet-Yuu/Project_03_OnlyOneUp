using System;
using UnityEngine;

public static class NestWinValidation
{
    public static int Run()
    {
        var nest = new GameObject("Test nest", typeof(BoxCollider), typeof(Rigidbody));
        var target = new GameObject("Test win surface", typeof(BoxCollider));
        int count = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
        try
        {
            nest.GetComponent<Rigidbody>().isKinematic = true;
            var box = target.GetComponent<BoxCollider>();
            target.tag = "Win_Area";
            target.transform.position = Vector3.right * 10;
            var detector = new NestWinContact(nest.transform);
            Check(!detector.IsTouching(), "Distant target cannot win");
            target.transform.position = Vector3.right * .9f;
            Check(detector.IsTouching(), "Kinematic nest detects static win box");
            box.isTrigger = true;
            Check(detector.IsTouching(), "Trigger win box also works");
            box.enabled = false;
            Check(!detector.IsTouching(), "Disabled collider cannot win");
            box.enabled = true;
            target.SetActive(false);
            Check(!detector.IsTouching(), "Inactive target cannot win");
            target.SetActive(true);
            target.tag = "Untagged";
            Check(!new NestWinContact(nest.transform).IsTouching(), "Ordinary scenery cannot win");
            return count;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(nest);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
