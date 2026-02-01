using System;
using UnityEngine;

namespace Magikill.UI
{
    /// <summary>
    /// Base class for all UI panels.
    /// Handles animated show/hide transitions using Unity Animator.
    /// Provides callbacks for when transitions complete.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Animator))]
    public class UIPanel : MonoBehaviour
    {
        #region Animator Parameters

        // Animator parameter names (must match your Animator Controller)
        private const string SHOW_TRIGGER = "Show";
        private const string HIDE_TRIGGER = "Hide";
        private const string IS_VISIBLE_BOOL = "IsVisible";

        #endregion

        #region Components

        protected CanvasGroup canvasGroup;
        protected Animator animator;

        #endregion

        #region State

        private bool _isVisible = false;
        private bool _isTransitioning = false;

        public bool IsVisible => _isVisible;
        public bool IsTransitioning => _isTransitioning;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the panel finishes showing (fade-in complete).
        /// </summary>
        public event Action OnShowComplete;

        /// <summary>
        /// Fired when the panel finishes hiding (fade-out complete).
        /// </summary>
        public event Action OnHideComplete;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            animator = GetComponent<Animator>();

            // Start hidden by default
            if (!_isVisible)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        #endregion

        #region Show/Hide Methods

        /// <summary>
        /// Shows the panel with animation.
        /// </summary>
        public virtual void Show()
        {
            if (_isVisible || _isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _isVisible = true;

            // Enable interaction
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Trigger show animation
            if (animator != null)
            {
                animator.SetBool(IS_VISIBLE_BOOL, true);
                animator.SetTrigger(SHOW_TRIGGER);
            }
            else
            {
                // Fallback if no animator
                canvasGroup.alpha = 1f;
                OnShowAnimationComplete();
            }

            Debug.Log($"[UIPanel] Showing: {gameObject.name}");
        }

        /// <summary>
        /// Hides the panel with animation.
        /// </summary>
        public virtual void Hide()
        {
            if (!_isVisible || _isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _isVisible = false;

            // Disable interaction immediately
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Trigger hide animation
            if (animator != null)
            {
                animator.SetBool(IS_VISIBLE_BOOL, false);
                animator.SetTrigger(HIDE_TRIGGER);
            }
            else
            {
                // Fallback if no animator
                canvasGroup.alpha = 0f;
                OnHideAnimationComplete();
            }

            Debug.Log($"[UIPanel] Hiding: {gameObject.name}");
        }

        /// <summary>
        /// Instantly shows the panel without animation.
        /// </summary>
        public virtual void ShowImmediate()
        {
            _isVisible = true;
            _isTransitioning = false;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (animator != null)
            {
                animator.SetBool(IS_VISIBLE_BOOL, true);
            }

            Debug.Log($"[UIPanel] Showing immediately: {gameObject.name}");
        }

        /// <summary>
        /// Instantly hides the panel without animation.
        /// </summary>
        public virtual void HideImmediate()
        {
            _isVisible = false;
            _isTransitioning = false;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (animator != null)
            {
                animator.SetBool(IS_VISIBLE_BOOL, false);
            }

            Debug.Log($"[UIPanel] Hiding immediately: {gameObject.name}");
        }

        #endregion

        #region Animation Event Callbacks

        /// <summary>
        /// Called by Animation Event when show animation completes.
        /// Add this as an Animation Event in your show animation's last frame.
        /// </summary>
        public void OnShowAnimationComplete()
        {
            _isTransitioning = false;
            OnShowComplete?.Invoke();
            Debug.Log($"[UIPanel] Show complete: {gameObject.name}");
        }

        /// <summary>
        /// Called by Animation Event when hide animation completes.
        /// Add this as an Animation Event in your hide animation's last frame.
        /// </summary>
        public void OnHideAnimationComplete()
        {
            _isTransitioning = false;
            OnHideComplete?.Invoke();
            Debug.Log($"[UIPanel] Hide complete: {gameObject.name}");
        }

        #endregion

        #region Toggle

        /// <summary>
        /// Toggles panel visibility (show if hidden, hide if visible).
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Show Panel")]
        private void DebugShow()
        {
            Show();
        }

        [ContextMenu("Hide Panel")]
        private void DebugHide()
        {
            Hide();
        }

        [ContextMenu("Toggle Panel")]
        private void DebugToggle()
        {
            Toggle();
        }

        #endregion
    }
}
