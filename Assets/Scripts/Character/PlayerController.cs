using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magikill.Character
{
    /// <summary>
    /// Handles player input collection and character movement.
    /// Collects input via INetworkRunnerCallbacks and processes movement on the server.
    /// Uses CharacterController for ground-based movement with smooth speed and rotation.
    /// Movement speed is affected by equipment bonuses.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
    {
        #region Movement Configuration

        [Header("Movement Speeds")]
        [SerializeField]
        [Tooltip("Base walking speed when joystick is barely pushed")]
        private float walkSpeed = 3f;

        [SerializeField]
        [Tooltip("Base running speed when joystick is fully pushed")]
        private float runSpeed = 6f;

        [Header("Rotation")]
        [SerializeField]
        [Tooltip("How fast the character rotates to face movement direction")]
        private float rotationSpeed = 10f;

        [Header("Gravity")]
        [SerializeField]
        [Tooltip("Gravity force applied to the character")]
        private float gravity = -9.81f;

        #endregion

        #region References

        private CharacterController _characterController;
        private Magikill.Networking.NetworkPlayer _networkPlayer;
        private Animator _animator;
        private Magikill.Combat.SkillSystem _skillSystem;


        #endregion

        #region Input State

        private Vector2 _currentMovementInput;
        private Vector2 _currentLookDirection;
        private float _verticalVelocity;
        private float _movementAmount;


        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _networkPlayer = GetComponent<Magikill.Networking.NetworkPlayer>();

            _animator = GetComponentInChildren<Animator>();
            _skillSystem = GetComponent<Magikill.Combat.SkillSystem>();

            if (_animator == null)
            {
                Debug.LogError("[PlayerController] Animator not found in children!");
            }

            if (_characterController == null)
            {
                Debug.LogError("[PlayerController] CharacterController component not found!");
            }

            if (_networkPlayer == null)
            {
                Debug.LogError("[PlayerController] NetworkPlayer component not found!");
            }
        }

        public override void Spawned()
        {
            // Register for input callbacks with NetworkRunner
            if (Runner != null)
            {
                Runner.AddCallbacks(this);
                Debug.Log($"[PlayerController] Spawned and registered for input callbacks. HasInputAuthority: {HasInputAuthority}");
            }
            else
            {
                Debug.LogError("[PlayerController] NetworkRunner not found! Cannot register for input callbacks.");
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Unregister callbacks when despawned
            if (Runner != null)
            {
                Runner.RemoveCallbacks(this);
            }
        }

        #endregion

        #region Input Collection (INetworkRunnerCallbacks)

        /// <summary>
        /// Fusion calls this on clients with input authority to collect input.
        /// We gather keyboard input here (simulating virtual joystick for testing).
        /// </summary>
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Only collect input if this is our local player
            if (!HasInputAuthority)
            {
                return;
            }

            var inputData = new NetworkInputData();

            // Collect movement input (WASD simulates virtual joystick)
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
            float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows

            Vector2 movementInput = new Vector2(horizontal, vertical);

            // Normalize to prevent faster diagonal movement
            if (movementInput.magnitude > 1f)
            {
                movementInput.Normalize();
            }

            inputData.movementInput = movementInput;

            // Look direction follows movement direction for now
            // (Will be updated when we add camera/targeting systems)
            if (movementInput.magnitude > 0.1f)
            {
                inputData.lookDirection = movementInput.normalized;
            }
            else
            {
                inputData.lookDirection = Vector2.zero;
            }

            // Collect skill inputs (keyboard keys for testing)
            inputData.skill1Button = Input.GetKey(KeyCode.Alpha1); // Basic attack
            inputData.skill2Button = Input.GetKey(KeyCode.Alpha2);
            inputData.skill3Button = Input.GetKey(KeyCode.Alpha3);
            inputData.skill4Button = Input.GetKey(KeyCode.Alpha4);
            inputData.skill5Button = Input.GetKey(KeyCode.Alpha5);
            inputData.skill6Button = Input.GetKey(KeyCode.Alpha6);

            // Collect interaction input
            inputData.interactButton = Input.GetKey(KeyCode.E);

            // Send input data to Fusion
            input.Set(inputData);
        }

        #endregion

        #region Network Fixed Update

        /// <summary>
        /// Fusion's fixed update - runs on both client and server
        /// This is where we process input and update movement
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            // Only process input if we have state authority (server)
            if (!HasStateAuthority)
            {
                return;
            }

            // Get input from the network
            if (GetInput(out NetworkInputData input))
            {
                ProcessNetworkInput(input);
            }
            // Everyone updates animation (including proxies)
            UpdateAnimator();
        }

        #endregion

        #region Network Input Processing

        /// <summary>
        /// Process the input received from client.
        /// This runs on the server with authority.
        /// </summary>
        public void ProcessNetworkInput(NetworkInputData input)
        {
            _currentMovementInput = input.movementInput;
            _currentLookDirection = input.lookDirection;

            // Process movement
            ProcessMovement();

            // Process rotation
            ProcessRotation();

            // Process skill inputs
            if (_skillSystem != null)
            {
                // Find nearest enemy for auto-targeting
                Transform target = FindNearestEnemy();

                // Check each skill button
                if (input.skill1Button) _skillSystem.UseSkill(0, target);
                if (input.skill2Button) _skillSystem.UseSkill(1, target);
                if (input.skill3Button) _skillSystem.UseSkill(2, target);
                if (input.skill4Button) _skillSystem.UseSkill(3, target);
                if (input.skill5Button) _skillSystem.UseSkill(4, target);
                if (input.skill6Button) _skillSystem.UseSkill(5, target);
            }
        }


        #endregion

        #region Movement Processing

        private void ProcessMovement()
        {
            if (_characterController == null)
            {
                return;
            }

            // Cache movement amount for animation (0 = idle, 1 = full input)
            _movementAmount = _currentMovementInput.magnitude;

            // Calculate movement direction in world space
            Vector3 moveDirection = new Vector3(_currentMovementInput.x, 0f, _currentMovementInput.y);

            // Get input magnitude to determine speed (smooth interpolation between walk and run)
            float inputMagnitude = _currentMovementInput.magnitude;
            float baseSpeed = Mathf.Lerp(walkSpeed, runSpeed, inputMagnitude);

            // Apply equipment movement speed multiplier
            float equipmentSpeedMultiplier = 1.0f;
            if (_networkPlayer != null)
            {
                equipmentSpeedMultiplier = _networkPlayer.TotalMovementSpeed;
            }

            float finalSpeed = baseSpeed * equipmentSpeedMultiplier;

            // Apply speed to direction
            Vector3 movement = moveDirection * finalSpeed;

            // Apply gravity
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -2f; // Small downward force to keep grounded
            }
            else
            {
                _verticalVelocity += gravity * Runner.DeltaTime;
            }

            movement.y = _verticalVelocity;

            // Move the character
            _characterController.Move(movement * Runner.DeltaTime);
        }

        #endregion

        #region Rotation Processing

        private void ProcessRotation()
        {
            // Only rotate if there's meaningful look direction input
            if (_currentLookDirection.magnitude < 0.1f)
            {
                return;
            }

            // Convert look direction to world space rotation
            Vector3 lookDirection3D = new Vector3(_currentLookDirection.x, 0f, _currentLookDirection.y);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection3D);

            // Smoothly rotate towards target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Runner.DeltaTime
            );
        }

        #endregion

        #region Update Animator

        private void UpdateAnimator()
        {
            if (_animator == null)
                return;

            // Smooth animation transitions
            _animator.SetFloat(
                "Speed",
                _movementAmount,
                0.1f,
                Runner != null ? Runner.DeltaTime : Time.deltaTime
            );

            _animator.SetBool("IsGrounded", _characterController.isGrounded);
        }

        #endregion

        #region INetworkRunnerCallbacks Implementation (Required but unused)

        // We only use OnInput, but must implement all interface methods
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnDisconnectedFromServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request) { }

        #endregion

        // Add this helper method to find enemies for auto-targeting:

        #region Enemy Targeting

        [Header("Targeting")]
        [SerializeField]
        [Tooltip("Maximum range to find enemies for auto-targeting")]
        private float autoTargetRange = 15f;

        /// <summary>
        /// Finds the nearest enemy within auto-target range.
        /// </summary>
        private Transform FindNearestEnemy()
        {
            // Find all enemies in the scene
            var enemies = FindObjectsOfType<Magikill.Combat.EnemyStats>();

            Transform nearestEnemy = null;
            float nearestDistance = autoTargetRange;

            foreach (var enemy in enemies)
            {
                // Check if enemy's NetworkObject is spawned before accessing networked properties
                var networkObject = enemy.GetComponent<NetworkObject>();
                if (networkObject == null || !networkObject.IsValid)
                    continue;

                // Now safe to check IsAlive
                if (!enemy.IsAlive)
                    continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }

            return nearestEnemy;
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Movement State")]
        private void LogMovementState()
        {
            Debug.Log("=== Movement State ===");
            Debug.Log($"Has Input Authority: {HasInputAuthority}");
            Debug.Log($"Has State Authority: {HasStateAuthority}");
            Debug.Log($"Movement Input: {_currentMovementInput}");
            Debug.Log($"Look Direction: {_currentLookDirection}");
            Debug.Log($"Is Grounded: {_characterController?.isGrounded}");
            Debug.Log($"Vertical Velocity: {_verticalVelocity}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Rotation: {transform.rotation.eulerAngles}");

            if (_networkPlayer != null)
            {
                Debug.Log($"Base Walk Speed: {walkSpeed}");
                Debug.Log($"Base Run Speed: {runSpeed}");
                Debug.Log($"Equipment Speed Multiplier: {_networkPlayer.TotalMovementSpeed:F2}x");
                Debug.Log($"Final Speed Range: {walkSpeed * _networkPlayer.TotalMovementSpeed:F2} - {runSpeed * _networkPlayer.TotalMovementSpeed:F2}");
            }
        }

        #endregion
    }
}