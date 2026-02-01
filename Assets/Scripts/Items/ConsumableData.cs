using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// ScriptableObject for consumable items (health potions, mana potions, etc.).
    /// Defines instant effects when consumed.
    /// Create via: Assets > Create > Magikill > Items > Consumable
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Magikill/Items/Consumable", order = 2)]
    public class ConsumableData : ItemData
    {
        [Header("Consumable Effects")]
        [Tooltip("Restores health when consumed")]
        public float healthRestore = 0f;

        [Tooltip("Restores mana when consumed")]
        public float manaRestore = 0f;

        [Tooltip("Cooldown in seconds before another consumable can be used")]
        public float cooldown = 1.0f;

        [Header("Usage")]
        [Tooltip("Can this consumable be used in combat?")]
        public bool usableInCombat = true;

        /// <summary>
        /// Validates that this consumable has at least one effect
        /// </summary>
        public bool HasEffect()
        {
            return healthRestore > 0 || manaRestore > 0;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Ensure itemType is set to Consumable
            itemType = ItemType.Consumable;

            // Consumables should be stackable
            isStackable = true;
            if (maxStackSize < 1)
            {
                maxStackSize = 99;
            }

            // Warn if consumable has no effects
            if (!HasEffect())
            {
                Debug.LogWarning($"[ConsumableData] {itemName} has no effects (healthRestore and manaRestore are both 0)");
            }
        }
    }
}
