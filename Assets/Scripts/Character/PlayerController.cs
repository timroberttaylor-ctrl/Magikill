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
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerController : NetworkBehaviour, INetworkRunnerCallbacks
    {
        #region Movement Configuration

        [Header("Movement Speeds")]
        [SerializeField]
        [Tooltip("Walking speed when joystick is barely pushed")]
        private float walkSpeed = 3f;

        [SerializeField]
        [Tooltip("Running speed when joystick is fully pushed")]
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
   

        #endregion

        #region Input State

        private Vector2 _currentMovementInput;
        private Vector2 _currentLookDirection;
        private float _verticalVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_characterController == null)
            {
                Debug.LogError("[PlayerController] CharacterController component not found!");
            }
        }
        //Removed Runner code for local host
        /*public override void Spawned()
        {
            // Register for input callbacks with NetworkRunner
            _runner = Runner;

            if (_runner != null)
            {
                _runner.AddCallbacks(this);
                Debug.Log($"[PlayerController] Spawned and registered for input callbacks. HasInputAuthority: {HasInputAuthority}");
            }
            else
            {
                Debug.LogError("[PlayerController] NetworkRunner not found! Cannot register for input callbacks.");
            }
        }*/
        public override void Spawned()
        {
            // Register for input callbacks with NetworkRunner
            //Remove code and use above Spawner for Server version not host
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

        //Removed Runner code for local host
        /*public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Unregister callbacks when despawned
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
            }
        }*/
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

            // TODO: Process combat input when combat system is ready
        }

        #endregion

        #region Movement Processing

        private void ProcessMovement()
        {
            if (_characterController == null)
            {
                return;
            }

            // Calculate movement direction in world space
            Vector3 moveDirection = new Vector3(_currentMovementInput.x, 0f, _currentMovementInput.y);

            // Get input magnitude to determine speed (smooth interpolation between walk and run)
            float inputMagnitude = _currentMovementInput.magnitude;
            float currentSpeed = Mathf.Lerp(walkSpeed, runSpeed, inputMagnitude);

            // Apply speed to direction
            Vector3 movement = moveDirection * currentSpeed;

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
        }

        #endregion
    }
}