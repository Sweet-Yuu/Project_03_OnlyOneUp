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
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == "RetryButton") _retry = button;
            if (button.name == "MenuButton") _menu = button;
            if (button.name == "RetryButton" || button.name == "MenuButton")
            {
                var oldSound = button.GetComponent<UIButtonSound>();
                if (oldSound != null) oldSound.enabled = false;
            }
        }
        // Thay event trên instance runtime, không sửa prefab gốc của thành viên khác.
        if (_retry != null)
        {
            _retry.onClick = new Button.ButtonClickedEvent();
            _retry.onClick.AddListener(PlayAgain);
        }
        if (_menu != null)
        {
            _menu.onClick = new Button.ButtonClickedEvent();
            _menu.onClick.AddListener(MainMenu);
        }
    }

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
