using UnityEngine;

// Trạng thái không phụ thuộc callback tiếp xúc: nảy trong lòng tổ vẫn là còn trong tổ.
public sealed class NestEggContainment
{
    private readonly Bounds _bounds;
    private readonly float _exitMargin;
    private bool _inside;

    public NestEggContainment(Bounds bounds, float exitMargin = 0.2f)
    {
        _bounds = bounds;
        _exitMargin = Mathf.Max(0f, exitMargin);
    }

    public void Reset() => _inside = false;

    public bool Evaluate(Vector3 localEggPosition)
    {
        Bounds region = _bounds;
        // Ngưỡng rời rộng hơn ngưỡng vào, tránh đổi trạng thái khi sát mép.
        if (_inside) region.Expand(_exitMargin * 2f);
        _inside = region.Contains(localEggPosition);
        return _inside;
    }
}
