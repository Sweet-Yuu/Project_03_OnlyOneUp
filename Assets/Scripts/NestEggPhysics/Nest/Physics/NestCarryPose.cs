using UnityEngine;

public sealed class NestCarryPose : INestCarryGeometry
{
    private Vector3 gripMidpoint;
    private Quaternion localGripFrame;

    public bool Initialize(Transform root, Transform leftGrip, Transform rightGrip, out string error)
    {
        error = null;
        if (leftGrip == null || rightGrip == null)
            error = "Nest: assign LeftGrip and RightGrip.";
        else if ((root.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            error = "Nest root must have world scale (1, 1, 1). Scale the visual child instead.";
        if (error != null) return false;
        Vector3 left = root.InverseTransformPoint(leftGrip.position);
        Vector3 right = root.InverseTransformPoint(rightGrip.position);
        Vector3 axis = right - left;
        if (!IsValidAxis(axis)) { error = "Nest: invalid grip positions."; return false; }
        gripMidpoint = (left + right) * 0.5f;
        localGripFrame = Quaternion.LookRotation(axis, Vector3.up);
        return true;
    }

    public static bool IsValidAxis(Vector3 axis) => axis.sqrMagnitude >= 0.001f &&
        Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up)) <= 0.99f;

    public Pose Calculate(Vector3 leftTarget, Vector3 rightTarget)
    {
        Quaternion rotation = Quaternion.LookRotation(rightTarget - leftTarget, Vector3.up)
            * Quaternion.Inverse(localGripFrame);
        return new Pose((leftTarget + rightTarget) * 0.5f - rotation * gripMidpoint, rotation);
    }
}
