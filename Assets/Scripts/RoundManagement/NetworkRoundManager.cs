using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// Quản lý checkpoint và điều kiện hồi sinh; không điều khiển vật lý tổ/trứng.
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(400)]
public sealed class NetworkRoundManager : NetworkBehaviour, IRoundSession, IOnlineRoundActions
{
    private bool _actionPending;
    private readonly HashSet<ulong> _coveredClients = new HashSet<ulong>();
    private OnlineRoundTransition _transition;
    public void RequestPlayAgain() => RequestActionRpc(false);
    public void RequestMainMenu() => RequestActionRpc(true);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestActionRpc(bool menu, RpcParams rpcParams = default)
    {
        if (_actionPending || _resetTarget == null ||
            !NetworkManager.ConnectedClients.ContainsKey(rpcParams.Receive.SenderClientId)) return;
        _actionPending = true;
        StartCoroutine(PerformAction(menu));
    }

    private System.Collections.IEnumerator PerformAction(bool menu)
    {
        _respawning.Value = true;
        _coveredClients.Clear();
        CoverRpc();
        float deadline = Time.realtimeSinceStartup + 10f;
        // Chờ cả hai màn hình được che trước khi teleport hoặc đóng phiên mạng.
        while (_coveredClients.Count < NetworkManager.ConnectedClientsIds.Count &&
            Time.realtimeSinceStartup < deadline) yield return null;
        if (_coveredClients.Count < NetworkManager.ConnectedClientsIds.Count)
        {
            FinishActionRpc(false);
            _respawning.Value = false;
            _actionPending = false;
            yield break;
        }
        if (menu)
        {
            FinishActionRpc(true);
            yield break;
        }
        if (_resetTarget.TryRestartRound(_startCenter))
        {
            _activeCheckpoint.Value = 0;
            _respawnCenter.Value = _startCenter;
            // Cho phép state machine chuẩn bị chạy; chính trạng thái Preparing khóa player.
            _respawning.Value = false;
            deadline = Time.realtimeSinceStartup + 20f;
            while (!_resetTarget.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_resetTarget.IsPlaying) Debug.LogWarning("Chuẩn bị lượt mới chưa hoàn tất. Kiểm tra kết nối và điểm spawn.", this);
        }
        else Debug.LogWarning("Không thể chơi lại: cần đủ hai người và tổ đã sẵn sàng.", this);
        _respawning.Value = false;
        FinishActionRpc(false);
        _actionPending = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CoverRpc() => StartCoroutine(CoverAndConfirm());
    private System.Collections.IEnumerator CoverAndConfirm()
    {
        _transition = OnlineRoundTransition.Create();
        yield return _transition.Cover();
        if (IsSpawned) ConfirmCoveredRpc();
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ConfirmCoveredRpc(RpcParams rpcParams = default)
    {
        if (_actionPending && NetworkManager.ConnectedClients.ContainsKey(rpcParams.Receive.SenderClientId))
            _coveredClients.Add(rpcParams.Receive.SenderClientId);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void FinishActionRpc(bool menu)
    {
        if (menu) StartCoroutine(LeaveAfterDelivery());
        else if (_transition != null) _transition.Reveal();
    }
    private System.Collections.IEnumerator LeaveAfterDelivery()
    {
        // Host chờ RPC được gửi ra trước khi Shutdown đóng transport.
        if (IsServer) yield return new WaitForSecondsRealtime(.5f);
        if (_transition != null) _transition.ReturnToMenu();
        else ReturnToMenuTransition.Begin();
    }
    [SerializeField] private Vector3 _startCenter = new Vector3(45f, 2.1f, 30f);
    [SerializeField, Min(0f)] private float _eggBreakRespawnDelay = 1f;
    private readonly NetworkVariable<Vector3> _respawnCenter = new NetworkVariable<Vector3>();
    private readonly NetworkVariable<int> _activeCheckpoint = new NetworkVariable<int>();
    private readonly NetworkVariable<bool> _respawning = new NetworkVariable<bool>();
    private readonly Dictionary<int, OnlineCheckpoint> _checkpoints = new Dictionary<int, OnlineCheckpoint>();
    private readonly List<Collider> _deathZones = new List<Collider>();
    private IRoundResetTarget _resetTarget;
    private double _respawnAt;
    public bool IsRespawning => IsSpawned && _respawning.Value;
    public int ActiveCheckpointId => _activeCheckpoint.Value;
    public Vector3 RespawnCenter => IsSpawned ? _respawnCenter.Value : _startCenter;

    private void Awake()
    {
        // Cùng NetworkObject chỉ để lắp ghép; giao tiếp qua abstraction.
        _resetTarget = GetComponent<IRoundResetTarget>();
    }

    public override void OnNetworkSpawn()
    {
        if (_resetTarget == null)
        {
            Debug.LogError("NetworkRoundManager requires an IRoundResetTarget on the same NetworkObject.", this);
            return;
        }
        if (!IsServer) return;
        _respawnCenter.Value = _startCenter;
        _activeCheckpoint.Value = 0;
        _respawning.Value = false;
        CacheRespawnZones();
        _resetTarget.RoundFailed += OnRoundFailed;
    }

    private void OnRoundFailed() => ScheduleRespawn(_eggBreakRespawnDelay);

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || _actionPending || !_respawning.Value || _resetTarget == null ||
            NetworkManager.ServerTime.Time < _respawnAt) return;
        // Giữ khóa khi thiếu người; chỉ mở sau khi lệnh chuẩn bị lượt mới được nhận.
        if (_resetTarget.TryRestartRound(RespawnCenter)) _respawning.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
        if (_transition != null) _transition.ReturnToMenu();
        _actionPending = false;
        _coveredClients.Clear();
        if (_resetTarget != null) _resetTarget.RoundFailed -= OnRoundFailed;
        _checkpoints.Clear();
        _deathZones.Clear();
    }

    private bool TryGetPlayers(out NetworkObject host, out NetworkObject guest)
    {
        host = guest = null;
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.ClientId == Unity.Netcode.NetworkManager.ServerClientId) host = client.PlayerObject;
            else guest = client.PlayerObject;
        }
        return host != null && guest != null;
    }

    private void CacheRespawnZones()
    {
        _checkpoints.Clear();
        _deathZones.Clear();
        foreach (OnlineCheckpoint checkpoint in FindObjectsByType<OnlineCheckpoint>(FindObjectsSortMode.None))
        {
            if (checkpoint.gameObject.scene != gameObject.scene) continue;
            if (checkpoint.CheckpointId <= 0 || _checkpoints.ContainsKey(checkpoint.CheckpointId))
            {
                Debug.LogError("Online checkpoints require unique positive IDs.", checkpoint);
                continue;
            }
            _checkpoints.Add(checkpoint.CheckpointId, checkpoint);
        }
        foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (collider.gameObject.scene == gameObject.scene && PlayerSpawnProbe.IsDeadZone(collider.transform))
                _deathZones.Add(collider);
    }

    public void ReportPlayerContact(Collider contact, uint sequence)
    {
        if (!IsSpawned || _resetTarget == null || !_resetTarget.CanPlayerMove(sequence) || contact == null) return;
        if (PlayerSpawnProbe.IsDeadZone(contact.transform)) ReportDeathRpc(sequence);
        else
        {
            var checkpoint = contact.GetComponentInParent<OnlineCheckpoint>();
            if (checkpoint != null && checkpoint.CheckpointId != ActiveCheckpointId)
                ReportCheckpointRpc(checkpoint.CheckpointId, sequence);
        }
    }

    private bool TryGetContactPlayer(ulong sender, uint sequence, out NetworkObject player,
        out NetworkObject host, out NetworkObject guest)
    {
        player = null;
        host = guest = null;
        if (!IsSpawned || _resetTarget == null || !_resetTarget.CanPlayerMove(sequence) || !TryGetPlayers(out host, out guest)) return false;
        if (sender == host.OwnerClientId) player = host;
        else if (sender == guest.OwnerClientId) player = guest;
        return player != null;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportCheckpointRpc(int id, uint sequence, RpcParams rpcParams = default)
    {
        if (!TryGetContactPlayer(rpcParams.Receive.SenderClientId, sequence, out NetworkObject player,
            out NetworkObject host, out NetworkObject guest) || id == ActiveCheckpointId ||
            !_checkpoints.TryGetValue(id, out OnlineCheckpoint checkpoint) || checkpoint == null ||
            !checkpoint.IsNear(player.transform.position, 6f)) return;
        // Owner mô phỏng CharacterController; host chỉ nhận ID và kiểm tra vùng đã đăng ký.
        // Khoảng dung sai bù độ trễ vị trí, không nhận tọa độ hồi sinh tùy ý từ client.
        Vector3 center = checkpoint.SpawnCenter;
        var h = host.GetComponent<NetworkTestMovement>();
        var g = guest.GetComponent<NetworkTestMovement>();
        float halfDistance = h.GetCarryRootDistance(g, _resetTarget.GripDistance) * .5f;
        if (!h.TryGetStartPosition(center + Vector3.left * halfDistance, out Vector3 hp) ||
            !g.TryGetStartPosition(center + Vector3.right * halfDistance, out Vector3 gp) ||
            Mathf.Abs(hp.y - gp.y) > .03f || IsInsideDeathZone(hp) || IsInsideDeathZone(gp) ||
            !h.StartPositionIsClear(hp, out _) || !g.StartPositionIsClear(gp, out _))
        {
            Debug.LogWarning("Checkpoint needs clear, level ground for both players. Assign its Spawn Center to a safe position.", checkpoint);
            return;
        }
        _respawnCenter.Value = center;
        _activeCheckpoint.Value = id;
    }

    private bool IsInsideDeathZone(Vector3 position)
    {
        foreach (Collider zone in _deathZones)
            if (zone != null && zone.enabled && zone.gameObject.activeInHierarchy &&
                (zone.ClosestPoint(position) - position).sqrMagnitude < .01f) return true;
        return false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportDeathRpc(uint sequence, RpcParams rpcParams = default)
    {
        if (!TryGetContactPlayer(rpcParams.Receive.SenderClientId, sequence, out NetworkObject player,
            out _, out _)) return;
        foreach (Collider zone in _deathZones)
        {
            if (zone == null || !zone.enabled || !zone.gameObject.activeInHierarchy) continue;
            if ((zone.ClosestPoint(player.transform.position) - player.transform.position).sqrMagnitude > 36f) continue;
            ScheduleRespawn(0f);
            return;
        }
    }

    private void ScheduleRespawn(float delay)
    {
        if (!IsSpawned || !IsServer || _resetTarget == null || _respawning.Value ||
            !_resetTarget.IsPlaying) return;
        _respawning.Value = true;
        _respawnAt = NetworkManager.ServerTime.Time + Mathf.Max(0f, delay);
    }

}
