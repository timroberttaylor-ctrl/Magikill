using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// Base ScriptableObject for all item data.
    /// This is the template/definition for items, not individual instances.
    /// Create items in Unity via Create > Magikill > Items > [Item Type]
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for this item")]
        public string itemId;

        [Tooltip("Display name shown to players")]
        public string itemName;

        [TextArea(3, 6)]
        [Tooltip("Description shown in tooltips")]
        public string description;

        [Tooltip("Icon displayed in inventory/UI")]
        public Sprite icon;

        [Header("Item Properties")]
        [Tooltip("Type of item (Equipment, Consumable, etc.)")]
        public ItemType itemType;

        [Tooltip("Rarity tier (Common, Rare, Epic)")]
        public ItemRarity rarity = ItemRarity.Common;

        [Tooltip("Minimum player level required to use this item")]
        [Range(1, 20)]
        public int levelRequirement = 1;

        [Header("Stacking")]
        [Tooltip("Can this item stack in inventory?")]
        public bool isStackable = false;

        [Tooltip("Maximum stack size (0 = infinite)")]
        public int maxStackSize = 1;

        [Header("Economy")]
        [Tooltip("Gold value when sold to vendor")]
        public int sellPrice = 0;

        [Tooltip("Gold cost when buying from vendor (0 = not purchasable)")]
        public int buyPrice = 0;

        /// <summary>
        /// Gets the rarity color for UI display
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return Color.white;
                case ItemRarity.Rare:
                    return new Color(0.3f, 0.5f, 1.0f); // Blue
                case ItemRarity.Epic:
                    return new Color(0.64f, 0.21f, 0.93f); // Purple
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Gets formatted item name with rarity color tags
        /// </summary>
        public string GetColoredName()
        {
            Color color = GetRarityColor();
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>{itemName}</color>";
        }

        /// <summary>
        /// Validates item data on save (Unity Editor only)
        /// </summary>
        protected virtual void OnValidate()
        {
            // Ensure itemId matches asset name if empty
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name;
            }

            // Ensure stackable items have valid max stack
            if (isStackable && maxStackSize < 1)
            {
                maxStackSize = 99;
            }
        }
    }
}
