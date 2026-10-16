using UnityEngine;

public class AudioManager : MonoBehaviour, IAudioService
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _backgroundMusicClip;
    [SerializeField] private AudioClip _gameWinMusicClip;
    [SerializeField] private AudioClip _gameOverMusicClip;
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] [Tooltip("Thời gian bắt đầu phát (giây) của âm thanh click nút để loại bỏ khoảng lặng đầu file")]
    private float _buttonClickStartTime = 1.95f;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;
    [SerializeField] private bool _persistAcrossScenes = true;

    private void Awake()
    {
        // Đảm bảo chỉ có một AudioManager tồn tại nếu chọn giữ qua các Scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializeAudioSources();
    }

    private void Start()
    {
        // Tự động phát nhạc nền mặc định nếu được gắn sẵn trên Inspector
        if (_backgroundMusicClip != null && !_musicSource.isPlaying)
        {
            PlayMusic(_backgroundMusicClip, true);
        }
    }

    /// <summary>
    /// Tự động khởi tạo và cache AudioSource nếu chưa được kéo thả trên Inspector
    /// </summary>
    private void InitializeAudioSources()
    {
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }

        _musicSource.playOnAwake = false;
        _musicSource.spatialBlend = 0f; // Âm thanh 2D
        _musicSource.volume = _musicVolume;

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }

        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
        _sfxSource.volume = _sfxVolume;
    }

    /// <summary>
    /// Phát một bản nhạc (BGM / Nhạc chiến thắng / Nhạc thua cuộc)
    /// </summary>
    public void PlayMusic(AudioClip clip, bool isLooping = true)
    {
        if (clip == null || _musicSource == null)
        {
            return;
        }

        // Nếu đang phát đúng bài nhạc đó rồi thì không khởi động lại từ đầu
        if (_musicSource.isPlaying && _musicSource.clip == clip)
        {
            return;
        }

        _musicSource.clip = clip;
        _musicSource.loop = isLooping;
        _musicSource.volume = _musicVolume;
        _musicSource.Play();
    }

    /// <summary>
    /// Phát nhạc chiến thắng đã được cấu hình sẵn trong AudioManager
    /// </summary>
    public void PlayWinMusic()
    {
        PlayMusic(_gameWinMusicClip, true);
    }

    /// <summary>
    /// Phát nhạc thua cuộc đã được cấu hình sẵn trong AudioManager
    /// </summary>
    public void PlayGameOverMusic()
    {
        PlayMusic(_gameOverMusicClip, false);
    }

    /// <summary>
    /// Phát âm thanh click nút (SFX) mặc định đã cấu hình trong AudioManager
    /// </summary>
    public void PlayButtonClick()
    {
        if (_buttonClickClip != null)
        {
            PlaySFX(_buttonClickClip, _sfxVolume, _buttonClickStartTime);
        }
    }

    public void StopMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            _musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            _musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (_musicSource != null && !_musicSource.isPlaying && _musicSource.clip != null)
        {
            _musicSource.UnPause();
        }
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh ngắn (SFX) không làm ngắt nhạc nền. Có hỗ trợ thời gian bắt đầu (startTime) để bỏ qua khoảng lặng.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float startTime = 0f)
    {
        if (clip == null || _sfxSource == null)
        {
            return;
        }

        float finalVolume = _sfxVolume * Mathf.Clamp01(volumeScale);
        float clampedStart = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.05f));

        // Nếu có thời gian bắt đầu > 0, tua trực tiếp bằng AudioSource.time để phát ngay lập tức
        if (clampedStart > 0f)
        {
            _sfxSource.clip = clip;
            _sfxSource.volume = finalVolume;
            _sfxSource.time = clampedStart;
            _sfxSource.Play();
            _sfxSource.time = clampedStart;
        }
        else
        {
            _sfxSource.PlayOneShot(clip, finalVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null)
        {
            _musicSource.volume = _musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        if (_sfxSource != null)
        {
            _sfxSource.volume = _sfxVolume;
        }
    }
}
