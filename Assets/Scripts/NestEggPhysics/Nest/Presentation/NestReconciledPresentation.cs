using UnityEngine;

// Sai lệch hiển thị được lưu riêng; input mới không phải chờ bộ lọc sửa sai.
public sealed class NestReconciledPresentation
{
    private Vector3 _positionError;
    private Quaternion _rotationError = Quaternion.identity;
    private readonly NestEggPresentation _eggPresentation = new NestEggPresentation();

    public void Reset(NestEggFrame frame)
    {
        _positionError = Vector3.zero;
        _rotationError = Quaternion.identity;
        _eggPresentation.Reset(frame);
    }

    public void Correct(Pose beforeReplay, Pose afterReplay)
    {
        _positionError += beforeReplay.position - afterReplay.position;
        _rotationError = _rotationError * beforeReplay.rotation * Quaternion.Inverse(afterReplay.rotation);
    }

    public NestEggFrame Present(NestEggFrame frame, Pose prediction, float deltaTime,
        float correctionTime, float correctionSpeed, float angularSpeed)
    {
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, correctionTime));
        _positionError = Vector3.MoveTowards(_positionError, _positionError * (1f - blend),
            Mathf.Max(0f, correctionSpeed * deltaTime));
        _rotationError = Quaternion.RotateTowards(_rotationError,
            Quaternion.Slerp(_rotationError, Quaternion.identity, blend), Mathf.Max(0f, angularSpeed * deltaTime));
        Pose shown = new Pose(prediction.position + _positionError, _rotationError * prediction.rotation);
        frame = _eggPresentation.Present(frame, shown, deltaTime);
        frame.NestPosition = shown.position;
        frame.NestRotation = shown.rotation;
        return frame;
    }
}
