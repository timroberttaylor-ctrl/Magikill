using UnityEngine;
using Fusion;
using Magikill.Items;
using Magikill.Inventory;

namespace Magikill.Loot
{
    /// <summary>
    /// Represents a loot item on the ground that players can pick up.
    /// Spawned when enemies die or items are dropped.
    /// Despawns after being picked up or after a timeout.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(SphereCollider))]
    public class GroundLoot : NetworkBehaviour
    {
        #region Configuration

        [Header("Loot Settings")]
        [Tooltip("Time in seconds before this loot despawns")]
        [SerializeField]
        private float despawnTime = 60f;

        [Tooltip("Pickup range (player must be this close to pick up)")]
        [SerializeField]
        private float pickupRange = 2f;

        [Header("Visual Settings")]
        [Tooltip("Height above ground to float")]
        [SerializeField]
        private float floatHeight = 0.5f;

        [Tooltip("Rotation speed for visual effect")]
        [SerializeField]
        private float rotationSpeed = 90f;

        [Tooltip("Bob up/down effect speed")]
        [SerializeField]
        private float bobSpeed = 2f;

        [Tooltip("Bob up/down distance")]
        [SerializeField]
        private float bobDistance = 0.2f;

        #endregion

        #region Networked State

        /// <summary>
        /// The item data this loot represents (networked string for item ID)
        /// </summary>
        [Networked]
        public NetworkString<_64> ItemDataId { get; set; }

        /// <summary>
        /// Quantity of the item
        /// </summary>
        [Networked]
        public int Quantity { get; set; }

        /// <summary>
        /// Upgrade level (for equipment)
        /// </summary>
        [Networked]
        public int UpgradeLevel { get; set; }

        /// <summary>
        /// Time when this loot was spawned (for despawn timer)
        /// </summary>
        [Networked]
        private TickTimer DespawnTimer { get; set; }

        #endregion

        #region References

        private ItemInstance _itemInstance;
        private SphereCollider _pickupCollider;
        private Transform _visualTransform;
        private Vector3 _startPosition;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            _pickupCollider = GetComponent<SphereCollider>();
            _pickupCollider.isTrigger = true;
            _pickupCollider.radius = pickupRange;

            _startPosition = transform.position;

            // Setup visual child object
            SetupVisuals();

            if (HasStateAuthority)
            {
                // Start despawn timer on server
                DespawnTimer = TickTimer.CreateFromSeconds(Runner, despawnTime);
                Debug.Log($"[GroundLoot] Spawned loot: {ItemDataId} x{Quantity}. Will despawn in {despawnTime}s");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                // Check if despawn timer expired
                if (DespawnTimer.Expired(Runner))
                {
                    Debug.Log($"[GroundLoot] Loot {ItemDataId} despawned due to timeout");
                    Runner.Despawn(Object);
                }
            }
        }

        private void Update()
        {
            // Visual effects (runs on all clients)
            if (_visualTransform != null)
            {
                // Rotate
                _visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

                // Bob up and down
                float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
                _visualTransform.position = _startPosition + Vector3.up * (floatHeight + bobOffset);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes this ground loot with item data (called by spawner).
        /// </summary>
        public void Initialize(ItemInstance itemInstance)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[GroundLoot] Cannot initialize without state authority!");
                return;
            }

            _itemInstance = itemInstance;
            ItemDataId = itemInstance.itemData.itemId;
            Quantity = itemInstance.quantity;
            UpgradeLevel = itemInstance.upgradeLevel;

            Debug.Log($"[GroundLoot] Initialized: {ItemDataId} x{Quantity} +{UpgradeLevel}");
        }

        private void SetupVisuals()
        {
            // Create a child object for visuals
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(transform);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localScale = Vector3.one * 0.3f;

            // Remove collider from visual (parent has the trigger)
            Destroy(visualObject.GetComponent<Collider>());

            _visualTransform = visualObject.transform;

            // TODO: Replace cube with proper item icon/mesh based on ItemDataId
            // For now, use colored cubes based on rarity
            // This will be improved when we add proper 3D models or icon billboards
        }

        #endregion

        #region Pickup Logic

        private void OnTriggerEnter(Collider other)
        {
            // Only server handles pickup logic
            if (!HasStateAuthority)
            {
                return;
            }

            // Check if it's a player
            var networkPlayer = other.GetComponent<Magikill.Networking.NetworkPlayer>();
            if (networkPlayer == null)
            {
                return;
            }

            // Check if player has input authority (is the local player for someone)
            if (!networkPlayer.HasInputAuthority)
            {
                return;
            }

            // Attempt pickup
            TryPickup(networkPlayer);
        }

        /// <summary>
        /// Attempts to pick up this loot for the given player.
        /// Called on server only.
        /// </summary>
        private void TryPickup(Magikill.Networking.NetworkPlayer player)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            // Get player's inventory
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning($"[GroundLoot] Player {player.PlayerName} has no inventory!");
                return;
            }

            // Recreate item instance from networked data
            // Note: In production, you'd need a proper item database to look up ItemData by ID
            // For now, we'll use the cached instance if available
            if (_itemInstance == null)
            {
                Debug.LogWarning($"[GroundLoot] Cannot pickup - item instance is null!");
                return;
            }

            // Try to add to inventory
            bool success = inventory.AddItem(_itemInstance);

            if (success)
            {
                Debug.Log($"[GroundLoot] Player {player.PlayerName} picked up {_itemInstance.GetDisplayName()}");
                
                // Despawn the loot
                Runner.Despawn(Object);
            }
            else
            {
                Debug.LogWarning($"[GroundLoot] Player {player.PlayerName} failed to pickup {_itemInstance.GetDisplayName()} (inventory full?)");
            }
        }

        #endregion

        #region Manual Pickup (For Testing)

        /// <summary>
        /// Manually triggers pickup for nearby player (for testing/UI button).
        /// </summary>
        [ContextMenu("Trigger Pickup")]
        public void TriggerManualPickup()
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[GroundLoot] Manual pickup requires state authority");
                return;
            }

            // Find nearest player
            var players = FindObjectsOfType<Magikill.Networking.NetworkPlayer>();
            Magikill.Networking.NetworkPlayer nearestPlayer = null;
            float nearestDistance = float.MaxValue;

            foreach (var player in players)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance && distance <= pickupRange)
                {
                    nearestDistance = distance;
                    nearestPlayer = player;
                }
            }

            if (nearestPlayer != null)
            {
                TryPickup(nearestPlayer);
            }
            else
            {
                Debug.LogWarning("[GroundLoot] No player in pickup range");
            }
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmosSelected()
        {
            // Draw pickup range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }

        #endregion
    }
}
