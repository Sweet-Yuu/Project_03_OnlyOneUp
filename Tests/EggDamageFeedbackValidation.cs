using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class EggDamageFeedbackValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int _assertions;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        _assertions++;
    }
    private static void Invoke(EggDamageFeedback feedback, string method, params object[] args) =>
        typeof(EggDamageFeedback).GetMethod(method, Private).Invoke(feedback, args);
    private static void SetTime(EggDamageFeedback feedback, float value) =>
        typeof(EggDamageFeedback).GetField("_started", Private).SetValue(feedback, value);

    public static int Run()
    {
        _assertions = 0;
        var root = new GameObject("Damage feedback test");
        var cameraObject = new GameObject("Gameplay camera");
        var otherObject = new GameObject("Other camera");
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            var other = otherObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(15, 40, 0));
            Pose original = new Pose(camera.transform.position, camera.transform.rotation);
            var feedback = root.AddComponent<EggDamageFeedback>();
            feedback.Initialize(camera, .35f, .07f, .9f);
            var canvas = root.GetComponentInChildren<Canvas>();
            var image = root.GetComponentInChildren<RawImage>();
            Check(!image.raycastTarget && root.GetComponentInChildren<GraphicRaycaster>() == null,
                "Feedback overlay cannot intercept clicks or touch input");
            Check(image.rectTransform.anchorMin == Vector2.zero && image.rectTransform.anchorMax == Vector2.one,
                "Overlay fills all screen aspect ratios");
            var gradient = (Texture2D)image.texture;
            Check(gradient.GetPixel(0, 0).a > .99f && gradient.GetPixel(127, 127).a > .99f &&
                gradient.GetPixel(0, 127).a > .99f && gradient.GetPixel(127, 0).a > .99f,
                "All four corners have equal red gradient coverage");
            Check(gradient.GetPixel(64, 64).a < .001f && gradient.GetPixel(0, 64).a < .001f,
                "Center and edge midpoints remain clear");
            feedback.Observe(1, 0, NestEggPhase.Intact);
            Check(!canvas.enabled, "Initial snapshot cannot flash");
            feedback.Observe(1, 1, NestEggPhase.Intact);
            Check(canvas.enabled && image.color.a > .4f, "First crack starts feedback");
            float firstAlpha = image.color.a;
            Invoke(feedback, "ApplyShake", other);
            Check(other.transform.position == Vector3.zero, "Unrelated camera is never shaken");
            Invoke(feedback, "ApplyShake", camera);
            Vector3 shaken = camera.transform.position;
            Check(Vector3.Distance(shaken, original.position) > .001f, "Render callback applies camera shake");
            Invoke(feedback, "ApplyShake", camera);
            Check(Vector3.Distance(camera.transform.position, shaken) < .00001f, "Multiple render callbacks cannot accumulate shake");
            Invoke(feedback, "RestoreCamera", camera);
            Check(Vector3.Distance(camera.transform.position, original.position) < .00001f &&
                Quaternion.Angle(camera.transform.rotation, original.rotation) < .001f,
                "After render camera returns to the exact Cinemachine pose");
            SetTime(feedback, Time.unscaledTime - .45f);
            feedback.Observe(1, 1, NestEggPhase.Intact);
            Invoke(feedback, "Update");
            Check(image.color.a > .01f && image.color.a < firstAlpha * .4f,
                "Duplicate snapshots do not restart the fading overlay");
            Invoke(feedback, "ApplyShake", camera);
            Check(camera.transform.position == original.position, "Shake ends before the longer red fade");
            feedback.Observe(1, 2, NestEggPhase.Intact);
            Check(image.color.a > firstAlpha, "Second crack has stronger feedback");
            Invoke(feedback, "ApplyShake", camera);
            feedback.Observe(2, 0, NestEggPhase.Intact);
            Check(!canvas.enabled && Vector3.Distance(camera.transform.position, original.position) < .00001f,
                "Round reset removes overlay and outstanding camera offset");
            feedback.Observe(3, 2, NestEggPhase.Intact);
            Check(!canvas.enabled, "Late join with existing cracks must not replay old damage");
            feedback.Observe(3, 3, NestEggPhase.Broken);
            Check(!canvas.enabled, "Third hit does not trigger a crack-only effect");
            feedback.Observe(4, 0, NestEggPhase.Intact);
            feedback.Observe(4, 1, NestEggPhase.Intact);
            SetTime(feedback, Time.unscaledTime - 2);
            Invoke(feedback, "Update");
            Check(!canvas.enabled, "Overlay fully disappears after its duration");
            feedback.Observe(4, 2, NestEggPhase.Intact);
            Invoke(feedback, "ApplyShake", camera);
            Invoke(feedback, "OnDisable");
            Check(!canvas.enabled && Vector3.Distance(camera.transform.position, original.position) < .00001f,
                "Despawn/disable restores camera and hides UI immediately");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(otherObject);
        }
        return _assertions;
    }
}
