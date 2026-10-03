using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

[RequireComponent(typeof(NetworkManager))]
public class NetworkTestUI : MonoBehaviour
{
    private NetworkManager manager;
    private UnityTransport transport;

    private string joinCodeInput = "";
    private string roomCode = "";
    private string status = "Chua ket noi.";

    private bool busy;
    private bool leaving;
    private float connectionStartedAt;
    private NetworkNest testNest;

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();

        manager.OnClientConnectedCallback += OnClientConnected;
        manager.OnClientStopped += OnClientStopped;
        manager.OnTransportFailure += OnTransportFailure;
    }

    private void OnDestroy()
    {
        if (manager == null) return;

        manager.OnClientConnectedCallback -= OnClientConnected;
        manager.OnClientStopped -= OnClientStopped;
        manager.OnTransportFailure -= OnTransportFailure;
    }

    private void Update()
    {
        // StartClient thanh cong chua co nghia da ket noi voi Host.
        if (manager.IsClient &&
            !manager.IsServer &&
            !manager.IsConnectedClient &&
            !manager.ShutdownInProgress &&
            Time.realtimeSinceStartup - connectionStartedAt > 20f)
        {
            status = "Ket noi qua lau. Hay kiem tra ma phong va mang.";
            manager.Shutdown();
        }
    }

    private async void ConnectRelay(bool createRoom)
    {
        if (busy || manager.IsListening || manager.ShutdownInProgress)
            return;

        string code = joinCodeInput.Trim().ToUpperInvariant();

        if (!createRoom && string.IsNullOrEmpty(code))
        {
            status = "Hay nhap ma phong.";
            return;
        }

        if (transport == null)
        {
            status = "NetworkManager dang thieu Unity Transport.";
            return;
        }

        busy = true;
        leaving = false;
        roomCode = "";
        status = "Dang ket noi Unity Services...";

        // Ngan viec bat dau mang neu da thoat Play Mode.
        var lifetime = destroyCancellationToken;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                lifetime.ThrowIfCancellationRequested();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Tach tai khoan test cua Editor va ban build tren cung may.
                AuthenticationService.Instance.SwitchProfile(
                    Application.isEditor ? "editor" : "build");

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                lifetime.ThrowIfCancellationRequested();
            }

            if (createRoom)
            {
                status = "Dang tao phong...";

                // 1 ket noi them + Host = 2 nguoi.
                var allocation =
                    await RelayService.Instance.CreateAllocationAsync(1);
                lifetime.ThrowIfCancellationRequested();

                string newCode =
                    await RelayService.Instance.GetJoinCodeAsync(
                        allocation.AllocationId);
                lifetime.ThrowIfCancellationRequested();

                transport.SetRelayServerData(
                    AllocationUtils.ToRelayServerData(allocation, "dtls"));

                if (!manager.StartHost())
                    throw new Exception("Khong khoi dong duoc Host.");

                roomCode = newCode;
                status = "Da tao phong. Gui ma cho nguoi choi con lai.";
            }
            else
            {
                status = "Dang tim phong...";

                var allocation =
                    await RelayService.Instance.JoinAllocationAsync(code);
                lifetime.ThrowIfCancellationRequested();

                transport.SetRelayServerData(
                    AllocationUtils.ToRelayServerData(allocation, "dtls"));

                connectionStartedAt = Time.realtimeSinceStartup;

                if (!manager.StartClient())
                    throw new Exception("Khong khoi dong duoc Client.");

                status = "Dang ket noi voi Host...";
            }
        }
        catch (OperationCanceledException)
        {
            // Object bi huy khi thoat Play Mode.
        }
        catch (Exception exception)
        {
            if (this == null) return;

            roomCode = "";
            status = "Loi: " + exception.Message;
            Debug.LogException(exception);

            if (manager.IsListening)
                manager.Shutdown();
        }
        finally
        {
            if (this != null)
                busy = false;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == manager.LocalClientId && !manager.IsHost)
            status = "Da ket noi voi Host!";
    }

    private void OnClientStopped(bool wasHost)
    {
        roomCode = "";

        if (leaving)
            status = "Da roi phong.";
        else
            status += "\nKet noi da dong. Host co the da thoat hoac mat mang.";

        leaving = false;
    }

    private void OnTransportFailure()
    {
        status = "Loi duong truyen. Hay kiem tra ket noi Internet.";
        roomCode = "";
    }

    private void OnGUI()
    {
        if (manager == null) return;

        GUILayout.BeginArea(
            new Rect(20, 20, 360, 490), GUI.skin.box);

        GUILayout.Label("ONLINE CO-OP - RELAY");
        GUILayout.Space(8);

        bool active = manager.IsClient || manager.IsServer;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled &&
                      !busy &&
                      !manager.ShutdownInProgress;

        if (!active)
        {
            if (GUILayout.Button("Create Room", GUILayout.Height(40)))
                ConnectRelay(true);

            GUILayout.Space(10);
            GUILayout.Label("Nhap ma phong:");
            joinCodeInput = GUILayout.TextField(joinCodeInput, 12);

            if (GUILayout.Button("Join Room", GUILayout.Height(40)))
                ConnectRelay(false);
        }
        else
        {
            if (testNest == null)
                testNest = FindFirstObjectByType<NetworkNest>();
            if (manager.IsHost)
            {
                GUILayout.Label("Ban la Host");
                GUILayout.Label("Ma phong: " + roomCode);
                GUILayout.Label(
                    "So nguoi: " + manager.ConnectedClientsIds.Count + "/2");

                if (GUILayout.Button("Copy Code"))
                    GUIUtility.systemCopyBuffer = roomCode;
            }
            else
            {
                GUILayout.Label(manager.IsConnectedClient
                    ? "Client - Da ket noi"
                    : "Client - Dang ket noi...");
            }

            if (testNest != null && testNest.IsSpawned && testNest.HasEggTest)
            {
                GUILayout.Space(8);
                GUILayout.Label(testNest.EggPhase == NestEggPhase.Waiting
                    ? "Trung: Cho du 2 nguoi"
                    : testNest.EggPhase == NestEggPhase.Intact
                        ? "Trung: Nguyen ven"
                        : "Trung: Da vo (mau do) - Host bam Reset");
                if (manager.IsHost && testNest.CanResetEgg)
                {
                    if (GUILayout.Button("Reset Egg (test)", GUILayout.Height(30)))
                        testNest.ResetEggOnServer();
                    if (GUILayout.Button("Drop Egg (test)", GUILayout.Height(30)))
                        testNest.DropEggForTestOnServer();
                }
            }

            if (GUILayout.Button("Disconnect", GUILayout.Height(40)))
            {
                leaving = true;
                roomCode = "";
                status = "Dang roi phong...";
                manager.Shutdown();
            }
        }

        GUI.enabled = previousEnabled;

        GUILayout.Space(10);
        GUILayout.Label(status);
        GUILayout.EndArea();
    }
}
