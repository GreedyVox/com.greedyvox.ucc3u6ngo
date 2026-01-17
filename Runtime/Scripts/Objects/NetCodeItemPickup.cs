using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Interfaces;
using Opsive.Shared.Game;
using Opsive.Shared.Utility;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Objects;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Objects
{
    /// <summary>
    /// Network-synchronized item pickup.
    /// Handles payload generation, serialization, and remote initialization.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetCodeItemPickup : ItemPickup, IPayloadEvent, IPayload
    {
        /// <summary>
        /// Cached trajectory object.
        /// </summary>
        private TrajectoryObject m_TrajectoryObject;
        /// <summary>
        /// Initializes cached component references.
        /// </summary>
        protected override void Awake()
        {
            m_TrajectoryObject = gameObject.GetCachedComponent<TrajectoryObject>();
            base.Awake();
        }
        /// <summary>
        /// Required by IPayload interface. Not used for item pickups.
        /// </summary>
        public void Initialize(uint id, GameObject owner) { }
        /// <summary>
        /// Applies payload from boxed network-serializable data.
        /// </summary>
        public bool Payload(INetworkSerializable data) => data is PayloadItemPickup p && Payload(p);
        /// <summary>
        /// Builds boxed network-serializable payload.
        /// </summary>
        public bool Payload(out INetworkSerializable data)
        {
            data = Payload();
            return true;
        }
        /// <summary>
        /// Builds authoritative item pickup payload.
        /// </summary>
        public PayloadItemPickup Payload()
        {
            var src = m_ItemDefinitionAmounts;
            var len = src.Length;
            var data = new PayloadItemPickup
            {
                Owner = m_TrajectoryObject != null
                        && m_TrajectoryObject.Owner != null
                        && m_TrajectoryObject.Owner.TryGetComponent(out NetworkObject net)
                        ? net : default,
                Rotation = transform.rotation,
                Velocity = m_TrajectoryObject != null ? m_TrajectoryObject.Velocity : Vector3.zero,
                Torque = m_TrajectoryObject != null ? m_TrajectoryObject.Torque : Vector3.zero,
                ItemCount = len,
                ItemID = new uint[len],
                ItemAmounts = new int[len]
            };
            for (int i = 0; i < len; ++i)
            {
                var entry = src[i];
                data.ItemAmounts[i] = entry.Amount;
                data.ItemID[i] = ((ItemType)entry.ItemIdentifier).ID;
            }
            return data;
        }
        /// <summary>
        /// Serializes payload into a FastBufferWriter.
        /// Caller owns writer disposal.
        /// </summary>
        public bool Payload(ref int idx, out FastBufferWriter writer)
        {
            writer = new FastBufferWriter(MaxBufferSize(), Allocator.Temp);
            writer.WriteValueSafe(idx);
            writer.WriteValueSafe(transform.position);
            writer.WriteValueSafe(Payload());
            return true;
        }
        /// <summary>
        /// Deserializes payload and initializes item pickup.
        /// </summary>
        public bool Payload(PayloadItemPickup data)
        {
            transform.rotation = data.Rotation;
            for (int i = 0; i < m_ItemDefinitionAmounts.Length; ++i)
                GenericObjectPool.Return(m_ItemDefinitionAmounts[i]);
            if (m_ItemDefinitionAmounts.Length != data.ItemCount)
                m_ItemDefinitionAmounts = new ItemIdentifierAmount[data.ItemCount];
            for (int i = 0; i < data.ItemCount; ++i)
            {
                m_ItemDefinitionAmounts[i] = new ItemIdentifierAmount(
                ItemIdentifierTracker.GetItemIdentifier(data.ItemID[i]).GetItemDefinition(), data.ItemAmounts[i]);
            }
            Initialize(true);
            if (m_TrajectoryObject != null)
                m_TrajectoryObject.Initialize(data.Velocity, data.Torque, data.Owner);
            return true;
        }
        /// <summary>
        /// Deserializes payload from a FastBufferReader.
        /// </summary>
        public void Payload(in FastBufferReader reader)
        {
            reader.ReadValueSafe(out PayloadItemPickup data);
            Payload(data);
        }
        /// <summary>
        /// Calculates required FastBufferWriter capacity.
        /// </summary>
        public int MaxBufferSize() => MaxBufferSize(m_ItemDefinitionAmounts.Length);
        public int MaxBufferSize(in int length)
        {
            return
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<Quaternion>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<NetworkObjectReference>() +
                length * FastBufferWriter.GetWriteSize<uint>() +
                length * FastBufferWriter.GetWriteSize<int>();
        }
    }
}
