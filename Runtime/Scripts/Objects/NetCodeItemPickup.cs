using System;
using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Interfaces;
using Opsive.Shared.Game;
using Opsive.Shared.Utility;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Objects;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Initializes the item pickup over the network.
/// </summary>
namespace GreedyVox.NetCode.Objects
{
    public class NetCodeItemPickup : ItemPickup, IPayload
    {
        private TrajectoryObject m_TrajectoryObject;
        private PayloadItemPickup m_Data;
        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            m_TrajectoryObject = gameObject.GetCachedComponent<TrajectoryObject>();
            base.Awake();
        }
        /// <summary>
        /// Initializes the object. This will be called from an object creating the projectile (such as a weapon).
        /// </summary>
        /// <param name="id">The id used to differentiate this projectile from others.</param>
        /// <param name="owner">The object that instantiated the trajectory object.</param>
        public void Initialize(uint id, GameObject own) { }
        /// <summary>
        /// The cloned object. This will be called from the object that was spawned.
        /// </summary>
        /// <param name="go">The object that instantiated.</param>
        public void Clone(GameObject go)
        {
            m_Data = new PayloadItemPickup()
            {
                Position = go.transform.position,
                Rotation = go.transform.rotation,
                ItemCount = m_ItemDefinitionAmounts.Length,
                ItemID = GetArrayDataIDs(m_ItemDefinitionAmounts),
                ItemAmounts = GetArrayDataAmounts(m_ItemDefinitionAmounts),
                Velocity = m_TrajectoryObject == null ? Vector3.zero : m_TrajectoryObject.Velocity,
                Torque = m_TrajectoryObject == null ? Vector3.zero : m_TrajectoryObject.Torque,
            };
        }
        /// <summary>
        /// Extracts the <c>Amount</c> values from an array of <see cref="ItemIdentifierAmount"/> objects
        /// and returns them as an array of integers.
        /// </summary>
        /// <param name="items">An array of <see cref="ItemIdentifierAmount"/> objects to extract amounts from.</param>
        /// <returns>An array of integers representing the <c>Amount</c> of each item in the input array.</returns>
        private int[] GetArrayDataAmounts(ItemIdentifierAmount[] items)
        {
            var dat = new int[items.Length];
            for (var i = 0; i < dat.Length; i++)
                dat[i] = items[i].Amount;
            return dat;
        }
        /// <summary>
        /// Retrieves an array of item type IDs from the provided array of <see cref="ItemIdentifierAmount"/>.
        /// Each ID corresponds to the <see cref="ItemType.ID"/> of the <see cref="ItemIdentifier"/> in the input array.
        /// </summary>
        /// <param name="items">An array of <see cref="ItemIdentifierAmount"/> objects to extract IDs from.</param>
        /// <returns>An array of <see cref="uint"/> containing the IDs of the item types.</returns>
        private uint[] GetArrayDataIDs(ItemIdentifierAmount[] items)
        {
            var dat = new uint[items.Length];
            for (var i = 0; i < dat.Length; i++)
                dat[i] = (items[i].ItemIdentifier as ItemType).ID;
            return dat;
        }
        /// <summary>
        /// Returns the maximus size for the fast buffer writer
        /// </summary>               
        public int MaxBufferSize()
        {
            return
            FastBufferWriter.GetWriteSize<int>() +
            FastBufferWriter.GetWriteSize(m_Data.Position) +
            FastBufferWriter.GetWriteSize(m_Data.Rotation) +
            FastBufferWriter.GetWriteSize(m_Data.ItemCount) +
            FastBufferWriter.GetWriteSize(m_Data.Torque) +
            FastBufferWriter.GetWriteSize(m_Data.Velocity) +
            FastBufferWriter.GetWriteSize(m_Data.ItemID ?? Array.Empty<uint>()) +
            FastBufferWriter.GetWriteSize(m_Data.ItemAmounts ?? Array.Empty<int>());
        }
        /// <summary>
        /// The object has been spawned, write the payload data.
        /// </summary>
        public bool PayLoad(ref int idx, out FastBufferWriter writer)
        {
            try
            {
                using (writer = new FastBufferWriter(MaxBufferSize(), Allocator.Temp))
                {
                    writer.WriteValueSafe(idx);
                    writer.WriteValueSafe(m_Data);
                }
                return true;
            }
            catch (Exception e)
            {
                NetworkLog.LogErrorServer(e.Message);
                return false;
            }
        }
        /// <summary>
        /// The object has been spawned, read the payload data.
        /// </summary>
        public void PayLoad(in FastBufferReader reader, GameObject go = default)
        {
            reader.ReadValueSafe(out m_Data);
            transform.position = m_Data.Position;
            transform.rotation = m_Data.Rotation;
            for (int i = 0; i < m_ItemDefinitionAmounts.Length; i++)
                GenericObjectPool.Return(m_ItemDefinitionAmounts[i]);
            // Setup the item counts.
            var length = m_Data.ItemCount;
            if (m_ItemDefinitionAmounts.Length != length)
                m_ItemDefinitionAmounts = new ItemIdentifierAmount[length];
            for (int n = 0; n < length; n++)
                m_ItemDefinitionAmounts[n] = new ItemIdentifierAmount(ItemIdentifierTracker.GetItemIdentifier(
                    m_Data.ItemID[n]).GetItemDefinition(), m_Data.ItemAmounts[n]);
            Initialize(true);
            // Setup the trajectory object.
            if (m_TrajectoryObject != null)
            {
                var velocity = m_Data.Velocity;
                var torque = m_Data.Torque;
                m_TrajectoryObject.Initialize(velocity, torque, go);
            }
        }
    }
}