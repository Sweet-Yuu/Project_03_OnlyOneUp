// Standalone harness only, OUTSIDE Assets. These doubles replace native engine,
// NGO and transport boundaries; they do not test real physics or Relay servers.
using System;
using UnityEngine;

namespace UnityEngine
{
    public class Object { }
    public class Transform : Object
    {
        public Vector3 position;
        public Vector3 lossyScale = Vector3.one;
        public Vector3 InverseTransformPoint(Vector3 point) => point - position;
    }
    public static class Time
    {
        public static float unscaledTime, realtimeSinceStartup;
        public static float fixedDeltaTime = .1f;
    }
    public static class Physics { public static void SyncTransforms() { } }
    public static class Debug
    {
        public static void LogError(string message, Object context) { }
        public static void LogException(Exception error) { }
    }
}
namespace Unity.Networking.Transport.Relay { public struct RelayServerData { } }
namespace Unity.Netcode.Transports.UTP
{
    public class UnityTransport
    {
        public int Configured;
        public void SetRelayServerData(Unity.Networking.Transport.Relay.RelayServerData data) => Configured++;
    }
}
namespace Unity.Netcode
{
    public class NetworkManager
    {
        public bool IsClient, IsServer, IsConnectedClient, ShutdownInProgress, IsListening, IsHost;
        public ulong LocalClientId = 1;
        public bool StartSucceeds = true;
        public int Starts, Shutdowns;
        public event Action<ulong> OnClientConnectedCallback;
        public event Action<bool> OnClientStopped;
        public event Action OnTransportFailure;
        public bool StartHost() { Starts++; IsHost = IsServer = IsListening = StartSucceeds; return StartSucceeds; }
        public bool StartClient() { Starts++; IsClient = IsListening = StartSucceeds; return StartSucceeds; }
        public void Shutdown() { Shutdowns++; IsClient = IsServer = IsListening = false; OnClientStopped?.Invoke(false); }
        public void Connected() { IsConnectedClient = true; OnClientConnectedCallback?.Invoke(LocalClientId); }
        public void Fail() => OnTransportFailure?.Invoke();
    }
    public class NetworkObject
    {
        public NetworkTestMovement Movement = new NetworkTestMovement();
        public Transform transform => Movement.transform;
        public T GetComponent<T>() where T : class => Movement as T;
    }
}
public class NetworkTestMovement
{
    public Transform transform = new Transform();
    public bool HasCarryAnchor = true, Clear = true, Ground = true, Accepted = true, Rejected;
    public float BodyRadius = .5f;
    public Vector3 CarryOffset = new Vector3(0, 1, 1);
    public Vector3 CarryPoint;
    public uint Sequence;
    public bool PreparationRejected(uint sequence) => Sequence == sequence && Rejected;
    public bool PreparedOnServer(uint sequence) => Sequence == sequence && Accepted;
    public string PreparationDetails(uint sequence) => "test owner";
    public float GetCarryRootDistance(NetworkTestMovement other, float distance) => distance + CarryOffset.z + other.CarryOffset.z;
    public bool TryGetStartPosition(Vector3 approximate, out Vector3 result) { result = approximate; return Ground; }
    public bool StartPositionIsClear(Vector3 position, out string reason) { reason = "blocked"; return Clear; }
    public void PrepareOnServer(uint sequence, Vector3 position, Vector3 other)
    {
        Sequence = sequence;
        transform.position = position;
        CarryPoint = position + (other - position).normalized * CarryOffset.z + Vector3.up * CarryOffset.y;
    }
}
