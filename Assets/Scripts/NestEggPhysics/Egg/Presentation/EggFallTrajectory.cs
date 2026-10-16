using System;
using UnityEngine;
using UnityEngine.Rendering;

// Chỉ dự báo để hiển thị, không áp lực hay thay đổi mô phỏng trứng.
public sealed class EggFallTrajectory : IDisposable
{
    private readonly Transform _nest;
    private readonly Transform _egg;
    private readonly Material _material;
    private readonly LineRenderer _path;
    private readonly LineRenderer _landing;
    private readonly RaycastHit[] _hits = new RaycastHit[128];
    private readonly Vector3[] _points = new Vector3[97];

    public EggFallTrajectory(Transform nest, Transform egg)
    {
        _nest = nest;
        _egg = egg;
        var shader = Resources.Load<Shader>("EggTrajectoryLine");
        if (shader == null)
        {
            Debug.LogError("Missing Resources/EggTrajectoryLine shader.");
            return;
        }
        _material = new Material(shader);
        _path = CreateLine("Egg fall trajectory", .15f);
        _landing = CreateLine("Egg landing marker", .12f);
        _landing.loop = true;
    }

    private LineRenderer CreateLine(string name, float width)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(_nest, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = _material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    public void Present(Vector3 position, Vector3 velocity, bool falling)
    {
        if (_path == null) return;
        _path.enabled = falling;
        _landing.enabled = false;
        if (!falling) return;
        const float step = .05f;
        _points[0] = position;
        int count = 1;
        for (int i = 1; i < _points.Length; i++)
        {
            Vector3 next = position + velocity * step + Physics.gravity * (.5f * step * step);
            Vector3 delta = next - position;
            // Quét theo từng đoạn để điểm đánh dấu nằm trên collider thực,
            // bỏ trigger checkpoint/Dead và collider của tổ, trứng, người chơi.
            int hits = Physics.SphereCastNonAlloc(position, .99f, delta.normalized, _hits,
                delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit contact = default;
            for (int h = 0; h < hits; h++)
            {
                Collider collider = _hits[h].collider;
                if (collider is CharacterController || collider.transform.IsChildOf(_nest) ||
                    collider.transform.IsChildOf(_egg) || _hits[h].distance >= nearest) continue;
                nearest = _hits[h].distance;
                contact = _hits[h];
            }
            if (!float.IsPositiveInfinity(nearest))
            {
                Vector3 end = contact.point + contact.normal * .04f;
                _points[count++] = end;
                DrawLanding(end, contact.normal);
                break;
            }
            _points[count++] = next;
            position = next;
            velocity += Physics.gravity * step;
        }
        _path.positionCount = count;
        for (int i = 0; i < count; i++) _path.SetPosition(i, _points[i]);
    }

    private void DrawLanding(Vector3 center, Vector3 normal)
    {
        Vector3 tangent = Vector3.Cross(normal, Vector3.forward);
        if (tangent.sqrMagnitude < .01f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        _landing.positionCount = 40;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI * 2f / 40;
            _landing.SetPosition(i, center + .45f * (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)));
        }
        _landing.enabled = true;
    }

    public void Dispose()
    {
        if (_path != null) DestroyResource(_path.gameObject);
        if (_landing != null) DestroyResource(_landing.gameObject);
        if (_material != null) DestroyResource(_material);
    }

    private static void DestroyResource(UnityEngine.Object resource)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(resource);
        else UnityEngine.Object.DestroyImmediate(resource);
    }
}
