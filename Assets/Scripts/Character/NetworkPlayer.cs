using Fusion;
using UnityEngine;
using Magikill.Character;
using Magikill.Items;

namespace Magikill.Networking
{
    /// <summary>
    /// Core networking component for player characters.
    /// Handles input collection, network state synchronization, player identity, and stats.
    /// Must be attached to the player prefab root GameObject.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        #region Networked Player State

        /// <summary>
        /// Player's display name visible to other players.
        /// </summary>
        [Networked]
        public NetworkString<_16> PlayerName { get; set; }

        /// <summary>
        /// Player's selected character class.
        /// </summary>
        [Networked]
        public CharacterClass PlayerClass { get; set; }

        /// <summary>
        /// Player's current level (1-20 for MVP).
        /// </summary>
        [Networked]
        public int Level { get; set; }

        #endregion

        #region Networked Base Stats (Without Equipment)

        /// <summary>
        /// Base attack damage (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseAttack { get; set; }

        /// <summary>
        /// Base defense (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseDefense { get; set; }

        /// <summary>
        /// Base maximum health (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseMaxHealth { get; set; }

        /// <summary>
        /// Base maximum mana (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseMaxMana { get; set; }

        /// <summary>
        /// Base attack speed multiplier (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseAttackSpeed { get; set; }

        /// <summary>
        /// Base movement speed multiplier (before equipment bonuses)
        /// </summary>
        [Networked]
        public float BaseMovementSpeed { get; set; }

        #endregion

        #region Networked Current Stats

        /// <summary>
        /// Player's current health points.
        /// </summary>
        [Networked]
        public float Health { get; set; }

        /// <summary>
        /// Player's current mana points.
        /// </summary>
        [Networked]
        public float Mana { get; set; }

        #endregion

        #region Equipment Bonuses (Calculated Locally)

        private ItemStats _equipmentBonuses = ItemStats.Zero;

        /// <summary>
        /// Gets the total equipment stat bonuses from all equipped items
        /// </summary>
        public ItemStats EquipmentBonuses => _equipmentBonuses;

        #endregion

        #region Total Stats (Properties for Gameplay)

        /// <summary>
        /// Total attack damage (base + equipment)
        /// </summary>
        public float TotalAttack => BaseAttack + _equipmentBonuses.attack;

        /// <summary>
        /// Total defense (base + equipment)
        /// </summary>
        public float TotalDefense => BaseDefense + _equipmentBonuses.defense;

        /// <summary>
        /// Total maximum health (base + equipment)
        /// </summary>
        public float TotalMaxHealth => BaseMaxHealth + _equipmentBonuses.maxHealth;

        /// <summary>
        /// Total maximum mana (base + equipment)
        /// </summary>
        public float TotalMaxMana => BaseMaxMana + _equipmentBonuses.maxMana;

        /// <summary>
        /// Total attack speed multiplier (base * equipment)
        /// </summary>
        public float TotalAttackSpeed => BaseAttackSpeed * _equipmentBonuses.attackSpeed;

        /// <summary>
        /// Total movement speed multiplier (base * equipment)
        /// </summary>
        public float TotalMovementSpeed => BaseMovementSpeed * _equipmentBonuses.movementSpeed;

        #endregion

        #region References

        private PlayerController _playerController;

        #endregion

        #region Fusion Lifecycle

        public override void Spawned()
        {
            base.Spawned();

            Debug.Log($"[NetworkPlayer] Player spawned. HasInputAuthority: {HasInputAuthority}, HasStateAuthority: {HasStateAuthority}");

            // Get reference to PlayerController
            _playerController = GetComponent<PlayerController>();

            if (_playerController == null)
            {
                Debug.LogError("[NetworkPlayer] PlayerController component not found!");
            }

            // Initialize player state if we have state authority (server)
            if (HasStateAuthority)
            {
                InitializePlayerState();
            }

            // Setup based on authority
            if (HasInputAuthority)
            {
                // This is the local player
                Debug.Log("[NetworkPlayer] This is the local player (has input authority).");
                OnLocalPlayerSpawned();
            }
            else
            {
                // This is a remote player
                Debug.Log("[NetworkPlayer] This is a remote player.");
                OnRemotePlayerSpawned();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Debug.Log("[NetworkPlayer] Player despawned.");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize default player state (server authority).
        /// Called when the player is first spawned on the server.
        /// </summary>
        private void InitializePlayerState()
        {
            Debug.Log("[NetworkPlayer] Initializing player state on server...");

            // Set default values (will be overridden by character selection later)
            PlayerName = "Player";
            PlayerClass = CharacterClass.None;
            Level = 1;

            // Initialize base stats (these will scale with level and class)
            BaseAttack = 10f;
            BaseDefense = 5f;
            BaseMaxHealth = 100f;
            BaseMaxMana = 100f;
            BaseAttackSpeed = 1.0f;
            BaseMovementSpeed = 1.0f;

            // Set current resources to max
            Health = TotalMaxHealth;
            Mana = TotalMaxMana;

            Debug.Log($"[NetworkPlayer] Player state initialized: {PlayerName}, Class: {PlayerClass}, Level: {Level}");
            Debug.Log($"[NetworkPlayer] Base Stats - Attack: {BaseAttack}, Defense: {BaseDefense}, HP: {BaseMaxHealth}, Mana: {BaseMaxMana}");
        }

        /// <summary>
        /// Called when this is the local player (client has input authority).
        /// Setup camera, UI, input, etc.
        /// </summary>
        private void OnLocalPlayerSpawned()
        {
            // TODO: Setup camera to follow this player
            // TODO: Enable player UI
            // TODO: Enable input controls

            Debug.Log("[NetworkPlayer] Local player setup complete.");
        }

        /// <summary>
        /// Called when this is a remote player (another client's character).
        /// Disable input, minimal setup.
        /// </summary>
        private void OnRemotePlayerSpawned()
        {
            // Disable input for remote players
            if (_playerController != null)
            {
                _playerController.enabled = false;
            }

            Debug.Log("[NetworkPlayer] Remote player setup complete.");
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Fusion calls this on clients with input authority to collect input data.
        /// This input is sent to the server for authoritative processing.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            // Only process input if we have input authority (local player)
            if (GetInput(out NetworkInputData input))
            {
                // Pass input to PlayerController for movement processing
                if (_playerController != null)
                {
                    _playerController.ProcessNetworkInput(input);
                }

                // TODO: Process combat input (skills, attacks) when combat system is ready
            }
        }

        #endregion

        #region Equipment System Integration

        /// <summary>
        /// Updates the equipment bonuses from the player's equipped items.
        /// Call this whenever equipment changes.
        /// </summary>
        public void UpdateEquipmentBonuses(ItemStats newBonuses)
        {
            ItemStats oldBonuses = _equipmentBonuses;
            _equipmentBonuses = newBonuses;

            Debug.Log($"[NetworkPlayer] Equipment bonuses updated:");
            Debug.Log($"  Total Attack: {TotalAttack} (Base: {BaseAttack} + Equip: {_equipmentBonuses.attack})");
            Debug.Log($"  Total Defense: {TotalDefense} (Base: {BaseDefense} + Equip: {_equipmentBonuses.defense})");
            Debug.Log($"  Total Max HP: {TotalMaxHealth} (Base: {BaseMaxHealth} + Equip: {_equipmentBonuses.maxHealth})");
            Debug.Log($"  Total Max Mana: {TotalMaxMana} (Base: {BaseMaxMana} + Equip: {_equipmentBonuses.maxMana})");
            Debug.Log($"  Total Attack Speed: {TotalAttackSpeed:F2}x (Base: {BaseAttackSpeed} * Equip: {_equipmentBonuses.attackSpeed})");
            Debug.Log($"  Total Move Speed: {TotalMovementSpeed:F2}x (Base: {BaseMovementSpeed} * Equip: {_equipmentBonuses.movementSpeed})");

            // Adjust current health/mana if max changed
            float maxHealthDelta = _equipmentBonuses.maxHealth - oldBonuses.maxHealth;
            float maxManaDelta = _equipmentBonuses.maxMana - oldBonuses.maxMana;

            if (maxHealthDelta != 0)
            {
                Health = Mathf.Min(Health + maxHealthDelta, TotalMaxHealth);
            }

            if (maxManaDelta != 0)
            {
                Mana = Mathf.Min(Mana + maxManaDelta, TotalMaxMana);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the player's identity (called from character selection or spawn manager).
        /// Can only be called by server (state authority).
        /// </summary>
        public void SetPlayerIdentity(string playerName, CharacterClass characterClass)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot set player identity without state authority!");
                return;
            }

            PlayerName = playerName;
            PlayerClass = characterClass;

            Debug.Log($"[NetworkPlayer] Player identity set: {playerName}, Class: {characterClass}");
        }

        /// <summary>
        /// Applies damage to the player (server authority).
        /// Takes defense into account.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot apply damage without state authority!");
                return;
            }

            // Apply defense reduction (simple formula: damage reduced by defense percentage)
            float defenseReduction = TotalDefense / (TotalDefense + 100f);
            float actualDamage = damage * (1f - defenseReduction);

            Health = Mathf.Max(0, Health - actualDamage);
            Debug.Log($"[NetworkPlayer] {PlayerName} took {actualDamage:F1} damage (Base: {damage}, Defense: {TotalDefense}). Health: {Health}/{TotalMaxHealth}");

            if (Health <= 0)
            {
                OnPlayerDeath();
            }
        }

        /// <summary>
        /// Heals the player (server authority).
        /// </summary>
        public void Heal(float amount)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot heal without state authority!");
                return;
            }

            Health = Mathf.Min(TotalMaxHealth, Health + amount);
            Debug.Log($"[NetworkPlayer] {PlayerName} healed {amount}. Health: {Health}/{TotalMaxHealth}");
        }

        /// <summary>
        /// Restores mana to the player (server authority).
        /// </summary>
        public void RestoreMana(float amount)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot restore mana without state authority!");
                return;
            }

            Mana = Mathf.Min(TotalMaxMana, Mana + amount);
            Debug.Log($"[NetworkPlayer] {PlayerName} restored {amount} mana. Mana: {Mana}/{TotalMaxMana}");
        }

        /// <summary>
        /// Spends mana for skill usage (server authority).
        /// Returns true if player had enough mana.
        /// </summary>
        public bool SpendMana(float amount)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot spend mana without state authority!");
                return false;
            }

            if (Mana < amount)
            {
                Debug.LogWarning($"[NetworkPlayer] {PlayerName} tried to spend {amount} mana but only has {Mana}");
                return false;
            }

            Mana -= amount;
            Debug.Log($"[NetworkPlayer] {PlayerName} spent {amount} mana. Mana: {Mana}/{TotalMaxMana}");
            return true;
        }

        #endregion

        #region Player Death

        private void OnPlayerDeath()
        {
            Debug.Log($"[NetworkPlayer] {PlayerName} has died!");
            // TODO: Implement death logic (respawn, drop loot, etc.)
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log Player State")]
        private void LogPlayerState()
        {
            Debug.Log("=== Player State ===");
            Debug.Log($"Name: {PlayerName}");
            Debug.Log($"Class: {PlayerClass}");
            Debug.Log($"Level: {Level}");
            Debug.Log($"Health: {Health}/{TotalMaxHealth} (Base: {BaseMaxHealth}, Equip: +{_equipmentBonuses.maxHealth})");
            Debug.Log($"Mana: {Mana}/{TotalMaxMana} (Base: {BaseMaxMana}, Equip: +{_equipmentBonuses.maxMana})");
            Debug.Log($"Attack: {TotalAttack} (Base: {BaseAttack}, Equip: +{_equipmentBonuses.attack})");
            Debug.Log($"Defense: {TotalDefense} (Base: {BaseDefense}, Equip: +{_equipmentBonuses.defense})");
            Debug.Log($"Attack Speed: {TotalAttackSpeed:F2}x (Base: {BaseAttackSpeed}, Equip: {_equipmentBonuses.attackSpeed:F2}x)");
            Debug.Log($"Move Speed: {TotalMovementSpeed:F2}x (Base: {BaseMovementSpeed}, Equip: {_equipmentBonuses.movementSpeed:F2}x)");
            Debug.Log($"Has Input Authority: {HasInputAuthority}");
            Debug.Log($"Has State Authority: {HasStateAuthority}");
        }

        #endregion
    }
}