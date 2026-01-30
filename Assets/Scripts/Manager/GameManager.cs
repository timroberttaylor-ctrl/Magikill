using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magikill.Core
{
    /// <summary>
    /// Central game manager that acts as a service locator.
    /// Auto-registers all IGameService components and manages their lifecycle.
    /// Persists across scenes using DontDestroyOnLoad.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] Instance accessed before initialization!");
                }
                return _instance;
            }
        }

        #endregion

        #region Service Storage

        private Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();
        private bool _isInitialized = false;

        #endregion

        #region Convenience Properties for Core Services

        /// <summary>
        /// Quick access to SceneLoader service.
        /// Usage: GameManager.Scenes.LoadSceneAdditive("MainMenu");
        /// </summary>
        public static SceneLoader Scenes => GetService<SceneLoader>();

        /// <summary>
        /// Quick access to PlayerSpawnManager service.
        /// Usage: GameManager.PlayerSpawner.GetAllPlayers();
        /// </summary>
        public static PlayerSpawnManager PlayerSpawner => GetService<PlayerSpawnManager>();

        // Add more convenience properties here as you create services
        // Example: public static NetworkManager Network => GetService<NetworkManager>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern enforcement
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate instance detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            AutoRegisterServices();
            InitializeServices();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ShutdownServices();
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            ShutdownServices();
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// Automatically finds and registers all IGameService components
        /// attached to this GameObject and its children.
        /// </summary>
        private void AutoRegisterServices()
        {
            // Find all IGameService components on this GameObject and children
            IGameService[] foundServices = GetComponentsInChildren<IGameService>(true);

            foreach (IGameService service in foundServices)
            {
                Type serviceType = service.GetType();

                if (_services.ContainsKey(serviceType))
                {
                    Debug.LogWarning($"[GameManager] Service {serviceType.Name} already registered. Skipping duplicate.");
                    continue;
                }

                _services.Add(serviceType, service);
                Debug.Log($"[GameManager] Registered service: {serviceType.Name} (Priority: {service.Priority})");
            }

            Debug.Log($"[GameManager] Auto-registration complete. Total services: {_services.Count}");
        }

        /// <summary>
        /// Manually register a service (for edge cases).
        /// </summary>
        public void RegisterService<T>(T service) where T : IGameService
        {
            Type serviceType = typeof(T);

            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"[GameManager] Service {serviceType.Name} already registered. Replacing.");
                _services[serviceType] = service;
            }
            else
            {
                _services.Add(serviceType, service);
                Debug.Log($"[GameManager] Manually registered service: {serviceType.Name}");
            }
        }

        #endregion

        #region Service Initialization

        /// <summary>
        /// Initializes all registered services in priority order (lowest first).
        /// </summary>
        private void InitializeServices()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameManager] Services already initialized.");
                return;
            }

            // Sort services by priority
            var sortedServices = _services.Values.OrderBy(s => s.Priority).ToList();

            Debug.Log("[GameManager] Initializing services in priority order...");

            foreach (IGameService service in sortedServices)
            {
                try
                {
                    Debug.Log($"[GameManager] Initializing {service.GetType().Name}...");
                    service.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameManager] Failed to initialize {service.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            _isInitialized = true;
            Debug.Log("[GameManager] All services initialized successfully.");
        }

        /// <summary>
        /// Shuts down all services in reverse priority order (highest first).
        /// </summary>
        private void ShutdownServices()
        {
            if (!_isInitialized)
            {
                return;
            }

            // Sort services by priority in reverse (shutdown in opposite order)
            var sortedServices = _services.Values.OrderByDescending(s => s.Priority).ToList();

            Debug.Log("[GameManager] Shutting down services...");

            foreach (IGameService service in sortedServices)
            {
                try
                {
                    Debug.Log($"[GameManager] Shutting down {service.GetType().Name}...");
                    service.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameManager] Failed to shutdown {service.GetType().Name}: {ex.Message}");
                }
            }

            _services.Clear();
            _isInitialized = false;
            Debug.Log("[GameManager] All services shut down.");
        }

        #endregion

        #region Service Locator

        /// <summary>
        /// Retrieves a service by type. Use this for accessing services throughout the game.
        /// </summary>
        public static T GetService<T>() where T : class, IGameService
        {
            if (_instance == null)
            {
                Debug.LogError("[GameManager] Cannot get service - GameManager instance is null!");
                return null;
            }

            Type serviceType = typeof(T);

            if (_instance._services.TryGetValue(serviceType, out IGameService service))
            {
                return service as T;
            }

            Debug.LogError($"[GameManager] Service {serviceType.Name} not found!");
            return null;
        }

        /// <summary>
        /// Checks if a service is registered.
        /// </summary>
        public static bool HasService<T>() where T : class, IGameService
        {
            if (_instance == null) return false;
            return _instance._services.ContainsKey(typeof(T));
        }

        #endregion

        #region Debug Utilities

        /// <summary>
        /// Logs all registered services and their priorities.
        /// Useful for debugging initialization order.
        /// </summary>
        [ContextMenu("Log All Services")]
        public void LogAllServices()
        {
            Debug.Log("=== Registered Services ===");
            var sortedServices = _services.Values.OrderBy(s => s.Priority).ToList();

            foreach (IGameService service in sortedServices)
            {
                Debug.Log($"Priority {service.Priority}: {service.GetType().Name}");
            }

            Debug.Log($"Total: {_services.Count} services");
        }

        #endregion
    }
}