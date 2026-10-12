using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

// Owns connection state and subscriptions, not GUI drawing or authentication details.
public sealed class RelaySession : IDisposable
{
    private readonly NetworkManager manager;
    private readonly UnityTransport transport;
    private readonly IRelayRoomService rooms;
    private readonly CancellationTokenSource lifetime;
    private bool disposed;
    private bool leaving;
    private float connectionStartedAt;
    public string RoomCode { get; private set; } = "";
    public string Status { get; private set; } = "Chua ket noi.";
    public bool Busy { get; private set; }

    public RelaySession(NetworkManager manager, UnityTransport transport,
        IRelayRoomService rooms, CancellationToken cancellation)
    {
        this.manager = manager;
        this.transport = transport;
        this.rooms = rooms;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        manager.OnClientConnectedCallback += OnClientConnected;
        manager.OnClientStopped += OnClientStopped;
        manager.OnTransportFailure += OnTransportFailure;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
        if (manager == null) return;
        manager.OnClientConnectedCallback -= OnClientConnected;
        manager.OnClientStopped -= OnClientStopped;
        manager.OnTransportFailure -= OnTransportFailure;
    }

    public void Disconnect()
    {
        if (disposed || Busy) return;
        leaving = true;
        RoomCode = "";
        Status = "Dang roi phong...";
        manager.Shutdown();
    }
    public void Tick()
    {
        if (disposed || manager == null) return;
        // StartClient thanh cong chua co nghia da ket noi voi Host.
        if (manager.IsClient &&
            !manager.IsServer &&
            !manager.IsConnectedClient &&
            !manager.ShutdownInProgress &&
            Time.realtimeSinceStartup - connectionStartedAt > 20f)
        {
            Status = "Ket noi qua lau. Hay kiem tra ma phong va mang.";
            manager.Shutdown();
        }
    }
    public async Task ConnectAsync(bool createRoom, string joinCodeInput)
    {
        if (disposed || manager == null || Busy || manager.IsListening || manager.ShutdownInProgress)
            return;

        string code = joinCodeInput.Trim().ToUpperInvariant();

        if (!createRoom && string.IsNullOrEmpty(code))
        {
            Status = "Hay nhap ma phong.";
            return;
        }

        if (transport == null)
        {
            Status = "NetworkManager dang thieu Unity Transport.";
            return;
        }

        Busy = true;
        leaving = false;
        RoomCode = "";
        Status = "Dang ket noi Unity Services...";

        // Ngan viec bat dau mang neu da thoat Play Mode.
        var cancellation = lifetime.Token;

        try
        {
            await rooms.SignInAsync(cancellation);
            cancellation.ThrowIfCancellationRequested();

            if (createRoom)
            {
                Status = "Dang tao phong...";

                RelayRoom room = await rooms.CreateAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
                transport.SetRelayServerData(room.ServerData);

                if (!manager.StartHost())
                    throw new Exception("Khong khoi dong duoc Host.");

                RoomCode = room.Code;
                Status = "Da tao phong. Gui ma cho nguoi choi con lai.";
            }
            else
            {
                Status = "Dang tim phong...";

                RelayRoom room = await rooms.JoinAsync(code, cancellation);
                cancellation.ThrowIfCancellationRequested();
                transport.SetRelayServerData(room.ServerData);

                connectionStartedAt = Time.realtimeSinceStartup;

                if (!manager.StartClient())
                    throw new Exception("Khong khoi dong duoc Client.");

                Status = "Dang ket noi voi Host...";
            }
        }
        catch (OperationCanceledException)
        {
            // Object bi huy khi thoat Play Mode.
        }
        catch (Exception exception)
        {
            if (disposed || manager == null) return;

            RoomCode = "";
            Status = "Loi: " + exception.Message;
            Debug.LogException(exception);

            if (manager.IsListening)
                manager.Shutdown();
        }
        finally
        {
            if (!disposed)
                Busy = false;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        if (clientId == manager.LocalClientId && !manager.IsHost)
            Status = "Da ket noi voi Host!";
    }
    private void OnClientStopped(bool wasHost)
    {
        RoomCode = "";

        if (leaving)
            Status = "Da roi phong.";
        else
            Status += "\nKet noi da dong. Host co the da thoat hoac mat mang.";

        leaving = false;
    }
    private void OnTransportFailure()
    {
        Status = "Loi duong truyen. Hay kiem tra ket noi Internet.";
        RoomCode = "";
    }
}
