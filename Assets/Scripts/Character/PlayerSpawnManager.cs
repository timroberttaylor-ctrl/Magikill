using System.Collections.Generic;
using Fusion;
using UnityEngine;
using NetworkPlayerClass = Magikill.Networking.NetworkPlayer;

namespace Magikill.Core
{
    /// <summary>
    /// Manages player spawning, despawning, and lifecycle.
    /// Tracks all active players and handles spawn point selection.
    /// Priority: 5 (gameplay systems tier)
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour, IGameService
    {
        #region IGameService Implementation

        public int Priority => 5;

        #endregion

        #region Configuration

        [Header("Player Prefab")]
        [SerializeField]
        [Tooltip("The networked player prefab to spawn")]
        private NetworkPlayerClass playerPrefab;

        [Header("Spawn Settings")]
        [SerializeField]
        [Tooltip("Tag used to identify spawn points in the scene")]
        private string spawnPointTag = "SpawnPoint";

        [SerializeField]
        [Tooltip("Default spawn position if no spawn points found")]
        private Vector3 defaultSpawnPosition = Vector3.zero;

        #endregion

        #region Active Players Tracking

        private Dictionary<PlayerRef, NetworkPlayerClass> _activePlayers = new Dictionary<PlayerRef, NetworkPlayerClass>();
        private List<Transform> _spawnPoints = new List<Transform>();

        /// <summary>
        /// Gets all currently active players.
        /// </summary>
        public IReadOnlyDictionary<PlayerRef, NetworkPlayerClass> ActivePlayers => _activePlayers;

        /// <summary>
        /// Gets the total number of active players.
        /// </summary>
        public int ActivePlayerCount => _activePlayers.Count;

        #endregion

        #region References

        private NetworkRunner _runner;

        #endregion

        #region IGameService Lifecycle

        public void Initialize()
        {
            Debug.Log("[PlayerSpawnManager] Initializing...");

            // Validate player prefab
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnManager] Player prefab is not assigned! Please assign it in the Inspector.");
                return;
            }

            // Get NetworkRunner reference
            _runner = FindObjectOfType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[PlayerSpawnManager] NetworkRunner not found!");
                return;
            }

            // Subscribe to NetworkManager events for player join/leave
            var networkManager = GameManager.GetService<NetworkManager>();
            if (networkManager != null)
            {
                // We'll handle spawning via Fusion callbacks directly
                Debug.Log("[PlayerSpawnManager] Ready to spawn players on connection.");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnManager] NetworkManager not found. Player spawning may not work correctly.");
            }

            // Find all spawn points in the scene
            RefreshSpawnPoints();

            Debug.Log("[PlayerSpawnManager] Initialization complete.");
        }

        public void Shutdown()
        {
            Debug.Log("[PlayerSpawnManager] Shutting down...");

            // Clear tracking
            _activePlayers.Clear();
            _spawnPoints.Clear();

            Debug.Log("[PlayerSpawnManager] Shutdown complete.");
        }

        #endregion

        #region Spawn Point Management

        /// <summary>
        /// Finds all spawn points in the current scene by tag.
        /// Call this when loading new scenes.
        /// </summary>
        public void RefreshSpawnPoints()
        {
            _spawnPoints.Clear();

            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag(spawnPointTag);

            foreach (GameObject spawnPointObj in spawnPointObjects)
            {
                _spawnPoints.Add(spawnPointObj.transform);
            }

            Debug.Log($"[PlayerSpawnManager] Found {_spawnPoints.Count} spawn points with tag '{spawnPointTag}'.");

            if (_spawnPoints.Count == 0)
            {
                Debug.LogWarning($"[PlayerSpawnManager] No spawn points found! Players will spawn at default position {defaultSpawnPosition}.");
            }
        }

        /// <summary>
        /// Gets a random spawn point position and rotation.
        /// </summary>
        private (Vector3 position, Quaternion rotation) GetRandomSpawnPoint()
        {
            if (_spawnPoints.Count == 0)
            {
                return (defaultSpawnPosition, Quaternion.identity);
            }

            int randomIndex = Random.Range(0, _spawnPoints.Count);
            Transform spawnPoint = _spawnPoints[randomIndex];

            return (spawnPoint.position, spawnPoint.rotation);
        }

        #endregion

        #region Player Spawning

        /// <summary>
        /// Spawns a player for the given PlayerRef.
        /// Called by Fusion when a player joins.
        /// </summary>
        public void SpawnPlayer(PlayerRef playerRef)
        {
            if (_runner == null || !_runner.IsServer)
            {
                Debug.LogWarning("[PlayerSpawnManager] Cannot spawn player - not running as server!");
                return;
            }
            if (_activePlayers.ContainsKey(playerRef))
            {
                Debug.LogWarning($"[PlayerSpawnManager] Player {playerRef} already has a spawned character!");
                return;
            }

            Debug.Log($"[PlayerSpawnManager] Spawning player for PlayerRef {playerRef}...");

            // IMPORTANT: Refresh spawn points right before spawning to ensure they're valid
            RefreshSpawnPoints();

            // Get spawn location
            var (spawnPosition, spawnRotation) = GetRandomSpawnPoint();

            // Spawn the player prefab on the network
            NetworkObject networkObject = _runner.Spawn(
                playerPrefab.GetComponent<NetworkObject>(),
                spawnPosition,
                spawnRotation,
                playerRef
            );

            if (networkObject != null)
            {
                NetworkPlayerClass spawnedPlayer = networkObject.GetComponent<NetworkPlayerClass>();

                if (spawnedPlayer != null)
                {
                    // Track the spawned player
                    _activePlayers.Add(playerRef, spawnedPlayer);
                    Debug.Log($"[PlayerSpawnManager] Player {playerRef} spawned successfully at {spawnPosition}. Total players: {_activePlayers.Count}");
                }
                else
                {
                    Debug.LogError("[PlayerSpawnManager] Spawned object does not have a NetworkPlayer component!");
                }
            }
            else
            {
                Debug.LogError($"[PlayerSpawnManager] Failed to spawn player for PlayerRef {playerRef}!");
            }
        }

        #endregion

        #region Player Despawning

        /// <summary>
        /// Despawns a player when they disconnect.
        /// Called by Fusion when a player leaves.
        /// </summary>
        public void DespawnPlayer(PlayerRef playerRef)
        {
            if (!_activePlayers.TryGetValue(playerRef, out NetworkPlayerClass player))
            {
                Debug.LogWarning($"[PlayerSpawnManager] Attempted to despawn player {playerRef}, but they are not tracked!");
                return;
            }

            Debug.Log($"[PlayerSpawnManager] Despawning player {playerRef}...");

            // Despawn the network object
            if (player != null && player.Object != null)
            {
                _runner.Despawn(player.Object);
            }

            // Remove from tracking
            _activePlayers.Remove(playerRef);

            Debug.Log($"[PlayerSpawnManager] Player {playerRef} despawned. Remaining players: {_activePlayers.Count}");
        }

        #endregion

        #region Player Lookup

        /// <summary>
        /// Gets the NetworkPlayer for a specific PlayerRef.
        /// Returns null if player not found.
        /// </summary>
        public NetworkPlayerClass GetPlayer(PlayerRef playerRef)
        {
            _activePlayers.TryGetValue(playerRef, out NetworkPlayerClass player);
            return player;
        }

        /// <summary>
        /// Checks if a player is currently spawned.
        /// </summary>
        public bool IsPlayerSpawned(PlayerRef playerRef)
        {
            return _activePlayers.ContainsKey(playerRef);
        }

        /// <summary>
        /// Gets all active players as a list.
        /// </summary>
        public List<NetworkPlayerClass> GetAllPlayers()
        {
            return new List<NetworkPlayerClass>(_activePlayers.Values);
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log All Players")]
        private void LogAllPlayers()
        {
            Debug.Log("=== Active Players ===");
            Debug.Log($"Total: {_activePlayers.Count}");

            foreach (var kvp in _activePlayers)
            {
                PlayerRef playerRef = kvp.Key;
                NetworkPlayerClass player = kvp.Value;

                if (player != null)
                {
                    Debug.Log($"PlayerRef: {playerRef} | Name: {player.PlayerName} | Class: {player.PlayerClass} | Position: {player.transform.position}");
                }
                else
                {
                    Debug.Log($"PlayerRef: {playerRef} | Player is NULL");
                }
            }
        }

        [ContextMenu("Refresh Spawn Points")]
        private void DebugRefreshSpawnPoints()
        {
            RefreshSpawnPoints();
        }

        #endregion
    }
}