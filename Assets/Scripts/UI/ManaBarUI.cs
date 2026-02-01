using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Magikill.Combat;

namespace Magikill.UI
{
    /// <summary>
    /// Displays player mana as a filled bar.
    /// Inherits visual functionality from HealthBarUI but subscribes to mana events.
    /// Uses dual images (background + fill) for professional appearance.
    /// </summary>
    public class ManaBarUI : MonoBehaviour
    {
        #region UI Components

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Background image for the mana bar")]
        private Image backgroundImage;

        [SerializeField]
        [Tooltip("Fill image that represents current mana")]
        private Image fillImage;

        [SerializeField]
        [Tooltip("Optional text displaying mana values (e.g., '80/100')")]
        private TextMeshProUGUI manaText;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color when mana is full")]
        private Color fullManaColor = Color.cyan;

        [SerializeField]
        [Tooltip("Color when mana is low")]
        private Color lowManaColor = Color.blue;

        [SerializeField]
        [Tooltip("Mana percentage threshold for low mana (0-1)")]
        [Range(0f, 1f)]
        private float lowManaThreshold = 0.3f;

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
        /// Initializes the mana bar and subscribes to CombatStats events.
        /// </summary>
        public void Initialize(CombatStats combatStats)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ManaBarUI] Already initialized!");
                return;
            }

            if (combatStats == null)
            {
                Debug.LogError("[ManaBarUI] CombatStats is null!");
                return;
            }

            _combatStats = combatStats;

            // Subscribe to mana change events (different from health)
            _combatStats.OnManaChanged += OnManaChanged;

            // Set initial values
            UpdateManaDisplay(_combatStats.CurrentMana, _combatStats.MaxMana, 0f);

            _isInitialized = true;

            Debug.Log("[ManaBarUI] Initialized and subscribed to CombatStats.");
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Cleanup()
        {
            if (_combatStats != null)
            {
                _combatStats.OnManaChanged -= OnManaChanged;
            }

            _isInitialized = false;

            Debug.Log("[ManaBarUI] Cleaned up.");
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
        /// Called when mana changes in CombatStats.
        /// </summary>
        private void OnManaChanged(float currentMana, float maxMana, float delta)
        {
            UpdateManaDisplay(currentMana, maxMana, delta);
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Updates the visual display of the mana bar.
        /// </summary>
        private void UpdateManaDisplay(float currentMana, float maxMana, float delta)
        {
            // Calculate fill amount (0-1)
            float manaPercent = maxMana > 0 ? currentMana / maxMana : 0f;
            _targetFillAmount = Mathf.Clamp01(manaPercent);

            // Update mana text
            if (manaText != null)
            {
                manaText.text = $"{Mathf.CeilToInt(currentMana)}/{Mathf.CeilToInt(maxMana)}";
            }

            // Update fill color based on mana percentage
            UpdateFillColor(manaPercent);

            // Optional: Add visual feedback for mana consumption/regeneration
            if (delta < 0)
            {
                // Consumed mana - could trigger flash effect here
            }
            else if (delta > 0)
            {
                // Regenerated mana - could trigger shimmer effect here
            }
        }

        /// <summary>
        /// Updates the fill image color based on current mana percentage.
        /// </summary>
        private void UpdateFillColor(float manaPercent)
        {
            if (fillImage == null) return;

            // Interpolate between low and full mana colors
            if (manaPercent <= lowManaThreshold)
            {
                // Lerp from blue to cyan based on percentage within low mana range
                float t = manaPercent / lowManaThreshold;
                fillImage.color = Color.Lerp(lowManaColor, fullManaColor, t);
            }
            else
            {
                // Full mana color
                fillImage.color = fullManaColor;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually updates the mana bar (for testing).
        /// </summary>
        public void SetMana(float current, float max)
        {
            UpdateManaDisplay(current, max, 0f);
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Test - Set 50% Mana")]
        private void TestHalfMana()
        {
            SetMana(50f, 100f);
        }

        [ContextMenu("Test - Set Low Mana")]
        private void TestLowMana()
        {
            SetMana(20f, 100f);
        }

        [ContextMenu("Test - Set Full Mana")]
        private void TestFullMana()
        {
            SetMana(100f, 100f);
        }

        #endregion
    }
}
