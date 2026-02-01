using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Magikill.Combat;

namespace Magikill.UI
{
    /// <summary>
    /// Displays player health as a filled bar.
    /// Uses dual images (background + fill) for professional appearance.
    /// Updates via CombatStats events.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        #region UI Components

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Background image for the health bar")]
        private Image backgroundImage;

        [SerializeField]
        [Tooltip("Fill image that represents current health")]
        private Image fillImage;

        [SerializeField]
        [Tooltip("Optional text displaying health values (e.g., '80/100')")]
        private TextMeshProUGUI healthText;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color when health is full")]
        private Color fullHealthColor = Color.green;

        [SerializeField]
        [Tooltip("Color when health is low")]
        private Color lowHealthColor = Color.red;

        [SerializeField]
        [Tooltip("Health percentage threshold for low health (0-1)")]
        [Range(0f, 1f)]
        private float lowHealthThreshold = 0.3f;

        [SerializeField]
        [Tooltip("Smooth fill animation speed")]
        private float fillSpeed = 5f;

        #endregion

        #region State

        private CombatStats _combatStats;
        private float _targetFillAmount = 1f;
        private float _currentFillAmount = 1f;
        private bool _isInitialized = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the health bar and subscribes to CombatStats events.
        /// </summary>
        public void Initialize(CombatStats combatStats)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[HealthBarUI] Already initialized!");
                return;
            }

            if (combatStats == null)
            {
                Debug.LogError("[HealthBarUI] CombatStats is null!");
                return;
            }

            _combatStats = combatStats;

            // Subscribe to health change events
            _combatStats.OnHealthChanged += OnHealthChanged;

            // Set initial values
            UpdateHealthDisplay(_combatStats.CurrentHealth, _combatStats.MaxHealth, 0f);

            _isInitialized = true;

            Debug.Log("[HealthBarUI] Initialized and subscribed to CombatStats.");
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Cleanup()
        {
            if (_combatStats != null)
            {
                _combatStats.OnHealthChanged -= OnHealthChanged;
            }

            _isInitialized = false;

            Debug.Log("[HealthBarUI] Cleaned up.");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Update()
        {
            // Smoothly animate fill amount
            if (Mathf.Abs(_currentFillAmount - _targetFillAmount) > 0.001f)
            {
                _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, fillSpeed * Time.deltaTime);
                
                if (fillImage != null)
                {
                    fillImage.fillAmount = _currentFillAmount;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when health changes in CombatStats.
        /// </summary>
        private void OnHealthChanged(float currentHealth, float maxHealth, float delta)
        {
            UpdateHealthDisplay(currentHealth, maxHealth, delta);
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Updates the visual display of the health bar.
        /// </summary>
        private void UpdateHealthDisplay(float currentHealth, float maxHealth, float delta)
        {
            // Calculate fill amount (0-1)
            float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            _targetFillAmount = Mathf.Clamp01(healthPercent);

            // Update health text
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }

            // Update fill color based on health percentage
            UpdateFillColor(healthPercent);

            // Optional: Add visual feedback for damage/healing
            if (delta < 0)
            {
                // Took damage - could trigger shake/flash effect here
            }
            else if (delta > 0)
            {
                // Healed - could trigger glow effect here
            }
        }

        /// <summary>
        /// Updates the fill image color based on current health percentage.
        /// </summary>
        private void UpdateFillColor(float healthPercent)
        {
            if (fillImage == null) return;

            // Interpolate between low and full health colors
            if (healthPercent <= lowHealthThreshold)
            {
                // Lerp from red to green based on percentage within low health range
                float t = healthPercent / lowHealthThreshold;
                fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, t);
            }
            else
            {
                // Full health color
                fillImage.color = fullHealthColor;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually updates the health bar (for testing).
        /// </summary>
        public void SetHealth(float current, float max)
        {
            UpdateHealthDisplay(current, max, 0f);
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Test - Set 50% Health")]
        private void TestHalfHealth()
        {
            SetHealth(50f, 100f);
        }

        [ContextMenu("Test - Set Low Health")]
        private void TestLowHealth()
        {
            SetHealth(20f, 100f);
        }

        [ContextMenu("Test - Set Full Health")]
        private void TestFullHealth()
        {
            SetHealth(100f, 100f);
        }

        #endregion
    }
}
