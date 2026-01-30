namespace Magikill.Character
{
    /// <summary>
    /// Defines the playable character classes in Magikill.
    /// Used for character selection, stats, and ability systems.
    /// </summary>
    public enum CharacterClass
    {
        /// <summary>
        /// No class selected or undefined state.
        /// Used during character selection or initialization.
        /// </summary>
        None = 0,

        /// <summary>
        /// Warrior class - Melee fighter with high health and defense.
        /// Specializes in close-range combat and tanking.
        /// </summary>
        Warrior = 1,

        /// <summary>
        /// Archer class - Ranged attacker with high agility and critical strikes.
        /// Specializes in long-range physical damage.
        /// </summary>
        Archer = 2,

        /// <summary>
        /// Mage class - Spellcaster with high mana and magical damage.
        /// Specializes in area-of-effect spells and crowd control.
        /// </summary>
        Mage = 3
    }
}
