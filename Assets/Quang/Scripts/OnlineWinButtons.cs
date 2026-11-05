using UnityEngine;
using UnityEngine.UI;

// Chỉ nối UI và âm thanh; việc đồng bộ lượt chơi do session xử lý.
public sealed class OnlineWinButtons : MonoBehaviour
{
    [SerializeField] private AudioClip _buttonClick;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField, Min(0f), Tooltip("Thời điểm bắt đầu phát (giây), bỏ qua khoảng lặng đầu clip.")]
    private float _startTimeOffset = 3.2f;
    private IOnlineRoundActions _actions;
    private Button _retry;
    private Button _menu;
    private float _nextClick;

    public void Bind(IOnlineRoundActions actions)
    {
        _actions = actions;
        bool isKitchen = gameObject.scene.name == "Kitchen_Map";

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == "RetryButton") _retry = button;
            if (button.name == "MenuButton") _menu = button;
            if (button.name == "RetryButton" || button.name == "MenuButton")
            {
                var oldSound = button.GetComponent<UIButtonSound>();
                if (oldSound != null) oldSound.enabled = false;
            }
            // Màn 1 chỉ hiển thị 2 nút: Chuyển sang màn 2 và Trở về lobby (ẩn nút Quit)
            if (isKitchen && button.name == "QuitButton")
            {
                button.gameObject.SetActive(false);
            }
        }

        // Tùy biến tiêu đề và nhãn nút cho Màn 1 Kitchen_Map
        if (isKitchen)
        {
            foreach (TMPro.TMP_Text tmp in GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp.gameObject.name == "GameWinTitle") tmp.text = "CHIẾN THẮNG MÀN 1";
                else if (tmp.gameObject.name == "GameWinSubtitle") tmp.text = "BẠN ĐÃ VƯỢT QUA NHÀ BẾP THÀNH CÔNG!";
            }

            if (_retry != null)
            {
                TMPro.TMP_Text retryText = _retry.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (retryText != null) retryText.text = "CHUYỂN SANG MÀN 2";
            }

            if (_menu != null)
            {
                TMPro.TMP_Text menuText = _menu.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (menuText != null) menuText.text = "TRỞ VỀ LOBBY";
            }
        }

        // Thay event trên instance runtime, không sửa prefab gốc của thành viên khác.
        if (_retry != null)
        {
            _retry.onClick = new Button.ButtonClickedEvent();
            if (isKitchen)
            {
                _retry.onClick.AddListener(NextStage);
            }
            else
            {
                _retry.onClick.AddListener(PlayAgain);
            }
        }
        if (_menu != null)
        {
            _menu.onClick = new Button.ButtonClickedEvent();
            _menu.onClick.AddListener(MainMenu);
        }
    }

    public void NextStage() { if (AcceptClick()) _actions.RequestNextStage(); }
    public void PlayAgain() { if (AcceptClick()) _actions.RequestPlayAgain(); }
    public void MainMenu() { if (AcceptClick()) _actions.RequestMainMenu(); }

    private bool AcceptClick()
    {
        if (Time.unscaledTime < _nextClick || _actions == null) return false;
        _nextClick = Time.unscaledTime + 1f;
        if (_buttonClick != null)
        {
            var sound = new GameObject("Online button click", typeof(AudioSource));
            DontDestroyOnLoad(sound);
            var source = sound.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = _buttonClick;
            source.volume = _volume;
            // Giới hạn offset theo độ dài clip để không tua vượt cuối âm thanh.
            float startTime = Mathf.Clamp(_startTimeOffset, 0f, Mathf.Max(0f, _buttonClick.length - .05f));
            source.time = startTime;
            source.Play();
            Destroy(sound, _buttonClick.length - startTime + .2f);
        }
        return true;
    }
}
