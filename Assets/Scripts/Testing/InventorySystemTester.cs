using UnityEngine;
using Magikill.Items;
using Magikill.Inventory;

namespace Magikill.Testing
{
    /// <summary>
    /// Test script to verify the inventory and equipment systems work correctly.
    /// Attach to a GameObject in TestZone scene.
    /// Requires a Player with PlayerInventory component in the scene.
    /// </summary>
    public class InventorySystemTester : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField]
        [Tooltip("The player GameObject with PlayerInventory component")]
        private PlayerInventory playerInventory;

        [Header("Items to Test")]
        [SerializeField]
        private EquipmentData ironSword;

        [SerializeField]
        private EquipmentData steelHelmet;

        [SerializeField]
        private EquipmentData dragonBoots;

        [SerializeField]
        private ConsumableData healthPotion;

        [SerializeField]
        private ConsumableData manaPotion;

        [SerializeField]
        private GemData rubyGemI;

        [SerializeField]
        private GemData sapphireGemII;

        [SerializeField]
        private CurrencyData goldCoin;

        [Header("Test Settings")]
        [SerializeField]
        private bool runOnStart = false;

        private void Start()
        {
            if (runOnStart && playerInventory != null)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Inventory Tests")]
        public void RunAllTests()
        {
            if (playerInventory == null)
            {
                Debug.LogError("[InventoryTester] PlayerInventory not assigned! Drag Player GameObject to Inspector.");
                return;
            }

            Debug.Log("========================================");
            Debug.Log("INVENTORY SYSTEM TESTS STARTING");
            Debug.Log("========================================");

            // Clear inventory first
            playerInventory.ClearInventory();

            TestAddingItems();
            TestCurrency();
            TestStacking();
            TestEquipment();
            TestUpgradedEquipment();
            TestGemmedEquipment();
            TestConsumables();
            TestInventoryFull();

            Debug.Log("========================================");
            Debug.Log("INVENTORY SYSTEM TESTS COMPLETE");
            Debug.Log("========================================");
        }

        #region Test Methods

        private void TestAddingItems()
        {
            Debug.Log("\n--- Test: Adding Items to Inventory ---");

            if (ironSword != null)
            {
                ItemInstance sword = new ItemInstance(ironSword);
                bool added = playerInventory.AddItem(sword);
                Debug.Log($"Add Iron Sword: {added} (Expected: true)");
                Debug.Log($"Inventory slots: {playerInventory.UsedSlots}/20");
            }

            if (steelHelmet != null)
            {
                ItemInstance helmet = new ItemInstance(steelHelmet);
                bool added = playerInventory.AddItem(helmet);
                Debug.Log($"Add Steel Helmet: {added} (Expected: true)");
            }

            if (healthPotion != null)
            {
                ItemInstance potion = new ItemInstance(healthPotion, 5);
                bool added = playerInventory.AddItem(potion);
                Debug.Log($"Add 5x Health Potion: {added} (Expected: true)");
            }

            playerInventory.LogInventory();
        }

        private void TestCurrency()
        {
            Debug.Log("\n--- Test: Currency System ---");

            if (goldCoin != null)
            {
                // Add gold via currency item
                ItemInstance gold = new ItemInstance(goldCoin, 100);
                bool added = playerInventory.AddItem(gold);
                Debug.Log($"Add 100 gold coins: {added}");
                Debug.Log($"Current gold: {playerInventory.Gold} (Expected: 100)");

                // Test spending gold
                bool canAfford = playerInventory.HasGold(50);
                Debug.Log($"Can afford 50g: {canAfford} (Expected: true)");

                bool spent = playerInventory.SpendGold(50);
                Debug.Log($"Spent 50g: {spent}");
                Debug.Log($"Remaining gold: {playerInventory.Gold} (Expected: 50)");

                // Test insufficient gold
                bool spentTooMuch = playerInventory.SpendGold(100);
                Debug.Log($"Try to spend 100g (don't have enough): {spentTooMuch} (Expected: false)");
                Debug.Log($"Gold after failed purchase: {playerInventory.Gold} (Expected: 50)");
            }
        }

        private void TestStacking()
        {
            Debug.Log("\n--- Test: Item Stacking ---");

            if (healthPotion != null)
            {
                Debug.Log($"Health potions before stacking: {playerInventory.GetItemCount(healthPotion)}");
                Debug.Log($"Used slots before: {playerInventory.UsedSlots}");

                // Add more health potions - should stack
                ItemInstance morePortions = new ItemInstance(healthPotion, 10);
                bool added = playerInventory.AddItem(morePortions);
                Debug.Log($"Add 10 more health potions: {added}");
                Debug.Log($"Total health potions after stacking: {playerInventory.GetItemCount(healthPotion)} (Expected: 15)");
                Debug.Log($"Used slots after (should not increase): {playerInventory.UsedSlots}");
            }

            playerInventory.LogInventory();
        }

        private void TestEquipment()
        {
            Debug.Log("\n--- Test: Equipment System ---");

            // Get items from inventory
            ItemInstance swordInInventory = null;
            ItemInstance helmetInInventory = null;

            foreach (ItemInstance item in playerInventory.InventorySlots)
            {
                if (item.itemData == ironSword) swordInInventory = item;
                if (item.itemData == steelHelmet) helmetInInventory = item;
            }

            if (swordInInventory != null)
            {
                Debug.Log("Equipping Iron Sword...");
                bool equipped = playerInventory.EquipItem(swordInInventory);
                Debug.Log($"Equipped: {equipped} (Expected: true)");
                Debug.Log($"Inventory slots after equip: {playerInventory.UsedSlots} (should decrease by 1)");

                ItemInstance equippedWeapon = playerInventory.GetEquippedItem(EquipmentSlot.Weapon);
                Debug.Log($"Weapon slot has item: {equippedWeapon != null} (Expected: true)");
            }

            if (helmetInInventory != null)
            {
                Debug.Log("\nEquipping Steel Helmet...");
                bool equipped = playerInventory.EquipItem(helmetInInventory);
                Debug.Log($"Equipped: {equipped} (Expected: true)");
            }

            playerInventory.LogEquipmentStats();

            // Test unequipping
            Debug.Log("\nUnequipping weapon...");
            bool unequipped = playerInventory.UnequipItem(EquipmentSlot.Weapon);
            Debug.Log($"Unequipped: {unequipped} (Expected: true)");
            Debug.Log($"Inventory slots after unequip: {playerInventory.UsedSlots} (should increase by 1)");

            playerInventory.LogInventory();
        }

        private void TestUpgradedEquipment()
        {
            Debug.Log("\n--- Test: Upgraded Equipment Stats ---");

            if (ironSword != null)
            {
                // Create an upgraded sword
                ItemInstance upgradedSword = new ItemInstance(ironSword);
                upgradedSword.upgradeLevel = 5;

                Debug.Log($"Created {upgradedSword.GetDisplayName()}");
                Debug.Log($"Stats: {upgradedSword.GetTotalStats()}");

                playerInventory.AddItem(upgradedSword);

                // Find and equip it
                foreach (ItemInstance item in playerInventory.InventorySlots)
                {
                    if (item.itemData == ironSword && item.upgradeLevel == 5)
                    {
                        Debug.Log("Equipping upgraded sword...");
                        playerInventory.EquipItem(item);
                        break;
                    }
                }

                playerInventory.LogEquipmentStats();
            }
        }

        private void TestGemmedEquipment()
        {
            Debug.Log("\n--- Test: Gemmed Equipment Stats ---");

            if (ironSword != null && rubyGemI != null && sapphireGemII != null)
            {
                // Create a gemmed sword
                ItemInstance gemmedSword = new ItemInstance(ironSword);
                gemmedSword.upgradeLevel = 5;
                gemmedSword.SocketGem(rubyGemI, 0);
                gemmedSword.SocketGem(sapphireGemII, 1);

                Debug.Log($"Created {gemmedSword.GetDisplayName()} with 2 gems");
                Debug.Log($"Stats: {gemmedSword.GetTotalStats()}");

                // Unequip current weapon if equipped
                if (playerInventory.GetEquippedItem(EquipmentSlot.Weapon) != null)
                {
                    playerInventory.UnequipItem(EquipmentSlot.Weapon);
                }

                playerInventory.AddItem(gemmedSword);

                // Find and equip it
                foreach (ItemInstance item in playerInventory.InventorySlots)
                {
                    if (item.itemData == ironSword && item.socketedGems[0] != null)
                    {
                        Debug.Log("Equipping gemmed sword...");
                        playerInventory.EquipItem(item);
                        break;
                    }
                }

                playerInventory.LogEquipmentStats();
            }
        }

        private void TestConsumables()
        {
            Debug.Log("\n--- Test: Using Consumables ---");

            if (healthPotion != null)
            {
                int potionCount = playerInventory.GetItemCount(healthPotion);
                Debug.Log($"Health potions before use: {potionCount}");

                // Find a health potion and use it
                foreach (ItemInstance item in playerInventory.InventorySlots)
                {
                    if (item.itemData == healthPotion)
                    {
                        Debug.Log("Using health potion...");
                        bool used = playerInventory.UseConsumable(item);
                        Debug.Log($"Used: {used} (Expected: true)");
                        break;
                    }
                }

                int potionCountAfter = playerInventory.GetItemCount(healthPotion);
                Debug.Log($"Health potions after use: {potionCountAfter} (Expected: {potionCount - 1})");
            }
        }

        private void TestInventoryFull()
        {
            Debug.Log("\n--- Test: Inventory Full ---");

            // Fill inventory with items
            Debug.Log("Filling inventory to capacity...");
            int itemsAdded = 0;

            while (playerInventory.FreeSlots > 0 && ironSword != null)
            {
                ItemInstance sword = new ItemInstance(ironSword);
                if (playerInventory.AddItem(sword))
                {
                    itemsAdded++;
                }
                else
                {
                    break;
                }
            }

            Debug.Log($"Added {itemsAdded} items");
            Debug.Log($"Inventory slots: {playerInventory.UsedSlots}/20 (Expected: 20)");
            Debug.Log($"Free slots: {playerInventory.FreeSlots} (Expected: 0)");

            // Try to add when full
            if (ironSword != null)
            {
                ItemInstance extraSword = new ItemInstance(ironSword);
                bool added = playerInventory.AddItem(extraSword);
                Debug.Log($"Try to add item when full: {added} (Expected: false)");
            }

            playerInventory.LogInventory();
        }

        #endregion

        #region Manual Test Buttons

        [ContextMenu("Add Test Items")]
        public void AddTestItems()
        {
            if (playerInventory == null) return;

            if (ironSword != null)
            {
                playerInventory.AddItem(new ItemInstance(ironSword));
                Debug.Log("Added Iron Sword");
            }

            if (healthPotion != null)
            {
                playerInventory.AddItem(new ItemInstance(healthPotion, 5));
                Debug.Log("Added 5x Health Potion");
            }

            if (goldCoin != null)
            {
                playerInventory.AddItem(new ItemInstance(goldCoin, 100));
                Debug.Log("Added 100 gold");
            }

            playerInventory.LogInventory();
        }

        [ContextMenu("Clear Inventory")]
        public void ClearInventory()
        {
            if (playerInventory != null)
            {
                playerInventory.ClearInventory();
                Debug.Log("Inventory cleared");
            }
        }

        #endregion
    }
}
