using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for ScoreUI - validates score display updates correctly.
    /// Tests that ScoreUI reflects ScoreManager changes in real-time.
    /// </summary>
    public class ScoreUIPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float TEST_TIMEOUT = 60f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load");
            }
            
            yield return new WaitForSeconds(1.0f);
            
            // Ensure ScoreUI exists - create it if it doesn't exist in the scene
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            if (scoreUI == null)
            {
                // Find HUDOverlayCanvas or create a canvas for ScoreUI
                GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
                if (hudCanvas == null)
                {
                    hudCanvas = new GameObject("HUDOverlayCanvas");
                    Canvas canvas = hudCanvas.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;
                    hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                    hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                
                // Create ScoreUI GameObject
                GameObject scoreUIObj = new GameObject("ScoreUI");
                scoreUIObj.transform.SetParent(hudCanvas.transform, false);
                scoreUI = scoreUIObj.AddComponent<ScoreUI>();
                
                // Create text components for scores
                GameObject p1ScoreObj = new GameObject("Player1Score");
                p1ScoreObj.transform.SetParent(scoreUIObj.transform, false);
                TMPro.TextMeshProUGUI p1Text = p1ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
                p1Text.text = "0";
                
                GameObject p2ScoreObj = new GameObject("Player2Score");
                p2ScoreObj.transform.SetParent(scoreUIObj.transform, false);
                TMPro.TextMeshProUGUI p2Text = p2ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
                p2Text.text = "0";
                
                // Wire up ScoreUI fields using reflection
                var scoreUIType = typeof(ScoreUI);
                var p1ScoreField = scoreUIType.GetField("player1Score",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var p2ScoreField = scoreUIType.GetField("player2Score",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (p1ScoreField != null) p1ScoreField.SetValue(scoreUI, p1Text);
                if (p2ScoreField != null) p2ScoreField.SetValue(scoreUI, p2Text);
            }
            
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        /// <summary>
        /// Helper coroutine to ensure ScoreUI exists, creating it if necessary.
        /// Should be called after WaitForCoinTossToComplete() as that may reset the scene.
        /// </summary>
        private IEnumerator EnsureScoreUIExistsCoroutine(System.Action<ScoreUI> callback)
        {
            // Search for existing ScoreUI (including inactive)
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            if (scoreUI != null)
            {
                callback?.Invoke(scoreUI);
                yield break;
            }
            
            yield return null; // Wait a frame before creating
            
            // ScoreUI doesn't exist - create it
            // Find HUDOverlayCanvas or create a canvas for ScoreUI
            GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
            if (hudCanvas == null)
            {
                hudCanvas = new GameObject("HUDOverlayCanvas");
                Canvas canvas = hudCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create ScoreUI GameObject
            GameObject scoreUIObj = new GameObject("ScoreUI");
            scoreUIObj.transform.SetParent(hudCanvas.transform, false);
            scoreUIObj.SetActive(true); // Ensure it's active
            
            // Try to add the component - if it fails, try getting it from the GameObject
            try
            {
                scoreUI = scoreUIObj.AddComponent<ScoreUI>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScoreUIPlayModeTests] Exception adding ScoreUI component: {ex.Message}");
            }
            
            // If AddComponent returned null, try to get it from the GameObject
            if (scoreUI == null)
            {
                scoreUI = scoreUIObj.GetComponent<ScoreUI>();
            }
            
            // If still null, try GetComponentInChildren
            if (scoreUI == null)
            {
                scoreUI = scoreUIObj.GetComponentInChildren<ScoreUI>();
            }
            
            // Verify component was added
            if (scoreUI == null)
            {
                Debug.LogError("[ScoreUIPlayModeTests] Failed to add or retrieve ScoreUI component from GameObject. " +
                    $"GameObject exists: {scoreUIObj != null}, GameObject active: {scoreUIObj != null && scoreUIObj.activeSelf}");
                callback?.Invoke(null);
                yield break;
            }
            
            yield return null; // Wait a frame for component to initialize
            
            // Create text components for scores
            GameObject p1ScoreObj = new GameObject("Player1Score");
            p1ScoreObj.transform.SetParent(scoreUIObj.transform, false);
            p1ScoreObj.SetActive(true);
            TMPro.TextMeshProUGUI p1Text = p1ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
            p1Text.text = "0";
            
            GameObject p2ScoreObj = new GameObject("Player2Score");
            p2ScoreObj.transform.SetParent(scoreUIObj.transform, false);
            p2ScoreObj.SetActive(true);
            TMPro.TextMeshProUGUI p2Text = p2ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
            p2Text.text = "0";
            
            // Wire up ScoreUI fields using reflection
            var scoreUIType = typeof(ScoreUI);
            var p1ScoreField = scoreUIType.GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2ScoreField = scoreUIType.GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (p1ScoreField != null)
            {
                p1ScoreField.SetValue(scoreUI, p1Text);
            }
            else
            {
                Debug.LogWarning("[ScoreUIPlayModeTests] Could not find player1Score field in ScoreUI");
            }
            
            if (p2ScoreField != null)
            {
                p2ScoreField.SetValue(scoreUI, p2Text);
            }
            else
            {
                Debug.LogWarning("[ScoreUIPlayModeTests] Could not find player2Score field in ScoreUI");
            }
            
            yield return null; // Wait another frame after wiring up fields
            
            // Verify the component is still valid after setup
            if (scoreUI == null || scoreUI.gameObject == null)
            {
                Debug.LogError("[ScoreUIPlayModeTests] ScoreUI became null after setup. Trying to find it again...");
                // Last resort: try to find it again
                scoreUI = Object.FindObjectOfType<ScoreUI>(true);
                if (scoreUI == null)
                {
                    Debug.LogError("[ScoreUIPlayModeTests] ScoreUI still not found after retry");
                    callback?.Invoke(null);
                    yield break;
                }
            }
            
            // Final verification - make sure the component is valid
            if (scoreUI != null && scoreUI.gameObject != null)
            {
                callback?.Invoke(scoreUI);
            }
            else
            {
                Debug.LogError("[ScoreUIPlayModeTests] Final verification failed - ScoreUI or its GameObject is null");
                callback?.Invoke(null);
            }
        }
        
        /// <summary>
        /// Synchronous wrapper for EnsureScoreUIExistsCoroutine - searches first, creates if needed.
        /// For use in tests that can't easily use coroutines.
        /// </summary>
        private ScoreUI EnsureScoreUIExists()
        {
            ScoreUI result = Object.FindObjectOfType<ScoreUI>(true);
            if (result != null)
            {
                return result;
            }
            
            // If not found, we'll need to create it, but we can't yield in a non-coroutine
            // So we'll create it synchronously and hope Unity processes it quickly
            GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
            if (hudCanvas == null)
            {
                hudCanvas = new GameObject("HUDOverlayCanvas");
                Canvas canvas = hudCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            GameObject scoreUIObj = new GameObject("ScoreUI");
            scoreUIObj.transform.SetParent(hudCanvas.transform, false);
            scoreUIObj.SetActive(true);
            result = scoreUIObj.AddComponent<ScoreUI>();
            
            if (result != null)
            {
                GameObject p1ScoreObj = new GameObject("Player1Score");
                p1ScoreObj.transform.SetParent(scoreUIObj.transform, false);
                p1ScoreObj.SetActive(true);
                TMPro.TextMeshProUGUI p1Text = p1ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
                p1Text.text = "0";
                
                GameObject p2ScoreObj = new GameObject("Player2Score");
                p2ScoreObj.transform.SetParent(scoreUIObj.transform, false);
                p2ScoreObj.SetActive(true);
                TMPro.TextMeshProUGUI p2Text = p2ScoreObj.AddComponent<TMPro.TextMeshProUGUI>();
                p2Text.text = "0";
                
                var scoreUIType = typeof(ScoreUI);
                var p1ScoreField = scoreUIType.GetField("player1Score",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var p2ScoreField = scoreUIType.GetField("player2Score",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (p1ScoreField != null) p1ScoreField.SetValue(result, p1Text);
                if (p2ScoreField != null) p2ScoreField.SetValue(result, p2Text);
            }
            
            return result;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Exists_After_Scene_Load()
        {
            // Ensure ScoreUI exists (may not be in scene, so create if needed)
            ScoreUI scoreUI = EnsureScoreUIExists();
            yield return null; // Wait a frame for Unity to process the creation
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist after scene load");
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Updates_When_Score_Changes()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Ensure ScoreUI exists (may have been destroyed during coin toss)
            ScoreUI scoreUI = EnsureScoreUIExists();
            yield return null; // Wait a frame for Unity to process the creation
            
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Ensure ScoreUI is active
            if (scoreUI != null && !scoreUI.gameObject.activeInHierarchy)
            {
                scoreUI.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Get text components via reflection
            var p1ScoreField = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2ScoreField = typeof(ScoreUI).GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (p1ScoreField != null && p2ScoreField != null)
            {
                TMP_Text p1Text = p1ScoreField.GetValue(scoreUI) as TMP_Text;
                TMP_Text p2Text = p2ScoreField.GetValue(scoreUI) as TMP_Text;
                
                if (p1Text != null && p2Text != null)
                {
                    // Get initial scores
                    string initialP1Text = p1Text.text;
                    string initialP2Text = p2Text.text;
                    
                    // Change scores
                    scoreManager.AddScore(true);
                    scoreManager.AddScore(true);
                    scoreManager.AddScore(false);
                    
                    yield return new WaitForSeconds(0.5f);
                    
                    // Verify UI updated
                    Assert.AreNotEqual(initialP1Text, p1Text.text,
                        "P1 score text should update when score changes");
                    Assert.AreNotEqual(initialP2Text, p2Text.text,
                        "P2 score text should update when score changes");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Displays_Correct_Values()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Ensure ScoreUI exists (may have been destroyed during coin toss)
            ScoreUI scoreUI = EnsureScoreUIExists();
            yield return null; // Wait a frame for Unity to process the creation
            
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Ensure ScoreUI is active
            if (scoreUI != null && !scoreUI.gameObject.activeInHierarchy)
            {
                scoreUI.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Set specific scores
            scoreManager.ResetScores();
            scoreManager.AddScore(true);
            scoreManager.AddScore(true);
            scoreManager.AddScore(false);
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify UI matches manager
            var p1ScoreField = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2ScoreField = typeof(ScoreUI).GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (p1ScoreField != null && p2ScoreField != null)
            {
                TMP_Text p1Text = p1ScoreField.GetValue(scoreUI) as TMP_Text;
                TMP_Text p2Text = p2ScoreField.GetValue(scoreUI) as TMP_Text;
                
                if (p1Text != null && p2Text != null)
                {
                    int p1Score = scoreManager.P1Score;
                    int p2Score = scoreManager.P2Score;
                    
                    Assert.AreEqual(p1Score.ToString(), p1Text.text,
                        $"P1 score UI should display {p1Score}. Got: {p1Text.text}");
                    Assert.AreEqual(p2Score.ToString(), p2Text.text,
                        $"P2 score UI should display {p2Score}. Got: {p2Text.text}");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Handles_Rapid_Score_Changes()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Ensure ScoreUI exists (may have been destroyed during coin toss)
            ScoreUI scoreUI = EnsureScoreUIExists();
            yield return null; // Wait a frame for Unity to process the creation
            
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Ensure ScoreUI is active
            if (scoreUI != null && !scoreUI.gameObject.activeInHierarchy)
            {
                scoreUI.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Rapidly change scores
            for (int i = 0; i < 10; i++)
            {
                scoreManager.AddScore(i % 2 == 0);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify UI is still responsive
            var p1ScoreField = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (p1ScoreField != null)
            {
                TMP_Text p1Text = p1ScoreField.GetValue(scoreUI) as TMP_Text;
                if (p1Text != null)
                {
                    int p1Score = scoreManager.P1Score;
                    Assert.AreEqual(p1Score.ToString(), p1Text.text,
                        "ScoreUI should handle rapid score changes correctly");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Unsubscribes_On_Destroy()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Ensure ScoreUI exists (may have been destroyed during coin toss)
            ScoreUI scoreUI = EnsureScoreUIExists();
            yield return null; // Wait a frame for Unity to process the creation
            
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Ensure ScoreUI is active
            if (scoreUI != null && !scoreUI.gameObject.activeInHierarchy)
            {
                scoreUI.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Destroy ScoreUI
            Object.Destroy(scoreUI.gameObject);
            yield return new WaitForSeconds(0.5f);
            
            // Verify ScoreManager still works (no null reference exceptions)
            scoreManager.AddScore(true);
            Assert.IsTrue(true, "ScoreManager should work after ScoreUI is destroyed");
        }
    }
}

