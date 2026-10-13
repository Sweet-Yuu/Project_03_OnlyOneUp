using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;

/// <summary>Host xác thực tên và phát danh sách phòng; không phụ thuộc giao diện.</summary>
public sealed class RelayRoomRoster : IRoomRoster, IDisposable
{
    private const string SnapshotMessage = "Menu.RoomRoster.v1";
    private const string RequestMessage = "Menu.RequestRoster.v1";
    private readonly NetworkManager _manager;
    private readonly Dictionary<ulong, string> _approvedNames = new Dictionary<ulong, string>();
    private readonly List<RoomMember> _members = new List<RoomMember>();
    public event Action Changed;
    public IReadOnlyList<RoomMember> Members => _members;
    public bool AcceptingPlayers { get; set; } = true;
    public string Code { get; private set; } = "";

    public RelayRoomRoster(NetworkManager manager)
    {
        _manager = manager;
        _manager.ConnectionApprovalCallback = Approve;
        _manager.OnServerStarted += RegisterMessages;
        _manager.OnClientStarted += RegisterMessages;
        _manager.OnClientConnectedCallback += Connected;
        _manager.OnClientDisconnectCallback += Disconnected;
        _manager.OnClientStopped += Stopped;
    }

    public void Prepare(string name)
    {
        Clear();
        _manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(CleanName(name));
        AcceptingPlayers = true;
    }

    public void SetCode(string code)
    {
        if (!_manager.IsServer) return;
        Code = code;
        Publish();
    }

    public void Clear()
    {
        _approvedNames.Clear();
        _members.Clear();
        Code = "";
        Changed?.Invoke();
    }

    private static string CleanName(string value)
    {
        var result = new StringBuilder();
        foreach (char c in (value ?? "").Trim())
        {
            if (char.IsControl(c)) continue;
            if (result.Length >= 20) break;
            result.Append(c);
        }
        if (result.Length > 0 && char.IsHighSurrogate(result[result.Length - 1])) result.Length--;
        return result.ToString();
    }

    private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;
        response.Pending = false;
        string name = request.Payload != null && request.Payload.Length <= 128
            ? CleanName(Encoding.UTF8.GetString(request.Payload)) : "";
        bool hasSpace =
            _approvedNames.ContainsKey(request.ClientNetworkId) ||
            _approvedNames.Count < 2;

        response.Approved =
            AcceptingPlayers &&
            name.Length > 0 &&
            hasSpace;

        response.Reason = !AcceptingPlayers
            ? "The game has already started."
            : name.Length == 0
                ? "Player name is required."
                : "Room is full (2/2).";
        if (response.Approved) _approvedNames[request.ClientNetworkId] = name;
    }

    private void RegisterMessages()
    {
        // Host gọi cả hai sự kiện khởi động; unregister trước để không đăng ký trùng.
        var messaging = _manager.CustomMessagingManager;
        messaging.UnregisterNamedMessageHandler(SnapshotMessage);
        messaging.UnregisterNamedMessageHandler(RequestMessage);
        messaging.RegisterNamedMessageHandler(SnapshotMessage, Receive);
        messaging.RegisterNamedMessageHandler(RequestMessage, Request);
    }

    private void Connected(ulong id)
    {
        if (_manager.IsServer) Publish();
        else if (id == _manager.LocalClientId)
        {
            using var writer = new FastBufferWriter(1, Allocator.Temp);
            _manager.CustomMessagingManager.SendNamedMessage(RequestMessage, NetworkManager.ServerClientId, writer);
        }
    }

    private void Disconnected(ulong id)
    {
        if (_manager.IsServer)
        {
            _approvedNames.Remove(id);
            Publish(id);
        }
        else if (id == _manager.LocalClientId) Clear();
    }

    private void Stopped(bool wasHost) => Clear();

    private void Request(ulong sender, FastBufferReader reader)
    {
        if (_manager.IsServer && _manager.ConnectedClients.ContainsKey(sender)) Publish();
    }

    private void Publish(ulong excluded = ulong.MaxValue)
    {
        if (!_manager.IsServer) return;
        _members.Clear();
        foreach (ulong id in _manager.ConnectedClientsIds)
            if (id != excluded && _approvedNames.TryGetValue(id, out string name))
                _members.Add(new RoomMember(id, name, id == NetworkManager.ServerClientId));
        _members.Sort((a, b) => a.ClientId.CompareTo(b.ClientId));
        using var writer = new FastBufferWriter(512, Allocator.Temp);
        writer.WriteValueSafe(new FixedString64Bytes(Code));
        writer.WriteValueSafe((byte)_members.Count);
        foreach (var member in _members)
        {
            writer.WriteValueSafe(member.ClientId);
            writer.WriteValueSafe(new FixedString128Bytes(member.Name));
        }
        foreach (var member in _members)
            if (!member.IsHost)
                _manager.CustomMessagingManager.SendNamedMessage(SnapshotMessage, member.ClientId, writer);
        Changed?.Invoke();
    }

    private void Receive(ulong sender, FastBufferReader reader)
    {
        if (_manager.IsServer || sender != NetworkManager.ServerClientId) return;
        reader.ReadValueSafe(out FixedString64Bytes code);
        reader.ReadValueSafe(out byte count);
        if (count > 2) return;
        var received = new List<RoomMember>();
        for (int i = 0; i < count; i++)
        {
            reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out FixedString128Bytes name);
            received.Add(new RoomMember(id, name.ToString(), id == NetworkManager.ServerClientId));
        }
        Code = code.ToString();
        _members.Clear();
        _members.AddRange(received);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _manager.OnServerStarted -= RegisterMessages;
        _manager.OnClientStarted -= RegisterMessages;
        _manager.OnClientConnectedCallback -= Connected;
        _manager.OnClientDisconnectCallback -= Disconnected;
        _manager.OnClientStopped -= Stopped;
        _manager.ConnectionApprovalCallback = null;
        _manager.CustomMessagingManager?.UnregisterNamedMessageHandler(SnapshotMessage);
        _manager.CustomMessagingManager?.UnregisterNamedMessageHandler(RequestMessage);
    }
}
