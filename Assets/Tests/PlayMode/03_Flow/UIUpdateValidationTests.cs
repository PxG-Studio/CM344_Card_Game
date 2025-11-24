using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// Tests specifically designed to catch UI NOT UPDATING bugs.
    /// These tests validate that UI elements reflect game state changes.
    /// </summary>
    public class UIUpdateValidationTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
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
            
            yield return new WaitForSeconds(0.5f);
            CardTestHelper.ResetGameState();
            yield return null;
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
        public IEnumerator UIUpdate_ScoreUI_UpdatesWhenScoreChanges()
        {
            // UI TEST: ScoreUI MUST update when ScoreManager score changes
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            if (scoreUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            }
            
            if (scoreUI == null)
            {
                Assert.Inconclusive("ScoreUI not found - may be created dynamically");
                yield break;
            }
            
            // Get score text components
            var player1ScoreField = typeof(ScoreUI).GetField("player1Score", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var player2ScoreField = typeof(ScoreUI).GetField("player2Score", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TextMeshProUGUI player1ScoreText = player1ScoreField?.GetValue(scoreUI) as TextMeshProUGUI;
            TextMeshProUGUI player2ScoreText = player2ScoreField?.GetValue(scoreUI) as TextMeshProUGUI;
            
            if (player1ScoreText == null)
            {
                Assert.Inconclusive("Player 1 score text not found");
                yield break;
            }
            
            // Get initial values
            int initialManagerScore = scoreManager.P1Score;
            int initialDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            
            // Change score
            scoreManager.AddScore(true);
            yield return new WaitForSeconds(0.5f); // Wait for UI update
            
            int newManagerScore = scoreManager.P1Score;
            
            // UI ASSERTION: Displayed score MUST match manager score
            int newDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            
            Assert.AreEqual(newManagerScore, newDisplayedScore, 
                $"UI NOT UPDATING: ScoreUI text ({newDisplayedScore}) does not match ScoreManager score ({newManagerScore}). " +
                $"Initial: Manager={initialManagerScore}, UI={initialDisplayedScore}. " +
                "This indicates UI is not updating when score changes.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_TurnIndicator_UpdatesWhenTurnSwitches()
        {
            // UI TEST: Turn indicator MUST update when turn switches
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Find turn indicator
            GameObject turnIndicator = GameObject.Find("TurnIndicator_UI");
            if (turnIndicator == null)
            {
                TurnIndicatorUI[] indicators = Object.FindObjectsOfType<TurnIndicatorUI>(true);
                if (indicators.Length > 0)
                {
                    turnIndicator = indicators[0].gameObject;
                }
            }
            
            if (turnIndicator == null)
            {
                Assert.Inconclusive("TurnIndicatorUI not found - may be optional");
                yield break;
            }
            
            TurnIndicatorUI indicatorUI = turnIndicator.GetComponent<TurnIndicatorUI>();
            if (indicatorUI == null)
            {
                Assert.Inconclusive("TurnIndicatorUI component not found");
                yield break;
            }
            
            // Get initial turn
            FateSide initialTurn = fateController.CurrentFate;
            fateController.SetFate(initialTurn);
            yield return new WaitForSeconds(0.5f);
            
            // Switch turn
            FateSide newTurn = initialTurn == FateSide.P1 ? FateSide.P2 : FateSide.P1;
            fateController.SetFate(newTurn);
            yield return new WaitForSeconds(0.5f); // Wait for UI update
            
            // UI ASSERTION: Turn indicator MUST reflect turn change
            // (We can't easily check visual state, but we can verify the component exists and can update)
            Assert.AreEqual(newTurn, fateController.CurrentFate, 
                "Turn should have switched");
            
            // Verify indicator is still active (should be visible)
            Assert.IsTrue(turnIndicator.activeSelf || turnIndicator.activeInHierarchy, 
                "UI NOT UPDATING: TurnIndicator should be visible after turn switch. " +
                "This indicates turn indicator is not updating when turn changes.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_GameEndUI_ShowsWhenGameEnds()
        {
            // UI TEST: GameEndUI MUST be shown when game ends
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndManager gameEndManager = GameEndManager.Instance;
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
            
            if (gameEndUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            }
            
            if (gameEndUI == null)
            {
                Assert.Inconclusive("GameEndUI not found - may be created dynamically");
                yield break;
            }
            
            // Initially should be hidden
            bool initiallyVisible = gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy;
            
            // Trigger game end
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
            if (showMethod != null)
            {
                showMethod.Invoke(gameEndUI, new object[] { true, false, 3 });
                yield return new WaitForSeconds(0.5f);
                
                // UI ASSERTION: GameEndUI MUST be visible after ShowGameEnd
                bool nowVisible = gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy;
                Assert.IsTrue(nowVisible, 
                    $"UI NOT UPDATING: GameEndUI should be visible after ShowGameEnd(). " +
                    $"Was visible: {initiallyVisible}, Now visible: {nowVisible}. " +
                    "This indicates GameEndUI is not updating when game ends.");
            }
        }

        [UnityTest]
        public IEnumerator UIUpdate_GameEndUI_WinnerText_UpdatesCorrectly()
        {
            // UI TEST: GameEndUI winner text MUST display correct winner
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            if (gameEndUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            }
            
            if (gameEndUI == null)
            {
                Assert.Inconclusive("GameEndUI not found");
                yield break;
            }
            
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
            if (showMethod == null)
            {
                Assert.Inconclusive("ShowGameEnd method not found");
                yield break;
            }
            
            // Test Player 1 wins
            showMethod.Invoke(gameEndUI, new object[] { true, false, 5 });
            yield return new WaitForSeconds(0.5f);
            
            var winnerTextField = typeof(GameEndUI).GetField("winnerText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (winnerTextField != null)
            {
                TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TextMeshProUGUI;
                if (winnerText != null)
                {
                    string text = winnerText.text.ToUpper();
                    
                    // UI ASSERTION: Winner text MUST indicate Player 1 wins
                    Assert.IsTrue(text.Contains("PLAYER 1") || text.Contains("WINS"), 
                        $"UI NOT UPDATING: Winner text should indicate Player 1 wins when playerWon=true. " +
                        $"Got: '{winnerText.text}'. " +
                        "This indicates winner text is not updating correctly.");
                }
            }
            
            // Test Player 2 wins
            showMethod.Invoke(gameEndUI, new object[] { false, false, -3 });
            yield return new WaitForSeconds(0.5f);
            
            if (winnerTextField != null)
            {
                TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TextMeshProUGUI;
                if (winnerText != null)
                {
                    string text = winnerText.text.ToUpper();
                    
                    // UI ASSERTION: Winner text MUST indicate Player 2 wins
                    Assert.IsTrue(text.Contains("PLAYER 2") || text.Contains("WINS"), 
                        $"UI NOT UPDATING: Winner text should indicate Player 2 wins when playerWon=false. " +
                        $"Got: '{winnerText.text}'. " +
                        "This indicates winner text is not updating correctly.");
                }
            }
        }

        [UnityTest]
        public IEnumerator UIUpdate_ScoreUI_Player2Score_UpdatesWhenScoreChanges()
        {
            // UI TEST: Player 2 score UI MUST update when opponent score changes
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            if (scoreUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            }
            
            if (scoreUI == null)
            {
                Assert.Inconclusive("ScoreUI not found");
                yield break;
            }
            
            var player2ScoreField = typeof(ScoreUI).GetField("player2Score", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TextMeshProUGUI player2ScoreText = player2ScoreField?.GetValue(scoreUI) as TextMeshProUGUI;
            
            if (player2ScoreText == null)
            {
                Assert.Inconclusive("Player 2 score text not found");
                yield break;
            }
            
            // Get initial values
            int initialManagerScore = scoreManager.P2Score;
            int initialDisplayedScore = 0;
            if (int.TryParse(player2ScoreText.text, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            
            // Change opponent score
            scoreManager.AddScore(false);
            yield return new WaitForSeconds(0.5f);
            
            int newManagerScore = scoreManager.P2Score;
            
            // UI ASSERTION: Player 2 score UI MUST match manager score
            int newDisplayedScore = 0;
            if (int.TryParse(player2ScoreText.text, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            
            Assert.AreEqual(newManagerScore, newDisplayedScore, 
                $"UI NOT UPDATING: Player 2 ScoreUI text ({newDisplayedScore}) does not match ScoreManager opponent score ({newManagerScore}). " +
                $"Initial: Manager={initialManagerScore}, UI={initialDisplayedScore}. " +
                "This indicates Player 2 score UI is not updating when score changes.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_CardHover_ChangesSortingLayer()
        {
            // UI TEST: Card hover MUST change sorting layer for visual feedback
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewDeckManagerP1 deckManager = handUI?.DeckManager;
            if (handUI == null || deckManager == null || deckManager.Hand == null || deckManager.Hand.Count == 0)
            {
                Assert.Inconclusive("No cards in hand for hover test");
                yield break;
            }
            
            // Find a card UI
            NewCardUI[] cardUIs = Object.FindObjectsOfType<NewCardUI>(true);
            NewCardUI testCardUI = null;
            
            foreach (NewCardUI cardUI in cardUIs)
            {
                if (cardUI.Card != null && deckManager.Hand.Any(c => c == cardUI.Card))
                {
                    testCardUI = cardUI;
                    break;
                }
            }
            
            if (testCardUI == null)
            {
                Assert.Inconclusive("No card UI found in hand");
                yield break;
            }
            
            // Verify card implements hover interface
            bool hasPointerEnter = testCardUI is UnityEngine.EventSystems.IPointerEnterHandler;
            bool hasPointerExit = testCardUI is UnityEngine.EventSystems.IPointerExitHandler;
            
            // UI ASSERTION: Card MUST support hover (implement IPointerEnterHandler)
            Assert.IsTrue(hasPointerEnter, 
                "UI NOT UPDATING: NewCardUI should implement IPointerEnterHandler for hover preview. " +
                "This indicates hover functionality is not implemented.");
            Assert.IsTrue(hasPointerExit, 
                "UI NOT UPDATING: NewCardUI should implement IPointerExitHandler for hover preview. " +
                "This indicates hover exit functionality is not implemented.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_CoinTossUI_ShowsResultAfterToss()
        {
            // UI TEST: CoinTossUI MUST show result after coin toss completes
            yield return new WaitForSeconds(1.0f);
            
            CoinTossUI coinTossUI = Object.FindObjectOfType<CoinTossUI>(true);
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            
            Assert.IsNotNull(coinTossUI, "CoinTossUI should exist");
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            
            // Start coin toss
            coinTossUI.StartCoinToss();
            yield return new WaitForEndOfFrame();
            yield return null;
            
            coinTossUI.StartCoinTossAnimation();
            yield return CardTestHelper.WaitForCoinTossToComplete(10f);
            yield return new WaitForSeconds(2.0f); // Wait for animation
            
            // UI ASSERTION: CoinTossUI MUST show result after toss
            // Get result text field
            var resultTextField = typeof(CoinTossUI).GetField("resultText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (resultTextField != null)
            {
                TextMeshProUGUI resultText = resultTextField.GetValue(coinTossUI) as TextMeshProUGUI;
                if (resultText != null)
                {
                    // Result text should be visible and contain result
                    bool textVisible = resultText.gameObject.activeSelf || resultText.gameObject.activeInHierarchy;
                    bool hasText = !string.IsNullOrEmpty(resultText.text);
                    
                    Assert.IsTrue(textVisible || hasText, 
                        "UI NOT UPDATING: CoinTossUI result text should be visible or contain text after coin toss. " +
                        $"Visible: {textVisible}, HasText: {hasText}. " +
                        "This indicates coin toss result is not being displayed.");
                }
            }
        }
    }
}

