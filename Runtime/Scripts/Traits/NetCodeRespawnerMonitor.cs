using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Networking.Traits;
using Opsive.UltimateCharacterController.Traits;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Synchronizes the Respawner over the network.
/// Handles local and remote respawn events for networked objects.
/// </summary>
namespace GreedyVox.NetCode.Traits
{
    public class NetCodeRespawnerMonitor : NetworkBehaviour, INetworkRespawnerMonitor
    {
        private Respawner m_Respawner;
        /// <summary>
        /// Initializes default references.
        /// Caches the Respawner component on Awake.
        /// </summary>
        private void Awake() => m_Respawner = gameObject.GetCachedComponent<Respawner>();
        /// <summary>
        /// Performs a respawn locally and propagates the respawn across the network.
        /// </summary>
        /// <param name="position">The target respawn position.</param>
        /// <param name="rotation">The target respawn rotation.</param>
        /// <param name="state">Whether the position or rotation changed.</param>
        public void Respawn(Vector3 position, Quaternion rotation, bool state) =>
        RespawnRpc(position, rotation, state);
        /// <summary>
        /// RPC called on all clients except the owner to execute a respawn.
        /// Invoked reliably on remote clients to synchronize position and rotation.
        /// </summary>
        /// <param name="position">The target respawn position.</param>
        /// <param name="rotation">The target respawn rotation.</param>
        /// <param name="state">Whether the position or rotation changed.</param>
        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
        private void RespawnRpc(Vector3 position, Quaternion rotation, bool state) =>
        m_Respawner.Respawn(position, rotation, state);
        /// <summary>
        /// RPC called on the server by the owner to execute a respawn.
        /// This ensures the server authority applies the respawn and propagates it.
        /// </summary>
        /// <param name="position">The target respawn position.</param>
        /// <param name="rotation">The target respawn rotation.</param>
        /// <param name="state">Whether the position or rotation changed.</param>
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Reliable)]
        public void RespawnServerRpc(Vector3 position, Quaternion rotation, bool state) =>
        m_Respawner.Respawn(position, rotation, state);
    }
}
