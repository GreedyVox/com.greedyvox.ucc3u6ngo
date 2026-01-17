using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    public struct PayloadProjectile : INetworkSerializable
    {
        public NetworkObjectReference Owner;
        public uint ProjectileID;
        public Vector3 Velocity;
        public Vector3 Torque;
        public float DamageAmount;
        public float ImpactForce;
        public int ImpactFrames;
        public int ImpactLayers;
        public float ImpactStateDisableTimer;
        public FixedString64Bytes ImpactStateName;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
        {
            serializer.SerializeValue(ref Owner);
            serializer.SerializeValue(ref ProjectileID);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Torque);
            serializer.SerializeValue(ref DamageAmount);
            serializer.SerializeValue(ref ImpactForce);
            serializer.SerializeValue(ref ImpactFrames);
            serializer.SerializeValue(ref ImpactLayers);
            serializer.SerializeValue(ref ImpactStateDisableTimer);
            serializer.SerializeValue(ref ImpactStateName);
        }
    }
}