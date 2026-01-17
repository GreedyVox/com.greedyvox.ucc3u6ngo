using GreedyVox.NetCode.Objects;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    /// <summary>
    /// Payload transport for magic initialization data.
    /// </summary>
    [RequireComponent(typeof(NetCodeMagicParticle))]
    public class PayloadMagicParticleData : PayloadAbstract
    {
        /// <summary>
        /// Receives authoritative payload data and dispatches it locally.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        protected void PongRpc(PayloadMagicParticle data, RpcParams rpc) => m_Payload?.Payload(data);
        /// <summary>
        /// Requests payload data from the authority.
        /// </summary>
        [Rpc(SendTo.Server)]
        protected override void PingRpc(RpcParams rpc = default)
        {
            if (m_Payload != null
            && m_Payload.Payload(out var data)
            && data is PayloadMagicParticle projectile)
                PongRpc(projectile, RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp));
        }
    }
}