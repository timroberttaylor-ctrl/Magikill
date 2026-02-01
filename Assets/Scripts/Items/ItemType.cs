namespace Magikill.Items
{
    /// <summary>
    /// Defines the type/category of an item.
    /// Determines which systems can interact with the item.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// Equipment that can be worn in equipment slots
        /// </summary>
        Equipment = 0,

        /// <summary>
        /// Consumable items (potions, food, etc.)
        /// </summary>
        Consumable = 1,

        /// <summary>
        /// Currency (gold, coins)
        /// </summary>
        Currency = 2,

        /// <summary>
        /// Gems for socketing into equipment
        /// </summary>
        Gem = 3,

        /// <summary>
        /// Quest items
        /// </summary>
        Quest = 4,

        /// <summary>
        /// Crafting materials
        /// </summary>
        Material = 5
    }
}
