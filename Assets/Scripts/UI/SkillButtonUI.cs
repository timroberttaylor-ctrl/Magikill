using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Magikill.Combat;

namespace Magikill.UI
{
    /// <summary>
    /// Individual skill button UI component.
    /// Displays skill icon, radial cooldown overlay, and mana availability.
    /// Visual-only for MVP (keyboard controls via 1-6 keys).
    /// Click functionality will be added later for mobile touch controls.
    /// </summary>
    public class SkillButtonUI : MonoBehaviour
    {
        #region UI Components

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Background image for the button")]
        private Image backgroundImage;

        [SerializeField]
        [Tooltip("Skill icon image")]
        private Image iconImage;

        [SerializeField]
        [Tooltip("Radial cooldown overlay (Image with Filled type)")]
        private Image cooldownOverlay;

        [SerializeField]
        [Tooltip("Optional keybind text (e.g., '1', '2')")]
        private TextMeshProUGUI keybindText;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color when skill is ready")]
        private Color readyColor = Color.white;

        [SerializeField]
        [Tooltip("Color when skill is on cooldown")]
        private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField]
        [Tooltip("Color when not enough mana")]
        private Color noManaColor = new Color(0.3f, 0.3f, 0.8f, 1f);

        #endregion

        #region State

        private int _skillSlot;
        private SkillDefinition _skill;
        private CombatStats _combatStats;
        private bool _isInitialized = false;

        private float _currentCooldown = 0f;
        private float _totalCooldown = 0f;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the skill button.
        /// </summary>
        public void Initialize(int skillSlot, SkillDefinition skill, CombatStats combatStats)
        {
            _skillSlot = skillSlot;
            _skill = skill;
            _combatStats = combatStats;

            // Set keybind text
            if (keybindText != null)
            {
                keybindText.text = (skillSlot + 1).ToString();
            }

            // Setup cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.type = Image.Type.Filled;
                cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                cooldownOverlay.fillClockwise = false;
                cooldownOverlay.fillAmount = 0f; // Start with no cooldown
            }

            // Set skill icon
            SetSkill(skill);

            _isInitialized = true;

            Debug.Log($"[SkillButtonUI] Initialized slot {skillSlot + 1}");
        }

        /// <summary>
        /// Cleans up the button.
        /// </summary>
        public void Cleanup()
        {
            _isInitialized = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isInitialized) return;

            // Update visual state based on mana availability
            UpdateVisualState();
        }

        #endregion

        #region Skill Management

        /// <summary>
        /// Sets or updates the skill for this button.
        /// </summary>
        public void SetSkill(SkillDefinition skill)
        {
            _skill = skill;

            // Update icon
            if (iconImage != null)
            {
                if (_skill != null && _skill.Icon != null)
                {
                    iconImage.sprite = _skill.Icon;
                    iconImage.enabled = true;
                }
                else
                {
                    // No skill or no icon - show empty
                    iconImage.enabled = false;
                }
            }

            // Reset cooldown
            _currentCooldown = 0f;
            _totalCooldown = 0f;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }

            UpdateVisualState();
        }

        #endregion

        #region Cooldown Display

        /// <summary>
        /// Updates the cooldown overlay fill amount.
        /// </summary>
        public void UpdateCooldown(float remainingTime, float totalCooldown)
        {
            _currentCooldown = remainingTime;
            _totalCooldown = totalCooldown;

            if (cooldownOverlay != null && totalCooldown > 0f)
            {
                // Fill amount goes from 1 (full cooldown) to 0 (ready)
                float fillAmount = remainingTime / totalCooldown;
                cooldownOverlay.fillAmount = Mathf.Clamp01(fillAmount);

                // Make overlay visible during cooldown
                cooldownOverlay.enabled = fillAmount > 0f;

                // Debug logging
                Debug.Log($"[SkillButtonUI] Slot {_skillSlot + 1} Cooldown: {remainingTime:F2}s / {totalCooldown:F2}s, Fill: {fillAmount:F2}, Enabled: {cooldownOverlay.enabled}");
            }
            else if (cooldownOverlay != null)
            {
                // No cooldown - hide overlay
                cooldownOverlay.enabled = false;
            }

            UpdateVisualState();
        }

        #endregion

        #region Visual State

        /// <summary>
        /// Updates the visual appearance based on skill state.
        /// </summary>
        private void UpdateVisualState()
        {
            if (_skill == null || iconImage == null)
            {
                return;
            }

            // Check cooldown state
            bool isOnCooldown = _currentCooldown > 0f;

            // Check mana availability
            bool hasEnoughMana = _combatStats != null && _combatStats.HasEnoughMana(_skill.ManaCost);

            // Determine color
            Color targetColor;

            if (isOnCooldown)
            {
                // On cooldown - gray
                targetColor = cooldownColor;
            }
            else if (!hasEnoughMana)
            {
                // Not enough mana - blue tint
                targetColor = noManaColor;
            }
            else
            {
                // Ready to use - white
                targetColor = readyColor;
            }

            // Apply color to icon
            iconImage.color = targetColor;
        }

        #endregion

        #region Event Callbacks

        /// <summary>
        /// Called when this skill is used.
        /// </summary>
        public void OnSkillUsed()
        {
            // Could trigger visual feedback here (flash, pulse, etc.)
            Debug.Log($"[SkillButtonUI] Skill {_skillSlot + 1} used!");
        }

        /// <summary>
        /// Called when skill cast fails.
        /// </summary>
        public void OnCastFailed(string reason)
        {
            // Could trigger error visual feedback here (shake, red flash, etc.)
            Debug.Log($"[SkillButtonUI] Skill {_skillSlot + 1} failed: {reason}");
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Test - Start Cooldown")]
        private void TestStartCooldown()
        {
            UpdateCooldown(5f, 5f); // 5 second cooldown
        }

        [ContextMenu("Test - Clear Cooldown")]
        private void TestClearCooldown()
        {
            UpdateCooldown(0f, 5f);
        }

        #endregion
    }
}