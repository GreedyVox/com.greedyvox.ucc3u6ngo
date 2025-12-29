using Opsive.Shared.Game;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Game
{
    public class NetCodeSpawnInstance : INetworkPrefabInstanceHandler
    {
        private GameObject m_Prefab;
        private bool m_IsPooled = false;
        public NetCodeSpawnInstance(GameObject fab) { m_Prefab = fab; }
        public NetworkObject Instantiate(ulong ID, Vector3 pos, Quaternion rot)
        {
            var go = ObjectPoolBase.Instantiate(m_Prefab, pos, rot);
            m_IsPooled = ObjectPoolBase.InstantiatedWithPool(go);
            return go?.GetComponent<NetworkObject>();
        }
        public void Destroy(NetworkObject net)
        {
            var go = net?.gameObject;
            if (m_IsPooled) ObjectPoolBase.Destroy(go);
            else GameObject.Destroy(go);
        }
    }
}