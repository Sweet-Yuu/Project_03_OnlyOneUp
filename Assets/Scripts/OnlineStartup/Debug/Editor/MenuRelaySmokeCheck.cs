using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

/// <summary>Kiểm tra hai endpoint Relay thật trong cùng một process; không thay thế kiểm tra hai máy/build độc lập.</summary>
public static class MenuRelaySmokeCheck
{
    private static bool _running;
    [InitializeOnLoadMethod]
    private static void Initialize() => EditorApplication.update += Poll;
    private static void Poll()
    {
        if (_running || !EditorApplication.isPlaying || !File.Exists("Docs/MenuStart/relay-smoke.request")) return;
        File.Delete("Docs/MenuStart/relay-smoke.request");
        Run();
    }
    private static async void Run()
    {
        _running = true;
        GameObject guestObject = null;
        RelaySession guestSession = null;
        RelayRoomRoster guestRoster = null;
        try
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<MenuRelayController>();
            var hostRoster = (RelayRoomRoster)typeof(MenuRelayController).GetField("_roster", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
            var host = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Single();
            if (!host.IsHost)
            {
                UnityEngine.Object.FindFirstObjectByType<MenuStartController>().OpenRoomSelection();
                controller.Host();
                await Until(() => host.IsHost && !string.IsNullOrEmpty(hostRoster.Code));
            }
            if (hostRoster.Members.Count != 1 || string.IsNullOrEmpty(hostRoster.Code)) throw new Exception("Host roster/code not ready.");
            guestObject = new GameObject("TemporaryRelayGuest");
            var transport = guestObject.AddComponent<UnityTransport>();
            var manager = guestObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig();
            manager.NetworkConfig.FromBase64(host.NetworkConfig.ToBase64());
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.AddRange(host.NetworkConfig.Prefabs.NetworkPrefabsLists);
            manager.NetworkConfig.Prefabs.Initialize();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.EnableSceneManagement = false;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.ProtocolVersion = 1;
            guestRoster = new RelayRoomRoster(manager);
            guestRoster.Prepare("Guest Check");
            guestSession = new RelaySession(manager, transport, new RelayRoomService(), CancellationToken.None);
            await guestSession.ConnectAsync(false, hostRoster.Code);
            await Until(() => manager.IsConnectedClient && guestRoster.Members.Count == 2 && hostRoster.Members.Count == 2);
            if (guestRoster.Code != hostRoster.Code || guestRoster.Members[0].Name != hostRoster.Members[0].Name || guestRoster.Members[1].Name != "Guest Check" || guestRoster.Members[1].IsHost)
                throw new Exception("Roster/role/code mismatch.");
            File.WriteAllText("Docs/MenuStart/relay-smoke-result.txt", "PASS: actual Relay host + guest connected; both roster counts 2/2, host and guest names/roles and room code match.\n");
            // Giữ vài giây để quan sát hàng client trong Game View.
            await Task.Delay(8000);
            guestSession.Disconnect();
            await Until(() => hostRoster.Members.Count == 1);
            File.AppendAllText("Docs/MenuStart/relay-smoke-result.txt", "PASS: guest leave updates host roster to 1/2.\n");
        }
        catch (Exception e)
        {
            File.AppendAllText("Docs/MenuStart/relay-smoke-result.txt", "FAILED: " + e + "\n");
            Debug.LogException(e);
        }
        finally
        {
            guestSession?.Dispose();
            guestRoster?.Dispose();
            if (guestObject != null)
            {
                var manager = guestObject.GetComponent<NetworkManager>();
                if (manager.IsListening) manager.Shutdown();
                UnityEngine.Object.Destroy(guestObject);
            }
            _running = false;
        }
    }
    private static async Task Until(Func<bool> condition)
    {
        double timeout = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Relay check timed out.");
            await Task.Delay(100);
        }
    }
}
