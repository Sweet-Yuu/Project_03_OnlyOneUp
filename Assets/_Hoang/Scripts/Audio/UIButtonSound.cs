using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] [Range(0f, 1f)] private float _volume = 1f;

    [Tooltip("Thời gian bắt đầu phát (giây) để bỏ qua khoảng lặng ở đầu file âm thanh")]
    [SerializeField] private float _startTimeOffset = 3.2f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(PlaySound);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlaySound);
        }
    }

    /// <summary>
    /// Phát âm thanh khi nút được click. Đảm bảo âm thanh phát ngay lập tức từ mốc thời gian đã cấu hình,
    /// đồng thời không bị ngắt kể cả khi chuyển Scene ngay lập tức.
    /// </summary>
    public void PlaySound()
    {
        AudioClip clipToPlay = _clickClip;

        // Nếu có AudioManager trong Scene thì ưu tiên phát qua AudioManager
        if (AudioManager.Instance != null)
        {
            if (clipToPlay != null)
            {
                AudioManager.Instance.PlaySFX(clipToPlay, _volume, _startTimeOffset);
            }
            else
            {
                AudioManager.Instance.PlayButtonClick();
            }
            return;
        }

        // Nếu clip trống và không có AudioManager thì dừng
        if (clipToPlay == null)
        {
            return;
        }

        // Fallback: Tạo một AudioSource tạm thời được giữ qua DontDestroyOnLoad để âm thanh không bị ngắt khi LoadScene
        PlayClipPersistent(clipToPlay, _volume, _startTimeOffset);
    }

    private static void PlayClipPersistent(AudioClip clip, float volume, float startTimeOffset = 0f)
    {
        if (clip == null) return;

        GameObject tempGameObject = new GameObject("ButtonSound_OneShot");
        DontDestroyOnLoad(tempGameObject);

        AudioSource audioSource = tempGameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // Âm thanh 2D cho UI

        // Giới hạn thời gian offset an toàn trong độ dài của clip
        float clampedOffset = Mathf.Clamp(startTimeOffset, 0f, Mathf.Max(0f, clip.length - 0.05f));
        if (clampedOffset > 0f)
        {
            audioSource.time = clampedOffset;
        }

        audioSource.Play();

        // Đảm bảo thiết lập vị trí phát chính xác sau khi Play
        if (clampedOffset > 0f)
        {
            audioSource.time = clampedOffset;
        }

        // Tự hủy GameObject tạm thời sau khi clip phát xong phần thời lượng còn lại
        float remainingDuration = Mathf.Max(0.1f, clip.length - clampedOffset);
        Destroy(tempGameObject, remainingDuration + 0.1f);
    }
}
