using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Tests
{
    /// <summary>
    /// Simple server-authoritative spawn test utility.
    /// Listens for key input and spawns networked prefabs
    /// at the center-screen raycast hit location.
    /// 
    /// Intended for debugging and rapid iteration.
    /// </summary>
    public sealed class SpawnManagerTest : NetworkBehaviour
    {
        /// <summary>
        /// Per-spawn configuration describing how and when
        /// a prefab should be spawned.
        /// </summary>
        [Serializable]
        public sealed class SpawnSettings
        {
            [Tooltip("Prefab to spawn (must contain NetworkObject).")]
            public GameObject m_VehiclePrefab;
            [Tooltip("Layer mask used for raycast hit detection.")]
            public LayerMask m_LayerMask;
            [Tooltip("Key that triggers this spawn.")]
            public KeyCode m_KeyCode = KeyCode.Alpha1;
            [Tooltip("Maximum raycast distance.")]
            public float m_RayLength = 10.0f;
        }
        [Tooltip("Spawn configurations evaluated every frame.")]
        [SerializeField] private SpawnSettings[] m_SpawnSettings;
        private Coroutine m_Coroutine;
        private Camera m_Camera;
        /// <summary>
        /// Cache camera reference.
        /// </summary>
        private void Awake() => m_Camera = Camera.main;
        /// <summary>
        /// Start polling for input on the server.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                m_Coroutine ??= StartCoroutine(Updating());
        }
        /// <summary>
        /// Stop polling for input when despawned.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);
        }
        /// <summary>
        /// Polls input on the server and triggers spawns
        /// based on configured key bindings.
        /// </summary>
        private IEnumerator Updating()
        {
            while (isActiveAndEnabled)
            {
                yield return null;
                for (int i = 0; i < m_SpawnSettings.Length; i++)
                {
                    var settings = m_SpawnSettings[i];
                    if (Input.GetKeyUp(settings.m_KeyCode))
                        TrySpawn(settings);
                }
            }
            m_Coroutine = null;
        }
        /// <summary>
        /// Performs a center-screen raycast and spawns the configured
        /// prefab at the hit location if valid.
        /// </summary>
        /// <param name="settings">Spawn configuration.</param>
        private void TrySpawn(in SpawnSettings settings)
        {
            if (m_Camera == null || settings.m_VehiclePrefab == null)
                return;
            var ray = m_Camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            Debug.DrawRay(ray.origin, ray.direction * settings.m_RayLength, Color.cyan, 2f);
            if (!Physics.Raycast(ray, out var hit, settings.m_RayLength, settings.m_LayerMask))
                return;
            var position = hit.point + Vector3.up * 0.5f;
            var instance = Instantiate(settings.m_VehiclePrefab, position, Quaternion.identity);
            if (instance.TryGetComponent(out NetworkObject net)) net.Spawn(true);
            else Destroy(instance); // safety: never allow non-networked spawn
            Debug.Log($"Spawned: {instance.name} @ {position}", instance);
        }
    }
}
