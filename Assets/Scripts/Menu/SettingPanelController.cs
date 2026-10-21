using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện cài đặt (Setting_Panel) bao gồm chuyển tab và cài đặt âm thanh.
/// </summary>
public class SettingPanelController : MonoBehaviour
{
    [Header("=== Tabs & Panels ===")]
    [SerializeField] private GameObject _soundPanel;
    [SerializeField] private GameObject _graphicPanel;
    [SerializeField] private Button _soundTabButton;
    [SerializeField] private Button _graphicTabButton;
    [SerializeField] private Button _exitButton;

    [Header("=== Tab Button Colors ===")]
    [SerializeField] private Color _activeTabColor = new Color(0.98f, 0.80f, 0.56f, 1f);
    [SerializeField] private Color _inactiveTabColor = new Color(0.70f, 0.70f, 0.70f, 1f);

    [Header("=== Master Audio Controls ===")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Button _masterMuteButton;
    [SerializeField] private TMP_Text _masterValueText;
    [SerializeField] private TMP_Text _masterMuteText;

    [Header("=== Music Audio Controls ===")]
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Button _musicMuteButton;
    [SerializeField] private TMP_Text _musicValueText;
    [SerializeField] private TMP_Text _musicMuteText;

    [Header("=== SFX Audio Controls ===")]
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Button _sfxMuteButton;
    [SerializeField] private TMP_Text _sfxValueText;
    [SerializeField] private TMP_Text _sfxMuteText;

    [Header("=== Mute Button Visuals ===")]
    [SerializeField] private Color _unmutedColor = new Color(0.94f, 0.46f, 0.08f, 1f);
    [SerializeField] private Color _mutedColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    // PlayerPrefs Keys
    private const string KEY_MASTER_VOL = "Setting_MasterVol";
    private const string KEY_MUSIC_VOL = "Setting_MusicVol";
    private const string KEY_SFX_VOL = "Setting_SFXVol";
    private const string KEY_MASTER_MUTE = "Setting_MasterMute";
    private const string KEY_MUSIC_MUTE = "Setting_MusicMute";
    private const string KEY_SFX_MUTE = "Setting_SFXMute";

    // Giá trị âm lượng hiện tại
    private float _masterVolume = 1f;
    private float _musicVolume = 0.8f;
    private float _sfxVolume = 1f;

    // Trạng thái Mute
    private bool _isMasterMuted = false;
    private bool _isMusicMuted = false;
    private bool _isSfxMuted = false;

    // Lưu âm lượng trước khi mute để khôi phục khi unmute
    private float _lastMasterVolume = 1f;
    private float _lastMusicVolume = 0.8f;
    private float _lastSfxVolume = 1f;

    private void Awake()
    {
        LoadSettings();
        RegisterEvents();
    }

    private void Start()
    {
        ApplyAudioSettings();
        UpdateAllUI();
    }

    private void OnEnable()
    {
        // Mặc định luôn mở giao diện panel sound setting khi Setting_Panel được hiển thị
        ShowSoundTab();
    }

    private void RegisterEvents()
    {
        if (_soundTabButton != null)
            _soundTabButton.onClick.AddListener(ShowSoundTab);

        if (_graphicTabButton != null)
            _graphicTabButton.onClick.AddListener(ShowGraphicTab);

        if (_exitButton != null)
            _exitButton.onClick.AddListener(ClosePanel);

        // Master events
        if (_masterSlider != null)
            _masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        if (_masterMuteButton != null)
            _masterMuteButton.onClick.AddListener(ToggleMasterMute);

        // Music events
        if (_musicSlider != null)
            _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        if (_musicMuteButton != null)
            _musicMuteButton.onClick.AddListener(ToggleMusicMute);

        // SFX events
        if (_sfxSlider != null)
            _sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        if (_sfxMuteButton != null)
            _sfxMuteButton.onClick.AddListener(ToggleSfxMute);
    }

    #region Tab Navigation
    public void ShowSoundTab()
    {
        if (_soundPanel != null) _soundPanel.SetActive(true);
        if (_graphicPanel != null) _graphicPanel.SetActive(false);

        UpdateTabButtons(isSoundActive: true);
    }

    public void ShowGraphicTab()
    {
        if (_soundPanel != null) _soundPanel.SetActive(false);
        if (_graphicPanel != null) _graphicPanel.SetActive(true);

        UpdateTabButtons(isSoundActive: false);
    }

    private void UpdateTabButtons(bool isSoundActive)
    {
        if (_soundTabButton != null)
        {
            var img = _soundTabButton.GetComponent<Image>();
            if (img != null) img.color = isSoundActive ? _activeTabColor : _inactiveTabColor;
        }

        if (_graphicTabButton != null)
        {
            var img = _graphicTabButton.GetComponent<Image>();
            if (img != null) img.color = !isSoundActive ? _activeTabColor : _inactiveTabColor;
        }
    }

    public void ClosePanel()
    {
        SaveSettings();
        gameObject.SetActive(false);
    }
    #endregion

    #region Audio Settings Logic
    private void OnMasterSliderChanged(float value)
    {
        _masterVolume = value;
        if (_masterVolume > 0f && _isMasterMuted)
        {
            _isMasterMuted = false;
        }
        _lastMasterVolume = _masterVolume > 0f ? _masterVolume : _lastMasterVolume;
        ApplyMasterAudio();
        UpdateMasterUI();
        SaveSettings();
    }

    public void ToggleMasterMute()
    {
        _isMasterMuted = !_isMasterMuted;
        if (_isMasterMuted)
        {
            if (_masterVolume > 0f) _lastMasterVolume = _masterVolume;
            _masterVolume = 0f;
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            _masterVolume = _lastMasterVolume > 0f ? _lastMasterVolume : 1f;
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(_masterVolume);
        }

        ApplyMasterAudio();
        UpdateMasterUI();
        SaveSettings();
    }

    private void OnMusicSliderChanged(float value)
    {
        _musicVolume = value;
        if (_musicVolume > 0f && _isMusicMuted)
        {
            _isMusicMuted = false;
        }
        _lastMusicVolume = _musicVolume > 0f ? _musicVolume : _lastMusicVolume;
        ApplyMusicAudio();
        UpdateMusicUI();
        SaveSettings();
    }

    public void ToggleMusicMute()
    {
        _isMusicMuted = !_isMusicMuted;
        if (_isMusicMuted)
        {
            if (_musicVolume > 0f) _lastMusicVolume = _musicVolume;
            _musicVolume = 0f;
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            _musicVolume = _lastMusicVolume > 0f ? _lastMusicVolume : 0.8f;
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(_musicVolume);
        }

        ApplyMusicAudio();
        UpdateMusicUI();
        SaveSettings();
    }

    private void OnSfxSliderChanged(float value)
    {
        _sfxVolume = value;
        if (_sfxVolume > 0f && _isSfxMuted)
        {
            _isSfxMuted = false;
        }
        _lastSfxVolume = _sfxVolume > 0f ? _sfxVolume : _lastSfxVolume;
        ApplySfxAudio();
        UpdateSfxUI();
        SaveSettings();
    }

    public void ToggleSfxMute()
    {
        _isSfxMuted = !_isSfxMuted;
        if (_isSfxMuted)
        {
            if (_sfxVolume > 0f) _lastSfxVolume = _sfxVolume;
            _sfxVolume = 0f;
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            _sfxVolume = _lastSfxVolume > 0f ? _lastSfxVolume : 1f;
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(_sfxVolume);
        }

        ApplySfxAudio();
        UpdateSfxUI();
        SaveSettings();
    }

    private void ApplyAudioSettings()
    {
        ApplyMasterAudio();
        ApplyMusicAudio();
        ApplySfxAudio();
    }

    private void ApplyMasterAudio()
    {
        AudioListener.volume = _isMasterMuted ? 0f : _masterVolume;
    }

    private void ApplyMusicAudio()
    {
        float effectiveVol = _isMusicMuted ? 0f : _musicVolume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(effectiveVol);
        }
    }

    private void ApplySfxAudio()
    {
        float effectiveVol = _isSfxMuted ? 0f : _sfxVolume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(effectiveVol);
        }
    }
    #endregion

    #region UI Updates
    private void UpdateAllUI()
    {
        UpdateMasterUI();
        UpdateMusicUI();
        UpdateSfxUI();
    }

    private void UpdateMasterUI()
    {
        if (_masterSlider != null)
            _masterSlider.SetValueWithoutNotify(_masterVolume);

        if (_masterValueText != null)
            _masterValueText.text = $"{Mathf.RoundToInt(_masterVolume * 100f)}%";

        if (_masterMuteText != null)
            _masterMuteText.text = _isMasterMuted ? "Unmute" : "Mute";

        if (_masterMuteButton != null)
        {
            var img = _masterMuteButton.GetComponent<Image>();
            if (img != null) img.color = _isMasterMuted ? _mutedColor : _unmutedColor;
        }
    }

    private void UpdateMusicUI()
    {
        if (_musicSlider != null)
            _musicSlider.SetValueWithoutNotify(_musicVolume);

        if (_musicValueText != null)
            _musicValueText.text = $"{Mathf.RoundToInt(_musicVolume * 100f)}%";

        if (_musicMuteText != null)
            _musicMuteText.text = _isMusicMuted ? "Unmute" : "Mute";

        if (_musicMuteButton != null)
        {
            var img = _musicMuteButton.GetComponent<Image>();
            if (img != null) img.color = _isMusicMuted ? _mutedColor : _unmutedColor;
        }
    }

    private void UpdateSfxUI()
    {
        if (_sfxSlider != null)
            _sfxSlider.SetValueWithoutNotify(_sfxVolume);

        if (_sfxValueText != null)
            _sfxValueText.text = $"{Mathf.RoundToInt(_sfxVolume * 100f)}%";

        if (_sfxMuteText != null)
            _sfxMuteText.text = _isSfxMuted ? "Unmute" : "Mute";

        if (_sfxMuteButton != null)
        {
            var img = _sfxMuteButton.GetComponent<Image>();
            if (img != null) img.color = _isSfxMuted ? _mutedColor : _unmutedColor;
        }
    }
    #endregion

    #region Persistence
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, _masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, _musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, _sfxVolume);
        PlayerPrefs.SetInt(KEY_MASTER_MUTE, _isMasterMuted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_MUSIC_MUTE, _isMusicMuted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SFX_MUTE, _isSfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
        _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);

        _isMasterMuted = PlayerPrefs.GetInt(KEY_MASTER_MUTE, 0) == 1;
        _isMusicMuted = PlayerPrefs.GetInt(KEY_MUSIC_MUTE, 0) == 1;
        _isSfxMuted = PlayerPrefs.GetInt(KEY_SFX_MUTE, 0) == 1;

        _lastMasterVolume = _masterVolume > 0f ? _masterVolume : 1f;
        _lastMusicVolume = _musicVolume > 0f ? _musicVolume : 0.8f;
        _lastSfxVolume = _sfxVolume > 0f ? _sfxVolume : 1f;
    }
    #endregion
}
