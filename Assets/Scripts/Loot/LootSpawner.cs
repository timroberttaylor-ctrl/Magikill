using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Magikill.Items;

namespace Magikill.Loot
{
    /// <summary>
    /// Handles spawning loot on the ground when enemies die.
    /// Scatters items in a circle pattern around the death position.
    /// Server-authoritative spawning.
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        #region Configuration

        [Header("Loot Prefab")]
        [SerializeField]
        [Tooltip("The GroundLoot prefab to spawn")]
        private GroundLoot groundLootPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Radius of the circle to scatter loot in")]
        [SerializeField]
        private float scatterRadius = 1.5f;

        [Tooltip("Height above ground to spawn loot")]
        [SerializeField]
        private float spawnHeight = 0.5f;

        [Tooltip("Randomize scatter positions slightly")]
        [SerializeField]
        private bool randomizePositions = true;

        #endregion

        #region References

        private NetworkRunner _runner;

        #endregion

        #region Initialization

        private void Awake()
        {
            if (groundLootPrefab == null)
            {
                Debug.LogError("[LootSpawner] Ground loot prefab not assigned!");
            }
        }

        private void Start()
        {
            _runner = FindObjectOfType<NetworkRunner>();
            if (_runner == null)
            {
                Debug.LogError("[LootSpawner] NetworkRunner not found!");
            }
        }

        #endregion

        #region Loot Spawning

        /// <summary>
        /// Spawns loot from a loot table at the specified position.
        /// Call this when an enemy dies.
        /// Server-only.
        /// </summary>
        public void SpawnLoot(LootTable lootTable, Vector3 position)
        {
            if (_runner == null || !_runner.IsServer)
            {
                Debug.LogWarning("[LootSpawner] Can only spawn loot on server!");
                return;
            }

            if (lootTable == null)
            {
                Debug.LogWarning("[LootSpawner] Loot table is null!");
                return;
            }

            if (groundLootPrefab == null)
            {
                Debug.LogError("[LootSpawner] Ground loot prefab not assigned!");
                return;
            }

            // Roll loot from the table
            List<ItemInstance> droppedItems = lootTable.RollLoot();

            if (droppedItems.Count == 0)
            {
                Debug.Log("[LootSpawner] No loot dropped from this enemy");
                return;
            }

            Debug.Log($"[LootSpawner] Spawning {droppedItems.Count} items at {position}");

            // Spawn each item scattered in a circle
            for (int i = 0; i < droppedItems.Count; i++)
            {
                Vector3 spawnPosition = CalculateScatterPosition(position, i, droppedItems.Count);
                SpawnGroundLoot(droppedItems[i], spawnPosition);
            }
        }

        /// <summary>
        /// Spawns a single ground loot object for an item instance.
        /// </summary>
        private void SpawnGroundLoot(ItemInstance itemInstance, Vector3 position)
        {
            if (_runner == null)
            {
                Debug.LogError("[LootSpawner] NetworkRunner is null!");
                return;
            }

            // Spawn the ground loot networked object
            NetworkObject lootObject = _runner.Spawn(
                groundLootPrefab.GetComponent<NetworkObject>(),
                position,
                Quaternion.identity
            );

            if (lootObject == null)
            {
                Debug.LogError("[LootSpawner] Failed to spawn ground loot!");
                return;
            }

            // Initialize the ground loot with item data
            GroundLoot groundLoot = lootObject.GetComponent<GroundLoot>();
            if (groundLoot != null)
            {
                groundLoot.Initialize(itemInstance);
                Debug.Log($"[LootSpawner] Spawned ground loot: {itemInstance.GetDisplayName()} at {position}");
            }
            else
            {
                Debug.LogError("[LootSpawner] Spawned object has no GroundLoot component!");
            }
        }

        /// <summary>
        /// Calculates a position in a circular scatter pattern.
        /// </summary>
        private Vector3 CalculateScatterPosition(Vector3 centerPosition, int index, int totalItems)
        {
            Vector3 spawnPosition = centerPosition + Vector3.up * spawnHeight;

            if (totalItems == 1)
            {
                // Single item, spawn at center
                return spawnPosition;
            }

            // Calculate angle for this item in the circle
            float angleStep = 360f / totalItems;
            float angle = angleStep * index;

            // Add randomization if enabled
            if (randomizePositions)
            {
                angle += Random.Range(-angleStep * 0.3f, angleStep * 0.3f);
            }

            // Convert to radians and calculate offset
            float angleRad = angle * Mathf.Deg2Rad;
            float radius = scatterRadius;

            // Add slight radius randomization
            if (randomizePositions)
            {
                radius += Random.Range(-scatterRadius * 0.2f, scatterRadius * 0.2f);
            }

            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * radius,
                0f,
                Mathf.Sin(angleRad) * radius
            );

            return spawnPosition + offset;
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            // Draw scatter radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, scatterRadius);
        }

        #endregion
    }
}
