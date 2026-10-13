using Unity.Collections;
using Unity.Netcode;

public sealed class NetworkPlayerIdentity : NetworkBehaviour
{
    private readonly NetworkVariable<FixedString128Bytes> _displayName =
        new NetworkVariable<FixedString128Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public string DisplayName => _displayName.Value.ToString();

    public void SetNameOnServer(string playerName)
    {
        if (!IsServer) return;

        _displayName.Value = new FixedString128Bytes(playerName);
    }
}