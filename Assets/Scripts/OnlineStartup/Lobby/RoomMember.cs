public readonly struct RoomMember
{
    public ulong ClientId { get; }
    public string Name { get; }
    public bool IsHost { get; }
    public RoomMember(ulong clientId, string name, bool isHost)
    {
        ClientId = clientId;
        Name = name;
        IsHost = isHost;
    }
}
