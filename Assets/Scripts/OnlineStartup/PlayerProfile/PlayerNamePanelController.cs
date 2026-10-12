using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Chặn menu cho đến khi người chơi đã có tên.</summary>
public sealed class PlayerNamePanelController : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private GameObject _mainMenuRoot;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private UnityEvent _completed = new UnityEvent();
    private IPlayerNameStore _store;

    public void Initialize(IPlayerNameStore store) => _store = store;

    private void Awake()
    {
        if (_store == null) _store = new PlayerNamePreferences();
        _nameInput.onValueChanged.AddListener(OnNameChanged);
        _nameInput.onSubmit.AddListener(OnSubmit);
        _confirmButton.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(_store.Load())) { Complete(); return; }
        _mainMenuRoot.SetActive(false);
        _panelRoot.SetActive(true);
        OnNameChanged(_nameInput.text);
        _nameInput.Select();
        _nameInput.ActivateInputField();
    }

    private void OnDestroy()
    {
        _nameInput.onValueChanged.RemoveListener(OnNameChanged);
        _nameInput.onSubmit.RemoveListener(OnSubmit);
        _confirmButton.onClick.RemoveListener(Confirm);
    }

    private void OnNameChanged(string value) => _confirmButton.interactable = !string.IsNullOrWhiteSpace(value);
    private void OnSubmit(string value) => Confirm();

    public void Confirm()
    {
        if (!_panelRoot.activeInHierarchy || string.IsNullOrWhiteSpace(_nameInput.text)) return;
        _store.Save(_nameInput.text.Trim());
        Complete();
    }

    private void Complete()
    {
        _nameInput.DeactivateInputField();
        _panelRoot.SetActive(false);
        _completed.Invoke();
    }
}
