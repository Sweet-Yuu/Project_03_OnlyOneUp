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
    public event System.Action<string> NameChanged;

    public override void OnNetworkSpawn()
    {
        _displayName.OnValueChanged += HandleNameChanged;
        NameChanged?.Invoke(DisplayName);
    }

    public override void OnNetworkDespawn()
    {
        _displayName.OnValueChanged -= HandleNameChanged;
        NameChanged?.Invoke(string.Empty);
    }

    private void HandleNameChanged(FixedString128Bytes previous, FixedString128Bytes current)
        => NameChanged?.Invoke(current.ToString());

    public void SetNameOnServer(string playerName)
    {
        if (!IsServer) return;

        _displayName.Value = new FixedString128Bytes(playerName);
    }
}
