using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hiển thị tên scene hiện tại và bộ đếm thời gian tăng dần ở góc trái dưới trong các màn chơi.
public sealed class GameplayInfoHud : MonoBehaviour
{
    private TMP_Text sceneLabel;
    private TMP_Text timerLabel;
    private int lastDisplayedSecond = -1;
    private string lastDisplayedScene = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameSpeedrunTimer.IsGameplayScene(scene.name)) return;

        var host = new GameObject("Gameplay Info HUD");
        SceneManager.MoveGameObjectToScene(host, scene);
        var hud = host.AddComponent<GameplayInfoHud>();
        hud.BuildHud(scene);
    }

    private void BuildHud(Scene scene)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Tạo khung gọn ở góc trái dưới, tắt raycastTarget để không chặn thao tác chuột trong game.
        GameObject panelObj = new GameObject("BottomLeftInfo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(transform, false);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(320f, 74f);

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.05f, 0.08f, 0.14f, 0.65f);
        panelBg.raycastTarget = false;

        TMP_FontAsset font = FindSceneFont(scene);
        sceneLabel = CreateLine(panelObj.transform, "SceneNameText", new Vector2(14f, -8f), 22f,
            new Color(0.82f, 0.92f, 1f, 1f), font);
        timerLabel = CreateLine(panelObj.transform, "TimerText", new Vector2(14f, -38f), 26f,
            new Color(1f, 0.92f, 0.45f, 1f), font);

        RefreshLabels(true);
    }

    private static TMP_Text CreateLine(
        Transform parent,
        string objectName,
        Vector2 anchoredPos,
        float fontSize,
        Color color,
        TMP_FontAsset font)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(-28f, 30f);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }

    private static TMP_FontAsset FindSceneFont(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            TMP_Text sample = root.GetComponentInChildren<TMP_Text>(true);
            if (sample != null && sample.font != null) return sample.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private void Update() => RefreshLabels(false);

    private void RefreshLabels(bool force)
    {
        string sceneName = gameObject.scene.name;
        if (force || sceneName != lastDisplayedScene)
        {
            lastDisplayedScene = sceneName;
            if (sceneLabel != null) sceneLabel.text = $"Scene: {sceneName}";
        }

        float elapsed = GameSpeedrunTimer.Instance != null ? GameSpeedrunTimer.Instance.ElapsedSeconds : 0f;
        int wholeSecond = Mathf.FloorToInt(Mathf.Max(0f, elapsed));
        if (force || wholeSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = wholeSecond;
            if (timerLabel != null)
                timerLabel.text = $"Time: {GameSpeedrunTimer.FormatTime(elapsed, false)}";
        }
    }
}
