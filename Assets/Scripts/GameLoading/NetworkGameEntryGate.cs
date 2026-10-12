using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkGameEntryGate :
    NetworkBehaviour, IGameEntryGate
{
    [SerializeField] private NetworkNest _nest;
    [SerializeField, Min(10f)] private float _timeout = 120f;

    private readonly NetworkVariable<bool> _reveal = new();
    private readonly NetworkVariable<bool> _open = new();
    private readonly NetworkVariable<bool> _failed = new();

    private readonly HashSet<ulong> _readyClients = new();
    private readonly HashSet<ulong> _finishedClients = new();

    private Coroutine _routine;
    private float _startedAt;

    public bool CanReveal => IsSpawned && _reveal.Value;
    public bool IsOpen => IsSpawned && _open.Value;
    public bool HasFailed => IsSpawned && _failed.Value;

    public override void OnNetworkSpawn()
    {
        _startedAt = Time.unscaledTime;

        if (IsServer)
        {
            _reveal.Value = false;
            _open.Value = false;
            _failed.Value = false;
            _readyClients.Clear();
            _finishedClients.Clear();
        }

        _routine = StartCoroutine(CheckReadiness());
    }

    private IEnumerator CheckReadiness()
    {
        var interval = new WaitForSecondsRealtime(0.1f);
        bool reported = false;
        float stableFor = 0f;

        while (IsSpawned && !_open.Value && !_failed.Value)
        {
            if (IsServer)
            {
                bool timedOut =
                    Time.unscaledTime - _startedAt > _timeout;

                bool disconnected =
                    NetworkManager.ConnectedClientsIds.Count != 2;

                bool setupFailed = _nest == null ||
                    _nest.RoundStage == NestRoundStage.SetupFailed;

                if (timedOut || disconnected || setupFailed)
                {
                    _failed.Value = true;
                    yield break;
                }
            }

            if (IsClient && !reported)
            {
                // Chờ trạng thái local ổn định qua nhiều lần kiểm tra.
                stableFor = LocalObjectsReady()
                    ? stableFor + 0.1f
                    : 0f;

                if (stableFor >= 0.2f)
                {
                    reported = true;
                    ReadyRpc();
                }
            }

            yield return interval;
        }
    }

    private bool LocalObjectsReady()
    {
        if (_nest == null ||
            !_nest.IsPlaying ||
            _nest.EggPhase != NestEggPhase.Intact)
            return false;

        int playerCount = 0;
        bool hasLocalPlayer = false;

        foreach (NetworkObject obj in
                 NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            if (!obj.IsSpawned || !obj.IsPlayerObject)
                continue;

            if (!obj.TryGetComponent(
                    out NetworkTestMovement movement) ||
                !obj.TryGetComponent(
                    out NetworkPlayerIdentity identity))
                return false;

            if (string.IsNullOrWhiteSpace(identity.DisplayName))
                return false;

            Transform grip = _nest.GetGrip(obj.OwnerClientId);

            // Client phải thực sự thấy player ở gần điểm giữ tổ.
            if (grip == null ||
                Vector3.Distance(
                    movement.CarryPoint, grip.position) > 0.2f)
                return false;

            playerCount++;
            hasLocalPlayer |= obj.IsOwner;
        }

        return playerCount == 2 && hasLocalPlayer;
    }

    private bool IsConnectedPlayer(ulong id)
    {
        return NetworkManager.ConnectedClients.TryGetValue(
                   id, out var client) &&
               client.PlayerObject != null;
    }

    private bool BothReported(HashSet<ulong> reports)
    {
        if (NetworkManager.ConnectedClientsIds.Count != 2)
            return false;

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
            if (!reports.Contains(id))
                return false;

        return true;
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Everyone)]
    private void ReadyRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (_failed.Value || _reveal.Value ||
            _nest == null || !_nest.IsPlaying ||
            !IsConnectedPlayer(sender))
            return;

        _readyClients.Add(sender);

        if (BothReported(_readyClients))
            _reveal.Value = true;
    }

    public void FinishLocalReveal()
    {
        if (IsSpawned && IsClient &&
            _reveal.Value && !_failed.Value)
            RevealFinishedRpc();
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Everyone)]
    private void RevealFinishedRpc(
        RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (_failed.Value || !_reveal.Value ||
            !IsConnectedPlayer(sender))
            return;

        _finishedClients.Add(sender);

        if (BothReported(_finishedClients))
            _open.Value = true;
    }

    public override void OnNetworkDespawn()
    {
        if (_routine != null)
            StopCoroutine(_routine);
    }
}