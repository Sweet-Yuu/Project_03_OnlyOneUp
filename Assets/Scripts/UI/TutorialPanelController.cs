using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị và chuyển trang cho Panel Hướng Dẫn chơi (Tutorial Panel).
/// Sử dụng CanvasGroup để ẩn/hiện mượt mà mà không làm vô hiệu hoá GameObject chứa Controller,
/// đảm bảo phím tắt (H, F1) và bộ đếm thời gian bắt đầu luôn luôn hoạt động.
/// </summary>
public sealed class TutorialPanelController : MonoBehaviour
{
    [Header("=== Giao diện chính ===")]
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
        if (_panelRoot == null)
        {
            _panelRoot = gameObject;
        }

        // Tự động tìm hoặc gắn CanvasGroup để điều khiển hiển thị
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null && _panelRoot != null && _panelRoot != gameObject)
            {
                _canvasGroup = _panelRoot.GetComponent<CanvasGroup>();
            }
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
        // Khởi tạo ở trạng thái ẩn bảng hướng dẫn thông qua CanvasGroup
        SetPanelVisibility(false);
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
        SetPanelVisibility(true);

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
        SetPanelVisibility(false);

        // Khôi phục con trỏ chuột nếu cần thiết cho góc nhìn camera gameplay
        if (_lockCursorOnClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Closed?.Invoke();
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
            // Nếu ở trang cuối mà bấm Tiếp thì đóng luôn panel
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
    /// Chuyển tới một trang hướng dẫn cụ thể theo chỉ mục (0-based).
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

        UpdateNavigationButtons();
        UpdatePageIndicator();

        PageChanged?.Invoke(_currentPageIndex);
    }

    private void SetPanelVisibility(bool visible)
    {
        _isOpen = visible;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }

        // Nếu _panelRoot là một GameObject con riêng biệt thì mới gọi SetActive
        // để tránh vô hiệu hoá chính GameObject chứa Controller và Trigger
        if (_panelRoot != null && _panelRoot != gameObject)
        {
            _panelRoot.SetActive(visible);
        }
    }

    private void UpdateNavigationButtons()
    {
        if (_pages == null || _pages.Length == 0)
        {
            return;
        }

        if (_prevButton != null)
        {
            _prevButton.interactable = _currentPageIndex > 0;
        }

        if (_nextButton != null)
        {
            // Trang cuối có thể đổi text thành "BẮT ĐẦU CHƠI" hoặc giữ nút đóng
            TMP_Text nextText = _nextButton.GetComponentInChildren<TMP_Text>();
            if (nextText != null)
            {
                nextText.text = (_currentPageIndex >= _pages.Length - 1) ? "Bắt đầu chơi ➔" : "Trang tiếp ➔";
            }
        }
    }

    private void UpdatePageIndicator()
    {
        if (_pageIndicatorText != null)
        {
            _pageIndicatorText.text = $"Trang {_currentPageIndex + 1} / {TotalPages}";
        }
    }
}
