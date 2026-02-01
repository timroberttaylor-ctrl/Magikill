using System;
using System.Collections.Generic;
using UnityEngine;
using Magikill.Items;

namespace Magikill.Inventory
{
    /// <summary>
    /// Manages a player's inventory and equipment.
    /// Handles item storage, equipment slots, currency, and stat calculations.
    /// Should be attached to the player GameObject alongside NetworkPlayer.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        #region Configuration

        [Header("Inventory Settings")]
        [SerializeField]
        [Tooltip("Maximum number of inventory slots")]
        private int maxInventorySlots = 20;

        #endregion

        #region Inventory Storage

        private List<ItemInstance> _inventorySlots = new List<ItemInstance>();

        /// <summary>
        /// Gets a read-only view of all inventory slots
        /// </summary>
        public IReadOnlyList<ItemInstance> InventorySlots => _inventorySlots;

        /// <summary>
        /// Current number of used inventory slots
        /// </summary>
        public int UsedSlots => _inventorySlots.Count;

        /// <summary>
        /// Number of free inventory slots remaining
        /// </summary>
        public int FreeSlots => maxInventorySlots - UsedSlots;

        #endregion

        #region Equipment Slots

        private Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
        private bool _equipmentSlotsInitialized = false;

        /// <summary>
        /// Gets the currently equipped item in a specific slot (null if empty)
        /// </summary>
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            EnsureEquipmentSlotsInitialized();

            if (!_equippedItems.ContainsKey(slot))
            {
                Debug.LogWarning($"[PlayerInventory] Equipment slot {slot} not initialized!");
                return null;
            }
            _equippedItems.TryGetValue(slot, out ItemInstance item);
            return item;
        }

        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public Dictionary<EquipmentSlot, ItemInstance> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentSlot, ItemInstance>(_equippedItems);
        }

        #endregion

        #region Currency

        private int _gold = 0;

        /// <summary>
        /// Player's current gold amount
        /// </summary>
        public int Gold => _gold;

        #endregion

        #region References

        private Magikill.Networking.NetworkPlayer _networkPlayer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _networkPlayer = GetComponent<Magikill.Networking.NetworkPlayer>();

            if (_networkPlayer == null)
            {
                Debug.LogWarning("[PlayerInventory] NetworkPlayer component not found! Stat updates won't work.");
            }

            // Initialize equipment slots
            InitializeEquipmentSlots();

            Debug.Log($"[PlayerInventory] Initialized with {maxInventorySlots} inventory slots and {_equippedItems.Count} equipment slots.");
        }

        #endregion

        #region Initialization

        private void InitializeEquipmentSlots()
        {
            if (_equipmentSlotsInitialized)
            {
                return; // Already initialized
            }

            // Create empty equipment slots for all 6 slots
            _equippedItems[EquipmentSlot.Weapon] = null;
            _equippedItems[EquipmentSlot.Helmet] = null;
            _equippedItems[EquipmentSlot.Chest] = null;
            _equippedItems[EquipmentSlot.Legs] = null;
            _equippedItems[EquipmentSlot.Boots] = null;
            _equippedItems[EquipmentSlot.Accessory] = null;

            _equipmentSlotsInitialized = true;
        }

        /// <summary>
        /// Ensures equipment slots are initialized (lazy initialization)
        /// </summary>
        private void EnsureEquipmentSlotsInitialized()
        {
            if (!_equipmentSlotsInitialized)
            {
                InitializeEquipmentSlots();
            }
        }

        #endregion

        #region Inventory Management

        /// <summary>
        /// Adds an item to inventory. Attempts to stack if possible.
        /// Returns true if successful, false if inventory is full.
        /// </summary>
        public bool AddItem(ItemInstance item)
        {
            if (item == null || item.itemData == null)
            {
                Debug.LogWarning("[PlayerInventory] Attempted to add null item!");
                return false;
            }

            // Special handling for currency
            if (item.itemData is CurrencyData currencyData)
            {
                AddGold(currencyData.goldValue * item.quantity);
                Debug.Log($"[PlayerInventory] Added {currencyData.goldValue * item.quantity} gold. Total: {_gold}");
                return true;
            }

            // Try to stack with existing items first
            if (item.itemData.isStackable)
            {
                foreach (ItemInstance existingItem in _inventorySlots)
                {
                    if (existingItem.CanStackWith(item))
                    {
                        int remainder = existingItem.Stack(item);

                        if (remainder <= 0)
                        {
                            Debug.Log($"[PlayerInventory] Stacked {item.itemData.itemName}. New quantity: {existingItem.quantity}");
                            return true;
                        }
                        else
                        {
                            // Partial stack, update item quantity and continue
                            item.quantity = remainder;
                        }
                    }
                }
            }

            // Check if we have space for a new slot
            if (UsedSlots >= maxInventorySlots)
            {
                Debug.LogWarning($"[PlayerInventory] Inventory full! Cannot add {item.itemData.itemName}");
                return false;
            }

            // Add to new slot
            _inventorySlots.Add(item);
            Debug.Log($"[PlayerInventory] Added {item.itemData.itemName} to inventory. Slots: {UsedSlots}/{maxInventorySlots}");
            return true;
        }

        /// <summary>
        /// Removes an item from inventory by instance.
        /// Returns true if successful.
        /// </summary>
        public bool RemoveItem(ItemInstance item)
        {
            bool removed = _inventorySlots.Remove(item);

            if (removed)
            {
                Debug.Log($"[PlayerInventory] Removed {item.itemData.itemName} from inventory. Slots: {UsedSlots}/{maxInventorySlots}");
            }

            return removed;
        }

        /// <summary>
        /// Removes a specific quantity of an item by item data.
        /// Returns true if successful.
        /// </summary>
        public bool RemoveItemByData(ItemData itemData, int quantity = 1)
        {
            int remainingToRemove = quantity;

            for (int i = _inventorySlots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                ItemInstance item = _inventorySlots[i];

                if (item.itemData == itemData)
                {
                    if (item.quantity <= remainingToRemove)
                    {
                        // Remove entire stack
                        remainingToRemove -= item.quantity;
                        _inventorySlots.RemoveAt(i);
                    }
                    else
                    {
                        // Remove partial stack
                        item.quantity -= remainingToRemove;
                        remainingToRemove = 0;
                    }
                }
            }

            bool success = remainingToRemove == 0;

            if (success)
            {
                Debug.Log($"[PlayerInventory] Removed {quantity}x {itemData.itemName}");
            }
            else
            {
                Debug.LogWarning($"[PlayerInventory] Could not remove {quantity}x {itemData.itemName}. Not enough in inventory.");
            }

            return success;
        }

        /// <summary>
        /// Gets the total quantity of a specific item in inventory
        /// </summary>
        public int GetItemCount(ItemData itemData)
        {
            int count = 0;

            foreach (ItemInstance item in _inventorySlots)
            {
                if (item.itemData == itemData)
                {
                    count += item.quantity;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if inventory has at least the specified quantity of an item
        /// </summary>
        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            return GetItemCount(itemData) >= quantity;
        }

        /// <summary>
        /// Clears all items from inventory (useful for testing)
        /// </summary>
        public void ClearInventory()
        {
            _inventorySlots.Clear();
            Debug.Log("[PlayerInventory] Inventory cleared.");
        }

        #endregion

        #region Equipment Management

        /// <summary>
        /// Equips an item from inventory.
        /// Automatically unequips any item in that slot first.
        /// Returns true if successful.
        /// </summary>
        public bool EquipItem(ItemInstance item)
        {
            EnsureEquipmentSlotsInitialized();

            if (item == null || !(item.itemData is EquipmentData equipData))
            {
                Debug.LogWarning("[PlayerInventory] Cannot equip non-equipment item!");
                return false;
            }

            // Check level requirement
            if (_networkPlayer != null && equipData.levelRequirement > _networkPlayer.Level)
            {
                Debug.LogWarning($"[PlayerInventory] Cannot equip {equipData.itemName}. Requires level {equipData.levelRequirement}");
                return false;
            }

            EquipmentSlot slot = equipData.equipmentSlot;

            // Ensure slot exists in dictionary
            if (!_equippedItems.ContainsKey(slot))
            {
                Debug.LogError($"[PlayerInventory] Equipment slot {slot} not initialized! Call InitializeEquipmentSlots first.");
                return false;
            }

            // Unequip existing item in that slot
            if (_equippedItems[slot] != null)
            {
                UnequipItem(slot);
            }

            // Remove from inventory
            if (!RemoveItem(item))
            {
                Debug.LogWarning($"[PlayerInventory] Item not found in inventory: {equipData.itemName}");
                return false;
            }

            // Equip the item
            _equippedItems[slot] = item;
            Debug.Log($"[PlayerInventory] Equipped {equipData.itemName} in {slot} slot");

            // Recalculate stats
            RecalculateEquipmentStats();

            return true;
        }

        /// <summary>
        /// Unequips an item from a slot and returns it to inventory.
        /// Returns true if successful.
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            EnsureEquipmentSlotsInitialized();

            // Ensure slot exists in dictionary
            if (!_equippedItems.ContainsKey(slot))
            {
                Debug.LogError($"[PlayerInventory] Equipment slot {slot} not initialized!");
                return false;
            }

            ItemInstance equippedItem = _equippedItems[slot];

            if (equippedItem == null)
            {
                Debug.LogWarning($"[PlayerInventory] No item equipped in {slot} slot!");
                return false;
            }

            // Check if we have inventory space
            if (UsedSlots >= maxInventorySlots)
            {
                Debug.LogWarning($"[PlayerInventory] Cannot unequip {equippedItem.itemData.itemName}. Inventory is full!");
                return false;
            }

            // Remove from equipment slot
            _equippedItems[slot] = null;

            // Add back to inventory
            _inventorySlots.Add(equippedItem);
            Debug.Log($"[PlayerInventory] Unequipped {equippedItem.itemData.itemName} from {slot} slot");

            // Recalculate stats
            RecalculateEquipmentStats();

            return true;
        }

        /// <summary>
        /// Recalculates total equipment bonuses and updates NetworkPlayer stats
        /// </summary>
        private void RecalculateEquipmentStats()
        {
            ItemStats totalBonuses = ItemStats.Zero;

            // Sum up all equipped item stats
            foreach (var kvp in _equippedItems)
            {
                ItemInstance equippedItem = kvp.Value;

                if (equippedItem != null)
                {
                    ItemStats itemStats = equippedItem.GetTotalStats();
                    totalBonuses = totalBonuses + itemStats;
                }
            }

            // Update NetworkPlayer with new bonuses
            if (_networkPlayer != null)
            {
                _networkPlayer.UpdateEquipmentBonuses(totalBonuses);
            }

            Debug.Log($"[PlayerInventory] Equipment stats recalculated: {totalBonuses}");
        }

        #endregion

        #region Currency Management

        /// <summary>
        /// Adds gold to the player's currency
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerInventory] Cannot add negative gold!");
                return;
            }

            _gold += amount;
            Debug.Log($"[PlayerInventory] Added {amount} gold. Total: {_gold}");
        }

        /// <summary>
        /// Removes gold from the player's currency.
        /// Returns true if player had enough gold.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerInventory] Cannot spend negative gold!");
                return false;
            }

            if (_gold < amount)
            {
                Debug.LogWarning($"[PlayerInventory] Not enough gold! Have: {_gold}, Need: {amount}");
                return false;
            }

            _gold -= amount;
            Debug.Log($"[PlayerInventory] Spent {amount} gold. Remaining: {_gold}");
            return true;
        }

        /// <summary>
        /// Checks if player has at least the specified amount of gold
        /// </summary>
        public bool HasGold(int amount)
        {
            return _gold >= amount;
        }

        #endregion

        #region Consumable Usage

        /// <summary>
        /// Uses a consumable item from inventory.
        /// Returns true if successful.
        /// </summary>
        public bool UseConsumable(ItemInstance item)
        {
            if (item == null || !(item.itemData is ConsumableData consumableData))
            {
                Debug.LogWarning("[PlayerInventory] Cannot use non-consumable item!");
                return false;
            }

            if (!consumableData.HasEffect())
            {
                Debug.LogWarning($"[PlayerInventory] {consumableData.itemName} has no effects!");
                return false;
            }

            // Apply effects to NetworkPlayer
            if (_networkPlayer != null)
            {
                if (consumableData.healthRestore > 0)
                {
                    _networkPlayer.Heal(consumableData.healthRestore);
                }

                if (consumableData.manaRestore > 0)
                {
                    _networkPlayer.RestoreMana(consumableData.manaRestore);
                }
            }

            // Consume one from the stack
            item.quantity--;

            if (item.quantity <= 0)
            {
                RemoveItem(item);
            }

            Debug.Log($"[PlayerInventory] Used {consumableData.itemName}. Remaining: {item.quantity}");
            return true;
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Inventory")]
        public void LogInventory()
        {
            Debug.Log("=== INVENTORY ===");
            Debug.Log($"Gold: {_gold}");
            Debug.Log($"Slots Used: {UsedSlots}/{maxInventorySlots}");
            Debug.Log("\n--- Items ---");

            for (int i = 0; i < _inventorySlots.Count; i++)
            {
                ItemInstance item = _inventorySlots[i];
                Debug.Log($"[{i}] {item.GetDisplayName()}");
            }

            Debug.Log("\n--- Equipped Items ---");
            foreach (var kvp in _equippedItems)
            {
                if (kvp.Value != null)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value.GetDisplayName()}");
                }
                else
                {
                    Debug.Log($"{kvp.Key}: (empty)");
                }
            }
        }

        [ContextMenu("Log Equipment Stats")]
        public void LogEquipmentStats()
        {
            Debug.Log("=== EQUIPMENT STATS ===");

            ItemStats totalBonuses = ItemStats.Zero;

            foreach (var kvp in _equippedItems)
            {
                if (kvp.Value != null)
                {
                    ItemStats itemStats = kvp.Value.GetTotalStats();
                    Debug.Log($"{kvp.Key}: {itemStats}");
                    totalBonuses = totalBonuses + itemStats;
                }
            }

            Debug.Log($"\nTotal Equipment Bonuses: {totalBonuses}");

            if (_networkPlayer != null)
            {
                Debug.Log($"\nFinal Player Stats:");
                Debug.Log($"  Attack: {_networkPlayer.TotalAttack}");
                Debug.Log($"  Defense: {_networkPlayer.TotalDefense}");
                Debug.Log($"  Max HP: {_networkPlayer.TotalMaxHealth}");
                Debug.Log($"  Max Mana: {_networkPlayer.TotalMaxMana}");
                Debug.Log($"  Attack Speed: {_networkPlayer.TotalAttackSpeed:F2}x");
                Debug.Log($"  Move Speed: {_networkPlayer.TotalMovementSpeed:F2}x");
            }
        }

        #endregion
    }
}