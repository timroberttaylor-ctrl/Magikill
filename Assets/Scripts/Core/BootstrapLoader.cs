using System.Collections;
using UnityEngine;

namespace Magikill.Core
{
    /// <summary>
    /// Handles the initial bootstrap flow.
    /// Waits for GameManager to initialize, then loads the MainMenu scene additively.
    /// This script should be placed in the Bootstrap scene.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Scene to load after bootstrap initialization")]
        private string sceneToLoad = "MainMenu";

        [SerializeField]
        [Tooltip("Delay in seconds before loading the next scene")]
        private float loadDelay = 0.5f;

        private void Start()
        {
            Debug.Log("[BootstrapLoader] Bootstrap started. Initializing...");
            StartCoroutine(LoadInitialScene());
        }

        private IEnumerator LoadInitialScene()
        {
            // Wait for the configured delay to ensure all services are fully initialized
            Debug.Log($"[BootstrapLoader] Waiting {loadDelay} seconds for services to initialize...");
            yield return new WaitForSeconds(loadDelay);

            // Verify GameManager is ready
            if (GameManager.Instance == null)
            {
                Debug.LogError("[BootstrapLoader] GameManager not found! Cannot proceed with scene loading.");
                yield break;
            }

            // Verify SceneLoader is available
            if (GameManager.Scenes == null)
            {
                Debug.LogError("[BootstrapLoader] SceneLoader not found! Cannot proceed with scene loading.");
                yield break;
            }

            // Load the initial scene additively
            Debug.Log($"[BootstrapLoader] Loading scene '{sceneToLoad}'...");
            GameManager.Scenes.LoadSceneAdditive(sceneToLoad);
        }
    }
}
