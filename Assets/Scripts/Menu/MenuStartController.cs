using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Chỉ điều hướng giữa menu chính và bảng chọn phòng.</summary>
public sealed class MenuStartController : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuRoot;
    [SerializeField] private GameObject _roomSelectionRoot;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _firstRoomButton;
    [SerializeField] private Image logoImage;
    [SerializeField] private Button shopButton;
    [SerializeField] private EventSystem _eventSystem;

    public bool IsRoomSelectionOpen => _roomSelectionRoot != null && _roomSelectionRoot.activeSelf;

    private void Start()
    {
        // [Kitchen_Map]: Giới hạn FPS ở mức 60 và kích hoạt vSync để tối ưu hóa hiệu năng, chống rách hình (tearing)
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        ShowMainMenu();
    }

    private void Update()
    {
        if (!IsRoomSelectionOpen) return;

        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            ShowMainMenu();
    }

    public void OpenRoomSelection()
    {
        if (_mainMenuRoot == null || _roomSelectionRoot == null) return;
        _mainMenuRoot.SetActive(false);
        _roomSelectionRoot.SetActive(true);
        logoImage.gameObject.SetActive(false);
        shopButton.gameObject.SetActive(false);
        Select(_firstRoomButton);
    }

    public void ShowMainMenu()
    {
        if (_mainMenuRoot == null || _roomSelectionRoot == null) return;
        _roomSelectionRoot.SetActive(false);
        _mainMenuRoot.SetActive(true);
        logoImage.gameObject.SetActive(true);
        Select(_startButton);
    }

    private void Select(Selectable target)
    {
        if (_eventSystem == null || target == null) return;
        // Xóa lựa chọn cũ để bàn phím/gamepad không trỏ tới nút vừa bị ẩn.
        _eventSystem.SetSelectedGameObject(null);
        _eventSystem.SetSelectedGameObject(target.gameObject);
    }
}
