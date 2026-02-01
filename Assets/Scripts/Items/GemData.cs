using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// ScriptableObject for gem items used in the socketing system.
    /// Defines stat bonuses when socketed into equipment.
    /// Create via: Assets > Create > Magikill > Items > Gem
    /// </summary>
    [CreateAssetMenu(fileName = "New Gem", menuName = "Magikill/Items/Gem", order = 3)]
    public class GemData : ItemData
    {
        [Header("Gem Properties")]
        [Tooltip("Which stat this gem boosts")]
        public GemStatType statType = GemStatType.Attack;

        [Tooltip("Gem tier (I, II, III, IV, V)")]
        [Range(1, 5)]
        public int tier = 1;

        [Tooltip("Stat bonus value provided by this gem")]
        public float bonusValue = 5f;

        /// <summary>
        /// Gets the formatted gem name with tier (e.g., "Ruby Gem III")
        /// </summary>
        public string GetTieredName()
        {
            string tierRoman = GetRomanNumeral(tier);
            return $"{itemName} {tierRoman}";
        }

        /// <summary>
        /// Converts tier number to Roman numeral
        /// </summary>
        private string GetRomanNumeral(int number)
        {
            switch (number)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return number.ToString();
            }
        }

        /// <summary>
        /// Gets the stat bonus as ItemStats structure for easy application
        /// </summary>
        public ItemStats GetStatBonus()
        {
            ItemStats stats = ItemStats.Zero;

            switch (statType)
            {
                case GemStatType.Attack:
                    stats.attack = bonusValue;
                    break;
                case GemStatType.Defense:
                    stats.defense = bonusValue;
                    break;
                case GemStatType.MaxHealth:
                    stats.maxHealth = bonusValue;
                    break;
                case GemStatType.MaxMana:
                    stats.maxMana = bonusValue;
                    break;
                case GemStatType.AttackSpeed:
                    stats.attackSpeed = 1.0f + (bonusValue / 100f); // Convert percentage to multiplier
                    break;
                case GemStatType.MovementSpeed:
                    stats.movementSpeed = 1.0f + (bonusValue / 100f); // Convert percentage to multiplier
                    break;
            }

            return stats;
        }

        /// <summary>
        /// Gets suggested bonus value based on tier for balancing
        /// </summary>
        public static float GetSuggestedBonusValue(GemStatType statType, int tier)
        {
            // Base values per tier (tier 1 = base, each tier adds this much)
            float baseValue = 0f;

            switch (statType)
            {
                case GemStatType.Attack:
                case GemStatType.Defense:
                    baseValue = 5f; // +5/+10/+15/+20/+25 per tier
                    break;
                case GemStatType.MaxHealth:
                    baseValue = 20f; // +20/+40/+60/+80/+100 per tier
                    break;
                case GemStatType.MaxMana:
                    baseValue = 15f; // +15/+30/+45/+60/+75 per tier
                    break;
                case GemStatType.AttackSpeed:
                case GemStatType.MovementSpeed:
                    baseValue = 2f; // +2%/+4%/+6%/+8%/+10% per tier
                    break;
            }

            return baseValue * tier;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Ensure itemType is set to Gem
            itemType = ItemType.Gem;

            // Gems should be stackable
            isStackable = true;
            if (maxStackSize < 1)
            {
                maxStackSize = 99;
            }

            // Auto-suggest bonus value if it's 0
            if (bonusValue == 0)
            {
                bonusValue = GetSuggestedBonusValue(statType, tier);
            }
        }
    }
}
