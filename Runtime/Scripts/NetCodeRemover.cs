using GreedyVox.NetCode.Game;
using Opsive.Shared.Game;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode
{
    public class NetCodeRemover : NetworkBehaviour
    {
        [Tooltip("The number of seconds until the object should be placed back in the pool.")]
        [SerializeField] protected float m_Lifetime = 5;
        private NetworkObject m_NetCodeObject;
        private ScheduledEventBase m_RemoveEvent;
        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake() => m_NetCodeObject = GetComponent<NetworkObject>();
        /// <summary>
        /// Schedule the object for removal.
        /// </summary>
        private void OnEnable()
        {
            m_RemoveEvent = Scheduler.Schedule(m_Lifetime, Remove);
            Debug.Log($"<color=blue>Removing <color=white><b>{transform.name}</b></color> after {m_Lifetime} seconds</color>");
        }
        /// <summary>
        /// The object has been destroyed - no need for removal if it hasn't already been removed.
        /// </summary>
        private void OnDisable() => CancelRemoveEvent();
        public void OnDeath(Vector3 position, Vector3 force, GameObject attacker) =>
        m_RemoveEvent = Scheduler.Schedule(m_Lifetime, Remove);
        /// <summary>
        /// Cancels the remove event.
        /// </summary>
        public void CancelRemoveEvent()
        {
            if (m_RemoveEvent != null)
            {
                Scheduler.Cancel(m_RemoveEvent);
                m_RemoveEvent = null;
            }
        }
        /// <summary>
        /// Force remove the object.
        /// </summary>
        public void Remove(bool force = true)
        {
            if (!force) return;
            CancelRemoveEvent();
            Remove();
        }
        /// <summary>
        /// Remove the object.
        /// </summary>
        private void Remove()
        {
            if (ObjectPoolBase.IsPooledObject(gameObject))
            {
                if (m_NetCodeObject == null)
                    ObjectPoolBase.Destroy(gameObject);
                else if (IsServer)
                    NetCodeObjectPool.Destroy(gameObject);
            }
            else if (m_NetCodeObject == null) Destroy(gameObject);
            else if (IsServer) m_NetCodeObject.Despawn();
            else if (IsClient) Debug.Log($"Server will destory the game object {gameObject}");
            else Debug.LogError($"Error occurred destroying the game object {gameObject}");
        }
    }
}