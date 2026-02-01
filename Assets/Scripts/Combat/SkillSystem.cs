using System;
using UnityEngine;
using Fusion;
using Magikill.Character;


namespace Magikill.Combat
{
    /// <summary>
    /// Manages player skills, cooldowns, and skill execution.
    /// Handles 6 skill slots with cooldown tracking and mana validation.
    /// Attach to player prefab alongside CombatStats.
    /// </summary>
    public class SkillSystem : NetworkBehaviour
    {
        #region Configuration

        [Header("Skill Slots")]
        [SerializeField]
        [Tooltip("Skill assigned to slot 1 (also basic attack)")]
        private SkillDefinition skill1;

        [SerializeField]
        [Tooltip("Skill assigned to slot 2")]
        private SkillDefinition skill2;

        [SerializeField]
        [Tooltip("Skill assigned to slot 3")]
        private SkillDefinition skill3;

        [SerializeField]
        [Tooltip("Skill assigned to slot 4")]
        private SkillDefinition skill4;

        [SerializeField]
        [Tooltip("Skill assigned to slot 5")]
        private SkillDefinition skill5;

        [SerializeField]
        [Tooltip("Skill assigned to slot 6")]
        private SkillDefinition skill6;

        #endregion

        #region References

        private CombatStats _combatStats;
        private Animator _animator;

        #endregion

        #region Cooldown State

        // Cooldown timers for each skill (in seconds remaining)
        private float[] _cooldownTimers = new float[6];

        // Track when skills were last used (for network sync)
        [Networked, Capacity(6)]
        private NetworkArray<float> LastUsedTimes => default;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a skill is used. Parameters: (skillSlot, skillDefinition)
        /// </summary>
        public event Action<int, SkillDefinition> OnSkillUsed;

        /// <summary>
        /// Fired when a skill's cooldown updates. Parameters: (skillSlot, remainingTime, totalCooldown)
        /// </summary>
        public event Action<int, float, float> OnCooldownUpdate;

        /// <summary>
        /// Fired when a skill cast fails. Parameters: (skillSlot, reason)
        /// </summary>
        public event Action<int, string> OnSkillCastFailed;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            // Get references
            _combatStats = GetComponent<CombatStats>();
            _animator = GetComponentInChildren<Animator>();

            if (_combatStats == null)
            {
                Debug.LogError("[SkillSystem] CombatStats component not found!");
            }

            // Initialize cooldown timers
            for (int i = 0; i < 6; i++)
            {
                _cooldownTimers[i] = 0f;
            }

            Debug.Log("[SkillSystem] Initialized with 6 skill slots.");
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            // Update cooldown timers
            UpdateCooldowns();
        }

        private void UpdateCooldowns()
        {
            for (int i = 0; i < 6; i++)
            {
                if (_cooldownTimers[i] > 0f)
                {
                    _cooldownTimers[i] -= Time.deltaTime;

                    if (_cooldownTimers[i] <= 0f)
                    {
                        _cooldownTimers[i] = 0f;
                    }

                    // Always fire cooldown update event
                    OnCooldownUpdate?.Invoke(i, _cooldownTimers[i], GetSkillCooldown(i));
                }
            }
        }

        #endregion

        #region Skill Execution

        /// <summary>
        /// Attempts to use a skill from the specified slot (0-5).
        /// Validates mana, cooldown, and range before execution.
        /// </summary>
        public void UseSkill(int skillSlot, Transform target = null)
        {
            // Validate skill slot
            if (skillSlot < 0 || skillSlot >= 6)
            {
                Debug.LogWarning($"[SkillSystem] Invalid skill slot: {skillSlot}");
                return;
            }

            // Get skill definition
            SkillDefinition skill = GetSkill(skillSlot);

            if (skill == null)
            {
                OnSkillCastFailed?.Invoke(skillSlot, "No skill equipped");
                return;
            }

            // Check if skill is on cooldown
            if (IsOnCooldown(skillSlot))
            {
                OnSkillCastFailed?.Invoke(skillSlot, "Skill on cooldown");
                return;
            }

            // Check mana cost
            if (!_combatStats.HasEnoughMana(skill.ManaCost))
            {
                OnSkillCastFailed?.Invoke(skillSlot, "Not enough mana");
                return;
            }

            // Check range if we have a target
            if (target != null && !IsTargetInRange(target, skill.Range))
            {
                OnSkillCastFailed?.Invoke(skillSlot, "Target out of range");
                return;
            }

            // Execute skill on server
            if (HasStateAuthority)
            {
                ExecuteSkillServer(skillSlot, skill, target);
            }
        }

        /// <summary>
        /// Executes the skill on the server (authoritative).
        /// </summary>
        private void ExecuteSkillServer(int skillSlot, SkillDefinition skill, Transform target)
        {
            // Consume mana
            if (!_combatStats.ConsumeMana(skill.ManaCost))
            {
                Debug.LogWarning($"[SkillSystem] Failed to consume mana for {skill.SkillName}");
                return;
            }

            // Start cooldown
            float actualCooldown = DamageCalculator.CalculateCooldown(skill.Cooldown, _combatStats.CooldownReduction);
            _cooldownTimers[skillSlot] = actualCooldown;

            // Record last used time (for network sync)
            LastUsedTimes.Set(skillSlot, (float)Runner.SimulationTime);

            // Play animation
            PlaySkillAnimation(skill);

            // Execute skill effect based on target type
            switch (skill.TargetType)
            {
                case SkillTargetType.AutoTarget:
                    ExecuteAutoTargetSkill(skill, target);
                    break;

                case SkillTargetType.Directional:
                    ExecuteDirectionalSkill(skill);
                    break;

                case SkillTargetType.GroundTargeted:
                    ExecuteGroundTargetedSkill(skill, target);
                    break;
            }

            // Fire event
            OnSkillUsed?.Invoke(skillSlot, skill);

            Debug.Log($"[SkillSystem] Used skill: {skill.SkillName} (Slot {skillSlot + 1})");
        }

        #endregion

        #region Skill Effect Execution

        private void ExecuteAutoTargetSkill(SkillDefinition skill, Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning($"[SkillSystem] Auto-target skill {skill.SkillName} has no target!");
                return;
            }

            // Try to get target's CombatStats (for players)
            CombatStats targetStats = target.GetComponent<CombatStats>();
            if (targetStats != null)
            {
                // Calculate damage
                float damage = DamageCalculator.CalculateSkillDamage(skill, _combatStats, targetStats);

                // Apply damage
                targetStats.ModifyHealth(-damage);

                // Spawn particle effect
                SpawnSkillEffect(skill, target.position);

                Debug.Log($"[SkillSystem] {skill.SkillName} hit {target.name} for {damage} damage!");
                return;
            }

            // Try to get target's EnemyStats (for enemies)
            EnemyStats enemyStats = target.GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                // Calculate damage for enemy
                float damage = DamageCalculator.CalculateSkillDamage(skill, _combatStats, null);

                // Apply damage to enemy
                enemyStats.TakeDamage(damage, transform);

                // Spawn particle effect
                SpawnSkillEffect(skill, target.position);

                Debug.Log($"[SkillSystem] {skill.SkillName} hit enemy {target.name} for {damage} damage!");
                return;
            }

            Debug.LogWarning($"[SkillSystem] Target {target.name} has no CombatStats or EnemyStats component!");
        }

        private void ExecuteDirectionalSkill(SkillDefinition skill)
        {
            // TODO: Implement directional projectile/raycast
            // For now, just spawn effect at player position
            SpawnSkillEffect(skill, transform.position + transform.forward * 2f);

            Debug.Log($"[SkillSystem] {skill.SkillName} used (Directional - not fully implemented yet)");
        }

        private void ExecuteGroundTargetedSkill(SkillDefinition skill, Transform target)
        {
            // TODO: Implement ground-targeted AOE
            Vector3 targetPosition = target != null ? target.position : transform.position + transform.forward * skill.Range;

            SpawnSkillEffect(skill, targetPosition);

            Debug.Log($"[SkillSystem] {skill.SkillName} used at {targetPosition} (Ground Target - not fully implemented yet)");
        }

        #endregion

        #region Visual Effects

        private void PlaySkillAnimation(SkillDefinition skill)
        {
            if (_animator != null && !string.IsNullOrEmpty(skill.AnimationTrigger))
            {
                _animator.SetTrigger(skill.AnimationTrigger);
            }
        }

        private void SpawnSkillEffect(SkillDefinition skill, Vector3 position)
        {
            if (skill.ParticleEffectPrefab != null)
            {
                GameObject effect = Instantiate(skill.ParticleEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 3f); // Auto-cleanup after 3 seconds
            }
        }

        #endregion

        #region Skill Slot Management

        /// <summary>
        /// Gets the skill in the specified slot (0-5).
        /// </summary>
        public SkillDefinition GetSkill(int skillSlot)
        {
            return skillSlot switch
            {
                0 => skill1,
                1 => skill2,
                2 => skill3,
                3 => skill4,
                4 => skill5,
                5 => skill6,
                _ => null
            };
        }

        /// <summary>
        /// Assigns a skill to a specific slot (for future class-based loading).
        /// </summary>
        public void SetSkill(int skillSlot, SkillDefinition skill)
        {
            switch (skillSlot)
            {
                case 0: skill1 = skill; break;
                case 1: skill2 = skill; break;
                case 2: skill3 = skill; break;
                case 3: skill4 = skill; break;
                case 4: skill5 = skill; break;
                case 5: skill6 = skill; break;
            }

            Debug.Log($"[SkillSystem] Skill slot {skillSlot + 1} set to: {(skill != null ? skill.SkillName : "Empty")}");
        }

        /// <summary>
        /// Loads skills based on character class (future implementation).
        /// </summary>
        public void LoadSkillsForClass(CharacterClass characterClass)
        {
            // TODO: Implement class-based skill loading
            // For now, this is a placeholder
            Debug.Log($"[SkillSystem] Loading skills for class: {characterClass} (not yet implemented)");
        }

        #endregion

        #region Cooldown Queries

        /// <summary>
        /// Checks if a skill is currently on cooldown.
        /// </summary>
        public bool IsOnCooldown(int skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= 6) return true;
            return _cooldownTimers[skillSlot] > 0f;
        }

        /// <summary>
        /// Gets remaining cooldown time for a skill.
        /// </summary>
        public float GetCooldownRemaining(int skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= 6) return 0f;
            return _cooldownTimers[skillSlot];
        }

        /// <summary>
        /// Gets the total cooldown duration for a skill.
        /// </summary>
        public float GetSkillCooldown(int skillSlot)
        {
            SkillDefinition skill = GetSkill(skillSlot);
            if (skill == null) return 0f;

            return DamageCalculator.CalculateCooldown(skill.Cooldown, _combatStats != null ? _combatStats.CooldownReduction : 1f);
        }

        #endregion

        #region Range Validation

        private bool IsTargetInRange(Transform target, float range)
        {
            if (target == null) return false;

            return DamageCalculator.IsInRange(transform.position, target.position, range);
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log All Skills")]
        private void LogAllSkills()
        {
            Debug.Log("=== Equipped Skills ===");
            for (int i = 0; i < 6; i++)
            {
                SkillDefinition skill = GetSkill(i);
                if (skill != null)
                {
                    Debug.Log($"Slot {i + 1}: {skill.SkillName} - CD: {GetCooldownRemaining(i):F1}s / {GetSkillCooldown(i):F1}s");
                }
                else
                {
                    Debug.Log($"Slot {i + 1}: Empty");
                }
            }
        }

        [ContextMenu("Use Skill 1")]
        private void DebugUseSkill1() => UseSkill(0);

        #endregion
    }
}