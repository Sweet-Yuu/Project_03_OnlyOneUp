using Unity.Netcode;
using UnityEngine;

// Điểm mang sau khi CharacterController xử lý di chuyển, không phải vị trí tổ do client áp đặt.
public struct NestCarryInput : INetworkSerializable
{
    public uint Generation;
    public uint Sequence;
    public Vector3 HostAnchor;
    public Vector3 GuestAnchor;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Generation);
        serializer.SerializeValue(ref Sequence);
        serializer.SerializeValue(ref HostAnchor);
        serializer.SerializeValue(ref GuestAnchor);
    }
}
