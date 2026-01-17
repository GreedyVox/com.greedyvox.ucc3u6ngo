using GreedyVox.NetCode.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    /// <summary>
    /// Abstract base class for payload-driven network initialization.
    /// 
    /// This component coordinates authority-based payload requests
    /// during the NetworkObject spawn lifecycle. Non-authoritative
    /// instances request payload data from the authoritative side
    /// (Server or Owner depending on implementation).
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class PayloadAbstract : NetworkBehaviour
    {
        /// <summary>
        /// Cached payload handler implemented on the same GameObject.
        /// Responsible for producing and consuming payload data.
        /// </summary>
        protected IPayloadEvent m_Payload;
        /// <summary>
        /// Sends a payload request to the authoritative instance.
        /// Implementations define whether authority resides on
        /// the Server or the Owner.
        /// </summary>
        /// <param name="rpc">
        /// Optional RPC parameters providing sender context.
        /// </param>
        protected abstract void PingRpc(RpcParams rpc = default);
        /// <summary>
        /// Called when the NetworkObject is spawned.
        /// 
        /// On non-owning instances, this requests authoritative
        /// payload data to initialize local state.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // Cache once; missing payload handler is a hard error
            if (m_Payload == null && !TryGetComponent(out m_Payload))
            {
                Debug.LogError($"[{GetType().Name}] Missing {nameof(IPayloadEvent)} on {name}", this);
                return;
            }
            // Only non-authoritative instances request payload data
            if (!IsOwner) PingRpc();
        }
    }
}
