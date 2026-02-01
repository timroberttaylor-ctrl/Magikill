using System;
using UnityEngine;
using Fusion;

namespace Magikill.Combat
{
    /// <summary>
    /// Manages player combat statistics including health, mana, and regeneration.
    /// Fires events when resources change to support UI updates.
    /// Attach to player prefab alongside SkillSystem.
    /// </summary>
    public class CombatStats : NetworkBehaviour
    {
        #region Configuration

        [Header("Health")]
        [SerializeField]
        [Tooltip("Maximum health points")]
        private float maxHealth = 100f;

        [SerializeField]
        [Tooltip("Health regeneration per second")]
        private float healthRegen = 2f;

        [Header("Mana")]
        [SerializeField]
        [Tooltip("Maximum mana points")]
        private float maxMana = 100f;

        [SerializeField]
        [Tooltip("Mana regeneration per second")]
        private float manaRegen = 5f;

        #endregion

        #region Networked State

        /// <summary>
        /// Current health (synced across network).
        /// </summary>
        [Networked]
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// Current mana (synced across network).
        /// </summary>
        [Networked]
        public float CurrentMana { get; private set; }

        /// <summary>
        /// Cooldown reduction multiplier (1.0 = normal, 1.5 = 50% faster cooldowns).
        /// Used by skill system for cooldown calculations.
        /// </summary>
        [Networked]
        public float CooldownReduction { get; set; }

        #endregion

        #region Properties

        public float MaxHealth => maxHealth;
        public float MaxMana => maxMana;
        public float HealthPercent => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;
        public float ManaPercent => maxMana > 0 ? CurrentMana / maxMana : 0f;
        public bool IsDead => CurrentHealth <= 0;

        #endregion

        #region Events

        /// <summary>
        /// Fired when health changes. Parameters: (current, max, delta)
        /// </summary>
        public event Action<float, float, float> OnHealthChanged;

        /// <summary>
        /// Fired when mana changes. Parameters: (current, max, delta)
        /// </summary>
        public event Action<float, float, float> OnManaChanged;

        /// <summary>
        /// Fired when the character dies.
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            // Initialize resources if we have state authority (server)
            if (HasStateAuthority)
            {
                InitializeStats();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes combat stats (server authority).
        /// Called when player spawns.
        /// </summary>
        private void InitializeStats()
        {
            CurrentHealth = maxHealth;
            CurrentMana = maxMana;
            CooldownReduction = 1f; // Normal cooldown speed

            Debug.Log($"[CombatStats] Stats initialized - Health: {CurrentHealth}/{maxHealth}, Mana: {CurrentMana}/{maxMana}");
        }

        /// <summary>
        /// Sets max health and mana (for future class-based initialization).
        /// Can only be called by server.
        /// </summary>
        public void SetMaxStats(float newMaxHealth, float newMaxMana)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[CombatStats] Cannot set max stats without state authority!");
                return;
            }

            maxHealth = newMaxHealth;
            maxMana = newMaxMana;

            // Clamp current values to new max
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
            CurrentMana = Mathf.Min(CurrentMana, maxMana);

            Debug.Log($"[CombatStats] Max stats set - Health: {maxHealth}, Mana: {maxMana}");
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Regenerates health and mana over time (server authority).
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || IsDead)
            {
                return;
            }

            // Regenerate health
            if (CurrentHealth < maxHealth)
            {
                ModifyHealth(healthRegen * Runner.DeltaTime);
            }

            // Regenerate mana
            if (CurrentMana < maxMana)
            {
                ModifyMana(manaRegen * Runner.DeltaTime);
            }
        }

        /// <summary>
        /// Modifies health by the specified amount (server authority).
        /// Positive for healing, negative for damage.
        /// </summary>
        public void ModifyHealth(float amount)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[CombatStats] Cannot modify health without state authority!");
                return;
            }

            if (IsDead && amount < 0)
            {
                return; // Can't damage what's already dead
            }

            float oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
            float actualChange = CurrentHealth - oldHealth;

            // Fire event
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth, actualChange);

            // Check for death
            if (CurrentHealth <= 0 && oldHealth > 0)
            {
                OnDeath?.Invoke();
                HandleDeath();
            }
        }

        /// <summary>
        /// Modifies mana by the specified amount (server authority).
        /// Positive for mana gain, negative for consumption.
        /// </summary>
        public void ModifyMana(float amount)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[CombatStats] Cannot modify mana without state authority!");
                return;
            }

            float oldMana = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, maxMana);
            float actualChange = CurrentMana - oldMana;

            // Fire event
            OnManaChanged?.Invoke(CurrentMana, maxMana, actualChange);
        }

        /// <summary>
        /// Checks if player has enough mana for a skill.
        /// </summary>
        public bool HasEnoughMana(float manaCost)
        {
            return CurrentMana >= manaCost;
        }

        /// <summary>
        /// Consumes mana for a skill (server authority).
        /// Returns true if successful, false if not enough mana.
        /// </summary>
        public bool ConsumeMana(float manaCost)
        {
            if (!HasEnoughMana(manaCost))
            {
                return false;
            }

            ModifyMana(-manaCost);
            return true;
        }

        #endregion

        #region Death Handling

        private void HandleDeath()
        {
            Debug.Log($"[CombatStats] Player has died!");
            // TODO: Implement death logic (disable controls, play animation, respawn timer, etc.)
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Fully restores health and mana (server authority).
        /// </summary>
        public void FullRestore()
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[CombatStats] Cannot restore without state authority!");
                return;
            }

            ModifyHealth(maxHealth);
            ModifyMana(maxMana);
            Debug.Log("[CombatStats] Fully restored health and mana.");
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Combat Stats")]
        private void LogCombatStats()
        {
            Debug.Log("=== Combat Stats ===");
            Debug.Log($"Health: {CurrentHealth}/{maxHealth} ({HealthPercent:P0})");
            Debug.Log($"Mana: {CurrentMana}/{maxMana} ({ManaPercent:P0})");
            Debug.Log($"Health Regen: {healthRegen}/s");
            Debug.Log($"Mana Regen: {manaRegen}/s");
            Debug.Log($"Cooldown Reduction: {CooldownReduction}x");
            Debug.Log($"Is Dead: {IsDead}");
        }

        [ContextMenu("Take 20 Damage")]
        private void DebugTakeDamage()
        {
            ModifyHealth(-20f);
        }

        [ContextMenu("Consume 30 Mana")]
        private void DebugConsumeMana()
        {
            ModifyMana(-30f);
        }

        [ContextMenu("Full Restore")]
        private void DebugFullRestore()
        {
            FullRestore();
        }

        #endregion
    }
}
