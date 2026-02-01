namespace Magikill.Items
{
    /// <summary>
    /// Equipment slots for the 6-slot equipment system.
    /// Each slot can hold one piece of equipment.
    /// </summary>
    public enum EquipmentSlot
    {
        /// <summary>
        /// No slot / not equipable
        /// </summary>
        None = 0,

        /// <summary>
        /// Weapon slot - 2 sockets
        /// </summary>
        Weapon = 1,

        /// <summary>
        /// Helmet slot - 1 socket
        /// </summary>
        Helmet = 2,

        /// <summary>
        /// Chest armor slot - 1 socket
        /// </summary>
        Chest = 3,

        /// <summary>
        /// Leg armor slot - 1 socket
        /// </summary>
        Legs = 4,

        /// <summary>
        /// Boot slot - 1 socket
        /// </summary>
        Boots = 5,

        /// <summary>
        /// Accessory slot (ring, necklace) - 1 socket
        /// </summary>
        Accessory = 6
    }
}
