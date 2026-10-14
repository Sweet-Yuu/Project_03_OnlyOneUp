using UnityEngine;

// Chỉ thay đổi pose hiển thị; không dự đoán va chạm hoặc kết quả trứng vỡ.
public sealed class NestClientPresentation
{
    private readonly NestPositionSmoother _position = new NestPositionSmoother();
    private readonly NestPositionSmoother _predictionPosition = new NestPositionSmoother();
    private Vector3 _previousPredictionPosition;
    private Quaternion _predictionRotation;
    private bool _wasAnticipating;
    private Quaternion _rotation;
    private float _eggCarryWeight;

    public void Reset(NestEggFrame frame)
    {
        _position.Reset(frame.NestPosition);
        _rotation = frame.NestRotation;
        _eggCarryWeight = frame.EggSupported ? 1f : 0f;
        _wasAnticipating = false;
    }

    public NestEggFrame Step(NestEggFrame frame, Pose target, float smoothTime,
        float maxOffset, float maxAngle, float deltaTime, bool anticipate = true,
        float correctionTime = 0.2f, float correctionSpeed = 4f, float correctionAngularSpeed = 90f)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        if (anticipate && _wasAnticipating)
        {
            Vector3 predicted = _predictionPosition.Step(target.position, smoothTime, deltaTime, 20f);
            Quaternion predictedRotation = Quaternion.RotateTowards(_predictionRotation,
                Quaternion.Slerp(_predictionRotation, target.rotation,
                    1f - Mathf.Exp(-deltaTime / Mathf.Max(0.01f, smoothTime))), 180f * deltaTime);
            // Phản hồi chuyển động dự đoán nhanh, giữ nguyên sai lệch hiển thị hiện tại.
            Vector3 displacement = predicted - _previousPredictionPosition;
            float currentDistance = Vector3.Distance(_position.Position, frame.NestPosition);
            float nextDistance = Vector3.Distance(_position.Position + displacement, frame.NestPosition);
            if (nextDistance <= maxOffset || nextDistance < currentDistance)
                _position.Translate(displacement);
            Quaternion nextRotation = predictedRotation * Quaternion.Inverse(_predictionRotation) * _rotation;
            float nextAngle = Quaternion.Angle(nextRotation, frame.NestRotation);
            if (nextAngle <= maxAngle || nextAngle < Quaternion.Angle(_rotation, frame.NestRotation))
                _rotation = nextRotation;
            _previousPredictionPosition = predicted;
            _predictionRotation = predictedRotation;
        }
        else
        {
            // Khi bật/tắt dự đoán, không coi việc đổi mục tiêu là chuyển động của người chơi.
            _predictionPosition.Reset(target.position);
            _previousPredictionPosition = target.position;
            _predictionRotation = target.rotation;
        }
        _wasAnticipating = anticipate;

        // Đây là giới hạn của mục tiêu, không ép pose đang hiển thị nhảy vào giới hạn mới.
        target.position = frame.NestPosition + Vector3.ClampMagnitude(
            target.position - frame.NestPosition, maxOffset);
        target.rotation = Quaternion.RotateTowards(frame.NestRotation, target.rotation, maxAngle);
        Vector3 position = _position.Step(target.position, correctionTime, deltaTime,
            Mathf.Max(0f, correctionSpeed));
        Quaternion correctedRotation = Quaternion.Slerp(_rotation, target.rotation,
            1f - Mathf.Exp(-deltaTime / Mathf.Max(0.01f, correctionTime)));
        _rotation = Quaternion.RotateTowards(_rotation, correctedRotation,
            Mathf.Max(0f, correctionAngularSpeed) * deltaTime);

        float supported = frame.Phase == NestEggPhase.Intact && frame.EggSupported ? 1f : 0f;
        _eggCarryWeight = Mathf.MoveTowards(_eggCarryWeight, supported, deltaTime / 0.1f);
        Quaternion correction = _rotation * Quaternion.Inverse(frame.NestRotation);
        Vector3 carriedEgg = position + correction * (frame.EggPosition - frame.NestPosition);
        frame.EggPosition = Vector3.Lerp(frame.EggPosition, carriedEgg, _eggCarryWeight);
        frame.EggRotation = Quaternion.Slerp(frame.EggRotation,
            correction * frame.EggRotation, _eggCarryWeight);
        frame.NestPosition = position;
        frame.NestRotation = _rotation;
        return frame;
    }
}
