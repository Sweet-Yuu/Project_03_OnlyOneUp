using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Lắp ghép Relay, dữ liệu phòng và UI; không tải màn chơi.</summary>
public sealed class MenuRelayController : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private UnityTransport _transport;
    [SerializeField] private GameObject _roomSelectionRoot;
    [SerializeField] private GameObject _joinRoot;
    [SerializeField] private GameObject _lobbyRoot;
    [SerializeField] private RoomLobbyView _view;
    [SerializeField] private UnityEvent _backRequested = new UnityEvent();
    [Header("Host loading")]
    [SerializeField] private GameObject _loadingRoot;
    [SerializeField] private TMP_Text _loadingText;
    [SerializeField] private GameObject _loadingBackButton;
    [SerializeField] private NetworkSessionRuntime _runtime;
    [SerializeField] private NetworkGameStarter _gameStarter;

    [SerializeField, TextArea]
    private string _hostLoadingMessage = "Honk­ing in progress...";
    private RelaySession _session;
    private RelayRoomRoster _roster;
    private IPlayerNameStore _names;
    private bool _connecting;

    private void Awake()
    {
        _lobbyRoot.SetActive(false);
        Application.runInBackground = true;
        _names = new PlayerNamePreferences(); 
        _runtime.Initialize();
        _roster = _runtime.Roster;
        _session = _runtime.Session;
        _view.Bind(_roster);
        _loadingRoot.SetActive(false);
        _view.SetStartGameState(false, false);
    }

    public void Host() => _ = Connect(true, "");
    public void Join(string code) => _ = Connect(false, code);
    private bool CanStartGame =>
    _networkManager.IsHost &&
    _networkManager.IsListening &&
    !_networkManager.ShutdownInProgress &&
    !_connecting &&
    !_session.Busy &&
    _lobbyRoot.activeInHierarchy &&
    _networkManager.ConnectedClientsIds.Count == 2 &&
    !_gameStarter.IsStarting &&
    _roster.Members.Count == 2;

    private async Task Connect(bool host, string code)
    {
        if (_connecting ||
            _networkManager.IsListening ||
            _networkManager.ShutdownInProgress)
            return;

        _connecting = true;
        _roster.Prepare(_names.Load());

        _roomSelectionRoot.SetActive(false);
        _joinRoot.SetActive(false);

        // Host chờ bên ngoài bảng; Join giữ luồng hiện tại.
        _lobbyRoot.SetActive(!host);
        _loadingRoot.SetActive(host);
        _loadingBackButton.SetActive(false);

        if (host)
            _loadingText.text = _hostLoadingMessage;

        try
        {
            await _session.ConnectAsync(host, code);
            if (this == null) return;

            if (!host) return;

            // ConnectAsync tự xử lý lỗi, nên phải kiểm tra kết quả thật.
            bool roomReady =
                _networkManager.IsHost &&
                !_networkManager.ShutdownInProgress &&
                !string.IsNullOrEmpty(_session.RoomCode) &&
                _roster.Members.Count > 0;

            if (roomReady)
            {
                // Cập nhật thông tin trước, rồi mới hiện bảng.
                _roster.SetCode(_session.RoomCode);

                _loadingRoot.SetActive(false);
                _lobbyRoot.SetActive(true);
            }
            else
            {
                _loadingText.text =
                    "Không tạo được phòng.\n" + _session.Status;
            }
        }
        catch (System.Exception exception)
        {
            if (this == null) return;

            Debug.LogException(exception);

            if (host)
                _loadingText.text = "Không tạo được phòng. Hãy thử lại.";
        }
        finally
        {
            _connecting = false;

            if (this != null && host && _loadingRoot.activeSelf)
                _loadingBackButton.SetActive(true);
        }
    }

    private void Update()
    {
        _view.SetStartGameState(_networkManager.IsHost, CanStartGame);
        if (!_lobbyRoot.activeSelf) return;
        bool canLeave = !_connecting && !_session.Busy && !_networkManager.ShutdownInProgress;
        string status = _session.Status;
        _view.SetStatus(status, canLeave);
    }

    private void LateUpdate()
    {
        if (_lobbyRoot.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
    }

    public void Back()
    {
        if (_gameStarter.IsStarting) return;
        if (_connecting || _session.Busy || _networkManager.ShutdownInProgress) return;
        _session.Disconnect();
        _roster.Clear();
        _lobbyRoot.SetActive(false);
        _loadingRoot.SetActive(false);
        _backRequested.Invoke();
    }

    public void StartGame()
    {
        if (!CanStartGame) return;

        _gameStarter.StartGame();
    }
}
