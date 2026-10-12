using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public sealed class RelayRoomService : IRelayRoomService
{
    public async Task SignInAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
            cancellation.ThrowIfCancellationRequested();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SwitchProfile(Application.isEditor ? "editor" : "build");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            cancellation.ThrowIfCancellationRequested();
        }
    }

    public async Task<RelayRoom> CreateAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var allocation = await RelayService.Instance.CreateAllocationAsync(1);
        cancellation.ThrowIfCancellationRequested();
        string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        cancellation.ThrowIfCancellationRequested();
        return new RelayRoom(code, AllocationUtils.ToRelayServerData(allocation, "dtls"));
    }

    public async Task<RelayRoom> JoinAsync(string code, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var allocation = await RelayService.Instance.JoinAllocationAsync(code);
        cancellation.ThrowIfCancellationRequested();
        return new RelayRoom(code, AllocationUtils.ToRelayServerData(allocation, "dtls"));
    }
}
