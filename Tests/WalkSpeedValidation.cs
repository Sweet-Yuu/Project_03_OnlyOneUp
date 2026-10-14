using System;
using UnityEditor.Animations;
using UnityEngine;

public static class WalkSpeedValidation
{
    public static int Run()
    {
        var root = new GameObject("Walking speed validation");
        var bone = new GameObject("Bone");
        bone.transform.SetParent(root.transform);
        bone.transform.localPosition = Vector3.up;
        var controller = new AnimatorController();
        controller.AddLayer("Base Layer");
        var state = controller.layers[0].stateMachine.AddState("Walk");
        controller.layers[0].stateMachine.defaultState = state;
        var clip = new AnimationClip();
        clip.SetCurve("Bone", typeof(Transform), "m_LocalPosition.x", AnimationCurve.Linear(0, 0, 2, 2));
        state.motion = clip;
        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        try
        {
            var presenter = new PlayerAnimationPresenter(animator, root);
            presenter.Tick(0, 5, 10, 4, .02f);
            Check(!animator.enabled, "No input displacement keeps walking disabled");
            presenter.Tick(5, 5, 10, 4, .02f);
            Check(animator.enabled && animator.speed > 0 && animator.speed < 1, "Start blends toward reference playback");
            for (int i = 0; i < 100; i++) presenter.Tick(5, 5, 10, 4, .02f);
            Check(Mathf.Abs(animator.speed - 1) < .001f, "Reference motion converges to 1x");
            for (int i = 0; i < 100; i++) presenter.Tick(2.5f, 5, 10, 4, .02f);
            Check(Mathf.Abs(animator.speed - .5f) < .001f, "Half movement converges to half playback");
            animator.Update(.2f);
            float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            presenter.Tick(2.5f, 5, 10, 4, .02f);
            animator.Update(.2f);
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > time, "Speed updates do not restart clip");
            presenter.Tick(.1f, 5, 10, 4, .02f);
            Check(animator.enabled, "Hysteresis keeps current walking at intermediate speed");
            presenter.Tick(0, 5, 10, 4, .02f);
            Check(!animator.enabled && bone.transform.localPosition == Vector3.up, "Blocked motion restores original pose");
            presenter.Tick(.1f, 5, 10, 4, .02f);
            Check(!animator.enabled, "Small idle jitter cannot start clip");
            return count;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(controller);
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
