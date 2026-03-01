using Opsive.Shared.Game;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Game
{
    /// <summary>
    /// Netcode-safe prefab instance handler supporting pooled and non-pooled objects.
    /// </summary>
    public sealed class NetCodeSpawnInstance : INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// The prefab used to instantiate networked objects.
        /// </summary>
        private readonly GameObject m_Prefab;
        /// <summary>
        /// Creates a new prefab instance handler for the given prefab.
        /// </summary>
        /// <param name="prefab">The prefab to spawn.</param>
        public NetCodeSpawnInstance(GameObject prefab) => m_Prefab = prefab;
        /// <summary>
        /// Instantiates a network prefab instance at the given position and rotation.
        /// Uses pooling if available.
        /// </summary>
        /// <param name="id">The client ID requesting the instantiation.</param>
        /// <param name="pos">The world position to spawn the object.</param>
        /// <param name="rot">The world rotation to spawn the object.</param>
        /// <returns>The spawned <see cref="NetworkObject"/> instance.</returns>
        public NetworkObject Instantiate(ulong id, Vector3 pos, Quaternion rot)
        {
            var go = ObjectPoolBase.Instantiate(m_Prefab, pos, rot);
            return go.GetComponent<NetworkObject>();
        }
        /// <summary>
        /// Safely destroys a network object.
        /// The actual destruction to finish despawning.
        /// </summary>
        /// <param name="ngo">The network object to destroy.</param>
        public void Destroy(NetworkObject ngo)
        {
            if (ngo == null) return;
            ObjectPoolBase.Destroy(ngo.gameObject);
        }
    }
}
