using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Hiển thị dòng chữ bay hất lên từ đầu nhân vật rồi mờ dần theo góc nhìn camera.
public sealed class HonkPopupText : MonoBehaviour
{
    private TextMeshPro label;
    private Vector3 startPosition;
    private Vector3 travelOffset;
    private Vector3 jitterOffset;
    private Color baseColor;
    private float riseDuration;
    private float fadeDuration;
    private float elapsed;

    public static void Spawn(
        Vector3 worldPosition,
        Vector3 travelOffset,
        string message,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float riseDuration,
        float fadeDuration)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        GameObject go = new GameObject("HonkPopupText");
        go.transform.position = worldPosition;

        HonkPopupText popup = go.AddComponent<HonkPopupText>();
        popup.Initialize(worldPosition, travelOffset, message, color, font, fontSize, riseDuration, fadeDuration);
    }

    private void Initialize(
        Vector3 worldPosition,
        Vector3 travelOffset,
        string message,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float riseDuration,
        float fadeDuration)
    {
        startPosition = worldPosition;
        this.travelOffset = travelOffset;
        baseColor = color;
        this.riseDuration = Mathf.Max(0.05f, riseDuration);
        this.fadeDuration = Mathf.Max(0.05f, fadeDuration);

        // Lệch nhẹ ngẫu nhiên để bấm liên tiếp các dòng chữ không đè khít lên nhau.
        jitterOffset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.15f), 0f);

        label = gameObject.AddComponent<TextMeshPro>();
        if (font != null) label.font = font;
        label.text = message;
        label.fontSize = Mathf.Max(1f, fontSize);
        label.fontStyle = FontStyles.Bold;
        label.color = baseColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.richText = false;

        if (label.TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        AlignToCamera();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float totalDuration = riseDuration + fadeDuration;
        if (elapsed >= totalDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (elapsed <= riseDuration)
        {
            float t = elapsed / riseDuration;
            // Hất nhanh ở đầu theo hướng chếch rồi chậm dần khi tới đỉnh.
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = startPosition + travelOffset * eased + jitterOffset * t;

            float scale = t < 0.25f
                ? Mathf.Lerp(0.75f, 1.12f, t / 0.25f)
                : Mathf.Lerp(1.12f, 1f, (t - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * scale;
            SetAlpha(baseColor.a);
            return;
        }

        float fadeProgress = (elapsed - riseDuration) / fadeDuration;
        transform.position = startPosition
            + travelOffset * (1f + 0.15f * fadeProgress)
            + jitterOffset;
        transform.localScale = Vector3.one;
        SetAlpha(baseColor.a * (1f - fadeProgress));
    }

    private void LateUpdate() => AlignToCamera();

    private void AlignToCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    private void SetAlpha(float alpha)
    {
        if (label == null) return;
        Color c = baseColor;
        c.a = Mathf.Clamp01(alpha);
        label.color = c;
    }
}

