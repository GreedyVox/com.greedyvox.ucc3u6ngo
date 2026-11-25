using Opsive.Shared.Game;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Game
{
    public class NetCodeSpawnManager : NetCodeSingletonAbstract<NetCodeSpawnManager>
    {
        public bool TryDespawner(GameObject prefab)
        {
            if (IsServer && prefab != null)
            {
                Despawner(prefab);
                return true;
            }
            return false;
        }
        public void Despawner(GameObject prefab)
        {
            var rem = prefab.GetComponentInParent<NetCodeRemover>();
            if (rem != null)
            {
                rem.Remove();
                return;
            }
            Remove(prefab);
        }
        /// <summary>
        /// Remove the object.
        /// </summary>
        private void Remove(GameObject go)
        {
            if (IsServer && go.TryGetComponent(out NetworkObject ngo))
                ngo.Despawn();
            else if (IsClient) Debug.Log($"Server will despawn the game object {go}");
            else Debug.LogError($"Error while trying to despawn {go}");
        }
        public bool TrySpawner(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (IsServer && prefab != null)
            {
                Spawner(prefab, pos, rot);
                return true;
            }
            return false;
        }
        public bool TrySpawner(GameObject prefab, Vector3 pos, Quaternion rot, out GameObject go)
        {
            if (IsServer && prefab != null)
            {
                go = Spawner(prefab, pos, rot);
                return go != null;
            }
            go = default;
            return false;
        }
        /// <summary>
        /// Spawner for creating or pooling a game object.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public GameObject Spawner(GameObject prefab, Vector3 pos, Quaternion rot = default)
        {
            if (!IsServer || prefab == null) return null;
            var go = ObjectPoolBase.Instantiate(prefab, pos, rot);
            if (NetCodeObjectPool.TryNetworkSpawn(prefab, go, true))
            {
                Debug.Log($"<color=white>Pooling GameObject: [<color=blue><b>{go?.name} | {pos}</b></color>]</color>");
                return go;
            }
            else if (go.TryGetComponent(out NetworkObject net))
            {
                net.Spawn();
                Debug.Log($"<color=white>Spawned GameObject: [<color=green><b>{go?.name} | {pos}</b></color>]</color>");
                return go;
            }
            return null;
        }
    }
}