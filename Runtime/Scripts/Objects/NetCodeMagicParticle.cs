using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Interfaces;
using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items.Actions;
using Opsive.UltimateCharacterController.Networking.Objects;
using Opsive.UltimateCharacterController.Objects.ItemAssist;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Objects
{
    /// <summary>
    /// Network-synchronized magic particle initializer.
    /// Handles payload generation, serialization, and application.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetCodeMagicParticle : MonoBehaviour, INetworkMagicObject, IPayloadEvent, IPayload
    {
        private MagicAction m_MagicAction;
        private GameObject m_Character;
        private int m_ActionIndex;
        private uint m_CastID;
        /// <summary>
        /// Stores local spawn data prior to network replication.
        /// </summary>
        /// <param name="character">Instantiating character.</param>
        /// <param name="magicAction">Magic action source.</param>
        /// <param name="actionIndex">Action index.</param>
        /// <param name="castID">Cast identifier.</param>
        public void Instantiate(GameObject character, MagicAction magicAction, int actionIndex, uint castID)
        {
            m_MagicAction = magicAction;
            m_ActionIndex = actionIndex;
            m_Character = character;
            m_CastID = castID;
        }
        /// <summary>
        /// Required interface hook. Not used for magic particles.
        /// </summary>
        public void Initialize(uint id, GameObject owner) { }
        /// <summary>
        /// Applies payload via interface dispatch.
        /// </summary>
        public bool Payload(INetworkSerializable data) => data is PayloadMagicParticle p && Payload(p);
        /// <summary>
        /// Produces payload as boxed network-serializable data.
        /// </summary>
        public bool Payload(out INetworkSerializable data)
        {
            data = Payload();
            return true;
        }
        /// <summary>
        /// Serializes payload data to a FastBufferWriter.
        /// Caller is responsible for disposing the writer.
        /// </summary>
        public bool Payload(ref int idx, out FastBufferWriter writer)
        {
            writer = new FastBufferWriter(MaxBufferSize(), Allocator.Temp);
            writer.WriteValueSafe(idx);
            writer.WriteValueSafe(transform.position);
            writer.WriteValueSafe(Payload());
            return true;
        }
        /// <summary>
        /// Builds the authoritative magic particle payload.
        /// </summary>
        public PayloadMagicParticle Payload()
        {
            return new PayloadMagicParticle
            {
                Owner = m_Character != null && m_Character.TryGetComponent(out NetworkObject net) ? net : default,
                SlotID = m_MagicAction.CharacterItem.SlotID,
                ActionID = m_MagicAction.ID,
                ActionIndex = m_ActionIndex,
                CastID = m_CastID
            };
        }
        /// <summary>
        /// Applies payload data and initializes the magic particle.
        /// </summary>
        public bool Payload(PayloadMagicParticle data)
        {
            if (!data.Owner.TryGet(out NetworkObject net)) return false;
            var inventory = net.gameObject.GetCachedComponent<Inventory>();
            if (inventory == null) return false;
            var item = inventory.GetActiveCharacterItem(data.SlotID);
            if (item == null) return false;
            if (item.GetItemAction(data.ActionID) is not MagicAction magicAction)
                return false;
            var particle = gameObject.GetCachedComponent<MagicParticle>();
            particle?.Initialize(magicAction, data.CastID);
            return true;
        }
        /// <summary>
        /// Deserializes payload data and applies it.
        /// </summary>
        public void Payload(in FastBufferReader reader)
        {
            reader.ReadValueSafe(out PayloadMagicParticle data);
            Payload(data);
        }
        /// <summary>
        /// Returns required buffer size for payload serialization.
        /// </summary>
        public int MaxBufferSize()
        {
            return
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<Vector3>() +
                FastBufferWriter.GetWriteSize<NetworkObjectReference>() +
                FastBufferWriter.GetWriteSize<uint>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<int>() +
                FastBufferWriter.GetWriteSize<int>();
        }
    }
}
