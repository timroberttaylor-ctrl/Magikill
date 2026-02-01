namespace Magikill.Items
{
    /// <summary>
    /// Item rarity tiers for the loot system.
    /// Determines base stats, drop rates, and visual presentation.
    /// </summary>
    public enum ItemRarity
    {
        /// <summary>
        /// Common items - White color, most frequent drops
        /// </summary>
        Common = 0,

        /// <summary>
        /// Rare items - Blue color, moderate drop rate
        /// </summary>
        Rare = 1,

        /// <summary>
        /// Epic items - Purple color, rare drops
        /// </summary>
        Epic = 2
    }
}
