using System;
using Fusion;
using UnityEngine;

namespace Magikill.Combat
{
    /// <summary>
    /// Manages enemy health, damage, and combat statistics.
    /// Works with NetworkBehaviour for multiplayer synchronization.
    /// Handles damage reception and death logic.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class EnemyStats : NetworkBehaviour
    {
        #region Events

        /// <summary>
        /// Event fired when this enemy dies.
        /// Subscribe to this to handle loot drops, XP rewards, etc.
        /// </summary>
        public event Action OnDeathEvent;

        #endregion

        #region Configuration

        [Header("Combat Stats")]
        [SerializeField]
        [Tooltip("Maximum health points")]
        private float maxHealth = 100f;

        [SerializeField]
        [Tooltip("Damage dealt per attack")]
        private float attackDamage = 10f;

        [SerializeField]
        [Tooltip("Time between attacks in seconds")]
        private float attackCooldown = 2f;

        [SerializeField]
        [Tooltip("Attack range in units")]
        private float attackRange = 2f;

        #endregion

        #region Networked State

        /// <summary>
        /// Current health points (synchronized across network).
        /// </summary>
        [Networked]
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// Is the enemy currently alive?
        /// </summary>
        [Networked]
        public NetworkBool IsAlive { get; private set; }

        #endregion

        #region Public Properties

        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float AttackRange => attackRange;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            // Initialize health on server
            if (HasStateAuthority)
            {
                CurrentHealth = maxHealth;
                IsAlive = true;
                Debug.Log($"[EnemyStats] Enemy spawned with {CurrentHealth}/{maxHealth} health");
            }
        }

        #endregion

        #region Damage Handling

        /// <summary>
        /// Apply damage to this enemy (server authority).
        /// Called by player attacks or other damage sources.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="attacker">Transform of the attacker (optional)</param>
        public void TakeDamage(float damage, Transform attacker = null)
        {
            // Only server can modify health
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[EnemyStats] Cannot apply damage without state authority!");
                return;
            }

            if (!IsAlive)
            {
                return; // Already dead
            }

            // Apply damage
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            Debug.Log($"[EnemyStats] Enemy took {damage} damage. Health: {CurrentHealth}/{maxHealth}");

            // Check for death
            if (CurrentHealth <= 0)
            {
                Die(attacker);
            }
        }

        /// <summary>
        /// Instantly kill this enemy (server authority).
        /// </summary>
        public void Kill()
        {
            if (HasStateAuthority && IsAlive)
            {
                Die(null);
            }
        }

        #endregion

        #region Death Logic

        /// <summary>
        /// Handle enemy death (server authority).
        /// </summary>
        /// <param name="killer">Transform of the entity that killed this enemy</param>
        private void Die(Transform killer)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            IsAlive = false;
            Debug.Log($"[EnemyStats] Enemy died! Killer: {(killer != null ? killer.name : "Unknown")}");

            // Fire death event BEFORE despawning so loot can spawn at this position
            OnDeathEvent?.Invoke();

            // Despawn after a delay (reduced to 0.1s so loot spawns quickly)
            StartCoroutine(DespawnAfterDelay(0.1f));
        }

        /// <summary>
        /// Despawn the enemy after a delay.
        /// </summary>
        private System.Collections.IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (HasStateAuthority && Runner != null)
            {
                Runner.Despawn(Object);
                Debug.Log("[EnemyStats] Enemy despawned after death delay");
            }
        }

        #endregion

        #region Healing (for future use)

        /// <summary>
        /// Heal this enemy (server authority).
        /// </summary>
        /// <param name="amount">Amount of health to restore</param>
        public void Heal(float amount)
        {
            if (!HasStateAuthority || !IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            Debug.Log($"[EnemyStats] Enemy healed {amount}. Health: {CurrentHealth}/{maxHealth}");
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Take 20 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(20f);
        }

        [ContextMenu("Kill Enemy")]
        private void DebugKillEnemy()
        {
            Kill();
        }

        [ContextMenu("Log Enemy Stats")]
        private void LogEnemyStats()
        {
            Debug.Log("=== Enemy Stats ===");
            Debug.Log($"Health: {CurrentHealth}/{maxHealth}");
            Debug.Log($"Attack Damage: {attackDamage}");
            Debug.Log($"Attack Cooldown: {attackCooldown}s");
            Debug.Log($"Attack Range: {attackRange} units");
            Debug.Log($"Is Alive: {IsAlive}");
            Debug.Log($"Has State Authority: {HasStateAuthority}");
        }

        #endregion
    }
}