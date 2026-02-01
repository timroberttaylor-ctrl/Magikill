using System;
using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// Defines the stat bonuses an item provides when equipped.
    /// Used for equipment base stats and gem bonuses.
    /// </summary>
    [Serializable]
    public struct ItemStats
    {
        [Header("Combat Stats")]
        [Tooltip("Attack damage bonus")]
        public float attack;

        [Tooltip("Defense bonus (damage reduction)")]
        public float defense;

        [Header("Resource Stats")]
        [Tooltip("Maximum health bonus")]
        public float maxHealth;

        [Tooltip("Maximum mana bonus")]
        public float maxMana;

        [Header("Speed Stats")]
        [Tooltip("Attack speed multiplier (1.0 = normal, 1.1 = 10% faster)")]
        public float attackSpeed;

        [Tooltip("Movement speed multiplier (1.0 = normal, 1.1 = 10% faster)")]
        public float movementSpeed;

        /// <summary>
        /// Creates a zero-stat item (no bonuses)
        /// </summary>
        public static ItemStats Zero => new ItemStats
        {
            attack = 0,
            defense = 0,
            maxHealth = 0,
            maxMana = 0,
            attackSpeed = 1.0f,
            movementSpeed = 1.0f
        };

        /// <summary>
        /// Adds two ItemStats together (for calculating total equipped stats)
        /// </summary>
        public static ItemStats operator +(ItemStats a, ItemStats b)
        {
            return new ItemStats
            {
                attack = a.attack + b.attack,
                defense = a.defense + b.defense,
                maxHealth = a.maxHealth + b.maxHealth,
                maxMana = a.maxMana + b.maxMana,
                attackSpeed = a.attackSpeed * b.attackSpeed, // Multiplicative for speed
                movementSpeed = a.movementSpeed * b.movementSpeed
            };
        }

        /// <summary>
        /// Multiplies ItemStats by a scalar (for upgrade calculations)
        /// </summary>
        public static ItemStats operator *(ItemStats stats, float multiplier)
        {
            return new ItemStats
            {
                attack = stats.attack * multiplier,
                defense = stats.defense * multiplier,
                maxHealth = stats.maxHealth * multiplier,
                maxMana = stats.maxMana * multiplier,
                attackSpeed = 1.0f + ((stats.attackSpeed - 1.0f) * multiplier), // Scale the bonus portion
                movementSpeed = 1.0f + ((stats.movementSpeed - 1.0f) * multiplier)
            };
        }

        public override string ToString()
        {
            return $"Attack: {attack}, Defense: {defense}, HP: {maxHealth}, Mana: {maxMana}, " +
                   $"AtkSpd: {attackSpeed:F2}x, MoveSpd: {movementSpeed:F2}x";
        }
    }
}
