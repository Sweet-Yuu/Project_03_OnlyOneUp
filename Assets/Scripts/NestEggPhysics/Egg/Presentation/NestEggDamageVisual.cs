using System;
using System.Collections.Generic;
using UnityEngine;

// Chỉ phụ trách hình ảnh: vết rạn dùng material riêng, không sửa asset gốc.
public sealed class NestEggDamageVisual : IDisposable
{
    private readonly Renderer[] _shells;
    private readonly Material[][] _original;
    private readonly Material[][] _light;
    private readonly Material[][] _heavy;
    private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
    private readonly GameObject _broken;
    private bool _texturesReady;

    public NestEggDamageVisual(Transform root, Renderer[] shells, GameObject brokenPrefab)
    {
        _shells = shells;
        _original = new Material[shells.Length][];
        _light = new Material[shells.Length][];
        _heavy = new Material[shells.Length][];
        for (int i = 0; i < shells.Length; i++) _original[i] = shells[i].sharedMaterials;
        BuildCracks();
        if (brokenPrefab == null) return;
        _broken = UnityEngine.Object.Instantiate(brokenPrefab, root);
        _broken.name = "BrokenEggVisual";
        _broken.transform.localPosition = Vector3.zero;
        _broken.transform.localRotation = Quaternion.identity;
        _broken.transform.localScale = Vector3.one;
        // Prefab chỉ cung cấp hình ảnh; không cho camera/âm thanh/đèn đi kèm
        // chiếm camera gameplay khi Show() bật model trứng vỡ.
        // foreach (Camera camera in _broken.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        // foreach (AudioListener listener in _broken.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        // foreach (Light light in _broken.GetComponentsInChildren<Light>(true)) light.enabled = false;
        // foreach (Collider collider in _broken.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in _broken.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.detectCollisions = false; }
        Renderer[] fragments = _broken.GetComponentsInChildren<Renderer>();
        if (fragments.Length > 0)
        {
            Bounds bounds = fragments[0].bounds;
            foreach (Renderer fragment in fragments) bounds.Encapsulate(fragment.bounds);
            // Model vỡ là lòng trắng/lòng đỏ dẹt: chuẩn hóa theo bề ngang, không theo chiều cao.
            float scale = 1.98f / Mathf.Max(bounds.size.x, bounds.size.z, .001f);
            Vector3 bottom = root.InverseTransformPoint(bounds.center - Vector3.up * bounds.extents.y);
            _broken.transform.localScale *= scale;
            _broken.transform.localPosition = Vector3.down * (NestEggBody.Height * .5f) - bottom * scale;
        }
        _broken.SetActive(false);
    }

    public void Show(NestEggPhase phase, byte damage)
    {
        bool broken = phase == NestEggPhase.Broken;
        if (damage > 0 && !broken && !_texturesReady) BuildCracks();
        for (int i = 0; i < _shells.Length; i++)
        {
            _shells[i].enabled = phase != NestEggPhase.Waiting && !broken;
            _shells[i].sharedMaterials = damage == 0 || !_texturesReady ? _original[i] :
                damage == 1 ? _light[i] : _heavy[i];
        }
        if (_broken != null) _broken.SetActive(broken);
    }

    private void BuildCracks()
    {
        for (int i = 0; i < _shells.Length; i++)
        {
            _light[i] = new Material[_original[i].Length];
            _heavy[i] = new Material[_original[i].Length];
            for (int j = 0; j < _original[i].Length; j++)
            {
                Material source = _original[i][j];
                if (source == null) continue;
                _light[i][j] = CrackMaterial(source, 1);
                _heavy[i][j] = CrackMaterial(source, 2);
            }
        }
        _texturesReady = true;
    }

    private Material CrackMaterial(Material source, int stage)
    {
        const int size = 512;
        var material = new Material(source) { name = source.name + " - cracks " + stage };
        _owned.Add(material);
        string property = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        if (!source.HasProperty(property)) return material;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "Egg cracks " + stage;
        _owned.Add(texture);
        Color[] pixels = ReadBaseTexture(source.GetTexture(property), size);
        var random = new System.Random(731);
        // Cùng seed ở cả hai máy. Mức hai giữ các vết cũ, thêm nhánh và tăng độ rộng.
        int cracks = stage == 1 ? 5 : 18;
        for (int crack = 0; crack < cracks; crack++)
        {
            Vector2 center = new Vector2((float)random.NextDouble() * size,
                (.15f + (float)random.NextDouble() * .7f) * size);
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            for (int arm = 0; arm < 3; arm++)
            {
                Vector2 point = center;
                float heading = angle + arm * 2.1f;
                for (int segment = 0; segment < 5; segment++)
                {
                    float bend = ((float)random.NextDouble() - .5f) * 1.3f;
                    Vector2 next = point + new Vector2(Mathf.Cos(heading + bend), Mathf.Sin(heading + bend)) * 9f;
                    DrawLine(pixels, size, point, next, stage == 1 ? .9f : 1.5f);
                    point = next;
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        material.SetTexture(property, texture);
        return material;
    }

    private static Color[] ReadBaseTexture(Texture source, int size)
    {
        var colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        if (source == null) return colors;
        if (source is Texture2D readable && readable.isReadable)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) colors[y * size + x] = readable.GetPixelBilinear((float)x / size, (float)y / size);
            return colors;
        }
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return colors;
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
        var copy = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            copy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            copy.Apply();
            return copy.GetPixels();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            UnityEngine.Object.Destroy(copy);
        }
    }

    private static void DrawLine(Color[] pixels, int size, Vector2 from, Vector2 to, float width)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 2);
        for (int step = 0; step <= steps; step++)
        {
            Vector2 point = Vector2.Lerp(from, to, (float)step / Mathf.Max(1, steps));
            for (int y = Mathf.FloorToInt(point.y - width - 1); y <= Mathf.CeilToInt(point.y + width + 1); y++)
                for (int x = Mathf.FloorToInt(point.x - width - 1); x <= Mathf.CeilToInt(point.x + width + 1); x++)
                {
                    if (y < 0 || y >= size) continue;
                    float alpha = Mathf.Clamp01(width + .5f - Vector2.Distance(new Vector2(x, y), point));
                    int index = y * size + ((x % size) + size) % size;
                    pixels[index] = Color.Lerp(pixels[index], new Color(.10f, .065f, .08f, pixels[index].a), alpha);
                }
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < _shells.Length; i++)
            if (_shells[i] != null) _shells[i].sharedMaterials = _original[i];
        foreach (UnityEngine.Object resource in _owned) UnityEngine.Object.Destroy(resource);
        _owned.Clear();
        if (_broken != null) UnityEngine.Object.Destroy(_broken);
    }
}
