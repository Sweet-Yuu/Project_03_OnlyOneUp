using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Quản lý form nhập mã; phát yêu cầu để lớp kết nối xử lý riêng.</summary>
public sealed class JoinCodePanelController : MonoBehaviour
{
    [SerializeField] private GameObject _roomSelectionRoot;
    [SerializeField] private GameObject _joinPanelRoot;
    [SerializeField] private TMP_InputField _codeInput;
    [SerializeField] private Button _joinButton;
    [SerializeField] private UnityEvent _backRequested = new UnityEvent();
    [SerializeField] private UnityEvent<string> _joinRequested = new UnityEvent<string>();

    public string RoomCode => _codeInput.text.Trim().ToUpperInvariant();

    private void OnEnable()
    {
        _codeInput.onValueChanged.AddListener(OnCodeChanged);
        _codeInput.onSubmit.AddListener(OnSubmit);
        RefreshSubmit();
    }

    private void OnDisable()
    {
        _codeInput.onValueChanged.RemoveListener(OnCodeChanged);
        _codeInput.onSubmit.RemoveListener(OnSubmit);
    }

    // Xử lý sau Update của menu trước để Escape chỉ lùi một màn hình.
    private void LateUpdate()
    {
        if (!_joinPanelRoot.activeSelf) return;
        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            Back();
    }

    public void Open()
    {
        _roomSelectionRoot.SetActive(false);
        _joinPanelRoot.SetActive(true);
        RefreshSubmit();
        if (Application.isPlaying)
        {
            _codeInput.Select();
            _codeInput.ActivateInputField();
        }
    }

    public void Back()
    {
        _codeInput.DeactivateInputField();
        _joinPanelRoot.SetActive(false);
        _backRequested.Invoke();
    }

    public void Submit()
    {
        if (!_joinPanelRoot.activeInHierarchy || string.IsNullOrWhiteSpace(RoomCode)) return;
        _joinRequested.Invoke(RoomCode);
    }

    private void OnCodeChanged(string value)
    {
        // Không phát lại onValueChanged khi chuẩn hóa chữ hoa, tránh vòng lặp sự kiện.
        _codeInput.SetTextWithoutNotify(value.ToUpperInvariant());
        RefreshSubmit();
    }

    private void OnSubmit(string value) => Submit();
    private void RefreshSubmit() => _joinButton.interactable = !string.IsNullOrWhiteSpace(RoomCode);
}
