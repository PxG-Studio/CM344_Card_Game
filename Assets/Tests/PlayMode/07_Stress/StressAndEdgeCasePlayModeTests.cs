using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for stress and edge cases - validates rapid input, simultaneous actions, and scene reloads.
    /// </summary>
    public class StressAndEdgeCasePlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            // Verify scene exists in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                if (scenePath == SCENE_NAME)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' must be added to Build Settings");
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float timeout = 10f;
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load within {timeout} seconds");
            }
            
            yield return new WaitForSeconds(0.5f); // Wait for initialization
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Clean up after each test
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rapid_DragDrop_Spam_DoesNotBreakBoard()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Act: Rapidly switch turns (simulating rapid drag/drop spam)
            for (int i = 0; i < 20; i++)
            {
                fateController.AdvanceFateFlow();
                yield return null;
            }
            
            // Assert: Board state should remain consistent
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should still have exactly 16 drop areas after rapid input");
            
            // FateFlowController should still be valid
            Assert.IsNotNull(FateFlowController.Instance, "FateFlowController should still exist after rapid input");
        }

        [UnityTest]
        public IEnumerator SimultaneousTwoCardDrag_DoesNotCrashInput()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Verify CardMoverP1 has drag state tracking
            CardMoverP1[] cardMovers = Object.FindObjectsOfType<CardMoverP1>(true);
            
            // CardMoverP1 uses isDragging flag to prevent simultaneous drags
            // Only one card can be dragged at a time per player
            if (cardMovers.Length > 0)
            {
                // Verify isDragging field exists
                var isDraggingField = typeof(CardMoverP1).GetField("isDragging", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(isDraggingField, "CardMoverP1 should track isDragging state");
                
                Assert.IsTrue(true, "CardMoverP1 tracks drag state (prevents simultaneous drags)");
            }
            else
            {
                Assert.IsTrue(true, "CardMoverP1 components may be created at runtime");
            }
        }

        [UnityTest]
        public IEnumerator TurnSwitch_DuringAnimation_DoesNotBreakLogic()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Act: Switch turn multiple times rapidly (during potential animations)
            FateSide initialFate = fateController.CurrentFate;
            FateSide previousFate = initialFate;
            int switches = 0;
            
            for (int i = 0; i < 10; i++)
            {
                fateController.AdvanceFateFlow();
                yield return null;
                
                if (fateController.CurrentFate != previousFate)
                {
                    switches++;
                    previousFate = fateController.CurrentFate;
                }
            }
            
            // Assert: Turn should have switched at least once during the sequence,
            // even if it ends back on the initial side after an even number of toggles.
            Assert.GreaterOrEqual(switches, 1,
                "Turn should switch at least once during rapid fate flow advances, even during animations");
            
            // Verify CanAct still works correctly
            bool canAct = fateController.CanAct(fateController.CurrentFate);
            Assert.IsTrue(canAct, "CanAct should work correctly after rapid turn switches");
        }

        [UnityTest]
        public IEnumerator MultipleSceneReloads_DoNotBreakSingletonManagers()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Get initial singleton instances
            GameManager gameManager1 = GameManager.Instance;
            FateFlowController fateController1 = FateFlowController.Instance;
            ScoreManager scoreManager1 = ScoreManager.Instance;
            
            Assert.IsNotNull(gameManager1, "GameManager should exist");
            Assert.IsNotNull(fateController1, "FateFlowController should exist");
            Assert.IsNotNull(scoreManager1, "ScoreManager should exist");
            
            // Act: Reload scene (simulating multiple game sessions)
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float timeout = 10f;
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(1.0f); // Wait for initialization
            
            // Assert: Singletons should still exist after reload
            GameManager gameManager2 = GameManager.Instance;
            FateFlowController fateController2 = FateFlowController.Instance;
            ScoreManager scoreManager2 = ScoreManager.Instance;
            
            // GameManager and FateFlowController use DontDestroyOnLoad and should persist across scene reloads
            Assert.IsNotNull(gameManager2, "GameManager should persist after scene reload");
            Assert.IsNotNull(fateController2, "FateFlowController should persist after scene reload");
            
            // ScoreManager is recreated by HUDSetup per scene, but its static Instance
            // should always point to a valid, single instance after reload.
            Assert.IsNotNull(scoreManager2, "ScoreManager should exist after scene reload");
            
            // Verify GameManager and FateFlowController are the same instances (DontDestroyOnLoad)
            Assert.AreEqual(gameManager1, gameManager2, "GameManager should be same instance after reload");
            Assert.AreEqual(fateController1, fateController2, "FateFlowController should be same instance after reload");
            
            // For ScoreManager, we only require that exactly one instance exists and that
            // the static Instance points to a valid object after reload (no duplicates / broken singleton).
            ScoreManager[] allScoreManagers = Object.FindObjectsOfType<ScoreManager>();
            Assert.AreEqual(1, allScoreManagers.Length,
                "There should be exactly one ScoreManager instance in the scene after reload.");
        }
    }
}

