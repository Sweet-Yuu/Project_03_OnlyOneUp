using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [FormerlySerializedAs("gameOverPanel")]
    [SerializeField] private GameObject _gameOverPanel;

    [FormerlySerializedAs("gameWinPanel")]
    [SerializeField] private GameObject _gameWinPanel;

    [Header("Scene Navigation")]
    [SerializeField] private string _mainMenuSceneName = "Menu_Scene";
    [SerializeField] private string _defaultGameplaySceneName = "Kitchen_Map";

    [Header("Game Win Audio")]
    [SerializeField] private AudioClip _winMusicClip;
    [SerializeField] [Range(0f, 1f)] private float _winMusicVolume = 0.85f;

    [Header("Editor Preview")]
    [SerializeField] private bool _previewWinInEditor;

    private IGameSceneNavigator _sceneNavigator;
    private IAudioService _audioService;
    private AudioSource _localAudioSource;

    /// <summary>
    /// Cho phép inject các dependency từ bên ngoài (Dependency Inversion)
    /// </summary>
    public void Initialize(IGameSceneNavigator sceneNavigator, IAudioService audioService = null)
    {
        _sceneNavigator = sceneNavigator;
        _audioService = audioService;
    }

    private void Awake()
    {
        // Khởi tạo bộ điều hướng mặc định nếu chưa được inject
        _sceneNavigator ??= new UnitySceneNavigator();
        _audioService ??= AudioManager.Instance;

        // Cache AudioSource tại chỗ để đảm bảo nhạc Win luôn phát được kể cả khi Scene chưa có AudioManager
        _localAudioSource = GetComponent<AudioSource>();
        if (_localAudioSource == null)
        {
            _localAudioSource = gameObject.AddComponent<AudioSource>();
        }

        _localAudioSource.playOnAwake = false;
        _localAudioSource.spatialBlend = 0f;
        _localAudioSource.loop = true;
    }

    private void Start()
    {
        // Mở khóa con trỏ chuột để người chơi tương tác với các nút UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // Xác định trạng thái hiển thị: ưu tiên kết quả từ màn chơi, nếu chạy thử trực tiếp Scene UI thì dùng biến preview
        bool shouldShowWin = GameResultSession.HasRecordedResult
            ? GameResultSession.IsGameWin
            : _previewWinInEditor;

        DisplayResultPanel(shouldShowWin);
    }

    /// <summary>
    /// Bật Panel tương ứng với kết quả Thắng hoặc Thua và phát nhạc chiến thắng nếu người chơi thắng
    /// </summary>
    public void DisplayResultPanel(bool isWin)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(!isWin);
        }

        if (_gameWinPanel != null)
        {
            _gameWinPanel.SetActive(isWin);
        }

        if (isWin)
        {
            PlayWinMusic();
        }
        else
        {
            StopWinMusic();
        }
    }

    private void PlayWinMusic()
    {
        if (_winMusicClip == null)
        {
            return;
        }

        // Ưu tiên phát qua IAudioService/AudioManager nếu có trong Scene, ngược lại phát bằng AudioSource nội bộ
        _audioService ??= AudioManager.Instance;
        if (_audioService != null)
        {
            _audioService.SetMusicVolume(_winMusicVolume);
            _audioService.PlayMusic(_winMusicClip, true);
            return;
        }

        if (_localAudioSource != null)
        {
            _localAudioSource.clip = _winMusicClip;
            _localAudioSource.volume = _winMusicVolume;
            _localAudioSource.Play();
        }
    }

    private void StopWinMusic()
    {
        if (_audioService != null)
        {
            _audioService.StopMusic();
        }

        if (_localAudioSource != null && _localAudioSource.isPlaying)
        {
            _localAudioSource.Stop();
        }
    }

    public void ShowGameOver()
    {
        DisplayResultPanel(false);
    }

    public void ShowGameWin()
    {
        DisplayResultPanel(true);
    }

    public void RetryLevel()
    {
        StopWinMusic();

        string targetScene = !string.IsNullOrEmpty(GameResultSession.PreviousSceneName)
            ? GameResultSession.PreviousSceneName
            : _defaultGameplaySceneName;

        _sceneNavigator.LoadScene(targetScene);
    }

    public void LoadMainMenu()
    {
        StopWinMusic();
        _sceneNavigator.LoadScene(_mainMenuSceneName);
    }

    public void QuitGame()
    {
        StopWinMusic();
        _sceneNavigator.QuitApplication();
    }

    public static void TriggerGameOver(string levelName)
    {
        GameResultSession.OpenResultScene(false, levelName);
    }

    public static void TriggerGameWin(string levelName)
    {
        GameResultSession.OpenResultScene(true, levelName);
    }
}
