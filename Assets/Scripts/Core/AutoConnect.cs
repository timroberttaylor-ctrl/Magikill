using UnityEngine;
using Magikill.Core;

namespace Magikill.Core
{
    /// <summary>
    /// Automatically connects to the network after a short delay.
    /// Useful for testing. Remove in production and replace with proper UI.
    /// </summary>
    public class AutoConnect : MonoBehaviour
    {
        [Header("Auto-Connect Settings")]
        [SerializeField]
        [Tooltip("Delay before attempting connection")]
        private float connectDelay = 1.5f;

        [SerializeField]
        [Tooltip("Enable auto-connect (disable this in production)")]
        private bool enableAutoConnect = true;

        private bool _hasAttemptedConnection = false;

        private void Start()
        {
            if (enableAutoConnect && !_hasAttemptedConnection)
            {
                Debug.Log($"[AutoConnect] Will attempt connection in {connectDelay} seconds...");
                Invoke(nameof(AttemptConnection), connectDelay);
            }
        }

        private void AttemptConnection()
        {
            if (_hasAttemptedConnection)
            {
                return;
            }

            _hasAttemptedConnection = true;

            var networkManager = GameManager.GetService<NetworkManager>();

            if (networkManager == null)
            {
                Debug.LogError("[AutoConnect] NetworkManager not found! Make sure GameManager is initialized.");
                return;
            }

            if (networkManager.IsServer)
            {
                //commented out server start until solved
                //Debug.Log("[AutoConnect] Starting server...");
                // networkManager.StartServer();
                if (networkManager.IsServer)
                {
                    Debug.Log("[AutoConnect] Starting host...");
                    networkManager.StartHost();
                }
            }
            else
            {
                Debug.Log("[AutoConnect] Connecting to server as client...");
                networkManager.ConnectAsClient();
            }
        }

        // Manual button support (optional)
        public void ManualConnect()
        {
            AttemptConnection();
        }
    }
}