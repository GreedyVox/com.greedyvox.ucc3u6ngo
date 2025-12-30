using GreedyVox.NetCode.Game;
using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Networking.Game;
using Opsive.UltimateCharacterController.Objects;
using Opsive.UltimateCharacterController.Traits;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Traits
{
    /// <summary>
    /// Synchronizes the Health component over the network.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetCodeHealthMonitor : NetCodeHealthAbstract
    {
        public override void Die(Vector3 position, Vector3 force, GameObject attacker)
        {
            base.Die(position, force, attacker);
            if (IsServer)
            {
                SpawningObjectsOnDeath(position, force);
                if (TryGetComponent(out Respawner com)
                && (com.ScheduleRespawnOnDeath || com.ScheduleRespawnOnDisable))
                    return;
                if (TryGetComponent(out NetCodeDespawner net))
                    net.Despawn();
                else if (m_NetCodeObject == null)
                    ObjectPoolBase.Destroy(m_GamingObject);
                else NetCodeObjectPool.Destroy(m_GamingObject);
            }
        }
        /// <summary>
        /// Spawn objects on death over the network.
        /// <param name="position">The position of the damage.</param>
        /// <param name="direction">The direction that the object took damage from.</param>
        /// </summary>
        protected virtual void SpawningObjectsOnDeath(Vector3 position, Vector3 force)
        {
            // Spawn any objects on death, such as an explosion if the object is an explosive barrel.
            if (SpawnObjectsOnDeath != null)
            {
                GameObject go;
                for (int n = 0; n < SpawnObjectsOnDeath.Count; n++)
                {
                    var spawn = SpawnObjectsOnDeath[n];
                    if (spawn == null) continue;
                    var obj = spawn.gameObject;
                    if (ObjectPoolBase.IsPooledObject(obj))
                    {
                        go = ObjectPoolBase.Instantiate(obj, transform.position, transform.rotation);
                        NetworkObjectPool.NetworkSpawn(obj, go, true);
                    }
                    else
                    {
                        go = Instantiate(obj, transform.position, transform.rotation);
                        if (go.TryGetComponent(out NetworkObject net)) net.Spawn();
                    }
                    if (go.TryGetComponent(out Explosion exp))
                        exp.Explode(gameObject);
                    var rigs = go.GetComponentsInChildren<Rigidbody>();
                    for (int i = 0; i < rigs.Length; i++)
                        rigs[i].AddForceAtPosition(force, position);
                }
            }
        }
    }
}