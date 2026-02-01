using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// ScriptableObject for equipment items (weapons, armor, accessories).
    /// Defines base stats, equipment slot, and socket configuration.
    /// Create via: Assets > Create > Magikill > Items > Equipment
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Magikill/Items/Equipment", order = 1)]
    public class EquipmentData : ItemData
    {
        [Header("Equipment Specific")]
        [Tooltip("Which equipment slot this item occupies")]
        public EquipmentSlot equipmentSlot = EquipmentSlot.None;

        [Tooltip("Base stats provided by this equipment at +0")]
        public ItemStats baseStats = ItemStats.Zero;

        [Header("Socket System")]
        [Tooltip("Number of gem sockets this item has")]
        [Range(0, 3)]
        public int socketCount = 0;

        [Header("Upgrade System")]
        [Tooltip("Maximum upgrade level (+0 to +maxUpgradeLevel)")]
        [Range(0, 10)]
        public int maxUpgradeLevel = 10;

        [Tooltip("Base gold cost to upgrade from +0 to +1 (increases per level)")]
        public int baseUpgradeCost = 100;

        /// <summary>
        /// Calculates stats at a specific upgrade level.
        /// Each level adds 5% of base stats.
        /// </summary>
        public ItemStats GetStatsAtLevel(int upgradeLevel)
        {
            if (upgradeLevel < 0 || upgradeLevel > maxUpgradeLevel)
            {
                Debug.LogWarning($"[EquipmentData] Invalid upgrade level {upgradeLevel} for {itemName}");
                upgradeLevel = Mathf.Clamp(upgradeLevel, 0, maxUpgradeLevel);
            }

            // 5% increase per level: multiplier = 1.0 + (level * 0.05)
            float multiplier = 1.0f + (upgradeLevel * 0.05f);
            return baseStats * multiplier;
        }

        /// <summary>
        /// Calculates gold cost to upgrade from current level to next level.
        /// Cost increases with each level.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= maxUpgradeLevel)
            {
                return 0; // Already max level
            }

            // Cost formula: baseCost * (currentLevel + 1)
            // Example: +0→+1 = 100g, +1→+2 = 200g, +2→+3 = 300g
            return baseUpgradeCost * (currentLevel + 1);
        }

        /// <summary>
        /// Gets the number of sockets based on equipment slot type.
        /// Weapons = 2, everything else = 1
        /// </summary>
        public int GetSocketCount()
        {
            if (equipmentSlot == EquipmentSlot.Weapon)
            {
                return 2;
            }
            else if (equipmentSlot != EquipmentSlot.None)
            {
                return 1;
            }
            return 0;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Ensure itemType is set to Equipment
            itemType = ItemType.Equipment;

            // Equipment should not be stackable
            isStackable = false;
            maxStackSize = 1;

            // Auto-set socket count based on slot
            socketCount = GetSocketCount();
        }
    }
}
