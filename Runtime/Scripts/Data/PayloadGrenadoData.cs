using GreedyVox.NetCode.Objects;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    /// <summary>
    /// Network payload transport for grenade initialization.
    /// </summary>    
    [RequireComponent(typeof(NetCodeGrenade))]
    public class PayloadGrenadoData : PayloadAbstract
    {
        /// <summary>
        /// Receives payload from authority and dispatches locally.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        protected void PongRpc(PayloadGrenado data, RpcParams rpc) => m_Payload?.Payload(data);
        /// <summary>
        /// Requests payload data from the authority.
        /// </summary>
        [Rpc(SendTo.Server)]
        protected override void PingRpc(RpcParams rpc = default)
        {
            if (m_Payload != null
            && m_Payload.Payload(out var data)
            && data is PayloadGrenado projectile)
                PongRpc(projectile, RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp));
        }
    }
}
