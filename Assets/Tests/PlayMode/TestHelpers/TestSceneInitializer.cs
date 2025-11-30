using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// Global utility for initializing test scenes with proper manager setup.
    /// Prevents NullReferenceExceptions by ensuring all managers are ready before tests run.
    /// </summary>
    public static class TestSceneInitializer
    {
        /// <summary>
        /// Loads the BattleScreenMultiplayer scene and waits for all required systems to initialize.
        /// All PlayMode tests should begin with: yield return TestSceneInitializer.LoadBattleScene();
        /// </summary>
        public static IEnumerator LoadBattleScene()
        {
            // Load the scene
            SceneManager.LoadScene("BattleScreenMultiplayer", LoadSceneMode.Single);
            yield return null;
            yield return null; // Allow managers, canvases, and event system to initialize

            // Wait for all required managers to be ready
            yield return new WaitUntil(() => GameManager.Instance != null);
            yield return new WaitUntil(() => Object.FindObjectOfType<HUDManager>() != null);
            yield return new WaitUntil(() => CoinTossManager.Instance != null);
            
            // Additional frame to ensure everything is fully initialized
            yield return null;
        }

        /// <summary>
        /// Loads a specific scene and waits for initialization.
        /// </summary>
        public static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            yield return null;
            yield return null; // Allow managers, canvases, and event system to initialize

            // Wait for core managers if they exist in this scene
            yield return new WaitUntil(() => GameManager.Instance != null || Time.time > 5f);
            
            yield return null;
        }
    }
}

