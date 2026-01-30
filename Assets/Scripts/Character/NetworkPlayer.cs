using Fusion;
using UnityEngine;
using Magikill.Character;

namespace Magikill.Networking
{
    /// <summary>
    /// Core networking component for player characters.
    /// Handles input collection, network state synchronization, and player identity.
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

        /// <summary>
        /// Player's current health points.
        /// </summary>
        [Networked]
        public float Health { get; set; }

        /// <summary>
        /// Player's maximum health points.
        /// </summary>
        [Networked]
        public float MaxHealth { get; set; }

        /// <summary>
        /// Player's current mana points.
        /// </summary>
        [Networked]
        public float Mana { get; set; }

        /// <summary>
        /// Player's maximum mana points.
        /// </summary>
        [Networked]
        public float MaxMana { get; set; }

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
            MaxHealth = 100f;
            Health = MaxHealth;
            MaxMana = 100f;
            Mana = MaxMana;

            Debug.Log($"[NetworkPlayer] Player state initialized: {PlayerName}, Class: {PlayerClass}, Level: {Level}");
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
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!HasStateAuthority)
            {
                Debug.LogWarning("[NetworkPlayer] Cannot apply damage without state authority!");
                return;
            }

            Health = Mathf.Max(0, Health - damage);
            Debug.Log($"[NetworkPlayer] {PlayerName} took {damage} damage. Health: {Health}/{MaxHealth}");

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

            Health = Mathf.Min(MaxHealth, Health + amount);
            Debug.Log($"[NetworkPlayer] {PlayerName} healed {amount}. Health: {Health}/{MaxHealth}");
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
            Debug.Log($"Health: {Health}/{MaxHealth}");
            Debug.Log($"Mana: {Mana}/{MaxMana}");
            Debug.Log($"Has Input Authority: {HasInputAuthority}");
            Debug.Log($"Has State Authority: {HasStateAuthority}");
        }

        #endregion
    }
}
