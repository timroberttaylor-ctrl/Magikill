using System.Collections.Generic;
using UnityEngine;
using Magikill.Core;

namespace Magikill.UI
{
    /// <summary>
    /// Manages all UI panels and provides centralized UI control.
    /// Handles panel registration, show/hide coordination, and HUD access.
    /// Priority: 15 (UI systems tier)
    /// </summary>
    public class UIManager : MonoBehaviour, IGameService
    {
        #region IGameService Implementation

        public int Priority => 15;

        #endregion

        #region Configuration

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Reference to the main HUD (always visible)")]
        private HUDPanel hudPanel;

        #endregion

        #region Panel Tracking

        private Dictionary<string, UIPanel> _registeredPanels = new Dictionary<string, UIPanel>();
        private bool _isInitialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// Quick access to the HUD panel.
        /// </summary>
        public static HUDPanel HUD { get; private set; }

        #endregion

        #region IGameService Lifecycle

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[UIManager] Already initialized.");
                return;
            }

            Debug.Log("[UIManager] Initializing...");

            // Set HUD reference
            if (hudPanel != null)
            {
                HUD = hudPanel;
                RegisterPanel("HUD", hudPanel);
                hudPanel.ShowImmediate(); // HUD is always visible
                Debug.Log("[UIManager] HUD registered and shown.");
            }
            else
            {
                Debug.LogWarning("[UIManager] HUD panel reference is not assigned!");
            }

            // Auto-register all UIPanel components in children
            AutoRegisterPanels();

            _isInitialized = true;
            Debug.Log($"[UIManager] Initialization complete. Registered {_registeredPanels.Count} panels.");
        }

        public void Shutdown()
        {
            Debug.Log("[UIManager] Shutting down...");

            // Hide all panels
            foreach (var panel in _registeredPanels.Values)
            {
                if (panel != null)
                {
                    panel.HideImmediate();
                }
            }

            _registeredPanels.Clear();
            HUD = null;
            _isInitialized = false;

            Debug.Log("[UIManager] Shutdown complete.");
        }

        #endregion

        #region Panel Registration

        /// <summary>
        /// Automatically finds and registers all UIPanel components in children.
        /// </summary>
        private void AutoRegisterPanels()
        {
            UIPanel[] panels = GetComponentsInChildren<UIPanel>(true);

            foreach (UIPanel panel in panels)
            {
                // Skip if already registered (like HUD)
                if (_registeredPanels.ContainsValue(panel))
                {
                    continue;
                }

                string panelName = panel.gameObject.name;
                RegisterPanel(panelName, panel);
            }

            Debug.Log($"[UIManager] Auto-registered {panels.Length} panels.");
        }

        /// <summary>
        /// Registers a panel with the UIManager.
        /// </summary>
        public void RegisterPanel(string panelName, UIPanel panel)
        {
            if (_registeredPanels.ContainsKey(panelName))
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' already registered. Replacing.");
                _registeredPanels[panelName] = panel;
            }
            else
            {
                _registeredPanels.Add(panelName, panel);
                Debug.Log($"[UIManager] Registered panel: {panelName}");
            }
        }

        /// <summary>
        /// Unregisters a panel from the UIManager.
        /// </summary>
        public void UnregisterPanel(string panelName)
        {
            if (_registeredPanels.ContainsKey(panelName))
            {
                _registeredPanels.Remove(panelName);
                Debug.Log($"[UIManager] Unregistered panel: {panelName}");
            }
        }

        #endregion

        #region Panel Control

        /// <summary>
        /// Shows a panel by name.
        /// </summary>
        public void ShowPanel(string panelName)
        {
            if (_registeredPanels.TryGetValue(panelName, out UIPanel panel))
            {
                panel.Show();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' not found!");
            }
        }

        /// <summary>
        /// Hides a panel by name.
        /// </summary>
        public void HidePanel(string panelName)
        {
            if (_registeredPanels.TryGetValue(panelName, out UIPanel panel))
            {
                panel.Hide();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' not found!");
            }
        }

        /// <summary>
        /// Toggles a panel's visibility by name.
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (_registeredPanels.TryGetValue(panelName, out UIPanel panel))
            {
                panel.Toggle();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' not found!");
            }
        }

        /// <summary>
        /// Gets a panel by name.
        /// </summary>
        public UIPanel GetPanel(string panelName)
        {
            _registeredPanels.TryGetValue(panelName, out UIPanel panel);
            return panel;
        }

        /// <summary>
        /// Gets a panel of a specific type.
        /// </summary>
        public T GetPanel<T>() where T : UIPanel
        {
            foreach (var panel in _registeredPanels.Values)
            {
                if (panel is T typedPanel)
                {
                    return typedPanel;
                }
            }

            return null;
        }

        /// <summary>
        /// Hides all panels except the HUD.
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var kvp in _registeredPanels)
            {
                // Don't hide HUD
                if (kvp.Key == "HUD" || kvp.Value == HUD)
                {
                    continue;
                }

                kvp.Value.Hide();
            }

            Debug.Log("[UIManager] All panels hidden (except HUD).");
        }

        #endregion

        #region Static Access

        /// <summary>
        /// Gets the UIManager instance via GameManager.
        /// </summary>
        public static UIManager Instance => GameManager.GetService<UIManager>();

        #endregion

        #region Debug Utilities

        [ContextMenu("Log All Panels")]
        private void LogAllPanels()
        {
            Debug.Log("=== Registered UI Panels ===");
            Debug.Log($"Total: {_registeredPanels.Count}");

            foreach (var kvp in _registeredPanels)
            {
                string visibleState = kvp.Value.IsVisible ? "VISIBLE" : "HIDDEN";
                Debug.Log($"- {kvp.Key}: {visibleState}");
            }
        }

        [ContextMenu("Hide All Panels")]
        private void DebugHideAllPanels()
        {
            HideAllPanels();
        }

        #endregion
    }
}
