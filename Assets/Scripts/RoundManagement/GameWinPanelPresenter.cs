using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

// Tự nối bảng có sẵn trong scene, kể cả bảng đang tắt; không cần sửa prefab tổ.
public sealed class GameWinPanelPresenter : MonoBehaviour
{
    private GameObject _panel;
    private NetworkNest _nest;
    private Button _menuButton;
    private NetworkManager _manager;
    private TMP_Text completionTimeText;
    private bool _won;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // [Kitchen_Map]: Cho phép hiển thị bảng Win trên cả hai màn chơi: MainMap_Online và Kitchen_Map
        if (scene.name != "MainMap_Online" && scene.name != "Kitchen_Map") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "GameWinPanel") continue;
                var owner = new GameObject("Game win panel presenter");
                SceneManager.MoveGameObjectToScene(owner, scene);
                var presenter = owner.AddComponent<GameWinPanelPresenter>();
                presenter._panel = child.gameObject;
                presenter._panel.SetActive(false);
                return;
            }
        Debug.LogWarning($"{scene.name}: không tìm thấy GameWinPanel. Hãy lưu bảng vào scene.");
    }

    private IEnumerator Start()
    {
        foreach (Button button in _panel.GetComponentsInChildren<Button>(true))
            if (button.name == "MenuButton") { _menuButton = button; break; }
        _manager = NetworkManager.Singleton;
        if (_manager != null) _manager.OnClientStopped += OnClientStopped;
        // Chỉ tìm trong giai đoạn chờ tổ spawn, không tìm mỗi frame gameplay.
        while (_nest == null)
        {
            foreach (NetworkNest nest in FindObjectsByType<NetworkNest>(FindObjectsSortMode.None))
                if (nest.gameObject.scene == gameObject.scene) { _nest = nest; break; }
            if (_nest == null) yield return new WaitForSecondsRealtime(.25f);
        }
        _nest.WinChanged += Show;
        var actions = _nest.GetComponent<IOnlineRoundActions>();
        var buttons = _panel.GetComponent<OnlineWinButtons>();
        if (buttons == null) buttons = _panel.AddComponent<OnlineWinButtons>();
        buttons.Bind(actions);
        Show(_nest.HasWon);
    }

    private void Show(bool won)
    {
        if (_nest == null || !_nest.IsSpawned) return;
        bool wasWon = _won;
        _won = won;
        if (_panel != null) _panel.SetActive(won);

        if (!won)
        {
            if (wasWon && GameSpeedrunTimer.Instance != null)
                GameSpeedrunTimer.Instance.NotifyRoundRestarted();
            return;
        }

        if (GameSpeedrunTimer.Instance != null)
            GameSpeedrunTimer.Instance.NotifyRoundWon();

        if (gameObject.scene.name == GameSpeedrunTimer.MainMapSceneName)
            UpdateCompletionTimeLabel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateCompletionTimeLabel()
    {
        if (_panel == null) return;

        if (completionTimeText == null)
        {
            Transform existing = _panel.transform.Find("GameWinCompletionTime");
            if (existing != null) existing.TryGetComponent(out completionTimeText);
        }

        if (completionTimeText == null)
            completionTimeText = CreateCompletionTimeLabel();

        float totalSeconds = GameSpeedrunTimer.Instance != null ? GameSpeedrunTimer.Instance.ElapsedSeconds : 0f;
        completionTimeText.text = $"TIME: {GameSpeedrunTimer.FormatTime(totalSeconds, true)}";
        completionTimeText.gameObject.SetActive(true);
    }

    private TMP_Text CreateCompletionTimeLabel()
    {
        var labelObj = new GameObject("GameWinCompletionTime", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(_panel.transform, false);

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // Đặt vào khoảng trống giữa GameWinSubtitle (y = 135) và RetryButton (y = 5).
        rect.anchoredPosition = new Vector2(0f, 74f);
        rect.sizeDelta = new Vector2(660f, 44f);

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        TMP_Text subtitle = FindSubtitleTemplate();
        if (subtitle != null && subtitle.font != null) label.font = subtitle.font;
        label.fontSize = 32f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.88f, 0.35f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }

    private TMP_Text FindSubtitleTemplate()
    {
        foreach (TMP_Text candidate in _panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (candidate.gameObject.name == "GameWinSubtitle") return candidate;
        }
        return _panel.GetComponentInChildren<TMP_Text>(true);
    }

    private void OnDestroy()
    {
        if (_menuButton != null) _menuButton.onClick.RemoveListener(ReturnToMenuTransition.Begin);
        if (_manager != null) _manager.OnClientStopped -= OnClientStopped;
        if (_nest != null) _nest.WinChanged -= Show;
    }

    private void OnClientStopped(bool wasHost)
    {
        if (_won) ReturnToMenuTransition.Begin();
    }
}
