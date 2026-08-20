/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace GreedyVox.NetCode
{
    using System.Collections.Generic;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.Shared.StateSystem;
    using Unity.Netcode;
    using UnityEngine;

    /// <summary>
    /// Synchronizes Opsive character states between the server and connected clients.
    /// </summary>
    /// <remarks>
    /// The server stores the active states for each networked character and forwards
    /// state changes to connected clients. When a new client joins, the server sends
    /// the currently active states only to that joining client.
    ///
    /// <see cref="StateManager.SendStateChangeEvent"/> must be enabled for state-change
    /// events to be raised.
    /// </remarks>
    public class NetCodeStateManager : NetworkBehaviour
    {
        /// <summary>
        /// Contains the active state names for each registered networked character.
        /// This collection is authoritative on the server.
        /// </summary>
        private readonly Dictionary<NetworkObjectReference, HashSet<string>> m_ActiveCharacterStates = new();
        /// <summary>
        /// Specifies whether the Opsive events are currently registered.
        /// </summary>
        private bool m_EventsRegistered;
        /// <summary>
        /// Specifies whether this behaviour is inside its spawned network lifetime.
        /// </summary>
        private bool m_NetworkReady;
        /// <summary>
        /// Registers the Opsive events when the component is enabled after its
        /// associated <see cref="NetworkObject"/> has already spawned.
        /// </summary>
        private void OnEnable()
        {
            if (IsSpawned) RegisterEvents();
        }
        /// <summary>
        /// Unregisters the Opsive events when the component is disabled.
        /// </summary>
        private void OnDisable() => UnregisterEvents();
        /// <summary>
        /// Initializes the state synchronization system when this behaviour is spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_NetworkReady = true;
            if (isActiveAndEnabled) RegisterEvents();
            var stateManager = FindFirstObjectByType<StateManager>();
            if (stateManager != null)
                stateManager.SendStateChangeEvent = true;
        }
        /// <summary>
        /// Stops state synchronization before this behaviour is despawned.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            // Set this before any further cleanup so callbacks cannot send an RPC
            // while the NetworkBehaviour is being removed from the network session.
            m_NetworkReady = false;
            UnregisterEvents();
            m_ActiveCharacterStates.Clear();
            base.OnNetworkDespawn();
        }
        /// <summary>
        /// Registers the events used to receive state and player lifecycle notifications.
        /// </summary>
        private void RegisterEvents()
        {
            if (m_EventsRegistered) return;
            EventHandler.RegisterEvent<GameObject, string, bool>("OnStateChange", OnStateChange);
            EventHandler.RegisterEvent<ulong, NetworkObjectReference>("OnPlayerConnected", OnPlayerConnected);
            EventHandler.RegisterEvent<ulong, NetworkObjectReference>("OnPlayerDisconnected", OnPlayerDisconnected);
            m_EventsRegistered = true;
        }
        /// <summary>
        /// Unregisters the events used by the state synchronization system.
        /// </summary>
        private void UnregisterEvents()
        {
            if (!m_EventsRegistered) return;
            EventHandler.UnregisterEvent<GameObject, string, bool>("OnStateChange", OnStateChange);
            EventHandler.UnregisterEvent<ulong, NetworkObjectReference>("OnPlayerConnected", OnPlayerConnected);
            EventHandler.UnregisterEvent<ulong, NetworkObjectReference>("OnPlayerDisconnected", OnPlayerDisconnected);
            m_EventsRegistered = false;
        }
        /// <summary>
        /// Determines whether this instance can currently send state RPCs.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when this behaviour is spawned, running on the
        /// server, and attached to an active network session; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        private bool CanSendStateRpc()
        {
            // IsSpawned is intentionally checked before accessing the NetworkManager.
            // It verifies this specific NetworkBehaviour, not only its NetworkObject.
            return m_NetworkReady &&
                   IsSpawned &&
                   IsServer &&
                   NetworkManager != null &&
                   NetworkManager.IsListening;
        }
        /// <summary>
        /// Removes the disconnected player's stored state information.
        /// </summary>
        /// <param name="clientId">
        /// The network client identifier of the disconnected player.
        /// </param>
        /// <param name="playerReference">
        /// A reference to the disconnected player's <see cref="NetworkObject"/>.
        /// </param>
        private void OnPlayerDisconnected(ulong clientId, NetworkObjectReference playerReference)
        {
            if (!CanSendStateRpc()) return;
            // Remove the reference directly. The object may already be despawned,
            // in which case TryGet would fail even though the dictionary entry exists.
            m_ActiveCharacterStates.Remove(playerReference);
        }
        /// <summary>
        /// Registers the connected player and synchronizes the current active states
        /// with that joining client.
        /// </summary>
        /// <param name="clientId">
        /// The network client identifier of the connected player.
        /// </param>
        /// <param name="playerReference">
        /// A reference to the connected player's <see cref="NetworkObject"/>.
        /// </param>
        private void OnPlayerConnected(ulong clientId, NetworkObjectReference playerReference)
        {
            if (!CanSendStateRpc()) return;
            // Register the joining character without throwing if the event is raised
            // more than once for the same NetworkObject.
            if (!m_ActiveCharacterStates.ContainsKey(playerReference))
                m_ActiveCharacterStates.Add(playerReference, new HashSet<string>(System.StringComparer.Ordinal));
            // A host already has the server's local state, so no replay RPC is needed.
            if (clientId == Unity.Netcode.NetworkManager.ServerClientId) return;
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId)) return;
            ReplayActiveStates(clientId);
        }
        /// <summary>
        /// Sends all currently active character states to one joining client.
        /// </summary>
        /// <param name="clientId">
        /// The client that should receive the active-state replay.
        /// </param>
        private void ReplayActiveStates(ulong clientId)
        {
            if (!CanSendStateRpc()) return;
            var rpcTarget = RpcTarget.Single(clientId, RpcTargetUse.Temp);
            foreach (var characterStates in m_ActiveCharacterStates)
                foreach (var stateName in characterStates.Value)
                    StateEventRpc(characterStates.Key, stateName, true, rpcTarget);
        }
        /// <summary>
        /// Handles an Opsive state change and forwards the change to remote clients.
        /// </summary>
        /// <param name="character">
        /// The character whose state changed.
        /// </param>
        /// <param name="stateName">
        /// The name of the changed state.
        /// </param>
        /// <param name="active">
        /// <see langword="true"/> when the state became active; otherwise,
        /// <see langword="false"/>.
        /// </param>
        private void OnStateChange(GameObject character, string stateName, bool active)
        {
            if (!CanSendStateRpc() || character == null || string.IsNullOrEmpty(stateName)) return;
            var characterNetworkObject = character.GetCachedComponent<NetworkObject>();
            if (characterNetworkObject == null || !characterNetworkObject.IsSpawned) return;
            var characterReference = new NetworkObjectReference(characterNetworkObject);
            // Register characters lazily so a missed or reordered player-connected
            // event does not permanently prevent state synchronization.
            if (!m_ActiveCharacterStates.TryGetValue(characterReference, out var activeStates))
            {
                activeStates = new HashSet<string>(System.StringComparer.Ordinal);
                m_ActiveCharacterStates.Add(characterReference, activeStates);
            }
            if (active)
                activeStates.Add(stateName);
            else
                activeStates.Remove(stateName);
            StateEventRpc(characterReference, stateName, active);
        }
        /// <summary>
        /// Applies a synchronized state change on the receiving client.
        /// </summary>
        /// <param name="characterReference">
        /// A reference to the character whose state should be changed.
        /// </param>
        /// <param name="stateName">
        /// The name of the state to change.
        /// </param>
        /// <param name="active">
        /// <see langword="true"/> to activate the state; otherwise,
        /// <see langword="false"/>.
        /// </param>
        /// <param name="rpcParams">
        /// RPC parameters used by NGO to optionally override the destination client.
        /// This parameter is consumed by NGO's generated RPC code.
        /// </param>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void StateEventRpc(NetworkObjectReference characterReference, string stateName, bool active, RpcParams rpcParams = default)
        {
            // Do not put an IsServer check here. This method body executes on
            // receiving clients, where IsServer is normally false.
            if (!characterReference.TryGet(out var characterNetworkObject) ||
                characterNetworkObject == null || !characterNetworkObject.IsSpawned) return;
            StateManager.SetState(characterNetworkObject.gameObject, stateName, active);
        }
    }
}