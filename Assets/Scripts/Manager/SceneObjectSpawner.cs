using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Magikill.Core
{
    /// <summary>
    /// Spawns pre-placed NetworkObjects in the scene when Fusion starts.
    /// Attach this to a GameObject in scenes that contain pre-placed enemies or other networked objects.
    /// </summary>
    public class SceneObjectSpawner : MonoBehaviour
    {
        [Header("Scene Objects to Spawn")]
        [SerializeField]
        [Tooltip("List of pre-placed NetworkObjects in this scene that need to be spawned")]
        private List<NetworkObject> sceneNetworkObjects = new List<NetworkObject>();

        [Header("Auto-Find Settings")]
        [SerializeField]
        [Tooltip("Automatically find all inactive NetworkObjects in scene on start")]
        private bool autoFindSceneObjects = true;

        private NetworkRunner _runner;
        private bool _hasSpawned = false;

        private void Start()
        {
            // Auto-find scene objects if enabled
            if (autoFindSceneObjects)
            {
                FindSceneNetworkObjects();
            }

            // Get NetworkRunner reference
            _runner = FindObjectOfType<NetworkRunner>();

            if (_runner == null)
            {
                Debug.LogError("[SceneObjectSpawner] NetworkRunner not found!");
                return;
            }

            // Subscribe to runner events
            if (_runner.IsRunning)
            {
                // Already running, spawn immediately
                SpawnSceneObjects();
            }
            else
            {
                // Wait for runner to start
                StartCoroutine(WaitForRunnerAndSpawn());
            }
        }

        /// <summary>
        /// Automatically finds all NetworkObjects in the scene.
        /// </summary>
        private void FindSceneNetworkObjects()
        {
            sceneNetworkObjects.Clear();

            // Find all NetworkObjects in scene (including inactive)
            NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>(true);

            foreach (NetworkObject netObj in allNetworkObjects)
            {
                // Only add objects that are in the scene (not prefabs or runtime-spawned)
                if (netObj.gameObject.scene.IsValid() && !netObj.gameObject.scene.name.Contains("DontDestroyOnLoad"))
                {
                    sceneNetworkObjects.Add(netObj);
                    Debug.Log($"[SceneObjectSpawner] Found scene NetworkObject: {netObj.gameObject.name}");
                }
            }

            Debug.Log($"[SceneObjectSpawner] Auto-found {sceneNetworkObjects.Count} scene NetworkObjects");
        }

        /// <summary>
        /// Waits for NetworkRunner to be running, then spawns objects.
        /// </summary>
        private System.Collections.IEnumerator WaitForRunnerAndSpawn()
        {
            Debug.Log("[SceneObjectSpawner] Waiting for NetworkRunner to start...");

            // Wait until runner is running
            while (_runner != null && !_runner.IsRunning)
            {
                yield return null;
            }

            // Additional small delay to ensure Fusion is fully initialized
            yield return new WaitForSeconds(0.5f);

            SpawnSceneObjects();
        }

        /// <summary>
        /// Spawns all registered scene NetworkObjects.
        /// </summary>
        private void SpawnSceneObjects()
        {
            if (_hasSpawned)
            {
                Debug.LogWarning("[SceneObjectSpawner] Scene objects already spawned!");
                return;
            }

            if (_runner == null || !_runner.IsRunning)
            {
                Debug.LogError("[SceneObjectSpawner] Cannot spawn - NetworkRunner not running!");
                return;
            }

            // Only the host/server should spawn scene objects
            if (!_runner.IsServer)
            {
                Debug.Log("[SceneObjectSpawner] Not server - skipping scene object spawn");
                return;
            }

            Debug.Log($"[SceneObjectSpawner] Spawning {sceneNetworkObjects.Count} scene NetworkObjects...");

            int spawnedCount = 0;
            foreach (NetworkObject netObj in sceneNetworkObjects)
            {
                if (netObj == null)
                {
                    Debug.LogWarning("[SceneObjectSpawner] Null NetworkObject in list - skipping");
                    continue;
                }

                // Skip if already spawned
                if (netObj.IsValid)
                {
                    Debug.Log($"[SceneObjectSpawner] {netObj.gameObject.name} already spawned - skipping");
                    continue;
                }

                try
                {
                    // Spawn the object on the network
                    _runner.Spawn(netObj);
                    spawnedCount++;
                    Debug.Log($"[SceneObjectSpawner] Spawned: {netObj.gameObject.name}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneObjectSpawner] Failed to spawn {netObj.gameObject.name}: {ex.Message}");
                }
            }

            _hasSpawned = true;
            Debug.Log($"[SceneObjectSpawner] Successfully spawned {spawnedCount}/{sceneNetworkObjects.Count} scene objects");
        }

        #region Debug Utilities

        [ContextMenu("Find Scene Network Objects")]
        private void DebugFindSceneObjects()
        {
            FindSceneNetworkObjects();
        }

        [ContextMenu("Log Scene Objects")]
        private void LogSceneObjects()
        {
            Debug.Log("=== Scene Network Objects ===");
            for (int i = 0; i < sceneNetworkObjects.Count; i++)
            {
                if (sceneNetworkObjects[i] != null)
                {
                    Debug.Log($"[{i}] {sceneNetworkObjects[i].gameObject.name} - Valid: {sceneNetworkObjects[i].IsValid}");
                }
                else
                {
                    Debug.Log($"[{i}] NULL");
                }
            }
        }

        #endregion
    }
}