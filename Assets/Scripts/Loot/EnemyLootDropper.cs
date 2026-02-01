using UnityEngine;
using Magikill.Loot;

namespace Magikill.Combat
{
    /// <summary>
    /// Handles loot dropping when this enemy dies.
    /// Attach to enemy prefabs alongside EnemyStats.
    /// Subscribes to death event and spawns loot from configured loot table.
    /// </summary>
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyLootDropper : MonoBehaviour
    {
        [Header("Loot Configuration")]
        [SerializeField]
        [Tooltip("Loot table that defines what this enemy drops")]
        private LootTable lootTable;

        [Header("References")]
        [SerializeField]
        [Tooltip("Leave null to auto-find LootSpawner in scene")]
        private LootSpawner lootSpawner;

        private EnemyStats _enemyStats;

        private void Awake()
        {
            _enemyStats = GetComponent<EnemyStats>();

            if (_enemyStats == null)
            {
                Debug.LogError("[EnemyLootDropper] EnemyStats component not found!");
                return;
            }

            // Find LootSpawner if not assigned
            if (lootSpawner == null)
            {
                lootSpawner = FindObjectOfType<LootSpawner>();
                
                if (lootSpawner == null)
                {
                    Debug.LogError("[EnemyLootDropper] LootSpawner not found in scene! Add a LootSpawner to your scene.");
                }
            }
        }

        private void OnEnable()
        {
            if (_enemyStats != null)
            {
                _enemyStats.OnDeathEvent += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_enemyStats != null)
            {
                _enemyStats.OnDeathEvent -= HandleDeath;
            }
        }

        /// <summary>
        /// Called when the enemy dies. Spawns loot at death position.
        /// </summary>
        private void HandleDeath()
        {
            if (lootSpawner == null)
            {
                Debug.LogWarning($"[EnemyLootDropper] Cannot drop loot - LootSpawner is null!");
                return;
            }

            if (lootTable == null)
            {
                Debug.Log($"[EnemyLootDropper] {gameObject.name} has no loot table assigned - no loot will drop");
                return;
            }

            // Spawn loot at this enemy's position
            Vector3 deathPosition = transform.position;
            Debug.Log($"[EnemyLootDropper] {gameObject.name} died at {deathPosition}. Spawning loot...");
            
            lootSpawner.SpawnLoot(lootTable, deathPosition);
        }
    }
}
