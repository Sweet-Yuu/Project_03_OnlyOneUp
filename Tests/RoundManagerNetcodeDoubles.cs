// Chỉ trong project kiểm thử: Unity collider thật, transport/RPC và player là doubles.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    public class NetworkBehaviour : MonoBehaviour
    {
        public bool IsSpawned, IsServer;
        public NetworkManager NetworkManager;
        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }
    }
    public class NetworkObject : MonoBehaviour { public ulong OwnerClientId; }
    public class NetworkVariable<T> { public T Value; }
    public enum SendTo { Server }
    public enum RpcInvokePermission { Everyone }
    public sealed class RpcAttribute : Attribute
    {
        public RpcAttribute(SendTo target) { }
        public RpcInvokePermission InvokePermission;
    }
    public struct RpcParams { public ReceiveParams Receive; }
    public struct ReceiveParams { public ulong SenderClientId; }
    public class NetworkManager
    {
        public const ulong ServerClientId = 0;
        public Clock ServerTime = new Clock();
        public List<Client> ConnectedClientsList = new List<Client>();
    }
    public class Clock { public double Time; }
    public class Client { public ulong ClientId; public NetworkObject PlayerObject; }
}

public class NetworkTestMovement : MonoBehaviour
{
    public bool Ground = true, Clear = true;
    public float GetCarryRootDistance(NetworkTestMovement other, float grip) => grip;
    public bool TryGetStartPosition(Vector3 approximate, out Vector3 result)
    { result = approximate; return Ground; }
    public bool StartPositionIsClear(Vector3 position, out string reason)
    { reason = null; return Clear; }
}
public class NetworkNest : MonoBehaviour { }
