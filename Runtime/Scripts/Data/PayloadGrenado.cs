using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    public struct PayloadGrenado : INetworkSerializable
    {
        public uint OwnerID;
        public Vector3 Velocity;
        public Vector3 Torque;
        public int ImpactFrames;
        public int ImpactLayers;
        public float ImpactForce;
        public float DamageAmount;
        public FixedString64Bytes ImpactStateName;
        public float ImpactStateDisableTimer;
        public float ScheduledDeactivation;
        public NetworkObjectReference Owner;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
        {
            serializer.SerializeValue(ref OwnerID);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Torque);
            serializer.SerializeValue(ref ImpactFrames);
            serializer.SerializeValue(ref ImpactLayers);
            serializer.SerializeValue(ref ImpactForce);
            serializer.SerializeValue(ref DamageAmount);
            serializer.SerializeValue(ref ImpactStateName);
            serializer.SerializeValue(ref ImpactStateDisableTimer);
            serializer.SerializeValue(ref ScheduledDeactivation);
            serializer.SerializeValue(ref Owner);
        }
    }
}