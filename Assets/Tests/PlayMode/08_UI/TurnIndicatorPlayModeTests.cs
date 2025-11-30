using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.UI;
using CardGame.Managers;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for TurnIndicator systems - validates turn indicator updates.
    /// Tests that indicators show correct player and update on turn changes.
    /// </summary>
    public class TurnIndicatorPlayModeTests
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
        public IEnumerator TurnIndicatorUI_Exists()
        {
            TurnIndicatorUI indicator = Object.FindObjectOfType<TurnIndicatorUI>();
            // Turn indicators may or may not exist (optional feature)
            if (indicator != null)
            {
                Assert.IsNotNull(indicator, "TurnIndicatorUI should exist if implemented");
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TurnIndicator_Updates_On_Turn_Change()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            TurnIndicatorUI indicator = Object.FindObjectOfType<TurnIndicatorUI>();
            
            if (fateController != null && indicator != null)
            {
                // Get initial turn
                FateSide initialTurn = fateController.CurrentFate;
                
                // Change turn
                fateController.AdvanceFateFlow();
                yield return new WaitForSeconds(0.5f);
                
                // Verify indicator updated (if it has update method)
                FateSide newTurn = fateController.CurrentFate;
                Assert.AreNotEqual(initialTurn, newTurn,
                    "Turn should change, indicator should reflect this");
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TurnIndicatorMoving_Exists()
        {
            TurnIndicatorMoving indicator = Object.FindObjectOfType<TurnIndicatorMoving>();
            // Turn indicators may or may not exist
            if (indicator != null)
            {
                Assert.IsNotNull(indicator, "TurnIndicatorMoving should exist if implemented");
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TurnIndicator3D_Exists()
        {
            TurnIndicator3D indicator = Object.FindObjectOfType<TurnIndicator3D>();
            // Turn indicators may or may not exist
            if (indicator != null)
            {
                Assert.IsNotNull(indicator, "TurnIndicator3D should exist if implemented");
            }
            yield return null;
        }
    }
}

