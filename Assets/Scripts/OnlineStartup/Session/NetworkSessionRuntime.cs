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

        Roster = new RelayRoomRoster(_manager);
        Session = new RelaySession(
            _manager,
            GetComponent<UnityTransport>(),
            new RelayRoomService(),
            destroyCancellationToken
        );
    }

    private void Update() => Session?.Tick();

    private void OnDestroy()
    {
        Session?.Dispose();

        if (_manager != null)
            Roster?.Dispose();
    }
}