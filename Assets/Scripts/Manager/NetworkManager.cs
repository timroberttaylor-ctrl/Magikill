using System;
using UnityEngine;
using UnityEngine.Events;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;

namespace Magikill.Core
{
    /// <summary>
    /// Manages Photon Fusion network connections for dedicated server mode.
    /// Handles server startup and client connections with configurable mode.
    /// Priority: 0 (initializes first)
    /// </summary>
    public class NetworkManager : MonoBehaviour, IGameService, INetworkRunnerCallbacks
    {
        #region IGameService Implementation

        public int Priority => 0;

        #endregion

        #region Configuration

        [Header("Network Mode")]
        [SerializeField]
        [Tooltip("Toggle between Server and Client mode for testing")]
        private bool isServer = false;

        [Header("Connection Settings")]
        private const string SESSION_NAME = "MagikillDevSession";
        private const string SERVER_ADDRESS = "127.0.0.1";
        private const int SERVER_PORT = 27015;

        #endregion

        #region Network State

        private NetworkRunner _runner;
        private bool _isConnected = false;
        private bool _isInitialized = false;

        public bool IsConnected => _isConnected;
        public bool IsServer => isServer;
        public NetworkRunner Runner => _runner;

        #endregion

        #region C# Events

        public event Action OnConnectedEvent;
        public event Action OnDisconnectedEvent;
        public event Action<string> OnConnectionFailedEvent;

        #endregion

        #region Unity Events

        [Header("Unity Events")]
        public UnityEvent OnConnectedUnityEvent;
        public UnityEvent OnDisconnectedUnityEvent;
        public UnityEvent<string> OnConnectionFailedUnityEvent;

        #endregion

        #region IGameService Lifecycle

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[NetworkManager] Already initialized.");
                return;
            }

            Debug.Log($"[NetworkManager] Initializing in {(isServer ? "SERVER" : "CLIENT")} mode...");

            // Create NetworkRunner GameObject
            CreateNetworkRunner();

            _isInitialized = true;
            Debug.Log("[NetworkManager] Initialization complete. Ready to connect.");
        }

        public void Shutdown()
        {
            Debug.Log("[NetworkManager] Shutting down...");

            if (_runner != null)
            {
                if (_runner.IsRunning)
                {
                    _runner.Shutdown();
                }
                Destroy(_runner.gameObject);
                _runner = null;
            }

            _isConnected = false;
            _isInitialized = false;

            Debug.Log("[NetworkManager] Shutdown complete.");
        }

        #endregion

        #region NetworkRunner Setup

        private void CreateNetworkRunner()
        {
            // Check if NetworkRunner already exists
            _runner = FindObjectOfType<NetworkRunner>();

            if (_runner != null)
            {
                Debug.Log("[NetworkManager] Found existing NetworkRunner.");
                _runner.AddCallbacks(this);
                return;
            }

            // Create new NetworkRunner GameObject
            GameObject runnerObject = new GameObject("NetworkRunner");
            runnerObject.transform.SetParent(transform);

            _runner = runnerObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = !isServer; // Only clients provide input in Server mode

            // Add callbacks
            _runner.AddCallbacks(this);

            Debug.Log("[NetworkManager] NetworkRunner created and configured.");
        }

        #endregion

        #region Connection Methods

        /// <summary>
        /// Starts the dedicated server.
        /// Call this to start hosting a game session.
        /// </summary>
        public async void StartServer()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[NetworkManager] Cannot start server - not initialized!");
                return;
            }

            if (!isServer)
            {
                Debug.LogError("[NetworkManager] Cannot start server - NetworkManager is in CLIENT mode!");
                return;
            }

            if (_runner.IsRunning)
            {
                Debug.LogWarning("[NetworkManager] Server is already running!");
                return;
            }

            Debug.Log($"[NetworkManager] Starting dedicated server... Session: {SESSION_NAME}");

            try
            {
                var startGameArgs = new StartGameArgs()
                {
                    GameMode = GameMode.Client,
                    SessionName = SESSION_NAME,
                    Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                };

                var result = await _runner.StartGame(startGameArgs);

                if (result.Ok)
                {
                    Debug.Log("[NetworkManager] Server started successfully!");
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Failed to start server: {result.ShutdownReason}");
                    InvokeConnectionFailed($"Server start failed: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Exception starting server: {ex.Message}\n{ex.StackTrace}");
                InvokeConnectionFailed($"Server exception: {ex.Message}");
            }
        }
        /// <summary>
        /// Starts as host (creates a room and acts as server).
        /// Call this to start hosting a game session.
        /// </summary>
        public async void StartHost()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[NetworkManager] Cannot start host - not initialized!");
                return;
            }

            if (!isServer)
            {
                Debug.LogError("[NetworkManager] Cannot start host - NetworkManager is in CLIENT mode!");
                return;
            }

            if (_runner.IsRunning)
            {
                Debug.LogWarning("[NetworkManager] Host is already running!");
                return;
            }

            Debug.Log($"[NetworkManager] Starting as host... Session: {SESSION_NAME}");

            try
            {
                var startGameArgs = new StartGameArgs()
                {
                    GameMode = GameMode.Host,  // Changed from Server to Host
                    SessionName = SESSION_NAME,
                    Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                };

                var result = await _runner.StartGame(startGameArgs);

                if (result.Ok)
                {
                    Debug.Log("[NetworkManager] Host started successfully!");
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Failed to start host: {result.ShutdownReason}");
                    InvokeConnectionFailed($"Host start failed: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Exception starting host: {ex.Message}\n{ex.StackTrace}");
                InvokeConnectionFailed($"Host exception: {ex.Message}");
            }
        }
        /// <summary>
        /// Connects to the dedicated server as a client.
        /// Call this from the main menu or character select screen.
        /// </summary>
        public async void ConnectAsClient()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[NetworkManager] Cannot connect - not initialized!");
                return;
            }

            if (isServer)
            {
                Debug.LogError("[NetworkManager] Cannot connect as client - NetworkManager is in SERVER mode!");
                return;
            }

            if (_runner.IsRunning)
            {
                Debug.LogWarning("[NetworkManager] Client is already connected or connecting!");
                return;
            }

            Debug.Log($"[NetworkManager] Connecting to server at {SERVER_ADDRESS}... Session: {SESSION_NAME}");

            try
            {
                var startGameArgs = new StartGameArgs()
                {
                    GameMode = GameMode.Client,
                    SessionName = SESSION_NAME,
                    Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                };

                var result = await _runner.StartGame(startGameArgs);

                if (result.Ok)
                {
                    Debug.Log("[NetworkManager] Connected to server successfully!");
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Failed to connect: {result.ShutdownReason}");
                    InvokeConnectionFailed($"Connection failed: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Exception connecting to server: {ex.Message}\n{ex.StackTrace}");
                InvokeConnectionFailed($"Connection exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        public void Disconnect()
        {
            if (_runner != null && _runner.IsRunning)
            {
                Debug.Log("[NetworkManager] Disconnecting...");
                _runner.Shutdown();
            }
            else
            {
                Debug.LogWarning("[NetworkManager] Cannot disconnect - not connected!");
            }
        }

        #endregion

        #region Event Invocation Helpers

        private void InvokeConnected()
        {
            _isConnected = true;
            OnConnectedEvent?.Invoke();
            OnConnectedUnityEvent?.Invoke();
            Debug.Log("[NetworkManager] Connection established - events fired.");
        }

        private void InvokeDisconnected()
        {
            _isConnected = false;
            OnDisconnectedEvent?.Invoke();
            OnDisconnectedUnityEvent?.Invoke();
            Debug.Log("[NetworkManager] Disconnected - events fired.");
        }

        private void InvokeConnectionFailed(string reason)
        {
            _isConnected = false;
            OnConnectionFailedEvent?.Invoke(reason);
            OnConnectionFailedUnityEvent?.Invoke(reason);
            Debug.LogError($"[NetworkManager] Connection failed: {reason} - events fired.");
        }

        #endregion

        #region INetworkRunnerCallbacks Implementation

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkManager] Player joined: {player.PlayerId}");

            // Notify PlayerSpawnManager to spawn the player
            if (isServer && GameManager.HasService<PlayerSpawnManager>())
            {
                PlayerSpawnManager spawnManager = GameManager.GetService<PlayerSpawnManager>();
                spawnManager.SpawnPlayer(player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkManager] Player left: {player.PlayerId}");

            // Notify PlayerSpawnManager to despawn the player
            if (isServer && GameManager.HasService<PlayerSpawnManager>())
            {
                PlayerSpawnManager spawnManager = GameManager.GetService<PlayerSpawnManager>();
                spawnManager.DespawnPlayer(player);
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Input handling will be implemented in PlayerController
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            // Handle missing input
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[NetworkManager] Shutdown: {shutdownReason}");
            InvokeDisconnected();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Connected to server callback received.");
            InvokeConnected();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log($"[NetworkManager] Disconnected from server: {reason}");
            InvokeDisconnected();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            Debug.Log($"[NetworkManager] Connect request from: {request.RemoteAddress}");
            request.Accept();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[NetworkManager] Connect failed to {remoteAddress}: {reason}");
            InvokeConnectionFailed($"Connect failed: {reason}");
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            // Handle custom messages
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            // Handle session list updates
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            // Handle authentication
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            // Handle host migration (not needed for dedicated server)
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            // Handle reliable data
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            // Handle reliable data progress
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Scene load done.");
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Scene load start.");
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // Handle object exit Area of Interest
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // Handle object enter Area of Interest
        }

        public void OnDisconnectedFromServer(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Disconnected from server (parameterless callback).");
            InvokeDisconnected();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request)
        {
            Debug.Log($"[NetworkManager] Connect request received.");
            request.Accept();
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Network Status")]
        private void LogNetworkStatus()
        {
            Debug.Log("=== Network Status ===");
            Debug.Log($"Mode: {(isServer ? "SERVER" : "CLIENT")}");
            Debug.Log($"Initialized: {_isInitialized}");
            Debug.Log($"Connected: {_isConnected}");
            Debug.Log($"Runner Running: {(_runner != null ? _runner.IsRunning : false)}");
            Debug.Log($"Session: {SESSION_NAME}");
        }

        #endregion
    }
}