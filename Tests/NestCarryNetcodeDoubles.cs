// Chỉ dùng trong project kiểm thử tạm: các phép nội suy dùng Unity thật.
namespace Unity.Netcode
{
    public interface IReaderWriter { }
    public interface INetworkSerializable
    {
        void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;
    }
    public struct BufferSerializer<T> where T : IReaderWriter
    {
        public void SerializeValue<TValue>(ref TValue value) { }
    }
}
