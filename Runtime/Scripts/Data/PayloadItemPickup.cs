using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    public struct PayloadItemPickup : INetworkSerializable
    {
        public NetworkObjectReference Owner;
        public Quaternion Rotation;
        public int ItemCount;
        public Vector3 Torque;
        public Vector3 Velocity;
        public uint[] ItemID;
        public int[] ItemAmounts;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
        {
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref ItemCount);
            serializer.SerializeValue(ref Torque);
            serializer.SerializeValue(ref Velocity);
            if (serializer.IsReader)
            {
                ItemID = new uint[ItemCount];
                ItemAmounts = new int[ItemCount];
            }
            for (int n = 0; n < ItemCount; n++)
                serializer.SerializeValue(ref ItemID[n]);
            for (int n = 0; n < ItemCount; n++)
                serializer.SerializeValue(ref ItemAmounts[n]);
        }
    }
}