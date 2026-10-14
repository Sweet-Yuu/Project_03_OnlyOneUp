using UnityEngine;

// Bộ lọc dùng được cho cả bước vật lý host và khung hình hiển thị client.
public sealed class NestPositionSmoother
{
    private Vector3 _position;
    private Vector3 _velocity;
    public Vector3 Position => _position;

    public void Reset(Vector3 position)
    {
        _position = position;
        _velocity = Vector3.zero;
    }

    public void Translate(Vector3 displacement) => _position += displacement;

    public Vector3 Step(Vector3 target, float smoothTime, float deltaTime,
        float maxSpeed = Mathf.Infinity)
    {
        if (deltaTime <= 0f) return _position;
        Vector3 next = Vector3.SmoothDamp(_position, target, ref _velocity,
            Mathf.Max(0.01f, smoothTime), maxSpeed, deltaTime);
        _position = Vector3.MoveTowards(_position, next, maxSpeed * deltaTime);
        return _position;
    }
}
