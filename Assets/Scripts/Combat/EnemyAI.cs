using Fusion;
using UnityEngine;

namespace Magikill.Combat
{
    /// <summary>
    /// Simple stationary enemy AI.
    /// Detects players within detection range, attacks when in attack range.
    /// Enemy does not move - stays in place and attacks nearby players.
    /// </summary>
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(NetworkObject))]
    public class EnemyAI : NetworkBehaviour
    {
        #region Configuration

        [Header("Detection")]
        [SerializeField]
        [Tooltip("How far the enemy can detect players")]
        private float detectionRange = 10f;

        [SerializeField]
        [Tooltip("How often to check for nearby players (in seconds)")]
        private float detectionInterval = 0.5f;

        [Header("Combat")]
        [SerializeField]
        [Tooltip("Layer mask for detecting players")]
        private LayerMask playerLayer = ~0; // Default: detect everything

        #endregion

        #region References

        private EnemyStats _enemyStats;

        #endregion

        #region State

        [Networked]
        private NetworkBool IsInCombat { get; set; }

        [Networked]
        private TickTimer AttackTimer { get; set; }

        private Transform _currentTarget;
        private float _nextDetectionTime;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            _enemyStats = GetComponent<EnemyStats>();

            if (_enemyStats == null)
            {
                Debug.LogError("[EnemyAI] EnemyStats component not found!");
            }

            Debug.Log("[EnemyAI] Enemy AI initialized and ready");
        }

        #endregion

        #region Update Loop

        public override void FixedUpdateNetwork()
        {
            // Only server processes AI logic
            if (!HasStateAuthority)
            {
                return;
            }

            // Don't do anything if dead
            if (_enemyStats != null && !_enemyStats.IsAlive)
            {
                IsInCombat = false;
                _currentTarget = null;
                return;
            }

            // Periodic detection check
            if (Time.time >= _nextDetectionTime)
            {
                _nextDetectionTime = Time.time + detectionInterval;
                DetectNearbyPlayers();
            }

            // Process combat if we have a target
            if (_currentTarget != null)
            {
                ProcessCombat();
            }
        }

        #endregion

        #region Detection

        /// <summary>
        /// Detect nearby players within detection range.
        /// </summary>
        private void DetectNearbyPlayers()
        {
            // Find all colliders in detection range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;

            // Find the closest player
            foreach (Collider col in hitColliders)
            {
                // Check if this is a player (has CombatStats component)
                CombatStats playerStats = col.GetComponent<CombatStats>();
                if (playerStats != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = col.transform;
                    }
                }
            }

            // Update target
            if (closestPlayer != null)
            {
                if (_currentTarget == null)
                {
                    Debug.Log($"[EnemyAI] Target acquired: {closestPlayer.name}");
                }
                _currentTarget = closestPlayer;
                IsInCombat = true;
            }
            else
            {
                if (_currentTarget != null)
                {
                    Debug.Log("[EnemyAI] Target lost");
                }
                _currentTarget = null;
                IsInCombat = false;
            }
        }

        #endregion

        #region Combat

        /// <summary>
        /// Process combat with current target.
        /// </summary>
        private void ProcessCombat()
        {
            if (_currentTarget == null || _enemyStats == null)
            {
                return;
            }

            // Check if target is still in detection range
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            if (distanceToTarget > detectionRange)
            {
                Debug.Log("[EnemyAI] Target out of range");
                _currentTarget = null;
                IsInCombat = false;
                return;
            }

            // Face the target
            FaceTarget();

            // Attack if in range and cooldown expired
            if (distanceToTarget <= _enemyStats.AttackRange)
            {
                if (AttackTimer.ExpiredOrNotRunning(Runner))
                {
                    PerformAttack();
                }
            }
        }

        /// <summary>
        /// Make the enemy face its target.
        /// </summary>
        private void FaceTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            Vector3 direction = (_currentTarget.position - transform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane only

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Runner.DeltaTime);
            }
        }

        /// <summary>
        /// Perform an attack on the current target.
        /// </summary>
        private void PerformAttack()
        {
            if (_currentTarget == null || _enemyStats == null)
            {
                return;
            }

            // Get target's CombatStats
            CombatStats targetStats = _currentTarget.GetComponent<CombatStats>();
            if (targetStats != null)
            {
                // Calculate damage (using DamageCalculator for consistency)
                float damage = DamageCalculator.CalculateDamage(
                    _enemyStats.AttackDamage,
                    DamageType.Physical,
                    null, // Enemy doesn't have CombatStats (could add later)
                    targetStats
                );

                // Apply damage
                targetStats.ModifyHealth(-damage);

                Debug.Log($"[EnemyAI] Attacked {_currentTarget.name} for {damage} damage!");

                // TODO: Play attack animation
                // TODO: Play attack sound
                // TODO: Spawn attack VFX
            }

            // Set attack cooldown
            AttackTimer = TickTimer.CreateFromSeconds(Runner, _enemyStats.AttackCooldown);
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            if (_enemyStats != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _enemyStats.AttackRange);
            }

            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log AI State")]
        private void LogAIState()
        {
            Debug.Log("=== Enemy AI State ===");
            Debug.Log($"Detection Range: {detectionRange}");
            Debug.Log($"Is In Combat: {IsInCombat}");
            Debug.Log($"Current Target: {(_currentTarget != null ? _currentTarget.name : "None")}");
            Debug.Log($"Attack Timer Running: {!AttackTimer.ExpiredOrNotRunning(Runner)}");
            Debug.Log($"Has State Authority: {HasStateAuthority}");
        }

        #endregion
    }
}
