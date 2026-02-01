using System;
using UnityEngine;
using Magikill.Items;

namespace Magikill.Loot
{
    /// <summary>
    /// Defines a single item drop entry with quantity range and drop chance.
    /// Used in loot tables to configure what enemies drop.
    /// </summary>
    [Serializable]
    public class LootDropEntry
    {
        [Header("Item Configuration")]
        [Tooltip("The item data that can drop")]
        public ItemData itemData;

        [Header("Quantity")]
        [Tooltip("Minimum quantity that drops (if drop succeeds)")]
        [Range(1, 100)]
        public int minQuantity = 1;

        [Tooltip("Maximum quantity that drops (if drop succeeds)")]
        [Range(1, 100)]
        public int maxQuantity = 1;

        [Header("Drop Chance")]
        [Tooltip("Percentage chance this item drops (0-100)")]
        [Range(0f, 100f)]
        public float dropChance = 100f;

        [Header("Upgrade Configuration (Equipment Only)")]
        [Tooltip("Should dropped equipment have random upgrade levels?")]
        public bool canBeUpgraded = false;

        [Tooltip("Minimum upgrade level (+0 to +10)")]
        [Range(0, 10)]
        public int minUpgradeLevel = 0;

        [Tooltip("Maximum upgrade level (+0 to +10)")]
        [Range(0, 10)]
        public int maxUpgradeLevel = 3;

        /// <summary>
        /// Checks if this item should drop based on drop chance.
        /// </summary>
        public bool ShouldDrop()
        {
            return UnityEngine.Random.Range(0f, 100f) <= dropChance;
        }

        /// <summary>
        /// Gets a random quantity within the configured range.
        /// </summary>
        public int GetRandomQuantity()
        {
            return UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
        }

        /// <summary>
        /// Gets a random upgrade level within the configured range.
        /// </summary>
        public int GetRandomUpgradeLevel()
        {
            if (!canBeUpgraded || !(itemData is EquipmentData))
            {
                return 0;
            }

            return UnityEngine.Random.Range(minUpgradeLevel, maxUpgradeLevel + 1);
        }

        /// <summary>
        /// Validates the loot entry configuration.
        /// </summary>
        public bool IsValid()
        {
            if (itemData == null)
            {
                Debug.LogWarning("[LootDropEntry] Item data is null!");
                return false;
            }

            if (minQuantity > maxQuantity)
            {
                Debug.LogWarning($"[LootDropEntry] {itemData.itemName}: minQuantity ({minQuantity}) is greater than maxQuantity ({maxQuantity})");
                return false;
            }

            if (canBeUpgraded && minUpgradeLevel > maxUpgradeLevel)
            {
                Debug.LogWarning($"[LootDropEntry] {itemData.itemName}: minUpgradeLevel ({minUpgradeLevel}) is greater than maxUpgradeLevel ({maxUpgradeLevel})");
                return false;
            }

            return true;
        }
    }
}
