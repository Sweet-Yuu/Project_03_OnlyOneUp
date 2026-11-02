using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị và chuyển trang cho Panel Hướng Dẫn chơi (Tutorial Panel).
/// </summary>
public sealed class TutorialPanelController : MonoBehaviour
{
    [Header("=== Giao diện chính ===")]
    [Tooltip("GameObject con chứa toàn bộ UI hiển thị (overlay, window). Nếu để trống, script sẽ tự tìm Content hoặc dùng CanvasGroup để tránh tắt nhầm root GameObject.")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private TMP_Text _pageIndicatorText;

    [Header("=== Danh sách các trang hướng dẫn ===")]
    [SerializeField] private GameObject[] _pages = Array.Empty<GameObject>();

    [Header("=== Cấu hình phím tắt ===")]
    [Tooltip("Phím tắt để mở/tắt lại bảng hướng dẫn bất kỳ lúc nào.")]
    [SerializeField] private Key _toggleKey = Key.H;
    [SerializeField] private Key _alternateToggleKey = Key.F1;

    [Header("=== Cấu hình con trỏ chuột ===")]
    [SerializeField] private bool _lockCursorOnClose = true;

    private int _currentPageIndex;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public int CurrentPageIndex => _currentPageIndex;
    public int TotalPages => _pages != null ? _pages.Length : 0;

    public event Action<int> PageChanged;
    public event Action Closed;
    public event Action Opened;

    private void Awake()
    {
        // Nếu _panelRoot trỏ vào chính gameObject này thì ưu tiên dùng CanvasGroup để không làm dừng Update/Coroutine của root
        if (_panelRoot == gameObject)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }
        }
        else if (_panelRoot == null)
        {
            Transform content = transform.Find("PanelContent") ?? transform.Find("Content");
            if (content != null)
            {
                _panelRoot = content.gameObject;
            }
            else
            {
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(NextPage);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.AddListener(PreviousPage);
        }
    }

    private void Start()
    {
        // Đảm bảo ban đầu ẩn giao diện nhưng root GameObject vẫn hoạt động
        SetDisplayVisible(false);
        _isOpen = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Kiểm tra phím bật/tắt (H hoặc F1)
        if (keyboard[_toggleKey].wasPressedThisFrame || keyboard[_alternateToggleKey].wasPressedThisFrame)
        {
            Toggle();
            return;
        }

        if (!_isOpen)
        {
            return;
        }

        // Khi panel đang mở, hỗ trợ phím điều hướng nhanh
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            NextPage();
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            PreviousPage();
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái mở của Panel hướng dẫn.
    /// </summary>
    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Hiển thị panel hướng dẫn từ trang đầu tiên và mở con trỏ chuột để thao tác.
    /// </summary>
    public void Show()
    {
        _isOpen = true;
        SetDisplayVisible(true);

        // Mở khóa chuột để người chơi bấm nút chuyển trang hoặc đóng
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPage(0);
        Opened?.Invoke();
    }

    /// <summary>
    /// Đóng panel hướng dẫn và đưa chuột trở lại chế độ chơi bình thường.
    /// </summary>
    public void Close()
    {
        _isOpen = false;
        SetDisplayVisible(false);

        // Khôi phục con trỏ chuột nếu cần thiết cho góc nhìn camera gameplay
        if (_lockCursorOnClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Closed?.Invoke();
    }

    private void SetDisplayVisible(bool visible)
    {
        if (_panelRoot != null && _panelRoot != gameObject)
        {
            _panelRoot.SetActive(visible);
        }
        else if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            // Fallback nếu không có con riêng hay CanvasGroup
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// Chuyển sang trang tiếp theo.
    /// </summary>
    public void NextPage()
    {
        if (_pages == null || _pages.Length == 0)
        {
            return;
        }

        if (_currentPageIndex < _pages.Length - 1)
        {
            SetPage(_currentPageIndex + 1);
        }
        else
        {
            // Nếu đang ở trang cuối và ấn tiếp thì đóng panel để bắt đầu chơi
            Close();
        }
    }

    /// <summary>
    /// Quay lại trang trước đó.
    /// </summary>
    public void PreviousPage()
    {
        if (_pages == null || _pages.Length == 0)
        {
            return;
        }

        if (_currentPageIndex > 0)
        {
            SetPage(_currentPageIndex - 1);
        }
    }

    /// <summary>
    /// Thiết lập trang hiển thị cụ thể theo index (0-based).
    /// </summary>
    public void SetPage(int pageIndex)
    {
        if (_pages == null || _pages.Length == 0)
        {
            return;
        }

        _currentPageIndex = Mathf.Clamp(pageIndex, 0, _pages.Length - 1);

        for (int i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null)
            {
                _pages[i].SetActive(i == _currentPageIndex);
            }
        }

        UpdateNavigationUI();
        PageChanged?.Invoke(_currentPageIndex);
    }

    private void UpdateNavigationUI()
    {
        int total = _pages != null ? _pages.Length : 0;
        int currentDisplay = total > 0 ? _currentPageIndex + 1 : 0;

        if (_pageIndicatorText != null)
        {
            _pageIndicatorText.text = $"Trang {currentDisplay} / {total}";
        }

        if (_prevButton != null)
        {
            _prevButton.interactable = _currentPageIndex > 0;
        }

        if (_nextButton != null)
        {
            // Ở trang cuối, có thể đổi text hoặc vẫn để nút bấm để người chơi bấm xong đóng panel
            _nextButton.interactable = true;
            TMP_Text nextText = _nextButton.GetComponentInChildren<TMP_Text>();
            if (nextText != null)
            {
                nextText.text = _currentPageIndex == total - 1 ? "Bắt đầu chơi >" : "Trang sau >";
            }
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(NextPage);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.RemoveListener(PreviousPage);
        }
    }
}
