using UnityEngine;

namespace Magikill.Camera
{
    /// <summary>
    /// Third-person camera controller with smooth following, rotation, and obstacle avoidance.
    /// Supports hybrid control: fixed offset with optional two-finger rotation and auto snap-back.
    /// Attach to the Main Camera GameObject.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Configuration

        [Header("Follow Target")]
        [SerializeField]
        [Tooltip("The transform to follow (usually the player)")]
        private Transform target;

        [Header("Camera Positioning")]
        [SerializeField]
        [Tooltip("Distance behind the target")]
        private float followDistance = 10f;

        [SerializeField]
        [Tooltip("Height above the target")]
        private float followHeight = 8f;

        [SerializeField]
        [Tooltip("Smoothing speed for camera movement")]
        private float followDamping = 8f;

        [Header("Rotation")]
        [SerializeField]
        [Tooltip("Rotation speed for manual camera control")]
        private float rotationSpeed = 100f;

        [SerializeField]
        [Tooltip("Delay before camera snaps back to default position (seconds)")]
        private float snapBackDelay = 2f;

        [SerializeField]
        [Tooltip("Speed at which camera returns to default rotation")]
        private float snapBackSpeed = 3f;

        [Header("Obstacle Avoidance")]
        [SerializeField]
        [Tooltip("Enable raycast obstacle detection")]
        private bool enableObstacleAvoidance = true;

        [SerializeField]
        [Tooltip("Layers that block the camera")]
        private LayerMask obstacleLayers = ~0; // Everything by default

        [SerializeField]
        [Tooltip("Camera radius for collision detection")]
        private float cameraRadius = 0.5f;

        #endregion

        #region State

        private float _currentYaw = 0f; // Current horizontal rotation
        private float _targetYaw = 0f; // Target rotation from input
        private float _timeSinceLastRotation = 0f;
        private bool _isRotating = false;

        private float _currentDistance; // Current distance (modified by obstacles)
        private float _targetDistance; // Target distance (default or obstacle-adjusted)

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the target transform to follow.
        /// </summary>
        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Checks if camera has a valid target.
        /// </summary>
        public bool HasTarget => target != null;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _currentYaw = 0f;
            _targetYaw = 0f;
            _currentDistance = followDistance;
            _targetDistance = followDistance;

            if (target == null)
            {
                Debug.LogWarning("[CameraController] No target assigned. Camera will not follow anything.");
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleRotationInput();
            HandleSnapBack();
            HandleObstacleAvoidance();
            UpdateCameraPosition();
        }

        #endregion

        #region Rotation Input

        /// <summary>
        /// Handles manual camera rotation input (will be replaced with touch input later).
        /// For now, uses Q/E keys for testing.
        /// </summary>
        private void HandleRotationInput()
        {
            // TODO: Replace with two-finger touch input for mobile
            // For now, use Q/E keys for testing rotation
            float rotationInput = 0f;

            if (Input.GetKey(KeyCode.Q))
            {
                rotationInput = -1f; // Rotate left
            }
            else if (Input.GetKey(KeyCode.E))
            {
                rotationInput = 1f; // Rotate right
            }

            if (Mathf.Abs(rotationInput) > 0.01f)
            {
                _isRotating = true;
                _timeSinceLastRotation = 0f;
                _targetYaw += rotationInput * rotationSpeed * Time.deltaTime;
            }
            else
            {
                _isRotating = false;
                _timeSinceLastRotation += Time.deltaTime;
            }

            // Smoothly interpolate current yaw toward target
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, followDamping * Time.deltaTime);
        }

        #endregion

        #region Snap Back

        /// <summary>
        /// Handles automatic snap-back to default camera position after delay.
        /// </summary>
        private void HandleSnapBack()
        {
            // If not currently rotating and delay has passed, snap back to default
            if (!_isRotating && _timeSinceLastRotation >= snapBackDelay)
            {
                // Smoothly return target yaw to 0 (behind player)
                _targetYaw = Mathf.LerpAngle(_targetYaw, 0f, snapBackSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Obstacle Avoidance

        /// <summary>
        /// Uses raycast to detect obstacles between camera and player.
        /// Adjusts camera distance to avoid clipping through geometry.
        /// </summary>
        private void HandleObstacleAvoidance()
        {
            if (!enableObstacleAvoidance)
            {
                _targetDistance = followDistance;
                return;
            }

            // Calculate desired camera position
            Vector3 desiredPosition = CalculateDesiredPosition(followDistance);
            Vector3 direction = desiredPosition - target.position;
            float maxDistance = direction.magnitude;

            // Raycast from player to desired camera position
            if (Physics.SphereCast(target.position, cameraRadius, direction.normalized, out RaycastHit hit, maxDistance, obstacleLayers))
            {
                // Obstacle detected - move camera closer
                _targetDistance = hit.distance - cameraRadius;
                _targetDistance = Mathf.Max(_targetDistance, 1f); // Minimum distance of 1 unit
            }
            else
            {
                // No obstacle - use default distance
                _targetDistance = followDistance;
            }

            // Snap in quickly when blocked, smooth out when clear
            if (_targetDistance < _currentDistance)
            {
                // Snap closer instantly when obstacle detected
                _currentDistance = _targetDistance;
            }
            else
            {
                // Smoothly return to normal distance when clear
                _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, followDamping * Time.deltaTime);
            }
        }

        #endregion

        #region Camera Positioning

        /// <summary>
        /// Updates the camera position based on target, rotation, and distance.
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Calculate desired position with current rotation and distance
            Vector3 desiredPosition = CalculateDesiredPosition(_currentDistance);

            // Smoothly move camera toward desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followDamping * Time.deltaTime);

            // Always look at target
            transform.LookAt(target.position + Vector3.up * 1.5f); // Look slightly above target center
        }

        /// <summary>
        /// Calculates the desired camera position based on rotation and distance.
        /// </summary>
        private Vector3 CalculateDesiredPosition(float distance)
        {
            // Apply yaw rotation to create offset
            Quaternion rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, followHeight, -distance);

            return target.position + offset;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera to follow a new target.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (newTarget != null)
            {
                Debug.Log($"[CameraController] Now following: {newTarget.name}");
            }
        }

        /// <summary>
        /// Resets camera rotation to default (behind player).
        /// </summary>
        public void ResetRotation()
        {
            _currentYaw = 0f;
            _targetYaw = 0f;
            _timeSinceLastRotation = 0f;
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Camera State")]
        private void LogCameraState()
        {
            Debug.Log("=== Camera State ===");
            Debug.Log($"Target: {(target != null ? target.name : "None")}");
            Debug.Log($"Current Yaw: {_currentYaw}");
            Debug.Log($"Target Yaw: {_targetYaw}");
            Debug.Log($"Current Distance: {_currentDistance}");
            Debug.Log($"Target Distance: {_targetDistance}");
            Debug.Log($"Is Rotating: {_isRotating}");
            Debug.Log($"Time Since Last Rotation: {_timeSinceLastRotation}");
        }

        private void OnDrawGizmos()
        {
            if (target == null || !enableObstacleAvoidance)
            {
                return;
            }

            // Draw raycast line for debugging obstacle detection
            Vector3 desiredPosition = CalculateDesiredPosition(followDistance);
            Vector3 direction = desiredPosition - target.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position, desiredPosition);

            // Draw sphere at camera position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, cameraRadius);
        }

        #endregion
    }
}
