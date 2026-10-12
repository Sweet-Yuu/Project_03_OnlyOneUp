using UnityEngine;
using UnityEngine.UI;

public sealed class CircularRevealGraphic : MaskableGraphic
{
    private float _progress;

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_progress >= 1f)
            return;

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;

        // Tính trong không gian UI để vòng tròn không thành oval.
        float outerRadius = rect.size.magnitude * 0.55f;
        float innerRadius = outerRadius * _progress;

        const int segments = 160;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 inner = center + direction * innerRadius;
            Vector2 outer = center + direction * outerRadius;

            vh.AddVert(
                new Vector3(inner.x, inner.y, 0f),
                color, Vector2.zero);

            vh.AddVert(
                new Vector3(outer.x, outer.y, 0f),
                color, Vector2.zero);

            if (i == segments)
                continue;

            int index = i * 2;

            vh.AddTriangle(index, index + 2, index + 1);
            vh.AddTriangle(index + 2, index + 3, index + 1);
        }
    }
}