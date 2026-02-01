namespace Magikill.Combat
{
    /// <summary>
    /// Defines how a skill targets enemies or locations.
    /// Used to determine skill execution behavior.
    /// </summary>
    public enum SkillTargetType
    {
        /// <summary>
        /// Automatically targets the nearest valid enemy within range.
        /// Common for basic attacks and simple abilities.
        /// </summary>
        AutoTarget = 0,

        /// <summary>
        /// Fires in the direction the player is facing.
        /// Used for skillshot abilities and projectiles.
        /// </summary>
        Directional = 1,

        /// <summary>
        /// Requires player to select a ground location.
        /// Used for area-of-effect abilities and targeted spells.
        /// </summary>
        GroundTargeted = 2
    }

    /// <summary>
    /// Defines the type of damage dealt by a skill.
    /// Used for damage calculation and resistance systems.
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Physical damage - reduced by armor.
        /// Typical for warrior and archer abilities.
        /// </summary>
        Physical = 0,

        /// <summary>
        /// Magical damage - reduced by magic resistance.
        /// Typical for mage abilities and spells.
        /// </summary>
        Magical = 1
    }

    /// <summary>
    /// Defines how a skill's effect is delivered.
    /// Used for visual effects and hit detection.
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>
        /// Instant hit - damage applies immediately.
        /// No travel time, hits as soon as cast.
        /// </summary>
        Instant = 0,

        /// <summary>
        /// Projectile - spawns a moving object that travels to target.
        /// Can be dodged, has travel time.
        /// </summary>
        Projectile = 1,

        /// <summary>
        /// Area of Effect - affects all targets in an area.
        /// Can hit multiple enemies simultaneously.
        /// </summary>
        AOE = 2
    }
}
