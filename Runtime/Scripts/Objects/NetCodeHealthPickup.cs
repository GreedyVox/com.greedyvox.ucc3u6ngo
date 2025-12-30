using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Opsive.UltimateCharacterController.Traits.Damage;
using UnityEngine;

/// <summary>
/// Initializes the item pickup over the network.
/// </summary>
namespace GreedyVox.NetCode.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetCodeInfo))]
    public class NetCodeHealthPickup : ObjectPickup
    {
        [Tooltip("The amount of health to replenish.")]
        [SerializeField] private float m_HealthAmount = 40;
        [Tooltip("Should the object be picked up even if the object has full health?")]
        [SerializeField] private bool m_AlwaysPickup;
        public float HealthAmount { get { return m_HealthAmount; } set { m_HealthAmount = value; } }
        public bool AlwaysPickup { get { return m_AlwaysPickup; } set { m_AlwaysPickup = value; } }
        /// <summary>
        /// A GameObject has entered the trigger.
        /// </summary>
        /// <param name="other">The GameObject that entered the trigger.</param>
        public override void TriggerEnter(GameObject other) => DoPickup(other);
        /// <summary>
        /// Picks up the object.
        /// </summary>
        /// <param name="target">The object doing the pickup.</param>
        public override void DoPickup(GameObject target)
        {
            var damage = target.GetCachedParentComponent<IDamageTarget>();
            if ((damage != null && damage.IsAlive()
            && damage.Heal(m_HealthAmount)) || m_AlwaysPickup)
                ObjectPickedUp(damage.Owner);
        }
    }
}