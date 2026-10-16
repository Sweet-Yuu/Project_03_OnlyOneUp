using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Hiệu ứng local cho một lần trứng nứt: rung khi render và đỏ mờ ở bốn góc.
[RequireComponent(typeof(Canvas))]
public sealed class EggDamageFeedback : MonoBehaviour
{
    private Camera _camera;
    private Canvas _canvas;
    private RawImage _image;
    private Texture2D _gradient;
    private uint _generation;
    private byte _damage;
    private uint _impactSequence;
    private bool _initialized;
    private bool _applied;
    private Pose _basePose;
    private float _started = float.NegativeInfinity;
    private float _shakeDuration;
    private float _shakeStrength;
    private float _flashDuration;
    private float _intensity;

    public void Initialize(Camera camera, float shakeDuration, float shakeStrength, float flashDuration)
    {
        _camera = camera;
        _shakeDuration = Mathf.Max(.01f, shakeDuration);
        _shakeStrength = Mathf.Max(0f, shakeStrength);
        _flashDuration = Mathf.Max(.01f, flashDuration);
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32000;
        var imageObject = new GameObject("Red gradient", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(transform, false);
        _image = imageObject.GetComponent<RawImage>();
        _image.raycastTarget = false;
        RectTransform rect = _image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        _gradient = CreateGradient();
        _image.texture = _gradient;
        Clear();
    }

    private IEnumerator Start()
    {
        // Chỉ tìm lại trong giai đoạn chờ camera được tạo, không tìm trong Update.
        while (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) yield return new WaitForSecondsRealtime(.25f);
        }
    }

    public void Observe(uint generation, byte damage, NestEggPhase phase, uint impactSequence = 0)
    {
        // Snapshot đầu tiên/đổi vòng chỉ thiết lập mốc: vào muộn không bị rung vì vết nứt cũ.
        if (!_initialized || generation != _generation)
        {
            _initialized = true;
            _generation = generation;
            _damage = damage;
            _impactSequence = impactSequence;
            Clear();
            return;
        }
        bool nestImpact = impactSequence != _impactSequence;
        _impactSequence = impactSequence;
        if (phase == NestEggPhase.Waiting || damage < _damage)
        {
            _damage = damage;
            Clear();
            return;
        }
        if (nestImpact || (phase == NestEggPhase.Intact && damage > _damage && damage <= 2))
        {
            _intensity = damage == 1 ? 1f : 1.35f;
            _started = Time.unscaledTime;
            Update();
        }
        _damage = damage;
    }

    private void Update()
    {
        if (_image == null) return;
        float progress = Mathf.Clamp01((Time.unscaledTime - _started) / (_flashDuration * _intensity));
        float alpha = .42f * _intensity * (1f - progress) * (1f - progress);
        _image.color = new Color(1f, 1f, 1f, alpha);
        _canvas.enabled = alpha > .001f;
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginRender;
        RenderPipelineManager.endCameraRendering += EndRender;
        Camera.onPreCull += ApplyShake;
        Camera.onPostRender += RestoreCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginRender;
        RenderPipelineManager.endCameraRendering -= EndRender;
        Camera.onPreCull -= ApplyShake;
        Camera.onPostRender -= RestoreCamera;
        Clear();
    }

    private void BeginRender(ScriptableRenderContext context, Camera camera) => ApplyShake(camera);
    private void EndRender(ScriptableRenderContext context, Camera camera) => RestoreCamera(camera);

    private void ApplyShake(Camera camera)
    {
        if (camera == null || camera != _camera || _applied || !isActiveAndEnabled) return;
        float age = Time.unscaledTime - _started;
        float duration = _shakeDuration * _intensity;
        if (duration <= 0f || age < 0f || age >= duration) return;
        float envelope = Mathf.Pow(1f - age / duration, 2) * _intensity;
        float x = Mathf.Sin(age * 93f + .7f);
        float y = Mathf.Sin(age * 117f + 1.3f);
        _basePose = new Pose(camera.transform.position, camera.transform.rotation);
        // Áp sau Cinemachine, trả lại ngay sau render: không tích lũy lệch camera
        // và không làm hướng điều khiển nhân vật rung theo hình ảnh.
        camera.transform.SetPositionAndRotation(
            _basePose.position + _basePose.rotation * new Vector3(x, y, 0f) * (_shakeStrength * envelope),
            _basePose.rotation * Quaternion.Euler(
                y * envelope * _shakeStrength * 10f, x * envelope * _shakeStrength * 5f,
                -x * envelope * _shakeStrength * 6f));
        _applied = true;
    }

    private void RestoreCamera(Camera camera)
    {
        if (!_applied || camera != _camera) return;
        if (_camera != null) _camera.transform.SetPositionAndRotation(_basePose.position, _basePose.rotation);
        _applied = false;
    }

    private void Clear()
    {
        RestoreCamera(_camera);
        _started = float.NegativeInfinity;
        if (_canvas != null) _canvas.enabled = false;
    }

    private static Texture2D CreateGradient()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { name = "Egg damage corner gradient", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float horizontal = Mathf.Pow(Mathf.Abs(2f * x / (size - 1) - 1f), 2.5f);
                float vertical = Mathf.Pow(Mathf.Abs(2f * y / (size - 1) - 1f), 2.5f);
                pixels[y * size + x] = new Color(.9f, .015f, .025f, horizontal * vertical);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private void OnDestroy()
    {
        if (_gradient != null) Destroy(_gradient);
    }
}
