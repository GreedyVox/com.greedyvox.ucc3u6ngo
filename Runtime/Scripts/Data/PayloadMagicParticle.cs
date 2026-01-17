using Unity.Netcode;

namespace GreedyVox.NetCode.Data
{
    public struct PayloadMagicParticle : INetworkSerializable
    {
        public NetworkObjectReference Owner;
        public uint CastID;
        public int SlotID;
        public int ActionID;
        public int ActionIndex;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
        {
            serializer.SerializeValue(ref Owner);
            serializer.SerializeValue(ref CastID);
            serializer.SerializeValue(ref SlotID);
            serializer.SerializeValue(ref ActionID);
            serializer.SerializeValue(ref ActionIndex);
        }
    }
}