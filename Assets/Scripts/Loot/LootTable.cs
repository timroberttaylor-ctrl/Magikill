using System.Collections.Generic;
using UnityEngine;
using Magikill.Items;

namespace Magikill.Loot
{
    /// <summary>
    /// ScriptableObject that defines what items an enemy type can drop.
    /// Attach to enemy prefabs to configure their loot.
    /// Create via: Assets > Create > Magikill > Loot > Loot Table
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Table", menuName = "Magikill/Loot/Loot Table", order = 1)]
    public class LootTable : ScriptableObject
    {
        [Header("Loot Configuration")]
        [Tooltip("List of items that can drop from this enemy")]
        public List<LootDropEntry> lootEntries = new List<LootDropEntry>();

        [Header("Guaranteed Drops")]
        [Tooltip("Gold amount that always drops (set to 0 for no gold)")]
        [Range(0, 1000)]
        public int guaranteedGoldMin = 1;

        [Tooltip("Maximum gold that can drop")]
        [Range(0, 1000)]
        public int guaranteedGoldMax = 5;

        /// <summary>
        /// Rolls loot and returns a list of items that should drop.
        /// </summary>
        public List<ItemInstance> RollLoot()
        {
            List<ItemInstance> droppedItems = new List<ItemInstance>();

            // Always drop gold if configured
            if (guaranteedGoldMax > 0)
            {
                int goldAmount = Random.Range(guaranteedGoldMin, guaranteedGoldMax + 1);
                if (goldAmount > 0)
                {
                    // Find or create gold coin currency data
                    // Note: In production, you'd reference a specific gold coin asset
                    // For now, we'll create a basic currency instance
                    // This will be improved when we add proper currency handling
                    Debug.Log($"[LootTable] Would drop {goldAmount} gold (currency system needs proper reference)");
                }
            }

            // Roll each loot entry
            foreach (LootDropEntry entry in lootEntries)
            {
                if (!entry.IsValid())
                {
                    continue;
                }

                // Check if this item should drop
                if (entry.ShouldDrop())
                {
                    int quantity = entry.GetRandomQuantity();
                    ItemInstance droppedItem = new ItemInstance(entry.itemData, quantity);

                    // Apply random upgrade level if applicable
                    if (entry.canBeUpgraded && entry.itemData is EquipmentData)
                    {
                        droppedItem.upgradeLevel = entry.GetRandomUpgradeLevel();
                    }

                    droppedItems.Add(droppedItem);
                    Debug.Log($"[LootTable] Rolled drop: {droppedItem.GetDisplayName()}");
                }
            }

            return droppedItems;
        }

        /// <summary>
        /// Validates all loot entries in this table.
        /// </summary>
        [ContextMenu("Validate Loot Table")]
        public void ValidateLootTable()
        {
            Debug.Log($"=== Validating Loot Table: {name} ===");

            if (lootEntries.Count == 0)
            {
                Debug.LogWarning($"[LootTable] {name} has no loot entries!");
                return;
            }

            int validEntries = 0;
            foreach (LootDropEntry entry in lootEntries)
            {
                if (entry.IsValid())
                {
                    validEntries++;
                }
            }

            Debug.Log($"[LootTable] {name}: {validEntries}/{lootEntries.Count} valid entries");
            
            if (guaranteedGoldMax > 0)
            {
                Debug.Log($"[LootTable] Gold drop: {guaranteedGoldMin}-{guaranteedGoldMax}");
            }
        }

        /// <summary>
        /// Simulates rolling loot multiple times to test drop rates.
        /// </summary>
        [ContextMenu("Test Drop Rates (100 rolls)")]
        public void TestDropRates()
        {
            Debug.Log($"=== Testing Drop Rates for: {name} ===");
            Debug.Log("Rolling loot 100 times...\n");

            Dictionary<string, int> dropCounts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                List<ItemInstance> drops = RollLoot();
                
                foreach (ItemInstance item in drops)
                {
                    string itemName = item.itemData.itemName;
                    if (!dropCounts.ContainsKey(itemName))
                    {
                        dropCounts[itemName] = 0;
                    }
                    dropCounts[itemName]++;
                }
            }

            Debug.Log("=== Drop Rate Results ===");
            foreach (var kvp in dropCounts)
            {
                float dropRate = (kvp.Value / 100f) * 100f;
                Debug.Log($"{kvp.Key}: {kvp.Value}/100 ({dropRate:F1}%)");
            }
        }
    }
}
