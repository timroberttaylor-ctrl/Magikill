using UnityEngine;
using Magikill.Camera;
using NetworkPlayerClass = Magikill.Networking.NetworkPlayer;

namespace Magikill.Core
{
    /// <summary>
    /// Manages camera assignment and lifecycle.
    /// Automatically finds the Main Camera and assigns it to follow the local player.
    /// Priority: 10 (visual systems tier)
    /// </summary>
    public class CameraManager : MonoBehaviour, IGameService
    {
        #region IGameService Implementation

        public int Priority => 10;

        #endregion

        #region References

        private CameraController _cameraController;
        private UnityEngine.Camera _mainCamera;
        private NetworkPlayerClass _localPlayer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current camera controller.
        /// </summary>
        public CameraController CameraController => _cameraController;

        /// <summary>
        /// Gets the main camera.
        /// </summary>
        public UnityEngine.Camera MainCamera => _mainCamera;

        /// <summary>
        /// Checks if camera is currently following a player.
        /// </summary>
        public bool IsFollowingPlayer => _cameraController != null && _cameraController.HasTarget;

        #endregion

        #region IGameService Lifecycle

        public void Initialize()
        {
            Debug.Log("[CameraManager] Initializing...");

            // Find the Main Camera
            _mainCamera = UnityEngine.Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogError("[CameraManager] Main Camera not found! Ensure a camera is tagged as 'MainCamera'.");
                return;
            }

            // Get or add CameraController component
            _cameraController = _mainCamera.GetComponent<CameraController>();

            if (_cameraController == null)
            {
                Debug.Log("[CameraManager] CameraController not found on Main Camera. Adding component...");
                _cameraController = _mainCamera.gameObject.AddComponent<CameraController>();
            }

            Debug.Log("[CameraManager] Initialization complete.");
        }

        public void Shutdown()
        {
            Debug.Log("[CameraManager] Shutting down...");

            _cameraController = null;
            _mainCamera = null;
            _localPlayer = null;

            Debug.Log("[CameraManager] Shutdown complete.");
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // If we don't have a local player assigned yet, try to find one
            if (_localPlayer == null && _cameraController != null)
            {
                TryFindLocalPlayer();
            }
        }

        #endregion

        #region Player Assignment

        /// <summary>
        /// Attempts to find and assign the local player if one exists.
        /// </summary>
        private void TryFindLocalPlayer()
        {
            // Find all NetworkPlayer objects
            NetworkPlayerClass[] allPlayers = FindObjectsOfType<NetworkPlayerClass>();

            foreach (NetworkPlayerClass player in allPlayers)
            {
                // Check if this is the local player (has input authority)
                if (player.HasInputAuthority)
                {
                    _localPlayer = player;
                    _cameraController.SetTarget(player.transform);
                    Debug.Log($"[CameraManager] Camera assigned to follow local player: {player.PlayerName}");
                    return;
                }
            }
        }

        /// <summary>
        /// Manually assigns the camera to follow a specific player.
        /// </summary>
        public void AssignCameraToPlayer(NetworkPlayerClass player)
        {
            if (_cameraController == null)
            {
                Debug.LogWarning("[CameraManager] CameraController not available. Cannot assign player.");
                return;
            }

            if (player == null)
            {
                Debug.LogWarning("[CameraManager] Cannot assign camera to null player.");
                return;
            }

            _localPlayer = player;
            _cameraController.SetTarget(player.transform);
            Debug.Log($"[CameraManager] Camera manually assigned to player: {player.PlayerName}");
        }

        /// <summary>
        /// Clears the camera target (stops following).
        /// </summary>
        public void ClearCameraTarget()
        {
            if (_cameraController != null)
            {
                _cameraController.SetTarget(null);
                Debug.Log("[CameraManager] Camera target cleared.");
            }

            _localPlayer = null;
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Find and Assign Local Player")]
        private void DebugFindLocalPlayer()
        {
            TryFindLocalPlayer();
        }

        [ContextMenu("Log Camera Status")]
        private void LogCameraStatus()
        {
            Debug.Log("=== Camera Status ===");
            Debug.Log($"Main Camera: {(_mainCamera != null ? _mainCamera.name : "None")}");
            Debug.Log($"Camera Controller: {(_cameraController != null ? "Present" : "Missing")}");
            Debug.Log($"Following Player: {IsFollowingPlayer}");
            Debug.Log($"Local Player: {(_localPlayer != null ? _localPlayer.PlayerName.ToString() : "None")}");
        }

        #endregion
    }
}