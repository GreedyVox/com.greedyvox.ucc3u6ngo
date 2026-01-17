using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Interfaces;
using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Items.Actions.Impact;
using Opsive.UltimateCharacterController.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Objects
{
    /// <summary>
    /// Network-synchronized grenade implementation.
    /// Handles authoritative payload generation, serialization,
    /// and remote initialization.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NetCodeInfo))]
    public class NetCodeGrenade : Grenade, IPayloadEvent, IPayload
    {
        /// <summary>
        /// Cached damage data instance reused across initialization.
        /// </summary>
        private ImpactDamageData m_DamageData = new();
        /// <summary>
        /// Initializes the grenade locally on the authoritative side.
        /// </summary>
        /// <param name="id">Grenade identifier.</param>
        /// <param name="owner">Owning GameObject.</param>
        public void Initialize(uint id, GameObject owner)
        {
            InitializeComponentReferences();
            Initialize(id, Vector3.zero, Vector3.zero, owner, m_DamageData);
        }
        /// <summary>
        /// Applies payload via boxed network-serializable dispatch.
        /// </summary>
        public bool Payload(INetworkSerializable data) => data is PayloadGrenado g && Payload(g);
        /// <summary>
        /// Produces payload as boxed network-serializable data.
        /// </summary>
        public bool Payload(out INetworkSerializable data)
        {
            data = Payload();
            return true;
        }
        /// <summary>
        /// Builds the authoritative grenade payload.
        /// </summary>
        public PayloadGrenado Payload()
        {
            return new PayloadGrenado
            {
                OwnerID = m_ID,
                Velocity = m_Velocity,
                Torque = m_Torque,
                ImpactFrames = m_ImpactDamageData.ImpactForceFrames,
                ImpactLayers = m_ImpactLayers.value,
                ImpactForce = m_ImpactDamageData.ImpactForce,
                DamageAmount = m_ImpactDamageData.DamageAmount,
                ImpactStateName = m_ImpactDamageData.ImpactStateName,
                ImpactStateDisableTimer = m_ImpactDamageData.ImpactStateDisableTimer,
                ScheduledDeactivation = m_ScheduledDeactivation != null ? m_ScheduledDeactivation.EndTime - Time.time : -1f,
                Owner = m_Owner != null && m_Owner.TryGetComponent(out NetworkObject net) ? net : default
            };
        }
        /// <summary>
        /// Serializes grenade payload to a FastBufferWriter.
        /// Caller is responsible for disposing the writer.
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
        /// Applies payload data and initializes grenade state.
        /// </summary>
        public bool Payload(PayloadGrenado data)
        {
            m_DamageData ??= new ImpactDamageData();
            m_DamageData.DamageAmount = data.DamageAmount;
            m_DamageData.ImpactForce = data.ImpactForce;
            m_DamageData.ImpactForceFrames = data.ImpactFrames;
            m_DamageData.ImpactStateName = data.ImpactStateName.ToString();
            m_DamageData.ImpactStateDisableTimer = data.ImpactStateDisableTimer;
            m_ImpactLayers = data.ImpactLayers;
            Initialize(data.OwnerID, data.Velocity, data.Torque, data.Owner, m_DamageData);
            if (data.ScheduledDeactivation > 0f)
                m_ScheduledDeactivation = Scheduler.Schedule(data.ScheduledDeactivation, Deactivate);
            return true;
        }
        /// <summary>
        /// Deserializes grenade payload from a FastBufferReader.
        /// </summary>
        public void Payload(in FastBufferReader reader)
        {
            reader.ReadValueSafe(out PayloadGrenado data);
            Payload(data);
        }
        /// <summary>
        /// Returns required buffer size for payload serialization.
        /// </summary>
        public int MaxBufferSize()
        {
            return
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<uint>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<FixedString64Bytes>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<NetworkObjectReference>();
        }
    }
}
