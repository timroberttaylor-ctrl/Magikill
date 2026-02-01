using UnityEngine;

namespace Magikill.Combat
{
    /// <summary>
    /// Static utility class for calculating damage values.
    /// Supports damage types, armor, resistances, and critical hits.
    /// Designed to be expanded as stat systems are implemented.
    /// </summary>
    public static class DamageCalculator
    {
        #region Damage Calculation

        /// <summary>
        /// Calculates final damage dealt from attacker to defender.
        /// </summary>
        /// <param name="baseDamage">Base damage from the skill</param>
        /// <param name="damageType">Type of damage (Physical or Magical)</param>
        /// <param name="attackerStats">Attacker's combat stats (for future attack power bonuses)</param>
        /// <param name="defenderStats">Defender's combat stats (for armor/resistance)</param>
        /// <returns>Final damage amount after all calculations</returns>
        public static float CalculateDamage(
            float baseDamage, 
            DamageType damageType, 
            CombatStats attackerStats = null, 
            CombatStats defenderStats = null)
        {
            float finalDamage = baseDamage;

            // TODO: Apply attacker's damage bonuses (attack power, spell power, etc.)
            // float damageBonus = GetDamageBonus(attackerStats, damageType);
            // finalDamage += damageBonus;

            // TODO: Apply defender's damage reduction (armor, magic resistance)
            // float damageReduction = GetDamageReduction(defenderStats, damageType);
            // finalDamage -= damageReduction;

            // TODO: Check for critical hit
            // if (IsCriticalHit(attackerStats))
            // {
            //     finalDamage *= GetCriticalMultiplier(attackerStats);
            // }

            // Ensure damage is never negative
            finalDamage = Mathf.Max(0f, finalDamage);

            return finalDamage;
        }

        /// <summary>
        /// Calculates skill damage with cooldown reduction applied.
        /// </summary>
        public static float CalculateSkillDamage(
            SkillDefinition skill, 
            CombatStats attackerStats, 
            CombatStats defenderStats = null)
        {
            if (skill == null)
            {
                Debug.LogWarning("[DamageCalculator] Skill is null, returning 0 damage.");
                return 0f;
            }

            return CalculateDamage(skill.Damage, skill.DamageType, attackerStats, defenderStats);
        }

        #endregion

        #region Future Expansion Methods

        // These methods are placeholders for future stat system implementation

        /// <summary>
        /// Gets damage bonus from attacker's stats (future implementation).
        /// </summary>
        private static float GetDamageBonus(CombatStats attackerStats, DamageType damageType)
        {
            if (attackerStats == null)
            {
                return 0f;
            }

            // TODO: Implement based on damage type
            // if (damageType == DamageType.Physical)
            // {
            //     return attackerStats.AttackPower * 0.1f; // Example: 10% of attack power
            // }
            // else if (damageType == DamageType.Magical)
            // {
            //     return attackerStats.SpellPower * 0.1f; // Example: 10% of spell power
            // }

            return 0f;
        }

        /// <summary>
        /// Gets damage reduction from defender's stats (future implementation).
        /// </summary>
        private static float GetDamageReduction(CombatStats defenderStats, DamageType damageType)
        {
            if (defenderStats == null)
            {
                return 0f;
            }

            // TODO: Implement armor/resistance formulas
            // if (damageType == DamageType.Physical)
            // {
            //     float armor = defenderStats.Armor;
            //     // Common formula: damage * (100 / (100 + armor))
            //     return armor / (100f + armor);
            // }
            // else if (damageType == DamageType.Magical)
            // {
            //     float magicResist = defenderStats.MagicResistance;
            //     return magicResist / (100f + magicResist);
            // }

            return 0f;
        }

        /// <summary>
        /// Checks if attack is a critical hit (future implementation).
        /// </summary>
        private static bool IsCriticalHit(CombatStats attackerStats)
        {
            if (attackerStats == null)
            {
                return false;
            }

            // TODO: Implement critical hit chance
            // float critChance = attackerStats.CriticalChance; // e.g., 0.15 for 15%
            // return Random.value < critChance;

            return false;
        }

        /// <summary>
        /// Gets critical hit damage multiplier (future implementation).
        /// </summary>
        private static float GetCriticalMultiplier(CombatStats attackerStats)
        {
            if (attackerStats == null)
            {
                return 1.5f; // Default 150% damage on crit
            }

            // TODO: Implement critical damage scaling
            // return 1f + attackerStats.CriticalDamage; // e.g., 1.5 for 150% damage

            return 1.5f;
        }

        #endregion

        #region Cooldown Calculation

        /// <summary>
        /// Calculates actual cooldown duration with reduction applied.
        /// </summary>
        /// <param name="baseCooldown">Base cooldown from skill</param>
        /// <param name="cooldownReduction">Cooldown reduction multiplier (1.0 = normal, 1.5 = 50% faster)</param>
        /// <returns>Actual cooldown duration in seconds</returns>
        public static float CalculateCooldown(float baseCooldown, float cooldownReduction = 1f)
        {
            if (cooldownReduction <= 0f)
            {
                Debug.LogWarning("[DamageCalculator] Invalid cooldown reduction value, using 1.0");
                cooldownReduction = 1f;
            }

            // Higher multiplier = faster cooldowns
            // Example: 1.5x CDR means cooldowns are 1/1.5 = 66.7% of base duration
            float actualCooldown = baseCooldown / cooldownReduction;

            return Mathf.Max(0f, actualCooldown);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if a target is within range of the attacker.
        /// </summary>
        public static bool IsInRange(Vector3 attackerPosition, Vector3 targetPosition, float range)
        {
            float distance = Vector3.Distance(attackerPosition, targetPosition);
            return distance <= range;
        }

        /// <summary>
        /// Gets the direction from attacker to target (normalized).
        /// </summary>
        public static Vector3 GetDirectionToTarget(Vector3 attackerPosition, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - attackerPosition;
            direction.y = 0f; // Ignore vertical component for ground-based combat
            return direction.normalized;
        }

        #endregion

        #region Debug Utilities

        /// <summary>
        /// Logs damage calculation details for debugging.
        /// </summary>
        public static void LogDamageCalculation(
            float baseDamage, 
            DamageType damageType, 
            float finalDamage, 
            string attackerName = "Attacker", 
            string defenderName = "Defender")
        {
            Debug.Log($"[DamageCalculator] {attackerName} -> {defenderName}: " +
                      $"Base: {baseDamage} ({damageType}) => Final: {finalDamage}");
        }

        #endregion
    }
}
