using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
public sealed class NetworkSessionRuntime : MonoBehaviour
{
    public RelaySession Session { get; private set; }
    public RelayRoomRoster Roster { get; private set; }

    private NetworkManager _manager;

    private void Awake() => Initialize();

    public void Initialize()
    {
        // Cho phép menu gọi trước Awake của component này.
        if (Session != null) return;

        _manager = GetComponent<NetworkManager>();
        _manager.OnClientStopped += OnClientStopped;
        _manager.OnClientDisconnectCallback += OnClientDisconnected;

        Roster = new RelayRoomRoster(_manager);
        Session = new RelaySession(
            _manager,
            GetComponent<UnityTransport>(),
            new RelayRoomService(),
            destroyCancellationToken
        );
    }

    private void Update() => Session?.Tick();

    private void OnClientStopped(bool wasHost)
    {
        // Host đóng phòng thì client không thể tiếp tục trận; thoát qua cùng hiệu ứng UI.
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeSceneName == "MainMap_Online" || activeSceneName == "Kitchen_Map")
            ReturnToMenuTransition.Begin();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Client rời trận không làm host dừng mạng, nên host cần tự kết thúc
        // lượt chơi hai người qua hiệu ứng chuyển cảnh trước khi Shutdown.
        if (_manager == null || !_manager.IsHost || _manager.ShutdownInProgress ||
            clientId == _manager.LocalClientId)
            return;

        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeSceneName == "MainMap_Online" || activeSceneName == "Kitchen_Map")
            ReturnToMenuTransition.Begin();
    }

    private void OnDestroy()
    {
        Session?.Dispose();

        if (_manager != null)
        {
            _manager.OnClientStopped -= OnClientStopped;
            _manager.OnClientDisconnectCallback -= OnClientDisconnected;
            Roster?.Dispose();
        }
    }
}
