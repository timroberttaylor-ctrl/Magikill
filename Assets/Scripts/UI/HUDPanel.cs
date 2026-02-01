using UnityEngine;
using Magikill.Combat;
using NetworkPlayer = Magikill.Networking.NetworkPlayer;


namespace Magikill.UI
{
    /// <summary>
    /// Main HUD panel that displays player health, mana, and skills.
    /// Automatically finds child UI components and connects them to the local player.
    /// Always visible during gameplay.
    /// </summary>
    public class HUDPanel : UIPanel
    {
        #region Child UI Components

        [Header("HUD Components")]
        [SerializeField]
        [Tooltip("Health bar UI component")]
        private HealthBarUI healthBar;

        [SerializeField]
        [Tooltip("Mana bar UI component")]
        private ManaBarUI manaBar;

        [SerializeField]
        [Tooltip("Skill bar UI component with 6 skill buttons")]
        private SkillBarUI skillBar;

        #endregion

        #region Player References

        private NetworkPlayer _localPlayer;
        private CombatStats _combatStats;
        private SkillSystem _skillSystem;

        #endregion

        #region State

        private bool _isConnectedToPlayer = false;
        private bool _hasTriedInitialConnect = false;


        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Auto-find child components if not assigned
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<HealthBarUI>();
            }

            if (manaBar == null)
            {
                manaBar = GetComponentInChildren<ManaBarUI>();
            }

            if (skillBar == null)
            {
                skillBar = GetComponentInChildren<SkillBarUI>();
            }

            // Verify components were found
            if (healthBar == null)
            {
                Debug.LogWarning("[HUDPanel] HealthBarUI component not found in children!");
            }

            if (manaBar == null)
            {
                Debug.LogWarning("[HUDPanel] ManaBarUI component not found in children!");
            }

            if (skillBar == null)
            {
                Debug.LogWarning("[HUDPanel] SkillBarUI component not found in children!");
            }

            Debug.Log("[HUDPanel] Initialized. Waiting for local player...");
        }

        private void Start()
        {
            // Try once on startup
            TryConnectToLocalPlayer();
        }

        private void Update()
        {
            // Try to connect to local player if not already connected
            if (!_isConnectedToPlayer)
            {
                TryConnectToLocalPlayer();
            }
        }

        private void OnDestroy()
        {
            DisconnectFromPlayer();
        }

        #endregion

        #region Player Connection

        /// <summary>
        /// Attempts to find and connect to the local player.
        /// </summary>
        private void TryConnectToLocalPlayer()
        {
            if (_isConnectedToPlayer)
                return;

            NetworkPlayer[] allPlayers = FindObjectsOfType<NetworkPlayer>();

            foreach (NetworkPlayer player in allPlayers)
            {
                if (player.HasInputAuthority)
                {
                    ConnectToPlayer(player);
                    return;
                }
            }

            // If no player yet, retry once shortly later
            if (!_hasTriedInitialConnect)
            {
                _hasTriedInitialConnect = true;
                Invoke(nameof(TryConnectToLocalPlayer), 0.25f);
            }
        }


        /// <summary>
        /// Connects HUD to the specified player.
        /// </summary>
        private void ConnectToPlayer(NetworkPlayer player)
        {
            if (_isConnectedToPlayer)
            {
                Debug.LogWarning("[HUDPanel] Already connected to a player!");
                return;
            }

            _localPlayer = player;

            // Get combat components
            _combatStats = player.GetComponent<CombatStats>();
            _skillSystem = player.GetComponent<SkillSystem>();

            if (_combatStats == null)
            {
                Debug.LogError("[HUDPanel] Local player has no CombatStats component!");
                return;
            }

            if (_skillSystem == null)
            {
                Debug.LogError("[HUDPanel] Local player has no SkillSystem component!");
                return;
            }

            // Connect health bar
            if (healthBar != null)
            {
                healthBar.Initialize(_combatStats);
            }

            // Connect mana bar
            if (manaBar != null)
            {
                manaBar.Initialize(_combatStats);
            }

            // Connect skill bar
            if (skillBar != null)
            {
                skillBar.Initialize(_skillSystem, _combatStats);
            }

            _isConnectedToPlayer = true;
            // Ensure HUD is visible only after successful connection
            Show();

            Debug.Log($"[HUDPanel] Connected to local player: {player.PlayerName}");
        }

        /// <summary>
        /// Disconnects HUD from the current player.
        /// </summary>
        private void DisconnectFromPlayer()
        {
            if (!_isConnectedToPlayer)
            {
                return;
            }

            // Disconnect health bar
            if (healthBar != null)
            {
                healthBar.Cleanup();
            }

            // Disconnect mana bar
            if (manaBar != null)
            {
                manaBar.Cleanup();
            }

            // Disconnect skill bar
            if (skillBar != null)
            {
                skillBar.Cleanup();
            }

            _localPlayer = null;
            _combatStats = null;
            _skillSystem = null;
            _isConnectedToPlayer = false;

            Debug.Log("[HUDPanel] Disconnected from player.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually force reconnection to local player.
        /// Useful if player respawns or changes.
        /// </summary>
        public void Reconnect()
        {
            DisconnectFromPlayer();
            _isConnectedToPlayer = false;
            TryConnectToLocalPlayer();
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Reconnect to Player")]
        private void DebugReconnect()
        {
            Reconnect();
        }

        [ContextMenu("Log Connection Status")]
        private void LogConnectionStatus()
        {
            Debug.Log("=== HUD Connection Status ===");
            Debug.Log($"Connected to Player: {_isConnectedToPlayer}");
            if (_isConnectedToPlayer)
            {
                Debug.Log($"Player Name: {_localPlayer?.PlayerName}");
                Debug.Log($"CombatStats: {(_combatStats != null ? "Found" : "Missing")}");
                Debug.Log($"SkillSystem: {(_skillSystem != null ? "Found" : "Missing")}");
            }
            Debug.Log($"HealthBar: {(healthBar != null ? "Found" : "Missing")}");
            Debug.Log($"ManaBar: {(manaBar != null ? "Found" : "Missing")}");
            Debug.Log($"SkillBar: {(skillBar != null ? "Found" : "Missing")}");
        }

        #endregion
    }
}
