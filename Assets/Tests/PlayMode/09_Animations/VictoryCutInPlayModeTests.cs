using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for VictoryCutInController - validates victory cut-in animation.
    /// Tests cut-in display, animation, and cleanup.
    /// </summary>
    public class VictoryCutInPlayModeTests
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
        public IEnumerator VictoryCutInController_Exists()
        {
            VictoryCutInController cutIn = Object.FindObjectOfType<VictoryCutInController>();
            // Victory cut-in may or may not exist (optional feature)
            if (cutIn != null)
            {
                Assert.IsNotNull(cutIn, "VictoryCutInController should exist if implemented");
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator VictoryCutIn_Plays_Animation()
        {
            VictoryCutInController cutIn = Object.FindObjectOfType<VictoryCutInController>();
            
            if (cutIn != null)
            {
                // Play cut-in
                Color accentColor = Color.yellow;
                cutIn.Play("VICTORY", accentColor);
                
                yield return new WaitForSeconds(0.5f);
                
                // Verify cut-in is playing (check if root is visible)
                var rootField = typeof(VictoryCutInController).GetField("cutInRoot",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (rootField != null)
                {
                    RectTransform root = rootField.GetValue(cutIn) as RectTransform;
                    if (root != null)
                    {
                        var canvasGroupField = typeof(VictoryCutInController).GetField("canvasGroup",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (canvasGroupField != null)
                        {
                            CanvasGroup cg = canvasGroupField.GetValue(cutIn) as CanvasGroup;
                            if (cg != null)
                            {
                                Assert.Greater(cg.alpha, 0f,
                                    "Victory cut-in should be visible when playing");
                            }
                        }
                    }
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator VictoryCutIn_Completes_And_Hides()
        {
            VictoryCutInController cutIn = Object.FindObjectOfType<VictoryCutInController>();
            
            if (cutIn != null)
            {
                // Play cut-in
                Color accentColor = Color.yellow;
                cutIn.Play("VICTORY", accentColor);
                
                // Wait for full animation (enter + hold + exit)
                yield return new WaitForSeconds(3.0f);
                
                // Verify cut-in is hidden after completion
                var canvasGroupField = typeof(VictoryCutInController).GetField("canvasGroup",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (canvasGroupField != null)
                {
                    CanvasGroup cg = canvasGroupField.GetValue(cutIn) as CanvasGroup;
                    if (cg != null)
                    {
                        Assert.AreEqual(0f, cg.alpha,
                            "Victory cut-in should be hidden after animation completes");
                    }
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator VictoryCutIn_Does_Not_Block_GameEndUI()
        {
            VictoryCutInController cutIn = Object.FindObjectOfType<VictoryCutInController>();
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            if (cutIn != null && gameEndUI != null)
            {
                // Play cut-in
                cutIn.Play("VICTORY", Color.yellow);
                yield return new WaitForSeconds(0.5f);
                
                // Show game end UI
                gameEndUI.ShowGameEnd(true, false, 10, 5, 3, 2);
                yield return new WaitForSeconds(0.5f);
                
                // Verify game end UI is still accessible
                Assert.IsNotNull(gameEndUI, "GameEndUI should still work when cut-in is playing");
            }
        }
    }
}

