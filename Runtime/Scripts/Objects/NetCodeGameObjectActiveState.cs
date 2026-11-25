using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Objects
{
    /// <summary>
    /// Synchronizes the activeSelf state of a target GameObject through
    /// the NetworkObject located on a parent GameObject.
    /// </summary>
    public class NetCodeGameObjectActiveState : NetworkBehaviour
    {
        [Tooltip("The GameObject whose activeSelf state should be synchronized.")]
        [SerializeField] protected GameObject m_Target;
        [SerializeField] protected bool m_InitializeState = true;
        [Tooltip("Log active-state changes to the Unity Console.")]
        [SerializeField] protected bool m_IsDebugLogging;
        /// <summary>
        /// The server-authoritative active state synchronized with every client.
        /// </summary>
        protected NetworkVariable<bool> m_ActiveState = new(false);
        /// <summary>
        /// The GameObject controlled by this component.
        /// </summary>
        public GameObject Target => m_Target;
        /// <summary>
        /// The currently synchronized active state.
        /// </summary>
        public bool ActiveState => m_ActiveState.Value;
        /// <summary>z
        /// Set target defaults
        /// </summary>
        protected void Awake() => m_Target = m_Target == null ? gameObject : m_Target;
        /// <summary>
        /// Removes the active-state callback when the parent NetworkObject
        /// is despawned.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            m_ActiveState.OnValueChanged -= ActiveStateChanged;
            base.OnNetworkDespawn();
        }
        /// <summary>
        /// Initializes the synchronized state after the parent NetworkObject
        /// has been spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_ActiveState.OnValueChanged += ActiveStateChanged;
            // The server establishes the initial authoritative state.
            if (IsServer)
            {
                m_Target.SetActive(m_InitializeState);
                m_ActiveState.Value = m_Target.activeSelf;
            }
            // Apply the initial value because OnValueChanged is not invoked
            // when a client receives its initial synchronization.
            ApplyActiveState(m_ActiveState.Value);
        }
        /// <summary>
        /// Sets the server-authoritative active state.
        /// </summary>
        /// <param name="active">The active state to assign to the target.</param>
        /// <returns>True if the state was accepted; otherwise false.</returns>
        public virtual bool SetActive(bool active)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[{nameof(NetCodeGameObjectActiveState)}] Only the server or host can change the active state.", gameObject);
                return false;
            }
            if (m_ActiveState.Value != active) m_ActiveState.Value = active;
            // Correct the server locally even when the value already matched.
            ApplyActiveState(active);
            return true;
        }
        /// <summary>
        /// Applies a synchronized active-state change.
        /// </summary>
        /// <param name="previous">The previous active state.</param>
        /// <param name="current">The current active state.</param>
        protected virtual void ActiveStateChanged(bool previous, bool current) => ApplyActiveState(current);
        /// <summary>
        /// Applies the synchronized state to the target GameObject.
        /// </summary>
        /// <param name="active">The active state to apply.</param>
        protected virtual void ApplyActiveState(bool active)
        {
            if (m_Target.activeSelf != active)
                m_Target.SetActive(active);
            if (m_IsDebugLogging)
                Debug.LogFormat("[{0}] Controller: {1} | Target: {2} | Active: [{3}]",
                nameof(NetCodeGameObjectActiveState), gameObject.name, m_Target.name, active);
        }
    }
}