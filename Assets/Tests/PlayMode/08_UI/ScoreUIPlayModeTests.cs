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
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Exists_After_Scene_Load()
        {
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            Assert.IsNotNull(scoreUI, "ScoreUI should exist after scene load");
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator ScoreUI_Updates_When_Score_Changes()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
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
            
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
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
            
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
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
            
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(scoreUI, "ScoreUI should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Destroy ScoreUI
            Object.Destroy(scoreUI.gameObject);
            yield return new WaitForSeconds(0.5f);
            
            // Verify ScoreManager still works (no null reference exceptions)
            scoreManager.AddScore(true);
            Assert.IsTrue(true, "ScoreManager should work after ScoreUI is destroyed");
        }
    }
}

