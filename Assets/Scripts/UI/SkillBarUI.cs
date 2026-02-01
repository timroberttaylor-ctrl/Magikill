using UnityEngine;
using Magikill.Combat;

namespace Magikill.UI
{
    /// <summary>
    /// Manages the skill bar UI with 6 skill buttons.
    /// Displays skill icons and radial cooldown overlays.
    /// Updates via SkillSystem events.
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        #region Configuration

        [Header("Skill Button References")]
        [SerializeField]
        [Tooltip("Array of 6 skill button UI components (in order: 1-6)")]
        private SkillButtonUI[] skillButtons = new SkillButtonUI[6];

        #endregion

        #region State

        private SkillSystem _skillSystem;
        private CombatStats _combatStats;
        private bool _isInitialized = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the skill bar and subscribes to SkillSystem events.
        /// </summary>
        public void Initialize(SkillSystem skillSystem, CombatStats combatStats)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[SkillBarUI] Already initialized!");
                return;
            }

            if (skillSystem == null)
            {
                Debug.LogError("[SkillBarUI] SkillSystem is null!");
                return;
            }

            if (combatStats == null)
            {
                Debug.LogError("[SkillBarUI] CombatStats is null!");
                return;
            }

            _skillSystem = skillSystem;
            _combatStats = combatStats;

            // Auto-find skill buttons if not assigned
            if (skillButtons == null || skillButtons.Length != 6)
            {
                skillButtons = GetComponentsInChildren<SkillButtonUI>();
            }

            // Verify we have 6 buttons
            if (skillButtons.Length != 6)
            {
                Debug.LogError($"[SkillBarUI] Expected 6 skill buttons, found {skillButtons.Length}!");
                return;
            }

            // Subscribe to skill system events
            _skillSystem.OnSkillUsed += OnSkillUsed;
            _skillSystem.OnCooldownUpdate += OnCooldownUpdate;
            _skillSystem.OnSkillCastFailed += OnSkillCastFailed;

            // Initialize each skill button
            for (int i = 0; i < 6; i++)
            {
                if (skillButtons[i] != null)
                {
                    SkillDefinition skill = _skillSystem.GetSkill(i);
                    skillButtons[i].Initialize(i, skill, _combatStats);
                }
                else
                {
                    Debug.LogWarning($"[SkillBarUI] Skill button {i + 1} is null!");
                }
            }

            _isInitialized = true;

            Debug.Log("[SkillBarUI] Initialized with 6 skill buttons.");
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Cleanup()
        {
            if (_skillSystem != null)
            {
                _skillSystem.OnSkillUsed -= OnSkillUsed;
                _skillSystem.OnCooldownUpdate -= OnCooldownUpdate;
                _skillSystem.OnSkillCastFailed -= OnSkillCastFailed;
            }

            // Cleanup each button
            if (skillButtons != null)
            {
                foreach (var button in skillButtons)
                {
                    if (button != null)
                    {
                        button.Cleanup();
                    }
                }
            }

            _isInitialized = false;

            Debug.Log("[SkillBarUI] Cleaned up.");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a skill is used.
        /// </summary>
        private void OnSkillUsed(int skillSlot, SkillDefinition skill)
        {
            if (skillSlot >= 0 && skillSlot < 6 && skillButtons[skillSlot] != null)
            {
                skillButtons[skillSlot].OnSkillUsed();
            }
        }

        /// <summary>
        /// Called when a skill's cooldown updates.
        /// </summary>
        private void OnCooldownUpdate(int skillSlot, float remainingTime, float totalCooldown)
        {
            if (skillSlot >= 0 && skillSlot < 6 && skillButtons[skillSlot] != null)
            {
                skillButtons[skillSlot].UpdateCooldown(remainingTime, totalCooldown);
            }
        }

        /// <summary>
        /// Called when a skill cast fails (cooldown, no mana, etc.)
        /// </summary>
        private void OnSkillCastFailed(int skillSlot, string reason)
        {
            if (skillSlot >= 0 && skillSlot < 6 && skillButtons[skillSlot] != null)
            {
                skillButtons[skillSlot].OnCastFailed(reason);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates a specific skill slot (useful when skills change).
        /// </summary>
        public void UpdateSkillSlot(int skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= 6)
            {
                Debug.LogWarning($"[SkillBarUI] Invalid skill slot: {skillSlot}");
                return;
            }

            if (_skillSystem == null || skillButtons[skillSlot] == null)
            {
                return;
            }

            SkillDefinition skill = _skillSystem.GetSkill(skillSlot);
            skillButtons[skillSlot].SetSkill(skill);
        }

        /// <summary>
        /// Refreshes all skill buttons (useful after class change).
        /// </summary>
        public void RefreshAllSkills()
        {
            for (int i = 0; i < 6; i++)
            {
                UpdateSkillSlot(i);
            }

            Debug.Log("[SkillBarUI] Refreshed all skill buttons.");
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Refresh All Skills")]
        private void DebugRefreshAllSkills()
        {
            RefreshAllSkills();
        }

        [ContextMenu("Log Skill Buttons")]
        private void LogSkillButtons()
        {
            Debug.Log("=== Skill Buttons ===");
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] != null)
                {
                    Debug.Log($"Slot {i + 1}: {skillButtons[i].name}");
                }
                else
                {
                    Debug.Log($"Slot {i + 1}: NULL");
                }
            }
        }

        #endregion
    }
}
