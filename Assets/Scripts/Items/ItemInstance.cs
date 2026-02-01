using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// Represents an actual instance of an item at runtime.
    /// Contains item data reference plus instance-specific data (upgrade level, socketed gems, quantity).
    /// This is what exists in inventory, on the ground, and in equipment slots.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        [Header("Item Reference")]
        [Tooltip("Reference to the item's data definition")]
        public ItemData itemData;

        [Header("Instance Data")]
        [Tooltip("Quantity of this item (for stackable items)")]
        public int quantity = 1;

        [Tooltip("Unique instance ID (for tracking specific items)")]
        public string instanceId;

        [Header("Equipment Instance Data")]
        [Tooltip("Current upgrade level (+0 to +10)")]
        public int upgradeLevel = 0;

        [Tooltip("Gems socketed into this equipment (null = empty socket)")]
        public List<GemData> socketedGems = new List<GemData>();

        /// <summary>
        /// Creates a new item instance from item data
        /// </summary>
        public ItemInstance(ItemData data, int qty = 1)
        {
            itemData = data;
            quantity = qty;
            instanceId = Guid.NewGuid().ToString();
            upgradeLevel = 0;
            socketedGems = new List<GemData>();

            // Initialize socket list for equipment
            if (data is EquipmentData equipData)
            {
                for (int i = 0; i < equipData.socketCount; i++)
                {
                    socketedGems.Add(null); // Empty sockets
                }
            }
        }

        /// <summary>
        /// Gets the total stats for this equipment including upgrades and gems
        /// </summary>
        public ItemStats GetTotalStats()
        {
            if (!(itemData is EquipmentData equipData))
            {
                return ItemStats.Zero;
            }

            // Start with upgraded base stats
            ItemStats totalStats = equipData.GetStatsAtLevel(upgradeLevel);

            // Add gem bonuses
            foreach (GemData gem in socketedGems)
            {
                if (gem != null)
                {
                    totalStats = totalStats + gem.GetStatBonus();
                }
            }

            return totalStats;
        }

        /// <summary>
        /// Checks if this item can stack with another item instance
        /// </summary>
        public bool CanStackWith(ItemInstance other)
        {
            if (other == null || itemData == null || other.itemData == null)
                return false;

            // Must be same item and stackable
            if (itemData != other.itemData || !itemData.isStackable)
                return false;

            // Equipment can't stack even if same item (each has unique upgrade/gems)
            if (itemData is EquipmentData)
                return false;

            return true;
        }

        /// <summary>
        /// Attempts to stack with another item. Returns remaining quantity if can't fully stack.
        /// </summary>
        public int Stack(ItemInstance other)
        {
            if (!CanStackWith(other))
                return other.quantity;

            int maxStack = itemData.maxStackSize;
            if (maxStack <= 0) maxStack = int.MaxValue; // Unlimited stack

            int availableSpace = maxStack - quantity;
            int amountToAdd = Mathf.Min(availableSpace, other.quantity);

            quantity += amountToAdd;
            return other.quantity - amountToAdd; // Return remainder
        }

        /// <summary>
        /// Sockets a gem into an empty slot. Returns true if successful.
        /// </summary>
        public bool SocketGem(GemData gem, int socketIndex)
        {
            if (!(itemData is EquipmentData))
            {
                Debug.LogWarning("[ItemInstance] Cannot socket gem into non-equipment item");
                return false;
            }

            if (gem == null)
            {
                Debug.LogWarning("[ItemInstance] Cannot socket null gem");
                return false;
            }

            if (socketIndex < 0 || socketIndex >= socketedGems.Count)
            {
                Debug.LogWarning($"[ItemInstance] Socket index {socketIndex} out of range");
                return false;
            }

            if (socketedGems[socketIndex] != null)
            {
                Debug.LogWarning($"[ItemInstance] Socket {socketIndex} is already occupied");
                return false;
            }

            socketedGems[socketIndex] = gem;
            Debug.Log($"[ItemInstance] Socketed {gem.GetTieredName()} into slot {socketIndex} of {itemData.itemName}");
            return true;
        }

        /// <summary>
        /// Removes a gem from a socket. Returns the removed gem.
        /// </summary>
        public GemData UnsocketGem(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= socketedGems.Count)
            {
                Debug.LogWarning($"[ItemInstance] Socket index {socketIndex} out of range");
                return null;
            }

            GemData removedGem = socketedGems[socketIndex];
            socketedGems[socketIndex] = null;

            if (removedGem != null)
            {
                Debug.Log($"[ItemInstance] Removed {removedGem.GetTieredName()} from slot {socketIndex} of {itemData.itemName}");
            }

            return removedGem;
        }

        /// <summary>
        /// Gets the number of empty sockets
        /// </summary>
        public int GetEmptySocketCount()
        {
            int count = 0;
            foreach (GemData gem in socketedGems)
            {
                if (gem == null) count++;
            }
            return count;
        }

        /// <summary>
        /// Gets a display-friendly name including upgrade level and gems
        /// </summary>
        public string GetDisplayName()
        {
            if (itemData == null)
                return "[NULL ITEM]";

            string displayName = itemData.GetColoredName();

            // Add upgrade level for equipment
            if (itemData is EquipmentData && upgradeLevel > 0)
            {
                displayName += $" <color=#00FF00>+{upgradeLevel}</color>";
            }

            // Add quantity for stackable items
            if (itemData.isStackable && quantity > 1)
            {
                displayName += $" x{quantity}";
            }

            return displayName;
        }

        public override string ToString()
        {
            return $"{itemData?.itemName ?? "NULL"} (Qty: {quantity}, +{upgradeLevel}, Sockets: {socketedGems.Count})";
        }
    }
}
