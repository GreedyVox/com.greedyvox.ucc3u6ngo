using GreedyVox.NetCode.Objects;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Data
{
    /// <summary>
    /// Payload transport for projectile initialization data.
    /// </summary>
    [RequireComponent(typeof(NetCodeItemPickup))]
    public class PayloadItemPickupData : PayloadAbstract
    {
        /// <summary>
        /// Receives authoritative payload data and dispatches it locally.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        protected void PongRpc(PayloadItemPickup data, RpcParams rpc) => m_Payload?.Payload(data);
        /// <summary>
        /// Requests payload data from the authority.
        /// </summary>
        [Rpc(SendTo.Server)]
        protected override void PingRpc(RpcParams rpc = default)
        {
            if (m_Payload != null
            && m_Payload.Payload(out var data)
            && data is PayloadItemPickup projectile)
                PongRpc(projectile, RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp));
        }
    }
}
