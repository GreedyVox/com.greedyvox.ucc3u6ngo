using System;
using System.Collections.Generic;
using GreedyVox.NetCode.Interfaces;
using GreedyVox.NetCode.Utilities;
using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Networking.Game;
using Unity.Netcode;
using UnityEngine;
using static Opsive.Shared.Game.ObjectPoolBase;

namespace GreedyVox.NetCode.Game
{
    /// <summary>
    /// Manages synchronization of pooled objects over the network.
    /// </summary>
    public class NetCodeObjectPool : NetworkObjectPool
    {
        [Tooltip("An array of objects that can be spawned over the network. These objects will require manually custom pooling.")]
        [SerializeField] protected ObjectPoolDataAbstract[] m_InjectObjectPoolData;
        [SerializeField] protected bool m_IsDebugLogging = false;
        protected HashSet<GameObject> m_SpawnableGameObjects = new();
        protected HashSet<GameObject> m_SpawnedGameObjects = new();
        protected HashSet<GameObject> m_ActiveGameObjects = new();
        /// <summary>
        /// Initializes the default values.
        /// </summary>
        protected virtual void Start()
        {
            SetupSpawnManager(FindFirstObjectByType<ObjectPool>()?.PreloadedPrefabs);
            foreach (var pool in m_InjectObjectPoolData)
                pool.InjectGameObject(m_SpawnableGameObjects);
        }
        /// <summary>
        /// Destroys the object.
        /// </summary>
        /// <param name="go">The object that should be destroyed.</param>
        public new void Destroy(GameObject go)
        {
            m_ActiveGameObjects.Remove(go);
            if (InstantiatedWithPool(go))
                DestroyInternal(go);
            else GameObject.Destroy(go);
            if (m_IsDebugLogging)
                Debug.Log($"<color=blue>{this} Destroy <color=white>{go}</color></color>");
        }
        /// <summary>
        /// Injects a GameObject into the pool manager for networked spawning.
        /// </summary>
        /// <param name="go">The original GameObject to be injected into the pool manager.</param>
        /// <param name="pool">Specifies whether to use the pool manager for this object.</param>
        public virtual void SetupSpawnManager(GameObject go, bool pool = true)
        {
            if (ComponentUtility.HasComponent<NetworkObject>(go))
                InjectSpawnManager(new NetCodeSpawnInstance(go), go);
        }
        /// <summary>
        /// Injects multiple GameObjects into the pool manager for networked spawning.
        /// </summary>
        /// <param name="list">The array of prefabs to be injected into the pool manager.</param>
        /// <param name="pool">Specifies whether to use the pool manager for these objects.</param>
        public virtual void SetupSpawnManager(PreloadedPrefab[] list, bool pool = true)
        {
            if (list == null) return;
            foreach (var obj in list)
                SetupSpawnManager(obj.Prefab, pool);
        }
        /// <summary>
        /// Injects a GameObject into the pool manager for networked spawning.
        /// </summary>
        /// <param name="go">The original GameObject to be injected into the pool manager.</param>
        /// <param name="inject">The handler responsible for managing the instantiation and handling of networked prefabs.</param>
        /// <param name="pool">Specifies whether to use the pool manager for this object.</param>
        public virtual void InjectSpawnManager(INetworkPrefabInstanceHandler inject, GameObject go)
        {
            m_SpawnableGameObjects.Add(go);
            NetworkManager.Singleton.PrefabHandler.AddHandler(go, inject);
        }
        /// <summary>
        /// Spawns an object over the network without instantiating a new object on the local client.
        /// </summary>
        /// <param name="original">The original object the instance was created from.</param>
        /// <param name="instance">The instance object created from the original object.</param>
        /// <param name="sceneObject">Indicates if the object is owned by the scene. If false, it will be owned by the character.</param>
        protected override void NetworkSpawnInternal(GameObject original, GameObject instance, bool scene)
        {
            if (m_SpawnableGameObjects.Contains(original))
            {
                if (!m_ActiveGameObjects.Contains(instance))
                    m_ActiveGameObjects.Add(instance);
                if (TryNetworkSpawnInternal(original, instance, scene))
                {
                    if (!m_SpawnedGameObjects.Contains(instance))
                        m_SpawnedGameObjects.Add(instance);
                    return;
                }
            }
            Debug.LogError($"Error: Unable to spawn {original.name} on the network. Ensure the object has been added to the NetworkObjectPool.");
        }
        /// <summary>
        /// Try an object over the network without instantiating a new object on the local client.
        /// </summary>
        /// <param name="original">The original object the instance was created from.</param>
        /// <param name="instance">The instance object created from the original object.</param>
        /// <param name="sceneObject">Indicates if the object is owned by the scene. If false, it will be owned by the character.</param>
        /// <returns></returns>
        protected virtual bool TryNetworkSpawnInternal(GameObject original, GameObject instance, bool scene)
        {
            if (NetworkManager.Singleton.IsServer
            && instance.TryGetComponent(out NetworkObject ngo))
            {
                ngo.enabled = true; ngo.Spawn(scene);
                return true;
            }
            if (NetworkManager.Singleton.IsClient
            && ComponentUtility.TryGet<IPayload>(instance, out var dat))
            {
                NetCodeMessenger.Instance.ClientSpawnObject(original, instance, dat);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Destroys an object instance on the network.
        /// </summary>
        /// <param name="obj">The object to be destroyed.</param>
        protected override void DestroyInternal(GameObject go)
        {
            try
            {
                if (TryDestroyInternal(go)) return;
                // Only pool the object once despawned
                ObjectPoolBase.Destroy(go);
            }
            catch (Exception e)
            {
                Debug.Log($"{e.Message}\n{e.StackTrace}");
                GameObject.Destroy(go);
            }
        }
        /// <summary>
        /// Despawns an object instance over the network.
        /// </summary>
        /// <param name="ngo">The object to be despawn.</param>
        protected virtual bool TryDestroyInternal(GameObject go)
        {
            var ngo = go.GetCachedComponent<NetworkObject>();
            if (ngo?.IsSpawned == true)
            {
                if (m_IsDebugLogging)
                    Debug.Log($"<color=blue>{this} TryDestroyInternal [<color=white>{go} | {NetworkManager.Singleton.IsServer}</color>]</color>");
                if (NetworkManager.Singleton.IsServer)
                    ngo.Despawn();
                else NetCodeMessenger.Instance.ClientDespawnObject(ngo.NetworkObjectId);
                m_SpawnedGameObjects.Remove(go);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Determines if the specified object was spawned using the network object pool.
        /// </summary>
        /// <param name="obj">The object instance to check.</param>
        /// <returns>True if the object was spawned using the network object pool, otherwise false.</returns>
        protected override bool SpawnedWithPoolInternal(GameObject obj) => m_SpawnedGameObjects.Contains(obj);
    }
}
