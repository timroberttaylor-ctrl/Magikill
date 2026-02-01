namespace Magikill.Items
{
    /// <summary>
    /// Stat bonuses provided by gems when socketed into equipment.
    /// Core 6 stats for MVP.
    /// </summary>
    public enum GemStatType
    {
        /// <summary>
        /// Increases attack damage
        /// </summary>
        Attack = 0,

        /// <summary>
        /// Increases defense (damage reduction)
        /// </summary>
        Defense = 1,

        /// <summary>
        /// Increases maximum health points
        /// </summary>
        MaxHealth = 2,

        /// <summary>
        /// Increases maximum mana points
        /// </summary>
        MaxMana = 3,

        /// <summary>
        /// Increases attack speed (reduces attack cooldown)
        /// </summary>
        AttackSpeed = 4,

        /// <summary>
        /// Increases movement speed
        /// </summary>
        MovementSpeed = 5
    }
}
