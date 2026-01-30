using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Magikill.Core
{
    /// <summary>
    /// Manages scene loading and unloading using Unity's async scene management.
    /// Supports additive scene loading for persistent Bootstrap architecture.
    /// Priority: 1 (initializes after NetworkManager)
    /// </summary>
    public class SceneLoader : MonoBehaviour, IGameService
    {
        #region IGameService Implementation

        public int Priority => 1;

        #endregion

        #region Scene Loading State

        private bool _isLoading = false;
        private string _currentlyLoadingScene = "";

        public bool IsLoading => _isLoading;
        public string CurrentlyLoadingScene => _currentlyLoadingScene;

        #endregion

        #region C# Events

        public event Action<string> OnSceneLoadStartedEvent;
        public event Action<string> OnSceneLoadedEvent;
        public event Action<string> OnSceneUnloadStartedEvent;
        public event Action<string> OnSceneUnloadedEvent;

        #endregion

        #region Unity Events

        [Header("Unity Events")]
        public UnityEvent<string> OnSceneLoadStartedUnityEvent;
        public UnityEvent<string> OnSceneLoadedUnityEvent;
        public UnityEvent<string> OnSceneUnloadStartedUnityEvent;
        public UnityEvent<string> OnSceneUnloadedUnityEvent;

        #endregion

        #region IGameService Lifecycle

        public void Initialize()
        {
            Debug.Log("[SceneLoader] Initialized and ready.");
        }

        public void Shutdown()
        {
            Debug.Log("[SceneLoader] Shutting down...");
            StopAllCoroutines();
            _isLoading = false;
            _currentlyLoadingScene = "";
        }

        #endregion

        #region Scene Loading Methods

        /// <summary>
        /// Loads a scene additively (keeps existing scenes loaded).
        /// This is the primary loading method for the persistent Bootstrap architecture.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load (must be in Build Settings)</param>
        public void LoadSceneAdditive(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading scene '{_currentlyLoadingScene}'. Cannot load '{sceneName}' yet.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is null or empty!");
                return;
            }

            // Check if scene is already loaded
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is already loaded!");
                return;
            }

            Debug.Log($"[SceneLoader] Starting to load scene '{sceneName}' additively...");
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Unloads a previously loaded scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to unload</param>
        public void UnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is null or empty!");
                return;
            }

            // Check if scene is actually loaded
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is not loaded, cannot unload!");
                return;
            }

            Debug.Log($"[SceneLoader] Starting to unload scene '{sceneName}'...");
            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        #endregion

        #region Async Loading Coroutines

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            _currentlyLoadingScene = sceneName;

            // Fire start events
            InvokeSceneLoadStarted(sceneName);

            // Start async load operation
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            if (asyncLoad == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{sceneName}'. Is it in Build Settings?");
                _isLoading = false;
                _currentlyLoadingScene = "";
                yield break;
            }

            // Wait until the scene is fully loaded
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded successfully!");

            // Fire completion events
            InvokeSceneLoaded(sceneName);

            _isLoading = false;
            _currentlyLoadingScene = "";
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            // Fire start events
            InvokeSceneUnloadStarted(sceneName);

            // Start async unload operation
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            if (asyncUnload == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start unloading scene '{sceneName}'.");
                yield break;
            }

            // Wait until the scene is fully unloaded
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            Debug.Log($"[SceneLoader] Scene '{sceneName}' unloaded successfully!");

            // Fire completion events
            InvokeSceneUnloaded(sceneName);
        }

        #endregion

        #region Event Invocation Helpers

        private void InvokeSceneLoadStarted(string sceneName)
        {
            OnSceneLoadStartedEvent?.Invoke(sceneName);
            OnSceneLoadStartedUnityEvent?.Invoke(sceneName);
            Debug.Log($"[SceneLoader] Scene load started: {sceneName} - events fired.");
        }

        private void InvokeSceneLoaded(string sceneName)
        {
            OnSceneLoadedEvent?.Invoke(sceneName);
            OnSceneLoadedUnityEvent?.Invoke(sceneName);
            Debug.Log($"[SceneLoader] Scene loaded: {sceneName} - events fired.");
        }

        private void InvokeSceneUnloadStarted(string sceneName)
        {
            OnSceneUnloadStartedEvent?.Invoke(sceneName);
            OnSceneUnloadStartedUnityEvent?.Invoke(sceneName);
            Debug.Log($"[SceneLoader] Scene unload started: {sceneName} - events fired.");
        }

        private void InvokeSceneUnloaded(string sceneName)
        {
            OnSceneUnloadedEvent?.Invoke(sceneName);
            OnSceneUnloadedUnityEvent?.Invoke(sceneName);
            Debug.Log($"[SceneLoader] Scene unloaded: {sceneName} - events fired.");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a specific scene is currently loaded.
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }

        #endregion

        #region Debug Utilities

        [ContextMenu("Log All Loaded Scenes")]
        private void LogAllLoadedScenes()
        {
            Debug.Log("=== Currently Loaded Scenes ===");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                Debug.Log($"[{i}] {scene.name} - Loaded: {scene.isLoaded}");
            }
        }

        #endregion
    }
}
