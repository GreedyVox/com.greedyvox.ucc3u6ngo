using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Interfaces;
using Opsive.UltimateCharacterController.Items.Actions.Impact;
using Opsive.UltimateCharacterController.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Objects
{
    /// <summary>
    /// Network-synchronized projectile implementation.
    /// Handles payload generation, serialization, and application.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetCodeProjectile : Projectile, IPayloadEvent, IPayload
    {
        /// <summary>
        /// Cached impact damage data instance reused across payload applications.
        /// </summary>
        protected ImpactDamageData m_DamageData = new();
        /// <summary>
        /// Initializes the projectile on the authoritative instance.
        /// </summary>
        /// <param name="id">Projectile identifier.</param>
        /// <param name="owner">Owning GameObject.</param>
        public void Initialize(uint id, GameObject owner)
        {
            InitializeComponentReferences();
            Initialize(id, Vector3.zero, Vector3.zero, owner, m_DamageData);
        }
        /// <summary>
        /// Applies payload data via interface dispatch.
        /// </summary>
        /// <param name="data">Network-serialized payload.</param>
        /// <returns>True if payload type matched and applied.</returns>
        public bool Payload(INetworkSerializable data) => data is PayloadProjectile p && Payload(p);
        /// <summary>
        /// Produces the authoritative payload as a boxed network-serializable value.
        /// </summary>
        /// <param name="data">Generated payload.</param>
        /// <returns>True if payload was produced.</returns>
        public bool Payload(out INetworkSerializable data)
        {
            data = Payload();
            return true;
        }
        /// <summary>
        /// Applies payload data to initialize projectile runtime state.
        /// </summary>
        /// <param name="data">Projectile payload.</param>
        /// <returns>True if successfully applied.</returns>
        public bool Payload(PayloadProjectile data)
        {
            m_DamageData ??= new ImpactDamageData();
            m_DamageData.DamageAmount = data.DamageAmount;
            m_DamageData.ImpactForce = data.ImpactForce;
            m_DamageData.ImpactForceFrames = data.ImpactFrames;
            m_DamageData.ImpactStateName = data.ImpactStateName.ToString();
            m_DamageData.ImpactStateDisableTimer = data.ImpactStateDisableTimer;
            m_ImpactLayers = data.ImpactLayers;
            Initialize(data.ProjectileID, data.Velocity, data.Torque, data.Owner, m_DamageData);
            return true;
        }
        /// <summary>
        /// Builds the authoritative projectile payload.
        /// </summary>
        /// <returns>PayloadProjectile struct.</returns>
        public PayloadProjectile Payload()
        {
            return new PayloadProjectile
            {
                Owner = m_Owner.TryGetComponent(out NetworkObject net) ? net : default,
                ProjectileID = m_ID,
                Velocity = m_Velocity,
                Torque = m_Torque,
                DamageAmount = m_ImpactDamageData.DamageAmount,
                ImpactForce = m_ImpactDamageData.ImpactForce,
                ImpactFrames = m_ImpactDamageData.ImpactForceFrames,
                ImpactLayers = m_ImpactLayers.value,
                ImpactStateDisableTimer = m_ImpactDamageData.ImpactStateDisableTimer,
                ImpactStateName = m_ImpactDamageData.ImpactStateName
            };
        }
        /// <summary>
        /// Serializes payload data into a FastBufferWriter.
        /// Caller is responsible for disposing the writer.
        /// </summary>
        /// <param name="idx">Stream index.</param>
        /// <param name="writer">Writer containing serialized data.</param>
        /// <returns>True if serialization succeeded.</returns>
        public bool Payload(ref int idx, out FastBufferWriter writer)
        {
            writer = new FastBufferWriter(MaxBufferSize(), Allocator.Temp);
            writer.WriteValueSafe(idx);
            writer.WriteValueSafe(transform.position);
            writer.WriteValueSafe(Payload());
            return true;
        }
        /// <summary>
        /// Reads payload data from a buffer and applies it.
        /// </summary>
        /// <param name="reader">Fast buffer reader.</param>
        public void Payload(in FastBufferReader reader)
        {
            reader.ReadValueSafe(out PayloadProjectile data);
            Payload(data);
        }
        /// <summary>
        /// Returns the maximum serialized buffer size for the projectile payload.
        /// </summary>
        /// <returns>Required buffer size in bytes.</returns>
        public int MaxBufferSize()
        {
            return
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<NetworkObjectReference>() +
                FastBufferWriter.GetWriteSize<uint>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<float>() +
                FastBufferWriter.GetWriteSize<FixedString64Bytes>();
        }
    }
}
