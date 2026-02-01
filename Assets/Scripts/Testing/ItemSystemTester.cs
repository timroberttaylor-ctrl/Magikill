using UnityEngine;
using Magikill.Items;

namespace Magikill.Testing
{
    /// <summary>
    /// Test script to verify the item system is working correctly.
    /// Attach to a GameObject in your TestZone scene and assign items in Inspector.
    /// Check Unity Console for test results.
    /// </summary>
    public class ItemSystemTester : MonoBehaviour
    {
        [Header("Equipment Items to Test")]
        [SerializeField]
        private EquipmentData ironSword;

        [SerializeField]
        private EquipmentData steelHelmet;

        [SerializeField]
        private EquipmentData dragonBoots;

        [Header("Consumable Items to Test")]
        [SerializeField]
        private ConsumableData healthPotion;

        [SerializeField]
        private ConsumableData manaPotion;

        [Header("Gem Items to Test")]
        [SerializeField]
        private GemData rubyGemI;

        [SerializeField]
        private GemData rubyGemIII;

        [SerializeField]
        private GemData sapphireGemII;

        [SerializeField]
        private GemData emeraldGemI;

        [Header("Currency to Test")]
        [SerializeField]
        private CurrencyData goldCoin;

        [Header("Test Settings")]
        [SerializeField]
        [Tooltip("Run tests automatically on Start")]
        private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("========================================");
            Debug.Log("ITEM SYSTEM TESTS STARTING");
            Debug.Log("========================================");

            TestEquipmentStats();
            TestEquipmentUpgrades();
            TestEquipmentSockets();
            TestGemBonuses();
            TestItemInstances();
            TestConsumables();
            TestCurrency();
            TestRarityColors();

            Debug.Log("========================================");
            Debug.Log("ITEM SYSTEM TESTS COMPLETE");
            Debug.Log("========================================");
        }

        #region Equipment Tests

        private void TestEquipmentStats()
        {
            Debug.Log("\n--- Testing Equipment Base Stats ---");

            if (ironSword != null)
            {
                Debug.Log($"Iron Sword Stats at +0: {ironSword.baseStats}");
                Debug.Log($"  Socket Count: {ironSword.socketCount} (Expected: 2 for weapons)");
                Debug.Log($"  Equipment Slot: {ironSword.equipmentSlot}");
                Debug.Log($"  Rarity Color: {ironSword.GetRarityColor()}");
            }
            else
            {
                Debug.LogWarning("Iron Sword not assigned! Drag it from Project to Inspector.");
            }

            if (steelHelmet != null)
            {
                Debug.Log($"Steel Helmet Stats at +0: {steelHelmet.baseStats}");
                Debug.Log($"  Socket Count: {steelHelmet.socketCount} (Expected: 1 for armor)");
            }

            if (dragonBoots != null)
            {
                Debug.Log($"Dragon Boots Stats at +0: {dragonBoots.baseStats}");
                Debug.Log($"  Movement Speed Bonus: {dragonBoots.baseStats.movementSpeed:F2}x (Expected: 1.15x)");
            }
        }

        private void TestEquipmentUpgrades()
        {
            Debug.Log("\n--- Testing Equipment Upgrade System ---");

            if (ironSword != null)
            {
                Debug.Log("Iron Sword Upgrade Progression:");
                Debug.Log($"  +0: {ironSword.GetStatsAtLevel(0)}");
                Debug.Log($"  +1: {ironSword.GetStatsAtLevel(1)}");
                Debug.Log($"  +5: {ironSword.GetStatsAtLevel(5)}");
                Debug.Log($"  +10: {ironSword.GetStatsAtLevel(10)}");

                Debug.Log("\nIron Sword Upgrade Costs:");
                Debug.Log($"  +0→+1: {ironSword.GetUpgradeCost(0)} gold");
                Debug.Log($"  +1→+2: {ironSword.GetUpgradeCost(1)} gold");
                Debug.Log($"  +4→+5: {ironSword.GetUpgradeCost(4)} gold");
                Debug.Log($"  +9→+10: {ironSword.GetUpgradeCost(9)} gold");
                Debug.Log($"  Already max: {ironSword.GetUpgradeCost(10)} gold (Expected: 0)");
            }
        }

        private void TestEquipmentSockets()
        {
            Debug.Log("\n--- Testing Equipment Socket System ---");

            if (ironSword != null)
            {
                Debug.Log($"Iron Sword (Weapon): {ironSword.GetSocketCount()} sockets (Expected: 2)");
            }

            if (steelHelmet != null)
            {
                Debug.Log($"Steel Helmet (Helmet): {steelHelmet.GetSocketCount()} sockets (Expected: 1)");
            }

            if (dragonBoots != null)
            {
                Debug.Log($"Dragon Boots (Boots): {dragonBoots.GetSocketCount()} sockets (Expected: 1)");
            }
        }

        #endregion

        #region Gem Tests

        private void TestGemBonuses()
        {
            Debug.Log("\n--- Testing Gem Stat Bonuses ---");

            if (rubyGemI != null)
            {
                Debug.Log($"Ruby Gem I (Tier {rubyGemI.tier}):");
                Debug.Log($"  Display Name: {rubyGemI.GetTieredName()}");
                Debug.Log($"  Stat Type: {rubyGemI.statType}");
                Debug.Log($"  Bonus: {rubyGemI.GetStatBonus()}");
            }

            if (rubyGemIII != null)
            {
                Debug.Log($"Ruby Gem III (Tier {rubyGemIII.tier}):");
                Debug.Log($"  Display Name: {rubyGemIII.GetTieredName()}");
                Debug.Log($"  Bonus: {rubyGemIII.GetStatBonus()}");
            }

            if (sapphireGemII != null)
            {
                Debug.Log($"Sapphire Gem II (Tier {sapphireGemII.tier}):");
                Debug.Log($"  Display Name: {sapphireGemII.GetTieredName()}");
                Debug.Log($"  Stat Type: {sapphireGemII.statType}");
                Debug.Log($"  Bonus: {sapphireGemII.GetStatBonus()}");
            }

            if (emeraldGemI != null)
            {
                Debug.Log($"Emerald Gem I (Tier {emeraldGemI.tier}):");
                Debug.Log($"  Display Name: {emeraldGemI.GetTieredName()}");
                Debug.Log($"  Stat Type: {emeraldGemI.statType}");
                Debug.Log($"  Bonus: {emeraldGemI.GetStatBonus()}");
            }
        }

        #endregion

        #region Item Instance Tests

        private void TestItemInstances()
        {
            Debug.Log("\n--- Testing Item Instances ---");

            if (ironSword != null)
            {
                // Create a basic instance
                ItemInstance basicSword = new ItemInstance(ironSword);
                Debug.Log($"Basic Iron Sword Instance: {basicSword}");
                Debug.Log($"  Display Name: {basicSword.GetDisplayName()}");
                Debug.Log($"  Total Stats: {basicSword.GetTotalStats()}");

                // Create an upgraded instance
                ItemInstance upgradedSword = new ItemInstance(ironSword);
                upgradedSword.upgradeLevel = 5;
                Debug.Log($"\nUpgraded Iron Sword +5: {upgradedSword}");
                Debug.Log($"  Display Name: {upgradedSword.GetDisplayName()}");
                Debug.Log($"  Total Stats: {upgradedSword.GetTotalStats()}");

                // Test socketing gems
                if (rubyGemI != null && sapphireGemII != null)
                {
                    ItemInstance gemmedSword = new ItemInstance(ironSword);
                    gemmedSword.upgradeLevel = 5;

                    bool socket1 = gemmedSword.SocketGem(rubyGemI, 0);
                    bool socket2 = gemmedSword.SocketGem(sapphireGemII, 1);

                    Debug.Log($"\nGemmed Iron Sword +5:");
                    Debug.Log($"  Socket 1 Success: {socket1} (Ruby Gem I)");
                    Debug.Log($"  Socket 2 Success: {socket2} (Sapphire Gem II)");
                    Debug.Log($"  Total Stats with Gems: {gemmedSword.GetTotalStats()}");
                    Debug.Log($"  Empty Sockets: {gemmedSword.GetEmptySocketCount()} (Expected: 0)");

                    // Test unsocketing
                    GemData removedGem = gemmedSword.UnsocketGem(0);
                    Debug.Log($"  Removed gem from socket 0: {removedGem?.itemName}");
                    Debug.Log($"  Empty Sockets after removal: {gemmedSword.GetEmptySocketCount()} (Expected: 1)");
                }
            }

            // Test stacking
            if (healthPotion != null)
            {
                ItemInstance potion1 = new ItemInstance(healthPotion, 10);
                ItemInstance potion2 = new ItemInstance(healthPotion, 15);

                Debug.Log($"\nStacking Test:");
                Debug.Log($"  Potion 1: {potion1.quantity} potions");
                Debug.Log($"  Potion 2: {potion2.quantity} potions");
                Debug.Log($"  Can stack: {potion1.CanStackWith(potion2)}");

                int remainder = potion1.Stack(potion2);
                Debug.Log($"  After stacking: Potion 1 = {potion1.quantity}, Remainder = {remainder}");
            }
        }

        #endregion

        #region Consumable Tests

        private void TestConsumables()
        {
            Debug.Log("\n--- Testing Consumables ---");

            if (healthPotion != null)
            {
                Debug.Log($"Health Potion:");
                Debug.Log($"  Restores: {healthPotion.healthRestore} HP");
                Debug.Log($"  Cooldown: {healthPotion.cooldown}s");
                Debug.Log($"  Usable in Combat: {healthPotion.usableInCombat}");
                Debug.Log($"  Stackable: {healthPotion.isStackable} (Max: {healthPotion.maxStackSize})");
                Debug.Log($"  Has Effect: {healthPotion.HasEffect()}");
            }

            if (manaPotion != null)
            {
                Debug.Log($"Mana Potion:");
                Debug.Log($"  Restores: {manaPotion.manaRestore} Mana");
                Debug.Log($"  Cooldown: {manaPotion.cooldown}s");
            }
        }

        #endregion

        #region Currency Tests

        private void TestCurrency()
        {
            Debug.Log("\n--- Testing Currency ---");

            if (goldCoin != null)
            {
                Debug.Log($"Gold Coin:");
                Debug.Log($"  Gold Value: {goldCoin.goldValue}");
                Debug.Log($"  Stackable: {goldCoin.isStackable} (Max: {goldCoin.maxStackSize})");

                ItemInstance goldStack = new ItemInstance(goldCoin, 999);
                Debug.Log($"  Gold Stack Example: {goldStack.quantity} coins");
            }
        }

        #endregion

        #region Rarity Tests

        private void TestRarityColors()
        {
            Debug.Log("\n--- Testing Rarity System ---");

            if (ironSword != null)
            {
                Debug.Log($"Iron Sword (Common):");
                Debug.Log($"  Colored Name: {ironSword.GetColoredName()}");
                Debug.Log($"  Rarity Color: {ironSword.GetRarityColor()}");
            }

            if (steelHelmet != null)
            {
                Debug.Log($"Steel Helmet (Rare):");
                Debug.Log($"  Colored Name: {steelHelmet.GetColoredName()}");
                Debug.Log($"  Rarity Color: {steelHelmet.GetRarityColor()}");
            }

            if (dragonBoots != null)
            {
                Debug.Log($"Dragon Boots (Epic):");
                Debug.Log($"  Colored Name: {dragonBoots.GetColoredName()}");
                Debug.Log($"  Rarity Color: {dragonBoots.GetRarityColor()}");
            }
        }

        #endregion
    }
}