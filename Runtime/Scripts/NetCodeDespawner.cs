using GreedyVox.NetCode.Game;
using Opsive.Shared.Game;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode
{
    public class NetCodeDespawner : NetworkBehaviour
    {
        [Tooltip("The number of seconds until the object should be desapwned.")]
        [SerializeField] private float m_DespawnTimer = 5;
        private ScheduledEventBase m_RemoveEvent;
        private NetworkObject m_NetCodeObject;
        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake() =>
        m_NetCodeObject = GetComponent<NetworkObject>();
        /// <summary>
        /// The object has died.
        /// </summary>
        /// <param name="position">The position of the force.</param>
        /// <param name="force">The amount of force which killed the object.</param>
        /// <param name="attacker">The GameObject that killed the object.</param>
        public void Despawn() =>
        m_RemoveEvent = SchedulerBase.Schedule(m_DespawnTimer, Remove);
        /// <summary>
        /// Cancels the remove event.
        /// </summary>
        public void CancelRemoveEvent()
        {
            if (m_RemoveEvent != null)
            {
                SchedulerBase.Cancel(m_RemoveEvent);
                m_RemoveEvent = null;
            }
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
            else Debug.LogError($"Error occurred destroying the game object {gameObject}");
        }
    }
}