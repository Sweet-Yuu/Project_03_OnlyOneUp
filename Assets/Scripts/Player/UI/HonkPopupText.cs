using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Hiển thị dòng chữ bay hất lên từ đầu nhân vật rồi mờ dần theo góc nhìn camera.
public sealed class HonkPopupText : MonoBehaviour
{
    private TextMeshPro label;
    private Vector3 startPosition;
    private Vector3 driftVelocity;
    private Color baseColor;
    private float riseDistance;
    private float riseDuration;
    private float fadeDuration;
    private float elapsed;

    public static void Spawn(
        Vector3 worldPosition,
        string message,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float riseDistance,
        float riseDuration,
        float fadeDuration)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        GameObject go = new GameObject("HonkPopupText");
        go.transform.position = worldPosition;

        HonkPopupText popup = go.AddComponent<HonkPopupText>();
        popup.Initialize(worldPosition, message, color, font, fontSize, riseDistance, riseDuration, fadeDuration);
    }

    private void Initialize(
        Vector3 worldPosition,
        string message,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float riseDistance,
        float riseDuration,
        float fadeDuration)
    {
        startPosition = worldPosition;
        baseColor = color;
        this.riseDistance = Mathf.Max(0.1f, riseDistance);
        this.riseDuration = Mathf.Max(0.05f, riseDuration);
        this.fadeDuration = Mathf.Max(0.05f, fadeDuration);

        // Tạo độ lệch ngang nhẹ để khi bấm liên tiếp các chữ không đè khít lên nhau.
        driftVelocity = new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.2f, 0.2f));

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
            // Hất nhanh ở đầu rồi chậm dần khi lên đỉnh.
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = startPosition + Vector3.up * (riseDistance * eased) + driftVelocity * elapsed;

            float scale = t < 0.25f
                ? Mathf.Lerp(0.75f, 1.12f, t / 0.25f)
                : Mathf.Lerp(1.12f, 1f, (t - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * scale;
            SetAlpha(baseColor.a);
            return;
        }

        float fadeProgress = (elapsed - riseDuration) / fadeDuration;
        transform.position = startPosition
            + Vector3.up * (riseDistance * (1f + 0.15f * fadeProgress))
            + driftVelocity * elapsed;
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

