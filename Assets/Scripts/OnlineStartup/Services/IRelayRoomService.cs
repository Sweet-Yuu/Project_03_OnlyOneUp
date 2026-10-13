using System.Threading;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;

public readonly struct RelayRoom
{
    public readonly string Code;
    public readonly RelayServerData ServerData;
    public RelayRoom(string code, RelayServerData data) { Code = code; ServerData = data; }
}

public interface IRelayRoomService
{
    Task SignInAsync(CancellationToken cancellation);
    Task<RelayRoom> CreateAsync(CancellationToken cancellation);
    Task<RelayRoom> JoinAsync(string code, CancellationToken cancellation);
}

