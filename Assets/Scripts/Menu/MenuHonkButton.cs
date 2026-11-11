using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gắn vào HonkButton / HonlButton trong Menu_Scene để duyệt lần lượt tiếng kêu, phát nghe thử và tự lưu lựa chọn cuối.
public sealed class MenuHonkButton : MonoBehaviour
{
    private const string MenuSceneName = "Menu_Scene";

    [SerializeField] private HonkCatalog catalog;
    [SerializeField] private Button button;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource previewSource;
    private int currentIndex;
    private bool hasSelection;
    private bool bound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MenuSceneName) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button candidate in root.GetComponentsInChildren<Button>(true))
            {
                if (!IsHonkButtonName(candidate.gameObject.name)) continue;
                if (candidate.GetComponent<MenuHonkButton>() == null)
                    candidate.gameObject.AddComponent<MenuHonkButton>();
                return;
            }
        }
    }

    private static bool IsHonkButtonName(string objectName) =>
        string.Equals(objectName, "Honk_Button", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(objectName, "HonkButton", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(objectName, "HonlButton", StringComparison.OrdinalIgnoreCase);

    private void Awake()
    {
        if (catalog == null) catalog = HonkCatalog.LoadDefault();
        if (button == null) button = GetComponent<Button>();

        previewSource = gameObject.AddComponent<AudioSource>();
        previewSource.playOnAwake = false;
        previewSource.spatialBlend = 0f;

        if (catalog != null && catalog.Count > 0)
        {
            hasSelection = HonkPreferences.HasSavedSelection;
            currentIndex = HonkPreferences.LoadIndex(catalog.Count);
        }

        if (button != null && !bound)
        {
            var oldSound = button.GetComponent<UIButtonSound>();
            if (oldSound != null) oldSound.enabled = false;
            if (!HasPersistentCycleListener())
            {
                button.onClick.AddListener(CycleAndPlay);
                bound = true;
            }
        }
    }

    private bool HasPersistentCycleListener()
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == nameof(CycleAndPlay))
                return true;
        }
        return false;
    }

    public void CycleAndPlay()
    {
        if (catalog == null) catalog = HonkCatalog.LoadDefault();
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("Chưa có HonkCatalog hoặc danh sách âm thanh Honk đang trống.", this);
            return;
        }

        if (!hasSelection)
        {
            hasSelection = true;
            currentIndex = 0;
        }
        else
        {
            currentIndex = (currentIndex + 1) % catalog.Count;
        }

        // Tự động lưu phần tử vừa bấm làm lựa chọn cuối cùng vì menu không có nút Equip riêng.
        HonkPreferences.SaveIndex(currentIndex);

        AudioClip clip = catalog.GetClip(currentIndex);
        if (clip == null || previewSource == null) return;
        float sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        previewSource.Stop();
        previewSource.clip = clip;
        previewSource.volume = volume * sfxVolume;
        previewSource.Play();
    }

    private void OnDestroy()
    {
        if (button != null && bound)
        {
            button.onClick.RemoveListener(CycleAndPlay);
            bound = false;
        }
    }
}

