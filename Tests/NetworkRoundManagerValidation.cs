using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;

public static class NetworkRoundManagerValidation
{
    private sealed class Target : IRoundResetTarget
    {
        public event Action RoundFailed;
        public float GripDistance => 4f;
        public bool IsPlaying { get; set; } = true;
        public bool CanRestart = true;
        public int Restarts;
        public Vector3 LastCenter;
        public IRoundSession Session;
        public bool CanPlayerMove(uint sequence) => IsPlaying && sequence == 1 && !Session.IsRespawning;
        public bool TryRestartRound(Vector3 center)
        {
            if (!CanRestart) return false;
            LastCenter = center;
            Restarts++;
            return true;
        }
        public void BreakEgg() => RoundFailed?.Invoke();
    }
    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static RpcParams Sender(ulong sender) => new RpcParams { Receive = new ReceiveParams { SenderClientId = sender } };

    public static int Run()
    {
        int checks = 0;
        void Check(bool valid, string message) { checks++; if (!valid) throw new Exception(message); }
        var root = new GameObject("Round manager test");
        var h = new GameObject("Host");
        var g = new GameObject("Guest");
        var flag = new GameObject("Checkpoint");
        var dead = new GameObject("Dead zone");
        try
        {
            var host = h.AddComponent<NetworkObject>();
            var guest = g.AddComponent<NetworkObject>();
            guest.OwnerClientId = 1;
            h.AddComponent<NetworkTestMovement>();
            var guestMovement = g.AddComponent<NetworkTestMovement>();
            flag.transform.position = new Vector3(30, 2, 40);
            flag.AddComponent<BoxCollider>().isTrigger = true;
            var checkpoint = flag.AddComponent<OnlineCheckpoint>();
            Call(checkpoint, "Awake"); // EditMode không tự chạy Awake như scene đang chơi.
            dead.tag = "Dead";
            dead.transform.position = Vector3.right * 100;
            dead.AddComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();
            var manager = root.AddComponent<NetworkRoundManager>();
            var target = new Target { Session = manager };
            typeof(NetworkRoundManager).GetField("_resetTarget", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, target);
            manager.NetworkManager = new NetworkManager();
            manager.NetworkManager.ConnectedClientsList.Add(new Client { ClientId = 0, PlayerObject = host });
            manager.NetworkManager.ConnectedClientsList.Add(new Client { ClientId = 1, PlayerObject = guest });
            manager.IsSpawned = manager.IsServer = true;
            manager.OnNetworkSpawn();
            Check(manager.RespawnCenter == new Vector3(45, 2.1f, 30), "Initial spawn preserved");
            Call(manager, "ReportCheckpointRpc", 1, 1u, Sender(1));
            Check(manager.ActiveCheckpointId == 0, "Remote checkpoint rejected when player is far away");
            guest.transform.position = flag.transform.position;
            Call(manager, "ReportCheckpointRpc", 1, 1u, Sender(99));
            Check(manager.ActiveCheckpointId == 0, "Unknown sender rejected");
            Call(manager, "ReportCheckpointRpc", 1, 2u, Sender(1));
            Check(manager.ActiveCheckpointId == 0, "Stale preparation sequence rejected");
            Call(manager, "ReportCheckpointRpc", 99, 1u, Sender(1));
            Check(manager.ActiveCheckpointId == 0, "Unregistered checkpoint rejected");
            guestMovement.Ground = false;
            Call(manager, "ReportCheckpointRpc", 1, 1u, Sender(1));
            Check(manager.ActiveCheckpointId == 0, "Unsafe spawn rejected");
            guestMovement.Ground = true;
            Call(manager, "ReportCheckpointRpc", 1, 1u, Sender(1));
            Check(manager.ActiveCheckpointId == 1 && manager.RespawnCenter == checkpoint.SpawnCenter,
                "Either player can save the shared checkpoint");
            target.BreakEgg();
            Check(manager.IsRespawning && !target.CanPlayerMove(1), "Egg break suspends movement");
            target.BreakEgg();
            Call(manager, "FixedUpdate");
            Check(target.Restarts == 0, "Broken visual delay respected");
            manager.NetworkManager.ServerTime.Time = 2;
            target.CanRestart = false;
            Call(manager, "FixedUpdate");
            Check(manager.IsRespawning && target.Restarts == 0, "Missing players keep respawn pending");
            target.CanRestart = true;
            Call(manager, "FixedUpdate");
            Call(manager, "FixedUpdate");
            Check(!manager.IsRespawning && target.Restarts == 1 && target.LastCenter == checkpoint.SpawnCenter,
                "Reconnect restarts once at the checkpoint");
            Call(manager, "ReportDeathRpc", 1u, Sender(0));
            Check(!manager.IsRespawning, "Death report must be near a registered zone");
            host.transform.position = dead.transform.position;
            Call(manager, "ReportDeathRpc", 1u, Sender(0));
            Check(manager.IsRespawning, "Host can report death");
            Call(manager, "FixedUpdate");
            Check(target.Restarts == 2 && target.LastCenter == checkpoint.SpawnCenter, "Death respawns immediately at shared checkpoint");
            manager.OnNetworkDespawn();
            target.BreakEgg();
            Check(!manager.IsRespawning, "Despawn unsubscribes failure events");
            manager.OnNetworkSpawn();
            Check(manager.ActiveCheckpointId == 0 && manager.RespawnCenter == new Vector3(45, 2.1f, 30), "New session resets checkpoint");
            manager.OnNetworkDespawn();
            manager.IsServer = false;
            manager.OnNetworkSpawn();
            target.BreakEgg();
            Check(!manager.IsRespawning, "Client does not schedule authoritative respawns");
            manager.OnNetworkDespawn();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(h);
            UnityEngine.Object.DestroyImmediate(g);
            UnityEngine.Object.DestroyImmediate(flag);
            UnityEngine.Object.DestroyImmediate(dead);
        }
        return checks;
    }
}
